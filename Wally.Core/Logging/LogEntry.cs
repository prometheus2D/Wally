using System;
using System.Text.Json.Serialization;

namespace Wally.Core.Logging
{
    /// <summary>
    /// A single structured log entry written by <see cref="SessionLogger"/>.
    /// Serialised as one JSON object per line in the session log file.
    /// </summary>
    public sealed class LogEntry
    {
        /// <summary>UTC timestamp of the entry.</summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>Session identifier.</summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Category of the log entry, e.g. <c>Command</c>, <c>Prompt</c>,
        /// <c>Response</c>, <c>Info</c>, <c>Error</c>.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>The actor name involved, if any.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ActorName { get; set; }

        /// <summary>The command or action that generated this entry.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Command { get; set; }

        /// <summary>The prompt text sent to an actor.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Prompt { get; set; }

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
    }
}
