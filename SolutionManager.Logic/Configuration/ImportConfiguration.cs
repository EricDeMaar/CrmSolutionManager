using System;
using System.Xml.Serialization;

namespace SolutionManager.Logic.Configuration
{
    [Serializable]
    [XmlRoot]
    public class ImportConfiguration
    {
        [XmlAttribute("timeOutInMinutes")]
        public int TimeOutInMinutes { get; set; }

        [XmlAttribute("showImportProgress")]
        public bool ShowImportProgress { get; set; }

        [XmlArray]
        [XmlArrayItem("SolutionFile")]
        public SolutionFile[] SolutionFiles { get; set; }

        public bool Validate()
        {
            if (SolutionFiles == null)
                throw new Exception("No solution files found in config.");

            if (TimeOutInMinutes == 0)
                TimeOutInMinutes = 6;

            return true;
        }
    }
}