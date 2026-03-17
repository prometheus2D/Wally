using System;
using System.Collections.Generic;

namespace Wally.Core
{
    /// <summary>
    /// Represents an iterative execution loop for Wally operations.
    /// <para>
    /// A <see cref="WallyLoop"/> operates in one of two modes:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <b>Single-actor mode</b> (legacy) — constructed with a single action, a start
    ///     prompt, and an optional continue-prompt factory. Each iteration runs the same
    ///     action with an evolving prompt.
    ///   </item>
    ///   <item>
    ///     <b>Multi-step mode</b> — constructed with a list of <see cref="WallyStep"/> objects.
    ///     Each loop pass executes every step in order, threading the previous step's result
    ///     into the next step's prompt. The loop terminates when any step reports
    ///     <see cref="LoopStopReason.Completed"/> or <see cref="LoopStopReason.Error"/>, 
    ///     or when <see cref="MaxIterations"/> full passes have been completed.
    ///   </item>
    /// </list>
    /// <para>
    /// After each iteration (single-actor) or each full pass (multi-step) the result
    /// is checked for two keywords:
    /// <list type="bullet">
    ///   <item><see cref="CompletedKeyword"/> (<c>[LOOP COMPLETED]</c>) — the loop finished successfully.</item>
    ///   <item><see cref="ErrorKeyword"/> (<c>[LOOP ERROR]</c>) — the actor detected an error.</item>
    /// </list>
    /// The loop also ends when <see cref="MaxIterations"/> is reached.
    /// </para>
    /// </summary>
    public class WallyLoop
    {
        // ??? Keywords ?????????????????????????????????????????????????????????

        /// <summary>
        /// Keyword that signals the loop has completed successfully.
        /// Detected case-insensitively in the iteration result.
        /// </summary>
        public const string CompletedKeyword = "[LOOP COMPLETED]";

        /// <summary>
        /// Keyword that signals the actor detected an error.
        /// Detected case-insensitively in the iteration result.
        /// </summary>
        public const string ErrorKeyword = "[LOOP ERROR]";

        // ??? Single-actor state ???????????????????????????????????????????????

        /// <summary>
        /// The action lambda that this loop executes on each iteration.
        /// <see langword="null"/> when running in multi-step mode.
        /// </summary>
        private readonly Func<string, string>? _action;

        /// <summary>
        /// The prompt used on the first iteration of the loop (single-actor mode).
        /// </summary>
        public string StartPrompt { get; set; }

        /// <summary>
        /// Function that builds the prompt for iterations after the first (single-actor mode).
        /// Receives the previous iteration's result. When <see langword="null"/>, 
        /// the previous result is fed directly as the next prompt.
        /// </summary>
        public Func<string, string>? ContinuePrompt { get; set; }

        // ??? Multi-step state ?????????????????????????????????????????????????

        /// <summary>
        /// The ordered list of steps to execute on each loop pass.
        /// When non-empty the loop operates in multi-step mode, ignoring
        /// <see cref="_action"/>, <see cref="StartPrompt"/>, and
        /// <see cref="ContinuePrompt"/>.
        /// </summary>
        public IReadOnlyList<WallyStep> Steps { get; }

        /// <summary>
        /// The user's original runtime prompt. Passed to each step's prompt
        /// factories in multi-step mode.
        /// </summary>
        public string UserPrompt { get; set; }

        /// <summary>Returns <see langword="true"/> when this loop runs in multi-step mode.</summary>
        public bool HasSteps => Steps != null && Steps.Count > 0;

        // ??? Shared state ?????????????????????????????????????????????????????

        /// <summary>
        /// The maximum number of iterations (single-actor) or full passes (multi-step)
        /// the loop is allowed to perform.
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        /// The keyword checked after each iteration to detect successful completion.
        /// Defaults to <see cref="CompletedKeyword"/>. Can be overridden per-instance.
        /// Only used in single-actor mode; multi-step mode delegates keyword checking
        /// to each individual <see cref="WallyStep"/>.
        /// </summary>
        public string CompletedKeywordOverride { get; set; } = CompletedKeyword;

        /// <summary>
        /// The keyword checked after each iteration to detect an error.
        /// Defaults to <see cref="ErrorKeyword"/>. Can be overridden per-instance.
        /// Only used in single-actor mode.
        /// </summary>
        public string ErrorKeywordOverride { get; set; } = ErrorKeyword;

