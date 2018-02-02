using System;
using System.Xml.Serialization;

namespace SolutionManager.App.Configuration.WorkItems
{
    public class EnableEntityChangeTrackingWorkItem : WorkItem
    {
        [XmlElement]
        public string EntityLogicalNames { get; set; }

        [XmlElement]
        public bool EnableChangeTracking { get; set; }

        public override bool Validate()
        {
            throw new NotImplementedException();
        }
    }
}