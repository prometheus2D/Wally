using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wally.Core;
using Wally.Core.Actors;
using Wally.Core.Logging;

namespace Wally.Forms.ChatPanelSupport
{
    internal static class ChatPanelExecutionService
    {
        public static ChatPanelResolvedRequest ResolveRequest(WallyEnvironment env, ChatPanelRequest request)
        {
            ArgumentNullException.ThrowIfNull(env);
            ArgumentNullException.ThrowIfNull(request);

            if (!env.HasWorkspace)
                throw new InvalidOperationException("No workspace is loaded.");

            string prompt = request.DisplayPrompt;
            WallyLoopDefinition? loopDef = null;
            if (!string.IsNullOrWhiteSpace(request.LoopName))
            {
                loopDef = env.GetLoop(request.LoopName!)
                    ?? throw new InvalidOperationException($"Loop '{request.LoopName}' was not found.");
            }

            string? actorName = string.IsNullOrWhiteSpace(request.ActorName) ? null : request.ActorName;
            if (string.IsNullOrWhiteSpace(actorName) && !string.IsNullOrWhiteSpace(loopDef?.ActorName))
                actorName = loopDef!.ActorName;

            bool directMode = string.IsNullOrWhiteSpace(actorName);
            Actor? actor = null;
            if (!directMode)
            {
                actor = env.GetActor(actorName!)
                    ?? throw new InvalidOperationException($"Actor '{actorName}' was not found.");
                env.EnforceLoopPolicy(actor, loopDef?.Name);
            }

            string? wrapperName = ResolveWrapperName(env, request.Mode, actor, request.WrapperName);
            string? model = string.IsNullOrWhiteSpace(request.ModelOverride)
                ? env.Workspace!.Config.DefaultModel
                : request.ModelOverride;
            string initialPrompt = loopDef != null && !loopDef.HasSteps && !string.IsNullOrWhiteSpace(loopDef.StartPrompt)
                ? loopDef.StartPrompt.Replace("{userPrompt}", prompt)
                : prompt;

            return new ChatPanelResolvedRequest
            {
                Request = request,
                Actor = actor,
                LoopDefinition = loopDef,
                ResolvedModel = model,
                ResolvedWrapperName = wrapperName,
                ActorLabel = directMode ? "(no actor)" : actorName!,
                DirectMode = directMode,
                InitialPrompt = initialPrompt
            };
        }

        public static ChatPanelPromptPreview BuildPromptPreview(WallyEnvironment env, ChatPanelRequest request)
        {
            var session = CreateSession(env, request, prepareExecutionState: false);
            return session.BuildNextPromptPreview();
        }

        public static ChatPanelExecutionSession CreateSession(
            WallyEnvironment env,
            ChatPanelRequest request,
            bool prepareExecutionState = true)
        {
            ChatPanelResolvedRequest resolved = ResolveRequest(env, request);
            return new ChatPanelExecutionSession(env, resolved, prepareExecutionState);
        }

