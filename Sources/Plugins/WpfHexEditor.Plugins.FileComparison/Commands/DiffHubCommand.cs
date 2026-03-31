// ==========================================================
// Project: WpfHexEditor.Plugins.FileComparison
// File: Commands/DiffHubCommand.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-30
// Description: HxTerminal command — open or focus the Diff Hub document tab.
// ==========================================================

using WpfHexEditor.SDK.Contracts.Terminal;

namespace WpfHexEditor.Plugins.FileComparison.Commands;

internal sealed class DiffHubCommand(Action showDiffHub) : PluginTerminalCommandBase
{
    public override string CommandName => "diff-hub";
    public override string Description => "Open or focus the Diff Hub document tab.";
    public override string Usage       => "diff-hub";

    protected override Task<int> ExecuteCoreAsync(
        string[] args, ITerminalOutput output, ITerminalContext ctx, CancellationToken ct)
    {
        showDiffHub();
        output.WriteInfo("Diff Hub opened.");
        return Task.FromResult(0);
    }
}
