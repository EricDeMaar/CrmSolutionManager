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
using SolutionManager.Logic.Logging;
using SolutionManager.Logic.Results;
using SolutionManager.Logic.Sdk;

namespace SolutionManager.Logic.Messages
{
    public class ImportSolutionMessage : Message
    {
        /// <summary>
        /// Gets or sets the FileStream containing the Dynamics CRM solution file.
        /// </summary>
        public FileStream SolutionFileStream { get; set; }

        /// <summary>
        /// Gets or sets whether the Dynamics CRM solution should be imported
        /// using the holding solution mechanism.
        /// </summary>
        public bool HoldingSolution { get; set; }

        /// <summary>
        /// Gets or sets whether the system should be directed to convert any
        /// matching unmanaged customizations into your managed solution.
        /// </summary>
        public bool ConvertToManaged { get; set; }

        /// <summary>
        /// Gets or sets whether any unmanaged customizations that have been
        /// applied over existing managed solution components should be overwritten. 
        /// </summary>
        public bool OverwriteUnmanagedCustomizations { get; set; }

        /// <summary>
        /// Gets or sets whether any processes (workflows) included in the
        /// solution should be activated after they are imported.
        /// </summary>
        public bool PublishWorkflows { get; set; }

        /// <summary>
        /// Gets or sets whether enforcement of dependencies related to product
        /// updates should be skipped.
        /// </summary>
        public bool SkipProductUpdateDependencies { get; set; }

        /// <summary>
        /// Gets or sets whether an existing solution should be overwritten in case that
        /// a solution with the same version exists in the target environment.
        /// </summary>
        public bool OverwriteIfSameVersionExists { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the import job that will be
        /// created to perform the SolutionImportRequest.
        /// </summary>
        private Guid ImportJobId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the asynchronous job that will be
        /// created to perform the SolutionImportRequest.
        /// </summary>
        private Guid AsyncJobId { get; set; }

        /// <summary>
        /// Returns the file name of the SolutionFileStream object.
        /// </summary>
        private string FileName
        {
            get
            {
                return Path.GetFileName(SolutionFileStream.Name);
            }
        }


        public ImportSolutionMessage(CrmOrganization organization) : base(organization)
        {
            this.ImportJobId = Guid.NewGuid();
            this.AsyncJobId = Guid.NewGuid();
        }

        /// <summary>
        /// Imports a Dynamics CRM solution to a Dynamics CRM organization.
        /// </summary>
        /// <returns>An <seealso cref="ImportSolutionResult"/> object</returns>
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
                    SkipProductUpdateDependencies = this.SkipProductUpdateDependencies
                };

                ExecuteAsyncRequest asyncRequest = new ExecuteAsyncRequest()
                {
                    Request = importSolutionRequest
                };

                ExecuteAsyncResponse asyncResponse = this.CrmOrganization.Execute<ExecuteAsyncResponse>(asyncRequest);
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

            ImportSolutionProgress(this.ImportJobId);

            job = this.CrmOrganization.GetEntityByField("importjob", "importjobid", this.ImportJobId);
            ImportSolutionResult status = CreateImportStatus(job);
            Logger.Log($"Solution {this.FileName} was imported with status {status.Status.ToString()}", LogLevel.Info);
            return status;
        }

        /// <summary>
        /// Reads the Solution XML from a given Zip Archive.
        /// </summary>
        /// <param name="zip">A FileStream object containing the Zip archive.</param>
        /// <returns>A <seealso cref="Solution"/> object</returns>
        private Solution ReadSolutionXmlFromZip(FileStream zip)
        {
            using (var zipfile = new ZipArchive(zip, ZipArchiveMode.Read))
            {
                ZipArchiveEntry solutionInfo = zipfile.Entries.First(x => x.Name == "solution.xml");

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

        /// <summary>
        /// Compares the version of a Dynamics CRM solution in a target environment
        /// with a given version.
        /// </summary>
        /// <param name="version">The version which has to be checked.</param>
        /// <param name="solutionName">The unique name of the solution in the target environment.</param>
        /// <returns></returns>
        private bool CompareSolutionVersion(Version version, string solutionName)
        {
            var message = new RetrieveSolutionDataMessage(this.CrmOrganization)
            {
                UniqueName = solutionName,
            };

            var result = (RetrieveSolutionDataResult)this.CrmOrganization.Execute(message);

            if (result.Solution == null)
            {
                Logger.Log($"The solution {solutionName} was not found in the target system.", LogLevel.Info);
                return true;
            }

            if (version == result.Solution.GetVersion())
            {
                if (this.OverwriteIfSameVersionExists)
                {
                    Logger.Log($"Found solution {result.Solution.UniqueName} with the same version {result.Solution.Version} in target system. Overwriting...", LogLevel.Info);
                    return true;
                }

                Logger.Log($"Found solution {result.Solution.UniqueName} in target system - version {result.Solution.Version} is already loaded.", LogLevel.Info);
                return false;
            }

            if (version < result.Solution.GetVersion())
            {
                Logger.Log($"Found solution {result.Solution.UniqueName} in target system - a higher version ({result.Solution.Version}) is already loaded.", LogLevel.Info);
                return false;
            }
            else if (version > result.Solution.GetVersion())
            {
                Logger.Log($"Found solution {result.Solution.UniqueName} with lower version {result.Solution.Version} in target system - starting update.", LogLevel.Info);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves the status of an ImportJob and logs the import progress.
        /// </summary>
        /// <param name="importJobId">The unique identifier of the ImportJob.</param>
        private void ImportSolutionProgress(Guid importJobId)
        {
            while (true)
            {
                // Make sure that the request is fired after the ImportSolutionRequest has been executed
                Thread.Sleep(5000);
                    
                var job = this.CrmOrganization.GetEntityByField("importjob", "importjobid", importJobId);

                if (job == null)
                {
                    Logger.Log($"Job with id {importJobId} was not found", LogLevel.Debug);
                    continue;
                }

                decimal progress = Convert.ToDecimal(job["progress"]);

                if (progress == 100)
                {
                    Logger.Log("Solution import is at 100%", LogLevel.Info);
                    break;
                }

                // Todo: Nice this up
                Console.Write($"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}] - [INFO] - Solution import is at {progress.ToString("N0")}%\r");
            }
        }

        /// <summary>
        /// Reads the solution import result from a given Job entity record.
        /// </summary>
        /// <param name="job">An ImportJob entity object.</param>
        /// <returns>An <seealso cref="ImportSolutionResult"/> object.</returns>
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
                        string.IsNullOrEmpty(errorCode) ? 0 :
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
