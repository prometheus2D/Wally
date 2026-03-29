using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Wally.Core;

namespace Wally.Forms.ChatPanelSupport
{
    internal sealed class ChatPanelExecutionSession
    {
        private static readonly Regex ActionBlockRegex = new(
            @"```action\s*\n(.*?)\n```",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private readonly WallyEnvironment _environment;
        private readonly string _originalPrompt;
        private string? _previousPipelineResult;
        private string _currentAgentPrompt;
        private int _nextPipelineStepIndex;
        private int _nextAgentIteration;
        private bool _singleRunExecuted;

        public ChatPanelExecutionSession(WallyEnvironment environment, ChatPanelResolvedRequest resolvedRequest)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            ResolvedRequest = resolvedRequest ?? throw new ArgumentNullException(nameof(resolvedRequest));
            _originalPrompt = ResolvedRequest.Request.DisplayPrompt;
            _currentAgentPrompt = ResolvedRequest.InitialPrompt;
        }

        public ChatPanelResolvedRequest ResolvedRequest { get; }
        public List<WallyRunResult> Results { get; } = new();
        public bool IsCompleted { get; private set; }
        public string? StopReason { get; private set; }

        public string CurrentStatus => IsCompleted
            ? $"Completed ({StopReason ?? "Completed"})"
            : GetPendingExecution().ExecutionSummary;

