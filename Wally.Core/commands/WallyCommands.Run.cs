using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wally.Core.Actors;
using Wally.Core.Mailbox;

namespace Wally.Core
{
    public static partial class WallyCommands
    {
        // ?? Run ???????????????????????????????????????????????????????????????

        /// <summary>
        /// Backward-compatible overload � returns raw response strings.
        /// </summary>
        public static List<string> HandleRun(
            WallyEnvironment env,
            string prompt,
            string? actorName  = null,
            string? model      = null,
            string? loopName   = null,
            string? wrapper    = null,
            bool noHistory     = false,
            CancellationToken cancellationToken = default)
            => WallyRunResult.ToStringList(
                HandleRunTyped(env, prompt, actorName, model, loopName, wrapper, noHistory, cancellationToken));

        /// <summary>
        /// Synchronous wrapper � delegates to <see cref="HandleRunTypedAsync"/>.
        /// </summary>
        public static List<WallyRunResult> HandleRunTyped(
            WallyEnvironment env,
            string prompt,
            string? actorName  = null,
            string? model      = null,
            string? loopName   = null,
            string? wrapper    = null,
            bool noHistory     = false,
            CancellationToken cancellationToken = default,
            TextWriter? output = null)
            => HandleRunTypedAsync(env, prompt, actorName, model, loopName, wrapper, noHistory,
                    cancellationToken, output)
                .GetAwaiter().GetResult();

        /// <summary>
        /// Executes a prompt through the pipeline defined by the selected loop definition
        /// (or a direct single-actor call when no loop is specified).
        /// Returns one <see cref="WallyRunResult"/> per step executed.
        /// <para>
        /// The optional <paramref name="output"/> writer is used by the WallyScript
        /// <c>parallel</c> block to buffer per-branch console output. When
        /// <see langword="null"/> all output goes to <see cref="Console.Out"/>.
        /// </para>
        /// </summary>
        public static async Task<List<WallyRunResult>> HandleRunTypedAsync(
            WallyEnvironment env,
            string prompt,
            string? actorName  = null,
            string? model      = null,
            string? loopName   = null,
            string? wrapper    = null,
            bool noHistory     = false,
            CancellationToken cancellationToken = default,
            TextWriter? output = null)
        {
            var out_ = output ?? Console.Out;
            if (RequireWorkspace(env, "run") == null) return new List<WallyRunResult>();

            // Resolve loop definition
            WallyLoopDefinition? loopDef = null;
            if (!string.IsNullOrWhiteSpace(loopName))
            {
                loopDef = env.GetLoop(loopName!);
                if (loopDef == null)
                {
                    await out_.WriteLineAsync($"Loop '{loopName}' not found. Available loops:").ConfigureAwait(false);
                    foreach (var l in env.Loops)
                        await out_.WriteLineAsync($"  {l.Name} \u2014 {l.Description}").ConfigureAwait(false);
                    env.Logger.LogError($"Loop '{loopName}' not found.", "run");
                    return new List<WallyRunResult>();
                }
            }

            string loopLabel = loopDef != null ? $"[run:{loopDef.Name}]" : "[run]";

            // Named-step routed loop
            if (loopDef?.UsesNamedStepRouting == true)
                return await RunNamedStepLoopAsync(env, prompt, loopDef, loopLabel, model, wrapper,
                    noHistory, cancellationToken, out_).ConfigureAwait(false);

            // Multi-step pipeline
            if (loopDef?.HasSteps == true)
                return await RunPipelineAsync(env, prompt, loopDef, loopLabel, model, wrapper,
                    noHistory, cancellationToken, out_).ConfigureAwait(false);

            // Single-actor / direct
            if (string.IsNullOrWhiteSpace(actorName) && !string.IsNullOrWhiteSpace(loopDef?.ActorName))
                actorName = loopDef!.ActorName;

            bool directMode = string.IsNullOrWhiteSpace(actorName);
            Actor? actor = null;
            if (!directMode)
            {
                actor = env.GetActor(actorName!);
                if (actor == null)
                {
                    await out_.WriteLineAsync($"Actor '{actorName}' not found.").ConfigureAwait(false);
                    env.Logger.LogError($"Actor '{actorName}' not found.", "run");
                    return new List<WallyRunResult>();
                }
            }

            string actorLabel = directMode ? "(no actor)" : actorName!;

            // Resolve the prompt � apply StartPrompt template if defined.
            string resolvedPrompt = (loopDef != null && !string.IsNullOrWhiteSpace(loopDef.StartPrompt))
                ? loopDef.StartPrompt.Replace("{userPrompt}", prompt)
                : prompt;

            // Agent loop routing � when loop defines MaxIterations > 0
            if (loopDef?.IsAgentLoop == true)
            {
                return await env.RunAgentLoopAsync(
                    loopDef, actor, resolvedPrompt, model, wrapper,
                    noHistory, cancellationToken, out_).ConfigureAwait(false);
            }

            env.Logger.LogCommand("run", $"Actor='{actorLabel}' loop='{loopDef?.Name ?? "(none)"}' model='{model ?? "(default)"}' wrapper='{wrapper ?? "(default)"}'");
            env.Logger.LogPrompt(actorLabel, resolvedPrompt, model ?? env.Workspace!.Config.DefaultModel);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            string response = directMode
                ? await env.ExecutePromptAsync(resolvedPrompt, model, wrapper, skipHistory: noHistory,
                    cancellationToken: cancellationToken).ConfigureAwait(false)
                : await env.ExecuteActorAsync(actor!, resolvedPrompt, model, wrapper,
                    skipHistory: noHistory, cancellationToken: cancellationToken).ConfigureAwait(false);
            sw.Stop();
            env.Logger.LogResponse(actorLabel, response, sw.ElapsedMilliseconds, 1);

            await out_.WriteLineAsync(response).ConfigureAwait(false);
            await out_.WriteLineAsync().ConfigureAwait(false);

            return new List<WallyRunResult>
            {
                new WallyRunResult { StepName = null, ActorName = actorLabel, Response = response }
            };
        }

