using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Xrm.Sdk;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.App.Helpers;
using SolutionManager.App.Configuration;
using SolutionManager.App.Extensions;

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
                    Console.WriteLine($"{CurrentTime()} - Error reading configuration '{_importConfig}'. Exception: {exception.Message}");
                }

                if (config == null)
                {
                    return;
                }

                config.Validate();

                foreach (SolutionFile solution in config.SolutionFiles)
                {
                    bool validated = solution.Validate();

                    if (!validated && !solution.ContinueOnError)
                    {
                        PrintAbortMessage();
                        return;
                    }

                    if (!validated && solution.ContinueOnError)
                        continue;

                    var crm = OrganizationHelper.CreateOrganizationService(solution.CrmCredentials);

                    ExecuteAction(crm, solution);
                }
            }

            Console.WriteLine($"{CurrentTime()} - All solution files have been processed. Press any key to exit.");
            Console.ReadKey();
        }

        private static void ExecuteAction(IOrganizationService crm, SolutionFile solution)
        {
            switch (solution.Action)
            {
                case ActionType.Import:
                    ExecuteImport(crm, solution);
                    break;
                case ActionType.Export:
                    ExecuteExport(crm, solution);
                    break;
                case ActionType.Delete:
                    ExecuteDelete(crm, solution);
                    break;
                default:
                    throw new InvalidOperationException($"Action {solution.Action} not understood.");
            }
        }

        private static bool ExecuteImport(IOrganizationService orgService, SolutionFile solution)
        {
            SolutionData solutionData = SolutionHelper.ReadVersionFromZip(solution.FileName);

            using (var crm = new CrmOrganization(orgService))
            {
                bool startImport = crm.CompareSolutionVersion(solutionData.Version, solutionData.UniqueName, solution.ImportSettings.OverwriteIfSameVersionExists);

                if (startImport)
                {
                    using (FileStream zip = File.Open(Path.Combine(_baseDirectory, $@"Solutions\{solution.FileName}"), FileMode.Open))
                    {
                        Console.WriteLine($"{CurrentTime()} - Starting with import of {solution.FileName}");

                        var result = crm.ImportSolution(zip, solution.ToSolutionImportConfiguration(), true);

                        if (result.Status == ImportResultStatus.Failure)
                            return false;
                    }
                }
            }
            return true;
        }

        private static bool ExecuteExport(IOrganizationService orgService, SolutionFile solution)
        {
            Console.WriteLine($"{CurrentTime()} - Exporting solution {solution.UniqueName}.");
            var writeToFile = Path.Combine(_solutionsDirectory, solution.WriteToZipFile);
            bool result;

            using (var crm = new CrmOrganization(orgService))
            {
                result = crm.ExportSolution(solution.UniqueName, writeToFile, solution.ExportAsManaged);
            }

            if (!result && !solution.ContinueOnError)
                return false;

            if (!result && solution.ContinueOnError)
                return true;

            Console.WriteLine($"{CurrentTime()} - Solution {solution.UniqueName} was successfully exported to {solution.WriteToZipFile}");

            return true;
        }

        public static bool ExecuteDelete(IOrganizationService orgService, SolutionFile solution)
        {
            if (solution.UniqueName == null && solution.ContinueOnError)
                return true;

            if (solution.UniqueName == null && !solution.ContinueOnError)
            {
                PrintAbortMessage();
                return false;
            }

            using (var crm = new CrmOrganization(orgService))
            {
                crm.DeleteSolution(solution.UniqueName);
                return true;
            }
        }

        private static string CurrentTime() => $"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}]";
    }
}