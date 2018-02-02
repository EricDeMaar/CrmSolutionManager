using System;
using System.Management.Automation;
using System.Net;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;

namespace SolutionManager.Cmdlets.Base
{
    public abstract class CrmCmdlet : Cmdlet
    {
        protected IOrganizationService OrganizationService;
        protected CrmServiceClient ServiceClient;

        [Parameter(Mandatory = true)]
        public string ConnectionString { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            SetSecurityProtocol();

            CreateCrmConnection();
        }

        private void SetSecurityProtocol()
        {
            WriteVerbose($"Current Security Protocol: {ServicePointManager.SecurityProtocol}");

            if (!ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls11))
            {
                ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol ^ SecurityProtocolType.Tls11;
            }
            if (!ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls12))
            {
                ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol ^ SecurityProtocolType.Tls12;
            }

            WriteVerbose($"Modified Security Protocol: {ServicePointManager.SecurityProtocol}");
        }

        private void CreateCrmConnection()
        {
            ServiceClient = new CrmServiceClient(this.ConnectionString);

            if (ServiceClient != null && ServiceClient.IsReady)
            {
                OrganizationService = ServiceClient;
                return;
            }
            else
            {
                base.WriteWarning(ServiceClient.LastCrmError);
                if (ServiceClient.LastCrmException != null)
                {
                    base.WriteWarning(ServiceClient.LastCrmException.Message);
                }
            }

            throw new Exception($"Could not connect to the Dynamics CRM organization. Exception: {ServiceClient?.LastCrmError}");
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();

            if (ServiceClient != null)
                ServiceClient.Dispose();
        }
    }
}
