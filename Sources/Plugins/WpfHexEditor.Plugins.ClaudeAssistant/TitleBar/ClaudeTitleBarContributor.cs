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
    private readonly Action<UIElement?> _showCommandPalette;
    private readonly Action _newTab;
    private readonly Action _fixErrors;
    private readonly Action _openOptions;
    private readonly Action _manageConnections;
    private ClaudeTitleBarButton? _button;

    public string ContributorId => "ClaudeAssistant.TitleBar";
    public int Order => 10;

    public ClaudeTitleBarContributor(
        ClaudeConnectionService connectionService,
        Action<UIElement?> showCommandPalette,
        Action newTab,
        Action fixErrors,
        Action openOptions,
        Action manageConnections)
    {
        _connectionService = connectionService;
        _showCommandPalette = showCommandPalette;
        _newTab = newTab;
        _fixErrors = fixErrors;
        _openOptions = openOptions;
        _manageConnections = manageConnections;
        _connectionService.StatusChanged += OnStatusChanged;
    }

    public UIElement CreateButton()
    {
        _button = new ClaudeTitleBarButton();
        _button.ShowCommandPaletteRequested += () => SafeGuard.Run(() => _showCommandPalette(_button));
        _button.NewTabRequested += () => SafeGuard.Run(_newTab);
        _button.AskSelectionRequested += () => SafeGuard.Run(() => _showCommandPalette(_button));
        _button.FixErrorsRequested += () => SafeGuard.Run(_fixErrors);
        _button.OpenOptionsRequested += () => SafeGuard.Run(_openOptions);
        _button.ManageConnectionsRequested += () => SafeGuard.Run(_manageConnections);
        _button.UpdateStatus(_connectionService.Status);
        return _button;
    }

    private void OnStatusChanged(object? sender, ClaudeConnectionStatus status)
    {
        _button?.Dispatcher.InvokeAsync(() => _button.UpdateStatus(status));
    }
}
