using CommandLine;

namespace Wally.Console.Options
{
    [Verb("runbook", HelpText = "Execute a runbook — a sequence of Wally commands from a .wrb file.")]
    public class RunbookOptions
    {
        [Value(0, Required = true, HelpText = "Name of the runbook (without .wrb extension).")]
        public string Name { get; set; }

        [Value(1, Required = false, HelpText = "Optional prompt to substitute into {userPrompt} placeholders.")]
        public string? Prompt { get; set; }
    }
}
