using System;
using System.Collections.Generic;
using System.Linq;

namespace Wally.Core.Scripting
{
    // ?? Statement hierarchy ???????????????????????????????????????????????????

    /// <summary>Base for every node in a parsed runbook AST.</summary>
    public abstract class RunbookStatement
    {
        /// <summary>1-based source line number where this statement begins.</summary>
        public int LineNumber { get; init; }
    }

    /// <summary>
    /// A <c>loop { … }</c> definition.
    /// Setting <see cref="CurrentLoop"/> on the enclosing scope frame is the only
    /// effect of reaching this node during execution — the body does not run until
    /// a <see cref="RunbookCall"/> drives it.
    /// </summary>
    public sealed class RunbookLoop : RunbookStatement
    {
        /// <summary>Statements inside the braces — always runs on each call.</summary>
        public IReadOnlyList<RunbookStatement> Body { get; init; } = Array.Empty<RunbookStatement>();

        /// <summary>
        /// <see langword="true"/> when the body contains exactly one <see cref="RunbookOpen"/>.
        /// </summary>
        public bool HasOpenSlot => Body.Any(s => s is RunbookOpen);
    }

    /// <summary>
    /// A <c>call</c> or <c>call { … }</c> statement.
    /// <para>
    /// <see cref="ShotBody"/> is <see langword="null"/> for a bare <c>call</c>
    /// (no braces).  When present, its contents are injected at the
    /// <see cref="RunbookOpen"/> slot of the current loop.
    /// </para>
    /// </summary>
    public sealed class RunbookCall : RunbookStatement
    {
        /// <summary>
        /// The shot body to inject at <c>open</c>, or <see langword="null"/> for a
        /// bare <c>call</c>.
        /// </summary>
        public IReadOnlyList<RunbookStatement>? ShotBody { get; init; }
    }

    /// <summary>A <c>shell &lt;command&gt;</c> line — spawns an OS process.</summary>
    public sealed class RunbookShell : RunbookStatement
    {
        /// <summary>The remainder of the line after the <c>shell</c> keyword.</summary>
        public string Command { get; init; } = string.Empty;
    }

    /// <summary>A regular Wally command line (e.g. <c>run "…" -a Actor</c>).</summary>
    public sealed class RunbookCommand : RunbookStatement
    {
        /// <summary>The full command text as it appears in the source.</summary>
        public string Line { get; init; } = string.Empty;
    }

    /// <summary>
    /// The bare <c>open</c> keyword inside a <c>loop { }</c> body.
    /// Marks the injection slot where a <see cref="RunbookCall"/>'s shot body runs.
    /// </summary>
    public sealed class RunbookOpen : RunbookStatement { }

    // ?? Parser ????????????????????????????????????????????????????????????????

    /// <summary>
    /// Converts runbook source text into a flat, ordered list of
    /// <see cref="RunbookStatement"/> nodes.
    /// <para>
    /// Grammar (informal):
    /// <code>
    /// stmt    := shell_stmt | loop_stmt | call_stmt | open_stmt | cmd_stmt
    /// shell   := "shell" &lt;rest-of-line&gt;
    /// loop    := "loop" "{" &lt;stmts&gt; "}"
    /// call    := "call" [ "{" &lt;stmts&gt; "}" ]
    /// open    := "open"
    /// cmd     := &lt;any-other-line&gt;
    /// </code>
    /// <c>{</c> must appear on the same line as its keyword.
    /// <c>}</c> must be the only non-whitespace token on its line.
    /// Comments (<c>#</c>) and blank lines are ignored everywhere.
    /// </para>
    /// </summary>
    public static class RunbookParser
    {
        /// <summary>
        /// Parses <paramref name="source"/> and returns the top-level statement list.
        /// Throws <see cref="RunbookParseException"/> on any syntax error.
        /// </summary>
        public static List<RunbookStatement> Parse(string source)
        {
            var lines = SplitLines(source);
            int pos = 0;
            var stmts = ParseBlock(lines, ref pos, context: ParseContext.TopLevel);
            return stmts;
        }

        // ?? Internal helpers ??????????????????????????????????????????????????

        private enum ParseContext { TopLevel, LoopBody, ShotBody }

