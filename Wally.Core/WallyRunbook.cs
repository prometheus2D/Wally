using System;
using System.Collections.Generic;
using System.IO;

namespace Wally.Core
{
    /// <summary>
    /// A runbook loaded from a <c>.wrb</c> (Wally Runbook) file.
    /// <para>
    /// <b>Simple format</b> — one Wally command per line. Lines starting with
    /// <c>#</c> are comments. Blank lines are ignored.
    /// </para>
    /// <para>
    /// <b>Script format (WallyScript)</b> — detected automatically when the file
    /// contains WallyScript keywords. Script execution is not yet implemented;
    /// the format flag is stored so future phases can act on it.
    /// </para>
    /// </summary>
    public class WallyRunbook
    {
        /// <summary>Name derived from filename without extension.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Description extracted from the first non-directive comment line, or empty.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Absolute path to the <c>.wrb</c> file on disk.</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// <c>"simple"</c> or <c>"script"</c>.
        /// Detected automatically by <see cref="LoadFromFile"/>.
        /// Script execution is reserved for a future phase.
        /// </summary>
        public string Format { get; set; } = "simple";

        /// <summary>Parsed command lines (non-comment, non-blank, trimmed).</summary>
        public List<string> Commands { get; set; } = new();

        /// <summary>
        /// The full raw source text. Always populated regardless of format.
        /// Kept for editor display and future script execution.
        /// </summary>
        public string RawSource { get; set; } = string.Empty;

        /// <summary>
        /// When <see langword="false"/> this runbook is skipped during workspace load.
        /// </summary>
        public bool Enabled { get; set; } = true;

        // ?? Format detection ??????????????????????????????????????????????

        /// <summary>
        /// Keywords that, when they appear at the start of a non-comment line,
        /// indicate the runbook uses the WallyScript / brace-block format rather
        /// than the legacy simple (one command per line) format.
        /// <para>
        /// Phase-1/2 keywords: <c>shell</c>, <c>loop</c>, <c>call</c>, <c>open</c>
        /// (brace-delimited block syntax — see RunbookSyntaxProposal).
        /// </para>
        /// <para>
        /// Reserved future keywords: <c>if</c>, <c>while</c>, <c>foreach</c>,
        /// <c>function</c>, <c>parallel</c>, <c>pipeline</c>, <c>try</c>, <c>retry</c>.
        /// </para>
        /// </summary>
        private static readonly HashSet<string> ScriptKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            // Phase 1 – shell integration
            "shell",
            // Phase 2 – brace-delimited block syntax
            "loop", "call", "open",
            // Reserved future keywords (not yet implemented)
            "if", "while", "foreach", "function", "parallel", "pipeline", "try", "retry"
        };

        /// <summary>
        /// Returns <see langword="true"/> when any line in the source looks like a
        /// WallyScript statement (not a simple command or comment).
        /// Detection is line-prefix based and requires no parsing.
        /// </summary>
        public static bool DetectScriptFormat(string source)
        {
            foreach (string raw in source.Split('\n'))
            {
                string line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;

                if (line.StartsWith('$')) return true;

                int spaceIdx = line.IndexOf(' ');
                string word  = spaceIdx > 0 ? line[..spaceIdx] : line;
                if (ScriptKeywords.Contains(word)) return true;
            }
            return false;
        }

        // ?? Loading ????????????????????????????????????????????????????????

        /// <summary>Parses a single <c>.wrb</c> file.</summary>
        public static WallyRunbook LoadFromFile(string filePath)
        {
            string rawSource = File.ReadAllText(filePath);

            var rb = new WallyRunbook
            {
                Name      = Path.GetFileNameWithoutExtension(filePath),
                FilePath  = Path.GetFullPath(filePath),
                RawSource = rawSource
            };

            bool descriptionSet = false;

            foreach (string rawLine in rawSource.Split('\n'))
            {
                string line = rawLine.Trim().TrimEnd('\r');
                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith('#'))
                {
                    string directive = line.TrimStart('#').Trim();

                    if (directive.StartsWith("enabled:", StringComparison.OrdinalIgnoreCase))
                    {
                        string val = directive["enabled:".Length..].Trim();
                        rb.Enabled = !string.Equals(val, "false", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    if (!descriptionSet)
                    {
                        if (!directive.Contains(':'))
                        {
                            rb.Description = directive;
                            descriptionSet = true;
                        }
                    }
                    continue;
                }

                rb.Commands.Add(line);
            }

            // Tag format for future scripting support — no AST parsing yet.
            if (DetectScriptFormat(rawSource))
                rb.Format = "script";

            return rb;
        }

        /// <summary>
        /// Loads all <c>*.wrb</c> files from <paramref name="folder"/>.
        /// Skips files that fail to parse and runbooks whose
        /// <see cref="Enabled"/> flag is <see langword="false"/>.
        /// </summary>
        public static List<WallyRunbook> LoadFromFolder(string folder)
        {
            var runbooks = new List<WallyRunbook>();
            if (!Directory.Exists(folder)) return runbooks;

            foreach (string file in Directory.GetFiles(folder, "*.wrb"))
            {
                try
                {
                    var rb = LoadFromFile(file);
                    if (rb.Enabled)
                        runbooks.Add(rb);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(
                        $"Warning: Failed to load runbook '{file}': {ex.Message}");
                }
            }

            return runbooks;
        }
    }
}
