using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using SolutionManager.Logic.Configuration;

namespace SolutionManager.Logic.DynamicsCrm
{
    [DebuggerDisplay("CrmContext")]
    public class CrmOrganization : IDisposable
    {
        #region Constructor + Crm Context
        public CrmOrganization(string url, int? timeOutInMinutes = null) : this(CreateOrganizationService(url, timeOutInMinutes)) { }

        public CrmOrganization(string url, int? timeOutInMinutes, string user, string password, string domain) : this(CreateOrganizationService(url, timeOutInMinutes, user, password, domain)) { }

        private static IOrganizationService CreateOrganizationService(string url, int? timeOutInMinutes = null, string user = null, string password = null, string domain = null)
        {
            // Connect to the CRM web service using a connection string.
            string connectionString = $@"Url={url};";

            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
                connectionString += $"Username={user};Password={password};";

            connectionString += "authtype=Office365";

            CrmServiceClient conn = new CrmServiceClient(connectionString);

            // Cast the proxy client to the IOrganizationService interface.
            var orgService = (IOrganizationService)conn.OrganizationWebProxyClient != null ? (IOrganizationService)conn.OrganizationWebProxyClient : (IOrganizationService)conn.OrganizationServiceProxy;

            IServiceManagement<IOrganizationService> serviceManagement = ServiceConfigurationFactory.CreateManagement<IOrganizationService>(new Uri(url));

            return orgService;
        }

        public CrmOrganization(IOrganizationService organizationService)
        {
            OrganizationService = organizationService;
        }

        public IOrganizationService OrganizationService { get; }
        #endregion

        #region Helpers for Execute & Retrieve & Update & Delete
        [DebuggerStepThrough]
        public void Execute(OrganizationRequest request)
        {
            OrganizationService.Execute(request);
        }

        [DebuggerStepThrough]
        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            return OrganizationService.Retrieve(entityName, id, columnSet);
        }

        [DebuggerStepThrough]
        public TResponse Execute<TResponse>(OrganizationRequest request) where TResponse : OrganizationResponse
        {
            return (TResponse)OrganizationService.Execute(request);
        }

        [DebuggerStepThrough]
        public Entity RetrieveFetchXmlSingle(string fetchxml)
        {
            var retrieved = RetrieveFetchXml(fetchxml).ToList();

            switch (retrieved.Count)
            {
                case 0:
                    return null;
                case 1:
                    return retrieved.First();
                default:
                    throw new Exception($"Expected one, retrieved multiple at: {fetchxml}");
            }
        }

        [DebuggerStepThrough]
        public IEnumerable<Entity> RetrieveFetchXml(string fetchxml)
        {
            var result = OrganizationService.RetrieveMultiple(new FetchExpression(fetchxml));
            return result.Entities;
        }

        public void Delete(string entityName, Guid id)
        {
            OrganizationService.Delete(entityName, id);
        }
        #endregion

        public Solution GetSolutionByName(string solutionName)
        {
            QueryExpression querySolution = new QueryExpression
            {
                EntityName = Solution.EntityLogicalName,
                ColumnSet = new ColumnSet(new string[] { "installedon", "version", "versionnumber", "friendlyname", "uniquename" }),
                Criteria = new FilterExpression()
            };

            querySolution.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionName);
            Solution solution = (Solution)OrganizationService.RetrieveMultiple(querySolution).Entities[0];

