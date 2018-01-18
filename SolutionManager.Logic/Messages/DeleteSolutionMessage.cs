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
            // Todo
            var solution = this.CrmOrganization.GetSolutionByName(this.UniqueName);

            if (solution == null)
            {
                Logger.Log($"The solution {this.UniqueName} was not found in the target system");
            }

            Logger.Log($"Deleting solution {solution.UniqueName} with version {solution.Version} from target system.");

            this.CrmOrganization.Delete("solution", solution.SolutionId);

            var retrieveSolution = this.CrmOrganization.GetEntityByField("solution", "solutionid", solution.SolutionId);

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
