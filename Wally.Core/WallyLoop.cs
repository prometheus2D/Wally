using System;
using System.Collections.Generic;

namespace Wally.Core
{
    /// <summary>
    /// Executes an ordered sequence of AI steps once, threading each step's
    /// response into the next step's prompt.
    /// <para>
    /// Each step is a one-shot call: build prompt ? invoke action ? collect response.
    /// There is no retry, no iteration, and no stop-keyword detection. The caller
    /// receives a <see cref="WallyRunResult"/> for every step in execution order.
    /// </para>
    /// </summary>
    public sealed class WallyPipeline
    {
        private readonly IReadOnlyList<(string name, string description, string actorName, Func<string, string> action, Func<string, string?, string> promptFactory)> _steps;
        private readonly string _userPrompt;

        /// <param name="userPrompt">The original user prompt, available to every step's prompt factory.</param>
        /// <param name="steps">
        /// Each element carries the step's display name, description, actor name, the action to invoke,
        /// and a prompt factory of the form <c>(userPrompt, previousStepResult) => prompt</c>.
        /// </param>
        public WallyPipeline(
            string userPrompt,
            IReadOnlyList<(string name, string description, string actorName, Func<string, string> action, Func<string, string?, string> promptFactory)> steps)
        {
            _userPrompt = userPrompt ?? throw new ArgumentNullException(nameof(userPrompt));
            _steps      = steps      ?? throw new ArgumentNullException(nameof(steps));
        }

        /// <summary>
        /// Runs every step once in order and returns one <see cref="WallyRunResult"/> per step.
        /// </summary>
        public List<WallyRunResult> Run()
        {
            var results = new List<WallyRunResult>(_steps.Count);
            string? previousStepResult = null;

            foreach (var (name, description, actorName, action, promptFactory) in _steps)
            {
                string prompt   = promptFactory(_userPrompt, previousStepResult);
                string response = action(prompt);

                results.Add(new WallyRunResult
                {
                    StepName  = name,
                    ActorName = actorName,
                    Response  = response
                });

                previousStepResult = response;
            }

            return results;
        }
    }
}
