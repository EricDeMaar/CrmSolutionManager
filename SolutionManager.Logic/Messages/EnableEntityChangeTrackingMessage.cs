using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using SolutionManager.Logic.Results;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Logging;

namespace SolutionManager.Logic.Messages
{
    public class EnableEntityChangeTrackingMessage : Message
    {
        /// <summary>
        /// Gets or sets the logical name of the Entity.
        /// </summary>
        public string EntityLogicalName { get; set; }

        /// <summary>
        /// Gets or sets whether change tracking should be enabled on the Entity.
        /// </summary>
        public bool EnableChangeTracking { get; set; }

        public EnableEntityChangeTrackingMessage(CrmOrganization crm) : base(crm) { }

        public override Result Execute()
        {
            // Retrieve the entity metadata
            var retrieveEntityRequest = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.Entity,
                LogicalName = this.EntityLogicalName,
            };

            var retrieveEntityResponse = this.CrmOrganization.Execute<RetrieveEntityResponse>(retrieveEntityRequest);
            EntityMetadata entityMetadata = retrieveEntityResponse.EntityMetadata;

            // Update 'ChangeTrackingEnabled' property
            entityMetadata.ChangeTrackingEnabled = this.EnableChangeTracking;

            // Prepare the UpdateEntityRequest
            var updateEntityRequest = new UpdateEntityRequest
            {
                Entity = entityMetadata,
            };

            // Fire & execute
            Logger.Log($"Entity Change Tracking has been set to {this.EnableChangeTracking} for {this.EntityLogicalName}", LogLevel.Info);
            this.CrmOrganization.Execute(updateEntityRequest);

            return new Result() { Success = true };
        }
    }
}
