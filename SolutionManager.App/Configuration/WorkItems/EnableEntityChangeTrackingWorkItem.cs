using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xrm.Sdk;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Messages;

namespace SolutionManager.App.Configuration.WorkItems
{
    public class EnableEntityChangeTrackingWorkItem : WorkItem
    {
        [XmlElement]
        public string EntityLogicalNames { get; set; }

        [XmlElement]
        public bool EnableChangeTracking { get; set; }

        public override void Execute(IOrganizationService service)
        {
            using (var crm = new CrmOrganization(service))
            {
                List<string> entities = this.EntityLogicalNames.Split(',').ToList();

                foreach (var entity in entities)
                {
                    var message = new EnableEntityChangeTrackingMessage(crm)
                    {
                        EntityLogicalName = entity,
                        EnableChangeTracking = this.EnableChangeTracking,
                    };

                    crm.Execute(message);
                }
            }
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(EntityLogicalNames);
        }
    }
}