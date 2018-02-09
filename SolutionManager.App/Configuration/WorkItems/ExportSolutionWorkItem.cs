using System;
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

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(UniqueName) && !string.IsNullOrEmpty(WriteToZipFile);
        }
    }
}
