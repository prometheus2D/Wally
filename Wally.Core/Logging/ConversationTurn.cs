using System;
using System.Text.Json.Serialization;

namespace Wally.Core.Logging
{
    /// <summary>
    /// One completed LLM exchange: prompt in, response out.
    /// Serialised as a single JSON line in <c>conversation.jsonl</c>.
    /// <para>
    /// Each turn captures the raw user prompt and the wrapper response,
    /// along with enough metadata to reconstruct context: who (actor),
    /// how (wrapper, model), when (timestamp, elapsed), and where in a
    /// loop (loop name, iteration).
    /// </para>
    /// </summary>
    public sealed class ConversationTurn
    {
        /// <summary>UTC timestamp when the call completed.</summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>Links to <see cref="SessionLogger.SessionId"/>.</summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// The actor name, or <see langword="null"/> for direct/no-actor mode.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ActorName { get; set; }

        /// <summary>Which <see cref="Providers.LLMWrapper.Name"/> executed the call.</summary>
        public string WrapperName { get; set; } = string.Empty;

        /// <summary>The resolved model identifier, or <see langword="null"/>.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Model { get; set; }

        /// <summary>The raw user prompt (not the RBA-enriched version).</summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>What came back from <c>wrapper.Execute()</c>.</summary>
        public string Response { get; set; } = string.Empty;

        /// <summary>
        /// <see langword="true"/> when the wrapper returned an error response
        /// (non-zero exit code, process failure, or empty output).
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsError { get; set; }

        /// <summary>Wall-clock time for <c>wrapper.Execute()</c> in milliseconds.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long ElapsedMs { get; set; }

        /// <summary>
        /// The loop name if this turn was part of a named loop, otherwise <see langword="null"/>.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LoopName { get; set; }

        /// <summary>
        /// 0-based iteration index. 0 for non-loop or first iteration.
        /// Mid-loop turns (Iteration &gt; 0) are excluded from history injection.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Iteration { get; set; }
    }
}
