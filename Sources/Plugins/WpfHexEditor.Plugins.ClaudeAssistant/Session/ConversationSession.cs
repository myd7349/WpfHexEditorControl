// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Session/ConversationSession.cs
// Description: Represents a single conversation with message history, provider, and model state.

using WpfHexEditor.Plugins.ClaudeAssistant.Api;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Session;

public sealed class ConversationSession
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "New conversation";
    public string ProviderId { get; set; } = "anthropic";
    public string ModelId { get; set; } = "claude-sonnet-4-6";
    public bool ThinkingEnabled { get; set; }
    public int ThinkingBudgetTokens { get; set; } = 8192;
    public List<ChatMessage> Messages { get; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    public void AddMessage(ChatMessage msg)
    {
        Messages.Add(msg);
        LastModifiedAt = DateTime.UtcNow;
    }
}
