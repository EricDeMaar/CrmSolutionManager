using System;
using System.Threading;
using System.ServiceModel;
using System.IO;
using System.Globalization;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Helpers;
using SolutionManager.Logic.Results;
using SolutionManager.Logic.Sdk;

namespace SolutionManager.Logic.Messages
{
    public class ImportSolutionMessage : Message
    {
        public string FileName { get; set; }
        public FileStream SolutionFileStream { get; set; }
        public bool HoldingSolution { get; set; }
        public bool ConvertToManaged { get; set; }
        public bool OverwriteUnmanagedCustomizations { get; set; }
        public bool PublishWorkflows { get; set; }
        public bool SkipProductDependencies { get; set; }
        public bool OverwriteIfSameVersionExists { get; set; }

        private Guid ImportJobId { get; set; }
        private Guid AsyncJobId { get; set; }
        private Thread ProgressPollingThread { get; set; }

        public ImportSolutionMessage(CrmOrganization organization) : base(organization)
        {
            this.ImportJobId = Guid.NewGuid();
            this.AsyncJobId = Guid.NewGuid();
            this.ProgressPollingThread = null;
        }

        public override Result Execute()
        {
            if (this.SolutionFileStream == null)
                throw new ArgumentNullException(nameof(this.SolutionFileStream));

            var buffer = new byte[(int)this.SolutionFileStream.Length];
            this.SolutionFileStream.Read(buffer, 0, buffer.Length);

            Entity job;

            this.ProgressPollingThread = new Thread(new ParameterizedThreadStart(ImportSolutionProgress));
            this.ProgressPollingThread.Start(this.ImportJobId);

            try
            {
                var importSolutionRequest = new ImportSolutionRequest
                {
                    HoldingSolution = this.HoldingSolution,
                    ConvertToManaged = this.ConvertToManaged,
                    CustomizationFile = buffer,
                    ImportJobId = this.ImportJobId,
                    OverwriteUnmanagedCustomizations = this.OverwriteUnmanagedCustomizations,
                    PublishWorkflows = this.PublishWorkflows,
                    SkipProductUpdateDependencies = this.SkipProductDependencies
                };

                ExecuteAsyncRequest asyncRequest = new ExecuteAsyncRequest()
                {
                    Request = importSolutionRequest
                };

                ExecuteAsyncResponse asyncResponse = this.CrmOrganization.Execute<ExecuteAsyncResponse>(asyncRequest) as ExecuteAsyncResponse;
                this.AsyncJobId = asyncResponse.AsyncJobId;

                DateTime end = DateTime.Now.AddSeconds(10);
                while (end >= DateTime.Now)
                {
                    Entity asyncOperation = this.CrmOrganization.Retrieve("asyncoperation", this.AsyncJobId,
                        new ColumnSet("asyncoperationid", "statuscode", "message"));

                    switch (asyncOperation.GetAttributeValue<OptionSetValue>("statuscode").Value)
                    {
                        // 30: Succeeded // 0: Ready
                        case 0:
                        case 30:
                            break;
                        // 21: Pausing // 22: Canceling // 31: Failed // 32: Canceled
                        case 21:
                        case 22:
                        case 31:
                        case 32:
                            throw new Exception(string.Format("Solution Import Failed: {0} {1}",
                            asyncOperation["statuscode"], asyncOperation["message"]));
                        default:
                            break;
                    }
                }
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                if (ex.Detail.ErrorCode == CrmErrorCodes.ImportSolutionError)
                {
                    // Todo: Log this to a file.
                    CreateImportStatus(this.CrmOrganization.GetEntityByField("importjob", "importjobid", this.ImportJobId));
                }

                throw;
            }

            this.ProgressPollingThread.Join();
            job = this.CrmOrganization.GetEntityByField("importjob", "importjobid", this.ImportJobId);

            ImportSolutionResult status = CreateImportStatus(job);

            Logger.Log($"Solution {this.FileName} was imported with status {status.Status.ToString()}");

            return status;
        }

        private bool CompareSolutionVersion(Version version, string solutionName)
        {
            Solution solution = this.CrmOrganization.GetSolutionByName(solutionName);

            if (solution == null)
            {
                Logger.Log($"The solution {solutionName} was not found in the target system.");
                return true;
            }

            if (solution != null)
            {
                if (version == solution.GetVersion())
                {
                    if (this.OverwriteIfSameVersionExists)
                    {
                        Logger.Log($"Found solution {solution.UniqueName} with the same version {solution.Version} in target system. Overwriting...");
                        return true;
                    }

                    Logger.Log($"Found solution {solution.UniqueName} in target system - version {solution.Version} is already loaded.");
                    return false;
                }

                if (version < solution.GetVersion())
                {
                    Logger.Log($"Found solution {solution.UniqueName} in target system - a higher version ({solution.Version}) is already loaded.");
                    return false;
                }

                if (version > solution.GetVersion())
                {
                    Logger.Log($"Found solution {solution.UniqueName} with lower version {solution.Version} in target system - starting update.");
                    return true;
                }
            }

            return false;
        }

        private void ImportSolutionProgress(object importJobId)
        {
            while (true)
            {
                // Make sure that the request is fired after the ImportSolutionRequest has been executed
                Thread.Sleep(5000);

                var job = this.CrmOrganization.GetEntityByField("importjob", "importjobid", (Guid)importJobId);

                if (job == null)
                    continue;

                decimal progress = Convert.ToDecimal(job["progress"]);

                if (progress == 100)
                {
                    Logger.Log("Solution import is at 100%");
                    break;
                }

                Console.Write($"Solution import is at {progress.ToString("N0")}%\r");
            }
        }

        private ImportSolutionResult CreateImportStatus(Entity job)
        {
            using (var reader = new StringReader(job["data"] as string))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(job.GetAttributeValue<string>("data"));

                string solutionImportResult = xmlDoc.SelectSingleNode("//solutionManifest/result/@result")?.Value;
                string errorCode = xmlDoc.SelectSingleNode("//solutionManifest/result/@errorcode")?.Value;
                string errorText = xmlDoc.SelectSingleNode("//solutionManifest/result/@errortext")?.Value;

                ImportResultStatus status;

                if (string.IsNullOrEmpty(solutionImportResult))
                {
                    status = ImportResultStatus.UnableToRetrieve;
                }
                else
                {
                    Enum.TryParse(solutionImportResult, true, out status);
                }

                return new ImportSolutionResult
                {
                    ImportJobId = job.Id,
                    ErrorCode =
                        errorCode.StartsWith("0x")
                        ? int.Parse(errorCode.Substring(2), NumberStyles.HexNumber)
                        : int.Parse(errorCode),
                    ErrorMessage = errorText,
                    Status = status,
                    Data = job["data"] as string
                };
            }
        }
    }
}
