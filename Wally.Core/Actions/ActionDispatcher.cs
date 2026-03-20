using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Wally.Core.Actors;

namespace Wally.Core.Actions
{
    /// <summary>
    /// Scans an LLM response for fenced <c>action</c> blocks and dispatches
    /// each block to the matching built-in handler — but only when the actor
    /// has declared that action in its <c>actions</c> list.
    /// <para>
    /// Undeclared actions are silently skipped and logged as warnings.
    /// Mutating actions are additionally rejected when the active wrapper is
    /// read-only (<c>CanMakeChanges = false</c>).
    /// </para>
    /// <h3>Supported action names (semantic + legacy aliases):</h3>
    /// <list type="bullet">
    ///   <item><c>change_code</c>       — write any file (Engineer exclusive). Alias: <c>write_file</c>.</item>
    ///   <item><c>write_document</c>    — write a Markdown document (BA, Engineer). Alias: <c>write_file</c>.</item>
    ///   <item><c>write_requirements</c>— write a Markdown requirements doc (RequirementsExtractor exclusive). Alias: <c>write_file</c>.</item>
    ///   <item><c>read_context</c>      — read any file for inspection (all actors). Alias: <c>read_file</c>.</item>
    ///   <item><c>browse_workspace</c>  — list files in a directory (all actors). Alias: <c>list_files</c>.</item>
    ///   <item><c>send_message</c>      — write a message to a target actor's Inbox/ (all actors).</item>
    ///   <item><c>write_file</c>        — legacy alias for change_code / write_document.</item>
    ///   <item><c>read_file</c>         — legacy alias for read_context.</item>
    ///   <item><c>append_file</c>       — append content to a file.</item>
    ///   <item><c>list_files</c>        — legacy alias for browse_workspace.</item>
    ///   <item><c>delete_file</c>       — delete a file (mutating).</item>
    /// </list>
    /// </summary>
    public sealed class ActionDispatcher
    {
        // ?? Regex: matches ```action ... ``` blocks ???????????????????????????
        private static readonly Regex _blockRx = new Regex(
            @"```action\s*\r?\n(?<body>.*?)```",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ?? Dependencies ??????????????????????????????????????????????????????

        private readonly Actor            _actor;
        private readonly string           _workSource;
        private readonly bool             _canMakeChanges;
        private readonly Action<string>?  _logWarning;

        /// <summary>
        /// The workspace's loaded actors, injected so <c>send_message</c> can
        /// validate the target actor exists before writing to its Inbox/.
        /// When <see langword="null"/> the target-validation step is skipped
        /// (permissive mode — used in unit tests without a full workspace).
        /// </summary>
        public IReadOnlyList<Actor>? LoadedActors { get; set; }

        /// <summary>
        /// Collected read-results that can be prepended to the next prompt so
        /// the LLM sees files it asked to read.
        /// </summary>
        public List<string> ReadResults { get; } = new();

        /// <summary>
        /// Number of action blocks dispatched successfully in the last
        /// <see cref="Dispatch"/> call.
        /// </summary>
        public int DispatchedCount { get; private set; }

        /// <summary>
        /// Number of action blocks skipped (undeclared, forbidden, or invalid)
        /// in the last <see cref="Dispatch"/> call.
        /// </summary>
        public int SkippedCount { get; private set; }

        // ?? Constructor ???????????????????????????????????????????????????????

        /// <param name="actor">The actor whose declared actions are the allow-list.</param>
        /// <param name="workSource">Absolute path to the workspace WorkSource root.</param>
        /// <param name="canMakeChanges">
        /// Whether the active wrapper permits file mutations (Agent mode = true).
        /// </param>
        /// <param name="logWarning">Optional callback for skipped-action warnings.</param>
        public ActionDispatcher(
            Actor actor,
            string workSource,
            bool canMakeChanges,
            Action<string>? logWarning = null)
        {
            _actor          = actor;
            _workSource     = workSource;
            _canMakeChanges = canMakeChanges;
            _logWarning     = logWarning;
        }

        // ?? Public API ????????????????????????????????????????????????????????

        /// <summary>
        /// Scans <paramref name="llmResponse"/> for action blocks and dispatches
        /// each one that passes the allow-list and mutability checks.
        /// </summary>
        /// <returns>
        /// A summary string listing every action that was dispatched or skipped,
        /// or <see langword="null"/> when no action blocks were found.
        /// </returns>
        public string? Dispatch(string llmResponse)
        {
            DispatchedCount = 0;
            SkippedCount    = 0;
            ReadResults.Clear();

            var matches = _blockRx.Matches(llmResponse);
            if (matches.Count == 0) return null;

            var sb = new StringBuilder();
            sb.AppendLine("## Action Results");

            foreach (Match m in matches)
            {
                string body = m.Groups["body"].Value;
                var fields  = ParseBlock(body);

                if (!fields.TryGetValue("name", out string? actionName) ||
                    string.IsNullOrWhiteSpace(actionName))
                {
                    Warn("Action block missing 'name' field — skipped.");
                    SkippedCount++;
                    continue;
                }

                // ?? Allow-list check ?????????????????????????????????????????
                var declared = FindDeclaredAction(actionName!);
                if (declared == null)
                {
                    Warn($"Action '{actionName}' is not declared in actor '{_actor.Name}' — skipped.");
                    SkippedCount++;
                    sb.AppendLine($"- **{actionName}**: SKIPPED (not declared for this actor)");
                    continue;
                }

                // ?? Mutability check ?????????????????????????????????????????
                if (declared.IsMutating && !_canMakeChanges)
                {
                    Warn($"Action '{actionName}' is mutating but wrapper is read-only — skipped.");
                    SkippedCount++;
                    sb.AppendLine($"- **{actionName}**: SKIPPED (mutating action requires Agent mode)");
                    continue;
                }

                // ?? Path-pattern check ???????????????????????????????????????
                if (!string.IsNullOrWhiteSpace(declared.PathPattern) &&
                    fields.TryGetValue("path", out string? path) &&
                    !string.IsNullOrWhiteSpace(path))
                {
                    if (!GlobMatch(declared.PathPattern!, path!))
                    {
                        Warn($"Action '{actionName}' path '{path}' does not match pattern '{declared.PathPattern}' — skipped.");
                        SkippedCount++;
                        sb.AppendLine($"- **{actionName}** `{path}`: SKIPPED (path not permitted by actor policy)");
                        continue;
                    }
                }

                // ?? Validate required parameters ?????????????????????????????
                bool valid = true;
                foreach (var param in declared.Parameters)
                {
                    if (param.Required && !fields.ContainsKey(param.Name))
                    {
                        Warn($"Action '{actionName}' missing required parameter '{param.Name}' — skipped.");
                        SkippedCount++;
                        sb.AppendLine($"- **{actionName}**: SKIPPED (missing required parameter '{param.Name}')");
                        valid = false;
                        break;
                    }
                }
                if (!valid) continue;

                // ?? Dispatch ??????????????????????????????????????????????????
                string result = DispatchBuiltIn(actionName!, fields);
                DispatchedCount++;
                sb.AppendLine($"- **{actionName}**: {result}");
            }

            return sb.ToString().TrimEnd();
        }

        // ?? Private: dispatch built-in actions ????????????????????????????????

        private string DispatchBuiltIn(string actionName, Dictionary<string, string> fields)
        {
            return actionName.ToLowerInvariant() switch
            {
                // ?? Semantic names (role-scoped) ??????????????????????????????
                "change_code"        => ActionWriteFile(fields),      // Engineer: write any file
                "write_document"     => ActionWriteFile(fields),      // Engineer / BA: write .md doc
                "write_requirements" => ActionWriteFile(fields),      // RequirementsExtractor: write .md req
                "read_context"       => ActionReadFile(fields),       // All actors: read for inspection
                "browse_workspace"   => ActionListFiles(fields),      // All actors: list directory
                "send_message"       => ActionSendMessage(fields),    // All actors: mailbox handoff

                // ?? Legacy primitive names (kept for back-compat) ?????????????
                "write_file"  => ActionWriteFile(fields),
                "append_file" => ActionAppendFile(fields),
                "read_file"   => ActionReadFile(fields),
                "list_files"  => ActionListFiles(fields),
                "delete_file" => ActionDeleteFile(fields),

                _             => $"WARNING: No built-in handler for '{actionName}'."
            };
        }

        // ?? send_message ??????????????????????????????????????????????????????

        /// <summary>
        /// Writes a WallyMessage file into the target actor's <c>Inbox/</c> folder.
        /// <para>
        /// The message file uses YAML front-matter followed by a free-text Markdown body:
        /// <code>
        /// ---
        /// from: Engineer
        /// to:   BusinessAnalyst
        /// subject: FeasibilityCheck
        /// replyTo: Engineer
        /// correlationId: &lt;guid&gt;
        /// ***
        /// [body]
        /// </code>
        /// The file is named <c>{timestamp}-{from}-{subject}.md</c> and written to
        /// <c>.wally/Actors/{to}/Inbox/</c> relative to the workspace folder
        /// (one level below WorkSource).
        /// </para>
        /// </summary>
        private string ActionSendMessage(Dictionary<string, string> fields)
        {
            string to      = fields.GetValueOrDefault("to",      "").Trim();
            string subject = fields.GetValueOrDefault("subject", "").Trim();
            string body    = fields.GetValueOrDefault("body",    "").Trim();
            string replyTo = fields.GetValueOrDefault("replyTo", _actor.Name).Trim();

            if (string.IsNullOrWhiteSpace(to))
                return "ERROR: send_message missing required parameter 'to'.";
            if (string.IsNullOrWhiteSpace(subject))
                return "ERROR: send_message missing required parameter 'subject'.";

            // ?? Validate target actor exists ?????????????????????????????????
            if (LoadedActors != null)
            {
                bool targetExists = false;
                foreach (var a in LoadedActors)
                {
                    if (string.Equals(a.Name, to, StringComparison.OrdinalIgnoreCase))
                    {
                        targetExists = true;
                        break;
                    }
                }
                if (!targetExists)
                    return $"ERROR: send_message target actor '{to}' is not loaded in this workspace — message not written.";
            }

            // ?? Resolve target Inbox path ????????????????????????????????????
            // WorkSource is the repo root; workspace folder is WorkSource/.wally
            string workspaceFolder = Path.Combine(_workSource, ".wally");
            string inboxDir = Path.Combine(workspaceFolder, "Actors", to, "Inbox");

            if (!Directory.Exists(inboxDir))
                return $"ERROR: Inbox folder for actor '{to}' not found at '{inboxDir}' — run 'wally setup' to initialise mailbox folders.";

            // ?? Build file name ??????????????????????????????????????????????
            string correlationId = Guid.NewGuid().ToString("N")[..8]; // short 8-char prefix
            string timestamp     = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
            string safeSubject   = Regex.Replace(subject, @"[^\w\-]", "-");
            string fileName      = $"{timestamp}-{_actor.Name}-{safeSubject}.md";
            string filePath      = Path.Combine(inboxDir, fileName);

            // ?? Write message file ???????????????????????????????????????????
            var content = new StringBuilder();
            content.AppendLine("---");
            content.AppendLine($"from: {_actor.Name}");
            content.AppendLine($"to: {to}");
            content.AppendLine($"subject: {subject}");
            content.AppendLine($"replyTo: {replyTo}");
            content.AppendLine($"correlationId: {correlationId}");
            content.AppendLine("---");
            content.AppendLine();
            content.Append(body);

            File.WriteAllText(filePath, content.ToString(), Encoding.UTF8);

            ReadResults.Add(
                $"### Message queued for `{to}`\n" +
                $"- File: `{Path.GetRelativePath(_workSource, filePath)}`\n" +
                $"- correlationId: `{correlationId}`\n" +
                $"- Subject: `{subject}`");

            return $"OK — message queued for '{to}' (correlationId: {correlationId}, file: {fileName})";
        }

        // ?? File I/O primitives ???????????????????????????????????????????????

        private string ActionWriteFile(Dictionary<string, string> fields)
        {
            string path    = fields.GetValueOrDefault("path", "");
            string content = fields.GetValueOrDefault("content", "");
            string? full   = ResolveSafe(path);
            if (full == null) return $"ERROR: path '{path}' escapes WorkSource — rejected.";
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content, Encoding.UTF8);
            return $"OK — wrote {content.Length} chars to `{path}`";
        }

