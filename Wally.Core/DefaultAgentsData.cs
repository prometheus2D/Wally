using System.Collections.Generic;
using Wally.Core.Agents.RBA;

namespace Wally.Core
{
    /// <summary>
    /// Represents the default data for agents loaded from JSON.
    /// </summary>
    public class DefaultAgentsData
    {
        /// <summary>
        /// List of default roles.
        /// </summary>
        public List<Role> Roles { get; set; } = new List<Role>();

        /// <summary>
        /// List of default acceptance criteria.
        /// </summary>
        public List<AcceptanceCriteria> AcceptanceCriterias { get; set; } = new List<AcceptanceCriteria>();

        /// <summary>
        /// List of default intents.
        /// </summary>
        public List<Intent> Intents { get; set; } = new List<Intent>();
    }
}