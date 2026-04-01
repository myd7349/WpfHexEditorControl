// ==========================================================
// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: ConversationTab.xaml.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-03-31
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     Conversation tab code-behind. Auto-scroll, input key handling, button events.
// ==========================================================
using System.Collections.Specialized;
using System.Windows;
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

    private ConversationTabViewModel? Vm => DataContext as ConversationTabViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Vm is not null)
        {
            ((INotifyCollectionChanged)Vm.Messages).CollectionChanged += (_, _) =>
                ChatScroller.ScrollToEnd();
        }
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (Vm?.SendCommand is { } cmd && cmd.CanExecute(null))
            {
                cmd.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape && Vm?.IsStreaming == true)
        {
            Vm.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnSendClick(object sender, MouseButtonEventArgs e)
    {
        if (Vm?.SendCommand is { } cmd && cmd.CanExecute(null))
            cmd.Execute(null);
    }

    private void OnCancelClick(object sender, MouseButtonEventArgs e) => Vm?.CancelCommand.Execute(null);
}
