using CommandLine;

namespace Wally.Console.Options
{
    [Verb("delete-actor", HelpText = "Delete an actor and its folder from the workspace.")]
    public class DeleteActorOptions
    {
        [Value(0, Required = true, HelpText = "Name of the actor to delete.")]
        public string Name { get; set; } = string.Empty;
    }
}
