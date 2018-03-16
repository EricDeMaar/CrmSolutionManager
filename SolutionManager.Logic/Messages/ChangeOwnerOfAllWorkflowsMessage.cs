using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using SolutionManager.Logic.Results;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Sdk;
using SolutionManager.Logic.Logging;

namespace SolutionManager.Logic.Messages
{
    public class ChangeOwnerOfAllWorkflowsMessage : Message
    {
        public bool ActivateAllWorkflows { get; set; }
        public Guid SystemUserId { get; set; }

        public ChangeOwnerOfAllWorkflowsMessage(CrmOrganization organization) : base(organization) { }

        /// <summary>
        /// Changes the owner of all the Workflows in the target environment.
        /// This message can optionally activate all workflows.
        /// </summary>
        /// <returns>A result containing a Success boolean.</returns>
        public override Result Execute()
        {
            var systemUser = this.CrmOrganization.Retrieve("systemuser", this.SystemUserId, new ColumnSet("firstname", "lastname"));
            Logger.Log($"Retrieved SystemUser with id {this.SystemUserId}: {systemUser.GetAttributeValue<string>("firstname")} {systemUser.GetAttributeValue<string>("lastname")}.", LogLevel.Info);

            var executeMultiple = new ExecuteMultipleRequest()
            {
                Requests = new OrganizationRequestCollection(),
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = false,
                }
            };

            QueryExpression queryWorkflows = new QueryExpression
            {
                EntityName = "workflow",
                ColumnSet = new ColumnSet(new string[] { "workflowid", "name", "ownerid", "statuscode" }),
                Criteria = new FilterExpression()
                {
                    Conditions =
                    {
                        {
                            new ConditionExpression("category", ConditionOperator.Equal, 0)
                        }
                    }
                }
            };

            var workflows = this.CrmOrganization.RetrieveMultiple(queryWorkflows).Entities.Cast<Workflow>().ToList();
            Logger.Log($"Retrieved {workflows.Count()} workflows from the target environment.", LogLevel.Info);

            int updatedWorkflows = 0;

            foreach (var workflow in workflows)
            {
                if (workflow.OwnerId?.Id != this.SystemUserId)
                {
                    var assignRequest = new AssignRequest()
                    {
                        Assignee = new EntityReference("systemuser", SystemUserId),
                        Target = new EntityReference("workflow", workflow.Id)
                    };

                    executeMultiple.Requests.Add(assignRequest);
                    
                    if (workflow.StatusCode?.Value != 2 && this.ActivateAllWorkflows)
                    {
                        var setStateRequest = new SetStateRequest()
                        {
                            EntityMoniker = new EntityReference("workflow", workflow.Id),
                            State = new OptionSetValue(1),
                            Status = new OptionSetValue(2)
                        };

                        executeMultiple.Requests.Add(setStateRequest);
                    }

                    updatedWorkflows++;
                }
            }

            this.CrmOrganization.Execute(executeMultiple);
            Logger.Log($"Updated {updatedWorkflows} workflows in the target environment.", LogLevel.Info);

            return new Result()
            {
                Success = true,
            };
        }
    }
}
