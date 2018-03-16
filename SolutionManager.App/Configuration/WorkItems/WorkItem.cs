using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Microsoft.Xrm.Sdk;

namespace SolutionManager.App.Configuration.WorkItems
{
    [KnownType(typeof(ImportSolutionWorkItem))]
    [KnownType(typeof(ExportSolutionWorkItem))]
    [KnownType(typeof(DeleteSolutionWorkItem))]
    [KnownType(typeof(EnableEntityChangeTrackingWorkItem))]
    [KnownType(typeof(ChangeOwnerOfWorkflowsWorkItem))]
    [XmlInclude(typeof(ImportSolutionWorkItem))]
    [XmlInclude(typeof(ExportSolutionWorkItem))]
    [XmlInclude(typeof(DeleteSolutionWorkItem))]
    [XmlInclude(typeof(EnableEntityChangeTrackingWorkItem))]
    [XmlInclude(typeof(ChangeOwnerOfWorkflowsWorkItem))]
    public abstract class WorkItem
    {
        [XmlAttribute("continueOnError")]
        public bool ContinueOnError { get; set; }

        [XmlAttribute("organizationName")]
        public string OrganizationName { get; set; }

        public abstract void Execute(IOrganizationService service);

        public abstract bool Validate();
    }
}