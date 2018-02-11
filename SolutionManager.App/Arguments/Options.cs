using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace SolutionManager.App.Arguments
{ 
    public class Options
    {
        [Option('l', "list", Required = false, HelpText = "Lists the available WorkDefinitions in the ImportConfiguration.xml")]
        public bool PrintWorkDefinitions { get; set; }

        [Option('o', "organizations", Required = false, HelpText = "Prints a list of the available Dynamics CRM organizations.")]
        public bool PrintOrganizations { get; set; }

        [Option('w', "workdefinition", Required = false, HelpText = "Executes a WorkDefinition from the ImportConfiguration.xml")]
        public string WorkDefinition { get; set; }
    }
}