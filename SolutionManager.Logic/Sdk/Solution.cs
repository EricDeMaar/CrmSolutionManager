using System;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace SolutionManager.Logic.Sdk
{
    /// <summary>
    /// Proxy class for a Dynamics CRM solution entity record.
    /// </summary>
    [DataContractAttribute()]
    [EntityLogicalName("solution")]
    public class Solution : Entity
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

        [AttributeLogicalNameAttribute("ismanaged")]
        public bool IsManaged
        {
            get
            {
                return this.GetAttributeValue<bool>("ismanaged");
            }
            set
            {
                this.SetAttributeValue("ismanaged", value);
            }
        }

        [AttributeLogicalNameAttribute("description")]
        public string Description
        {
            get
            {
                return this.GetAttributeValue<string>("description");
            }
            set
            {
                this.SetAttributeValue("description", value);
            }
        }

        public Version GetVersion()
        {
            if (System.Version.TryParse(this.Version, out Version version))
            {
                return version;
            }

            return null;
        }
    }
}
