using System.Text.Json.Serialization;

namespace Wally.Core.Actions
{
    /// <summary>
    /// Describes a single parameter accepted by an <see cref="ActorAction"/>.
    /// The schema is intentionally simple — name, type hint, and description —
    /// so it can be injected verbatim into the LLM system prompt as plain text.
    /// </summary>
    public class ActionParameter
    {
        /// <summary>
        /// The parameter name as it appears in the action call block.
        /// Example: <c>"path"</c>, <c>"content"</c>, <c>"query"</c>.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable type hint for the LLM.
        /// Examples: <c>"string"</c>, <c>"int"</c>, <c>"bool"</c>.
        /// Not enforced at runtime — serves as documentation for the LLM.
        /// </summary>
        public string Type { get; set; } = "string";

        /// <summary>
        /// Description of what this parameter represents and any constraints.
        /// Injected into the action manifest section of the system prompt.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// When <see langword="true"/> this parameter must be present in every call.
        /// The dispatcher validates required parameters before dispatching.
        /// </summary>
        public bool Required { get; set; } = true;
    }
}
