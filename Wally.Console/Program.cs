using System;
using System.Linq;
using System.Reflection;
using CommandLine;
using Wally.Console.Options;
using Wally.Core;
using Wally.Default.Options;

namespace Wally.Console
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var environment = new WallyEnvironment();
            WallyCommands.SetEnvironment(environment);

            var defaultAssembly = typeof(CreateTodoOptions).Assembly;
            var assemblies = new[] { Assembly.GetExecutingAssembly(), typeof(WallyEnvironment).Assembly, defaultAssembly };
            var types = assemblies.SelectMany(a => a.GetTypes()).Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();

            var result = Parser.Default.ParseArguments(args, types);
            return result.MapResult(
                (opts) =>
                {
                    if (opts is LoadOptions lo) { WallyCommands.HandleLoad(lo.Path); return 0; }
                    if (opts is SaveOptions so) { WallyCommands.HandleSave(so.Path); return 0; }
                    if (opts is CreateOptions co) { WallyCommands.HandleCreate(co.Path); return 0; }
                    if (opts is RunOptions ro)
                    {
                        var responses = WallyCommands.HandleRun(ro.Prompt);
                        foreach (var response in responses)
                        {
                            System.Console.WriteLine(response);
                        }
                        if (responses.Count == 0)
                        {
                            System.Console.WriteLine("No responses from Actors.");
                        }
                        return 0;
                    }
                    if (opts is ListOptions) { WallyCommands.HandleList(); return 0; }
                    if (opts is AddFileOptions afo) { WallyCommands.HandleAddFile(afo.FilePath); return 0; }
                    if (opts is LoadConfigOptions lco) { WallyCommands.HandleLoadConfig(lco.JsonPath); return 0; }
                    if (opts is LoadActorsOptions lao) { WallyCommands.HandleLoadActors(lao.JsonPath); return 0; }
                    if (opts is EnsureFoldersOptions) { WallyCommands.HandleEnsureFolders(); return 0; }
                    if (opts is SetupOptions) { WallyCommands.HandleSetup(); return 0; }
                    if (opts is InfoOptions) { WallyCommands.HandleInfo(); return 0; }
                    if (opts is HelpOptions) { WallyCommands.HandleHelp(); return 0; }
                    if (opts is CreateTodoOptions cto) { WallyCommands.HandleCreateTodo(cto.Path); return 0; }
                    if (opts is CreateWeatherOptions cwo) { WallyCommands.HandleCreateWeather(cwo.Path); return 0; }
                    return 0;
                },
                errs => 1
            );
        }
    }
}

