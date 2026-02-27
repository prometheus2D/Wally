using System;
using System.IO;
using System.Text.Json;

namespace Wally.Core.Logging
{
    /// <summary>
    /// Writes structured JSON-lines log entries for a single process / environment lifetime.
    /// <para>
    /// Each logger owns a <see cref="SessionId"/> (GUID) and a <see cref="StartedAt"/>
    /// timestamp. Log files are stored at:
    /// <c>&lt;workspace&gt;/Logs/&lt;yyyy-MM-dd_HHmmss_shortguid&gt;/session.jsonl</c>
    /// </para>
    /// The logger is thread-safe (writes are serialised via a lock).
    /// When no workspace is loaded, entries are buffered in memory and
    /// flushed to disk once <see cref="Bind"/> is called.
    /// </summary>
    public sealed class SessionLogger : IDisposable
    {
        // — Constants ————————————————————————————————————————————————————————

        private const string LogFileName = "session.jsonl";

        // — Session identity —————————————————————————————————————————————————

        /// <summary>Unique identifier for this session.</summary>
        public Guid SessionId { get; }

        /// <summary>UTC timestamp when the logger was created.</summary>
        public DateTimeOffset StartedAt { get; }

        /// <summary>
        /// A short, filesystem-safe name derived from <see cref="StartedAt"/> and
        /// <see cref="SessionId"/>. Used as the session log subfolder name.
        /// Example: <c>2025-07-13_143022_a1b2c3d4</c>
        /// </summary>
        public string SessionName { get; }

        // — Internal state ———————————————————————————————————————————————————

        private readonly object _lock = new();
        private readonly JsonSerializerOptions _jsonOptions;

        private StreamWriter? _writer;
        private MemoryStream? _buffer;
        private StreamWriter? _bufferWriter;
        private string? _logFolder;
        private bool _disposed;

        // — Constructor ——————————————————————————————————————————————————————

        public SessionLogger()
        {
            SessionId   = Guid.NewGuid();
            StartedAt   = DateTimeOffset.UtcNow;
            SessionName = $"{StartedAt:yyyy-MM-dd_HHmmss}_{SessionId.ToString("N")[..8]}";

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Buffer entries in memory until Bind() is called.
            _buffer = new MemoryStream();
            _bufferWriter = new StreamWriter(_buffer, leaveOpen: true) { AutoFlush = true };
        }

        // — Public properties ————————————————————————————————————————————————

        /// <summary>The folder where log files are written, or <see langword="null"/> if not yet bound.</summary>
        public string? LogFolder => _logFolder;

        // — Binding to a workspace ——————————————————————————————————————————

        /// <summary>
        /// Binds the logger to a workspace, creating the log folder and flushing
        /// any buffered entries to disk.
        /// </summary>
        /// <param name="workspaceFolder">The workspace folder path (e.g. <c>/repo/.wally</c>).</param>
        /// <param name="logsFolderName">The name of the logs subfolder (default <c>Logs</c>).</param>
        public void Bind(string workspaceFolder, string logsFolderName = "Logs")
        {
            lock (_lock)
            {
                if (_disposed) return;

                _logFolder = Path.Combine(workspaceFolder, logsFolderName, SessionName);
                Directory.CreateDirectory(_logFolder);

                string logPath = Path.Combine(_logFolder, LogFileName);
                _writer = new StreamWriter(logPath, append: true) { AutoFlush = true };

                // Flush buffered entries to disk.
                if (_buffer is { Length: > 0 })
                {
                    _buffer.Position = 0;
                    using var reader = new StreamReader(_buffer, leaveOpen: true);
                    string buffered = reader.ReadToEnd();
                    _writer.Write(buffered);
                }

                _bufferWriter?.Dispose();
                _buffer?.Dispose();
                _bufferWriter = null;
                _buffer = null;
            }
        }

        // — Logging methods ——————————————————————————————————————————————————

        /// <summary>Logs a command invocation.</summary>
        public void LogCommand(string command, string? message = null)
        {
            Write(new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                SessionId = SessionId.ToString("N"),
                Category  = "Command",
                Command   = command,
                Message   = message
            });
        }

        /// <summary>Logs a prompt sent to an actor.</summary>
        public void LogPrompt(string actorName, string prompt, string? model = null)
        {
            Write(new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                SessionId = SessionId.ToString("N"),
                Category  = "Prompt",
                ActorName = actorName,
                Prompt    = prompt,
                Model     = model
            });
        }

        /// <summary>Logs a response received from an actor.</summary>
        public void LogResponse(string actorName, string? response, long elapsedMs)
        {
            Write(new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                SessionId = SessionId.ToString("N"),
                Category  = "Response",
                ActorName = actorName,
                Response  = response,
                ElapsedMs = elapsedMs
            });
        }

        /// <summary>Logs an informational message.</summary>
        public void LogInfo(string message)
        {
            Write(new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                SessionId = SessionId.ToString("N"),
                Category  = "Info",
                Message   = message
            });
        }

        /// <summary>Logs an error message.</summary>
        public void LogError(string message, string? command = null)
        {
            Write(new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                SessionId = SessionId.ToString("N"),
                Category  = "Error",
                Command   = command,
                Message   = message
            });
        }

        // — Core write ———————————————————————————————————————————————————————

        private void Write(LogEntry entry)
        {
            string json = JsonSerializer.Serialize(entry, _jsonOptions);
            string line = $"[{entry.Category}] {json}";

            lock (_lock)
            {
                if (_disposed) return;

                if (_writer != null)
                {
                    _writer.WriteLine(line);
                }
                else
                {
                    // Still buffering — workspace not yet bound.
                    _bufferWriter?.WriteLine(line);
                }
            }
        }

        // — Dispose ——————————————————————————————————————————————————————————

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;

                _writer?.Dispose();
                _bufferWriter?.Dispose();
                _buffer?.Dispose();
            }
        }
    }
}
