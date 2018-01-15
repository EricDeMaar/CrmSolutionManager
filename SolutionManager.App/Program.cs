using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using SolutionManager.Logic.Configuration;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.App.Helpers;
using System.Globalization;

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
                    
                    using (var crm = CreateCrmOrganization(solution.CrmCredentials, config.TimeOutInMinutes))
                    {
                        ExecuteAction(crm, solution);
                    }
                }
            }

            Console.WriteLine($"{CurrentTime()} - All solution files have been processed. Press any key to exit.");
            Console.ReadKey();
        }

        private static void ExecuteAction(CrmOrganization crm, SolutionFile solution)
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

        private static bool ExecuteImport(CrmOrganization crm, SolutionFile solution)
        {
            SolutionData solutionData = SolutionHelper.ReadVersionFromZip(solution.FileName);

            bool startImport = crm.CompareSolutionVersion(solutionData.Version, solutionData.UniqueName, solution.ImportSettings.OverwriteIfSameVersionExists);

            if (startImport)
            {
                using (FileStream zip = File.Open(Path.Combine(_baseDirectory, $@"Solutions\{solution.FileName}"), FileMode.Open))
                {
                    Console.WriteLine($"{CurrentTime()} - Starting with import of {solution.FileName}");

                    var result = crm.ImportSolution(zip, solution, true);

                    if (result.Status == ImportResultStatus.Failure)
                        return false;
                }
            }

            return true;
        }

        private static bool ExecuteExport(CrmOrganization crm, SolutionFile solution)
        {
            Console.WriteLine($"{CurrentTime()} - Exporting solution {solution.UniqueName}.");
            var writeToFile = Path.Combine(_solutionsDirectory, solution.WriteToZipFile);
            bool result = crm.ExportSolution(solution.UniqueName, writeToFile, solution.ExportAsManaged);

            if (!result && !solution.ContinueOnError)
                return false;

            if (!result && solution.ContinueOnError)
                return true;

            Console.WriteLine($"{CurrentTime()} - Solution {solution.UniqueName} was successfully exported to {solution.WriteToZipFile}");

            return true;
        }

        public static bool ExecuteDelete(CrmOrganization crm, SolutionFile solution)
        {
            if (solution.UniqueName == null && solution.ContinueOnError)
                return true;

            if (solution.UniqueName == null && !solution.ContinueOnError)
            {
                PrintAbortMessage();
                return false;
            }

            crm.DeleteSolution(solution.UniqueName);

            return true;
        }

        private static CrmOrganization CreateCrmOrganization(CrmCredentials credentials, int timeOutInMinutes)
        {
            if (credentials.UserName != null && credentials.Password != null)
            {
                return new CrmOrganization(credentials.OrganizationUri, timeOutInMinutes, credentials.UserName, credentials.Password, credentials.DomainName);
            }

            return new CrmOrganization(credentials.OrganizationUri, timeOutInMinutes);
        }

        private static string CurrentTime() => $"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}]";
    }
}