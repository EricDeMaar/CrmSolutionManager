using System;
using System.IO;
using Microsoft.Crm.Sdk.Messages;
using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Helpers;
using SolutionManager.Logic.Results;

namespace SolutionManager.Logic.Messages
{
    public class ExportSolutionMessage : Message
    {
        public string UniqueName { get; set; }
        public string OutputFile { get; set; }
        public bool ExportAsManaged { get; set; }

        public ExportSolutionMessage(CrmOrganization organization) : base(organization) { }

        public override Result Execute()
        {
            if (this.UniqueName == null || this.OutputFile == null)
                throw new InvalidOperationException("UniqueName and/or OutputFile is not in a correct format");

            var message = new RetrieveSolutionDataMessage(this.CrmOrganization)
            {
                UniqueName = this.UniqueName,
            };

            var retrieveSolutionResult = (RetrieveSolutionDataResult)this.CrmOrganization.Execute(message);

            if (retrieveSolutionResult.Solution == null)
            {
                Logger.Log($"The solution {this.UniqueName} was not found in the target system.");
                return new Result() { Success = false };
            }

            if (retrieveSolutionResult.Solution.IsManaged == false)
            {
                Logger.Log($"The solution {this.UniqueName} is a managed solution and cannot be exported.");
                return new Result() { Success = false };
            }

            var result = this.CrmOrganization.Execute<ExportSolutionResponse>(new ExportSolutionRequest
            {
                SolutionName = this.UniqueName,
                Managed = this.ExportAsManaged,
            });

            byte[] exportXml = result.ExportSolutionFile;

            if (File.Exists(this.OutputFile))
            {
                try
                {
                    File.Delete(this.OutputFile);
                }
                catch (IOException e)
                {
                    Logger.Log($"Exception thrown while deleting existing file: {e.Message}.");
                    throw;
                }
            }

            File.WriteAllBytes(this.OutputFile, exportXml);
            Logger.Log($"Solution {this.UniqueName} was exported successfully.");

            return new Result()
            {
                Success = true,
            };
        }
    }
}
