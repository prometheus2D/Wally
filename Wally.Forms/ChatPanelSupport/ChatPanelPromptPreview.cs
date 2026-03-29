using System.Collections.Generic;

namespace Wally.Forms.ChatPanelSupport
{
    internal sealed class ChatPanelPromptPreviewSection
    {
        public string Heading { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
    }

    internal sealed class ChatPanelPromptPreview
    {
        public ChatPanelRequest Request { get; init; } = new();
        public string Title { get; init; } = string.Empty;
        public string ExactPrompt { get; init; } = string.Empty;
        public string NextExecutionLabel { get; init; } = string.Empty;
        public bool IsCompletionPreview { get; init; }
        public List<ChatPanelPromptPreviewSection> Sections { get; init; } = new();
    }
}