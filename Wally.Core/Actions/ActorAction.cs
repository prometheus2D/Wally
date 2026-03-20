using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Wally.Core.Actions
{
    /// <summary>
    /// Declares a single capability/action that an <see cref="Wally.Core.Actors.Actor"/> is
    /// permitted to invoke during a session.
    /// <para>
    /// Actions are declared in the actor's <c>actor.json</c> file under the <c>actions</c>
    /// array.  At runtime, <see cref="ActionDispatcher"/> scans the LLM response for
    /// fenced action blocks and executes only the actions the actor has declared.
    /// </para>
    /// <h3>actor.json example:</h3>
    /// <code>
    /// "actions": [
    ///   {
    ///     "name": "write_file",
    ///     "description": "Write content to a file in the workspace",
    ///     "pathPattern": "Projects/**/*.md",
    ///     "parameters": [
    ///       { "name": "path",    "type": "string", "description": "Relative path from WorkSource" },
    ///       { "name": "content", "type": "string", "description": "Full file content to write"    }
    ///     ]
    ///   },
    ///   {
    ///     "name": "read_file",
    ///     "description": "Read the content of a file",
    ///     "parameters": [
    ///       { "name": "path", "type": "string", "description": "Relative path from WorkSource" }
    ///     ]
    ///   }
    /// ]
    /// </code>
    /// <h3>LLM response block format (fenced, parsed by ActionDispatcher):</h3>
    /// <code>
    /// ```action
    /// name: write_file
    /// path: Projects/MyProject/Requirements/auth.md
    /// content: |
    ///   # Auth Requirements
    ///   ...
    /// ```
    /// </code>
    /// </summary>
    public class ActorAction
    {
        // ?? Identity ?????????????????????????????????????????????????????????

        /// <summary>
        /// The unique action name used in LLM response blocks.
        /// Examples: <c>"write_file"</c>, <c>"read_file"</c>, <c>"run_command"</c>.
        /// Must be a valid identifier (no spaces).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description injected into the system prompt so the LLM
        /// understands when and how to use this action.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        // ?? Parameters ???????????????????????????????????????????????????????

        /// <summary>
        /// Ordered list of parameters this action accepts.
        /// Injected into the action manifest section of the system prompt.
        /// </summary>
        public List<ActionParameter> Parameters { get; set; } = new();

        // ?? Path constraints ?????????????????????????????????????????????????

        /// <summary>
        /// Optional glob pattern that restricts which paths this action may
        /// operate on.  When set, <see cref="ActionDispatcher"/> rejects calls
        /// whose <c>path</c> parameter does not match this pattern.
        /// <para>
        /// Uses <c>*</c> (any chars in a single segment) and <c>**</c>
        /// (any chars across path separators).
        /// </para>
        /// Examples:
        /// <list type="bullet">
        ///   <item><c>"Projects/**/*.md"</c> — any Markdown file under Projects/</item>
        ///   <item><c>"**"</c> — unrestricted (same as omitting the field)</item>
        /// </list>
        /// When <see langword="null"/> or empty, no path restriction is applied.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PathPattern { get; set; }

        // ?? Scope hint ???????????????????????????????????????????????????????

        /// <summary>
        /// Indicates whether this action performs a write/mutating operation.
        /// When <see langword="true"/> the action is only permitted when the
        /// active wrapper has <c>CanMakeChanges = true</c> (Agent mode).
        /// Default: <see langword="false"/> (read / inspect only).
        /// </summary>
        public bool IsMutating { get; set; } = false;

        // ?? Prompt manifest ??????????????????????????????????????????????????

        /// <summary>
        /// Renders this action as a single entry in the system-prompt action manifest.
        /// Format:
        /// <code>
        /// - write_file(path: string, content: string)
        ///     Write content to a file in the workspace [paths: Projects/**/*.md]
        /// </code>
        /// </summary>
        public string ToManifestLine()
        {
            string paramSig = Parameters.Count > 0
                ? string.Join(", ", System.Linq.Enumerable.Select(Parameters, p =>
                    $"{p.Name}: {p.Type}{(p.Required ? "" : "?")}"))
                : "";

            string constraint = !string.IsNullOrWhiteSpace(PathPattern)
                ? $" [paths: {PathPattern}]"
                : "";

            string mutatingHint = IsMutating ? " [mutating — Agent mode only]" : "";

            string line = $"- {Name}({paramSig})";
            if (!string.IsNullOrWhiteSpace(Description))
                line += $"\n    {Description}{constraint}{mutatingHint}";

            return line;
        }
    }
}
