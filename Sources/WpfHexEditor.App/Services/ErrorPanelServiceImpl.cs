
//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using WpfHexEditor.Shell.Panels.Panels;
using WpfHexEditor.SDK.Contracts.Services;
using CoreEntry = WpfHexEditor.Editor.Core.DiagnosticEntry;
using CoreSeverity = WpfHexEditor.Editor.Core.DiagnosticSeverity;
using CoreSource = WpfHexEditor.Editor.Core.IDiagnosticSource;

namespace WpfHexEditor.App.Services;

/// <summary>
/// Adapts the ErrorPanel (IDiagnosticSource) to the IErrorPanelService SDK contract.
/// Each plugin gets an isolated diagnostic source keyed by its plugin ID.
/// </summary>
public sealed class ErrorPanelServiceImpl : IErrorPanelService
{
    private ErrorPanel? _errorPanel;
    private readonly Dictionary<string, PluginDiagnosticSource> _sources =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Called by MainWindow once the ErrorPanel is created.</summary>
    public void SetErrorPanel(ErrorPanel panel) => _errorPanel = panel;

    public void PostDiagnostic(DiagnosticSeverity severity, string message,
                               string source = "", int line = -1, int column = -1)
    {
        if (_errorPanel is null) return;

        // Use source as the plugin identifier; fall back to "Plugin" if empty.
        var pluginId = string.IsNullOrWhiteSpace(source) ? "Plugin" : source;
        var ds = GetOrCreateSource(pluginId);
        // Map SDK DiagnosticSeverity (Info=0,Warning=1,Error=2) to Core DiagnosticSeverity (Error=0,Warning=1,Message=2).
        var coreSev = severity switch
        {
            DiagnosticSeverity.Error   => CoreSeverity.Error,
            DiagnosticSeverity.Warning => CoreSeverity.Warning,
            _                          => CoreSeverity.Message
        };
        ds.Add(new CoreEntry(coreSev, Code: "", Description: message,
                             ProjectName: pluginId, Line: line, Column: column));
    }

    public void ClearPluginDiagnostics(string pluginId)
    {
        if (_sources.TryGetValue(pluginId, out var ds)) ds.Clear();
    }

    public IReadOnlyList<string> GetRecentErrors(int count)
    {
        // Aggregate entries from all registered sources, newest last, limited to count.
        lock (_sources)
        {
            return _sources.Values
                .SelectMany(s => s.GetDiagnostics())
                .TakeLast(count)
                .Select(e => $"[{e.Severity}] {e.ProjectName}: {e.Description}")
                .ToList();
        }
    }

    private PluginDiagnosticSource GetOrCreateSource(string pluginId)
    {
        if (!_sources.TryGetValue(pluginId, out var ds))
        {
            ds = new PluginDiagnosticSource(pluginId);
            _sources[pluginId] = ds;
            _errorPanel?.AddSource(ds);
        }
        return ds;
    }

    /// <summary>Per-plugin diagnostic source forwarded to ErrorPanel.</summary>
    private sealed class PluginDiagnosticSource : CoreSource
    {
        private readonly List<CoreEntry> _entries = new();
        private readonly object _lock = new();

        public string SourceLabel { get; }
        public event EventHandler? DiagnosticsChanged;

        public PluginDiagnosticSource(string pluginId) =>
            SourceLabel = $"Plugin: {pluginId}";

        public IReadOnlyList<CoreEntry> GetDiagnostics()
        {
            lock (_lock) return _entries.ToList();
        }

        public void Add(CoreEntry entry)
        {
            lock (_lock) _entries.Add(entry);
            DiagnosticsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            lock (_lock) _entries.Clear();
            DiagnosticsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