        // ?? Pipeline execution ????????????????????????????????????????????????

        private static List<WallyRunResult> RunPipeline(
            WallyEnvironment env, string prompt, WallyLoopDefinition loopDef, string loopLabel,
            string? model, string? wrapper, bool noHistory, CancellationToken cancellationToken = default)
            => RunPipelineAsync(env, prompt, loopDef, loopLabel, model, wrapper, noHistory,
                cancellationToken, Console.Out).GetAwaiter().GetResult();

        private static async Task<List<WallyRunResult>> RunPipelineAsync(
            WallyEnvironment env, string prompt, WallyLoopDefinition loopDef, string loopLabel,
            string? model, string? wrapper, bool noHistory,
            CancellationToken cancellationToken, TextWriter out_)
        {
            var stepDefs = loopDef.Steps;
            env.Logger.LogCommand("run", $"[pipeline] loop='{loopDef.Name}' steps={stepDefs.Count} model='{model ?? "(default)"}' wrapper='{wrapper ?? "(default)"}'");
            await out_.WriteLineAsync($"{loopLabel} Pipeline \u2014 {stepDefs.Count} step(s)").ConfigureAwait(false);
            await out_.WriteLineAsync().ConfigureAwait(false);

            for (int i = 0; i < stepDefs.Count; i++)
            {
                var stepDef = stepDefs[i];
                string stepName = string.IsNullOrWhiteSpace(stepDef.Name) ? $"step-{i + 1}" : stepDef.Name;
                string stepLabel = GetStepExecutionLabel(loopDef, stepDef);
                await out_.WriteLineAsync($"  Step {i + 1}: [{stepName}]  Kind: {stepDef.EffectiveKind}  Target: {stepLabel}").ConfigureAwait(false);
            }
            await out_.WriteLineAsync().ConfigureAwait(false);

            // Run steps in order
            var results = new List<WallyRunResult>(stepDefs.Count);
            string? previousStepResult = null;

            for (int i = 0; i < stepDefs.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var stepDef = stepDefs[i];
                string stepName   = string.IsNullOrWhiteSpace(stepDef.Name) ? $"step-{i + 1}" : stepDef.Name;
                string stepLabel = GetStepExecutionLabel(loopDef, stepDef);

                await out_.WriteLineAsync($"--- Step {i + 1}: {stepName} [{stepDef.EffectiveKind}] ({stepLabel}) ---").ConfigureAwait(false);

                StepExecutionResult stepResult;
                try
                {
                    stepResult = await ExecuteStepAsync(
                        env,
                        prompt,
                        previousStepResult,
                        loopDef,
                        stepDef,
                        model,
                        wrapper,
                        noHistory,
                        cancellationToken,
                        i + 1)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    string message = $"{loopLabel} Step '{stepName}' failed: {ex.Message}";
                    await out_.WriteLineAsync(message).ConfigureAwait(false);
                    env.Logger.LogError(message, "run");
                    return new List<WallyRunResult>();
                }

                if (!string.IsNullOrWhiteSpace(stepResult.Response))
                    await out_.WriteLineAsync(stepResult.Response).ConfigureAwait(false);
                await out_.WriteLineAsync().ConfigureAwait(false);

                results.Add(new WallyRunResult { StepName = stepName, ActorName = stepResult.ActorLabel, Response = stepResult.Response });

                if (stepResult.RequestsPause)
                {
                    await out_.WriteLineAsync($"{loopLabel} Pipeline paused - waiting for user input at step '{stepName}'.").ConfigureAwait(false);
                    env.Logger.LogInfo($"Pipeline '{loopDef.Name}' paused: waiting for user input at step '{stepName}'.");
                    return results;
                }

                previousStepResult = stepResult.Response;
            }

            await out_.WriteLineAsync($"{loopLabel} Pipeline complete \u2014 {results.Count} step(s).").ConfigureAwait(false);
            env.Logger.LogInfo($"Pipeline '{loopDef.Name}' complete: {results.Count} step(s).");
            return results;
        }

