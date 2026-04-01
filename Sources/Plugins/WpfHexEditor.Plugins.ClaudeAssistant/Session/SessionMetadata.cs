// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Session/SessionMetadata.cs
// Description: Lightweight metadata for conversation index (no full message history).

namespace WpfHexEditor.Plugins.ClaudeAssistant.Session;

public sealed class SessionMetadata
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string ProviderId { get; set; } = "";
    public string ModelId { get; set; } = "";
    public int MessageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
}
