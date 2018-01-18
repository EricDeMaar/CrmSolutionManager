using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
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

            Solution zipSolution = this.ReadSolutionXmlFromZip(this.SolutionFileStream);
            bool startImport = this.CompareSolutionVersion(zipSolution.GetVersion(), zipSolution.UniqueName);

            if (!startImport)
            {
                return new Result()
                {
                    Success = false
                };
            }

            Entity job;

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

            this.ProgressPollingThread = new Thread(new ParameterizedThreadStart(ImportSolutionProgress));
            this.ProgressPollingThread.Start(this.ImportJobId);
            this.ProgressPollingThread.Join();

            job = this.CrmOrganization.GetEntityByField("importjob", "importjobid", this.ImportJobId);

            ImportSolutionResult status = CreateImportStatus(job);

            Logger.Log($"Solution {this.FileName} was imported with status {status.Status.ToString()}");

            return status;
        }

        private Solution ReadSolutionXmlFromZip(FileStream zip)
        {
            using (var zipfile = new ZipArchive(zip, ZipArchiveMode.Read))
            {
                var solutionInfo = zipfile.Entries.First(x => x.Name == "solution.xml");

                using (var dataStream = solutionInfo.Open())
                {
                    var xml = XElement.Load(new StreamReader(dataStream)).CreateReader();
                    var data = new XmlSerializer(typeof(SolutionXml.ImportExportXml)).Deserialize(xml) as SolutionXml.ImportExportXml;

                    return new Solution()
                    {
                        UniqueName = data.SolutionManifest.UniqueName,
                        Version = data.SolutionManifest.Version.ToString(),
                    };
                }
            }
        }

        private bool CompareSolutionVersion(Version version, string solutionName)
        {
            var message = new RetrieveSolutionDataMessage(this.CrmOrganization)
            {
                UniqueName = solutionName,
            };

            var result = (RetrieveSolutionDataResult)this.CrmOrganization.ExecuteMessage(message);

            if (result.Solution == null)
            {
                Logger.Log($"The solution {solutionName} was not found in the target system.");
                return true;
            }

            if (version == result.Solution.GetVersion())
            {
                if (this.OverwriteIfSameVersionExists)
                {
                    Logger.Log($"Found solution {result.Solution.UniqueName} with the same version {result.Solution.Version} in target system. Overwriting...");
                    return true;
                }

                Logger.Log($"Found solution {result.Solution.UniqueName} in target system - version {result.Solution.Version} is already loaded.");
                return false;
            }

            if (version < result.Solution.GetVersion())
            {
                Logger.Log($"Found solution {result.Solution.UniqueName} in target system - a higher version ({result.Solution.Version}) is already loaded.");
                return false;
            }

            if (version > result.Solution.GetVersion())
            {
                Logger.Log($"Found solution {result.Solution.UniqueName} with lower version {result.Solution.Version} in target system - starting update.");
                return true;
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
                {
                    Logger.Log($"Job with id {importJobId} was not found");
                    continue;
                }

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
