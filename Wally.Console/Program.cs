using System;
using CommandLine;
using Wally.Core;

namespace Wally.Console
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var environment = new WallyEnvironment();
            WallyCommands.SetEnvironment(environment);

            return Parser.Default.ParseArguments<LoadOptions, SaveOptions, CreateOptions, RunOptions, ListOptions, AddFileOptions, LoadConfigOptions, LoadAgentsOptions, EnsureFoldersOptions>(args)
                .MapResult(
                    (LoadOptions opts) => { WallyCommands.HandleLoad(opts.Path); return 0; },
                    (SaveOptions opts) => { WallyCommands.HandleSave(opts.Path); return 0; },
                    (CreateOptions opts) => { WallyCommands.HandleCreate(opts.Path); return 0; },
                    (RunOptions opts) =>
                    {
                        var responses = WallyCommands.HandleRun(opts.Prompt);
                        foreach (var response in responses)
                        {
                            System.Console.WriteLine(response);
                        }
                        if (responses.Count == 0)
                        {
                            System.Console.WriteLine("No responses from agents.");
                        }
                        return 0;
                    },
                    (ListOptions opts) => { WallyCommands.HandleList(); return 0; },
                    (AddFileOptions opts) => { WallyCommands.HandleAddFile(opts.FilePath); return 0; },
                    (LoadConfigOptions opts) => { WallyCommands.HandleLoadConfig(opts.JsonPath); return 0; },
                    (LoadAgentsOptions opts) => { WallyCommands.HandleLoadAgents(opts.JsonPath); return 0; },
                    (EnsureFoldersOptions opts) => { WallyCommands.HandleEnsureFolders(); return 0; },
                    errs => 1
                );
        }
    }
}

