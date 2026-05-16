// ==========================================================
// Project: WpfHexEditor.App
// File: TerminalHostContextAdapter.cs
// Description:
//     Bridges IIDEHostContext to ITerminalHostContext so WpfTerminal NuGet
//     package stays free of IDE-only references (nuget-guard ADR-P1.2 fix).
//     Lives in WpfHexEditor.App which is allowed to reference both sides.
// ==========================================================

using WpfHexEditor.Core.Events.IDEEvents;
using WpfHexEditor.SDK.Contracts;
using WpfHexEditor.SDK.Contracts.Terminal;

namespace WpfHexEditor.App.Services;

/// <summary>
/// Adapts <see cref="IIDEHostContext"/> to the <see cref="ITerminalHostContext"/>
/// contract expected by <c>TerminalPanelViewModel</c> and <c>ShellSessionViewModel</c>.
/// </summary>
internal sealed class TerminalHostContextAdapter : ITerminalHostContext
{
    private readonly IIDEHostContext _ide;

    public TerminalHostContextAdapter(IIDEHostContext ide)
        => _ide = ide ?? throw new ArgumentNullException(nameof(ide));

    public string? ActiveDocumentPath
        => _ide.FocusContext.ActiveDocument?.FilePath;

    public string? ActivePanelTitle
        => _ide.FocusContext.ActivePanel?.Title;

    public void PublishCommandExecuted(string source, string command, string shellType)
        => _ide.IDEEvents.Publish(new TerminalCommandExecutedEvent
        {
            Source    = source,
            Command   = command,
            ShellType = shellType,
        });
}
