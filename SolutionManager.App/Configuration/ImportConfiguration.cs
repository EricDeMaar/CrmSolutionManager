using System;
using System.Xml.Serialization;

namespace SolutionManager.App.Configuration
{
    [Serializable]
    [XmlRoot]
    public class ImportConfiguration
    {
        [XmlElement]
        public string Name { get; set; }

        [XmlElement]
        public string Description { get; set; }

        [XmlArray]
        [XmlArrayItem("Organization")]
        public Organization[] Organizations { get; set; }

        [XmlArray]
        [XmlArrayItem("WorkItem")]
        public WorkItem[] WorkItems { get; set; }

        public bool Validate()
        {
            if (WorkItems == null)
                throw new Exception("No work items were found in config.");

            return true;
        }
    }
}