        private string ActionAppendFile(Dictionary<string, string> fields)
        {
            string path    = fields.GetValueOrDefault("path", "");
            string content = fields.GetValueOrDefault("content", "");
            string? full   = ResolveSafe(path);
            if (full == null) return $"ERROR: path '{path}' escapes WorkSource — rejected.";
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.AppendAllText(full, content, Encoding.UTF8);
            return $"OK — appended {content.Length} chars to `{path}`";
        }

        private string ActionReadFile(Dictionary<string, string> fields)
        {
            string path  = fields.GetValueOrDefault("path", "");
            string? full = ResolveSafe(path);
            if (full == null) return $"ERROR: path '{path}' escapes WorkSource — rejected.";
            if (!File.Exists(full)) return $"ERROR: file not found — `{path}`";
            string content = File.ReadAllText(full, Encoding.UTF8);
            ReadResults.Add($"### File: `{path}`\n```\n{content}\n```");
            return $"OK — read {content.Length} chars from `{path}`";
        }

        private string ActionListFiles(Dictionary<string, string> fields)
        {
            string dir   = fields.GetValueOrDefault("directory", ".");
            string? full = ResolveSafe(dir);
            if (full == null) return $"ERROR: directory '{dir}' escapes WorkSource — rejected.";
            if (!Directory.Exists(full)) return $"ERROR: directory not found — `{dir}`";
            var files = Directory.GetFiles(full, "*", SearchOption.AllDirectories);
            var lines = new StringBuilder();
            foreach (string f in files)
                lines.AppendLine(Path.GetRelativePath(_workSource, f));
            string listing = lines.ToString().TrimEnd();
            ReadResults.Add($"### Directory listing: `{dir}`\n```\n{listing}\n```");
            return $"OK — listed {files.Length} file(s) in `{dir}`";
        }

