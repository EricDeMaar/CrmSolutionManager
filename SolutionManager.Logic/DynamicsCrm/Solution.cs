using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Runtime.Serialization;

namespace SolutionManager.Logic.DynamicsCrm
{
    [DataContractAttribute()]
    [EntityLogicalName("solution")]
    public partial class Solution : Entity
    {
        public Solution() : base(EntityLogicalName) { }

        public static readonly string EntityLogicalName = "solution";

        [AttributeLogicalNameAttribute("solutionid")]
        public Guid SolutionId
        {
            get
            {
                return this.GetAttributeValue<Guid>("solutionid");
            }
            set
            {
                this.SetAttributeValue("solutionid", value);
            }
        }

        [AttributeLogicalNameAttribute("uniquename")]
        public string UniqueName
        {
            get
            {
                return this.GetAttributeValue<string>("uniquename");
            }
            set
            {
                this.SetAttributeValue("uniquename", value);
            }
        }

        [AttributeLogicalNameAttribute("version")]
        public string Version
        {
            get
            {
                return this.GetAttributeValue<string>("version");
            }
            set
            {
                this.SetAttributeValue("version", value);
            }
        }

        public Version GetVersion()
        {
            Version version;

            if (System.Version.TryParse(this.Version, out version))
            {
                return version;
            }

            return null as Version;
        }
    }
}
