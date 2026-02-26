using System;
using System.Diagnostics;
using Wally.Core.RBA;

namespace Wally.Core.Actors
{
    /// <summary>
    /// The default Wally actor. Identical to <see cref="CopilotActor"/> — exists as a
    /// named type so workspace code can refer to the default actor by a meaningful name.
    /// </summary>
    public class WallyActor : CopilotActor
    {
        public WallyActor(string name, string folderPath,
                          Wally.Core.RBA.Role role,
                          Wally.Core.RBA.AcceptanceCriteria acceptanceCriteria,
                          Wally.Core.RBA.Intent intent,
                          WallyWorkspace? workspace = null)
            : base(name, folderPath, role, acceptanceCriteria, intent, workspace) { }
    }
}