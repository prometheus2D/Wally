using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Wally.Core.Actors;
using Wally.Core.Logging;

namespace Wally.Core
{
    /// <summary>
    /// Parses and executes action blocks from LLM responses.
    /// Action blocks are fenced code blocks with ```action syntax.
    /// </summary>
    public static class ActionDispatcher
    {
        private static readonly Regex ActionBlockRegex = new(
            @"```action\s*\n(.*?)\n```", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        /// <summary>
        /// Parses and executes all action blocks found in the LLM response.
        /// Only executes actions that the actor has declared in their abilities or actions list.
        /// </summary>
        /// <param name="actor">The actor whose response contains the actions.</param>
        /// <param name="llmResponse">The full LLM response text.</param>
        /// <param name="workspace">The workspace context.</param>
        /// <param name="logger">Session logger for recording actions.</param>
        /// <returns>The original response with executed action results appended.</returns>
        public static string ProcessActionBlocks(Actor actor, string llmResponse, WallyWorkspace workspace, SessionLogger? logger)
        {
            var matches = ActionBlockRegex.Matches(llmResponse);
            if (matches.Count == 0)
                return llmResponse;

            var results = new List<string> { llmResponse };

            foreach (Match match in matches)
            {
                try
                {
                    var actionResult = ExecuteActionBlock(actor, match.Groups[1].Value, workspace, logger);
                    if (!string.IsNullOrEmpty(actionResult))
                        results.Add(actionResult);
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Action execution failed: {ex.Message}");
                    results.Add($"? Action failed: {ex.Message}");
                }
            }

            return string.Join("\n\n", results);
        }

        private static string ExecuteActionBlock(Actor actor, string actionContent, WallyWorkspace workspace, SessionLogger? logger)
        {
            var actionParams = ParseActionParameters(actionContent);
            
            if (!actionParams.TryGetValue("name", out string? actionName) || string.IsNullOrEmpty(actionName))
                return "? Action block missing 'name' parameter";

            // Check if actor has this ability/action
            bool hasSharedAbility = actor.Abilities.Contains(actionName, StringComparer.OrdinalIgnoreCase);
            bool hasRoleAction = HasRoleAction(actor, actionName);
            
            if (!hasSharedAbility && !hasRoleAction)
            {
                logger?.LogError($"Actor '{actor.Name}' attempted to use unauthorized action '{actionName}'");
                return $"? Actor '{actor.Name}' is not authorized to use action '{actionName}'";
            }

            logger?.LogInfo($"Actor '{actor.Name}' executing action '{actionName}'");

            // Execute the action
            return actionName.ToLowerInvariant() switch
            {
                "read_context" => ExecuteReadContext(actionParams, workspace, logger),
                "browse_workspace" => ExecuteBrowseWorkspace(actionParams, workspace, logger),
                "send_message" => ExecuteSendMessage(actor, actionParams, workspace, logger),
                "change_code" => ExecuteChangeCode(actionParams, workspace, logger),
                "write_document" => ExecuteWriteDocument(actionParams, workspace, logger),
                "write_requirements" => ExecuteWriteRequirements(actionParams, workspace, logger),
                _ => $"? Unknown action: {actionName}"
            };
        }

        private static Dictionary<string, string> ParseActionParameters(string actionContent)
        {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var lines = actionContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            string? currentKey = null;
            var currentValue = new List<string>();

            foreach (string line in lines)
            {
                if (line.Contains(':') && !line.StartsWith(' ') && !line.StartsWith('\t'))
                {
                    // Save previous parameter
                    if (currentKey != null)
                        parameters[currentKey] = string.Join('\n', currentValue).Trim();

                    // Parse new parameter
                    var colonIndex = line.IndexOf(':');
                    currentKey = line[..colonIndex].Trim();
                    var valueStart = line[(colonIndex + 1)..].Trim();
                    
                    currentValue.Clear();
                    if (valueStart == "|")
                    {
                        // Multi-line value starts next line
                        continue;
                    }
                    else if (!string.IsNullOrEmpty(valueStart))
                    {
                        // Single-line value
                        currentValue.Add(valueStart);
                    }
                }
                else if (currentKey != null)
                {
                    // Continuation of multi-line value
                    currentValue.Add(line.TrimStart());
                }
            }

            // Save final parameter
            if (currentKey != null)
                parameters[currentKey] = string.Join('\n', currentValue).Trim();

            return parameters;
        }

        private static bool HasRoleAction(Actor actor, string actionName)
        {
            // This would check the actor's "actions" array from actor.json
            // For now, hardcode the known role actions
            return actionName.ToLowerInvariant() switch
            {
                "change_code" => actor.Name.Equals("Engineer", StringComparison.OrdinalIgnoreCase),
                "write_document" => actor.Name.Equals("Engineer", StringComparison.OrdinalIgnoreCase) || 
                                  actor.Name.Equals("BusinessAnalyst", StringComparison.OrdinalIgnoreCase),
                "write_requirements" => actor.Name.Equals("RequirementsExtractor", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        // Shared ability implementations
        
        private static string ExecuteReadContext(Dictionary<string, string> parameters, WallyWorkspace workspace, SessionLogger? logger)
        {
            if (!parameters.TryGetValue("path", out string? relativePath) || string.IsNullOrEmpty(relativePath))
                return "? read_context requires 'path' parameter";

            try
            {
                string fullPath = Path.Combine(workspace.WorkSource, relativePath);
                if (!File.Exists(fullPath))
                    return $"? File not found: {relativePath}";

                string content = File.ReadAllText(fullPath);
                logger?.LogInfo($"Read file: {relativePath} ({content.Length} characters)");
                
                return $"? Read file: {relativePath}\n\nContent:\n```\n{content}\n```";
            }
            catch (Exception ex)
            {
                return $"? Failed to read file: {ex.Message}";
            }
        }

        private static string ExecuteBrowseWorkspace(Dictionary<string, string> parameters, WallyWorkspace workspace, SessionLogger? logger)
        {
            string relativePath = parameters.GetValueOrDefault("path", "") ?? "";
            bool recursive = parameters.GetValueOrDefault("recursive", "false")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

            try
            {
                string fullPath = string.IsNullOrEmpty(relativePath) 
                    ? workspace.WorkSource 
                    : Path.Combine(workspace.WorkSource, relativePath);

                if (!Directory.Exists(fullPath))
                    return $"? Directory not found: {relativePath}";

                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(fullPath, "*", searchOption)
                    .Select(f => Path.GetRelativePath(workspace.WorkSource, f))
                    .OrderBy(f => f)
                    .Take(50) // Limit output
                    .ToList();

                var directories = Directory.GetDirectories(fullPath, "*", searchOption)
                    .Select(d => Path.GetRelativePath(workspace.WorkSource, d))
                    .OrderBy(d => d)
                    .Take(50) // Limit output
                    .ToList();

                logger?.LogInfo($"Browsed directory: {relativePath} (found {files.Count} files, {directories.Count} directories)");

                var result = $"? Browsed directory: {(string.IsNullOrEmpty(relativePath) ? "root" : relativePath)}\n\n";
                
                if (directories.Any())
                {
                    result += "**Directories:**\n";
                    result += string.Join('\n', directories.Select(d => $"- {d}/"));
                    result += "\n\n";
                }

                if (files.Any())
                {
                    result += "**Files:**\n";
                    result += string.Join('\n', files.Select(f => $"- {f}"));
                }

                return result;
            }
            catch (Exception ex)
            {
                return $"? Failed to browse directory: {ex.Message}";
            }
        }

        private static string ExecuteSendMessage(Actor fromActor, Dictionary<string, string> parameters, WallyWorkspace workspace, SessionLogger? logger)
        {
            if (!parameters.TryGetValue("to", out string? targetActorName) || string.IsNullOrEmpty(targetActorName))
                return "? send_message requires 'to' parameter";
            
            if (!parameters.TryGetValue("subject", out string? subject) || string.IsNullOrEmpty(subject))
                return "? send_message requires 'subject' parameter";
                
            if (!parameters.TryGetValue("body", out string? body) || string.IsNullOrEmpty(body))
                return "? send_message requires 'body' parameter";

            string replyTo = parameters.GetValueOrDefault("replyTo", fromActor.Name) ?? fromActor.Name;

            try
            {
                // Find target actor
                var targetActor = workspace.Actors.FirstOrDefault(a => 
                    string.Equals(a.Name, targetActorName, StringComparison.OrdinalIgnoreCase));

                if (targetActor == null)
                    return $"? Target actor '{targetActorName}' not found";

                // Generate correlation ID and timestamp
                string correlationId = Guid.NewGuid().ToString("N")[..8];
                string timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                // Create message content with YAML front matter
                string messageContent = $"""
                    ---
                    from: {fromActor.Name}
                    to: {targetActorName}
                    replyTo: {replyTo}
                    subject: {subject}
                    correlationId: {correlationId}
                    timestamp: {timestamp}
                    status: new
                    ---
                    
                    {body}
                    """;

                // Write to target's Inbox
                string inboxPath = Path.Combine(targetActor.FolderPath, "Inbox");
                Directory.CreateDirectory(inboxPath);

                string messageFileName = $"{timestamp}_{correlationId}_{subject}.md";
                string messageFilePath = Path.Combine(inboxPath, messageFileName);

                File.WriteAllText(messageFilePath, messageContent);

                logger?.LogInfo($"Message sent from '{fromActor.Name}' to '{targetActorName}': {subject} (ID: {correlationId})");

                return $"? Message sent to {targetActorName}\n**Subject:** {subject}\n**Correlation ID:** {correlationId}";
            }
            catch (Exception ex)
            {
                return $"? Failed to send message: {ex.Message}";
            }
        }

        // Role-exclusive action implementations

        private static string ExecuteChangeCode(Dictionary<string, string> parameters, WallyWorkspace workspace, SessionLogger? logger)
        {
            if (!parameters.TryGetValue("path", out string? relativePath) || string.IsNullOrEmpty(relativePath))
                return "? change_code requires 'path' parameter";
                
            if (!parameters.TryGetValue("content", out string? content))
                return "? change_code requires 'content' parameter";

            try
            {
                string fullPath = Path.Combine(workspace.WorkSource, relativePath);
                string? directory = Path.GetDirectoryName(fullPath);
                
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(fullPath, content);
                
                logger?.LogInfo($"Code changed: {relativePath} ({content.Length} characters)");
                
                return $"? File written: {relativePath}";
            }
            catch (Exception ex)
            {
                return $"? Failed to write file: {ex.Message}";
            }
        }

        private static string ExecuteWriteDocument(Dictionary<string, string> parameters, WallyWorkspace workspace, SessionLogger? logger)
        {
            if (!parameters.TryGetValue("path", out string? relativePath) || string.IsNullOrEmpty(relativePath))
                return "? write_document requires 'path' parameter";
                
            if (!parameters.TryGetValue("content", out string? content))
                return "? write_document requires 'content' parameter";

            if (!relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                return "? write_document can only write .md files";

            try
            {
                string fullPath = Path.Combine(workspace.WorkSource, relativePath);
                string? directory = Path.GetDirectoryName(fullPath);
                
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(fullPath, content);
                
                logger?.LogInfo($"Document written: {relativePath} ({content.Length} characters)");
                
                return $"? Document written: {relativePath}";
            }
            catch (Exception ex)
            {
                return $"? Failed to write document: {ex.Message}";
            }
        }

        private static string ExecuteWriteRequirements(Dictionary<string, string> parameters, WallyWorkspace workspace, SessionLogger? logger)
        {
            // Same as write_document but restricted to RequirementsExtractor
            return ExecuteWriteDocument(parameters, workspace, logger);
        }
    }
}