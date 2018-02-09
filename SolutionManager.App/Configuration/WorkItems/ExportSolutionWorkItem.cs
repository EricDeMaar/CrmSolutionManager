using Microsoft.Xrm.Sdk;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Logging;
using SolutionManager.Logic.Messages;
using System;
using System.IO;
using System.Xml.Serialization;

namespace SolutionManager.App.Configuration.WorkItems
{
    public class ExportSolutionWorkItem : WorkItem
    {
        [XmlElement]
        public string UniqueName { get; set; }

        [XmlElement]
        public bool ExportAsManaged { get; set; }

        [XmlElement]
        public string WriteToZipFile { get; set; }

        private readonly string _solutionsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Solutions\");

        public override void Execute(IOrganizationService service)
        {
            if (!this.Validate())
            {
                throw new ArgumentNullException($"Either {nameof(this.UniqueName)} or {nameof(this.WriteToZipFile)} where not filled in.");
            }

            Logger.Log($"Exporting solution {this.UniqueName}.", LogLevel.Info);
            var writeToFile = Path.Combine(_solutionsDirectory, this.WriteToZipFile);

            using (var crm = new CrmOrganization(service))
            {
                var message = new ExportSolutionMessage(crm)
                {
                    UniqueName = this.UniqueName,
                    ExportAsManaged = this.ExportAsManaged,
                    OutputFile = writeToFile,
                };

                crm.Execute(message);
            }
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(UniqueName) && !string.IsNullOrEmpty(WriteToZipFile);
        }
    }
}
