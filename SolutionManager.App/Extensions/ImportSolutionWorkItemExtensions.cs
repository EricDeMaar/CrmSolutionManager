using SolutionManager.App.Configuration.WorkItems;
using SolutionManager.Logic.DynamicsCrm;

namespace SolutionManager.App.Extensions
{
    public static class ImportSolutionWorkItemExtensions
    {
        public static SolutionImportConfiguration ToSolutionImportConfiguration(this ImportSolutionWorkItem importSolutionWorkItem)
        {
            return new SolutionImportConfiguration
            {
                FileName = importSolutionWorkItem.FileName,
                OverwriteIfSameVersionExists = importSolutionWorkItem.OverwriteIfSameVersionExists,
                OverwriteUnmanagedCustomizations = importSolutionWorkItem.OverwriteUnmanagedCustomizations,
                PublishWorkflows = importSolutionWorkItem.PublishWorkflows,
                SkipProductDependencies = importSolutionWorkItem.SkipProductDependencies,
            };
        }
    }
}
