// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Api/IModelProvider.cs
// Description: Provider-agnostic interface for AI model backends (Anthropic, OpenAI, Gemini, Ollama).

namespace WpfHexEditor.Plugins.ClaudeAssistant.Api;

public interface IModelProvider
{
    string ProviderId { get; }
    string DisplayName { get; }
    string[] AvailableModels { get; }
    bool SupportsTools { get; }
    bool SupportsVision { get; }
    bool SupportsThinking { get; }
    int MaxContextTokens { get; }

    Task<bool> TestConnectionAsync(CancellationToken ct = default);

    IAsyncEnumerable<ChatStreamChunk> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string modelId,
        IReadOnlyList<ToolDefinition>? tools = null,
        ThinkingConfig? thinking = null,
        CancellationToken ct = default);
}

public sealed record ToolDefinition(string Name, string Description, string InputSchemaJson);
