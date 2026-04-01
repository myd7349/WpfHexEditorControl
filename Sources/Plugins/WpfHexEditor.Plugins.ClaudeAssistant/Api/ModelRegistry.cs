// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Api/ModelRegistry.cs
// Description: Registry of all available model providers. Providers register at startup.

namespace WpfHexEditor.Plugins.ClaudeAssistant.Api;

public sealed class ModelRegistry
{
    private readonly Dictionary<string, IModelProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<IModelProvider> Providers => _providers.Values;

    public void Register(IModelProvider provider)
    {
        _providers[provider.ProviderId] = provider;
    }

    public IModelProvider? GetProvider(string providerId)
    {
        _providers.TryGetValue(providerId, out var provider);
        return provider;
    }

    public IEnumerable<(string ProviderId, string ModelId)> GetAllModels()
    {
        foreach (var p in _providers.Values)
            foreach (var m in p.AvailableModels)
                yield return (p.ProviderId, m);
    }
}
