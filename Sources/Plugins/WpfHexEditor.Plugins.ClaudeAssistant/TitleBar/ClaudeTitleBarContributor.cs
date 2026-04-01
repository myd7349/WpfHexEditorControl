// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: TitleBar/ClaudeTitleBarContributor.cs
// Description: ITitleBarContributor implementation — creates the Claude button for the IDE title bar.

using System.Windows;
using WpfHexEditor.Plugins.ClaudeAssistant.Connection;
using WpfHexEditor.SDK.Contracts;

namespace WpfHexEditor.Plugins.ClaudeAssistant.TitleBar;

public sealed class ClaudeTitleBarContributor : ITitleBarContributor
{
    private readonly ClaudeConnectionService _connectionService;
    private readonly Action _togglePanel;
    private ClaudeTitleBarButton? _button;

    public string ContributorId => "ClaudeAssistant.TitleBar";
    public int Order => 10;

    public ClaudeTitleBarContributor(ClaudeConnectionService connectionService, Action togglePanel)
    {
        _connectionService = connectionService;
        _togglePanel = togglePanel;

        _connectionService.StatusChanged += OnStatusChanged;
    }

    public UIElement CreateButton()
    {
        _button = new ClaudeTitleBarButton();
        _button.TogglePanelRequested += () => _togglePanel();
        // TODO Phase 2+: wire NewTabRequested, AskSelectionRequested, etc.
        _button.UpdateStatus(_connectionService.Status);
        return _button;
    }

    private void OnStatusChanged(object? sender, ClaudeConnectionStatus status)
    {
        _button?.Dispatcher.InvokeAsync(() => _button.UpdateStatus(status));
    }
}
