using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SolutionManager.Logic.Sdk
{
    /// <summary>
    /// Proxy class for a Dynamics CRM solution entity record.
    /// </summary>
    [DataContractAttribute()]
    [EntityLogicalName("workflow")]
    public class Workflow : Entity
    {
        public Workflow() : base(EntityLogicalName) { }

        public static readonly string EntityLogicalName = "workflow";

        [AttributeLogicalNameAttribute("workflowidid")]
        public Guid WorkflowId
        {
            get
            {
                return this.GetAttributeValue<Guid>("workflowid");
            }
        }

        [AttributeLogicalNameAttribute("name")]
        public string Name
        {
            get
            {
                return this.GetAttributeValue<string>("name");
            }
            set
            {
                this.SetAttributeValue("name", value);
            }
        }

        [AttributeLogicalNameAttribute("ownerid")]
        public EntityReference OwnerId
        {
            get
            {
                return this.GetAttributeValue<EntityReference>("ownerid");
            }
        }

        [AttributeLogicalNameAttribute("statuscode")]
        public OptionSetValue StatusCode
        {
            get
            {
                return this.GetAttributeValue<OptionSetValue>("statuscode");
            }
        }
    }
}

