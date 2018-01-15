using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using SolutionManager.App.Configuration;

namespace SolutionManager.App.Helpers
{
    public static class OrganizationHelper
    {
        public static IOrganizationService CreateOrganizationService(CrmCredentials credentials)
        {
            // Connect to the CRM web service using a connection string.
            string connectionString = $@"Url={credentials.OrganizationUri};";

            if (!string.IsNullOrEmpty(credentials.UserName) && !string.IsNullOrEmpty(credentials.Password))
                connectionString += $"Username={credentials.UserName};Password={credentials.Password};";

            connectionString += "authtype=Office365";

            CrmServiceClient conn = new CrmServiceClient(connectionString);

            // Return the OrganizationService object.
            return (IOrganizationService)conn.OrganizationWebProxyClient ?? (IOrganizationService)conn.OrganizationServiceProxy;
        }
    }
}
