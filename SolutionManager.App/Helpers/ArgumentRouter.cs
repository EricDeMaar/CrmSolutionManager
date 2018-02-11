using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using CommandLine;
using SolutionManager.App.Arguments;
using SolutionManager.App.Configuration;

namespace SolutionManager.App.Helpers
{
    public class ArgumentRouter
    {
        private ImportConfiguration ImportConfiguration { get; set; }

        public ArgumentRouter(string[] args, ImportConfiguration config)
        {
            PrintHeader();

            this.ImportConfiguration = config;

            var commandLineParser = new Parser(x =>
            {
                x.HelpWriter = null;
                x.IgnoreUnknownArguments = false;
            });

            commandLineParser.ParseArguments<Options>(args)
                .WithParsed<Options>(x => Execute(x))
                .WithNotParsed<Options>((errs) => PrintHelp(errs));
        }

        private void Execute(Options options)
        {
            if (options.PrintOrganizations)
            {
                this.PrintOrganizations();
            }

            if (options.PrintWorkDefinitions)
            {
                this.PrintWorkDefinitions();
            }

            if (!string.IsNullOrEmpty(options.WorkDefinition))
            {
                var executor = new WorkDefinitionExecutor(options.WorkDefinition, this.ImportConfiguration);

                executor.Execute();
            }
        }

        private void PrintHelp(IEnumerable<Error> errors = null)
        {
            if (errors != null)
            {
                Console.WriteLine("ERROR(S):");
                foreach (var item in errors.OfType<UnknownOptionError>())
                {
                    Console.WriteLine($"  Option '{item.Token}' is not a valid option.");
                }

                foreach (var item in errors.OfType<MissingRequiredOptionError>())
                {
                    Console.WriteLine($"  Option '{item.NameInfo}' is a required option.");
                }

                Console.WriteLine();
            }

            Console.WriteLine("COMMAND(S):");
            Console.WriteLine("  --workdefinition <name>\t\tExecutes a WorkDefinition from the ImportConfiguration.xml");
            Console.WriteLine("  --organizations\t\t\tPrints a list of the available Dynamics CRM organizations");
            Console.WriteLine("  --list\t\t\t\tLists the available WorkDefinitions in the ImportConfiguration.xml");
        }

        private void PrintWorkDefinitions()
        {
            Console.WriteLine("WORKDEFINITION(S):");

            foreach (var item in this.ImportConfiguration.WorkDefinitions)
            {
                Console.WriteLine($"  {item.Name}\t\t{item.Description}");
            }

            Console.WriteLine();
        }

        private void PrintOrganizations()
        {
            Console.WriteLine("ORGANIZATION(S):");

            foreach (var item in this.ImportConfiguration.Organizations)
            {
                Console.WriteLine($"  {item.OrganizationName}\t\t{item.OrganizationUri}");
            }

            Console.WriteLine();
        }
        
        private void PrintHeader()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            Console.WriteLine($"CrmSolutionManager {fvi.FileVersion}");
            Console.WriteLine($"Copyright (c) 2018 - {fvi.CompanyName}\n");
        }
    }
}
