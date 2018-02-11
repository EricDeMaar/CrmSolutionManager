using System;
using Microsoft.Xrm.Sdk.Query;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Results;
using SolutionManager.Logic.Sdk;

namespace SolutionManager.Logic.Messages
{
    public class RetrieveSolutionDataMessage : Message
    {
        /// <summary>
        /// Gets or sets the unique name of the Dynamics CRM solution.
        /// </summary>
        public string UniqueName { get; set; }

        public RetrieveSolutionDataMessage(CrmOrganization organization) : base(organization) { }

        /// <summary>
        /// Retrieves information about a Dynamics CRM solution in a target environment.
        /// </summary>
        /// <returns>A <seealso cref="RetrieveSolutionDataResult"/> object.</returns>
        public override Result Execute()
        {
            if (string.IsNullOrEmpty(this.UniqueName))
            {
                throw new ArgumentNullException(nameof(this.UniqueName));
            }

            QueryExpression querySolution = new QueryExpression
            {
                EntityName = Solution.EntityLogicalName,
                ColumnSet = new ColumnSet(new string[] { "installedon", "version", "versionnumber", "friendlyname", "uniquename", "description", "ismanaged" }),
                Criteria = new FilterExpression()
                {
                    Conditions =
                    {
                        {
                            new ConditionExpression("uniquename", ConditionOperator.Equal, this.UniqueName)
                        }
                    }
                }
            };

            Solution solution = (Solution)this.CrmOrganization.RetrieveMultiple(querySolution).Entities[0];

            return new RetrieveSolutionDataResult()
            {
                Success = true,
                Solution = solution,
            };
        }
    }
}
