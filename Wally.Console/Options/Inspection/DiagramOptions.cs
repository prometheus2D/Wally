using CommandLine;

namespace Wally.Console.Options.Inspection
{
    [Verb("diagram", HelpText = "Generate a Mermaid diagram for a workspace, loop, step, or runbook.")]
    public class DiagramOptions
    {
        [Value(0, Required = true, HelpText = "Target type: workspace | loop | step | runbook.")]
        public string TargetType { get; set; } = string.Empty;

        [Value(1, Required = false, HelpText = "Target name. Required for loop/runbook. For step, this is the loop name.")]
        public string? Name { get; set; }

        [Value(2, Required = false, HelpText = "Step name or 1-based step index when target type is step.")]
        public string? SecondaryName { get; set; }

        [Option('f', "format", Required = false, Default = "png",
            HelpText = "Output format: png, svg, or pdf.")]
        public string Format { get; set; } = "png";

        [Option('o', "output", Required = false, Default = null,
            HelpText = "Optional output path. Defaults to Docs/Diagrams inside the workspace.")]
        public string? OutputPath { get; set; }
    }
}