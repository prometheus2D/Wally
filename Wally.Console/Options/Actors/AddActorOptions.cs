using CommandLine;

namespace Wally.Console.Options.Actors
{
    [Verb("add-actor", HelpText = "Create a new actor with RBA prompts in the workspace.")]
    public class AddActorOptions
    {
        [Value(0, Required = true, HelpText = "Name for the new actor (becomes the folder name).")]
        public string Name { get; set; } = string.Empty;

        [Option('r', "role", Required = false, Default = "",
            HelpText = "The role prompt for this actor.")]
        public string RolePrompt { get; set; } = string.Empty;

        [Option('c', "criteria", Required = false, Default = "",
            HelpText = "The acceptance criteria prompt for this actor.")]
        public string CriteriaPrompt { get; set; } = string.Empty;

        [Option('i', "intent", Required = false, Default = "",
            HelpText = "The intent prompt for this actor.")]
        public string IntentPrompt { get; set; } = string.Empty;
    }
}