        private string ActionDeleteFile(Dictionary<string, string> fields)
        {
            string path  = fields.GetValueOrDefault("path", "");
            string? full = ResolveSafe(path);
            if (full == null) return $"ERROR: path '{path}' escapes WorkSource — rejected.";
            if (!File.Exists(full)) return $"WARNING: file not found — `{path}` (nothing deleted)";
            File.Delete(full);
            return $"OK — deleted `{path}`";
        }

        // ?? Private: helpers ??????????????????????????????????????????????????

        private string? ResolveSafe(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return null;
            string full = Path.GetFullPath(Path.Combine(_workSource, relativePath));
            string root = Path.GetFullPath(_workSource).TrimEnd(Path.DirectorySeparatorChar);
            return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || string.Equals(full, root, StringComparison.OrdinalIgnoreCase)
                ? full
                : null;
        }

        private static Dictionary<string, string> ParseBlock(string body)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var lines  = body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            int i      = 0;

            while (i < lines.Length)
            {
                string line = lines[i];
                int colon   = line.IndexOf(':');
                if (colon <= 0) { i++; continue; }

                string key   = line[..colon].Trim();
                string value = line[(colon + 1)..].Trim();

                if (value == "|")
                {
                    var blockLines = new List<string>();
                    i++;
                    int indent = -1;
                    while (i < lines.Length)
                    {
                        string bl = lines[i];
                        if (string.IsNullOrWhiteSpace(bl)) { blockLines.Add(""); i++; continue; }
                        int blIndent = bl.Length - bl.TrimStart().Length;
                        if (indent < 0) indent = blIndent;
                        if (blIndent < indent) break;
                        blockLines.Add(bl.Length >= indent ? bl[indent..] : bl);
                        i++;
                    }
                    while (blockLines.Count > 0 && string.IsNullOrWhiteSpace(blockLines[^1]))
                        blockLines.RemoveAt(blockLines.Count - 1);
                    result[key] = string.Join("\n", blockLines);
                }
                else
                {
                    result[key] = value;
                    i++;
                }
            }

