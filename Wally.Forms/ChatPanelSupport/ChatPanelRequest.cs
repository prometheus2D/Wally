namespace Wally.Forms.ChatPanelSupport
{
    internal sealed class ChatPanelRequest
    {
        public string Prompt { get; init; } = string.Empty;
        public string? ActorName { get; init; }
        public string? LoopName { get; init; }
        public string? ModelOverride { get; init; }
        public string? WrapperName { get; init; }
        public bool NoHistory { get; init; }
        public ChatPanelExecutionMode Mode { get; init; } = ChatPanelExecutionMode.Ask;

        public string DisplayPrompt => Prompt.Trim();

        public string DisplayLabel => string.IsNullOrWhiteSpace(ActorName)
            ? "AI"
            : ActorName!;

        public ChatPanelRequest WithPrompt(string prompt)
        {
            return new ChatPanelRequest
            {
                Prompt = prompt,
                ActorName = ActorName,
                LoopName = LoopName,
                ModelOverride = ModelOverride,
                WrapperName = WrapperName,
                NoHistory = NoHistory,
                Mode = Mode
            };
        }
    }
}