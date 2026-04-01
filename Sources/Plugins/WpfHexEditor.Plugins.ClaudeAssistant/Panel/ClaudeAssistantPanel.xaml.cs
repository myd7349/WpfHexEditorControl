// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Panel/ClaudeAssistantPanel.xaml.cs
// Description: Panel code-behind — tab click handler, auto-save on unload.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfHexEditor.Plugins.ClaudeAssistant.Panel.Tabs;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Panel;

public partial class ClaudeAssistantPanel : UserControl
{
    public ClaudeAssistantPanel()
    {
        InitializeComponent();
    }

    private void OnTabClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ConversationTabViewModel tab }
            && DataContext is ClaudeAssistantPanelViewModel vm)
        {
            vm.ActiveTab = tab;
        }
    }
}