        /// <summary>
        /// Gets the number of iterations (single-actor) or passes (multi-step)
        /// executed since the last call to <see cref="Run"/>.
        /// </summary>
        public int ExecutionCount { get; private set; }

        /// <summary>
        /// Indicates how the loop ended after the most recent <see cref="Run"/>:
        /// <see cref="LoopStopReason.Completed"/>, <see cref="LoopStopReason.Error"/>, 
        /// or <see cref="LoopStopReason.MaxIterations"/>.
        /// </summary>
        public LoopStopReason StopReason { get; private set; }

        /// <summary>
        /// The result strings produced by each iteration (single-actor) or each
        /// step execution (multi-step, flattened in pass × step order), in order.
        /// Populated during <see cref="Run"/> and cleared at the start of each run.
        /// </summary>
        public List<string> Results { get; } = new();

        /// <summary>
        /// The result of the most recent iteration or step execution, or
        /// <see langword="null"/> if <see cref="Run"/> has not been called.
        /// </summary>
        public string? LastResult => Results.Count > 0 ? Results[^1] : null;

        // ??? Factory ??????????????????????????????????????????????????????????

        /// <summary>
        /// Creates a <see cref="WallyLoop"/> from a <see cref="WallyLoopDefinition"/>
        /// in <b>single-actor</b> mode. The definition's prompts, keywords, and iteration
        /// limit drive the loop behaviour.
        /// </summary>
        /// <param name="definition">The loop definition loaded from JSON.</param>
        /// <param name="userPrompt">The user's runtime prompt.</param>
        /// <param name="actorAction">The action to execute each iteration.</param>
        /// <param name="fallbackMaxIterations">
        /// Used when the definition's <see cref="WallyLoopDefinition.MaxIterations"/> is 0.
        /// </param>
        public static WallyLoop FromDefinition(
            WallyLoopDefinition definition,
            string userPrompt,
            Func<string, string> actorAction,
            int fallbackMaxIterations = 10)
        {
            // Resolve the start prompt — substitute {userPrompt}.
            string startPrompt = definition.StartPrompt.Replace("{userPrompt}", userPrompt);

            int maxIter = definition.MaxIterations > 0
                ? definition.MaxIterations
                : fallbackMaxIterations;

            // Build the continue-prompt function using the definition's template.
            Func<string, string> continuePrompt = previousResult =>
                definition.BuildContinuePrompt(previousResult, userPrompt);

            var loop = new WallyLoop(actorAction, startPrompt, continuePrompt, maxIter);

            // Apply custom keywords from the definition.
            loop.CompletedKeywordOverride = definition.ResolvedCompletedKeyword;
            loop.ErrorKeywordOverride     = definition.ResolvedErrorKeyword;

            return loop;
        }

        /// <summary>
        /// Creates a <see cref="WallyLoop"/> from a <see cref="WallyLoopDefinition"/>
        /// in <b>multi-step</b> mode, using the provided pre-built steps.
        /// </summary>
        /// <param name="definition">The loop definition loaded from JSON.</param>
        /// <param name="userPrompt">The user's runtime prompt.</param>
        /// <param name="steps">
        /// The ordered list of runtime steps to execute on each pass.
        /// Must not be empty.
        /// </param>
        /// <param name="fallbackMaxIterations">
        /// Used when the definition's <see cref="WallyLoopDefinition.MaxIterations"/> is 0.
        /// </param>
        public static WallyLoop FromDefinitionWithSteps(
            WallyLoopDefinition definition,
            string userPrompt,
            IReadOnlyList<WallyStep> steps,
            int fallbackMaxIterations = 10)
        {
            int maxIter = definition.MaxIterations > 0 ? definition.MaxIterations : fallbackMaxIterations;
            return new WallyLoop(steps, userPrompt, maxIter);
        }

        // ??? Constructors ?????????????????????????????????????????????????????

        /// <summary>
        /// Creates a new <see cref="WallyLoop"/> in single-actor mode.
        /// </summary>
        public WallyLoop(
            Func<string, string> action,
            string startPrompt,
            Func<string, string>? continuePrompt = null,
            int maxIterations = 10)
        {
            _action        = action ?? throw new ArgumentNullException(nameof(action));
            StartPrompt    = startPrompt ?? throw new ArgumentNullException(nameof(startPrompt));
            ContinuePrompt = continuePrompt;
            MaxIterations  = maxIterations;
            Steps          = Array.Empty<WallyStep>();
            UserPrompt     = startPrompt;
        }

