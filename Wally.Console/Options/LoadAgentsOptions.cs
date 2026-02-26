using CommandLine;

namespace Wally.Console.Options
{
    /// <summary>Re-reads all agent folders from disk and rebuilds the actor list.</summary>
    [Verb("reload-agents", HelpText = "Re-read agent folders from .wally/Agents/ and rebuild actors.")]
    public class ReloadAgentsOptions { }
}