        public static MermaidDiagramDefinition BuildDiagramDefinition(WallyEnvironment env, ChatPanelRequest request)
        {
            ChatPanelResolvedRequest resolved = ResolveRequest(env, request);
            var sb = new StringBuilder();
            sb.AppendLine(resolved.LoopDefinition?.HasSteps == true ? "flowchart LR" : "flowchart TD");

            sb.AppendLine("    INPUT[\"User Prompt\"]");
            sb.AppendLine($"    MODE[\"Mode<br/>{request.Mode}\"]");
            sb.AppendLine($"    MODEL[\"Model<br/>{EscapeLabel(resolved.ResolvedModel ?? "(default)")}\"]");
            sb.AppendLine($"    WRAPPER[\"Wrapper<br/>{EscapeLabel(resolved.ResolvedWrapperName ?? "(auto)")}\"]");
            sb.AppendLine("    INPUT --> MODE");
            sb.AppendLine("    MODE --> MODEL");
            sb.AppendLine("    MODEL --> WRAPPER");

            if (resolved.LoopDefinition?.UsesNamedStepRouting == true)
            {
                sb.AppendLine($"    ROUTED[\"Routed Loop<br/>{EscapeLabel(resolved.LoopDefinition.Name)}<br/>Start: {EscapeLabel(resolved.LoopDefinition.StartStepName)}\"]");
                sb.AppendLine($"    MAX[\"Max Iterations<br/>{resolved.LoopDefinition.MaxIterations}\"]");
                sb.AppendLine("    OUTPUT[\"Routed Loop Result\"]");
                sb.AppendLine("    WRAPPER --> ROUTED");
                sb.AppendLine("    ROUTED --> MAX");
                sb.AppendLine("    MAX --> OUTPUT");
            }
            else if (resolved.LoopDefinition?.HasSteps == true)
            {
                for (int i = 0; i < resolved.LoopDefinition.Steps.Count; i++)
                {
                    WallyStepDefinition step = resolved.LoopDefinition.Steps[i];
                    string stepActor = string.IsNullOrWhiteSpace(step.ActorName)
                        ? (string.IsNullOrWhiteSpace(resolved.LoopDefinition.ActorName) ? "direct" : resolved.LoopDefinition.ActorName + " (fallback)")
                        : step.ActorName;
                    string stepName = string.IsNullOrWhiteSpace(step.Name) ? $"step-{i + 1}" : step.Name;
                    sb.AppendLine($"    STEP{i + 1}[\"{i + 1}. {EscapeLabel(stepName)}<br/>Actor: {EscapeLabel(stepActor)}\"]");
                    sb.AppendLine(i == 0 ? "    WRAPPER --> STEP1" : $"    STEP{i} --> STEP{i + 1}");
                }
                sb.AppendLine($"    OUTPUT[\"Pipeline Result<br/>{resolved.LoopDefinition.Steps.Count} step(s)\"]");
                sb.AppendLine($"    STEP{resolved.LoopDefinition.Steps.Count} --> OUTPUT");
            }
            else if (resolved.LoopDefinition?.IsAgentLoop == true)
            {
                sb.AppendLine($"    LOOP[\"Agent Loop<br/>{EscapeLabel(resolved.LoopDefinition.Name)}<br/>Actor: {EscapeLabel(resolved.ActorLabel)}\"]");
                sb.AppendLine($"    CHECK{{\"Stop?<br/>Keyword: {EscapeLabel(resolved.LoopDefinition.StopKeyword)}<br/>Max: {resolved.LoopDefinition.MaxIterations}\"}}");
                sb.AppendLine($"    FEEDBACK[\"Feedback<br/>{EscapeLabel(resolved.LoopDefinition.FeedbackMode)}\"]");
                sb.AppendLine("    OUTPUT[\"Loop Result\"]");
                sb.AppendLine("    WRAPPER --> LOOP");
                sb.AppendLine("    LOOP --> CHECK");
                sb.AppendLine("    CHECK -- \"No\" --> FEEDBACK");
                sb.AppendLine("    FEEDBACK --> LOOP");
                sb.AppendLine("    CHECK -- \"Yes\" --> OUTPUT");
            }
            else
            {
                string runLabel = resolved.DirectMode
                    ? "Direct Prompt"
                    : $"Actor<br/>{EscapeLabel(resolved.ActorLabel)}";
                if (resolved.LoopDefinition != null)
                    runLabel = $"Loop<br/>{EscapeLabel(resolved.LoopDefinition.Name)}<br/>{runLabel}";
                sb.AppendLine($"    RUN[\"{runLabel}\"]");
                sb.AppendLine("    OUTPUT[\"Response\"]");
                sb.AppendLine("    WRAPPER --> RUN");
                sb.AppendLine("    RUN --> OUTPUT");
            }

            string stemBase = string.IsNullOrWhiteSpace(request.LoopName)
                ? (string.IsNullOrWhiteSpace(request.ActorName) ? "direct-chat" : request.ActorName!)
                : request.LoopName!;

            return new MermaidDiagramDefinition
            {
                TargetKind = MermaidDiagramTargetKind.Workspace,
                Title = string.IsNullOrWhiteSpace(request.LoopName)
                    ? "Chat Execution Diagram"
                    : $"Chat Execution Diagram - {request.LoopName}",
                FileStem = "chat-" + SanitizeFileStem(stemBase),
                RelativeOutputFolder = "Chats",
                MermaidSource = sb.ToString()
            };
        }

