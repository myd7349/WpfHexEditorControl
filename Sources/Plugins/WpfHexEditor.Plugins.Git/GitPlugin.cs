// ==========================================================
// Project: WpfHexEditor.Plugins.Git
// File: GitPlugin.cs
// Description:
//     Entry point for the Git Integration plugin.
//     Registers GitVersionControlService via ExtensionRegistry,
//     creates GitChangesPanel, wires blame to open documents,
//     and updates the status bar on StatusChanged.
// Architecture Notes:
//     Pattern: IWpfHexEditorPlugin — auto-discovered by PluginHost.
//     No direct reference to WpfHexEditor.App (fully decoupled).
//     Git state is polled every 5s; blame is loaded on file open.
// ==========================================================

using System.Windows;
using WpfHexEditor.Core.Events.IDEEvents;
using WpfHexEditor.Editor.Core.LSP;
using WpfHexEditor.Plugins.Git.Services;
using WpfHexEditor.Plugins.Git.ViewModels;
using WpfHexEditor.Plugins.Git.Views;
using WpfHexEditor.SDK.Commands;
using WpfHexEditor.SDK.Contracts;
using WpfHexEditor.SDK.Descriptors;
using WpfHexEditor.SDK.Models;

namespace WpfHexEditor.Plugins.Git;

/// <summary>Git integration plugin entry point.</summary>
public sealed class GitPlugin : IWpfHexEditorPlugin
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public string  Id      => "WpfHexEditor.Plugins.Git";
    public string  Name    => "Git Integration";
    public Version Version => new(1, 0, 0);

    public PluginCapabilities Capabilities => new()
    {
        AccessFileSystem = true,
        RegisterMenus    = true,
        WriteOutput      = true
    };

    // ── UI IDs ────────────────────────────────────────────────────────────────

    private const string GitChangesPanelId = "WpfHexEditor.Plugins.Git.Panel.Changes";

    // ── State ─────────────────────────────────────────────────────────────────

    private IIDEHostContext?           _context;
    private GitVersionControlService?  _vcs;
    private GitChangesPanelViewModel?  _changesVm;
    private readonly CancellationTokenSource _cts = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public async Task InitializeAsync(IIDEHostContext context, CancellationToken ct = default)
    {
        _context = context;

        _vcs = new GitVersionControlService(
            System.Windows.Threading.Dispatcher.CurrentDispatcher);

        // Register as IVersionControlService so context.VersionControl resolves it
        context.ExtensionRegistry.Register<IVersionControlService>(Id, _vcs);

        // Create panel VM and panel
        _changesVm = new GitChangesPanelViewModel(_vcs, context.Output);
        var panel  = new GitChangesPanel { DataContext = _changesVm };

        // Register dockable panel
        context.UIRegistry.RegisterPanel(
            GitChangesPanelId,
            panel,
            Id,
            new PanelDescriptor
            {
                Title           = "Git Changes",
                DefaultDockSide = "Bottom",
                CanClose        = true,
                Category        = "Source Control"
            });

        // Subscribe to file open → update VCS root + trigger blame load
        context.HexEditor.FileOpened += OnFileOpened;

        // Subscribe to VCS status changes → publish IDE event
        _vcs.StatusChanged += OnVcsStatusChanged;

        // Register View menu items
        context.UIRegistry.RegisterMenuItem(
            "WpfHexEditor.Plugins.Git.Menu.GitChanges", Id,
            new MenuItemDescriptor
            {
                ParentPath = "View",
                Header     = "_Git Changes",
                Group      = "Git",
                IconGlyph  = "\uE943",
                Command    = new RelayCommand(_ => context.UIRegistry.TogglePanel(GitChangesPanelId))
            });

        context.UIRegistry.RegisterMenuItem(
            "WpfHexEditor.Plugins.Git.Menu.BlameGutter", Id,
            new MenuItemDescriptor
            {
                ParentPath = "View",
                Header     = "Toggle _Blame Gutter",
                Group      = "Git",
                IconGlyph  = "\uE90F",
                Command    = new RelayCommand(_ => ToggleBlameGutter(context))
            });

        // Initial refresh on active file
        var current = context.HexEditor.CurrentFilePath;
        if (!string.IsNullOrEmpty(current))
        {
            _vcs.SetActiveFile(current);
            _vcs.StartPolling();
        }

        await Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _cts.Cancel();
        _vcs?.StopPolling();
        _vcs?.Dispose();

        if (_context is not null)
            _context.HexEditor.FileOpened -= OnFileOpened;

        return Task.CompletedTask;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnFileOpened(object? sender, EventArgs e)
    {
        if (_context is null || _vcs is null) return;
        var path = _context.HexEditor.CurrentFilePath;
        _vcs.SetActiveFile(path);
        _vcs.StartPolling();
        LoadBlameAsync(path);
    }

    private async void LoadBlameAsync(string? filePath)
    {
        if (filePath is null || _context is null || _vcs is null) return;
        try
        {
            var entries = await _vcs.GetBlameAsync(filePath, _cts.Token);
            // Publish event — BlameGutterControl in CodeEditor subscribes via IDEEventBus
            _context.IDEEvents.Publish(new GitBlameLoadedEvent(filePath, entries.Count));
            // Also store entries so IDEHostContext.VersionControl.GetBlameAsync callers can use them
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _context.Output.Write("Git", $"Blame load failed: {ex.Message}");
        }
    }

    private static void ToggleBlameGutter(IIDEHostContext context)
    {
        // BlameGutterControl toggles itself via IDEEventBus subscription
        context.IDEEvents.Publish(new GitStatusChangedEvent(null, false, 0));
    }

    private void OnVcsStatusChanged(object? sender, EventArgs e)
    {
        if (_context is null || _vcs is null) return;

        _changesVm?.RefreshAsync();
        _context.IDEEvents.Publish(new GitStatusChangedEvent(
            _vcs.BranchName,
            _vcs.IsDirty,
            0));
    }
}
