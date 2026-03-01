using System;
using System.Text.Json.Serialization;

namespace Wally.Core.Logging
{
    /// <summary>
    /// A single structured log entry written by <see cref="SessionLogger"/>.
    /// Serialised as one line per entry in the session log file.
    /// </summary>
    public sealed class LogEntry
    {
        /// <summary>UTC timestamp of the entry.</summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>Session identifier.</summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Category of the log entry, e.g. <c>Command</c>, <c>Prompt</c>,
        /// <c>Response</c>, <c>Info</c>, <c>Error</c>, <c>ProcessedPrompt</c>,
        /// <c>CliError</c>.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>The actor name involved, if any.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ActorName { get; set; }

        /// <summary>The command or action that generated this entry.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Command { get; set; }

        /// <summary>The raw user prompt text sent to an actor.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Prompt { get; set; }

        /// <summary>
        /// The fully enriched prompt (after RBA processing) that was sent to the
        /// CLI tool (e.g. <c>gh copilot -p</c>). Logged separately from
        /// <see cref="Prompt"/> so both raw and processed prompts are captured.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ProcessedPrompt { get; set; }

        /// <summary>The response text received from an actor.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Response { get; set; }

        /// <summary>Free-form message for informational or error entries.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }

        /// <summary>Elapsed time for the operation, in milliseconds.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long ElapsedMs { get; set; }

        /// <summary>The model used for the actor call, if any.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Model { get; set; }

        /// <summary>
        /// The 1-based iteration number within a <c>run-loop</c>.
        /// Zero or omitted for non-loop entries.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Iteration { get; set; }

        /// <summary>
        /// The number of workspace-level documents that were injected into the prompt.
        /// Zero or omitted when no workspace docs are loaded.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int WorkspaceDocsCount { get; set; }

        /// <summary>
        /// The number of actor-level documents that were injected into the prompt.
        /// Zero or omitted when no actor docs are loaded.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int ActorDocsCount { get; set; }

        /// <summary>
        /// Comma-separated list of document file names that were injected into
        /// the prompt. Omitted when no docs are loaded.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DocsLoaded { get; set; }
    }
}
