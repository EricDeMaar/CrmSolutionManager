using System;
using System.Xml.Serialization;

namespace SolutionManager.App.Configuration
{
    [Serializable]
    [XmlRoot]
    public class ImportConfiguration
    {
        [XmlAttribute("showImportProgress")]
        public bool ShowImportProgress { get; set; }

        [XmlArray]
        [XmlArrayItem("SolutionFile")]
        public SolutionFile[] SolutionFiles { get; set; }

        public bool Validate()
        {
            if (SolutionFiles == null)
                throw new Exception("No solution files found in config.");

            return true;
        }
    }
}