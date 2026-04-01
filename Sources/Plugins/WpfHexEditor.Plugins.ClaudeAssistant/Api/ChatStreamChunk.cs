// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Api/ChatStreamChunk.cs
// Description: Provider-agnostic streaming chunk model for token-by-token delivery.

namespace WpfHexEditor.Plugins.ClaudeAssistant.Api;

public sealed record ChatStreamChunk(
    ChunkKind Kind,
    string? Text = null,
    string? ToolCallId = null,
    string? ToolName = null,
    string? ToolInputJson = null,
    string? ThinkingText = null,
    bool IsFinal = false,
    string? ErrorMessage = null);

public enum ChunkKind
{
    TextDelta,
    ToolUseStart,
    ToolInputDelta,
    ThinkingDelta,
    Done,
    Error
}
