
//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

// MainWindow — Plugin System Integration
// Partial class responsible for:
//   - Constructing all service adapters (DockingAdapter, MenuAdapter, etc.)
//   - Building IDEHostContext and WpfPluginHost
//   - Calling DiscoverPluginsAsync + LoadAllAsync on startup
//   - Wiring FocusContextService on ActiveItemChanged
//   - Surfacing SlowPlugin / PluginCrash messages to OutputPanel
//   - Opening the Plugin Manager document tab (Tools > Plugin Manager)
//
// Pattern: Adapter — all App internals are wrapped in adapter interfaces
//          so PluginHost never references WpfHexEditor.App directly.

using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using WpfHexEditor.App.Services;
using WpfHexEditor.Core.Terminal;
using WpfHexEditor.Docking.Core;
using WpfHexEditor.Docking.Core.Nodes;
using WpfHexEditor.PluginHost;
using WpfHexEditor.PluginHost.Monitoring;
using WpfHexEditor.PluginHost.Services;
using WpfHexEditor.PluginHost.UI;
using WpfHexEditor.Options;
using WpfHexEditor.Editor.Core;
using WpfHexEditor.SDK.Contracts.Focus;
using WpfHexEditor.Terminal;

namespace WpfHexEditor.App;

public partial class MainWindow
{
    // --- Plugin system singletons ---------------------------------------
    private WpfPluginHost? _pluginHost;
    private IDEHostContext? _ideHostContext;
    private readonly FocusContextService _focusContextService = new();

    // Service adapters (lazily set in InitializePluginSystemAsync after layout is ready)
    private HexEditorServiceImpl? _hexEditorService;
    private OutputServiceImpl? _outputService;
    private ErrorPanelServiceImpl? _errorPanelService;
    private ThemeServiceImpl? _themeService;
    private TerminalServiceImpl? _terminalService;

    private const string PluginManagerContentId   = "plugin-manager";
    private const string TerminalPanelContentId   = "panel-terminal";
    private const string PluginMonitorContentId   = "plugin-monitor";

    private int    _pluginFaultCount = 0;
    private string? _infoBarPluginId;   // ID of the plugin currently shown in the InfoBar

    // Panels restored from a saved layout before the plugin system was ready.
    // Their DataContext is wired up at the end of InitializePluginSystemAsync.
    private TerminalPanel? _pendingTerminalPanel;
    private WpfHexEditor.Panels.IDE.Panels.PluginMonitoringPanel? _pendingPluginMonitorPanel;

    // --- Startup --------------------------------------------------------

