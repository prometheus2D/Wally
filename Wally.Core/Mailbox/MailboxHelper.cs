using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Wally.Core.Actors;
using Wally.Core.Logging;

namespace Wally.Core.Mailbox
{
    /// <summary>
    /// Result for routing one mailbox file from an Outbox to one or more recipient Inbox folders.
    /// </summary>
    public sealed class MailboxRouteItemResult
    {
        public string SourceFilePath { get; init; } = string.Empty;

        public IReadOnlyList<string> Recipients { get; init; } = Array.Empty<string>();

        public bool Success { get; init; }

        public string Outcome { get; init; } = string.Empty;
    }

    public sealed class MailboxRouteResult
    {
        public List<MailboxRouteItemResult> Items { get; } = new();

        public int RoutedCount => Items.Count(item => item.Success);

        public int FailedCount => Items.Count(item => !item.Success);

        public bool HasMessages => Items.Count > 0;

        public string PrimaryKeyword => !HasMessages
            ? "NO_MESSAGES"
            : FailedCount > 0
                ? "ROUTING_FAILED"
                : "ROUTED";

        public string BuildSummary()
        {
            var builder = new StringBuilder();
            builder.AppendLine(PrimaryKeyword);
            builder.AppendLine($"Routed: {RoutedCount}");
            builder.AppendLine($"Failed: {FailedCount}");

            foreach (MailboxRouteItemResult item in Items)
            {
                string fileName = Path.GetFileName(item.SourceFilePath);
                string recipients = item.Recipients.Count == 0
                    ? "(none)"
                    : string.Join(", ", item.Recipients);
                builder.AppendLine();
                builder.AppendLine($"- {fileName} [{recipients}] -> {item.Outcome}");
            }

            return builder.ToString().TrimEnd();
        }
    }

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
        /// Routes existing mailbox files from a source Outbox folder into the target actor Inbox folders.
        /// This helper only reads front matter, resolves recipients, copies the message, and deletes the
        /// source file after successful delivery. It is intentionally not a background mailbox service.
        /// </summary>
        public static MailboxRouteResult RouteMessages(
            WallyWorkspace workspace,
            string sourceFolderPath,
            SessionLogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(workspace);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolderPath);

            var result = new MailboxRouteResult();
            if (!Directory.Exists(sourceFolderPath))
            {
                logger?.LogInfo($"Mailbox source folder does not exist: {sourceFolderPath}");
                return result;
            }

            foreach (string messagePath in Directory.GetFiles(sourceFolderPath, "*.md", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                result.Items.Add(RouteSingleMessage(workspace, messagePath, logger));
            }

            return result;
        }

        public static void AppendRoutingLog(string logPath, MailboxRouteResult routeResult)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(logPath);
            ArgumentNullException.ThrowIfNull(routeResult);

            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            var builder = new StringBuilder();
            builder.AppendLine($"## Mailbox Routing {DateTimeOffset.UtcNow:O}");
            builder.AppendLine();
            builder.AppendLine(routeResult.BuildSummary());
            builder.AppendLine();

            File.AppendAllText(logPath, builder.ToString());
        }

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
                return fileContent.Trim(); // No front-matter � entire content is the body

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

        private static MailboxRouteItemResult RouteSingleMessage(
            WallyWorkspace workspace,
            string messagePath,
            SessionLogger? logger)
        {
            try
            {
                string content = File.ReadAllText(messagePath);
                Dictionary<string, string> frontMatter = ParseFrontMatter(content);
                string recipientsValue = frontMatter.GetValueOrDefault("to", string.Empty) ?? string.Empty;
                string[] recipients = recipientsValue
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (recipients.Length == 0)
                {
                    return new MailboxRouteItemResult
                    {
                        SourceFilePath = messagePath,
                        Success = false,
                        Outcome = "missing 'to' recipient"
                    };
                }

                var targetActors = new List<Actor>(recipients.Length);
                foreach (string recipient in recipients)
                {
                    Actor? targetActor = workspace.Actors.FirstOrDefault(actor =>
                        string.Equals(actor.Name, recipient, StringComparison.OrdinalIgnoreCase));
                    if (targetActor == null)
                    {
                        return new MailboxRouteItemResult
                        {
                            SourceFilePath = messagePath,
                            Recipients = recipients,
                            Success = false,
                            Outcome = $"unknown recipient '{recipient}'"
                        };
                    }

                    targetActors.Add(targetActor);
                }

                foreach (Actor targetActor in targetActors)
                {
                    string inboxPath = Path.Combine(targetActor.FolderPath, WallyHelper.MailboxInboxFolderName);
                    Directory.CreateDirectory(inboxPath);
                    string destinationPath = Path.Combine(inboxPath, Path.GetFileName(messagePath));
                    File.Copy(messagePath, destinationPath, overwrite: true);
                }

                File.Delete(messagePath);
                logger?.LogInfo($"Routed mailbox item '{Path.GetFileName(messagePath)}' to [{string.Join(", ", recipients)}].");

                return new MailboxRouteItemResult
                {
                    SourceFilePath = messagePath,
                    Recipients = recipients,
                    Success = true,
                    Outcome = $"delivered to {string.Join(", ", recipients)}"
                };
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to route mailbox item '{messagePath}': {ex.Message}");
                return new MailboxRouteItemResult
                {
                    SourceFilePath = messagePath,
                    Success = false,
                    Outcome = ex.Message
                };
            }
        }
    }
}
