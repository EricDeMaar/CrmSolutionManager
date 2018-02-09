using System;
using System.Xml.Serialization;
using Microsoft.Xrm.Sdk;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Messages;

namespace SolutionManager.App.Configuration.WorkItems
{
    public class DeleteSolutionWorkItem : WorkItem
    {
        [XmlElement]
        public string UniqueName { get; set; }

        public override void Execute(IOrganizationService service)
        {
            if (!this.Validate())
            {
                throw new ArgumentNullException(nameof(this.UniqueName));
            }

            using (var crm = new CrmOrganization(service))
            {
                var message = new DeleteSolutionMessage(crm)
                {
                    UniqueName = this.UniqueName,
                };

                crm.Execute(message);
            }
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(UniqueName);
        }
    }
}