        private static async Task<List<WallyRunResult>> RunNamedStepLoopAsync(
            WallyEnvironment env, string prompt, WallyLoopDefinition loopDef, string loopLabel,
            string? model, string? wrapper, bool noHistory,
            CancellationToken cancellationToken, TextWriter out_)
        {
            int maxIterations = WallyAgentLoop.ResolveMaxIterations(loopDef, loopDef.Steps.Count);
            string currentStepName = loopDef.StartStepName;
            string? previousStepResult = null;
            string? stopReason = null;
            var results = new List<WallyRunResult>();

            env.Logger.LogCommand("run", $"[named-step-loop] loop='{loopDef.Name}' start='{currentStepName}' maxIterations={maxIterations} model='{model ?? "(default)"}' wrapper='{wrapper ?? "(default)"}'");
            await out_.WriteLineAsync($"{loopLabel} Routed loop - startStep='{currentStepName}', maxIterations={maxIterations}").ConfigureAwait(false);
            await out_.WriteLineAsync().ConfigureAwait(false);

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stepDef = loopDef.FindStep(currentStepName);
                if (stepDef == null)
                {
                    string message = $"{loopLabel} Routed step '{currentStepName}' was not found in loop '{loopDef.Name}'.";
                    await out_.WriteLineAsync(message).ConfigureAwait(false);
                    env.Logger.LogError(message, "run");
                    return new List<WallyRunResult>();
                }

                string stepName = string.IsNullOrWhiteSpace(stepDef.Name) ? currentStepName : stepDef.Name;
                string stepLabel = GetStepExecutionLabel(loopDef, stepDef);
                await out_.WriteLineAsync($"--- Iteration {iteration + 1}/{maxIterations}: {stepName} [{stepDef.EffectiveKind}] ({stepLabel}) ---").ConfigureAwait(false);

                StepExecutionResult stepResult;
                try
                {
                    stepResult = await ExecuteStepAsync(
                        env,
                        prompt,
                        previousStepResult,
                        loopDef,
                        stepDef,
                        model,
                        wrapper,
                        noHistory,
                        cancellationToken,
                        iteration + 1)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    string message = $"{loopLabel} Step '{stepName}' failed: {ex.Message}";
                    await out_.WriteLineAsync(message).ConfigureAwait(false);
                    env.Logger.LogError(message, "run");
                    return new List<WallyRunResult>();
                }

                if (!string.IsNullOrWhiteSpace(stepResult.Response))
                    await out_.WriteLineAsync(stepResult.Response).ConfigureAwait(false);
                await out_.WriteLineAsync().ConfigureAwait(false);

                var routeMatch = stepResult.RequestsPause
                    ? (WallyStepRouteMatch?)null
                    : WallyAgentLoop.ResolveKeywordRoute(stepDef.KeywordRoutes, stepResult.Response);
                string? nextStepName = routeMatch?.NextStepName;
                bool usedDefaultRoute = false;

                if (!stepResult.RequestsPause && string.IsNullOrWhiteSpace(nextStepName) && !string.IsNullOrWhiteSpace(stepDef.DefaultNextStep))
                {
                    nextStepName = stepDef.DefaultNextStep;
                    usedDefaultRoute = true;
                }

                if (!stepResult.RequestsPause && !string.IsNullOrWhiteSpace(nextStepName) && loopDef.FindStep(nextStepName) == null)
                {
                    string message = $"{loopLabel} Step '{stepName}' routed to unknown step '{nextStepName}'.";
                    await out_.WriteLineAsync(message).ConfigureAwait(false);
                    env.Logger.LogError(message, "run");
                    return new List<WallyRunResult>();
                }

                bool stopForPause = stepResult.RequestsPause;
                bool stopForKeyword = !stopForPause && WallyAgentLoop.ContainsStopKeyword(stepResult.Response, loopDef.StopKeyword);
                bool stopForNoRoute = !stopForPause && string.IsNullOrWhiteSpace(nextStepName);
                bool stopForMaxIterations = !stopForPause && !stopForKeyword && !stopForNoRoute && iteration + 1 >= maxIterations;

                if (stopForPause)
                {
                    stopReason = stepResult.StopReason ?? "WaitingForUser";
                }
                else if (stopForKeyword)
                {
                    stopReason = "StopKeyword";
                }
                else if (stopForNoRoute)
                {
                    stopReason = "NoRoute";
                }
                else if (stopForMaxIterations)
                {
                    stopReason = "MaxIterations";
                }

                results.Add(new WallyRunResult
                {
                    StepName = stepName,
                    ActorName = stepResult.ActorLabel,
                    Response = stepResult.Response,
                    Iteration = iteration,
                    StopReason = stopReason
                });

                if (stopForPause)
                {
                    await out_.WriteLineAsync($"{loopLabel} Paused - waiting for user input at step '{stepName}'.").ConfigureAwait(false);
                    env.Logger.LogInfo($"Named-step loop '{loopDef.Name}' paused: waiting for user input at step '{stepName}'.");
                    break;
                }

                if (stopForKeyword)
                {
                    await out_.WriteLineAsync($"{loopLabel} Stopped - stop keyword '{loopDef.StopKeyword}' detected at step '{stepName}'.").ConfigureAwait(false);
                    env.Logger.LogInfo($"Named-step loop '{loopDef.Name}' stopped: stop keyword '{loopDef.StopKeyword}' at step '{stepName}'.");
                    break;
                }

                if (stopForNoRoute)
                {
                    await out_.WriteLineAsync($"{loopLabel} Complete - step '{stepName}' produced no matching route and no default next step.").ConfigureAwait(false);
                    env.Logger.LogInfo($"Named-step loop '{loopDef.Name}' complete: step '{stepName}' ended without a matching route.");
                    break;
                }

                if (stopForMaxIterations)
                {
                    await out_.WriteLineAsync($"{loopLabel} Stopped - MaxIterations ({maxIterations}) reached before routing to '{nextStepName}'.").ConfigureAwait(false);
                    env.Logger.LogInfo($"Named-step loop '{loopDef.Name}' stopped: MaxIterations ({maxIterations}) reached.");
                    break;
                }

                if (routeMatch != null)
                {
                    await out_.WriteLineAsync($"{loopLabel} Route - keyword '{routeMatch.Value.Keyword}' -> '{nextStepName}'.").ConfigureAwait(false);
                }
                else if (usedDefaultRoute)
                {
                    await out_.WriteLineAsync($"{loopLabel} Route - default -> '{nextStepName}'.").ConfigureAwait(false);
                }

                await out_.WriteLineAsync().ConfigureAwait(false);
                previousStepResult = stepResult.Response;
                currentStepName = nextStepName!;
                stopReason = null;
            }

