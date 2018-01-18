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
        private IOrganizationService OrganizationService { get; }

        public CrmOrganization(IOrganizationService organizationService)
        {
            OrganizationService = organizationService;
        }

        public Result ExecuteMessage(Message message)
        {
            return message.Execute();
        }     

        #region Helpers for Execute & Retrieve & Update & Delete
        [DebuggerStepThrough]
        public void Execute(OrganizationRequest request)
        {
            OrganizationService.Execute(request);
        }

        [DebuggerStepThrough]
        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            return OrganizationService.Retrieve(entityName, id, columnSet);
        }

        [DebuggerStepThrough]
        public EntityCollection RetrieveMultiple(QueryExpression query)
        {
            return OrganizationService.RetrieveMultiple(query);
        }

        [DebuggerStepThrough]
        public TResponse Execute<TResponse>(OrganizationRequest request) where TResponse : OrganizationResponse
        {
            return (TResponse)OrganizationService.Execute(request);
        }

        [DebuggerStepThrough]
        public Entity RetrieveFetchXmlSingle(string fetchxml)
        {
            var retrieved = RetrieveFetchXml(fetchxml).ToList();

            switch (retrieved.Count)
            {
                case 0:
                    return null;
                case 1:
                    return retrieved.First();
                default:
                    throw new Exception($"Expected one, retrieved multiple at: {fetchxml}");
            }
        }

        [DebuggerStepThrough]
        public IEnumerable<Entity> RetrieveFetchXml(string fetchxml)
        {
            var result = OrganizationService.RetrieveMultiple(new FetchExpression(fetchxml));
            return result.Entities;
        }

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

        [DebuggerStepThrough]
        public void Delete(string entityName, Guid id)
        {
            OrganizationService.Delete(entityName, id);
        }
        #endregion

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
