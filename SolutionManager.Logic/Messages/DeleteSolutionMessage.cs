using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Logging;
using SolutionManager.Logic.Results;

namespace SolutionManager.Logic.Messages
{
    public class DeleteSolutionMessage : Message
    {
        /// <summary>
        /// Gets or sets the unique name of the Dynamics CRM solution.
        /// </summary>
        public string UniqueName { get; set; }

        public DeleteSolutionMessage(CrmOrganization organization) : base(organization) { }

        /// <summary>
        /// Deletes a solution from a Dynamics CRM organization.
        /// </summary>
        /// <returns>A result containing a Success boolean.</returns>
        public override Result Execute()
        {
            var message = new RetrieveSolutionDataMessage(this.CrmOrganization)
            {
                UniqueName = this.UniqueName,
            };
            var result = (RetrieveSolutionDataResult)this.CrmOrganization.Execute(message);

            if (result.Solution == null)
            {
                Logger.Log($"The solution {this.UniqueName} was not found in the target system", LogLevel.Warning);
            }

            Logger.Log($"Deleting solution {result.Solution.UniqueName} with version {result.Solution.Version} from target system.", LogLevel.Info);

            this.CrmOrganization.Delete("solution", result.Solution.SolutionId);

            var retrieveSolution = this.CrmOrganization.GetEntityByField("solution", "solutionid", result.Solution.SolutionId);

            if (retrieveSolution != null)
            {
                Logger.Log("The solution still exists.", LogLevel.Warning);

                return new Result()
                {
                    Success = false,
                };
            }
            else
            {
                Logger.Log("The solution has been deleted.", LogLevel.Info);

                return new Result()
                {
                    Success = true,
                };
            }
        }
    }
}
