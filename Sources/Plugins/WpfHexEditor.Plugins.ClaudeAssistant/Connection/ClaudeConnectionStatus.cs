// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Connection/ClaudeConnectionStatus.cs
// Description: Connection state enum and info record for the connection module.

namespace WpfHexEditor.Plugins.ClaudeAssistant.Connection;

public enum ClaudeConnectionStatus
{
    NotConfigured,
    Connecting,
    Connected,
    RateLimited,
    Error,
    Offline
}

public sealed record ConnectionInfo(
    string ProviderId,
    string ModelId,
    int LatencyMs,
    ClaudeConnectionStatus Status,
    string? ErrorMessage = null);
