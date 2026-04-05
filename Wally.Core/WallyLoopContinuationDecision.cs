namespace Wally.Core
{
    public sealed class WallyLoopContinuationDecision
    {
        public string Status { get; init; } = "Running";

        public string NextStepName { get; init; } = string.Empty;

        public string? StopReason { get; init; }

        public string? RouteKeyword { get; init; }

        public bool UsedDefaultRoute { get; init; }

        public bool IsTerminal =>
            !string.Equals(Status, "Running", System.StringComparison.OrdinalIgnoreCase);
    }
}