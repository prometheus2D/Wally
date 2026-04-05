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
        private readonly WallyLoopExecutionState? _executionState;
        private string? _previousPipelineResult;
        private string? _previousNamedStepResult;
        private string _currentNamedStepName = string.Empty;
        private string _currentAgentPrompt;
        private int _nextPipelineStepIndex;
        private int _nextNamedIteration;
        private int _nextAgentIteration;
        private bool _singleRunExecuted;

        public ChatPanelExecutionSession(
            WallyEnvironment environment,
            ChatPanelResolvedRequest resolvedRequest,
            bool prepareExecutionState = true)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            ResolvedRequest = resolvedRequest ?? throw new ArgumentNullException(nameof(resolvedRequest));
            _originalPrompt = resolvedRequest.Request.DisplayPrompt;
            _currentAgentPrompt = resolvedRequest.InitialPrompt;

            if (ResolvedRequest.LoopDefinition != null &&
                WallyLoopExecutionStateStore.IsEnabled(ResolvedRequest.LoopDefinition))
            {
                _executionState = prepareExecutionState
                    ? WallyLoopExecutionStateStore.PrepareForRun(
                        _environment,
                        ResolvedRequest.LoopDefinition,
                        ResolvedRequest.Request.DisplayPrompt,
                        ResolvedRequest.InitialPrompt,
                        ResolveLoopExecutionMode(ResolvedRequest.LoopDefinition))
                    : LoadExistingExecutionState(ResolvedRequest.LoopDefinition);

                if (_executionState == null)
                    return;

                if (string.IsNullOrWhiteSpace(_originalPrompt) &&
                    !string.IsNullOrWhiteSpace(_executionState.OriginalRequest))
                {
                    _originalPrompt = _executionState.OriginalRequest;
                }

                if (ResolvedRequest.LoopDefinition.UsesNamedStepRouting)
                {
                    _currentNamedStepName = !string.IsNullOrWhiteSpace(_executionState.NextStepName)
                        ? _executionState.NextStepName
                        : ResolvedRequest.LoopDefinition.StartStepName;
                    _previousNamedStepResult = string.IsNullOrWhiteSpace(_executionState.PreviousStepResult)
                        ? null
                        : _executionState.PreviousStepResult;
                    _nextNamedIteration = Math.Max(0, _executionState.IterationCount);
                }
                else if (ResolvedRequest.LoopDefinition.HasSteps)
                {
                    _previousPipelineResult = string.IsNullOrWhiteSpace(_executionState.PreviousStepResult)
                        ? null
                        : _executionState.PreviousStepResult;

                    if (string.IsNullOrWhiteSpace(ResolvedRequest.Request.DisplayPrompt) &&
                        !string.IsNullOrWhiteSpace(_executionState.NextStepName))
                    {
                        _nextPipelineStepIndex = FindPipelineStepIndex(
                            ResolvedRequest.LoopDefinition,
                            _executionState.NextStepName);
                    }
                }
                else if (ResolvedRequest.LoopDefinition.IsAgentLoop)
                {
                    if (string.IsNullOrWhiteSpace(_currentAgentPrompt) &&
                        !string.IsNullOrWhiteSpace(_executionState.CurrentPrompt))
                    {
                        _currentAgentPrompt = _executionState.CurrentPrompt;
                    }

                    _nextAgentIteration = Math.Max(0, _executionState.IterationCount);
                }
            }
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

            if (ResolvedRequest.LoopDefinition?.UsesNamedStepRouting == true)
                return await ExecuteNextNamedStepAsync(pending, cancellationToken).ConfigureAwait(false);

            if (ResolvedRequest.LoopDefinition?.HasSteps == true)
                return await ExecuteNextPipelineStepAsync(pending, cancellationToken).ConfigureAwait(false);

            if (ResolvedRequest.LoopDefinition?.IsAgentLoop == true)
                return await ExecuteNextAgentIterationAsync(pending, cancellationToken).ConfigureAwait(false);

            return await ExecuteSingleRunAsync(pending, cancellationToken).ConfigureAwait(false);
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
                    RecordLoopName: ResolvedRequest.LoopDefinition?.IsAgentLoop == true ? ResolvedRequest.LoopDefinition.Name : null,
                    StepDefinition: null);

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
            if (ResolvedRequest.LoopDefinition?.UsesNamedStepRouting == true)
            {
                int maxIterations = WallyAgentLoop.ResolveMaxIterations(
                    ResolvedRequest.LoopDefinition,
                    ResolvedRequest.LoopDefinition.Steps.Count);
                if (_nextNamedIteration >= maxIterations)
                    throw new InvalidOperationException("Routed loop execution is complete.");

                WallyStepDefinition? step = ResolvedRequest.LoopDefinition.FindStep(_currentNamedStepName);
                if (step == null)
                    throw new InvalidOperationException(
                        $"Routed step '{_currentNamedStepName}' was not found in loop '{ResolvedRequest.LoopDefinition.Name}'.");

                string stepName = string.IsNullOrWhiteSpace(step.Name)
                    ? _currentNamedStepName
                    : step.Name;
                string actorLabel = WallyCommands.GetLoopStepExecutionLabel(ResolvedRequest.LoopDefinition, step);
                return new PendingExecution(
                    DisplayTitle: stepName,
                    ExecutionSummary: $"Routed step {_nextNamedIteration + 1}/{maxIterations}: {stepName} ({actorLabel})",
                    ExecutionPrompt: BuildStepPreviewText(step, _previousNamedStepResult),
                    ActorLabel: actorLabel,
                    HistoryActorName: actorLabel == "(no actor)" ? null : actorLabel,
                    StepName: stepName,
                    Iteration: _nextNamedIteration,
                    SkipHistoryInjection: ResolvedRequest.Request.NoHistory,
                    DirectMode: actorLabel == "(no actor)",
                    RecordLoopName: ResolvedRequest.LoopDefinition.Name,
                    StepDefinition: step);
            }

            if (ResolvedRequest.LoopDefinition?.HasSteps == true)
            {
                if (_nextPipelineStepIndex >= ResolvedRequest.LoopDefinition.Steps.Count)
                    throw new InvalidOperationException("Pipeline execution is complete.");

                WallyStepDefinition step = ResolvedRequest.LoopDefinition.Steps[_nextPipelineStepIndex];
                string stepName = string.IsNullOrWhiteSpace(step.Name)
                    ? $"step-{_nextPipelineStepIndex + 1}"
                    : step.Name;
                string actorLabel = WallyCommands.GetLoopStepExecutionLabel(ResolvedRequest.LoopDefinition, step);
                return new PendingExecution(
                    DisplayTitle: stepName,
                    ExecutionSummary: $"Pipeline step {_nextPipelineStepIndex + 1}/{ResolvedRequest.LoopDefinition.Steps.Count}: {stepName} ({actorLabel})",
                    ExecutionPrompt: BuildStepPreviewText(step, _previousPipelineResult),
                    ActorLabel: actorLabel,
                    HistoryActorName: actorLabel == "(no actor)" ? null : actorLabel,
                    StepName: stepName,
                    Iteration: _nextPipelineStepIndex,
                    SkipHistoryInjection: ResolvedRequest.Request.NoHistory,
                    DirectMode: actorLabel == "(no actor)",
                    RecordLoopName: ResolvedRequest.LoopDefinition.Name,
                    StepDefinition: step);
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
                    RecordLoopName: ResolvedRequest.LoopDefinition.Name,
                    StepDefinition: null);
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
                RecordLoopName: null,
                StepDefinition: null);
        }

        private async Task<WallyRunResult> ExecuteNextNamedStepAsync(PendingExecution pending, CancellationToken cancellationToken)
        {
            WallyLoopDefinition loopDef = ResolvedRequest.LoopDefinition!;
            WallyStepDefinition stepDef = pending.StepDefinition
                ?? throw new InvalidOperationException("No routed loop step is pending.");
            string stepName = pending.StepName ?? _currentNamedStepName;
            int maxIterations = WallyAgentLoop.ResolveMaxIterations(loopDef, loopDef.Steps.Count);

            WallyLoopExecutionStateStore.BeginStep(
                _environment,
                loopDef,
                _executionState,
                currentStepName: _currentNamedStepName,
                iterationCount: _nextNamedIteration,
                currentPrompt: _originalPrompt,
                previousStepResult: _previousNamedStepResult);

            WallyLoopStepExecutionOutcome stepResult = await WallyCommands.ExecuteLoopStepAsync(
                _environment,
                _originalPrompt,
                _previousNamedStepResult,
                loopDef,
                stepDef,
                ResolvedRequest.ResolvedModel,
                ResolvedRequest.ResolvedWrapperName,
                ResolvedRequest.Request.NoHistory,
                cancellationToken,
                _nextNamedIteration + 1).ConfigureAwait(false);

            WallyLoopContinuationDecision decision = WallyCommands.ResolveNamedStepDecision(
                loopDef,
                stepDef,
                stepName,
                stepResult,
                _nextNamedIteration + 1,
                maxIterations);

            string? stopReason = decision.StopReason;
            if (string.Equals(decision.Status, "WaitingForUser", StringComparison.OrdinalIgnoreCase))
            {
                WallyLoopExecutionStateStore.PauseForUser(
                    _environment,
                    loopDef,
                    _executionState,
                    currentStepName: stepName,
                    nextStepName: decision.NextStepName,
                    iterationCount: _nextNamedIteration + 1,
                    currentPrompt: _originalPrompt,
                    previousStepResult: stepResult.Response,
                    stopReason: stopReason);
            }
            else if (string.Equals(decision.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                WallyLoopExecutionStateStore.CompleteRun(
                    _environment,
                    loopDef,
                    _executionState,
                    currentStepName: stepName,
                    iterationCount: _nextNamedIteration + 1,
                    currentPrompt: _originalPrompt,
                    previousStepResult: stepResult.Response,
                    stopReason: stopReason);
            }
            else if (string.Equals(decision.Status, "Stopped", StringComparison.OrdinalIgnoreCase))
            {
                WallyLoopExecutionStateStore.StopRun(
                    _environment,
                    loopDef,
                    _executionState,
                    currentStepName: stepName,
                    nextStepName: decision.NextStepName,
                    iterationCount: _nextNamedIteration + 1,
                    currentPrompt: _originalPrompt,
                    previousStepResult: stepResult.Response,
                    stopReason: stopReason);
            }

            var result = new WallyRunResult
            {
                StepName = stepName,
                ActorName = stepResult.ActorLabel,
                Response = stepResult.Response,
                Iteration = _nextNamedIteration,
                StopReason = stopReason
            };

            Results.Add(result);

            if (stopReason != null)
            {
                StopReason = stopReason;
                IsCompleted = true;
                return result;
            }

            _previousNamedStepResult = stepResult.Response;
            _currentNamedStepName = decision.NextStepName;
            _nextNamedIteration++;
            WallyLoopExecutionStateStore.ContinueToNextStep(
                _environment,
                loopDef,
                _executionState,
                currentStepName: stepName,
                nextStepName: _currentNamedStepName,
                iterationCount: _nextNamedIteration,
                currentPrompt: _originalPrompt,
                previousStepResult: _previousNamedStepResult);
            return result;
        }

        private async Task<WallyRunResult> ExecuteNextPipelineStepAsync(PendingExecution pending, CancellationToken cancellationToken)
        {
            WallyLoopDefinition loopDef = ResolvedRequest.LoopDefinition!;
            WallyStepDefinition stepDef = pending.StepDefinition
                ?? throw new InvalidOperationException("No pipeline step is pending.");
            string stepName = pending.StepName ?? WallyLoopExecutionStateStore.GetStableStepName(stepDef, _nextPipelineStepIndex);

            WallyLoopExecutionStateStore.BeginStep(
                _environment,
                loopDef,
                _executionState,
                currentStepName: stepName,
                iterationCount: _nextPipelineStepIndex,
                currentPrompt: _originalPrompt,
                previousStepResult: _previousPipelineResult);

            WallyLoopStepExecutionOutcome stepResult = await WallyCommands.ExecuteLoopStepAsync(
                _environment,
                _originalPrompt,
                _previousPipelineResult,
                loopDef,
                stepDef,
                ResolvedRequest.ResolvedModel,
                ResolvedRequest.ResolvedWrapperName,
                ResolvedRequest.Request.NoHistory,
                cancellationToken,
                _nextPipelineStepIndex + 1).ConfigureAwait(false);

            var result = new WallyRunResult
            {
                StepName = stepName,
                ActorName = stepResult.ActorLabel,
                Response = stepResult.Response,
                Iteration = _nextPipelineStepIndex,
                StopReason = stepResult.RequestsPause ? stepResult.StopReason ?? "WaitingForUser" : null
            };

            Results.Add(result);

            if (stepResult.RequestsPause)
            {
                string nextStepName = _nextPipelineStepIndex + 1 < loopDef.Steps.Count
                    ? WallyLoopExecutionStateStore.GetStableStepName(loopDef.Steps[_nextPipelineStepIndex + 1], _nextPipelineStepIndex + 1)
                    : string.Empty;
                StopReason = stepResult.StopReason ?? "WaitingForUser";
                IsCompleted = true;
                WallyLoopExecutionStateStore.PauseForUser(
                    _environment,
                    loopDef,
                    _executionState,
                    currentStepName: stepName,
                    nextStepName: nextStepName,
                    iterationCount: _nextPipelineStepIndex + 1,
                    currentPrompt: _originalPrompt,
                    previousStepResult: stepResult.Response,
                    stopReason: StopReason);
                return result;
            }

            _previousPipelineResult = stepResult.Response;
            _nextPipelineStepIndex++;

            if (_nextPipelineStepIndex >= loopDef.Steps.Count)
            {
                StopReason = "PipelineComplete";
                IsCompleted = true;
                WallyLoopExecutionStateStore.CompleteRun(
                    _environment,
                    loopDef,
                    _executionState,
                    currentStepName: string.Empty,
                    iterationCount: Results.Count,
                    currentPrompt: _originalPrompt,
                    previousStepResult: _previousPipelineResult,
                    stopReason: "Completed");
                return result;
            }

            string upcomingStep = WallyLoopExecutionStateStore.GetStableStepName(
                loopDef.Steps[_nextPipelineStepIndex],
                _nextPipelineStepIndex);
            WallyLoopExecutionStateStore.ContinueToNextStep(
                _environment,
                loopDef,
                _executionState,
                currentStepName: stepName,
                nextStepName: upcomingStep,
                iterationCount: _nextPipelineStepIndex,
                currentPrompt: _originalPrompt,
                previousStepResult: _previousPipelineResult);
            return result;
        }

        private async Task<WallyRunResult> ExecuteNextAgentIterationAsync(PendingExecution pending, CancellationToken cancellationToken)
        {
            WallyLoopDefinition loopDef = ResolvedRequest.LoopDefinition!;

            WallyLoopExecutionStateStore.BeginStep(
                _environment,
                loopDef,
                _executionState,
                currentStepName: string.Empty,
                iterationCount: _nextAgentIteration,
                currentPrompt: _currentAgentPrompt,
                previousStepResult: string.Empty,
                mode: "agent-loop");

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

            string? stopReason = EvaluateAgentStopReason(response, _nextAgentIteration);
            var result = new WallyRunResult
            {
                StepName = null,
                ActorName = pending.ActorLabel,
                Response = response,
                Iteration = pending.Iteration,
                StopReason = stopReason
            };

            Results.Add(result);

            if (stopReason != null)
            {
                StopReason = stopReason;
                IsCompleted = true;
                if (stopReason == "MaxIterations")
                {
                    WallyLoopExecutionStateStore.StopRun(
                        _environment,
                        loopDef,
                        _executionState,
                        currentStepName: string.Empty,
                        nextStepName: string.Empty,
                        iterationCount: _nextAgentIteration + 1,
                        currentPrompt: _currentAgentPrompt,
                        previousStepResult: response,
                        stopReason: stopReason,
                        mode: "agent-loop");
                }
                else
                {
                    WallyLoopExecutionStateStore.CompleteRun(
                        _environment,
                        loopDef,
                        _executionState,
                        currentStepName: string.Empty,
                        iterationCount: _nextAgentIteration + 1,
                        currentPrompt: _currentAgentPrompt,
                        previousStepResult: response,
                        stopReason: stopReason,
                        mode: "agent-loop");
                }
                return result;
            }

            _currentAgentPrompt = ChatPanelExecutionService.CombineAgentPrompt(
                loopDef,
                _originalPrompt,
                response,
                _nextAgentIteration);
            _nextAgentIteration++;

            WallyLoopExecutionStateStore.ContinueToNextStep(
                _environment,
                loopDef,
                _executionState,
                currentStepName: string.Empty,
                nextStepName: string.Empty,
                iterationCount: _nextAgentIteration,
                currentPrompt: _currentAgentPrompt,
                previousStepResult: response,
                mode: "agent-loop");
            return result;
        }

        private async Task<WallyRunResult> ExecuteSingleRunAsync(PendingExecution pending, CancellationToken cancellationToken)
        {
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

            Results.Add(result);
            _singleRunExecuted = true;
            IsCompleted = true;
            StopReason = "Completed";
            return result;
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

        private static string ResolveLoopExecutionMode(WallyLoopDefinition loopDef)
        {
            if (loopDef.UsesNamedStepRouting)
                return "routed-loop";

            if (loopDef.HasSteps)
                return "pipeline";

            if (loopDef.IsAgentLoop)
                return "agent-loop";

            return "single-shot";
        }

        private static int FindPipelineStepIndex(WallyLoopDefinition loopDef, string stepName)
        {
            for (int i = 0; i < loopDef.Steps.Count; i++)
            {
                string stableName = WallyLoopExecutionStateStore.GetStableStepName(loopDef.Steps[i], i);
                if (string.Equals(stableName, stepName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 0;
        }

        private WallyLoopExecutionState? LoadExistingExecutionState(WallyLoopDefinition loopDef)
        {
            return WallyLoopExecutionStateStore.TryLoadCurrent(_environment, loopDef, out WallyLoopExecutionState? state)
                ? state
                : null;
        }

        private string BuildStepPreviewText(WallyStepDefinition stepDef, string? previousStepResult)
        {
            return stepDef.EffectiveKind.ToLowerInvariant() switch
            {
                "prompt" => stepDef.BuildPrompt(_originalPrompt, previousStepResult),
                "user_input" => stepDef.BuildPrompt(_originalPrompt, previousStepResult),
                "shell" => string.IsNullOrWhiteSpace(stepDef.CommandTemplate)
                    ? stepDef.BuildPrompt(_originalPrompt, previousStepResult)
                    : stepDef.CommandTemplate,
                "command" => string.IsNullOrWhiteSpace(stepDef.CommandTemplate)
                    ? stepDef.BuildPrompt(_originalPrompt, previousStepResult)
                    : stepDef.CommandTemplate,
                "code" => string.IsNullOrWhiteSpace(stepDef.HandlerName)
                    ? "(code step)"
                    : $"handler: {stepDef.HandlerName}",
                _ => stepDef.BuildPrompt(_originalPrompt, previousStepResult)
            };
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
            string? RecordLoopName,
            WallyStepDefinition? StepDefinition);
    }
}