        internal static ChatPanelPromptPreview BuildPromptPreview(WallyEnvironment env, ChatPanelResolvedRequest resolved, ChatPanelExecutionSession.PendingExecution pending, int executedCount, string? stopReason, bool isCompletionPreview)
        {
            var sections = new List<ChatPanelPromptPreviewSection>();
            string? wrapperName = resolved.ResolvedWrapperName;
            var wrapper = wrapperName != null
                ? env.Workspace!.LlmWrappers.FirstOrDefault(w => string.Equals(w.Name, wrapperName, StringComparison.OrdinalIgnoreCase))
                : null;
            bool wrapperUsesHistory = wrapper?.UseConversationHistory ?? true;

            sections.Add(new ChatPanelPromptPreviewSection
            {
                Heading = "Resolved Selections",
                Body = string.Join(Environment.NewLine, new[]
                {
                    $"Mode:    {resolved.Request.Mode}",
                    $"Actor:   {(resolved.DirectMode ? "(none - direct prompt)" : resolved.ActorLabel)}",
                    $"Loop:    {(resolved.LoopDefinition != null ? resolved.LoopDefinition.Name : "(none - single run)")}",
                    $"Model:   {resolved.ResolvedModel ?? "(none)"}",
                    $"Wrapper: {wrapperName ?? "(none)"}",
                    $"History: {(resolved.Request.NoHistory ? "disabled by request" : "enabled when wrapper allows it")}" 
                })
            });

            sections.Add(new ChatPanelPromptPreviewSection
            {
                Heading = isCompletionPreview ? "Execution State" : "Next Execution",
                Body = isCompletionPreview
                    ? $"Execution complete after {executedCount} result(s). Stop reason: {stopReason ?? "Completed"}."
                    : pending.ExecutionSummary
            });

            sections.Add(new ChatPanelPromptPreviewSection
            {
                Heading = "Equivalent CLI Command",
                Body = BuildEquivalentCommand(resolved)
            });

            if (resolved.LoopDefinition?.UsesNamedStepRouting == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Start step: {resolved.LoopDefinition.StartStepName}");
                sb.AppendLine($"MaxIterations: {resolved.LoopDefinition.MaxIterations}");
                sb.AppendLine();

                for (int i = 0; i < resolved.LoopDefinition.Steps.Count; i++)
                {
                    WallyStepDefinition step = resolved.LoopDefinition.Steps[i];
                    string stepActor = string.IsNullOrWhiteSpace(step.ActorName)
                        ? (string.IsNullOrWhiteSpace(resolved.LoopDefinition.ActorName) ? "(direct)" : resolved.LoopDefinition.ActorName)
                        : step.ActorName;
                    string stepName = string.IsNullOrWhiteSpace(step.Name) ? $"step-{i + 1}" : step.Name;
                    sb.AppendLine($"Step {i + 1}: {stepName} ({stepActor})");
                    sb.AppendLine($"Kind: {step.EffectiveKind}");
                    if (!string.IsNullOrWhiteSpace(step.DefaultNextStep))
                        sb.AppendLine($"Default next: {step.DefaultNextStep}");
                    if (step.KeywordRoutes.Count > 0)
                    {
                        sb.AppendLine("Routes:");
                        foreach (KeyValuePair<string, string> route in step.KeywordRoutes)
                            sb.AppendLine($"  {route.Key} -> {route.Value}");
                    }
                    sb.AppendLine();
                }

                sections.Add(new ChatPanelPromptPreviewSection
                {
                    Heading = $"Routed Loop Steps ({resolved.LoopDefinition.Steps.Count})",
                    Body = sb.ToString().TrimEnd()
                });
            }
            else if (resolved.LoopDefinition?.HasSteps == true)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < resolved.LoopDefinition.Steps.Count; i++)
                {
                    WallyStepDefinition step = resolved.LoopDefinition.Steps[i];
                    string stepActor = string.IsNullOrWhiteSpace(step.ActorName)
                        ? (string.IsNullOrWhiteSpace(resolved.LoopDefinition.ActorName) ? "(direct)" : resolved.LoopDefinition.ActorName)
                        : step.ActorName;
                    string stepName = string.IsNullOrWhiteSpace(step.Name) ? $"step-{i + 1}" : step.Name;
                    sb.AppendLine($"Step {i + 1}: {stepName} ({stepActor})");
                    sb.AppendLine(string.IsNullOrWhiteSpace(step.PromptTemplate) ? "(default template)" : step.PromptTemplate);
                    sb.AppendLine();
                }

