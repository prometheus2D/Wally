using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Wally.Core.Actors;

namespace Wally.Core
{
    public readonly struct WallyStepRouteMatch
    {
        public WallyStepRouteMatch(string keyword, string nextStepName, int responseIndex)
        {
            Keyword = keyword;
            NextStepName = nextStepName;
            ResponseIndex = responseIndex;
        }

        public string Keyword { get; }

        public string NextStepName { get; }

        public int ResponseIndex { get; }
    }

    /// <summary>
    /// A self-driving iteration loop that feeds each response back into the actor
    /// until a <see cref="StopCondition"/> is satisfied or <see cref="MaxIterations"/>
    /// is reached.
    /// <para>
    /// Stop condition priority:
    /// <list type="number">
    ///   <item>Response contains <c>StopKeyword</c> (case-insensitive, when configured).</item>
    ///   <item>No action blocks found in response (actor considers itself done).</item>
    ///   <item><c>MaxIterations</c> reached � loop exits with <c>StopReason = "MaxIterations"</c>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <c>CancellationToken</c> is checked at every iteration boundary.
    /// </para>
    /// </summary>
    public sealed class WallyAgentLoop
    {
        private static readonly Regex ActionBlockRegex = new(
            @"```action\s*\n(.*?)\n```",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        /// <summary>Maximum number of iterations. Hard stop when reached.</summary>
        public int MaxIterations { get; }

        /// <summary>
        /// When non-empty, the loop stops immediately if the response contains this
        /// keyword (case-insensitive substring match).
        /// </summary>
        public string? StopKeyword { get; }

        /// <summary>
        /// How the previous response feeds into the next prompt.
        /// <c>"AppendResponse"</c> (default) or <c>"ReplacePrompt"</c>.
        /// </summary>
        public string FeedbackMode { get; }

        public WallyAgentLoop(WallyLoopDefinition loopDef)
        {
            if (loopDef == null) throw new ArgumentNullException(nameof(loopDef));
            MaxIterations = Math.Max(1, loopDef.MaxIterations);
            StopKeyword = string.IsNullOrWhiteSpace(loopDef.StopKeyword) ? null : loopDef.StopKeyword;
            FeedbackMode = string.IsNullOrWhiteSpace(loopDef.FeedbackMode) ? "AppendResponse" : loopDef.FeedbackMode;
        }

        /// <summary>
        /// Resolves the effective iteration limit for loop-driven workflows.
        /// </summary>
        public static int ResolveMaxIterations(WallyLoopDefinition loopDef, int fallbackIterations)
        {
            if (loopDef == null) throw new ArgumentNullException(nameof(loopDef));
            return loopDef.MaxIterations > 0
                ? loopDef.MaxIterations
                : Math.Max(1, fallbackIterations);
        }

        /// <summary>
        /// Returns <see langword="true"/> when the response contains the loop stop keyword.
        /// </summary>
        public static bool ContainsStopKeyword(string response, string? stopKeyword)
        {
            return TryFindExplicitKeyword(response, stopKeyword, out _);
        }

        /// <summary>
        /// Finds the earliest matching keyword route in a step response.
        /// </summary>
        public static WallyStepRouteMatch? ResolveKeywordRoute(
            IReadOnlyDictionary<string, string> keywordRoutes,
            string response)
        {
            if (keywordRoutes == null) throw new ArgumentNullException(nameof(keywordRoutes));
            if (string.IsNullOrWhiteSpace(response) || keywordRoutes.Count == 0)
                return null;

            WallyStepRouteMatch? bestMatch = null;

            foreach (var route in keywordRoutes)
            {
                if (string.IsNullOrWhiteSpace(route.Key) || string.IsNullOrWhiteSpace(route.Value))
                    continue;

                if (!TryFindExplicitKeyword(response, route.Key, out int matchIndex))
                    continue;

                if (bestMatch == null || matchIndex < bestMatch.Value.ResponseIndex)
                {
                    bestMatch = new WallyStepRouteMatch(route.Key, route.Value, matchIndex);
                }
            }

            return bestMatch;
        }

        private static bool TryFindExplicitKeyword(string response, string? keyword, out int matchIndex)
        {
            matchIndex = -1;

            if (string.IsNullOrWhiteSpace(response) || string.IsNullOrWhiteSpace(keyword))
                return false;

            string normalizedKeyword = keyword.Trim();
            string normalizedResponse = response.Replace("\r\n", "\n");
            string[] lines = normalizedResponse.Split('\n');
            int runningIndex = 0;

            foreach (string rawLine in lines)
            {
                string trimmedLine = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    runningIndex += rawLine.Length + 1;
                    continue;
                }

                if (string.Equals(trimmedLine, normalizedKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    matchIndex = runningIndex + rawLine.IndexOf(trimmedLine, StringComparison.Ordinal);
                    return true;
                }

                int separatorIndex = trimmedLine.IndexOf(':');
                if (separatorIndex > 0)
                {
                    string label = trimmedLine[..separatorIndex].Trim();
                    string value = trimmedLine[(separatorIndex + 1)..].Trim();

                    if ((label.Equals("Routing Keyword", StringComparison.OrdinalIgnoreCase)
                            || label.Equals("Route", StringComparison.OrdinalIgnoreCase)
                            || label.Equals("Next Step", StringComparison.OrdinalIgnoreCase)
                            || label.Equals("Stop Keyword", StringComparison.OrdinalIgnoreCase))
                        && string.Equals(value, normalizedKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        matchIndex = runningIndex + rawLine.IndexOf(value, StringComparison.OrdinalIgnoreCase);
                        return true;
                    }
                }

                runningIndex += rawLine.Length + 1;
            }

            return false;
        }

        /// <summary>
        /// Runs the agent loop, returning one <see cref="WallyRunResult"/> per iteration.
        /// </summary>
        /// <param name="env">The workspace environment.</param>
        /// <param name="actor">The actor to execute each iteration (or null for direct mode).</param>
        /// <param name="initialPrompt">The starting prompt (after StartPrompt template expansion).</param>
        /// <param name="loopDef">The loop definition (for logging).</param>
        /// <param name="model">Model override.</param>
        /// <param name="wrapper">Wrapper override.</param>
        /// <param name="noHistory">Whether to suppress conversation history injection.</param>
        /// <param name="cancellationToken">Cancellation token � checked at every iteration boundary.</param>
        /// <param name="output">Output writer for console/UI output.</param>
        /// <returns>List of results, one per iteration executed.</returns>
        public async Task<List<WallyRunResult>> RunAsync(
            WallyEnvironment env,
            Actor? actor,
            string initialPrompt,
            WallyLoopDefinition loopDef,
            string? model,
            string? wrapper,
            bool noHistory,
            CancellationToken cancellationToken,
            TextWriter output,
            WallyLoopExecutionState? executionState = null)
        {
            var results = new List<WallyRunResult>();
            string currentPrompt = executionState?.CurrentPrompt ?? initialPrompt;
            bool directMode = actor == null;
            string actorLabel = directMode ? "(no actor)" : actor!.Name;

            await output.WriteLineAsync(
                $"[agent-loop:{loopDef.Name}] Starting � actor={actorLabel}, maxIterations={MaxIterations}" +
                (StopKeyword != null ? $", stopKeyword=\"{StopKeyword}\"" : "") +
                $", feedbackMode={FeedbackMode}")
                .ConfigureAwait(false);
            await output.WriteLineAsync().ConfigureAwait(false);

            env.Logger.LogCommand("run",
                $"[agent-loop] loop='{loopDef.Name}' actor='{actorLabel}' maxIter={MaxIterations} " +
                $"stopKeyword='{StopKeyword ?? "(none)"}' feedbackMode='{FeedbackMode}'");

            string? stopReason = null;

            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                // CancellationToken honoured at every iteration boundary
                cancellationToken.ThrowIfCancellationRequested();

                WallyLoopExecutionStateStore.UpdateAndSave(
                    env,
                    loopDef,
                    executionState,
                    currentStepName: string.Empty,
                    nextStepName: string.Empty,
                    iterationCount: iteration,
                    status: "Running",
                    stopReason: null,
                    currentPrompt: currentPrompt,
                    previousStepResult: string.Empty,
                    mode: "agent-loop");

                await output.WriteLineAsync(
                    $"--- Iteration {iteration + 1}/{MaxIterations} ({actorLabel}) ---")
                    .ConfigureAwait(false);

                env.Logger.LogPrompt(actorLabel, currentPrompt,
                    model ?? env.Workspace!.Config.DefaultModel);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                string response = directMode
                    ? await env.ExecutePromptAsync(currentPrompt, model, wrapper,
                        loopName: loopDef.Name, iteration: iteration,
                        skipHistory: noHistory || iteration > 0,
                        cancellationToken: cancellationToken).ConfigureAwait(false)
                    : await env.ExecuteActorAsync(actor!, currentPrompt, model, wrapper,
                        loopName: loopDef.Name, iteration: iteration,
                        skipHistory: noHistory || iteration > 0,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                sw.Stop();

                env.Logger.LogResponse(actorLabel, response, sw.ElapsedMilliseconds, iteration + 1);
                await output.WriteLineAsync(response).ConfigureAwait(false);
                await output.WriteLineAsync().ConfigureAwait(false);

                // Stop condition 1: StopKeyword found
                if (ContainsStopKeyword(response, StopKeyword))
                {
                    stopReason = "StopKeyword";
                    WallyLoopExecutionStateStore.UpdateAndSave(
                        env,
                        loopDef,
                        executionState,
                        currentStepName: string.Empty,
                        nextStepName: string.Empty,
                        iterationCount: iteration + 1,
                        status: "Completed",
                        stopReason: stopReason,
                        currentPrompt: currentPrompt,
                        previousStepResult: response,
                        mode: "agent-loop");
                    results.Add(new WallyRunResult
                    {
                        StepName = null, ActorName = actorLabel,
                        Response = response, Iteration = iteration,
                        StopReason = stopReason
                    });

                    await output.WriteLineAsync(
                        $"[agent-loop:{loopDef.Name}] Stopped � StopKeyword \"{StopKeyword}\" detected at iteration {iteration + 1}.")
                        .ConfigureAwait(false);
                    env.Logger.LogInfo(
                        $"Agent loop '{loopDef.Name}' stopped: StopKeyword \"{StopKeyword}\" at iteration {iteration + 1}.");
                    break;
                }

                // Stop condition 2: No action blocks in response
                bool hasActions = ActionBlockRegex.IsMatch(response);
                if (!hasActions)
                {
                    stopReason = "NoActions";
                    WallyLoopExecutionStateStore.UpdateAndSave(
                        env,
                        loopDef,
                        executionState,
                        currentStepName: string.Empty,
                        nextStepName: string.Empty,
                        iterationCount: iteration + 1,
                        status: "Completed",
                        stopReason: stopReason,
                        currentPrompt: currentPrompt,
                        previousStepResult: response,
                        mode: "agent-loop");
                    results.Add(new WallyRunResult
                    {
                        StepName = null, ActorName = actorLabel,
                        Response = response, Iteration = iteration,
                        StopReason = stopReason
                    });

                    await output.WriteLineAsync(
                        $"[agent-loop:{loopDef.Name}] Stopped � no action blocks at iteration {iteration + 1}.")
                        .ConfigureAwait(false);
                    env.Logger.LogInfo(
                        $"Agent loop '{loopDef.Name}' stopped: no actions at iteration {iteration + 1}.");
                    break;
                }

                // Record result for this iteration (no stop yet)
                results.Add(new WallyRunResult
                {
                    StepName = null, ActorName = actorLabel,
                    Response = response, Iteration = iteration
                });

                // Stop condition 3: MaxIterations reached (last iteration)
                if (iteration + 1 >= MaxIterations)
                {
                    stopReason = "MaxIterations";
                    string resumedPrompt = CombinePrompt(initialPrompt, response, iteration);
                    WallyLoopExecutionStateStore.UpdateAndSave(
                        env,
                        loopDef,
                        executionState,
                        currentStepName: string.Empty,
                        nextStepName: string.Empty,
                        iterationCount: iteration + 1,
                        status: "Stopped",
                        stopReason: stopReason,
                        currentPrompt: resumedPrompt,
                        previousStepResult: response,
                        mode: "agent-loop");
                    // Update the last result's stop reason
                    results[results.Count - 1] = new WallyRunResult
                    {
                        StepName = null, ActorName = actorLabel,
                        Response = response, Iteration = iteration,
                        StopReason = stopReason
                    };

                    await output.WriteLineAsync(
                        $"[agent-loop:{loopDef.Name}] Stopped � MaxIterations ({MaxIterations}) reached.")
                        .ConfigureAwait(false);
                    env.Logger.LogInfo(
                        $"Agent loop '{loopDef.Name}' stopped: MaxIterations ({MaxIterations}) reached.");
                    break;
                }

                // Build next prompt using FeedbackMode
                currentPrompt = CombinePrompt(initialPrompt, response, iteration);
                WallyLoopExecutionStateStore.UpdateAndSave(
                    env,
                    loopDef,
                    executionState,
                    currentStepName: string.Empty,
                    nextStepName: string.Empty,
                    iterationCount: iteration + 1,
                    status: "Running",
                    stopReason: null,
                    currentPrompt: currentPrompt,
                    previousStepResult: response,
                    mode: "agent-loop");
            }

            await output.WriteLineAsync(
                $"[agent-loop:{loopDef.Name}] Complete � {results.Count} iteration(s), stopReason={stopReason ?? "unknown"}.")
                .ConfigureAwait(false);
            env.Logger.LogInfo(
                $"Agent loop '{loopDef.Name}' complete: {results.Count} iteration(s), stopReason={stopReason ?? "unknown"}.");

            return results;
        }

        /// <summary>
        /// Combines the original prompt and the latest response according to <see cref="FeedbackMode"/>.
        /// </summary>
        private string CombinePrompt(string originalPrompt, string latestResponse, int iteration)
        {
            return FeedbackMode switch
            {
                "ReplacePrompt" => latestResponse,
                // "AppendResponse" (default)
                _ => $"{originalPrompt}\n\n" +
                     $"--- Previous response (iteration {iteration + 1}) ---\n" +
                     $"{latestResponse}\n" +
                     $"---\n\n" +
                     $"Continue from where you left off. If you are done, respond without any action blocks."
            };
        }
    }
}
