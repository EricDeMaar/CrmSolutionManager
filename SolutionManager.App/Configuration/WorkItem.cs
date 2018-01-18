﻿using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using SolutionManager.App.Configuration.WorkItems;

namespace SolutionManager.App.Configuration
{
    [KnownType(typeof(ImportSolutionWorkItem))]
    [KnownType(typeof(ExportSolutionWorkItem))]
    [KnownType(typeof(DeleteSolutionWorkItem))]
    [XmlInclude(typeof(ImportSolutionWorkItem))]
    [XmlInclude(typeof(ExportSolutionWorkItem))]
    [XmlInclude(typeof(DeleteSolutionWorkItem))]
    public abstract class WorkItem
    {
        [XmlAttribute("continueOnError")]
        public bool ContinueOnError { get; set; }

        [XmlAttribute("organizationName")]
        public string OrganizationName { get; set; }

        public abstract bool Validate();
    }
}