            await out_.WriteLineAsync($"{loopLabel} Routed loop complete - {results.Count} step(s), stopReason={stopReason ?? "unknown"}.").ConfigureAwait(false);
            env.Logger.LogInfo($"Named-step loop '{loopDef.Name}' complete: {results.Count} step(s), stopReason={stopReason ?? "unknown"}.");
            return results;
        }

        private sealed class StepExecutionResult
        {
            public string ActorLabel { get; init; } = "(no actor)";
            public string Response { get; init; } = string.Empty;
            public bool RequestsPause { get; init; }
            public string? StopReason { get; init; }
        }

        private static string GetStepExecutionLabel(WallyLoopDefinition loopDef, WallyStepDefinition stepDef)
        {
            return stepDef.EffectiveKind.ToLowerInvariant() switch
            {
                "prompt" => ResolvePromptActorLabel(loopDef, stepDef),
                "shell" => "shell",
                "command" => "wally-command",
                "code" => string.IsNullOrWhiteSpace(stepDef.HandlerName) ? "code" : $"code:{stepDef.HandlerName}",
                "user_input" => "user-input",
                _ => stepDef.EffectiveKind
            };
        }

        private static string ResolvePromptActorLabel(WallyLoopDefinition loopDef, WallyStepDefinition stepDef)
        {
            string resolvedActorName = !string.IsNullOrWhiteSpace(stepDef.ActorName)
                ? stepDef.ActorName
                : loopDef.ActorName;

            return string.IsNullOrWhiteSpace(resolvedActorName) ? "(no actor)" : resolvedActorName;
        }

