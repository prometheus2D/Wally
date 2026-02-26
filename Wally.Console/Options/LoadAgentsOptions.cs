using CommandLine;

namespace Wally.Console.Options
{
    /// <summary>Re-reads all actor folders from disk and rebuilds the actor list.</summary>
    [Verb("reload-actors", HelpText = "Re-read actor folders from the workspace Actors/ directory and rebuild actors.")]
    public class ReloadActorsOptions { }
}