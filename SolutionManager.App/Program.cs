using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using SolutionManager.App.Helpers;
using SolutionManager.App.Configuration;
using SolutionManager.Logic.Logging;

namespace SolutionManager.App
{
    class Program
    {
        private static readonly string _solutionsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Solutions\");
        private static readonly string _importConfig = Path.Combine(_solutionsDirectory, "ImportConfig.xml");

        static int Main(string[] args)
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
                    return -1;
                }

                var cli = new ArgumentRouter(args, config);
            }

            Console.ReadKey();
            return 0;
        }
    }
}