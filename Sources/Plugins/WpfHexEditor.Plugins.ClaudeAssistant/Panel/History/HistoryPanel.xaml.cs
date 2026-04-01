// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Panel/History/HistoryPanel.xaml.cs
// Description: History panel code-behind — click opens session in a new tab.

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

    private void OnEntryClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SessionMetadata meta }
            && DataContext is HistoryPanelViewModel vm)
        {
            vm.OpenSessionCommand.Execute(meta);
        }
    }
}
