using System;
using System.IO;
using System.Text.Json;

namespace Wally.Core.Logging
{
    /// <summary>
    /// Writes structured log entries for a single process / environment lifetime.
    /// <para>
    /// Each logger owns a <see cref="SessionId"/> (GUID) and a <see cref="StartedAt"/>
    /// timestamp. Log files are stored under a session folder at:
    /// <c>&lt;workspace&gt;/Logs/&lt;SessionName&gt;/</c>
    /// </para>
    /// <para>
    /// Log files are named by rounding the current UTC time to the nearest
    /// <see cref="RotationMinutes"/>-minute boundary (e.g. <c>2025-07-13_1430.txt</c>).
    /// When the rounded timestamp differs from the current file, a new file is
    /// opened automatically. Set <see cref="RotationMinutes"/> to <c>0</c> to
    /// write everything to a single file.
    /// </para>
    /// The logger is thread-safe (writes are serialised via a lock).
    /// When no workspace is loaded, entries are buffered in memory and
    /// flushed to disk once <see cref="Bind"/> is called.
    /// </summary>
    public sealed class SessionLogger : IDisposable
    {
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

        /// <summary>
        /// The time bucket size in minutes used for log file names.
        /// Each file covers one bucket. Default: <c>2</c>.
        /// Set to <c>0</c> to disable rotation (single file named <c>session.txt</c>).
        /// </summary>
        public int RotationMinutes { get; set; } = 2;

        // — Internal state ———————————————————————————————————————————————————

        private readonly object _lock = new();
        private readonly JsonSerializerOptions _jsonOptions;

        private StreamWriter? _writer;
        private MemoryStream? _buffer;
        private StreamWriter? _bufferWriter;
        private string? _logFolder;
        private string? _currentFileName;
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

        /// <summary>The session log folder, or <see langword="null"/> if not yet bound.</summary>
        public string? LogFolder => _logFolder;

        /// <summary>The name of the current log file being written to.</summary>
        public string? CurrentLogFile => _currentFileName;

        // — Binding to a workspace ——————————————————————————————————————————

        /// <summary>
        /// Binds the logger to a workspace, creating the session folder and
        /// flushing any buffered entries to disk.
        /// </summary>
        public void Bind(string workspaceFolder, string logsFolderName = "Logs")
        {
            lock (_lock)
            {
                if (_disposed) return;

                _logFolder = Path.Combine(workspaceFolder, logsFolderName, SessionName);
                Directory.CreateDirectory(_logFolder);

                // Open the initial log file.
                EnsureWriter(DateTimeOffset.UtcNow);

                // Flush buffered entries to disk.
                if (_buffer is { Length: > 0 })
                {
                    _buffer.Position = 0;
                    using var reader = new StreamReader(_buffer, leaveOpen: true);
                    string buffered = reader.ReadToEnd();
                    _writer!.Write(buffered);
                }

                _bufferWriter?.Dispose();
                _buffer?.Dispose();
                _bufferWriter = null;
                _buffer = null;
            }
        }

        // — Logging methods ——————————————————————————————————————————————

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

        /// <summary>
        /// Logs the fully enriched prompt that is actually sent to the CLI tool.
        /// This is the output of <see cref="Actors.Actor.ProcessPrompt"/> — the
        /// RBA-wrapped version of the raw user prompt.
        /// </summary>
        public void LogProcessedPrompt(string actorName, string processedPrompt, string? model = null, int iteration = 0)
        {
            Write(new LogEntry
            {
                Timestamp       = DateTimeOffset.UtcNow,
                SessionId       = SessionId.ToString("N"),
                Category        = "ProcessedPrompt",
                ActorName       = actorName,
                ProcessedPrompt = processedPrompt,
                Model           = model,
                Iteration       = iteration
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

        /// <summary>
        /// Logs a response received from an actor, with an optional iteration number
        /// for loop-based runs.
        /// </summary>
        public void LogResponse(string actorName, string? response, long elapsedMs, int iteration)
        {
            Write(new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                SessionId = SessionId.ToString("N"),
                Category  = "Response",
                ActorName = actorName,
                Response  = response,
                ElapsedMs = elapsedMs,
                Iteration = iteration
            });
        }

        /// <summary>
        /// Logs a CLI-level error (non-zero exit code or stderr output) from a
        /// tool invocation such as <c>gh copilot</c>.
        /// </summary>
        public void LogCliError(string actorName, int exitCode, string? stderr)
        {
            Write(new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                SessionId = SessionId.ToString("N"),
                Category  = "CliError",
                ActorName = actorName,
                Message   = $"Exit code {exitCode}" +
                            (string.IsNullOrWhiteSpace(stderr) ? "" : $": {stderr}")
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

                if (_logFolder != null)
                {
                    EnsureWriter(entry.Timestamp);
                    _writer!.WriteLine(line);
                }
                else
                {
                    _bufferWriter?.WriteLine(line);
                }
            }
        }

        // — File rotation ————————————————————————————————————————————————————

        /// <summary>
        /// Computes the file name for <paramref name="now"/> and, if it differs
        /// from the current file, closes the old writer and opens the new one.
        /// Must be called under <see cref="_lock"/>.
        /// </summary>
        private void EnsureWriter(DateTimeOffset now)
        {
            string fileName = GetFileNameForTimestamp(now);

            if (fileName == _currentFileName && _writer != null)
                return;

            // Different bucket — rotate.
            _writer?.Dispose();
            _currentFileName = fileName;
            string filePath = Path.Combine(_logFolder!, _currentFileName);
            _writer = new StreamWriter(filePath, append: true) { AutoFlush = true };
        }

        /// <summary>
        /// Returns the log file name for a given timestamp.
        /// When rotation is enabled, the timestamp is floored to the nearest
        /// <see cref="RotationMinutes"/>-minute boundary.
        /// When rotation is disabled (<c>0</c>), returns <c>session.txt</c>.
        /// </summary>
        private string GetFileNameForTimestamp(DateTimeOffset timestamp)
        {
            if (RotationMinutes <= 0)
                return "session.txt";

            // Floor to the nearest N-minute boundary.
            long totalMinutes = (long)timestamp.UtcDateTime.TimeOfDay.TotalMinutes;
            long bucket = totalMinutes / RotationMinutes * RotationMinutes;
            var rounded = timestamp.UtcDateTime.Date.AddMinutes(bucket);
            return $"{rounded:yyyy-MM-dd_HHmm}.txt";
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
