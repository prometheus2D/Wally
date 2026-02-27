namespace Wally.Core
{
    /// <summary>
    /// Configuration for which Copilot model is used when running actors.
    /// Maps to the <c>--model</c> flag of <c>gh copilot</c>.
    /// </summary>
    public class CopilotModelConfig
    {
        /// <summary>
        /// The model identifier passed to <c>gh copilot --model</c>.
        /// When <see langword="null"/> or empty, the Copilot CLI default model is used.
        /// <para>
        /// Common values: <c>"gpt-4o"</c>, <c>"gpt-4.1"</c>, <c>"claude-3.5-sonnet"</c>,
        /// <c>"o4-mini"</c>, <c>"gemini-2.0-flash-001"</c>.
        /// Run <c>gh copilot model list</c> to see the full set available to your account.
        /// </para>
        /// </summary>
        public string? Default { get; set; }

        /// <summary>
        /// Optional per-actor model overrides. The key is the actor name (case-insensitive),
        /// the value is the model identifier to use for that actor.
        /// When an actor is not listed here, <see cref="Default"/> is used.
        /// </summary>
        public Dictionary<string, string>? ActorOverrides { get; set; }

        /// <summary>
        /// Resolves the model to use for a given actor name.
        /// Returns <see langword="null"/> when no model is configured (use Copilot default).
        /// </summary>
        public string? ResolveForActor(string actorName)
        {
            if (ActorOverrides != null && !string.IsNullOrEmpty(actorName))
            {
                foreach (var kvp in ActorOverrides)
                {
                    if (string.Equals(kvp.Key, actorName, StringComparison.OrdinalIgnoreCase))
                        return kvp.Value;
                }
            }

            return Default;
        }
    }
}
