// ==========================================================
// Project: WpfHexEditor.SDK.Terminal.Abstractions
// File: ITerminalHostContext.cs
// Description:
//     Minimal host-context contract for WpfTerminal standalone NuGet package.
//     Replaces the direct IIDEHostContext dependency so the Terminal package
//     remains usable without the IDE shell (ADR-P1.2 nuget-guard fix).
//     In the IDE, TerminalHostContextAdapter bridges to IIDEHostContext.
// ==========================================================

namespace WpfHexEditor.SDK.Contracts.Terminal;

/// <summary>
/// Minimal host context for terminal sessions. Exposes only what
/// <see cref="ShellSessionViewModel"/> actually needs, without pulling in
/// IDE-only types (<c>IIDEHostContext</c>, <c>IDockManager</c>, etc.).
/// </summary>
public interface ITerminalHostContext
{
    /// <summary>
    /// Publishes a terminal-command-executed notification to any interested listener.
    /// In standalone mode, this is a no-op. In the IDE, it routes to IIDEEventBus.
    /// </summary>
    void PublishCommandExecuted(string source, string command, string shellType);

    /// <summary>
    /// Returns the path of the currently active document, or <see langword="null"/>
    /// when no document is open or the host does not support focus tracking.
    /// </summary>
    string? ActiveDocumentPath { get; }

    /// <summary>
    /// Returns the title of the currently active panel, or <see langword="null"/>
    /// when the host does not support focus tracking.
    /// </summary>
    string? ActivePanelTitle { get; }
}
