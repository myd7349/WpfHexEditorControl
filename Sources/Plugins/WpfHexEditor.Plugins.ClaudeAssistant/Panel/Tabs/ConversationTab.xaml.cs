// ==========================================================
// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: ConversationTab.xaml.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-03-31
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     Conversation tab code-behind. All handlers wrapped in SafeGuard.Run().
// ==========================================================
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexEditor.Plugins.ClaudeAssistant.Panel.Messages;
using WpfHexEditor.Plugins.ClaudeAssistant.Panel.ModelSwitcher;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Panel.Tabs;

public partial class ConversationTab : UserControl
{
    private ConversationTabViewModel? _wiredVm;
    private bool _mentionWired;
    private NotifyCollectionChangedEventHandler? _collectionHandler;
    private PropertyChangedEventHandler? _propertyHandler;

    public ConversationTab()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += (_, _) => SafeGuard.Run(WireCurrentVm);
    }

    private ConversationTabViewModel? Vm => DataContext as ConversationTabViewModel;

    private void OnLoaded(object sender, RoutedEventArgs e)
        => SafeGuard.Run(() =>
        {
            WireCurrentVm();

            // Wire @mention selection (once only)
            if (!_mentionWired)
            {
                _mentionWired = true;
                MentionControl.MentionSelected += token =>
                {
                    SafeGuard.Run(() =>
                    {
                        if (Vm is null) return;
                        var text = Vm.InputText;
                        var atIdx = text.LastIndexOf('@');
                        if (atIdx >= 0)
                            Vm.InputText = text[..atIdx] + token;
                        else
                            Vm.InputText += token;
                        MentionPopup.IsOpen = false;
                        InputBox.CaretIndex = Vm.InputText.Length;
                        InputBox.Focus();
                    });
                };
            }
        });

    private void WireCurrentVm()
    {
        var vm = Vm;

        // Unwire old handlers to prevent leaks
        if (_wiredVm is not null)
        {
            ((INotifyCollectionChanged)_wiredVm.Messages).CollectionChanged -= _collectionHandler;
            _wiredVm.PropertyChanged -= _propertyHandler;
        }

        if (vm is null) { _wiredVm = null; UpdateEmptyState(); return; }
        if (ReferenceEquals(vm, _wiredVm)) { UpdateEmptyState(); return; }

        _wiredVm = vm;

        _collectionHandler = (_, _) =>
            SafeGuard.Run(() =>
            {
                ScrollChatToEnd();
                UpdateEmptyState();
            });

        _propertyHandler = (_, args) =>
        {
            if (args.PropertyName == nameof(vm.InputText))
            {
                UpdateWatermark();
                CheckMentionTrigger();
            }
        };

        ((INotifyCollectionChanged)vm.Messages).CollectionChanged += _collectionHandler;
        vm.PropertyChanged += _propertyHandler;

        UpdateEmptyState();
        UpdateWatermark();
    }

    private void ScrollChatToEnd()
    {
        if (VisualTreeHelper.GetChildrenCount(ChatList) > 0)
        {
            var border = VisualTreeHelper.GetChild(ChatList, 0) as Border;
            var sv = border?.Child as ScrollViewer;
            sv?.ScrollToEnd();
        }
    }

    private void UpdateWatermark()
    {
        InputWatermark.Visibility = string.IsNullOrEmpty(Vm?.InputText)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
        => SafeGuard.Run(() =>
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Alt)
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
        });

    private void OnSendClick(object sender, MouseButtonEventArgs e)
        => SafeGuard.Run(() =>
        {
            if (Vm?.SendCommand is { } cmd && cmd.CanExecute(null))
                cmd.Execute(null);
        });

    private void OnCancelClick(object sender, MouseButtonEventArgs e)
        => SafeGuard.Run(() => Vm?.CancelCommand.Execute(null));

    private void UpdateEmptyState()
    {
        var hasMessages = Vm?.Messages.Count > 0;
        WelcomePanel.Visibility = hasMessages ? Visibility.Collapsed : Visibility.Visible;
        ChatList.Visibility = hasMessages ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnSuggestionClick(object sender, MouseButtonEventArgs e)
        => SafeGuard.Run(() =>
        {
            if (sender is FrameworkElement { Tag: string prompt } && Vm is not null)
            {
                Vm.InputText = prompt;
                if (Vm.SendCommand.CanExecute(null))
                    Vm.SendCommand.Execute(null);
            }
        });

    private void CheckMentionTrigger()
    {
        var text = Vm?.InputText ?? "";
        var atIdx = text.LastIndexOf('@');
        if (atIdx >= 0 && (atIdx == 0 || text[atIdx - 1] == ' '))
        {
            var query = text[(atIdx + 1)..];
            // Don't show if the token is already complete (has a space after it)
            if (!query.Contains(' '))
            {
                MentionControl.Filter(query);
                MentionPopup.IsOpen = true;
                return;
            }
        }
        MentionPopup.IsOpen = false;
    }

    private void OnCopyMessageClick(object sender, RoutedEventArgs e)
        => SafeGuard.Run(() =>
        {
            if (sender is FrameworkElement { DataContext: ChatMessageViewModel msg } && !string.IsNullOrEmpty(msg.Text))
                Clipboard.SetText(msg.Text);
        });

    private void OnModelPillClick(object sender, MouseButtonEventArgs e)
        => SafeGuard.Run(() =>
        {
            System.Diagnostics.Debug.WriteLine("[ClaudeAssistant] OnModelPillClick FIRED");

            if (DataContext is not ConversationTabViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine("[ClaudeAssistant] DataContext is NOT ConversationTabViewModel");
                return;
            }

            // Pre-compute screen point and owner before constructing popup
            Point? screenAnchor = null;
            Window? ownerWin = null;
            if (sender is FrameworkElement { IsLoaded: true } fe)
            {
                ownerWin = Window.GetWindow(fe);
                System.Diagnostics.Debug.WriteLine($"[ClaudeAssistant] ownerWin={ownerWin?.GetType().Name}, IsLoaded={fe.IsLoaded}");
                try { screenAnchor = fe.PointToScreen(new Point(0, fe.RenderSize.Height)); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ClaudeAssistant] PointToScreen FAILED: {ex.Message}"); }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ClaudeAssistant] sender type={sender?.GetType().Name}, not FrameworkElement or not loaded");
            }
            ownerWin ??= Application.Current.MainWindow;

            System.Diagnostics.Debug.WriteLine($"[ClaudeAssistant] Opening ModelSwitcherPopup: owner={ownerWin?.GetType().Name}, anchor={screenAnchor}");

            var popup = new ModelSwitcherPopup(
                vm.Registry,
                vm.SelectedProviderId,
                vm.SelectedModelId,
                vm.ThinkingEnabled,
                ownerWin,
                screenAnchor);

            popup.Closed += (_, _) => SafeGuard.Run(() =>
            {
                if (popup.SelectedProviderId is not null)
                    vm.SelectedProviderId = popup.SelectedProviderId;
                if (popup.SelectedModelId is not null)
                    vm.SelectedModelId = popup.SelectedModelId;
                vm.ThinkingEnabled = popup.ThinkingEnabled;
            });

            popup.Show();
            System.Diagnostics.Debug.WriteLine("[ClaudeAssistant] ModelSwitcherPopup.Show() called");
        });
}
