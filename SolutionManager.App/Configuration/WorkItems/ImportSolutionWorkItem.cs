using System;
using System.Xml.Serialization;

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

        public override bool Validate()
        {
            return true;
        }
    }
}