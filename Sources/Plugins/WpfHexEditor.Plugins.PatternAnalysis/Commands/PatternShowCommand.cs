// ==========================================================
// Project: WpfHexEditor.Plugins.PatternAnalysis
// File: Commands/PatternShowCommand.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-30
// Description: HxTerminal command — show the Pattern Analysis panel and summary.
// ==========================================================

using WpfHexEditor.SDK.Contracts.Terminal;

namespace WpfHexEditor.Plugins.PatternAnalysis.Commands;

internal sealed class PatternShowCommand : PluginTerminalCommandBase
{
    private const string PanelUiId = "WpfHexEditor.Plugins.PatternAnalysis.Panel.PatternAnalysisPanel";

    public override string CommandName => "pattern-show";
    public override string Description => "Show the Pattern Analysis panel.";
    public override string Usage       => "pattern-show";

    protected override Task<int> ExecuteCoreAsync(
        string[] args, ITerminalOutput output, ITerminalContext ctx, CancellationToken ct)
    {
        ctx.IDE().UIRegistry.ShowPanel(PanelUiId);
        output.WriteInfo("Pattern Analysis panel opened.");
        return Task.FromResult(0);
    }
}
