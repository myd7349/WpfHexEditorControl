// ==========================================================
// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: ClaudeAskCommand.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-03-31
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     Terminal command /claude-ask — sends a question to the active conversation tab.
// ==========================================================
using WpfHexEditor.Plugins.ClaudeAssistant.Panel;
using WpfHexEditor.SDK.Contracts.Terminal;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Commands.Terminal;

public sealed class ClaudeAskCommand : ITerminalCommandProvider
{
    private readonly Func<ClaudeAssistantPanelViewModel> _getVm;

    public string CommandName => "claude-ask";
    public string Description => "Send a question to Claude AI Assistant";
    public string Usage => "/claude-ask <your question>";
    public string? Source => "Plugin";

    public ClaudeAskCommand(Func<ClaudeAssistantPanelViewModel> getVm) => _getVm = getVm;

    public Task<int> ExecuteAsync(string[] args, ITerminalOutput output, ITerminalContext context, CancellationToken ct = default)
    {
        if (args.Length == 0)
        {
            output.WriteError("Usage: /claude-ask <question>");
            return Task.FromResult(1);
        }

        var question = string.Join(' ', args);
        var vm = _getVm();

        if (vm.ActiveTab is null)
            vm.CreateNewTabCommand.Execute(null);

        if (vm.ActiveTab is not null)
        {
            vm.ActiveTab.InputText = question;
            vm.ActiveTab.SendCommand.Execute(null);
            output.WriteInfo($"[Claude] Question sent: {question[..Math.Min(60, question.Length)]}...");
        }

        return Task.FromResult(0);
    }
}