            return solution;
        }

        public Entity GetEntityByField(string entityLogicalName, string fieldName, object value)
        {
            var entity = this.RetrieveFetchXmlSingle(
                             $@"<fetch>
                              <entity name=""{entityLogicalName}"">
                                <filter>
                                  <condition attribute=""{fieldName}"" operator=""eq"" value=""{value}"" />
                                </filter>
                              </entity>
                            </fetch>");

            return entity;
        }

        public SolutionImportResult ImportSolution(Stream customizationFileStream, SolutionFile solution, bool showImportProgress)
        {
            if (customizationFileStream == null)
                throw new ArgumentNullException(nameof(customizationFileStream));

            var buffer = new byte[(int)customizationFileStream.Length];
            customizationFileStream.Read(buffer, 0, buffer.Length);

            Entity job;

            Guid importJobId = Guid.NewGuid();
            Guid? asyncJobId = Guid.NewGuid();
            Thread t = null;

            if (showImportProgress)
            {
                t = new Thread(new ParameterizedThreadStart(ImportSolutionProgress));
                t.Start(importJobId);
            }

            try
            {
                var importSolutionRequest = new ImportSolutionRequest
                {
                    ConvertToManaged = solution.ImportSettings.ConvertToManaged,
                    CustomizationFile = buffer,
                    ImportJobId = importJobId,
                    OverwriteUnmanagedCustomizations = solution.ImportSettings.OverwriteUnmanagedCustomizations,
                    PublishWorkflows = solution.ImportSettings.PublishWorkflows,
                    SkipProductUpdateDependencies = solution.ImportSettings.SkipProductDependencies
                };

                ExecuteAsyncRequest asyncRequest = new ExecuteAsyncRequest()
                {
                    Request = importSolutionRequest
                };
                ExecuteAsyncResponse asyncResponse = this.Execute<ExecuteAsyncResponse>(asyncRequest) as ExecuteAsyncResponse;

                asyncJobId = asyncResponse.AsyncJobId;

                DateTime end = DateTime.Now.AddSeconds(10);
                while (end >= DateTime.Now)
                {
                    Entity asyncOperation = this.OrganizationService.Retrieve("asyncoperation", asyncJobId.Value,
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
                    return CreateImportStatus(this.GetEntityByField("importjob", "importjobid", (Guid)importJobId));
                }

                throw;
            }

            t?.Join();
            job = this.GetEntityByField("importjob", "importjobid", (Guid)importJobId);

            SolutionImportResult status = CreateImportStatus(job);

            Console.WriteLine($@"Solution {solution.FileName} was imported with status {status.Status.ToString()}");
            return status;
        }

        public bool ExportSolution(string uniqueName, string outputFile, bool exportAsManaged)
        {
            if (uniqueName == null || outputFile == null)
                return false;

            var result = this.Execute<ExportSolutionResponse>(new ExportSolutionRequest
            {
                SolutionName = uniqueName,
                Managed = exportAsManaged,
            });

            byte[] exportXml = result.ExportSolutionFile;

            if (File.Exists(outputFile))
            {
                try
                {
                    File.Delete(outputFile);
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }

            File.WriteAllBytes(outputFile, exportXml);

            return true;
        }

        public bool DeleteSolution(string uniqueName)
        {
            var solution = this.GetSolutionByName(uniqueName);

            if (solution == null)
            {
                Console.WriteLine($@"The solution {uniqueName} was not found in the target system");
                return false;
            }

            Console.WriteLine($@"Deleting solution {solution.UniqueName} with version {solution.Version} from target system.");

            this.Delete("solution", solution.SolutionId);

            var retrieveSolution = this.GetEntityByField("solution", "solutionid", solution.SolutionId);

            if (retrieveSolution != null)
            {
                Console.WriteLine("The solution still exists.");
                return false;
            }

            Console.WriteLine("The solution has been deleted.");
            return true;
        }

        public bool CompareSolutionVersion(Version version, string solutionName, bool overwriteIfSameVersion)
        {
            Solution solution = this.GetSolutionByName(solutionName);

            if (solution == null)
            {
                Console.WriteLine($@"The solution {solutionName} was not found in the target system.");
                return true;
            }

            if (solution != null)
            {
                if (version == solution.GetVersion())
                {
                    if (overwriteIfSameVersion)
                    {
                        Console.WriteLine($@"Found solution {solution.UniqueName} with the same version {solution.Version} in target system. Overwriting...");

                        return true;
                    }

                    Console.WriteLine($@"Found solution {solution.UniqueName} in target system - version {solution.Version} is already loaded.");

                    return false;
                }

                if (version < solution.GetVersion())
                {
                    Console.WriteLine($@"Found solution {solution.UniqueName} in target system - a higher version ({solution.Version}) is already loaded.");

                    return false;
                }

                if (version > solution.GetVersion())
                {
                    Console.WriteLine($@"Found solution {solution.UniqueName} with lower version {solution.Version} in target system - starting update.");

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

                var job = this.GetEntityByField("importjob", "importjobid", (Guid)importJobId);

                if (job == null)
                    continue;

                decimal progress = Convert.ToDecimal(job["progress"]);

                if (progress == 100)
                {
                    Console.WriteLine("Solution import is at 100%");
                    break;
                }

                Console.Write("Solution import is at {0:N0}%\r", progress);
            }
        }

        private SolutionImportResult CreateImportStatus(Entity job)
        {
            using (var reader = new StringReader(job["data"] as string))
            {
                XDocument doc = XDocument.Load(reader);
                var result = doc.Descendants("solutionManifests").Descendants().First().Descendants("result").First();
                string errorCode = result.Attribute("errorcode").Value;
                ImportResultStatus status;
                Enum.TryParse(result.Attribute("result").Value, true, out status);

                return new SolutionImportResult
                {
                    ImportJobId = job.Id,
                    ErrorCode =
                        errorCode.StartsWith("0x")
                        ? int.Parse(errorCode.Substring(2), NumberStyles.HexNumber)
                        : int.Parse(errorCode),
                    ErrorMessage = result.Attribute("errortext").Value,
                    Status = status,
                    Data = job["data"] as string
                };
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // For example: OrganizationServiceProxy is IDisposable.
                    if (OrganizationService is IDisposable)
                    {
                        ((IDisposable)OrganizationService).Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
