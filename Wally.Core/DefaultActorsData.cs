using System.Collections.Generic;
using Wally.Core.RBA;

namespace Wally.Core
{
    /// <summary>
    /// Represents the default data for Actors loaded from JSON.
    /// </summary>
    public class DefaultActorsData
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