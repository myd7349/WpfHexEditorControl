// Project: WpfHexEditor.Plugins.ClaudeAssistant
// File: Panel/Messages/ChatMessageViewModel.cs
// Description: ViewModel for a single chat message bubble in the conversation view.

using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfHexEditor.Plugins.ClaudeAssistant.Panel.Messages;

public sealed partial class ChatMessageViewModel : ObservableObject
{
    [ObservableProperty] private string _role = "user";
    [ObservableProperty] private string _text = "";
    [ObservableProperty] private string _thinkingText = "";
    [ObservableProperty] private bool _isStreaming;
    [ObservableProperty] private bool _isError;

    public bool IsUser => Role == "user";
    public bool IsAssistant => Role == "assistant";
    public bool HasThinking => !string.IsNullOrEmpty(ThinkingText);

    public void AppendText(string delta)
    {
        Text += delta;
        OnPropertyChanged(nameof(Text));
    }

    public void AppendThinking(string delta)
    {
        ThinkingText += delta;
        OnPropertyChanged(nameof(ThinkingText));
        OnPropertyChanged(nameof(HasThinking));
    }
}
