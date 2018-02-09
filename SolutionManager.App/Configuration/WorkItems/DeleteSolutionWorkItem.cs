using System;
using System.Xml.Serialization;

namespace SolutionManager.App.Configuration.WorkItems
{
    public class DeleteSolutionWorkItem : WorkItem
    {
        [XmlElement]
        public string UniqueName { get; set; }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(UniqueName);
        }
    }
}