        private static List<RunbookStatement> ParseBlock(
            (int lineNo, string text)[] lines,
            ref int pos,
            ParseContext context)
        {
            var result = new List<RunbookStatement>();

            while (pos < lines.Length)
            {
                var (lineNo, raw) = lines[pos];
                string trimmed = raw.Trim();

                // skip blank lines and comments
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                {
                    pos++;
                    continue;
                }

                // closing brace — ends a block
                if (trimmed == "}")
                {
                    if (context == ParseContext.TopLevel)
                        throw new RunbookParseException(lineNo,
                            "Unexpected '}' — no matching opening brace.");
                    pos++; // consume the '}'
                    return result;
                }

                // tokenise first word
                int spaceIdx = trimmed.IndexOf(' ');
                string keyword = spaceIdx > 0 ? trimmed[..spaceIdx] : trimmed;

                switch (keyword.ToLowerInvariant())
                {
                    case "shell":
                    {
                        string cmd = spaceIdx > 0 ? trimmed[(spaceIdx + 1)..].TrimStart() : string.Empty;
                        result.Add(new RunbookShell { LineNumber = lineNo, Command = cmd });
                        pos++;
                        break;
                    }

                    case "loop":
                    {
                        if (!trimmed.EndsWith('{'))
                            throw new RunbookParseException(lineNo,
                                "Expected '{' at end of 'loop' line (e.g. 'loop {').");
                        pos++; // consume the 'loop {' line
                        var body = ParseBlock(lines, ref pos, ParseContext.LoopBody);
                        ValidateLoopBody(body, lineNo);
                        result.Add(new RunbookLoop { LineNumber = lineNo, Body = body });
                        break;
                    }

                    case "call":
                    {
                        if (trimmed.EndsWith('{'))
                        {
                            pos++; // consume the 'call {' line
                            var shotBody = ParseBlock(lines, ref pos, ParseContext.ShotBody);
                            ValidateShotBody(shotBody, lineNo);
                            result.Add(new RunbookCall { LineNumber = lineNo, ShotBody = shotBody });
                        }
                        else
                        {
                            // bare call — no shot body
                            result.Add(new RunbookCall { LineNumber = lineNo, ShotBody = null });
                            pos++;
                        }
                        break;
                    }

                    case "open":
                    {
                        if (context == ParseContext.TopLevel)
                            throw new RunbookParseException(lineNo,
                                "'open' is only valid inside a 'loop { }' body.");
                        if (context == ParseContext.ShotBody)
                            throw new RunbookParseException(lineNo,
                                "'open' is not valid inside a 'call { }' shot body — only loop bodies have a shot slot.");
                        result.Add(new RunbookOpen { LineNumber = lineNo });
                        pos++;
                        break;
                    }

                    default:
                    {
                        result.Add(new RunbookCommand { LineNumber = lineNo, Line = trimmed });
                        pos++;
                        break;
                    }
                }
            }

            // End of input
            if (context != ParseContext.TopLevel)
                throw new RunbookParseException(
                    lines.Length > 0 ? lines[^1].lineNo : 0,
                    "Unexpected end of file — unmatched '{'.");

            return result;
        }

        private static void ValidateLoopBody(List<RunbookStatement> body, int loopLineNo)
        {
            int openCount = body.Count(s => s is RunbookOpen);
            if (openCount > 1)
            {
                int secondOpenLine = body.OfType<RunbookOpen>().Skip(1).First().LineNumber;
                throw new RunbookParseException(secondOpenLine,
                    "A loop body may contain at most one 'open'. " +
                    $"(First 'loop {{' is at line {loopLineNo}.)");
            }
        }

        private static void ValidateShotBody(List<RunbookStatement> body, int callLineNo)
        {
            // open inside a shot body is already caught by ParseBlock (context == ShotBody)
            // — nothing more to validate here for now.
        }

        // ?? Source pre-processing ?????????????????????????????????????????????

        private static (int lineNo, string text)[] SplitLines(string source)
        {
            string[] raw = source.Split('\n');
            var result = new (int, string)[raw.Length];
            for (int i = 0; i < raw.Length; i++)
                result[i] = (i + 1, raw[i].TrimEnd('\r'));
            return result;
        }
    }

    // ?? Parse exception ???????????????????????????????????????????????????????

    /// <summary>
    /// Thrown by <see cref="RunbookParser"/> when it encounters a syntax error.
    /// </summary>
    public sealed class RunbookParseException : Exception
    {
        /// <summary>1-based line number where the error was detected.</summary>
        public int LineNumber { get; }

        public RunbookParseException(int lineNumber, string message)
            : base($"Line {lineNumber}: {message}")
        {
            LineNumber = lineNumber;
        }
    }
}
