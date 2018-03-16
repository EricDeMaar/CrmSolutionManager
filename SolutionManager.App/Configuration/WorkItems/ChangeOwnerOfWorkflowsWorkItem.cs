using System;
using System.Xml.Serialization;
using Microsoft.Xrm.Sdk;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Logging;
using SolutionManager.Logic.Messages;

namespace SolutionManager.App.Configuration.WorkItems
{
    public class ChangeOwnerOfWorkflowsWorkItem : WorkItem
    {
        [XmlElement]
        public string SystemUserId { get; set; }

        [XmlElement]
        public bool ActivateAllWorkflows { get; set; }

        private Guid SystemUserGuid;

        public override void Execute(IOrganizationService service)
        {
            if (!this.Validate())
            {
                throw new ArgumentNullException($"SystemUserId {this.SystemUserId} could not be parsed to a valid Guid.");
            }

            Logger.Log($"Changing the owner of workflows to {SystemUserGuid}. Activating workflows: {ActivateAllWorkflows}.", LogLevel.Info);

            using (var crm = new CrmOrganization(service))
            {
                var message = new ChangeOwnerOfAllWorkflowsMessage(crm)
                {
                    SystemUserId = this.SystemUserGuid,
                    ActivateAllWorkflows = this.ActivateAllWorkflows
                };

                crm.Execute(message);
            }
        }

        public override bool Validate()
        {
            return Guid.TryParse(this.SystemUserId, out this.SystemUserGuid);
        }
    }
}