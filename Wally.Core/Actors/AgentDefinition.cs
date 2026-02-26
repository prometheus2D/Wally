using Wally.Core.RBA;

namespace Wally.Core.Actors
{
    /// <summary>
    /// Represents the RBA definition for a single named agent, loaded from its dedicated
    /// folder under <c>.wally/Agents/&lt;AgentName&gt;/</c>.
    ///
    /// Folder layout:
    /// <code>
    ///   Agents/
    ///       &lt;AgentName&gt;/
    ///           role.txt       ? Role prompt (first line optionally: "# Tier: task")
    ///           criteria.txt   ? AcceptanceCriteria prompt
    ///           intent.txt     ? Intent prompt
    /// </code>
    ///
    /// The folder name is the agent name. Each <c>.txt</c> file may contain an optional
    /// metadata header on its first line in the form <c># Key: Value</c> (e.g.
    /// <c># Tier: task</c>). Everything after the header lines is the prompt body.
    /// </summary>
    public class AgentDefinition
    {
        /// <summary>The agent name — taken from the folder name on disk.</summary>
        public string Name { get; }

        /// <summary>The absolute path to this agent's folder.</summary>
        public string FolderPath { get; }

        /// <summary>The Role loaded from <c>role.txt</c>.</summary>
        public Role Role { get; }

        /// <summary>The AcceptanceCriteria loaded from <c>criteria.txt</c>.</summary>
        public AcceptanceCriteria Criteria { get; }

        /// <summary>The Intent loaded from <c>intent.txt</c>.</summary>
        public Intent Intent { get; }

        public AgentDefinition(string name, string folderPath,
                               Role role, AcceptanceCriteria criteria, Intent intent)
        {
            Name       = name;
            FolderPath = folderPath;
            Role       = role;
            Criteria   = criteria;
            Intent     = intent;
        }
    }
}