                sections.Add(new ChatPanelPromptPreviewSection
                {
                    Heading = $"Pipeline Steps ({resolved.LoopDefinition.Steps.Count})",
                    Body = sb.ToString().TrimEnd()
                });
            }
            else if (resolved.LoopDefinition?.IsAgentLoop == true)
            {
                sections.Add(new ChatPanelPromptPreviewSection
                {
                    Heading = "Agent Loop",
                    Body = string.Join(Environment.NewLine, new[]
                    {
                        $"Loop:         {resolved.LoopDefinition.Name}",
                        $"MaxIterations:{resolved.LoopDefinition.MaxIterations}",
                        $"StopKeyword:  {resolved.LoopDefinition.StopKeyword}",
                        $"FeedbackMode: {resolved.LoopDefinition.FeedbackMode}"
                    })
                });
            }

            string? historyBlock = null;
            if (!pending.SkipHistoryInjection && !resolved.Request.NoHistory && wrapperUsesHistory)
            {
                string? actorFilter = resolved.DirectMode ? null : pending.HistoryActorName;
                var recentTurns = env.History.GetRecentTurns(ConversationLogger.MaxInjectedTurns, actorFilter);
                historyBlock = ConversationLogger.FormatHistoryBlock(recentTurns);
                sections.Add(new ChatPanelPromptPreviewSection
                {
                    Heading = "Conversation History",
                    Body = string.IsNullOrWhiteSpace(historyBlock)
                        ? "(no matching history turns)"
                        : historyBlock
                });
            }
            else
            {
                sections.Add(new ChatPanelPromptPreviewSection
                {
                    Heading = "Conversation History",
                    Body = pending.SkipHistoryInjection
                        ? "Skipped for this execution step."
                        : resolved.Request.NoHistory
                            ? "Disabled by request."
                            : wrapperUsesHistory
                                ? "(no matching history turns)"
                                : $"Disabled for wrapper '{wrapperName}'."
                });
            }

            string exactPrompt = resolved.DirectMode
                ? (!string.IsNullOrWhiteSpace(historyBlock)
                    ? historyBlock + "\n" + pending.ExecutionPrompt
                    : pending.ExecutionPrompt)
                : resolved.Actor!.ProcessPrompt(pending.ExecutionPrompt, historyBlock);

            sections.Add(new ChatPanelPromptPreviewSection
            {
                Heading = resolved.DirectMode
                    ? "Final Prompt (direct mode)"
                    : $"Actor-Enriched Prompt ({pending.ActorLabel})",
                Body = exactPrompt
            });

            if (wrapper != null)
            {
                var wrapperSb = new StringBuilder();
                wrapperSb.AppendLine($"Executable:             {wrapper.Executable}");
                wrapperSb.AppendLine($"Template:               {wrapper.ArgumentTemplate}");
                wrapperSb.AppendLine($"CanMakeChanges:         {wrapper.CanMakeChanges}");
                wrapperSb.AppendLine($"UseConversationHistory: {wrapper.UseConversationHistory}");
                wrapperSb.AppendLine($"Model:                  {resolved.ResolvedModel ?? "(none)"}");
                wrapperSb.AppendLine($"SourcePath:             {env.SourcePath ?? "(none)"}");
                sections.Add(new ChatPanelPromptPreviewSection
                {
                    Heading = $"Wrapper: {wrapper.Name}",
                    Body = wrapperSb.ToString().TrimEnd()
                });
            }

