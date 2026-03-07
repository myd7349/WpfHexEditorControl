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
//     - Pattern: Adapter + Null-Object (no-op when _output is null)
//     - SetOutput() is called by MainWindow whenever a TerminalPanelViewModel
//       is created (on open) or disposed (on close, set to null).
//     - Thread-safe: volatile field, no locks needed for write-only sink.
// ==========================================================

using WpfHexEditor.Core.Terminal;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.App.Services;

/// <summary>
/// Bridges the SDK <see cref="ITerminalService"/> surface to the live
/// <see cref="ITerminalOutput"/> sink of the active Terminal panel.
/// </summary>
public sealed class TerminalServiceImpl : ITerminalService
{
    private volatile ITerminalOutput? _output;

    /// <summary>
    /// Registers (or clears) the active Terminal output sink.
    /// Called by MainWindow when a TerminalPanelViewModel is created or closed.
    /// </summary>
    public void SetOutput(ITerminalOutput? output) => _output = output;

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
}
