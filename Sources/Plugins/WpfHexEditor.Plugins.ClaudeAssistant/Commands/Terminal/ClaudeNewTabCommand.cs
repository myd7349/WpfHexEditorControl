// ==========================================================
// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: ClaudeNewTabCommand.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-03-31
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     Terminal command /claude-new-tab — creates a new conversation tab.
// ==========================================================
using WpfHexEditor.Plugins.ClaudeAssistant.Panel;
using WpfHexEditor.SDK.Contracts.Terminal;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Commands.Terminal;

public sealed class ClaudeNewTabCommand : ITerminalCommandProvider
{
    private readonly Func<ClaudeAssistantPanelViewModel> _getVm;

    public string CommandName => "claude-new-tab";
    public string Description => "Open a new Claude AI conversation tab";
    public string Usage => "/claude-new-tab";
    public string? Source => "Plugin";

    public ClaudeNewTabCommand(Func<ClaudeAssistantPanelViewModel> getVm) => _getVm = getVm;

    public Task<int> ExecuteAsync(string[] args, ITerminalOutput output, ITerminalContext context, CancellationToken ct = default)
    {
        _getVm().CreateNewTabCommand.Execute(null);
        output.WriteInfo("[Claude] New conversation tab created.");
        return Task.FromResult(0);
    }
}
