using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using Wally.Console.Options;
using Wally.Core;

namespace Wally.Console
{
    public static class Program
    {
        /// <summary>
        /// The single Wally environment for this process lifetime.
        /// Created once at startup; a workspace is loaded into it via 'load' or 'create'.
        /// </summary>
        private static readonly WallyEnvironment _environment = new WallyEnvironment();

        public static int Main(string[] args)
        {
            System.Console.OutputEncoding = Encoding.UTF8;

            if (args.Length == 0)
                return RunInteractiveMode();
            else
                return RunOneTimeMode(args);
        }

        private static int RunOneTimeMode(string[] args) => HandleArguments(args);

        private static int RunInteractiveMode()
        {
            System.Console.WriteLine("Wally Interactive Mode. Type 'help' for commands, 'exit' to quit.");
            while (true)
            {
                System.Console.Write("wally> ");
                string? input = System.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;
                if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                string[] interactiveArgs = SplitArgs(input);
                if (interactiveArgs.Length > 0)
                    HandleArguments(interactiveArgs);
            }
            return 0;
        }

        /// <summary>
        /// Splits a raw input line into arguments, respecting double-quoted strings.
        /// <c>run "Add input validation" Developer</c> →
        /// <c>["run", "Add input validation", "Developer"]</c>.
        /// </summary>
        private static string[] SplitArgs(string input)
        {
            var args = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;               // consume the quote character itself
                }

                if (c == ' ' && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        args.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0)
                args.Add(current.ToString());

            return args.ToArray();
        }

        private static int HandleArguments(string[] args)
        {
            var assemblies = new[] { Assembly.GetExecutingAssembly(), typeof(WallyEnvironment).Assembly };
            var types = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
                .ToArray();

            var result = Parser.Default.ParseArguments(args, types);
            return result.MapResult(
                (opts) =>
                {
                    // ── Workspace lifecycle ───────────────────────────────────
                    if (opts is LoadOptions lo)        { WallyCommands.HandleLoad(_environment, lo.Path); return 0; }
                    if (opts is SaveOptions so)        { WallyCommands.HandleSave(_environment, so.Path); return 0; }
                    if (opts is CreateOptions co)      { WallyCommands.HandleCreate(_environment, co.Path); return 0; }
                    if (opts is SetupOptions seto)     { WallyCommands.HandleSetup(_environment, seto.Path, seto.SourcePath); return 0; }

                    // ── Running actors ────────────────────────────────────────
                    if (opts is RunOptions ro)
                    {
                        var responses = WallyCommands.HandleRun(_environment, ro.Prompt, ro.ActorName, ro.Model);
                        foreach (var response in responses) System.Console.WriteLine(response);
                        if (responses.Count == 0) System.Console.WriteLine("No responses from Actors.");
                        return 0;
                    }
                    if (opts is RunIterativeOptions rio)
                    {
                        var responses = WallyCommands.HandleRunIterative(
                            _environment, rio.Prompt, rio.ActorName, rio.MaxIterations, rio.Model);
                        if (responses.Count == 0) System.Console.WriteLine("No responses from final iteration.");
                        return 0;
                    }

                    // ── Inspection ────────────────────────────────────────────
                    if (opts is ListOptions)              { WallyCommands.HandleList(_environment); return 0; }
                    if (opts is InfoOptions)              { WallyCommands.HandleInfo(_environment); return 0; }
                    if (opts is ReloadActorsOptions)      { WallyCommands.HandleReloadActors(_environment); return 0; }

                    // ── Help ──────────────────────────────────────────────────
                    if (opts is HelpOptions)              { WallyCommands.HandleHelp(); return 0; }

                    return 0;
                },
                errs =>
                {
                    System.Console.WriteLine("Invalid command. Type 'help' for available commands.");
                    return 1;
                }
            );
        }
    }
}

