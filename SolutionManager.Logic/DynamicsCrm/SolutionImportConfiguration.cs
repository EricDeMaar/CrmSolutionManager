using System;

namespace SolutionManager.Logic.DynamicsCrm
{
    public class SolutionImportConfiguration
    {
        public string FileName { get; set; }
        public bool ConvertToManaged { get; set; }
        public bool OverwriteUnmanagedCustomizations { get; set; }
        public bool PublishWorkflows { get; set; }
        public bool SkipProductDependencies { get; set; }
        public bool OverwriteIfSameVersionExists { get; set; }
    }
}