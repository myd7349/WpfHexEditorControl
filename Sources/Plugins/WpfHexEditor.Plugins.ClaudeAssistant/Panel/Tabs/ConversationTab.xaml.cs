// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Panel/Tabs/ConversationTab.xaml.cs
// Description: Conversation tab code-behind — auto-scroll and input key handling.

using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Panel.Tabs;

public partial class ConversationTab : UserControl
{
    public ConversationTab()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ConversationTabViewModel vm)
        {
            ((INotifyCollectionChanged)vm.Messages).CollectionChanged += (_, _) =>
            {
                ChatScroller.ScrollToEnd();
            };
        }
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (DataContext is ConversationTabViewModel { SendCommand: { } cmd } && cmd.CanExecute(null))
            {
                cmd.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void OnProviderChanged(object sender, SelectionChangedEventArgs e)
    {
        // Model list update handled by VM property change
    }
}
