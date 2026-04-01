// ==========================================================
// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: ClaudeTitleBarContributor.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-03-31
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     ITitleBarContributor implementation. Left-click opens command palette,
//     right-click shows context menu with quick actions.
// ==========================================================
using System.Windows;
using WpfHexEditor.Plugins.ClaudeAssistant.Connection;
using WpfHexEditor.SDK.Contracts;

namespace WpfHexEditor.Plugins.ClaudeAssistant.TitleBar;

public sealed class ClaudeTitleBarContributor : ITitleBarContributor
{
    private readonly ClaudeConnectionService _connectionService;
    private readonly Action _showCommandPalette;
    private readonly Action _togglePanel;
    private readonly Action _newTab;
    private ClaudeTitleBarButton? _button;

    public string ContributorId => "ClaudeAssistant.TitleBar";
    public int Order => 10;

    public ClaudeTitleBarContributor(
        ClaudeConnectionService connectionService,
        Action showCommandPalette,
        Action togglePanel,
        Action newTab)
    {
        _connectionService = connectionService;
        _showCommandPalette = showCommandPalette;
        _togglePanel = togglePanel;
        _newTab = newTab;
        _connectionService.StatusChanged += OnStatusChanged;
    }

    public UIElement CreateButton()
    {
        _button = new ClaudeTitleBarButton();
        _button.TogglePanelRequested += () => SafeGuard.Run(_togglePanel);
        _button.NewTabRequested += () => SafeGuard.Run(_newTab);
        _button.AskSelectionRequested += () => SafeGuard.Run(_showCommandPalette);
        _button.UpdateStatus(_connectionService.Status);
        return _button;
    }

    private void OnStatusChanged(object? sender, ClaudeConnectionStatus status)
    {
        _button?.Dispatcher.InvokeAsync(() => _button.UpdateStatus(status));
    }
}
