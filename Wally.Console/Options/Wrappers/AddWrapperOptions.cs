using CommandLine;

namespace Wally.Console.Options.Wrappers
{
    [Verb("add-wrapper", HelpText = "Create a new LLM wrapper definition in the workspace.")]
    public class AddWrapperOptions
    {
        [Value(0, Required = true, HelpText = "Name for the new wrapper (becomes the JSON file name).")]
        public string Name { get; set; } = string.Empty;

        [Option('d', "description", Required = false, Default = "",
            HelpText = "Description of what this wrapper does.")]
        public string Description { get; set; } = string.Empty;

        [Option('e', "executable", Required = false, Default = "gh",
            HelpText = "The executable to run (e.g. gh, ollama).")]
        public string Executable { get; set; } = "gh";

        [Option('t', "template", Required = false, Default = "",
            HelpText = "The argument template with {prompt}, {model}, {sourcePath} placeholders.")]
        public string ArgumentTemplate { get; set; } = string.Empty;

        [Option("can-make-changes", Required = false, Default = false,
            HelpText = "Whether this wrapper can make file changes (agentic mode).")]
        public bool CanMakeChanges { get; set; }
    }
}
