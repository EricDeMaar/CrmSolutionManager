using SolutionManager.App.Configuration;
using SolutionManager.Logic.DynamicsCrm;

namespace SolutionManager.App.Extensions
{
    public static class SolutionFileExtensions
    {
        public static SolutionImportConfiguration ToSolutionImportConfiguration(this SolutionFile solutionFile)
        {
            return new SolutionImportConfiguration
            {
                ConvertToManaged = solutionFile.ImportSettings.ConvertToManaged,
                FileName = solutionFile.FileName,
                OverwriteIfSameVersionExists = solutionFile.ImportSettings.OverwriteIfSameVersionExists,
                OverwriteUnmanagedCustomizations = solutionFile.ImportSettings.OverwriteUnmanagedCustomizations,
                PublishWorkflows = solutionFile.ImportSettings.PublishWorkflows,
                SkipProductDependencies = solutionFile.ImportSettings.SkipProductDependencies,
            };
        }
    }
}