        public async Task<WallyRunResult> ExecuteNextAsync(CancellationToken cancellationToken = default)
        {
            if (IsCompleted)
                throw new InvalidOperationException("This chat execution session is already complete.");

            PendingExecution pending = GetPendingExecution();

            string response = pending.DirectMode
                ? await _environment.ExecutePromptAsync(
                    pending.ExecutionPrompt,
                    ResolvedRequest.ResolvedModel,
                    ResolvedRequest.ResolvedWrapperName,
                    loopName: pending.RecordLoopName,
                    iteration: pending.Iteration,
                    skipHistory: pending.SkipHistoryInjection,
                    cancellationToken: cancellationToken).ConfigureAwait(false)
                : await _environment.ExecuteActorAsync(
                    ResolvedRequest.Actor!,
                    pending.ExecutionPrompt,
                    ResolvedRequest.ResolvedModel,
                    ResolvedRequest.ResolvedWrapperName,
                    loopName: pending.RecordLoopName,
                    iteration: pending.Iteration,
                    skipHistory: pending.SkipHistoryInjection,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            var result = new WallyRunResult
            {
                StepName = pending.StepName,
                ActorName = pending.ActorLabel,
                Response = response,
                Iteration = pending.Iteration,
                StopReason = null
            };

            if (ResolvedRequest.LoopDefinition?.HasSteps == true)
            {
                Results.Add(result);
                _previousPipelineResult = response;
                _nextPipelineStepIndex++;
                if (_nextPipelineStepIndex >= ResolvedRequest.LoopDefinition.Steps.Count)
                {
                    IsCompleted = true;
                    StopReason = "PipelineComplete";
                }
                return result;
            }

            if (ResolvedRequest.LoopDefinition?.IsAgentLoop == true)
            {
                string? stopReason = EvaluateAgentStopReason(response, _nextAgentIteration);
                if (stopReason != null)
                {
                    result = new WallyRunResult
                    {
                        StepName = null,
                        ActorName = pending.ActorLabel,
                        Response = response,
                        Iteration = pending.Iteration,
                        StopReason = stopReason
                    };
                    Results.Add(result);
                    StopReason = stopReason;
                    IsCompleted = true;
                    return result;
                }

                Results.Add(result);
                _currentAgentPrompt = ChatPanelExecutionService.CombineAgentPrompt(
                    ResolvedRequest.LoopDefinition,
                    _originalPrompt,
                    response,
                    _nextAgentIteration);
                _nextAgentIteration++;
                return result;
            }

            Results.Add(result);
            _singleRunExecuted = true;
            IsCompleted = true;
            StopReason = "Completed";
            return result;
        }

        public async Task<List<WallyRunResult>> ExecuteToCompletionAsync(CancellationToken cancellationToken = default)
        {
            while (!IsCompleted)
                await ExecuteNextAsync(cancellationToken).ConfigureAwait(false);
            return Results;
        }

        public ChatPanelPromptPreview BuildNextPromptPreview()
        {
            if (IsCompleted)
            {
                PendingExecution completed = new(
                    DisplayTitle: ResolvedRequest.Request.DisplayLabel,
                    ExecutionSummary: $"Execution complete after {Results.Count} result(s).",
                    ExecutionPrompt: string.Empty,
                    ActorLabel: ResolvedRequest.ActorLabel,
                    HistoryActorName: ResolvedRequest.DirectMode ? null : ResolvedRequest.ActorLabel,
                    StepName: null,
                    Iteration: Math.Max(0, Results.Count - 1),
                    SkipHistoryInjection: true,
                    DirectMode: ResolvedRequest.DirectMode,
                    RecordLoopName: ResolvedRequest.LoopDefinition?.IsAgentLoop == true ? ResolvedRequest.LoopDefinition.Name : null);

                return ChatPanelExecutionService.BuildPromptPreview(
                    _environment,
                    ResolvedRequest,
                    completed,
                    Results.Count,
                    StopReason,
                    isCompletionPreview: true);
            }

            PendingExecution pending = GetPendingExecution();
            return ChatPanelExecutionService.BuildPromptPreview(
                _environment,
                ResolvedRequest,
                pending,
                Results.Count,
                StopReason,
                isCompletionPreview: false);
        }

        public PendingExecution GetPendingExecution()
        {
            if (ResolvedRequest.LoopDefinition?.HasSteps == true)
            {
                if (_nextPipelineStepIndex >= ResolvedRequest.LoopDefinition.Steps.Count)
                    throw new InvalidOperationException("Pipeline execution is complete.");

                WallyStepDefinition step = ResolvedRequest.LoopDefinition.Steps[_nextPipelineStepIndex];
                string stepName = string.IsNullOrWhiteSpace(step.Name)
                    ? $"step-{_nextPipelineStepIndex + 1}"
                    : step.Name;
                string actorLabel = string.IsNullOrWhiteSpace(step.ActorName)
                    ? ResolvedRequest.ActorLabel
                    : step.ActorName;
                string prompt = step.BuildPrompt(_originalPrompt, _previousPipelineResult);
                return new PendingExecution(
                    DisplayTitle: stepName,
                    ExecutionSummary: $"Pipeline step {_nextPipelineStepIndex + 1}/{ResolvedRequest.LoopDefinition.Steps.Count}: {stepName} ({actorLabel})",
                    ExecutionPrompt: prompt,
                    ActorLabel: actorLabel,
                    HistoryActorName: actorLabel == "(no actor)" ? null : actorLabel,
                    StepName: stepName,
                    Iteration: 0,
                    SkipHistoryInjection: ResolvedRequest.Request.NoHistory,
                    DirectMode: actorLabel == "(no actor)",
                    RecordLoopName: null);
            }

            if (ResolvedRequest.LoopDefinition?.IsAgentLoop == true)
            {
                return new PendingExecution(
                    DisplayTitle: ResolvedRequest.LoopDefinition.Name,
                    ExecutionSummary: $"Agent iteration {_nextAgentIteration + 1}/{ResolvedRequest.LoopDefinition.MaxIterations} ({ResolvedRequest.ActorLabel})",
                    ExecutionPrompt: _currentAgentPrompt,
                    ActorLabel: ResolvedRequest.ActorLabel,
                    HistoryActorName: ResolvedRequest.DirectMode ? null : ResolvedRequest.ActorLabel,
                    StepName: null,
                    Iteration: _nextAgentIteration,
                    SkipHistoryInjection: ResolvedRequest.Request.NoHistory || _nextAgentIteration > 0,
                    DirectMode: ResolvedRequest.DirectMode,
                    RecordLoopName: ResolvedRequest.LoopDefinition.Name);
            }

            if (_singleRunExecuted)
                throw new InvalidOperationException("Single execution is complete.");

            return new PendingExecution(
                DisplayTitle: ResolvedRequest.Request.DisplayLabel,
                ExecutionSummary: $"Single run ({ResolvedRequest.ActorLabel})",
                ExecutionPrompt: ResolvedRequest.InitialPrompt,
                ActorLabel: ResolvedRequest.ActorLabel,
                HistoryActorName: ResolvedRequest.DirectMode ? null : ResolvedRequest.ActorLabel,
                StepName: null,
                Iteration: 0,
                SkipHistoryInjection: ResolvedRequest.Request.NoHistory,
                DirectMode: ResolvedRequest.DirectMode,
                RecordLoopName: null);
        }

        private string? EvaluateAgentStopReason(string response, int iteration)
        {
            WallyLoopDefinition loop = ResolvedRequest.LoopDefinition!;
            if (!string.IsNullOrWhiteSpace(loop.StopKeyword) &&
                response.Contains(loop.StopKeyword, StringComparison.OrdinalIgnoreCase))
            {
                return "StopKeyword";
            }

            if (!ActionBlockRegex.IsMatch(response))
                return "NoActions";

            if (iteration + 1 >= loop.MaxIterations)
                return "MaxIterations";

            return null;
        }

        internal sealed record PendingExecution(
            string DisplayTitle,
            string ExecutionSummary,
            string ExecutionPrompt,
            string ActorLabel,
            string? HistoryActorName,
            string? StepName,
            int Iteration,
            bool SkipHistoryInjection,
            bool DirectMode,
            string? RecordLoopName);
    }
}