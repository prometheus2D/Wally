using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Wally.Core.Logging
{
    /// <summary>
    /// Append-only conversation history logger that records every (prompt, response)
    /// pair flowing through the LLM wrappers.
    /// <para>
    /// Follows the same lifecycle as <see cref="SessionLogger"/>: created at
    /// <see cref="WallyEnvironment"/> construction, bound to a workspace folder
    /// via <see cref="Bind"/>, unbound on workspace close via <see cref="Unbind"/>.
    /// Before binding, turns are buffered in memory.
    /// </para>
    /// <para>
    /// The history file is a single <c>conversation.jsonl</c> in
    /// <c>&lt;workspace&gt;/History/</c>. One JSON line per completed LLM call.
    /// No rotation — clear the file to start fresh.
    /// </para>
    /// Thread-safe (all reads and writes are serialised via a lock).
    /// </summary>
    public sealed class ConversationLogger : IDisposable
    {
        // ?? Constants ?????????????????????????????????????????????????????????

        /// <summary>Default subfolder name inside the workspace folder.</summary>
        public const string DefaultFolderName = "History";

        /// <summary>The conversation history file name.</summary>
        private const string FileName = "conversation.jsonl";

        // ?? History injection limits ??????????????????????????????????????????

        /// <summary>Maximum number of recent turns injected into a prompt.</summary>
        public const int MaxInjectedTurns = 5;

        /// <summary>Maximum characters per response when formatting for injection.</summary>
        public const int MaxResponseCharsPerTurn = 2000;

        /// <summary>Maximum total characters for the formatted history block.</summary>
        public const int MaxTotalHistoryChars = 8000;

        // ?? State ?????????????????????????????????????????????????????????????

        private readonly object _lock = new();
        private readonly JsonSerializerOptions _jsonOptions;

        private StreamWriter? _writer;
        private string? _historyFolder;
        private string? _filePath;
        private List<ConversationTurn>? _buffer;
        private bool _disposed;

        // ?? Constructor ???????????????????????????????????????????????????????

        public ConversationLogger()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            // Buffer turns in memory until Bind() is called.
            _buffer = new List<ConversationTurn>();
        }

        // ?? Public properties ?????????????????????????????????????????????????

        /// <summary>The history folder path, or <see langword="null"/> if not yet bound.</summary>
        public string? HistoryFolder => _historyFolder;

        // ?? Binding to a workspace ????????????????????????????????????????????

        /// <summary>
        /// Binds the logger to a workspace, creating the history folder and
        /// flushing any buffered turns to disk.
        /// </summary>
        /// <param name="workspaceFolder">The <c>.wally/</c> workspace folder.</param>
        /// <param name="folderName">
        /// Subfolder name inside the workspace folder. Default: <c>"History"</c>.
        /// </param>
        public void Bind(string workspaceFolder, string folderName = DefaultFolderName)
        {
            lock (_lock)
            {
                if (_disposed) return;

                _historyFolder = Path.Combine(workspaceFolder, folderName);
                Directory.CreateDirectory(_historyFolder);

                _filePath = Path.Combine(_historyFolder, FileName);

                // Open writer in append mode with shared read/write access.
                var stream = new FileStream(_filePath, FileMode.Append, FileAccess.Write,
                    FileShare.ReadWrite | FileShare.Delete);
                _writer = new StreamWriter(stream) { AutoFlush = true };

                // Flush buffered turns to disk.
                if (_buffer is { Count: > 0 })
                {
                    foreach (var turn in _buffer)
                    {
                        string json = JsonSerializer.Serialize(turn, _jsonOptions);
                        _writer.WriteLine(json);
                    }
                }

                _buffer = null;
            }
        }

        /// <summary>
        /// Releases the file handle without disposing the logger.
        /// After unbinding, new turns are buffered in memory until
        /// <see cref="Bind"/> is called again.
        /// </summary>
        public void Unbind()
        {
            lock (_lock)
            {
                if (_disposed) return;

                _writer?.Dispose();
                _writer = null;
                _filePath = null;
                _historyFolder = null;

                // Re-create the in-memory buffer so recording can continue.
                _buffer = new List<ConversationTurn>();
            }
        }

        // ?? Recording ????????????????????????????????????????????????????????

        /// <summary>
        /// Records a completed LLM exchange. Appends one JSON line to the file.
        /// Thread-safe. If not yet bound, buffers the turn in memory.
        /// </summary>
        public void RecordTurn(ConversationTurn turn)
        {
            lock (_lock)
            {
                if (_disposed) return;

                if (_writer != null)
                {
                    try
                    {
                        string json = JsonSerializer.Serialize(turn, _jsonOptions);
                        _writer.WriteLine(json);
                    }
                    catch (IOException)
                    {
                        // Disk full or I/O error — turn lost. Same behaviour as SessionLogger.
                    }
                }
                else
                {
                    _buffer?.Add(turn);
                }
            }
        }

        // ?? Reading ???????????????????????????????????????????????????????????

        /// <summary>
        /// Reads all lines from the history file using sharing flags compatible
        /// with the open writer (which holds a Write handle with ReadWrite sharing).
        /// <c>File.ReadAllLines</c> uses <c>FileShare.Read</c> internally, which
        /// conflicts with the writer and throws <see cref="IOException"/>.
        /// </summary>
        private string[] ReadAllLinesShared(string filePath)
        {
            using var stream = new FileStream(
                filePath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var lines = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) != null)
                lines.Add(line);
            return lines.ToArray();
        }

        /// <summary>
        /// Returns the last <paramref name="maxTurns"/> turns in chronological order,
        /// filtered by actor and excluding errors and mid-loop iterations.
        /// <para>
        /// When <paramref name="actorFilter"/> is non-null, only turns whose
        /// <see cref="ConversationTurn.ActorName"/> matches (case-insensitive)
        /// are returned. When <paramref name="actorFilter"/> is null, only turns
        /// with a null <c>ActorName</c> (direct mode) are returned.
        /// </para>
        /// </summary>
        /// <param name="maxTurns">Maximum number of turns to return.</param>
        /// <param name="actorFilter">
        /// Actor name to filter by. Pass <see langword="null"/> to match direct-mode
        /// turns (where <c>ActorName</c> is null).
        /// Pass a specific value like <c>"__all__"</c> is not supported — this
        /// method is scoped to same-actor matching only.
        /// </param>
        /// <returns>Up to <paramref name="maxTurns"/> turns in chronological order.</returns>
        public List<ConversationTurn> GetRecentTurns(int maxTurns, string? actorFilter = null)
        {
            lock (_lock)
            {
                if (_disposed || _filePath == null || !File.Exists(_filePath))
                    return new List<ConversationTurn>();

                // Flush the writer so we can read what we've written.
                _writer?.Flush();
            }

            // Read outside the lock — using shared file access compatible with the writer.
            string[] lines;
            try
            {
                lines = ReadAllLinesShared(_filePath!);
            }
            catch (IOException)
            {
                return new List<ConversationTurn>();
            }

            var result = new List<ConversationTurn>();

            // Iterate in reverse to find the most recent matching turns.
            for (int i = lines.Length - 1; i >= 0 && result.Count < maxTurns; i--)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                ConversationTurn? turn;
                try
                {
                    turn = JsonSerializer.Deserialize<ConversationTurn>(lines[i], _jsonOptions);
                }
                catch
                {
                    continue; // Corrupt line — skip.
                }

                if (turn == null) continue;

                // Skip errors.
                if (turn.IsError) continue;

                // Skip mid-loop iterations (only first iteration / non-loop turns).
                if (turn.Iteration > 0) continue;

                // Actor filter: match same-actor (case-insensitive) or both null (direct mode).
                if (actorFilter == null)
                {
                    if (turn.ActorName != null) continue;
                }
                else
                {
                    if (!string.Equals(turn.ActorName, actorFilter, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                result.Add(turn);
            }

            // Reverse to restore chronological order.
            result.Reverse();
            return result;
        }

        /// <summary>
        /// Returns all turns in chronological order. Lines that fail to
        /// deserialise are silently skipped (standard JSONL resilience).
        /// </summary>
        public List<ConversationTurn> GetAllTurns()
        {
            lock (_lock)
            {
                if (_disposed || _filePath == null || !File.Exists(_filePath))
                    return new List<ConversationTurn>();

                _writer?.Flush();
            }

            string[] lines;
            try
            {
                lines = ReadAllLinesShared(_filePath!);
            }
            catch (IOException)
            {
                return new List<ConversationTurn>();
            }

            var result = new List<ConversationTurn>(lines.Length);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var turn = JsonSerializer.Deserialize<ConversationTurn>(line, _jsonOptions);
                    if (turn != null)
                        result.Add(turn);
                }
                catch
                {
                    // Corrupt line — skip.
                }
            }

            return result;
        }

        /// <summary>
        /// Deletes the history file and opens a fresh writer.
        /// Safe to call while bound.
        /// </summary>
        public void ClearHistory()
        {
            lock (_lock)
            {
                if (_disposed) return;

                if (_filePath != null)
                {
                    _writer?.Dispose();
                    _writer = null;

                    try
                    {
                        if (File.Exists(_filePath))
                            File.Delete(_filePath);
                    }
                    catch (IOException)
                    {
                        // Best effort — file may be locked by another process.
                    }

                    // Reopen writer on a fresh file.
                    var stream = new FileStream(_filePath, FileMode.Append, FileAccess.Write,
                        FileShare.ReadWrite | FileShare.Delete);
                    _writer = new StreamWriter(stream) { AutoFlush = true };
                }

                // Also clear any in-memory buffer.
                _buffer?.Clear();
            }
        }

        // ?? History formatting ????????????????????????????????????????????????

        /// <summary>
        /// Formats a list of conversation turns into a markdown block suitable
        /// for injection into the enriched prompt between Documentation Context
        /// and the user prompt.
        /// <para>
        /// Individual responses are truncated to <paramref name="maxCharsPerResponse"/>.
        /// The total block is capped at <paramref name="maxTotalChars"/> by dropping
        /// the oldest turns first.
        /// </para>
        /// </summary>
        /// <param name="turns">Turns in chronological order.</param>
        /// <param name="maxCharsPerResponse">Max chars per response (default 2000).</param>
        /// <param name="maxTotalChars">Max total block chars (default 8000).</param>
        /// <returns>
        /// A formatted markdown string, or <see langword="null"/> if
        /// <paramref name="turns"/> is empty.
        /// </returns>
        public static string? FormatHistoryBlock(
            List<ConversationTurn> turns,
            int maxCharsPerResponse = MaxResponseCharsPerTurn,
            int maxTotalChars = MaxTotalHistoryChars)
        {
            if (turns == null || turns.Count == 0)
                return null;

            // Build individual turn blocks, then trim from the front if over budget.
            var turnBlocks = new List<string>(turns.Count);
            for (int i = 0; i < turns.Count; i++)
            {
                var turn = turns[i];
                string response = turn.Response;
                if (response.Length > maxCharsPerResponse)
                    response = response[..maxCharsPerResponse] + "\u2026[truncated]";

                var sb = new StringBuilder();
                sb.AppendLine($"### Turn {i + 1}");
                sb.AppendLine($"**Prompt:** {turn.Prompt}");
                sb.AppendLine($"**Response:** {response}");
                turnBlocks.Add(sb.ToString());
            }

            // Header + footer overhead.
            const string header =
                "## Conversation History\n" +
                "The following exchanges occurred earlier in this session.\n" +
                "Use them for context but focus on the current prompt.\n\n";
            const string footer = "---\n";

            int overhead = header.Length + footer.Length;

            // Drop oldest turns until the total fits within budget.
            int startIndex = 0;
            int totalChars = overhead + turnBlocks.Sum(b => b.Length);
            while (totalChars > maxTotalChars && startIndex < turnBlocks.Count - 1)
            {
                totalChars -= turnBlocks[startIndex].Length;
                startIndex++;
            }

            // If even a single turn exceeds the budget, return it anyway (best effort).
            var result = new StringBuilder(totalChars);
            result.Append(header);
            for (int i = startIndex; i < turnBlocks.Count; i++)
                result.Append(turnBlocks[i]);
            result.Append(footer);

            return result.ToString();
        }

        // ?? Dispose ???????????????????????????????????????????????????????????

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;

                _writer?.Dispose();
            }
        }
    }
}
