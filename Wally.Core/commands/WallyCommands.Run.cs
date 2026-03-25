using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wally.Core.Actors;

namespace Wally.Core
{
    public static partial class WallyCommands
    {
        // ?? Run ???????????????????????????????????????????????????????????????

        /// <summary>
        /// Backward-compatible overload — returns raw response strings.
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
        /// Synchronous wrapper — delegates to <see cref="HandleRunTypedAsync"/>.
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

            // Resolve the prompt — apply StartPrompt template if defined.
            string resolvedPrompt = (loopDef != null && !string.IsNullOrWhiteSpace(loopDef.StartPrompt))
                ? loopDef.StartPrompt.Replace("{userPrompt}", prompt)
                : prompt;

            // Agent loop routing — when loop defines MaxIterations > 0
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

            // Resolve actors once
            var stepActors = new (Actor? actor, bool isDirect, string actorLabel)[stepDefs.Count];
            for (int i = 0; i < stepDefs.Count; i++)
            {
                var stepDef = stepDefs[i];
                string resolvedActorName = !string.IsNullOrWhiteSpace(stepDef.ActorName)
                    ? stepDef.ActorName : loopDef.ActorName;
                bool isDirect = string.IsNullOrWhiteSpace(resolvedActorName);
                Actor? stepActor = null;
                if (!isDirect)
                {
                    stepActor = env.GetActor(resolvedActorName!);
                    if (stepActor == null)
                    {
                        await out_.WriteLineAsync($"{loopLabel} Step '{stepDef.Name}': actor '{resolvedActorName}' not found.").ConfigureAwait(false);
                        env.Logger.LogError($"Pipeline '{loopDef.Name}' step '{stepDef.Name}': actor '{resolvedActorName}' not found.", "run");
                        return new List<WallyRunResult>();
                    }
                }
                string actorLabel = isDirect ? "(no actor)" : resolvedActorName!;
                stepActors[i] = (stepActor, isDirect, actorLabel);
                await out_.WriteLineAsync($"  Step {i + 1}: [{(string.IsNullOrWhiteSpace(stepDef.Name) ? $"step-{i+1}" : stepDef.Name)}]  Actor: {actorLabel}").ConfigureAwait(false);
            }
            await out_.WriteLineAsync().ConfigureAwait(false);

            // Run steps in order
            var results = new List<WallyRunResult>(stepDefs.Count);
            string? previousStepResult = null;

            for (int i = 0; i < stepDefs.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var stepDef = stepDefs[i];
                var (stepActor, isDirect, actorLabel) = stepActors[i];
                string stepName   = string.IsNullOrWhiteSpace(stepDef.Name) ? $"step-{i + 1}" : stepDef.Name;
                string stepPrompt = stepDef.BuildPrompt(prompt, previousStepResult);

                await out_.WriteLineAsync($"--- Step {i + 1}: {stepName} ({actorLabel}) ---").ConfigureAwait(false);
                env.Logger.LogPrompt(actorLabel, stepPrompt, model ?? env.Workspace!.Config.DefaultModel);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                string response = isDirect
                    ? await env.ExecutePromptAsync(stepPrompt, model, wrapper, skipHistory: noHistory,
                        cancellationToken: cancellationToken).ConfigureAwait(false)
                    : await env.ExecuteActorAsync(stepActor!, stepPrompt, model, wrapper,
                        skipHistory: noHistory, cancellationToken: cancellationToken).ConfigureAwait(false);
                sw.Stop();
                env.Logger.LogResponse(actorLabel, response, sw.ElapsedMilliseconds, i + 1);
                await out_.WriteLineAsync(response).ConfigureAwait(false);
                await out_.WriteLineAsync().ConfigureAwait(false);

                results.Add(new WallyRunResult { StepName = stepName, ActorName = actorLabel, Response = response });
                previousStepResult = response;
            }

            await out_.WriteLineAsync($"{loopLabel} Pipeline complete \u2014 {results.Count} step(s).").ConfigureAwait(false);
            env.Logger.LogInfo($"Pipeline '{loopDef.Name}' complete: {results.Count} step(s).");
            return results;
        }
    }
}
