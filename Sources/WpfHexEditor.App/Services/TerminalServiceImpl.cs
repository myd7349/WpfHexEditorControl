// ==========================================================
// Project: WpfHexEditor.App
// File: TerminalServiceImpl.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-07
// Description:
//     Adapter that implements the SDK ITerminalService contract by
//     forwarding calls to a live ITerminalOutput sink (TerminalPanelViewModel).
//     When no terminal panel is open, all write calls are silent no-ops.
//
// Architecture Notes:
//     - Pattern: Adapter + Null-Object (no-op when sink is null)
//     - SetOutput()       — called by MainWindow when a TerminalPanelViewModel is created or closed.
//     - SetSessionManager() — wires IShellSessionManager for OpenSession / CloseActiveSession (Feature #92).
//     - SetRegistry()     — wires TerminalCommandRegistry for RegisterCommand / UnregisterCommand.
//       All three follow the same null-object pattern: silently no-op when null.
//     - Thread-safe: volatile fields, no locks needed.
// ==========================================================

using WpfHexEditor.Core.Terminal;
using WpfHexEditor.Core.Terminal.ShellSession;
using WpfHexEditor.SDK.Contracts.Services;
using WpfHexEditor.SDK.Contracts.Terminal;

namespace WpfHexEditor.App.Services;

/// <summary>
/// Bridges the SDK <see cref="ITerminalService"/> surface to the live
/// <see cref="ITerminalOutput"/> sink of the active Terminal panel and
/// the <see cref="IShellSessionManager"/> for multi-tab session control.
/// </summary>
public sealed class TerminalServiceImpl : ITerminalService
{
    private volatile ITerminalOutput?        _output;
    private volatile IShellSessionManager?   _sessionManager;
    private volatile TerminalCommandRegistry? _registry;

    /// <summary>
    /// Registers (or clears) the active Terminal output sink.
    /// Called by MainWindow when a TerminalPanelViewModel is created or closed.
    /// </summary>
    public void SetOutput(ITerminalOutput? output) => _output = output;

    /// <summary>
    /// Registers (or clears) the session manager used by OpenSession / CloseActiveSession.
    /// Called by MainWindow.PluginSystem.cs after the terminal panel is initialised.
    /// </summary>
    public void SetSessionManager(IShellSessionManager? sessionManager)
        => _sessionManager = sessionManager;

    /// <summary>
    /// Registers (or clears) the command registry used by RegisterCommand / UnregisterCommand.
    /// Called by MainWindow.PluginSystem.cs after the TerminalPanelViewModel is created or closed.
    /// </summary>
    public void SetRegistry(TerminalCommandRegistry? registry) => _registry = registry;

    /// <inheritdoc />
    public void WriteLine(string text) => _output?.WriteLine(text);

    /// <inheritdoc />
    public void WriteInfo(string text) => _output?.WriteInfo(text);

    /// <inheritdoc />
    public void WriteWarning(string text) => _output?.WriteWarning(text);

    /// <inheritdoc />
    public void WriteError(string text) => _output?.WriteError(text);

    /// <inheritdoc />
    public void Clear() => _output?.Clear();

    /// <inheritdoc />
    public void OpenSession(string shellType)
    {
        var manager = _sessionManager;
        if (manager is null) return;

        var mode = shellType.ToLowerInvariant() switch
        {
            "powershell" => TerminalShellType.PowerShell,
            "bash"       => TerminalShellType.Bash,
            "cmd"        => TerminalShellType.Cmd,
            _            => TerminalShellType.HxTerminal
        };

        manager.CreateSession(mode);
    }

    /// <inheritdoc />
    public void CloseActiveSession()
    {
        var manager = _sessionManager;
        if (manager?.ActiveSession is { } active)
            manager.CloseSession(active.Id);
    }

    /// <inheritdoc />
    public void RegisterCommand(ITerminalCommandProvider command)
        => _registry?.Register(command);

    /// <inheritdoc />
    public void UnregisterCommand(string commandName)
        => _registry?.Unregister(commandName);
}
