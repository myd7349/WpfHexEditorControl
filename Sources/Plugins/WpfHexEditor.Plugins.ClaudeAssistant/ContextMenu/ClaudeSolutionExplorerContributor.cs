// ==========================================================
// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: ClaudeSolutionExplorerContributor.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-03-31
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     Context menu contributor for Solution Explorer.
//     Adds "Ask Claude to explain/review/document this file" items.
// ==========================================================
using System.Windows.Input;
using WpfHexEditor.Plugins.ClaudeAssistant.Panel;
using WpfHexEditor.SDK.Contracts;

namespace WpfHexEditor.Plugins.ClaudeAssistant.ContextMenu;

public sealed class ClaudeSolutionExplorerContributor : ISolutionExplorerContextMenuContributor
{
    private readonly Func<ClaudeAssistantPanelViewModel> _getVm;

    public ClaudeSolutionExplorerContributor(Func<ClaudeAssistantPanelViewModel> getVm) => _getVm = getVm;

    public IReadOnlyList<SolutionContextMenuItem> GetContextMenuItems(string nodeKind, string? nodePath)
    {
        if (string.IsNullOrEmpty(nodePath)) return [];

        return
        [
            new SolutionContextMenuItem { IsSeparator = true },
            new SolutionContextMenuItem
            {
                Header = "Ask Claude to explain this file",
                IconGlyph = "\uE734",
                Command = new DelegateCommand(() => SendToTab($"@file:{nodePath} Explain this file in detail."))
            },
            new SolutionContextMenuItem
            {
                Header = "Ask Claude to review this file",
                IconGlyph = "\uE734",
                Command = new DelegateCommand(() => SendToTab($"@file:{nodePath} Code review: identify bugs and improvements."))
            },
            new SolutionContextMenuItem
            {
                Header = "Ask Claude to document this file",
                IconGlyph = "\uE734",
                Command = new DelegateCommand(() => SendToTab($"@file:{nodePath} Add complete XML documentation to all public members."))
            }
        ];
    }

    private void SendToTab(string message)
    {
        var vm = _getVm();
        if (vm.ActiveTab is null)
            vm.CreateNewTabCommand.Execute(null);

        if (vm.ActiveTab is not null)
        {
            vm.ActiveTab.InputText = message;
            vm.ActiveTab.SendCommand.Execute(null);
        }
    }

    private sealed class DelegateCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }
}
