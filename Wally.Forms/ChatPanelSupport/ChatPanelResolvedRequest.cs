using Wally.Core;
using Wally.Core.Actors;

namespace Wally.Forms.ChatPanelSupport
{
    internal sealed class ChatPanelResolvedRequest
    {
        public ChatPanelRequest Request { get; init; } = new();
        public Actor? Actor { get; init; }
        public WallyLoopDefinition? LoopDefinition { get; init; }
        public string? ResolvedModel { get; init; }
        public string? ResolvedWrapperName { get; init; }
        public string ActorLabel { get; init; } = "(no actor)";
        public bool DirectMode { get; init; }
        public bool IsLooped => LoopDefinition != null;
        public string InitialPrompt { get; init; } = string.Empty;
    }
}