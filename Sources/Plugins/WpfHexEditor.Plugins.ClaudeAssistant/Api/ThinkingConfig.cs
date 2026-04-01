// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Api/ThinkingConfig.cs
// Description: Configuration for Claude extended thinking. Ignored by non-Anthropic providers.

namespace WpfHexEditor.Plugins.ClaudeAssistant.Api;

public sealed record ThinkingConfig(bool Enabled, int BudgetTokens = 8192);
