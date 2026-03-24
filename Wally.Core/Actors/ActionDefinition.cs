using System.Collections.Generic;

namespace Wally.Core.Actors
{
    /// <summary>
    /// Describes a role-specific action declared in <c>actor.json</c> under
    /// <c>"actions": [...]</c>.
    /// <para>
    /// At runtime <see cref="ActionDispatcher"/> reads these definitions to:
    /// <list type="bullet">
    ///   <item>Authorise the actor to use the named action.</item>
    ///   <item>Validate required parameters before execution.</item>
    ///   <item>Enforce the <see cref="PathPattern"/> glob on the <c>path</c> parameter.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class ActionDefinition
    {
        /// <summary>Action name as it appears in the LLM action block, e.g. <c>"change_code"</c>.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Human-readable description shown in actor documentation.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Glob pattern restricting which paths this action may write to.
        /// <c>"**"</c> allows any path; <c>"**/*.md"</c> restricts to Markdown files only.
        /// Defaults to <c>"**"</c> (unrestricted).
        /// </summary>
        public string PathPattern { get; set; } = "**";

        /// <summary>
        /// When <see langword="true"/> the action writes or modifies files.
        /// The dispatcher will block it when the current wrapper's
        /// <c>CanMakeChanges</c> flag is <see langword="false"/>.
        /// </summary>
        public bool IsMutating { get; set; }

        /// <summary>Parameter definitions for this action.</summary>
        public List<ActionParameterDefinition> Parameters { get; set; } = new();
    }
}
