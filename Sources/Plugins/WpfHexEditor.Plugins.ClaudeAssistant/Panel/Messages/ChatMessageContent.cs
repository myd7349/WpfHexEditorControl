// ==========================================================
// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: ChatMessageContent.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Opus 4.6
// Created: 2026-04-01
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     Custom control that renders markdown chat text as rich UIElements.
//     During streaming: plain text (fast). On stream end: full markdown render.
// ==========================================================
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Panel.Messages;

public sealed class ChatMessageContent : ContentControl
{
    private readonly StackPanel _panel = new();
    private DispatcherTimer? _debounceTimer;
    private string _lastRendered = "";
    private bool _isStreaming;

    public ChatMessageContent()
    {
        Content = _panel;
        DataContextChanged += OnDataContextChanged;
    }

    private ChatMessageViewModel? Vm => DataContext as ChatMessageViewModel;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ChatMessageViewModel oldVm)
            oldVm.PropertyChanged -= OnVmPropertyChanged;

        if (e.NewValue is ChatMessageViewModel newVm)
        {
            newVm.PropertyChanged += OnVmPropertyChanged;
            _isStreaming = newVm.IsStreaming;
            RenderContent(newVm.Text, !_isStreaming);
        }
        else
        {
            _panel.Children.Clear();
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatMessageViewModel.Text))
        {
            if (_isStreaming)
                ScheduleDebouncedRender();
            else
                RenderContent(Vm?.Text ?? "", true);
        }
        else if (e.PropertyName == nameof(ChatMessageViewModel.IsStreaming))
        {
            var wasStreaming = _isStreaming;
            _isStreaming = Vm?.IsStreaming ?? false;

            // Stream just ended → full markdown render
            if (wasStreaming && !_isStreaming)
            {
                _debounceTimer?.Stop();
                RenderContent(Vm?.Text ?? "", true);
            }
        }
    }

    private void ScheduleDebouncedRender()
    {
        if (_debounceTimer is null)
        {
            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(400)
            };
            _debounceTimer.Tick += (_, _) =>
            {
                _debounceTimer.Stop();
                RenderContent(Vm?.Text ?? "", false);
            };
        }

        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void RenderContent(string text, bool useMarkdown)
    {
        if (text == _lastRendered) return;
        _lastRendered = text;

        _panel.Children.Clear();

        if (string.IsNullOrEmpty(text)) return;

        if (useMarkdown)
        {
            try
            {
                var elements = ChatMarkdownRenderer.Render(text);
                foreach (var el in elements)
                    _panel.Children.Add(el);
            }
            catch
            {
                // Fallback to plain text if markdown parsing fails
                AddPlainText(text);
            }
        }
        else
        {
            // During streaming: render markdown anyway but with debounce
            try
            {
                var elements = ChatMarkdownRenderer.Render(text);
                foreach (var el in elements)
                    _panel.Children.Add(el);
            }
            catch
            {
                AddPlainText(text);
            }
        }
    }

    private void AddPlainText(string text)
    {
        var tb = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12.5,
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, "CA_MessageForegroundBrush");
        _panel.Children.Add(tb);
    }
}
