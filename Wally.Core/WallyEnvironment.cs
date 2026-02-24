using System.Collections.Generic;
using Wally.Core.Agents;

namespace Wally.Core
{
    /// <summary>
    /// Represents the Wally environment that manages a collection of agents.
    /// </summary>
    public class WallyEnvironment
    {
        /// <summary>
        /// The list of agents in the environment.
        /// </summary>
        public List<Agent> Agents { get; set; } = new List<Agent>();

        /// <summary>
        /// Adds an agent to the environment.
        /// </summary>
        /// <param name="agent">The agent to add.</param>
        public void AddAgent(Agent agent)
        {
            Agents.Add(agent);
        }

        /// <summary>
        /// Runs all agents on the given prompt and collects responses.
        /// </summary>
        /// <param name="prompt">The input prompt.</param>
        /// <returns>A list of responses from agents that returned text.</returns>
        public List<string> RunAgents(string prompt)
        {
            var responses = new List<string>();
            foreach (var agent in Agents)
            {
                string response = agent.Act(prompt);
                if (response != null)
                {
                    responses.Add($"{agent.GetType().Name}: {response}");
                }
            }
            return responses;
        }

        /// <summary>
        /// Gets an agent by type.
        /// </summary>
        /// <typeparam name="T">The type of agent.</typeparam>
        /// <returns>The first agent of the specified type, or null.</returns>
        public T GetAgent<T>() where T : Agent
        {
            return Agents.Find(a => a is T) as T;
        }
    }
}