        private static async Task<StepExecutionResult> ExecuteStepAsync(
            WallyEnvironment env,
            string userPrompt,
            string? previousStepResult,
            WallyLoopDefinition loopDef,
            WallyStepDefinition stepDef,
            string? model,
            string? wrapper,
            bool noHistory,
            CancellationToken cancellationToken,
            int stepIndex)
        {
            return stepDef.EffectiveKind.ToLowerInvariant() switch
            {
                "prompt" => await ExecutePromptStepAsync(env, userPrompt, previousStepResult, loopDef, stepDef,
                    model, wrapper, noHistory, cancellationToken, stepIndex).ConfigureAwait(false),
                "shell" => await ExecuteShellStepAsync(env, userPrompt, previousStepResult, stepDef,
                    cancellationToken).ConfigureAwait(false),
                "command" => await ExecuteCommandStepAsync(env, userPrompt, previousStepResult, stepDef,
                    cancellationToken).ConfigureAwait(false),
                "code" => await ExecuteCodeStepAsync(env, userPrompt, previousStepResult, stepDef,
                    cancellationToken).ConfigureAwait(false),
                "user_input" => await ExecuteUserInputStepAsync(env, userPrompt, previousStepResult, loopDef, stepDef,
                    model, wrapper, noHistory, cancellationToken, stepIndex).ConfigureAwait(false),
                _ => throw new NotSupportedException($"Step kind '{stepDef.EffectiveKind}' is not supported.")
            };
        }

