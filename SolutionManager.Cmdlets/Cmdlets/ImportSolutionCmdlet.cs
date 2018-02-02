using System.IO;
using System.Management.Automation;
using SolutionManager.Logic.Results;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Messages;
using SolutionManager.Logic.Logging;
using SolutionManager.Cmdlets.Base;
using SolutionManager.Cmdlets.Logging;

namespace SolutionManager.Cmdlets.Cmdlets
{
    [Cmdlet(VerbsData.Import, "CrmSolution")]
    [OutputType(typeof(ImportSolutionResult))]
    public class ImportSolutionCmdlet : CrmCmdlet
    {
        [Parameter(Mandatory = true)]
        public string SolutionFile { get; set; }

        [Parameter]
        public bool HoldingSolution { get; set; }

        [Parameter]
        public bool ConvertToManaged { get; set; }

        [Parameter]
        public bool OverwriteUnmanagedCustomizations { get; set; }

        [Parameter]
        public bool PublishWorkflows { get; set; }

        [Parameter]
        public bool SkipProductUpdateDependencies { get; set; }

        [Parameter]
        public bool OverwriteIfSameVersionExists { get; set; }

        protected override void ProcessRecord()
        {
            Logger.Log(ObjectLogger.GetLogFor(this), LogLevel.Info);

            var crm = new CrmOrganization(this.OrganizationService);

            using (FileStream solution = File.Open(this.SolutionFile, FileMode.Open))
            {
                var message = new ImportSolutionMessage(crm)
                {
                    SolutionFileStream = solution,
                    OverwriteIfSameVersionExists = this.OverwriteIfSameVersionExists,
                    HoldingSolution = this.HoldingSolution,
                    PublishWorkflows = this.PublishWorkflows,
                    ConvertToManaged = this.ConvertToManaged,
                    OverwriteUnmanagedCustomizations = this.OverwriteUnmanagedCustomizations,
                    SkipProductUpdateDependencies = this.SkipProductUpdateDependencies,
                };

                var result = (ImportSolutionResult)crm.Execute(message);
                WriteObject(result);
            }
        }
    }
}
