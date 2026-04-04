using System;
using System.IO;
using System.Text.Json;
using Wally.Core;
using Wally.Core.Logging;

namespace Wally.Forms.ChatPanelSupport
{
    internal sealed class ChatPanelSessionRecorder : IDisposable
    {
        private readonly object _gate = new();
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private bool _completed;

        private ChatPanelSessionRecorder(string filePath)
        {
            FilePath = filePath;
            SessionId = Guid.NewGuid().ToString("N");
            StartedAtUtc = DateTimeOffset.UtcNow;
        }

        public string SessionId { get; }

        public DateTimeOffset StartedAtUtc { get; }

        public string FilePath { get; }

        public static ChatPanelSessionRecorder Start(
            WallyEnvironment env,
            ChatPanelResolvedRequest resolved,
            string pacingMode,
            string commandText)
        {
            ArgumentNullException.ThrowIfNull(env);
            ArgumentNullException.ThrowIfNull(resolved);

            if (!env.HasWorkspace)
                throw new InvalidOperationException("A workspace is required to start ChatPanel session logging.");

            string relativePath = $"{ConversationLogger.DefaultFolderName}/ChatSessions/chatpanel-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.jsonl";
            string filePath = env.ResolveWorkspaceFilePath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var recorder = new ChatPanelSessionRecorder(filePath);
            recorder.Write(new ChatPanelSessionEntry
            {
                Timestamp = recorder.StartedAtUtc,
                SessionId = recorder.SessionId,
                EventType = "start",
                Prompt = resolved.Request.DisplayPrompt,
                ActorName = resolved.DirectMode ? null : resolved.ActorLabel,
                LoopName = resolved.LoopDefinition?.Name,
                Model = resolved.ResolvedModel,
                WrapperName = resolved.ResolvedWrapperName,
                Mode = resolved.Request.Mode.ToString(),
                PacingMode = pacingMode,
                CommandText = commandText,
                Message = "ChatPanel session started."
            });
            return recorder;
        }

        public void RecordMessage(string sender, string text, string messageKind)
        {
            Write(new ChatPanelSessionEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                SessionId = SessionId,
                EventType = "message",
                Sender = sender,
                MessageKind = messageKind,
                Message = text
            });
        }

        public void RecordEvent(
            string eventType,
            string message,
            string? stepName = null,
            int? iteration = null,
            string? stopReason = null)
        {
            Write(new ChatPanelSessionEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                SessionId = SessionId,
                EventType = eventType,
                Message = message,
                StepName = stepName,
                Iteration = iteration,
                StopReason = stopReason
            });
        }

        public void Complete(string outcome, string? stopReason = null, int resultCount = 0)
        {
            lock (_gate)
            {
                if (_completed)
                    return;

                WriteUnsafe(new ChatPanelSessionEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    SessionId = SessionId,
                    EventType = "complete",
                    Message = outcome,
                    ResultCount = resultCount,
                    StopReason = stopReason
                });
                _completed = true;
            }
        }

        public void Dispose()
        {
            Complete("Disposed");
        }

        private void Write(ChatPanelSessionEntry entry)
        {
            lock (_gate)
            {
                if (_completed)
                    return;

                WriteUnsafe(entry);
            }
        }

        private void WriteUnsafe(ChatPanelSessionEntry entry)
        {
            string json = JsonSerializer.Serialize(entry, _jsonOptions);
            File.AppendAllText(FilePath, json + Environment.NewLine);
        }

        private sealed class ChatPanelSessionEntry
        {
            public DateTimeOffset Timestamp { get; set; }

            public string SessionId { get; set; } = string.Empty;

            public string EventType { get; set; } = string.Empty;

            public string? Sender { get; set; }

            public string? MessageKind { get; set; }

            public string? Message { get; set; }

            public string? Prompt { get; set; }

            public string? ActorName { get; set; }

            public string? LoopName { get; set; }

            public string? StepName { get; set; }

            public int? Iteration { get; set; }

            public int? ResultCount { get; set; }

            public string? StopReason { get; set; }

            public string? Model { get; set; }

            public string? WrapperName { get; set; }

            public string? Mode { get; set; }

            public string? PacingMode { get; set; }

            public string? CommandText { get; set; }
        }
    }
}