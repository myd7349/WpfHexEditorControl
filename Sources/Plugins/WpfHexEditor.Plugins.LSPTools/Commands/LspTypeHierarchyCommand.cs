// ==========================================================
// Project: WpfHexEditor.Plugins.LSPTools
// File: Commands/LspTypeHierarchyCommand.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-30
// Description: HxTerminal command — show the Type Hierarchy panel.
// ==========================================================

using WpfHexEditor.SDK.Contracts.Terminal;

namespace WpfHexEditor.Plugins.LSPTools.Commands;

internal sealed class LspTypeHierarchyCommand : PluginTerminalCommandBase
{
    private const string PanelUiId = "WpfHexEditor.Plugins.LSPTools.Panel.TypeHierarchy";

    public override string CommandName => "lsp-type-hierarchy";
    public override string Description => "Show the LSP Type Hierarchy panel (Ctrl+F12).";
    public override string Usage       => "lsp-type-hierarchy";

    protected override Task<int> ExecuteCoreAsync(
        string[] args, ITerminalOutput output, ITerminalContext ctx, CancellationToken ct)
    {
        ctx.IDE.UIRegistry.ShowPanel(PanelUiId);
        output.WriteInfo("Type Hierarchy panel opened. Trigger with Ctrl+F12 on a symbol.");
        return Task.FromResult(0);
    }
}