        private static async Task<StepExecutionResult> ExecutePromptStepAsync(
            WallyEnvironment env,
            string userPrompt,
            string? previousStepResult,
            WallyLoopDefinition loopDef,
            WallyStepDefinition stepDef,
            string? model,
            string? wrapper,
            bool noHistory,
            CancellationToken cancellationToken,
            int stepIndex)
        {
            string actorLabel = ResolvePromptActorLabel(loopDef, stepDef);
            string stepPrompt = await env.BuildStepPromptAsync(stepDef, userPrompt, previousStepResult, cancellationToken)
                .ConfigureAwait(false);

            env.Logger.LogPrompt(actorLabel, stepPrompt, model ?? env.Workspace!.Config.DefaultModel);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            string response;
            if (actorLabel == "(no actor)")
            {
                response = await env.ExecutePromptAsync(
                    stepPrompt,
                    model,
                    wrapper,
                    loopName: loopDef.Name,
                    iteration: stepIndex - 1,
                    skipHistory: noHistory,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                Actor? actor = env.GetActor(actorLabel);
                if (actor == null)
                    throw new InvalidOperationException($"Actor '{actorLabel}' not found.");

                response = await env.ExecuteActorAsync(
                    actor,
                    stepPrompt,
                    model,
                    wrapper,
                    loopName: loopDef.Name,
                    iteration: stepIndex - 1,
                    skipHistory: noHistory,
                    cancellationToken: cancellationToken,
                    stepDefinition: stepDef)
                    .ConfigureAwait(false);
            }

            sw.Stop();
            env.Logger.LogResponse(actorLabel, response, sw.ElapsedMilliseconds, stepIndex);
            return new StepExecutionResult { ActorLabel = actorLabel, Response = response };
        }

        private static async Task<StepExecutionResult> ExecuteUserInputStepAsync(
            WallyEnvironment env,
            string userPrompt,
            string? previousStepResult,
            WallyLoopDefinition loopDef,
            WallyStepDefinition stepDef,
            string? model,
            string? wrapper,
            bool noHistory,
            CancellationToken cancellationToken,
            int stepIndex)
        {
            string actorLabel = ResolvePromptActorLabel(loopDef, stepDef);
            string stepPrompt = await BuildUserInputPromptAsync(env, stepDef, userPrompt, previousStepResult, cancellationToken)
                .ConfigureAwait(false);

            env.Logger.LogPrompt(actorLabel, stepPrompt, model ?? env.Workspace!.Config.DefaultModel);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            string response;
            if (actorLabel == "(no actor)")
            {
                response = await env.ExecutePromptAsync(
                    stepPrompt,
                    model,
                    wrapper,
                    loopName: loopDef.Name,
                    iteration: stepIndex - 1,
                    skipHistory: noHistory,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                Actor? actor = env.GetActor(actorLabel);
                if (actor == null)
                    throw new InvalidOperationException($"Actor '{actorLabel}' not found.");

                response = await env.ExecuteActorAsync(
                    actor,
                    stepPrompt,
                    model,
                    wrapper,
                    loopName: loopDef.Name,
                    iteration: stepIndex - 1,
                    skipHistory: noHistory,
                    cancellationToken: cancellationToken,
                    stepDefinition: stepDef)
                    .ConfigureAwait(false);
            }

            sw.Stop();
            env.Logger.LogResponse(actorLabel, response, sw.ElapsedMilliseconds, stepIndex);

            env.EnsureStepWriteAllowed(stepDef, InvestigationInteractionStore.DefaultInteractionStatePath, stepDef.Name);
            env.EnsureStepWriteAllowed(stepDef, InvestigationInteractionStore.DefaultUserResponsesPath, stepDef.Name);

            List<InvestigationInteractionQuestion> questions = InvestigationInteractionStore.ParseQuestionBatchMarkdown(response);
            InvestigationInteractionState waitingState = InvestigationInteractionStore.PersistWaitingQuestions(
                env,
                questions,
                responseTarget: InvestigationInteractionStore.DefaultUserResponsesPath,
                resumeCommand: $"wally run \"<answer batch>\" -l {loopDef.Name}");

            string summary = string.IsNullOrWhiteSpace(response)
                ? $"WAITING_FOR_USER\nRecorded question batch {waitingState.QuestionBatchId} for investigation {waitingState.InvestigationId}."
                : $"{response.TrimEnd()}\n\nWAITING_FOR_USER\nRecorded question batch {waitingState.QuestionBatchId} for investigation {waitingState.InvestigationId}.";

            return new StepExecutionResult
            {
                ActorLabel = actorLabel,
                Response = summary,
                RequestsPause = true,
                StopReason = "WaitingForUser"
            };
        }

        private static async Task<string> BuildUserInputPromptAsync(
            WallyEnvironment env,
            WallyStepDefinition stepDef,
            string userPrompt,
            string? previousStepResult,
            CancellationToken cancellationToken)
        {
            string prompt = await env.BuildStepPromptAsync(stepDef, userPrompt, previousStepResult, cancellationToken)
                .ConfigureAwait(false);

            return prompt + "\n\nReturn only the pending question batch in markdown using this exact structure for each question:\n### Q-001\n- Text: <question text shown to the user>\n- Reason: <why the answer is needed>\n- ExpectedAnswerShape: free_text|bullet_list|yes_no|short_text\n\nRepeat one question block per question. Do not add any prose before or after the question blocks.";
        }

        private static async Task<StepExecutionResult> ExecuteShellStepAsync(
            WallyEnvironment env,
            string userPrompt,
            string? previousStepResult,
            WallyStepDefinition stepDef,
            CancellationToken cancellationToken)
        {
            string command = await env.BuildStepCommandTextAsync(stepDef, userPrompt, previousStepResult, cancellationToken)
                .ConfigureAwait(false);
            string workDir = env.WorkSource ?? Directory.GetCurrentDirectory();

            var result = ExecuteShellCommand(command, workDir);
            string response = BuildShellStepResponse(command, result);

            if (!result.Success && !stepDef.ContinueOnFailure)
                throw new InvalidOperationException(response);

            return new StepExecutionResult { ActorLabel = "shell", Response = response };
        }

        private static async Task<StepExecutionResult> ExecuteCommandStepAsync(
            WallyEnvironment env,
            string userPrompt,
            string? previousStepResult,
            WallyStepDefinition stepDef,
            CancellationToken cancellationToken)
        {
            string commandLine = await env.BuildStepCommandTextAsync(stepDef, userPrompt, previousStepResult, cancellationToken)
                .ConfigureAwait(false);

            var result = await ExecuteWallyCommandLineAsync(env, commandLine, 0, null, cancellationToken)
                .ConfigureAwait(false);

            if (!result.Success && !stepDef.ContinueOnFailure)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(result.Output)
                        ? $"Command step failed: {commandLine}"
                        : result.Output.TrimEnd());
            }

            return new StepExecutionResult
            {
                ActorLabel = "wally-command",
                Response = string.IsNullOrWhiteSpace(result.Output)
                    ? $"Command {(result.Success ? "completed" : "failed")}: {commandLine}"
                    : result.Output.TrimEnd()
            };
        }

