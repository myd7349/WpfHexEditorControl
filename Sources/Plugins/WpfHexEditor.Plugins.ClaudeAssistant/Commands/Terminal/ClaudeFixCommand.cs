// ==========================================================
// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: ClaudeFixCommand.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-03-31
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     Terminal command /claude-fix — sends selection + errors for fix suggestions.
// ==========================================================
using WpfHexEditor.Plugins.ClaudeAssistant.Panel;
using WpfHexEditor.SDK.Contracts;
using WpfHexEditor.SDK.Contracts.Terminal;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Commands.Terminal;

public sealed class ClaudeFixCommand : ITerminalCommandProvider
{
    private readonly Func<ClaudeAssistantPanelViewModel> _getVm;
    private readonly IIDEHostContext _context;

    public string CommandName => "claude-fix";
    public string Description => "Ask Claude to fix errors in the current selection";
    public string Usage => "/claude-fix";
    public string? Source => "Plugin";

    public ClaudeFixCommand(Func<ClaudeAssistantPanelViewModel> getVm, IIDEHostContext context)
    {
        _getVm = getVm;
        _context = context;
    }

    public Task<int> ExecuteAsync(string[] args, ITerminalOutput output, ITerminalContext context, CancellationToken ct = default)
    {
        var vm = _getVm();
        if (vm.ActiveTab is null)
            vm.CreateNewTabCommand.Execute(null);

        if (vm.ActiveTab is not null)
        {
            vm.ActiveTab.InputText = "@selection @errors Fix the errors in this code. Show the corrected version.";
            vm.ActiveTab.SendCommand.Execute(null);
            output.WriteInfo("[Claude] Fix request sent...");
        }

        return Task.FromResult(0);
    }
}
