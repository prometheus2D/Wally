using System;
using System.CommandLine;
using Wally.Core;

public static class WallyCliCommands
{
    public static Command CreateLoadCommand()
    {
        var loadCommand = new Command("load", "Load a Wally workspace from the specified path.");
        var loadPathArgument = new Argument<string>("path", "The path to the workspace folder.");
        loadCommand.AddArgument(loadPathArgument);
        loadCommand.SetHandler((path) => WallyCommands.HandleLoad(path), loadPathArgument);
        return loadCommand;
    }

    public static Command CreateSaveCommand()
    {
        var saveCommand = new Command("save", "Save the current Wally environment to the specified path.");
        var savePathArgument = new Argument<string>("path", "The path to save the workspace.");
        saveCommand.AddArgument(savePathArgument);
        saveCommand.SetHandler((path) => WallyCommands.HandleSave(path), savePathArgument);
        return saveCommand;
    }

    public static Command CreateCreateCommand()
    {
        var createCommand = new Command("create", "Create a new default Wally workspace at the specified path.");
        var createPathArgument = new Argument<string>("path", "The path for the new workspace.");
        createCommand.AddArgument(createPathArgument);
        createCommand.SetHandler((path) => WallyCommands.HandleCreate(path), createPathArgument);
        return createCommand;
    }

    public static Command CreateRunCommand()
    {
        var runCommand = new Command("run", "Run all agents on the given prompt.");
        var runPromptArgument = new Argument<string>("prompt", "The prompt to process.");
        runCommand.AddArgument(runPromptArgument);
        runCommand.SetHandler((prompt) =>
        {
            var responses = WallyCommands.HandleRun(prompt);
            foreach (var response in responses)
            {
                Console.WriteLine(response);
            }
            if (responses.Count == 0)
            {
                Console.WriteLine("No responses from agents.");
            }
        }, runPromptArgument);
        return runCommand;
    }

    public static Command CreateListCommand()
    {
        var listCommand = new Command("list", "List agents and configuration files.");
        listCommand.SetHandler(() => WallyCommands.HandleList());
        return listCommand;
    }
}

public static class Program
{
    public static int Main(string[] args)
    {
        var environment = new WallyEnvironment();
        WallyCommands.SetEnvironment(environment);

        var rootCommand = new RootCommand("Wally - AI Agent Environment Manager");

        rootCommand.AddCommand(WallyCliCommands.CreateLoadCommand());
        rootCommand.AddCommand(WallyCliCommands.CreateSaveCommand());
        rootCommand.AddCommand(WallyCliCommands.CreateCreateCommand());
        rootCommand.AddCommand(WallyCliCommands.CreateRunCommand());
        rootCommand.AddCommand(WallyCliCommands.CreateListCommand());

        return rootCommand.Invoke(args);
    }
}

