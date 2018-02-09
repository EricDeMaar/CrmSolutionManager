using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xrm.Sdk;
using SolutionManager.App.Configuration;
using SolutionManager.App.Configuration.WorkItems;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Logging;
using SolutionManager.Logic.Messages;

namespace SolutionManager.App.Helpers
{
    public class ActionExecutor : IDisposable
    {
        private IOrganizationService Service { get; set; }

        public ActionExecutor(IOrganizationService orgService)
        {
            this.Service = orgService;

            this._workItemActionMapping = new Dictionary<Type, Action<WorkItem>>()
            {
                { typeof(DeleteSolutionWorkItem), (x => ExecuteDelete((DeleteSolutionWorkItem)x)) },
                { typeof(ImportSolutionWorkItem), (x => ExecuteImport((ImportSolutionWorkItem)x)) },
                { typeof(ExportSolutionWorkItem), (x => ExecuteExport((ExportSolutionWorkItem)x)) },
                { typeof(EnableEntityChangeTrackingWorkItem), (x => ExecuteEntityChangeTracking((EnableEntityChangeTrackingWorkItem)x)) }
            };

            _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _solutionsDirectory = Path.Combine(_baseDirectory, @"Solutions\");
        }

        private Dictionary<Type, Action<WorkItem>> _workItemActionMapping;
        private readonly string _baseDirectory;
        private readonly string _solutionsDirectory;

        public void ExecuteAction(WorkItem workItem)
        {
            if (!_workItemActionMapping.Any(x => x.Key == workItem.GetType()))
            {
                Logger.Log($"WorkItem type {workItem.GetType()} is not supported.",
                    LogLevel.Warning,
                    new InvalidOperationException($"WorkItem type {workItem.GetType()} is not supported."));
                return;
            }

            _workItemActionMapping[workItem.GetType()].Invoke(workItem);
        }

        private void ExecuteImport(ImportSolutionWorkItem importSolution)
        {
            using (var crm = new CrmOrganization(this.Service))
            {
                using (FileStream zip = File.Open(Path.Combine(_baseDirectory, $@"Solutions\{importSolution.FileName}"), FileMode.Open))
                {
                    Logger.Log($"Starting with import of {importSolution.FileName}", LogLevel.Info);
                    var message = new ImportSolutionMessage(crm)
                    {
                        SolutionFileStream = zip,
                        HoldingSolution = importSolution.HoldingSolution,
                        OverwriteIfSameVersionExists = importSolution.OverwriteIfSameVersionExists,
                        OverwriteUnmanagedCustomizations = importSolution.OverwriteUnmanagedCustomizations,
                        PublishWorkflows = importSolution.PublishWorkflows,
                        SkipProductUpdateDependencies = importSolution.SkipProductDependencies,
                    };

                    crm.Execute(message);
                }
            }
        }

        private void ExecuteExport(ExportSolutionWorkItem exportSolution)
        {
            Logger.Log($"Exporting solution {exportSolution.UniqueName}.", LogLevel.Info);
            var writeToFile = Path.Combine(_solutionsDirectory, exportSolution.WriteToZipFile);

            using (var crm = new CrmOrganization(this.Service))
            {
                var message = new ExportSolutionMessage(crm)
                {
                    UniqueName = exportSolution.UniqueName,
                    ExportAsManaged = exportSolution.ExportAsManaged,
                    OutputFile = writeToFile,
                };

                crm.Execute(message);
            }
        }

        private void ExecuteDelete(DeleteSolutionWorkItem deleteSolution)
        {
            using (var crm = new CrmOrganization(this.Service))
            {
                var message = new DeleteSolutionMessage(crm)
                {
                    UniqueName = deleteSolution.UniqueName,
                };

                crm.Execute(message);
            }
        }

        private void ExecuteEntityChangeTracking(EnableEntityChangeTrackingWorkItem workItem)
        {
            using (var crm = new CrmOrganization(this.Service))
            {
                List<string> entities = workItem.EntityLogicalNames.Split(',').ToList();

                foreach (var entity in entities)
                {
                    var message = new EnableEntityChangeTrackingMessage(crm)
                    {
                        EntityLogicalName = entity,
                        EnableChangeTracking = workItem.EnableChangeTracking,
                    };

                    crm.Execute(message);
                }
            }
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // For example: OrganizationServiceProxy is IDisposable.
                    if (this.Service is IDisposable)
                    {
                        ((IDisposable)this.Service).Dispose();
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
    }
}