            return new ChatPanelPromptPreview
            {
                Request = resolved.Request,
                Title = isCompletionPreview
                    ? $"Completed Preview - {resolved.Request.DisplayLabel}"
                    : $"Prompt Preview - {pending.DisplayTitle}",
                ExactPrompt = exactPrompt,
                NextExecutionLabel = pending.ExecutionSummary,
                IsCompletionPreview = isCompletionPreview,
                Sections = sections
            };
        }

        internal static string CombineAgentPrompt(WallyLoopDefinition loopDef, string originalPrompt, string latestResponse, int iteration)
        {
            return string.Equals(loopDef.FeedbackMode, "ReplacePrompt", StringComparison.OrdinalIgnoreCase)
                ? latestResponse
                : originalPrompt + "\n\n" +
                  $"--- Previous response (iteration {iteration + 1}) ---\n" +
                  latestResponse + "\n---\n\n" +
                  "Continue from where you left off. If you are done, respond without any action blocks.";
        }

        private static string BuildEquivalentCommand(ChatPanelResolvedRequest resolved)
        {
            string displayPrompt = string.IsNullOrWhiteSpace(resolved.Request.DisplayPrompt)
                ? "<resume-from-state>"
                : resolved.Request.DisplayPrompt;
            var parts = new List<string> { "run", $"\"{displayPrompt}\"" };
            if (!resolved.DirectMode)
                parts.Add($"-a {resolved.ActorLabel}");
            if (!string.IsNullOrWhiteSpace(resolved.Request.LoopName))
                parts.Add($"-l {resolved.Request.LoopName}");
            if (!string.IsNullOrWhiteSpace(resolved.Request.ModelOverride))
                parts.Add($"-m {resolved.Request.ModelOverride}");
            if (!string.IsNullOrWhiteSpace(resolved.ResolvedWrapperName))
                parts.Add($"-w {resolved.ResolvedWrapperName}");
            if (resolved.Request.NoHistory)
                parts.Add("--no-history");
            return string.Join(" ", parts);
        }

        private static string? ResolveWrapperName(
            WallyEnvironment env,
            ChatPanelExecutionMode mode,
            Actor? actor,
            string? explicitWrapperName)
        {
            if (!string.IsNullOrWhiteSpace(explicitWrapperName))
                return explicitWrapperName;

            bool wantAgent = mode == ChatPanelExecutionMode.Agent;
            var wrappers = env.Workspace!.LlmWrappers;

            if (actor != null)
            {
                if (!string.IsNullOrWhiteSpace(actor.PreferredWrapper))
                {
                    var preferred = wrappers.FirstOrDefault(w =>
                        string.Equals(w.Name, actor.PreferredWrapper, StringComparison.OrdinalIgnoreCase));
                    if (preferred != null && preferred.CanMakeChanges == wantAgent && actor.IsWrapperAllowed(preferred.Name))
                        return preferred.Name;
                }

                if (actor.AllowedWrappers.Count > 0)
                {
                    foreach (string name in actor.AllowedWrappers)
                    {
                        var allowed = wrappers.FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));
                        if (allowed != null && allowed.CanMakeChanges == wantAgent)
                            return allowed.Name;
                    }

                    foreach (string name in actor.AllowedWrappers)
                    {
                        if (wrappers.Any(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)))
                            return name;
                    }
                }
            }

            string? defaultWrapper = env.Workspace.Config.DefaultWrapper;
            if (!string.IsNullOrWhiteSpace(defaultWrapper))
            {
                var resolvedDefault = wrappers.FirstOrDefault(w =>
                    string.Equals(w.Name, defaultWrapper, StringComparison.OrdinalIgnoreCase));
                if (resolvedDefault != null && resolvedDefault.CanMakeChanges == wantAgent)
                    return resolvedDefault.Name;
            }

            return wrappers.FirstOrDefault(w => w.CanMakeChanges == wantAgent)?.Name
                ?? wrappers.FirstOrDefault()?.Name;
        }

        private static string EscapeLabel(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\r\n", "<br/>", StringComparison.Ordinal)
                .Replace("\n", "<br/>", StringComparison.Ordinal)
                .Replace("\r", "<br/>", StringComparison.Ordinal)
                .Replace("\"", "'", StringComparison.Ordinal);
        }

        private static string SanitizeFileStem(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "chat";

            return new string(value
                .Trim()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                .ToArray())
                .Trim('-')
                .ToLowerInvariant();
        }
    }
}