            return result;
        }

        private ActorAction? FindDeclaredAction(string name) =>
            _actor.Actions.Find(a =>
                string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

        private void Warn(string message) => _logWarning?.Invoke(message);

        /// <summary>
        /// Minimal glob matcher supporting <c>*</c> (within a segment) and
        /// <c>**</c> (across any number of segments).
        /// </summary>
        internal static bool GlobMatch(string pattern, string input)
        {
            string p = pattern.Replace('\\', '/');
            string s = input.Replace('\\', '/');
            return GlobMatchCore(p.AsSpan(), s.AsSpan());
        }

        private static bool GlobMatchCore(ReadOnlySpan<char> pattern, ReadOnlySpan<char> input)
        {
            while (true)
            {
                if (pattern.IsEmpty) return input.IsEmpty;
                if (pattern.StartsWith("**".AsSpan()))
                {
                    var rest = pattern.Slice(2);
                    if (rest.IsEmpty || rest[0] == '/') rest = rest.IsEmpty ? rest : rest.Slice(1);
                    if (rest.IsEmpty) return true;
                    for (int i = 0; i <= input.Length; i++)
                        if (GlobMatchCore(rest, input.Slice(i))) return true;
                    return false;
                }
                if (pattern[0] == '*')
                {
                    var rest = pattern.Slice(1);
                    for (int i = 0; i <= input.Length; i++)
                    {
                        if (i < input.Length && input[i] == '/') break;
                        if (GlobMatchCore(rest, input.Slice(i))) return true;
                    }
                    return false;
                }
                if (input.IsEmpty || pattern[0] != input[0]) return false;
                pattern = pattern.Slice(1);
                input   = input.Slice(1);
            }
        }
    }
}