    /// <summary>
    /// Called from OnLoaded (after layout is ready) to bootstrap the full plugin system.
    /// </summary>
    private async Task InitializePluginSystemAsync()
    {
        try
        {
            // 1. Build service adapters
            _hexEditorService = new HexEditorServiceImpl();
            _outputService = new OutputServiceImpl();
            _errorPanelService = new ErrorPanelServiceImpl();
            _themeService = new ThemeServiceImpl();
            _terminalService = new TerminalServiceImpl();

            if (_errorPanel is not null)
                _errorPanelService.SetErrorPanel(_errorPanel);

            var dockingAdapter = new DockingAdapter(_engine, _layout, DockHost, StoreContent);
            var menuAdapter = new MenuAdapter(MainMenuBar);
            var statusBarAdapter = new StatusBarAdapter(AppStatusBar);

            // 2. Build PluginHost singletons
            var permissionService = new PermissionService();
            var eventBus = new PluginEventBus();
            var uiRegistry = new UIRegistry(dockingAdapter, menuAdapter, statusBarAdapter);

            var solutionService = new SolutionExplorerServiceImpl(_solutionManager);
            solutionService.OpenFileHandler = path => Dispatcher.InvokeAsync(() => OpenStandaloneFileWithEditor(path, null)).Task;
            var codeEditorService = new NullCodeEditorService();
            var parsedFieldService = new NullParsedFieldService();

            var hostContext = new IDEHostContext(
                solutionExplorer: solutionService,
                hexEditor: _hexEditorService,
                codeEditor: codeEditorService,
                output: _outputService,
                parsedField: parsedFieldService,
                errorPanel: _errorPanelService,
                focusContext: _focusContextService,
                eventBus: eventBus,
                uiRegistry: uiRegistry,
                theme: _themeService,
                permissions: permissionService,
                terminal: _terminalService);

            // 3. Create orchestrator
            _ideHostContext = hostContext;
            _pluginHost = new WpfPluginHost(hostContext, uiRegistry, permissionService, Dispatcher);

            // 4. Subscribe to host events
            _pluginHost.PluginCrashed       += OnPluginCrashed;
            _pluginHost.SlowPluginDetected  += OnSlowPluginDetected;
            _pluginHost.PluginLoaded        += (_, _) => Dispatcher.InvokeAsync(() => UpdatePluginStatusBar());
            _pluginHost.PluginUnloaded      += (_, _) => Dispatcher.InvokeAsync(() => UpdatePluginStatusBar());

            // 5. Discover + load all plugins
            var binDir = AppDomain.CurrentDomain.BaseDirectory;
            var bundledPluginsDir = Path.Combine(binDir, "Plugins");
            await _pluginHost.LoadAllAsync(
                extraDirectories: Directory.Exists(bundledPluginsDir) ? [bundledPluginsDir] : null,
                ct: CancellationToken.None).ConfigureAwait(false);

            // Register a dynamic Options page for every plugin that supports IPluginWithOptions.
            foreach (var entry in _pluginHost.OptionsRegistry.GetAll())
            {
                var captured = entry;
                OptionsPageRegistry.RegisterDynamic(
                    "Plugins",
                    captured.PluginName,
                    () =>
                    {
                        captured.Plugin.LoadOptions();
                        var page = captured.Plugin.CreateOptionsPage();
                        if (page is System.Windows.Controls.UserControl uc) return uc;
                        // Wrap arbitrary FrameworkElement in a UserControl for the Options panel.
                        var wrapper = new System.Windows.Controls.UserControl { Content = page };
                        return wrapper;
                    });
            }

            // Wire up panels that were restored from a saved layout before the plugin
            // system was ready (DataContext was null at construction time).
            await Dispatcher.InvokeAsync(() =>
            {
                if (_pendingTerminalPanel is not null)
                {
                    var vm = new TerminalPanelViewModel(hostContext);
                    _terminalService?.SetOutput(vm);
                    _pendingTerminalPanel.DataContext = vm;
                    _pendingTerminalPanel = null;
                }

                if (_pendingPluginMonitorPanel is not null && _pluginHost is not null)
                {
                    var vm = new WpfHexEditor.Panels.IDE.Panels.ViewModels.PluginMonitoringViewModel(_pluginHost, Dispatcher);
                    _pendingPluginMonitorPanel.DataContext = vm;
                    _pendingPluginMonitorPanel = null;
                }
            });
        }
        catch (Exception ex)
        {
            OutputLogger.Error($"[PluginSystem] Failed to initialize: {ex.Message}");
        }
    }

    // --- Focus context wiring --------------------------------------------

    /// <summary>
    /// Called by OnActiveDocumentChanged to push focus changes into FocusContextService.
    /// </summary>
    private void UpdatePluginFocusContext(DockItem? activeItem)
    {
        if (activeItem is null)
        {
            _focusContextService.SetActiveDocument(null);
            return;
        }

        // Resolve the actual editor content
        _contentCache.TryGetValue(activeItem.ContentId, out var content);

        IDocument? document = content is IDocument d ? d : new SimpleDockItemDocument(activeItem, content as IDocumentEditor);
        _focusContextService.SetActiveDocument(document);
    }

    // --- Plugin notification handlers ------------------------------------

    private void OnSlowPluginDetected(object? sender, SlowPluginDetectedEventArgs e)
    {
        OutputLogger.Info(
            $"[PluginSystem] Warning: Plugin '{e.PluginName}' is slow " +
            $"(avg {e.AverageExecutionTime.TotalMilliseconds:F0} ms, " +
            $"threshold {e.Threshold.TotalMilliseconds:F0} ms).");

        Dispatcher.InvokeAsync(() => ShowPluginInfoBar(
            e.PluginId,
            $"Plugin '{e.PluginName}' is slowing down the IDE " +
            $"(avg {e.AverageExecutionTime.TotalMilliseconds:F0} ms).",
            showDisable: true, showRestart: true));
    }

    // --- Plugin InfoBar (VS-style notification banner) -------------------

    private void ShowPluginInfoBar(string pluginId, string message, bool showDisable, bool showRestart)
    {
        _infoBarPluginId = pluginId;
        PluginInfoBarMessage.Text = message;
        PluginInfoBarDisable.Visibility = showDisable  ? Visibility.Visible : Visibility.Collapsed;
        PluginInfoBarRestart.Visibility  = showRestart ? Visibility.Visible : Visibility.Collapsed;
        PluginInfoBarIgnore.Visibility   = (showDisable || showRestart) ? Visibility.Visible : Visibility.Collapsed;
        PluginInfoBar.Visibility = Visibility.Visible;
    }

    private void HidePluginInfoBar()
    {
        PluginInfoBar.Visibility = Visibility.Collapsed;
        _infoBarPluginId = null;
    }

