using System.Collections.Generic;
using System.Linq;

namespace Wally.Core
{
    /// <summary>
    /// The response from a single step in a <see cref="WallyPipeline"/> run.
    /// </summary>
    public sealed class WallyRunResult
    {
        /// <summary>
        /// The step name, or <see langword="null"/> for a single-actor (no-step) run.
        /// </summary>
        public string? StepName { get; init; }

        /// <summary>
        /// The actor that produced this result, or <c>"(no actor)"</c> for direct-mode runs.
        /// </summary>
        public string ActorName { get; init; } = "(no actor)";

        /// <summary>The AI response text.</summary>
        public string Response { get; init; } = string.Empty;

        /// <summary>
        /// Display label for UI bubbles.
        /// Examples: <c>"Engineer"</c>, <c>"TechnicalReview (Engineer)"</c>.
        /// </summary>
        public string DisplayLabel() =>
            StepName != null ? $"{StepName} ({ActorName})" : ActorName;

        /// <summary>Extracts the raw response strings from a result list.</summary>
        public static List<string> ToStringList(IEnumerable<WallyRunResult> results) =>
            results.Select(r => r.Response).ToList();
    }
}
