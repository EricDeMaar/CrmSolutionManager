using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Xrm.Sdk;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.App.Helpers;
using SolutionManager.App.Configuration;
using SolutionManager.App.Configuration.WorkItems;
using SolutionManager.Logic.Messages;

namespace SolutionManager.App
{
    class Program
    {
        private static readonly string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string _solutionsDirectory = Path.Combine(_baseDirectory, @"Solutions\");
        private static readonly string _importConfig = Path.Combine(_solutionsDirectory, "ImportConfig.xml");

        private static void PrintAbortMessage() => Console.WriteLine("Aborting import process. ContinueOnError is false.");

        static void Main(string[] args)
        {
            Console.SetWindowSize(150, 25);

            //GenerateXml();

            using (var xml = XElement.Load(_importConfig).CreateReader())
            {
                ImportConfiguration config = null;
                try
                {
                    config = new XmlSerializer(typeof(ImportConfiguration)).Deserialize(xml) as ImportConfiguration;
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"{CurrentTime()} - Error reading configuration '{_importConfig}'. Exception: {exception.Message}");
                }

                if (config == null)
                {
                    return;
                }

                config.Validate();

                foreach (WorkItem workItem in config.WorkItems)
                {
                    bool validated = true; //workItem.Validate();

                    if (!validated && !workItem.ContinueOnError)
                    {
                        PrintAbortMessage();
                        return;
                    }

                    if (!validated && workItem.ContinueOnError)
                        continue;

                    Organization org = config.Organizations.Where(x => x.OrganizationName == workItem.OrganizationName).FirstOrDefault();

                    if (org == null)
                        throw new NullReferenceException($"Organization with name {workItem.OrganizationName} was not found in the config.");

                    var crm = OrganizationHelper.CreateOrganizationService(org);

                    ExecuteAction(crm, workItem);
                }
            }

            Console.WriteLine($"{CurrentTime()} - All solution files have been processed. Press any key to exit.");
            Console.ReadKey();
        }

        private static void ExecuteAction(IOrganizationService crm, WorkItem workItem)
        {
            if (workItem is ImportSolutionWorkItem)
            {
                ExecuteImport(crm, (ImportSolutionWorkItem)workItem);
                return;
            }

            if (workItem is ExportSolutionWorkItem)
            {
                ExecuteExport(crm, (ExportSolutionWorkItem)workItem);
                return;
            }

            if (workItem is DeleteSolutionWorkItem)
            {
                ExecuteDelete(crm, (DeleteSolutionWorkItem)workItem);
                return;
            }

            throw new InvalidOperationException($"WorkItem type {workItem.GetType()} is not supported.");
        }

        private static void ExecuteImport(IOrganizationService orgService, ImportSolutionWorkItem importSolution)
        {
            SolutionData solutionData = SolutionHelper.ReadVersionFromZip(importSolution.FileName);

            using (var crm = new CrmOrganization(orgService))
            {
                using (FileStream zip = File.Open(Path.Combine(_baseDirectory, $@"Solutions\{importSolution.FileName}"), FileMode.Open))
                {
                    Console.WriteLine($"{CurrentTime()} - Starting with import of {importSolution.FileName}");
                    var message = new ImportSolutionMessage(crm)
                    {
                        FileName = importSolution.FileName,
                        SolutionFileStream = zip,
                        HoldingSolution = importSolution.HoldingSolution,
                        OverwriteIfSameVersionExists = importSolution.OverwriteIfSameVersionExists,
                        OverwriteUnmanagedCustomizations = importSolution.OverwriteUnmanagedCustomizations,
                        PublishWorkflows = importSolution.PublishWorkflows,
                        SkipProductDependencies = importSolution.SkipProductDependencies,
                    };

                    crm.ExecuteMessage(message);
                }
            }
        }

        private static void ExecuteExport(IOrganizationService orgService, ExportSolutionWorkItem exportSolution)
        {
            Console.WriteLine($"{CurrentTime()} - Exporting solution {exportSolution.UniqueName}.");
            var writeToFile = Path.Combine(_solutionsDirectory, exportSolution.WriteToZipFile);

            using (var crm = new CrmOrganization(orgService))
            {
                var message = new ExportSolutionMessage(crm)
                {
                    UniqueName = exportSolution.UniqueName,
                    ExportAsManaged = exportSolution.ExportAsManaged,
                    OutputFile = writeToFile,
                };

                crm.ExecuteMessage(message);
            }
        }

        public static void ExecuteDelete(IOrganizationService orgService, DeleteSolutionWorkItem deleteSolution)
        {
            if (deleteSolution.UniqueName == null && deleteSolution.ContinueOnError)
                return;

            if (deleteSolution.UniqueName == null && !deleteSolution.ContinueOnError)
            {
                PrintAbortMessage();
            }

            using (var crm = new CrmOrganization(orgService))
            {
                var message = new DeleteSolutionMessage(crm)
                {
                    UniqueName = deleteSolution.UniqueName,
                };

                crm.ExecuteMessage(message);
            }
        }

        private static string GenerateXml()
        {
            ImportConfiguration x = new ImportConfiguration();

            x.Name = "WorkDefinition 1";
            x.Description = "This is a WorkDefinition.";
            x.Organizations = new Organization[]
            {
                new Organization()
                {
                    DomainName = "domain",
                    OrganizationName = "OrganizationName",
                    OrganizationUri = "organizationUri",
                    Password = "Password",
                    UserName = "UserName",
                }
            };
            x.WorkItems = new WorkItem[]
            {
                new ImportSolutionWorkItem()
                {
                    ContinueOnError = true,
                    FileName = "FileName",
                    OrganizationName = "OrganizationName",
                    OverwriteIfSameVersionExists = true,
                    OverwriteUnmanagedCustomizations = true,
                    PublishWorkflows = true,
                    ShowImportProgress = true,
                    SkipProductDependencies = false,
                },
                new DeleteSolutionWorkItem()
                {
                    UniqueName = "SolutionName",
                    ContinueOnError = true,
                    OrganizationName = "OrganizationName"
                }
            };

            try
            {
                var xmlSerializer = new XmlSerializer(typeof(ImportConfiguration));

                using (StringWriter textWriter = new StringWriter())
                {
                    xmlSerializer.Serialize(textWriter, x);
                    var lala = textWriter.ToString();
                    return lala;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{CurrentTime()} - Error reading configuration '{_importConfig}'. Exception: {exception.Message}");
            }

            return "asdf";
        }

        private static string CurrentTime() => $"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}]";
    }
}