    private void OnPluginInfoBarDisable(object sender, RoutedEventArgs e)
    {
        if (_infoBarPluginId is not null)
            _ = _pluginHost?.DisablePluginAsync(_infoBarPluginId);
        HidePluginInfoBar();
    }

    private async void OnPluginInfoBarRestart(object sender, RoutedEventArgs e)
    {
        if (_infoBarPluginId is not null && _pluginHost is not null)
            await _pluginHost.ReloadPluginAsync(_infoBarPluginId).ConfigureAwait(true);
        HidePluginInfoBar();
    }

    private void OnPluginInfoBarIgnore(object sender, RoutedEventArgs e) => HidePluginInfoBar();

    private void OnPluginInfoBarClose(object sender, RoutedEventArgs e)  => HidePluginInfoBar();

    private void OnPluginCrashed(object? sender, PluginFaultedEventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            OutputLogger.Error(
                $"[PluginSystem] Plugin '{e.PluginName}' crashed during '{e.Phase}': {e.Exception.Message}");

            _pluginFaultCount++;
            UpdatePluginStatusBar();

            ShowPluginInfoBar(
                e.PluginId,
                $"Plugin '{e.PluginName}' crashed: {e.Exception.Message}",
                showDisable: false, showRestart: true);
        });
    }

    // --- Plugin status bar ----------------------------------------------------

    /// <summary>
    /// Refreshes the plugin count and fault-warning badge in the status bar.
    /// Must be called on the Dispatcher thread.
    /// </summary>
    private void UpdatePluginStatusBar()
    {
        if (_pluginHost is null || PluginStatusText is null) return;

        var all    = _pluginHost.GetAllPlugins();
        var loaded = all.Count(e => e.State == SDK.Models.PluginState.Loaded);
        var total  = all.Count;

        PluginStatusText.Text = total == 0
            ? "No plugins"
            : loaded == total
                ? $"{loaded} plugin{(loaded == 1 ? "" : "s")}"
                : $"{loaded}/{total} plugins";

        // Color icon green = all healthy, amber = some not loaded, red = faults
        if (_pluginFaultCount > 0)
        {
            PluginStatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
        }
        else if (loaded < total)
        {
            PluginStatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
        }
        else
        {
            PluginStatusIcon.Foreground = (Brush?)TryFindResource("DockTabActiveTextBrush")
                                          ?? Brushes.White;
        }

        if (_pluginFaultCount > 0)
        {
            PluginWarningText.Text   = _pluginFaultCount.ToString();
            PluginWarningBadge.Visibility = Visibility.Visible;
        }
        else
        {
            PluginWarningBadge.Visibility = Visibility.Collapsed;
        }
    }

    private void OnPluginStatusBarClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => OnOpenPluginManager(sender, new RoutedEventArgs());

    // --- Plugin Manager document tab ------------------------------------

    private void OnOpenPluginManager(object sender, RoutedEventArgs e)
    {
        if (_pluginHost is null) return;

        // Reuse existing tab
        var existing = _layout?.FindItemByContentId(PluginManagerContentId);
        if (existing is not null)
        {
            if (existing.Owner is { } owner) owner.ActiveItem = existing;
            DockHost.RebuildVisualTree();
            return;
        }

        var vm = new PluginManagerViewModel(_pluginHost, Dispatcher);
        var control = new PluginManagerControl(vm);

        var item = new DockItem
        {
            ContentId = PluginManagerContentId,
            Title = "Plugin Manager",
            CanClose = true
        };

        StoreContent(PluginManagerContentId, control);
        _engine.Dock(item, _layout.MainDocumentHost, DockDirection.Center);
        DockHost.RebuildVisualTree();
    }

    // --- Plugin Monitor docking panel -----------------------------------

    private void OnOpenPluginMonitor(object sender, RoutedEventArgs e)
    {
        if (_pluginHost is null) return;

        var existing = _layout?.FindItemByContentId(PluginMonitorContentId);
        if (existing is not null)
        {
            if (existing.Owner is { } owner) owner.ActiveItem = existing;
            DockHost.RebuildVisualTree();
            return;
        }

        var vm      = new WpfHexEditor.Panels.IDE.Panels.ViewModels.PluginMonitoringViewModel(_pluginHost, Dispatcher);
        var control = new WpfHexEditor.Panels.IDE.Panels.PluginMonitoringPanel { DataContext = vm };

        var item = new DockItem
        {
            ContentId = PluginMonitorContentId,
            Title     = "Plugin Monitor",
            CanClose  = true
        };

        StoreContent(PluginMonitorContentId, control);

        // Dock in the existing bottom panel group (Error List / Output area) if available,
        // otherwise split a new bottom panel from the main document host.
        var bottomGroup = _layout.FindItemByContentId(ErrorPanelContentId)?.Owner
                       ?? _layout.FindItemByContentId("panel-output")?.Owner;
        if (bottomGroup is not null)
            _engine.Dock(item, bottomGroup, DockDirection.Center);
        else
            _engine.Dock(item, _layout.MainDocumentHost, DockDirection.Bottom);

        DockHost.RebuildVisualTree();
    }

    // --- Content factories for panels that may be restored from layout --

    /// <summary>
    /// Creates the Terminal panel during layout restoration.
    /// If the plugin system is not yet initialised, the DataContext is deferred.
    /// </summary>
    private UIElement CreateTerminalPanelContent()
    {
        var panel = new TerminalPanel();

        if (_ideHostContext is not null)
        {
            var vm = new TerminalPanelViewModel(_ideHostContext);
            _terminalService?.SetOutput(vm);
            panel.DataContext = vm;
        }
        else
        {
            _pendingTerminalPanel = panel;   // wire up after InitializePluginSystemAsync
        }

        return panel;
    }

    /// <summary>
    /// Creates the Plugin Monitor panel during layout restoration.
    /// If the plugin system is not yet initialised, the DataContext is deferred.
    /// </summary>
    private UIElement CreatePluginMonitorPanelContent()
    {
        var panel = new WpfHexEditor.Panels.IDE.Panels.PluginMonitoringPanel();

        if (_pluginHost is not null)
        {
            var vm = new WpfHexEditor.Panels.IDE.Panels.ViewModels.PluginMonitoringViewModel(_pluginHost, Dispatcher);
            panel.DataContext = vm;
        }
        else
        {
            _pendingPluginMonitorPanel = panel;   // wire up after InitializePluginSystemAsync
        }

        return panel;
    }

    // --- Terminal panel --------------------------------------------------

    private void OnOpenTerminal(object sender, RoutedEventArgs e)
    {
        // Reuse existing panel
        var existing = _layout?.FindItemByContentId(TerminalPanelContentId);
        if (existing is not null)
        {
            if (existing.Owner is { } owner) owner.ActiveItem = existing;
            DockHost.RebuildVisualTree();
            return;
        }

        if (_ideHostContext is null) { OutputLogger.Error("[Terminal] Host context unavailable."); return; }

        var vm      = new TerminalPanelViewModel(_ideHostContext);
        _terminalService?.SetOutput(vm);
        var control = new TerminalPanel { DataContext = vm };

        var item = new DockItem
        {
            ContentId = TerminalPanelContentId,
            Title     = "Terminal",
            CanClose  = true
        };

        StoreContent(TerminalPanelContentId, control);

        // Dock in the existing bottom panel group (Error List / Output area) if available,
        // otherwise split a new bottom panel from the main document host.
        var bottomGroup = _layout.FindItemByContentId(ErrorPanelContentId)?.Owner
                       ?? _layout.FindItemByContentId("panel-output")?.Owner;
        if (bottomGroup is not null)
            _engine.Dock(item, bottomGroup, DockDirection.Center);
        else
            _engine.Dock(item, _layout.MainDocumentHost, DockDirection.Bottom);

        DockHost.RebuildVisualTree();
    }

    // --- Plugin system shutdown ------------------------------------------

    private async Task ShutdownPluginSystemAsync()
    {
        if (_pluginHost is null) return;
        _pluginHost.PluginCrashed      -= OnPluginCrashed;
        _pluginHost.SlowPluginDetected -= OnSlowPluginDetected;
        // PluginLoaded/PluginUnloaded lambdas are fire-and-forget; no need to unsubscribe explicitly.
        await _pluginHost.DisposeAsync().ConfigureAwait(false);
        _pluginHost = null;
    }

    // --- Minimal helpers ------------------------------------------------

    /// <summary>
    /// Minimal IDocument adapter wrapping a DockItem for FocusContextService.
    /// </summary>
    private sealed class SimpleDockItemDocument : IDocument
    {
        private readonly DockItem           _item;
        private readonly IDocumentEditor?   _editor;

        public SimpleDockItemDocument(DockItem item, IDocumentEditor? editor = null)
        {
            _item   = item;
            _editor = editor;
        }

        public string  ContentId    => _item.ContentId;
        public string  Title        => _item.Title ?? _item.ContentId;
        public string? FilePath     => _item.Metadata?.TryGetValue("FilePath", out var fp) == true ? fp : null;
        public string  DocumentType => "DockItem";
        public bool    IsDirty      => _editor?.IsDirty ?? false;
        public bool    CanUndo      => _editor?.CanUndo ?? false;
        public bool    CanRedo      => _editor?.CanRedo ?? false;
        public void    Undo()       => _editor?.Undo();
        public void    Redo()       => _editor?.Redo();
    }
}
