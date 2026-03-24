namespace Wally.Core.Actors
{
    /// <summary>
    /// Describes a single parameter in an <see cref="ActionDefinition"/>,
    /// loaded from the <c>"parameters": [...]</c> array in <c>actor.json</c>.
    /// </summary>
    public class ActionParameterDefinition
    {
        /// <summary>Parameter name, e.g. <c>"path"</c>, <c>"content"</c>.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Parameter type hint, e.g. <c>"string"</c>.</summary>
        public string Type { get; set; } = "string";

        /// <summary>Human-readable description of what the parameter does.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// When <see langword="true"/> the dispatcher will reject the action if this
        /// parameter is absent or empty.
        /// </summary>
        public bool Required { get; set; }
    }
}
