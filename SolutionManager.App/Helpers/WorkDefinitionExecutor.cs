using System;
using System.Linq;
using SolutionManager.App.Configuration;
using SolutionManager.App.Configuration.WorkItems;
using SolutionManager.Logic.Logging;

namespace SolutionManager.App.Helpers
{
    public class WorkDefinitionExecutor
    {
        private string WorkDefinitionToExecute { get; set; }
        private ImportConfiguration ImportConfiguration { get; set; }

        public WorkDefinitionExecutor(string name, ImportConfiguration config)
        {
            this.WorkDefinitionToExecute = name;
            this.ImportConfiguration = config;
        }

        public void Execute()
        {
            var workDefinition = this.ImportConfiguration.WorkDefinitions?.FirstOrDefault(x => x.Name == this.WorkDefinitionToExecute);

            if (workDefinition == null)
            {
                Logger.Log($"Could not find a WorkDefinition named {this.WorkDefinitionToExecute}.", LogLevel.Fatal);
                return;
            }

            if (!workDefinition.WorkItems.Any())
            {
                Logger.Log($"No WorkItems are defined in WorkDefinition: {this.WorkDefinitionToExecute}.", LogLevel.Fatal);
                return;
            }

            foreach (WorkItem workItem in workDefinition.WorkItems)
            {
                Organization org = this.ImportConfiguration.Organizations.FirstOrDefault(x => x.OrganizationName == workItem.OrganizationName);

                if (org == null)
                {
                    Logger.Log($"Organization with name {workItem.OrganizationName} was not found in the config.",
                        LogLevel.Warning,
                        new NullReferenceException($"Organization with name {workItem.OrganizationName} was not found in the config."));

                    return;
                }

                var crm = OrganizationHelper.CreateOrganizationService(org);

                workItem.Execute(crm);
            }

            Logger.Log("All WorkItems have been processed. Press any key to exit.", LogLevel.Info);
        }
    }
}
