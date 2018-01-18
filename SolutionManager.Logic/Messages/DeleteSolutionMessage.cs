using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Helpers;
using SolutionManager.Logic.Results;

namespace SolutionManager.Logic.Messages
{
    public class DeleteSolutionMessage : Message
    {
        public string UniqueName { get; set; }

        public DeleteSolutionMessage(CrmOrganization organization) : base(organization) { }

        public override Result Execute()
        {
            var message = new RetrieveSolutionDataMessage(this.CrmOrganization)
            {
                UniqueName = this.UniqueName,
            };
            var result = (RetrieveSolutionDataResult)this.CrmOrganization.ExecuteMessage(message);

            if (result.Solution == null)
            {
                Logger.Log($"The solution {this.UniqueName} was not found in the target system");
            }

            Logger.Log($"Deleting solution {result.Solution.UniqueName} with version {result.Solution.Version} from target system.");

            this.CrmOrganization.Delete("solution", result.Solution.SolutionId);

            var retrieveSolution = this.CrmOrganization.GetEntityByField("solution", "solutionid", result.Solution.SolutionId);

            if (retrieveSolution != null)
            {
                Logger.Log("The solution still exists.");
            }

            Logger.Log("The solution has been deleted.");

            return new Result()
            {
                Success = true,
            };
        }
    }
}
