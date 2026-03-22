using System;
using System.Collections.Generic;

namespace Wally.Core.Mailbox
{
    /// <summary>
    /// Parses YAML front-matter fields from mailbox message files.
    /// <para>
    /// Message files use a simple YAML front-matter block delimited by <c>---</c>
    /// at the top. Fields extracted: <c>to</c>, <c>from</c>, <c>replyTo</c>,
    /// <c>subject</c>, <c>correlationId</c>, <c>timestamp</c>, <c>status</c>.
    /// </para>
    /// <para>
    /// This is a lightweight parser that does not depend on any YAML library.
    /// It handles the simple key-value format used by the mailbox system.
    /// </para>
    /// </summary>
    public static class MailboxHelper
    {
        /// <summary>
        /// Parses YAML front-matter from a message file's content.
        /// Returns a dictionary of field name ? value pairs (both trimmed).
        /// Returns an empty dictionary if no front-matter block is found.
        /// </summary>
        /// <param name="fileContent">The full text content of the message file.</param>
        /// <returns>
        /// Case-insensitive dictionary of front-matter fields.
        /// Common fields: <c>from</c>, <c>to</c>, <c>replyTo</c>, <c>subject</c>,
        /// <c>correlationId</c>, <c>timestamp</c>, <c>status</c>.
        /// </returns>
        public static Dictionary<string, string> ParseFrontMatter(string fileContent)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(fileContent))
                return result;

            var lines = fileContent.Split('\n');
            int lineIndex = 0;

            // Skip leading whitespace/blank lines
            while (lineIndex < lines.Length && string.IsNullOrWhiteSpace(lines[lineIndex]))
                lineIndex++;

            // Must start with ---
            if (lineIndex >= lines.Length || lines[lineIndex].Trim() != "---")
                return result;

            lineIndex++; // skip opening ---

            // Parse key: value pairs until closing ---
            while (lineIndex < lines.Length)
            {
                string line = lines[lineIndex].Trim();
                lineIndex++;

                if (line == "---")
                    break; // End of front-matter

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                int colonIndex = line.IndexOf(':');
                if (colonIndex <= 0)
                    continue;

                string key = line[..colonIndex].Trim();
                string value = line[(colonIndex + 1)..].Trim();

                if (!string.IsNullOrEmpty(key))
                    result[key] = value;
            }

            return result;
        }

        /// <summary>
        /// Extracts the message body (everything after the closing <c>---</c> of the
        /// YAML front-matter block). Returns the full content if no front-matter is found.
        /// </summary>
        /// <param name="fileContent">The full text content of the message file.</param>
        /// <returns>The message body text, trimmed.</returns>
        public static string ExtractBody(string fileContent)
        {
            if (string.IsNullOrWhiteSpace(fileContent))
                return string.Empty;

            var lines = fileContent.Split('\n');
            int lineIndex = 0;

            // Skip leading whitespace/blank lines
            while (lineIndex < lines.Length && string.IsNullOrWhiteSpace(lines[lineIndex]))
                lineIndex++;

            // Must start with ---
            if (lineIndex >= lines.Length || lines[lineIndex].Trim() != "---")
                return fileContent.Trim(); // No front-matter — entire content is the body

            lineIndex++; // skip opening ---

            // Find closing ---
            while (lineIndex < lines.Length)
            {
                if (lines[lineIndex].Trim() == "---")
                {
                    lineIndex++; // skip closing ---
                    break;
                }
                lineIndex++;
            }

            // Everything after the closing --- is the body
            if (lineIndex >= lines.Length)
                return string.Empty;

            return string.Join('\n', lines, lineIndex, lines.Length - lineIndex).Trim();
        }
    }
}
