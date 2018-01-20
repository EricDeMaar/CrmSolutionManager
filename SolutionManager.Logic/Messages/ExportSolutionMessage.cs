using System;
using System.IO;
using Microsoft.Crm.Sdk.Messages;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Logging;
using SolutionManager.Logic.Results;

namespace SolutionManager.Logic.Messages
{
    public class ExportSolutionMessage : Message
    {
        /// <summary>
        /// Gets or sets the unique name of the Dynamics CRM solution.
        /// </summary>
        public string UniqueName { get; set; }

        /// <summary>
        /// Gets or sets the output file where the retrieved file stream should be written to.
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// Gets or sets whether the Dynamics CRM solution should be exported as managed or not.
        /// </summary>
        public bool ExportAsManaged { get; set; }

        public ExportSolutionMessage(CrmOrganization organization) : base(organization) { }

        /// <summary>
        /// Exports a Dynamics CRM solution from a Dynamics CRM organization.
        /// </summary>
        /// <returns>A <seealso cref="Result"/> object</returns>
        public override Result Execute()
        {
            if (this.UniqueName == null || this.OutputFile == null)
            {
                Logger.Log(
                    $"Invalid input argument(s). UniqueName is {this.UniqueName}, OutputFile is {this.OutputFile}.",
                    LogLevel.Warning,
                    new InvalidOperationException("UniqueName and/or OutputFile is not in a correct format")
                    );
            }

            var message = new RetrieveSolutionDataMessage(this.CrmOrganization)
            {
                UniqueName = this.UniqueName,
            };

            var retrieveSolutionResult = (RetrieveSolutionDataResult)this.CrmOrganization.Execute(message);

            if (retrieveSolutionResult.Solution == null)
            {
                Logger.Log($"The solution {this.UniqueName} was not found in the target system.", LogLevel.Warning);
                return new Result() { Success = false };
            }

            if (retrieveSolutionResult.Solution.IsManaged == true)
            {
                Logger.Log($"The solution {this.UniqueName} is a managed solution and cannot be exported.", LogLevel.Warning);
                return new Result() { Success = false };
            }

            var result = this.CrmOrganization.Execute<ExportSolutionResponse>(new ExportSolutionRequest
            {
                SolutionName = this.UniqueName,
                Managed = this.ExportAsManaged,
            });

            byte[] exportXml = result.ExportSolutionFile;

            // Overwrite the file if it already exists.
            if (File.Exists(this.OutputFile))
            {
                try
                {
                    File.Delete(this.OutputFile);
                }
                catch (IOException e)
                {
                    Logger.Log($"Exception thrown while deleting existing file: {e.Message}.", LogLevel.Warning, e);
                }
            }

            File.WriteAllBytes(this.OutputFile, exportXml);
            Logger.Log($"Solution {this.UniqueName} was exported successfully.", LogLevel.Info);

            return new Result()
            {
                Success = true,
            };
        }
    }
}
