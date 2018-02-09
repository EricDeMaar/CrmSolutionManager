using System;
using System.Xml.Serialization;
using Microsoft.Xrm.Sdk;
using SolutionManager.Logic.DynamicsCrm;
using System.IO;
using SolutionManager.Logic.Logging;
using SolutionManager.Logic.Messages;

namespace SolutionManager.App.Configuration.WorkItems
{
    [Serializable]
    public class ImportSolutionWorkItem : WorkItem
    {
        [XmlElement]
        public string FileName { get; set; }

        [XmlElement]
        public bool ShowImportProgress { get; set; }

        [XmlElement]
        public bool HoldingSolution { get; set; }

        [XmlElement]
        public bool OverwriteUnmanagedCustomizations { get; set; }

        [XmlElement]
        public bool PublishWorkflows { get; set; }

        [XmlElement]
        public bool SkipProductDependencies { get; set; }

        [XmlElement]
        public bool OverwriteIfSameVersionExists { get; set; }

        private readonly string _solutionsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Solutions\");

        public override void Execute(IOrganizationService service)
        {
            if (!this.Validate())
            {
                throw new ArgumentNullException(nameof(this.FileName));
            }

            using (var crm = new CrmOrganization(service))
            {
                using (FileStream zip = File.Open(Path.Combine(_solutionsDirectory, this.FileName), FileMode.Open))
                {
                    Logger.Log($"Starting with import of {this.FileName}", LogLevel.Info);
                    var message = new ImportSolutionMessage(crm)
                    {
                        SolutionFileStream = zip,
                        HoldingSolution = this.HoldingSolution,
                        OverwriteIfSameVersionExists = this.OverwriteIfSameVersionExists,
                        OverwriteUnmanagedCustomizations = this.OverwriteUnmanagedCustomizations,
                        PublishWorkflows = this.PublishWorkflows,
                        SkipProductUpdateDependencies = this.SkipProductDependencies,
                    };

                    crm.Execute(message);
                }
            }
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(FileName);
        }
    }
}