        /// <summary>
        /// Creates a new <see cref="WallyLoop"/> in multi-step mode.
        /// </summary>
        /// <param name="steps">Ordered list of steps. Must not be null or empty.</param>
        /// <param name="userPrompt">The original user prompt, threaded into step prompt factories.</param>
        /// <param name="maxIterations">Maximum number of full step-sequence passes.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="steps"/> or <paramref name="userPrompt"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="steps"/> is empty.</exception>
        public WallyLoop(
            IReadOnlyList<WallyStep> steps,
            string userPrompt,
            int maxIterations = 10)
        {
            if (steps == null)        throw new ArgumentNullException(nameof(steps));
            if (steps.Count == 0)     throw new ArgumentException("Steps list must not be empty.", nameof(steps));

            Steps         = steps;
            UserPrompt    = userPrompt ?? throw new ArgumentNullException(nameof(userPrompt));
            MaxIterations = maxIterations;
            StartPrompt   = userPrompt;
        }

        // ??? Execution ????????????????????????????????????????????????????????

        /// <summary>
        /// Executes the loop.
        /// <para>
        /// <b>Single-actor mode:</b> The first iteration uses <see cref="StartPrompt"/>.
        /// Subsequent iterations call <see cref="ContinuePrompt"/> with the previous result.
        /// Stops when <see cref="CompletedKeywordOverride"/> or <see cref="ErrorKeywordOverride"/>
        /// is found, or <see cref="MaxIterations"/> is reached.
        /// </para>
        /// <para>
        /// <b>Multi-step mode:</b> Each pass executes every step in <see cref="Steps"/> in order.
        /// The result of each step is passed as <c>previousStepResult</c> to the next step's
        /// prompt factories. The pass ends early if any step reports
        /// <see cref="LoopStopReason.Error"/>. A pass that sees at least one step report
        /// <see cref="LoopStopReason.Completed"/> marks the loop as completed.
        /// </para>
        /// </summary>
        public void Run()
        {
            ExecutionCount = 0;
            StopReason     = LoopStopReason.MaxIterations;
            Results.Clear();

            if (HasSteps)
                RunMultiStep();
            else
                RunSingleActor();
        }

        // ??? Private: single-actor execution ?????????????????????????????????

        private void RunSingleActor()
        {
            string currentPrompt = StartPrompt;

            while (ExecutionCount < MaxIterations)
            {
                string result = _action!.Invoke(currentPrompt);
                ExecutionCount++;
                Results.Add(result);

                if (result.Contains(CompletedKeywordOverride, StringComparison.OrdinalIgnoreCase))
                {
                    StopReason = LoopStopReason.Completed;
                    break;
                }

                if (result.Contains(ErrorKeywordOverride, StringComparison.OrdinalIgnoreCase))
                {
                    StopReason = LoopStopReason.Error;
                    break;
                }

                currentPrompt = ContinuePrompt != null
                    ? ContinuePrompt.Invoke(result)
                    : result;
            }
        }

        // ??? Private: multi-step execution ???????????????????????????????????

        private void RunMultiStep()
        {
            while (ExecutionCount < MaxIterations)
            {
                ExecutionCount++;

                string? previousStepResult = null;
                bool passCompleted = false;
                bool passErrored   = false;

                foreach (var step in Steps)
                {
                    string stepResult = step.Execute(UserPrompt, previousStepResult);
                    Results.Add(stepResult);

                    if (step.StopReason == LoopStopReason.Error)
                    {
                        passErrored = true;
                        break;
                    }

                    if (step.StopReason == LoopStopReason.Completed)
                    {
                        passCompleted = true;
                        // Don't break — allow remaining steps to run in this pass
                        // only if the completed keyword came from the last step.
                        // Convention: if any step signals completion, the whole pass is complete.
                        break;
                    }

                    previousStepResult = stepResult;
                }

                if (passErrored)
                {
                    StopReason = LoopStopReason.Error;
                    break;
                }

                if (passCompleted)
                {
                    StopReason = LoopStopReason.Completed;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Describes why a <see cref="WallyLoop"/> stopped.
    /// </summary>
    public enum LoopStopReason
    {
        /// <summary>The loop reached <see cref="WallyLoop.MaxIterations"/>.</summary>
        MaxIterations,

        /// <summary>The actor's response contained <see cref="WallyLoop.CompletedKeyword"/>.</summary>
        Completed,

        /// <summary>The actor's response contained <see cref="WallyLoop.ErrorKeyword"/>.</summary>
        Error
    }
}
