namespace Wally.Core
{
    public sealed class WallyLoopStepExecutionOutcome
    {
        public string ActorLabel { get; init; } = "(no actor)";

        public string Response { get; init; } = string.Empty;

        public bool RequestsPause { get; init; }

        public string? StopReason { get; init; }
    }
}