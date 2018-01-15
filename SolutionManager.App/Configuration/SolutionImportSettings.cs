using System;
using System.Xml.Serialization;

namespace SolutionManager.App.Configuration
{
    [Serializable]
    public class SolutionImportSettings
    {
        [XmlElement]
        public bool ConvertToManaged { get; set; }

        [XmlElement]
        public bool OverwriteUnmanagedCustomizations { get; set; }

        [XmlElement]
        public bool PublishWorkflows { get; set; }

        [XmlElement]
        public bool SkipProductDependencies { get; set; }

        [XmlElement]
        public bool OverwriteIfSameVersionExists { get; set; }
    }
}