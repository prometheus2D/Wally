using CommandLine;

namespace Wally.Console.Options.Inspection
{
    [Verb("tutorial-mode", HelpText = "Toggle the startup tutorial summary on or off. Usage: tutorial-mode [on|off|toggle]")]
    public class TutorialModeOptions
    {
        [Value(0, MetaName = "value", Required = false,
            HelpText = "on / off / toggle (default: toggle)")]
        public string? Value { get; set; }
    }
}
