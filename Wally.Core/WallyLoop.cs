using System;
using System.Collections.Generic;

namespace Wally.Core
{
    /// <summary>
    /// Represents an iterative execution loop for Wally operations.
    /// <para>
    /// A <see cref="WallyLoop"/> is constructed with three core pieces:
    /// <list type="bullet">
    ///   <item><b>Action</b> — a <see cref="Func{String, String}"/> that receives a
    ///         prompt and returns a result string (e.g. an actor's response).</item>
    ///   <item><b>StartPrompt</b> — the prompt used for the first iteration.</item>
    ///   <item><b>ContinuePrompt</b> — a function that receives the previous iteration's
    ///         result and returns the prompt for the next iteration. This is how
    ///         context is carried forward between stateless calls. When
    ///         <see langword="null"/>, the previous result is fed directly as the
    ///         next prompt.</item>
    /// </list>
    /// After each iteration the result is checked for two keywords:
    /// <list type="bullet">
    ///   <item><see cref="CompletedKeyword"/> (<c>[LOOP COMPLETED]</c>) — the loop finished successfully.</item>
    ///   <item><see cref="ErrorKeyword"/> (<c>[LOOP ERROR]</c>) — the actor detected an error.</item>
    /// </list>
    /// The loop also ends when <see cref="MaxIterations"/> is reached.
    /// </para>
    /// </summary>
    public class WallyLoop
    {
        // — Keywords ——————————————————————————————————————————————————————————

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

        // — State ——————————————————————————————————————————————————————————————

        /// <summary>
        /// The action lambda that this loop executes on each iteration.
        /// Receives the current prompt and returns a result string.
        /// </summary>
        private readonly Func<string, string> _action;

        /// <summary>
        /// The prompt used on the first iteration of the loop.
        /// </summary>
        public string StartPrompt { get; set; }

        /// <summary>
        /// Function that builds the prompt for iterations after the first.
        /// Receives the previous iteration's result so that context can be
        /// carried forward between stateless calls. When <see langword="null"/>, 
        /// the previous result is fed directly as the next prompt.
        /// </summary>
        public Func<string, string>? ContinuePrompt { get; set; }

        /// <summary>
        /// The maximum number of iterations the loop is allowed to perform.
        /// The loop ends when this ceiling is reached even if neither
        /// keyword has been detected.
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        /// Gets the number of iterations that have been executed since
        /// the last call to <see cref="Run"/>.
        /// </summary>
        public int ExecutionCount { get; private set; }

        /// <summary>
        /// Indicates how the loop ended after the most recent <see cref="Run"/>:
        /// <see cref="LoopStopReason.Completed"/>, <see cref="LoopStopReason.Error"/>, 
        /// or <see cref="LoopStopReason.MaxIterations"/>.
        /// </summary>
        public LoopStopReason StopReason { get; private set; }

        /// <summary>
        /// The result strings produced by each iteration, in order.
        /// Populated during <see cref="Run"/> and cleared at the start of each run.
        /// </summary>
        public List<string> Results { get; } = new();

        /// <summary>
        /// The result of the most recent iteration, or <see langword="null"/>
        /// if <see cref="Run"/> has not been called.
        /// </summary>
        public string? LastResult => Results.Count > 0 ? Results[^1] : null;

        // — Constructor ————————————————————————————————————————————————————————

        /// <summary>
        /// Creates a new <see cref="WallyLoop"/>.
        /// </summary>
        /// <param name="action">
        /// The work to perform on each iteration. Receives a prompt and returns
        /// a result. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="startPrompt">
        /// The prompt for the first iteration. Must not be <see langword="null"/>.
        /// </param>
        /// <param name="continuePrompt">
        /// Function that receives the previous result and returns the next prompt.
        /// When <see langword="null"/>, the previous result is used directly.
        /// </param>
        /// <param name="maxIterations">
        /// Hard ceiling on iterations. Defaults to <c>10</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="action"/> or <paramref name="startPrompt"/>
        /// is <see langword="null"/>.
        /// </exception>
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
        }

        // — Execution ——————————————————————————————————————————————————————————

        /// <summary>
        /// Executes the loop.
        /// <para>
        /// The first iteration uses <see cref="StartPrompt"/>. Subsequent
        /// iterations call <see cref="ContinuePrompt"/> with the previous result
        /// to build the next prompt (carrying context forward). If
        /// <see cref="ContinuePrompt"/> is null, the previous result is used directly.
        /// </para>
        /// <para>
        /// After each iteration the result is checked for
        /// <see cref="CompletedKeyword"/> and <see cref="ErrorKeyword"/>.
        /// If either is found the loop ends immediately. If
        /// <see cref="MaxIterations"/> is reached first, the loop ends with
        /// <see cref="StopReason"/> = <see cref="LoopStopReason.MaxIterations"/>.
        /// </para>
        /// </summary>
        public void Run()
        {
            // Reset per-run state.
            ExecutionCount = 0;
            StopReason     = LoopStopReason.MaxIterations;
            Results.Clear();

            string currentPrompt = StartPrompt;

            while (ExecutionCount < MaxIterations)
            {
                string result = _action.Invoke(currentPrompt);
                ExecutionCount++;
                Results.Add(result);

                // Check for stop keywords in the result.
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

                // Build the next prompt — either via the ContinuePrompt function
                // (which receives the previous result for context) or by using
                // the result directly.
                currentPrompt = ContinuePrompt != null
                    ? ContinuePrompt.Invoke(result)
                    : result;
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
