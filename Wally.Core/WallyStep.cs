using System;
using System.Collections.Generic;

namespace Wally.Core
{
    /// <summary>
    /// The runtime representation of a <see cref="WallyStepDefinition"/>.
    /// <para>
    /// A <see cref="WallyStep"/> is owned by a <see cref="WallyLoop"/> and
    /// represents one discrete unit of work within that loop. Each step:
    /// <list type="bullet">
    ///   <item>Receives an action lambda (typically bound to a specific actor).</item>
    ///   <item>Has its own start-prompt and continue-prompt factories.</item>
    ///   <item>Has its own stop keywords and iteration ceiling.</item>
    ///   <item>Tracks its own results and stop reason.</item>
    /// </list>
    /// </para>
    /// <para>
    /// The <see cref="WallyLoop"/> drives step execution: it calls
    /// <see cref="Execute"/> for each step in sequence per loop pass.
    /// The result of one step is passed as <c>previousStepResult</c> to the
    /// next step's prompt builders.
    /// </para>
    /// </summary>
    public class WallyStep
    {
        // ??? Identity ??????????????????????????????????????????????????????????

        /// <summary>Short display name for this step (e.g. <c>"Analyse"</c>, <c>"Review"</c>).</summary>
        public string Name { get; }

        /// <summary>Optional human-readable description of what this step does.</summary>
        public string Description { get; }

        // ??? Configuration ????????????????????????????????????????????????????

        /// <summary>The action to invoke on each iteration of this step.</summary>
        private readonly Func<string, string> _action;

        /// <summary>
        /// Builds the initial prompt for this step.
        /// Receives (userPrompt, previousStepResult).
        /// </summary>
        public Func<string, string?, string> StartPromptFactory { get; set; }

        /// <summary>
        /// Builds the continuation prompt when this step iterates.
        /// Receives (previousIterationResult, userPrompt, previousStepResult).
        /// When <see langword="null"/>, the previous result is used directly.
        /// </summary>
        public Func<string, string, string?, string>? ContinuePromptFactory { get; set; }

        /// <summary>
        /// The maximum number of iterations this step may execute before the loop
        /// automatically advances to the next step.
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        /// The keyword that signals this step has completed successfully.
        /// Detected case-insensitively.
        /// </summary>
        public string CompletedKeyword { get; set; }

        /// <summary>
        /// The keyword that signals this step encountered an error.
        /// Detected case-insensitively.
        /// </summary>
        public string ErrorKeyword { get; set; }

        // ??? Runtime state ????????????????????????????????????????????????????

        /// <summary>The results produced by each iteration of this step, in order.</summary>
        public List<string> Results { get; } = new();

        /// <summary>The number of iterations executed in the most recent call to <see cref="Execute"/>.</summary>
        public int ExecutionCount { get; private set; }

        /// <summary>The reason this step stopped in the most recent call to <see cref="Execute"/>.</summary>
        public LoopStopReason StopReason { get; private set; }

        /// <summary>
        /// The last result produced by this step, or <see langword="null"/> if
        /// the step has not yet been executed.
        /// </summary>
        public string? LastResult => Results.Count > 0 ? Results[^1] : null;

        // ??? Constructor ??????????????????????????????????????????????????????

        /// <summary>
        /// Creates a new <see cref="WallyStep"/>.
        /// </summary>
        /// <param name="name">Step display name.</param>
        /// <param name="description">Optional description.</param>
        /// <param name="action">
        /// Work to perform on each iteration. Receives the current prompt and
        /// returns a result string. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="startPromptFactory">
        /// Builds the initial prompt for this step. Receives
        /// (userPrompt, previousStepResult). Must not be <see langword="null"/>.
        /// </param>
        /// <param name="continuePromptFactory">
        /// Builds the continuation prompt when this step iterates more than once.
        /// Receives (previousIterationResult, userPrompt, previousStepResult).
        /// When <see langword="null"/>, the previous result is used directly.
        /// </param>
        /// <param name="maxIterations">Per-step iteration ceiling. Defaults to <c>1</c>.</param>
        /// <param name="completedKeyword">Completed keyword. Defaults to <see cref="WallyLoop.CompletedKeyword"/>.</param>
        /// <param name="errorKeyword">Error keyword. Defaults to <see cref="WallyLoop.ErrorKeyword"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="action"/> or <paramref name="startPromptFactory"/> is null.
        /// </exception>
        public WallyStep(
            string name,
            string description,
            Func<string, string> action,
            Func<string, string?, string> startPromptFactory,
            Func<string, string, string?, string>? continuePromptFactory = null,
            int maxIterations = 1,
            string? completedKeyword = null,
            string? errorKeyword = null)
        {
            Name                  = name ?? throw new ArgumentNullException(nameof(name));
            Description           = description ?? string.Empty;
            _action               = action ?? throw new ArgumentNullException(nameof(action));
            StartPromptFactory    = startPromptFactory ?? throw new ArgumentNullException(nameof(startPromptFactory));
            ContinuePromptFactory = continuePromptFactory;
            MaxIterations         = maxIterations > 0 ? maxIterations : 1;
            CompletedKeyword      = completedKeyword ?? WallyLoop.CompletedKeyword;
            ErrorKeyword          = errorKeyword     ?? WallyLoop.ErrorKeyword;
        }

        // ??? Execution ????????????????????????????????????????????????????????

        /// <summary>
        /// Executes this step.
        /// <para>
        /// The first iteration uses <see cref="StartPromptFactory"/> to build the
        /// prompt from (userPrompt, previousStepResult). Subsequent iterations call
        /// <see cref="ContinuePromptFactory"/> (or use the previous result directly).
        /// </para>
        /// <para>
        /// The step ends when <see cref="CompletedKeyword"/> or
        /// <see cref="ErrorKeyword"/> is found in a result, or when
        /// <see cref="MaxIterations"/> is reached.
        /// </para>
        /// </summary>
        /// <param name="userPrompt">The original runtime user prompt.</param>
        /// <param name="previousStepResult">
        /// The last result from the preceding step in this loop pass,
        /// or <see langword="null"/> for the first step.
        /// </param>
        /// <returns>
        /// The final result string produced by this step's last iteration.
        /// </returns>
        public string Execute(string userPrompt, string? previousStepResult)
        {
            ExecutionCount = 0;
            StopReason     = LoopStopReason.MaxIterations;
            Results.Clear();

            string currentPrompt = StartPromptFactory(userPrompt, previousStepResult);

            while (ExecutionCount < MaxIterations)
            {
                string result = _action.Invoke(currentPrompt);
                ExecutionCount++;
                Results.Add(result);

                if (result.Contains(CompletedKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    StopReason = LoopStopReason.Completed;
                    break;
                }

                if (result.Contains(ErrorKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    StopReason = LoopStopReason.Error;
                    break;
                }

                // Build next prompt for continued iteration.
                currentPrompt = ContinuePromptFactory != null
                    ? ContinuePromptFactory.Invoke(result, userPrompt, previousStepResult)
                    : result;
            }

            return LastResult ?? string.Empty;
        }
    }
}
