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
        public static string ProcessActionBlocks(
            Actor actor,
            string llmResponse,
            WallyWorkspace workspace,
            SessionLogger? logger,
            WallyEnvironment? environment = null,
            WallyStepDefinition? stepDefinition = null)
        {
            var matches = ActionBlockRegex.Matches(llmResponse);
            if (matches.Count == 0)
                return llmResponse;

            var results = new List<string> { llmResponse };

            foreach (Match match in matches)
            {
                try
                {
                    var actionResult = ExecuteActionBlock(actor, match.Groups[1].Value, workspace, logger, environment, stepDefinition);
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

        private static string ExecuteActionBlock(
            Actor actor,
            string actionContent,
            WallyWorkspace workspace,
            SessionLogger? logger,
            WallyEnvironment? environment,
            WallyStepDefinition? stepDefinition)
        {
            var actionParams = ParseActionParameters(actionContent);
            
            if (!actionParams.TryGetValue("name", out string? actionName) || string.IsNullOrEmpty(actionName))
                return "? Action block missing 'name' parameter";

            // ?? Authorization ????????????????????????????????????????????????
            // Check shared abilities (read_context, browse_workspace, send_message)
            bool hasAbility = actor.Abilities.Contains(actionName, StringComparer.OrdinalIgnoreCase);

            // Check role actions declared in actor.json actions[]
            var actionDef = actor.Actions.FirstOrDefault(a =>
                string.Equals(a.Name, actionName, StringComparison.OrdinalIgnoreCase));

            if (!hasAbility && actionDef == null)
            {
                logger?.LogError($"Actor '{actor.Name}' attempted to use unauthorized action '{actionName}'");
                return $"? Actor '{actor.Name}' is not authorized to use action '{actionName}'";
            }

            // ?? Validation ???????????????????????????????????????????????????
            // Required parameter check
            if (actionDef != null)
            {
                foreach (var param in actionDef.Parameters.Where(p => p.Required))
                {
                    if (!actionParams.TryGetValue(param.Name, out string? v) || string.IsNullOrEmpty(v))
                    {
                        logger?.LogError($"Action '{actionName}' missing required parameter '{param.Name}'");
                        return $"? Action '{actionName}' requires parameter '{param.Name}'";
                    }
                }
            }

            // Path pattern check � only enforced when pattern is not the catch-all "**"
            if (actionDef != null && actionDef.PathPattern != "**"
                && actionParams.TryGetValue("path", out string? targetPath)
                && !string.IsNullOrEmpty(targetPath))
            {
                if (!GlobMatch(actionDef.PathPattern, targetPath))
                {
                    logger?.LogError($"Action '{actionName}' path '{targetPath}' blocked by pattern '{actionDef.PathPattern}'");
                    return $"? Path '{targetPath}' is not permitted for action '{actionName}' (allowed pattern: {actionDef.PathPattern})";
                }
            }

            if (stepDefinition != null
                && environment != null
                && actionDef?.IsMutating == true
                && actionParams.TryGetValue("path", out string? scopedPath)
                && !string.IsNullOrEmpty(scopedPath))
            {
                try
                {
                    environment.EnsureStepWriteAllowed(stepDefinition, scopedPath, actionName);
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Step write scope blocked action '{actionName}' for path '{scopedPath}': {ex.Message}");
                    return $"? {ex.Message}";
                }
            }

            logger?.LogInfo($"Actor '{actor.Name}' executing action '{actionName}'");

            // ?? Dispatch ?????????????????????????????????????????????????????
            // Handler routing stays name-based and unchanged.
            return actionName.ToLowerInvariant() switch
            {
                "read_context"       => ExecuteReadContext(actionParams, workspace, logger),
                "browse_workspace"   => ExecuteBrowseWorkspace(actionParams, workspace, logger),
                "send_message"       => ExecuteSendMessage(actor, actionParams, workspace, logger),
                "change_code"        => ExecuteChangeCode(actionParams, workspace, logger),
                "write_document"     => ExecuteWriteDocument(actionParams, workspace, logger),
                "write_requirements" => ExecuteWriteRequirements(actionParams, workspace, logger),
                _                    => $"? Unknown action: {actionName}"
            };
        }

        /// <summary>
        /// Simple glob matcher supporting <c>**</c> (any path segments) and <c>*</c> (within a segment).
        /// <list type="bullet">
        ///   <item><c>"**"</c> � matches any path (catch-all; skipped before this method is called).</item>
        ///   <item><c>"**/*.md"</c> � matches any <c>.md</c> file at any depth.</item>
        ///   <item><c>"*.cs"</c> � matches any <c>.cs</c> file in the root only.</item>
        /// </list>
        /// </summary>
        private static bool GlobMatch(string pattern, string path)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == "**") return true;

            // Normalise path separators
            path    = path.Replace('\\', '/');
            pattern = pattern.Replace('\\', '/');

            // Convert glob to regex
            string regexPattern = "^"
                + Regex.Escape(pattern)
                    .Replace(@"\*\*/", "(.+/)?")  // **/ � zero or more path segments
                    .Replace(@"\*\*",  ".*")       // **  � anything remaining
                    .Replace(@"\*",    "[^/]*")    // *   � any chars within one segment
                + "$";

            return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
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
                // Validate target actor exists (but we write to sender's Outbox, not target's Inbox)
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

                // Write to sender's Outbox (not target's Inbox).
                // A workflow-owned routing step such as route_messages delivers it later.
                string outboxPath = Path.Combine(fromActor.FolderPath, "Outbox");
                Directory.CreateDirectory(outboxPath);

                string messageFileName = $"{timestamp}_{correlationId}_{subject}.md";
                string messageFilePath = Path.Combine(outboxPath, messageFileName);

                File.WriteAllText(messageFilePath, messageContent);

                logger?.LogInfo($"Message queued in '{fromActor.Name}' Outbox for '{targetActorName}': {subject} (ID: {correlationId})");

                return $"? Message queued in Outbox for {targetActorName}\n**Subject:** {subject}\n**Correlation ID:** {correlationId}\n(Run a routing step such as `route_messages` or use `route-outbox` to deliver)";
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