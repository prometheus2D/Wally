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
    /// <h3>Block format in LLM response:</h3>
    /// <code>
    /// ```action
    /// name: write_file
    /// path: Projects/MyProject/Requirements/auth.md
    /// content: |
    ///   # Auth Requirements
    ///   ...
    /// ```
    /// </code>
    /// <h3>Supported built-in action names:</h3>
    /// <list type="bullet">
    ///   <item><c>write_file</c> — write <c>content</c> to <c>path</c> (relative to WorkSource).</item>
    ///   <item><c>read_file</c>  — read <c>path</c> and return its content (inserted into next prompt).</item>
    ///   <item><c>append_file</c> — append <c>content</c> to <c>path</c>.</item>
    ///   <item><c>list_files</c> — list files in <c>directory</c> (relative to WorkSource).</item>
    ///   <item><c>delete_file</c> — delete <c>path</c> (mutating).</item>
    /// </list>
    /// </summary>
    public sealed class ActionDispatcher
    {
        // ?? Regex: matches ```action ... ``` blocks ???????????????????????????
        // The block starts with ``` followed immediately by "action" (case-insensitive)
        // and ends with a ``` on its own line.
        private static readonly Regex _blockRx = new Regex(
            @"```action\s*\r?\n(?<body>.*?)```",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ?? Dependencies ?????????????????????????????????????????????????????

        private readonly Actor   _actor;
        private readonly string  _workSource;
        private readonly bool    _canMakeChanges;
        private readonly Action<string>? _logWarning;

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

                // ?? Allow-list check ????????????????????????????????????????
                var declared = FindDeclaredAction(actionName!);
                if (declared == null)
                {
                    Warn($"Action '{actionName}' is not declared in actor '{_actor.Name}' — skipped.");
                    SkippedCount++;
                    sb.AppendLine($"- **{actionName}**: SKIPPED (not declared for this actor)");
                    continue;
                }

                // ?? Mutability check ????????????????????????????????????????
                if (declared.IsMutating && !_canMakeChanges)
                {
                    Warn($"Action '{actionName}' is mutating but wrapper is read-only — skipped.");
                    SkippedCount++;
                    sb.AppendLine($"- **{actionName}**: SKIPPED (mutating action requires Agent mode)");
                    continue;
                }

                // ?? Path-pattern check ??????????????????????????????????????
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

                // ?? Dispatch ?????????????????????????????????????????????????
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
                "write_file"  => ActionWriteFile(fields),
                "append_file" => ActionAppendFile(fields),
                "read_file"   => ActionReadFile(fields),
                "list_files"  => ActionListFiles(fields),
                "delete_file" => ActionDeleteFile(fields),
                _             => $"WARNING: No built-in handler for '{actionName}'."
            };
        }

        private string ActionWriteFile(Dictionary<string, string> fields)
        {
            string path    = fields.GetValueOrDefault("path", "");
            string content = fields.GetValueOrDefault("content", "");
            string full    = ResolveSafe(path);
            if (full == null!) return $"ERROR: path '{path}' escapes WorkSource — rejected.";
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content, Encoding.UTF8);
            return $"OK — wrote {content.Length} chars to `{path}`";
        }

        private string ActionAppendFile(Dictionary<string, string> fields)
        {
            string path    = fields.GetValueOrDefault("path", "");
            string content = fields.GetValueOrDefault("content", "");
            string full    = ResolveSafe(path);
            if (full == null!) return $"ERROR: path '{path}' escapes WorkSource — rejected.";
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.AppendAllText(full, content, Encoding.UTF8);
            return $"OK — appended {content.Length} chars to `{path}`";
        }

        private string ActionReadFile(Dictionary<string, string> fields)
        {
            string path = fields.GetValueOrDefault("path", "");
            string full = ResolveSafe(path);
            if (full == null!) return $"ERROR: path '{path}' escapes WorkSource — rejected.";
            if (!File.Exists(full)) return $"ERROR: file not found — `{path}`";
            string content = File.ReadAllText(full, Encoding.UTF8);
            ReadResults.Add($"### File: `{path}`\n```\n{content}\n```");
            return $"OK — read {content.Length} chars from `{path}`";
        }

        private string ActionListFiles(Dictionary<string, string> fields)
        {
            string dir  = fields.GetValueOrDefault("directory", ".");
            string full = ResolveSafe(dir);
            if (full == null!) return $"ERROR: directory '{dir}' escapes WorkSource — rejected.";
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
            string path = fields.GetValueOrDefault("path", "");
            string full = ResolveSafe(path);
            if (full == null!) return $"ERROR: path '{path}' escapes WorkSource — rejected.";
            if (!File.Exists(full)) return $"WARNING: file not found — `{path}` (nothing deleted)";
            File.Delete(full);
            return $"OK — deleted `{path}`";
        }

        // ?? Private: helpers ?????????????????????????????????????????????????

        /// <summary>
        /// Resolves <paramref name="relativePath"/> against WorkSource and validates
        /// the result stays inside WorkSource (path-traversal guard).
        /// Returns <see langword="null"/> when the path escapes.
        /// </summary>
        private string? ResolveSafe(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return null;
            string full = Path.GetFullPath(Path.Combine(_workSource, relativePath));
            string root = Path.GetFullPath(_workSource).TrimEnd(Path.DirectorySeparatorChar);
            return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || string.Equals(full, root, StringComparison.OrdinalIgnoreCase)
                ? full
                : null!;
        }

        /// <summary>
        /// Parses a YAML-lite action block body into a string?string dictionary.
        /// Multi-line values use the YAML block-scalar syntax: <c>key: |</c> followed
        /// by indented lines. All other values are single-line.
        /// </summary>
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
                    // Block scalar — collect indented lines until de-indent or EOF
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
                    // Remove trailing blank lines
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
        /// Path separators are normalised to <c>/</c> before matching.
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
