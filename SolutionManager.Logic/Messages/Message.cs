using SolutionManager.Logic.DynamicsCrm;
using SolutionManager.Logic.Results;

namespace SolutionManager.Logic.Messages
{
    public abstract class Message
    {
        protected CrmOrganization CrmOrganization { get; set; }

        public Message(CrmOrganization organization)
        {
            this.CrmOrganization = organization;
        }

        public abstract Result Execute();
    }
}
