// ==========================================================
// Project: WpfHexEditor.Plugins.SynalysisGrammar
// File: Commands/GrammarClearCommand.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-30
// Description: HxTerminal command — clear the current grammar overlay from the hex editor.
// ==========================================================

using WpfHexEditor.SDK.Contracts.Terminal;

namespace WpfHexEditor.Plugins.SynalysisGrammar.Commands;

internal sealed class GrammarClearCommand : PluginTerminalCommandBase
{
    public override string CommandName => "grammar-clear";
    public override string Description => "Clear the current grammar overlay from the hex editor.";
    public override string Usage       => "grammar-clear";

    protected override Task<int> ExecuteCoreAsync(
        string[] args, ITerminalOutput output, ITerminalContext ctx, CancellationToken ct)
    {
        ctx.IDE.HexEditor.ClearCustomBackgroundBlockByTag("synalysis:");
        output.WriteInfo("Grammar overlay cleared.");
        return Task.FromResult(0);
    }
}
