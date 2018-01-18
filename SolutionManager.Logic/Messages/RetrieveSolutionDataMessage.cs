﻿using Microsoft.Xrm.Sdk.Query;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Results;

namespace SolutionManager.Logic.Messages
{
    public class RetrieveSolutionDataMessage : Message
    {
        public string UniqueName { get; set; }

        public RetrieveSolutionDataMessage(CrmOrganization organization) : base(organization) { }

        public override Result Execute()
        {
            QueryExpression querySolution = new QueryExpression
            {
                EntityName = Solution.EntityLogicalName,
                ColumnSet = new ColumnSet(new string[] { "installedon", "version", "versionnumber", "friendlyname", "uniquename" }),
                Criteria = new FilterExpression()
            };

            querySolution.Criteria.AddCondition("uniquename", ConditionOperator.Equal, this.UniqueName);
            Solution solution = (Solution)this.CrmOrganization.RetrieveMultiple(querySolution).Entities[0];

            return new RetrieveSolutionDataResult()
            {
                Success = true,
                Solution = solution,
            };
        }
    }
}
