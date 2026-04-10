// ==========================================================
// Project: WpfHexEditor.Plugins.LSPTools
// File: Commands/LspCallHierarchyCommand.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-30
// Description: HxTerminal command — show the Call Hierarchy panel.
// ==========================================================

using WpfHexEditor.SDK.Contracts.Terminal;

namespace WpfHexEditor.Plugins.LSPTools.Commands;

internal sealed class LspCallHierarchyCommand : PluginTerminalCommandBase
{
    private const string PanelUiId = "WpfHexEditor.Plugins.LSPTools.Panel.CallHierarchy";

    public override string CommandName => "lsp-call-hierarchy";
    public override string Description => "Show the LSP Call Hierarchy panel (Shift+Alt+H).";
    public override string Usage       => "lsp-call-hierarchy";

    protected override Task<int> ExecuteCoreAsync(
        string[] args, ITerminalOutput output, ITerminalContext ctx, CancellationToken ct)
    {
        ctx.IDE().UIRegistry.ShowPanel(PanelUiId);
        output.WriteInfo("Call Hierarchy panel opened. Trigger with Shift+Alt+H on a symbol.");
        return Task.FromResult(0);
    }
}
