using System;
using System.Xml.Serialization;

namespace SolutionManager.App.Configuration
{
    [Serializable]
    [XmlRoot]
    public class ImportConfiguration
    {
        [XmlArray]
        [XmlArrayItem("Organization")]
        public Organization[] Organizations { get; set; }

        [XmlArray]
        [XmlArrayItem("WorkDefinition")]
        public WorkDefinition[] WorkDefinitions { get; set; }
    }
}