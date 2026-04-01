// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Panel/ClaudeAssistantPanelViewModel.cs
// Description: Root ViewModel for the assistant panel. Manages active conversation tab.

using CommunityToolkit.Mvvm.ComponentModel;
using WpfHexEditor.Plugins.ClaudeAssistant.Api;
using WpfHexEditor.Plugins.ClaudeAssistant.Options;
using WpfHexEditor.Plugins.ClaudeAssistant.Panel.Tabs;
using WpfHexEditor.Plugins.ClaudeAssistant.Providers.Anthropic;
using WpfHexEditor.Plugins.ClaudeAssistant.Providers.Google;
using WpfHexEditor.Plugins.ClaudeAssistant.Providers.Ollama;
using WpfHexEditor.Plugins.ClaudeAssistant.Providers.OpenAI;
using WpfHexEditor.Plugins.ClaudeAssistant.Session;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Panel;

public sealed partial class ClaudeAssistantPanelViewModel : ObservableObject
{
    private readonly ModelRegistry _registry = new();

    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private ConversationTabViewModel? _activeTab;

    public string[] ProviderIds { get; }
    public string[] ModelIds { get; private set; } = [];

    public ClaudeAssistantPanelViewModel()
    {
        // Register all built-in providers
        _registry.Register(new AnthropicModelProvider());
        _registry.Register(new OpenAIModelProvider());
        _registry.Register(new GeminiModelProvider());
        _registry.Register(new OllamaModelProvider());

        ProviderIds = _registry.Providers.Select(p => p.ProviderId).ToArray();

        // Create initial conversation tab
        CreateNewTab();
    }

    public void CreateNewTab()
    {
        var opts = ClaudeAssistantOptions.Instance;
        var session = new ConversationSession
        {
            ProviderId = opts.DefaultProviderId,
            ModelId = opts.DefaultModelId,
            ThinkingEnabled = opts.DefaultThinkingEnabled,
            ThinkingBudgetTokens = opts.ThinkingBudgetTokens
        };

        var tabVm = new ConversationTabViewModel(session, _registry);
        ActiveTab = tabVm;
        UpdateModelIds(opts.DefaultProviderId);
    }

    public void UpdateModelIds(string providerId)
    {
        var provider = _registry.GetProvider(providerId);
        ModelIds = provider?.AvailableModels ?? [];
        OnPropertyChanged(nameof(ModelIds));
    }
}
