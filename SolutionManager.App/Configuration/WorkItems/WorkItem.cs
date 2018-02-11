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
    [XmlInclude(typeof(ImportSolutionWorkItem))]
    [XmlInclude(typeof(ExportSolutionWorkItem))]
    [XmlInclude(typeof(DeleteSolutionWorkItem))]
    [XmlInclude(typeof(EnableEntityChangeTrackingWorkItem))]
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