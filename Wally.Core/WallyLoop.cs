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
    ///   <item><b>ContinuePrompt</b> — the prompt used for every subsequent iteration.
    ///         When <see langword="null"/>, the previous result is fed as the next prompt.</item>
    /// </list>
    /// After each iteration the <see cref="EndIdentifier"/> function inspects the
    /// result. If it returns <see langword="true"/> the loop is declared finished.
    /// The loop also ends when <see cref="MaxIterations"/> is reached.
    /// </para>
    /// <para>
    /// By default <see cref="EndIdentifier"/> checks for the stop word
    /// <c>LOOP COMPLETED</c> (case-insensitive).
    /// </para>
    /// </summary>
    public class WallyLoop
    {
        // — Constants ——————————————————————————————————————————————————————————

        /// <summary>
        /// The default stop word that <see cref="EndIdentifier"/> looks for
        /// when no custom function is supplied.
        /// </summary>
        public const string DefaultStopWord = "LOOP COMPLETED";

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
        /// Prompt used for every iteration after the first.
        /// When <see langword="null"/> or empty, the previous iteration's result
        /// is fed directly as the next prompt.
        /// </summary>
        public string? ContinuePrompt { get; set; }

        /// <summary>
        /// Function evaluated after each iteration. Receives the latest result
        /// and returns <see langword="true"/> when the stop word has been detected
        /// (i.e. the loop should end), or <see langword="false"/> to keep going.
        /// <para>
        /// Defaults to a case-insensitive check for <see cref="DefaultStopWord"/>
        /// (<c>LOOP COMPLETED</c>).
        /// </para>
        /// </summary>
        public Func<string, bool> EndIdentifier { get; set; }

        /// <summary>
        /// The maximum number of iterations the loop is allowed to perform.
        /// The loop ends when this ceiling is reached even if
        /// <see cref="EndIdentifier"/> has not yet returned <see langword="true"/>.
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        /// Gets the number of iterations that have been executed since
        /// the last call to <see cref="Run"/>.
        /// </summary>
        public int ExecutionCount { get; private set; }

        /// <summary>
        /// <see langword="true"/> when the most recent <see cref="Run"/> ended
        /// because <see cref="EndIdentifier"/> detected the stop word.
        /// <see langword="false"/> when the loop ended because
        /// <see cref="MaxIterations"/> was reached.
        /// </summary>
        public bool StoppedByDeclaration { get; private set; }

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
        /// The prompt for subsequent iterations. When <see langword="null"/>,
        /// the previous result is used as the next prompt.
        /// </param>
        /// <param name="endIdentifier">
        /// Function that inspects each result and returns <see langword="true"/>
        /// when the stop word is detected. When <see langword="null"/>, defaults
        /// to a case-insensitive check for <see cref="DefaultStopWord"/>.
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
            string? continuePrompt = null,
            Func<string, bool>? endIdentifier = null,
            int maxIterations = 10)
        {
            _action        = action ?? throw new ArgumentNullException(nameof(action));
            StartPrompt    = startPrompt ?? throw new ArgumentNullException(nameof(startPrompt));
            ContinuePrompt = continuePrompt;
            EndIdentifier  = endIdentifier
                             ?? (result => result.Contains(DefaultStopWord, StringComparison.OrdinalIgnoreCase));
            MaxIterations  = maxIterations;
        }

        // — Execution ——————————————————————————————————————————————————————————

        /// <summary>
        /// Executes the loop.
        /// <para>
        /// The first iteration uses <see cref="StartPrompt"/>. Subsequent
        /// iterations use <see cref="ContinuePrompt"/> (when set), otherwise
        /// the previous result becomes the next prompt.
        /// </para>
        /// <para>
        /// After each iteration <see cref="EndIdentifier"/> is called with the
        /// result. If it returns <see langword="true"/>, the loop ends with
        /// <see cref="StoppedByDeclaration"/> = <see langword="true"/>. If
        /// <see cref="MaxIterations"/> is reached first,
        /// <see cref="StoppedByDeclaration"/> = <see langword="false"/>.
        /// </para>
        /// </summary>
        public void Run()
        {
            // Reset per-run state.
            ExecutionCount       = 0;
            StoppedByDeclaration = false;
            Results.Clear();

            string currentPrompt = StartPrompt;

            while (ExecutionCount < MaxIterations)
            {
                string result = _action.Invoke(currentPrompt);
                ExecutionCount++;
                Results.Add(result);

                // Check if the stop word was detected in the result.
                if (EndIdentifier.Invoke(result))
                {
                    StoppedByDeclaration = true;
                    break;
                }

                // Prepare the prompt for the next iteration.
                currentPrompt = !string.IsNullOrEmpty(ContinuePrompt)
                    ? ContinuePrompt
                    : result;
            }
        }
    }
}
