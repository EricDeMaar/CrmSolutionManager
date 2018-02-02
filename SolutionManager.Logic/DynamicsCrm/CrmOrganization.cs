using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SolutionManager.Logic.Messages;
using SolutionManager.Logic.Results;

namespace SolutionManager.Logic.DynamicsCrm
{
    public class CrmOrganization : IDisposable
    {
        protected IOrganizationService OrganizationService { get; }

        public CrmOrganization(IOrganizationService organizationService)
        {
            OrganizationService = organizationService;
        }

        /// <summary>
        /// Executes a Message containing business logic.
        /// </summary>
        /// <param name="message">The Message to execute.</param>
        /// <returns>A Result object containing the execution results.</returns>
        [DebuggerStepThrough]
        public Result Execute(Message message)
        {
            return message.Execute();
        }

        /// <summary>
        /// Executes an OrganizationRequest to the Dynamics CRM OrganizationService.
        /// </summary>
        /// <param name="request">The OrganizationRequest to execute.</param>
        [DebuggerStepThrough]
        public void Execute(OrganizationRequest request)
        {
            OrganizationService.Execute(request);
        }

        /// <summary>
        /// Executes a Retrieve request to the Dynamics CRM OrganizationService.
        /// </summary>
        /// <param name="entityName">The logical name of the entity.</param>
        /// <param name="id">The unique identifier of the record.</param>
        /// <param name="columnSet">An array of column fields to retrieve.</param>
        /// <returns>An entity record.</returns>
        [DebuggerStepThrough]
        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            return OrganizationService.Retrieve(entityName, id, columnSet);
        }

        /// <summary>
        /// Executes a RetrieveMultiple request to the Dynamics CRM OrganizationService.
        /// </summary>
        /// <param name="query">The QueryExpression to execute.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public EntityCollection RetrieveMultiple(QueryExpression query)
        {
            return OrganizationService.RetrieveMultiple(query);
        }

        /// <summary>
        /// Executes an OrganizationRequest to the Dynamics CRM OrganizationService.
        /// </summary>
        /// <typeparam name="TResponse">The expected OrganizationResponse.</typeparam>
        /// <param name="request">The OrganizationRequest to execute.</param>
        /// <returns>The OrganizationResponse returned by the OrganizationService.</returns>
        [DebuggerStepThrough]
        public TResponse Execute<TResponse>(OrganizationRequest request) where TResponse : OrganizationResponse
        {
            return (TResponse)OrganizationService.Execute(request);
        }

        /// <summary>
        /// Executes a FetchXml to the OrganizationService.
        /// </summary>
        /// <param name="fetchXml">The FetchXml query to be executed.</param>
        /// <returns>A collection of entities.</returns>
        [DebuggerStepThrough]
        public IEnumerable<Entity> RetrieveFetchXml(string fetchXml)
        {
            var result = OrganizationService.RetrieveMultiple(new FetchExpression(fetchXml));
            return result.Entities;
        }

        /// <summary>
        /// Retrieves an Entity record based on a field value.
        /// </summary>
        /// <param name="entityLogicalName">The logical name of the entity.</param>
        /// <param name="fieldName">The logical name of the field.</param>
        /// <param name="value">The value of the field.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public Entity GetEntityByField(string entityLogicalName, string fieldName, object value)
        {
            var entity = this.RetrieveFetchXmlSingle(
                             $@"<fetch>
                              <entity name=""{entityLogicalName}"">
                                <filter>
                                  <condition attribute=""{fieldName}"" operator=""eq"" value=""{value}"" />
                                </filter>
                              </entity>
                            </fetch>");

            return entity;
        }

        /// <summary>
        /// Executes a Delete request to the Dynamics CRM OrganizationService.
        /// </summary>
        /// <param name="entityName">The logical name of the entity.</param>
        /// <param name="id">The unique identifier of the entity record.</param>
        [DebuggerStepThrough]
        public void Delete(string entityName, Guid id)
        {
            OrganizationService.Delete(entityName, id);
        }

        /// <summary>
        /// A private method for fetching a single Entity record with a FetchXml query.
        /// </summary>
        /// <param name="fetchXml">The FetchXml query to execute.</param>
        /// <returns>A single entity record.</returns>
        [DebuggerStepThrough]
        private Entity RetrieveFetchXmlSingle(string fetchXml)
        {
            var retrieved = RetrieveFetchXml(fetchXml).ToList();

            switch (retrieved.Count)
            {
                case 0:
                    return null;
                case 1:
                    return retrieved.First();
                default:
                    throw new Exception($"Expected one, retrieved multiple at: {fetchXml}");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // For example: OrganizationServiceProxy is IDisposable.
                    if (OrganizationService is IDisposable)
                    {
                        ((IDisposable)OrganizationService).Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
