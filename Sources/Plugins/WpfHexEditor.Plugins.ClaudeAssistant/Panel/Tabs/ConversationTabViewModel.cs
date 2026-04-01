// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Panel/Tabs/ConversationTabViewModel.cs
// Description: ViewModel for a single conversation tab — owns the session, handles streaming.

using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfHexEditor.Plugins.ClaudeAssistant.Api;
using WpfHexEditor.Plugins.ClaudeAssistant.Panel.Messages;
using WpfHexEditor.Plugins.ClaudeAssistant.Session;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Panel.Tabs;

public sealed partial class ConversationTabViewModel : ObservableObject
{
    private readonly ModelRegistry _registry;
    private CancellationTokenSource? _streamCts;

    public ConversationSession Session { get; }
    public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];

    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private bool _isStreaming;
    [ObservableProperty] private string _selectedProviderId;
    [ObservableProperty] private string _selectedModelId;
    [ObservableProperty] private bool _thinkingEnabled;

    public string Title => Session.Title;

    public ConversationTabViewModel(ConversationSession session, ModelRegistry registry)
    {
        Session = session;
        _registry = registry;
        _selectedProviderId = session.ProviderId;
        _selectedModelId = session.ModelId;
        _thinkingEnabled = session.ThinkingEnabled;
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // Add user message
        var userMsg = ChatMessage.User(text);
        Session.AddMessage(userMsg);
        Messages.Add(new ChatMessageViewModel { Role = "user", Text = text });
        InputText = "";

        // Auto-title from first message
        if (Session.Messages.Count == 1)
            Session.Title = text.Length > 40 ? text[..40] + "..." : text;

        // Update session provider/model
        Session.ProviderId = SelectedProviderId;
        Session.ModelId = SelectedModelId;
        Session.ThinkingEnabled = ThinkingEnabled;

        // Get provider
        var provider = _registry.GetProvider(SelectedProviderId);
        if (provider is null)
        {
            Messages.Add(new ChatMessageViewModel
            {
                Role = "assistant", Text = $"Provider '{SelectedProviderId}' not found.", IsError = true
            });
            return;
        }

        // Start streaming
        var assistantVm = new ChatMessageViewModel { Role = "assistant", IsStreaming = true };
        Messages.Add(assistantVm);
        IsStreaming = true;
        _streamCts = new CancellationTokenSource();

        var thinking = provider.SupportsThinking && ThinkingEnabled
            ? new ThinkingConfig(true, Session.ThinkingBudgetTokens)
            : null;

        try
        {
            await foreach (var chunk in provider.StreamAsync(
                Session.Messages, SelectedModelId, tools: null, thinking, _streamCts.Token))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    switch (chunk.Kind)
                    {
                        case ChunkKind.TextDelta:
                            assistantVm.AppendText(chunk.Text ?? "");
                            break;
                        case ChunkKind.ThinkingDelta:
                            assistantVm.AppendThinking(chunk.ThinkingText ?? "");
                            break;
                        case ChunkKind.Error:
                            assistantVm.Text = $"Error: {chunk.ErrorMessage}";
                            assistantVm.IsError = true;
                            break;
                        case ChunkKind.Done:
                            break;
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            assistantVm.AppendText(" [cancelled]");
        }
        catch (Exception ex)
        {
            assistantVm.Text = $"Error: {ex.Message}";
            assistantVm.IsError = true;
        }
        finally
        {
            assistantVm.IsStreaming = false;
            IsStreaming = false;
            _streamCts?.Dispose();
            _streamCts = null;

            // Save assistant message to session
            Session.AddMessage(ChatMessage.Assistant(assistantVm.Text));

            OnPropertyChanged(nameof(Title));
            SendCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSend() => !IsStreaming && !string.IsNullOrWhiteSpace(InputText);

    [RelayCommand(CanExecute = nameof(IsStreaming))]
    private void Cancel()
    {
        _streamCts?.Cancel();
    }

    partial void OnInputTextChanged(string value) => SendCommand.NotifyCanExecuteChanged();
    partial void OnIsStreamingChanged(bool value)
    {
        SendCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }
}
