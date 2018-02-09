using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using SolutionManager.App.Helpers;
using SolutionManager.App.Configuration;
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
                    bool validated = workItem.Validate();

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
                        Logger.Log($"Organization with name {workItem.OrganizationName} was not found in the config.", 
                            LogLevel.Warning, 
                            new NullReferenceException($"Organization with name {workItem.OrganizationName} was not found in the config."));

                        return;
                    }

                    var crm = OrganizationHelper.CreateOrganizationService(org);

                    using (var actionExecutor = new ActionExecutor(crm))
                    {
                        actionExecutor.ExecuteAction(workItem);
                    }
                }
            }

            Logger.Log("All solution files have been processed. Press any key to exit.", LogLevel.Info);
            Console.ReadKey();
        }
    }
}