// ==========================================================
// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: HistoryPanel.xaml.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-03-31
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     History panel code-behind. Entry click, delete, clear all handlers.
// ==========================================================
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfHexEditor.Plugins.ClaudeAssistant.Session;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Panel.History;

public partial class HistoryPanel : UserControl
{
    public HistoryPanel()
    {
        InitializeComponent();
    }

    private HistoryPanelViewModel? Vm => DataContext as HistoryPanelViewModel;

    private void OnEntryClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SessionMetadata meta })
            Vm?.OpenSessionCommand.Execute(meta);
    }

    private void OnDeleteClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SessionMetadata meta })
        {
            Vm?.DeleteSessionCommand.Execute(meta);
            e.Handled = true;
        }
    }

    private void OnClearAllClick(object sender, MouseButtonEventArgs e) => Vm?.ClearAllCommand.Execute(null);
}
