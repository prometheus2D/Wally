using System;
using CommandLine;
using Wally.Core;

namespace Wally.Console
{
    [Verb("load", HelpText = "Load a Wally workspace from the specified path.")]
    public class LoadOptions
    {
        [Value(0, Required = true, HelpText = "The path to the workspace folder.")]
        public string Path { get; set; }
    }

    [Verb("save", HelpText = "Save the current Wally environment to the specified path.")]
    public class SaveOptions
    {
        [Value(0, Required = true, HelpText = "The path to save the workspace.")]
        public string Path { get; set; }
    }

    [Verb("create", HelpText = "Create a new default Wally workspace at the specified path.")]
    public class CreateOptions
    {
        [Value(0, Required = true, HelpText = "The path for the new workspace.")]
        public string Path { get; set; }
    }

    [Verb("run", HelpText = "Run all agents on the given prompt.")]
    public class RunOptions
    {
        [Value(0, Required = true, HelpText = "The prompt to process.")]
        public string Prompt { get; set; }
    }

    [Verb("list", HelpText = "List agents and configuration files.")]
    public class ListOptions { }

    [Verb("add-file", HelpText = "Add a file path to the Wally environment.")]
    public class AddFileOptions
    {
        [Value(0, Required = true, HelpText = "The file path to add.")]
        public string FilePath { get; set; }
    }

    [Verb("load-config", HelpText = "Load configuration from a JSON file.")]
    public class LoadConfigOptions
    {
        [Value(0, Required = true, HelpText = "The path to the JSON configuration file.")]
        public string JsonPath { get; set; }
    }

    [Verb("load-agents", HelpText = "Load default agents from a JSON file.")]
    public class LoadAgentsOptions
    {
        [Value(0, Required = true, HelpText = "The path to the JSON agents file.")]
        public string JsonPath { get; set; }
    }

    [Verb("ensure-folders", HelpText = "Ensure all required folders exist in the workspace.")]
    public class EnsureFoldersOptions { }
}