        private static async Task<StepExecutionResult> ExecuteCodeStepAsync(
            WallyEnvironment env,
            string userPrompt,
            string? previousStepResult,
            WallyStepDefinition stepDef,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(stepDef.HandlerName))
            {
                throw new InvalidOperationException(
                    $"Step '{stepDef.Name}' uses kind 'code' but does not declare a handlerName.");
            }

            Dictionary<string, string> arguments = await BuildStepArgumentsAsync(
                env,
                userPrompt,
                previousStepResult,
                stepDef,
                cancellationToken).ConfigureAwait(false);

            string response = stepDef.HandlerName.ToLowerInvariant() switch
            {
                "route_messages" => ExecuteRouteMessagesHandler(env, stepDef, arguments),
                _ => throw new NotSupportedException(
                    $"Step '{stepDef.Name}' uses unknown code handler '{stepDef.HandlerName}'.")
            };

            return new StepExecutionResult
            {
                ActorLabel = $"code:{stepDef.HandlerName}",
                Response = response
            };
        }

        private static async Task<Dictionary<string, string>> BuildStepArgumentsAsync(
            WallyEnvironment env,
            string userPrompt,
            string? previousStepResult,
            WallyStepDefinition stepDef,
            CancellationToken cancellationToken)
        {
            var arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in stepDef.Arguments)
            {
                string value = await env.BuildStepArgumentValueAsync(
                    stepDef,
                    pair.Value ?? string.Empty,
                    userPrompt,
                    previousStepResult,
                    cancellationToken).ConfigureAwait(false);
                arguments[pair.Key] = value;
            }

            return arguments;
        }

        private static string ExecuteRouteMessagesHandler(
            WallyEnvironment env,
            WallyStepDefinition stepDef,
            IReadOnlyDictionary<string, string> arguments)
        {
            if (env.Workspace == null)
                throw new InvalidOperationException("A workspace is required to route mailbox messages.");

            if (!arguments.TryGetValue("sourceFolder", out string? sourceFolder) || string.IsNullOrWhiteSpace(sourceFolder))
            {
                throw new InvalidOperationException(
                    $"Step '{stepDef.Name}' requires argument 'sourceFolder' for handler '{stepDef.HandlerName}'.");
            }

            if (!arguments.TryGetValue("logPath", out string? logPath) || string.IsNullOrWhiteSpace(logPath))
            {
                throw new InvalidOperationException(
                    $"Step '{stepDef.Name}' requires argument 'logPath' for handler '{stepDef.HandlerName}'.");
            }

            env.EnsureStepWriteAllowed(stepDef, logPath, stepDef.HandlerName);

            string resolvedSourceFolder = env.ResolveWorkspaceFilePath(sourceFolder);
            string resolvedLogPath = env.ResolveWorkspaceFilePath(logPath);

            MailboxRouteResult routeResult = MailboxHelper.RouteMessages(
                env.Workspace,
                resolvedSourceFolder,
                env.Logger);
            MailboxHelper.AppendRoutingLog(resolvedLogPath, routeResult);
            return routeResult.BuildSummary();
        }

        private static string BuildShellStepResponse(string command, ShellExecutionResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"shell> {command}");
            builder.AppendLine($"exitCode: {result.ExitCode}");

            if (!string.IsNullOrWhiteSpace(result.Stdout))
            {
                builder.AppendLine();
                builder.AppendLine(result.Stdout.TrimEnd());
            }

            if (!string.IsNullOrWhiteSpace(result.Stderr))
            {
                builder.AppendLine();
                builder.AppendLine(result.Stderr.TrimEnd());
            }

            return builder.ToString().TrimEnd();
        }
    }
}
