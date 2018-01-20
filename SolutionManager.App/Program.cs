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
using SolutionManager.Logic.Logging;

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

            using (var xml = XElement.Load(_importConfig).CreateReader())
            {
                ImportConfiguration config = null;
                try
                {
                    config = new XmlSerializer(typeof(ImportConfiguration)).Deserialize(xml) as ImportConfiguration;
                }
                catch (Exception exception)
                {
                    Logger.Log($"Error reading configuration '{_importConfig}'. Exception: {exception.Message}", LogLevel.Debug);
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
                    {
                        Logger.Log($"Organization with name {workItem.OrganizationName} was not found in the config.", LogLevel.Warning, new NullReferenceException($"Organization with name {workItem.OrganizationName} was not found in the config."));
                    }

                    var crm = OrganizationHelper.CreateOrganizationService(org);

                    ExecuteAction(crm, workItem);
                }
            }

            Logger.Log("All solution files have been processed. Press any key to exit.", LogLevel.Info);
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

            Logger.Log($"WorkItem type {workItem.GetType()} is not supported.", LogLevel.Warning, new InvalidOperationException($"WorkItem type {workItem.GetType()} is not supported."));
        }

        private static void ExecuteImport(IOrganizationService orgService, ImportSolutionWorkItem importSolution)
        {
            using (var crm = new CrmOrganization(orgService))
            {
                using (FileStream zip = File.Open(Path.Combine(_baseDirectory, $@"Solutions\{importSolution.FileName}"), FileMode.Open))
                {
                    Logger.Log($"Starting with import of {importSolution.FileName}", LogLevel.Info);
                    var message = new ImportSolutionMessage(crm)
                    {
                        SolutionFileStream = zip,
                        HoldingSolution = importSolution.HoldingSolution,
                        OverwriteIfSameVersionExists = importSolution.OverwriteIfSameVersionExists,
                        OverwriteUnmanagedCustomizations = importSolution.OverwriteUnmanagedCustomizations,
                        PublishWorkflows = importSolution.PublishWorkflows,
                        SkipProductUpdateDependencies = importSolution.SkipProductDependencies,
                    };

                    crm.Execute(message);
                }
            }
        }

        private static void ExecuteExport(IOrganizationService orgService, ExportSolutionWorkItem exportSolution)
        {
            Logger.Log($"Exporting solution {exportSolution.UniqueName}.", LogLevel.Info);
            var writeToFile = Path.Combine(_solutionsDirectory, exportSolution.WriteToZipFile);

            using (var crm = new CrmOrganization(orgService))
            {
                var message = new ExportSolutionMessage(crm)
                {
                    UniqueName = exportSolution.UniqueName,
                    ExportAsManaged = exportSolution.ExportAsManaged,
                    OutputFile = writeToFile,
                };

                crm.Execute(message);
            }
        }

        public static void ExecuteDelete(IOrganizationService orgService, DeleteSolutionWorkItem deleteSolution)
        {
            using (var crm = new CrmOrganization(orgService))
            {
                var message = new DeleteSolutionMessage(crm)
                {
                    UniqueName = deleteSolution.UniqueName,
                };

                crm.Execute(message);
            }
        }
    }
}