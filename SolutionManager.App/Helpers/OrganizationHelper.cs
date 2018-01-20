using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using SolutionManager.App.Configuration;

namespace SolutionManager.App.Helpers
{
    public static class OrganizationHelper
    {
        /// <summary>
        /// A static helper method to create an OrganizationService object using
        /// the given <seealso cref="Organization">credentials</seealso> object.
        /// </summary>
        /// <param name="credentials">The credentials which are used to build the connection string.</param>
        /// <returns>An <seealso cref="Organization"/> object.</returns>
        public static IOrganizationService CreateOrganizationService(Organization credentials)
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
