// Project      : WpfHexEditor.App
// File         : Scripting/ViewModels/ScriptingConsolePanelViewModel.cs
// Description  : VM for the Scripting Console panel.
// Architecture : Delegates execution to IScriptingService; maintains a ring-buffer
//                command history (20 entries) and a flat output log.

using System.Collections.ObjectModel;
using WpfHexEditor.Core.ViewModels;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.App.Scripting.ViewModels;

public sealed class ScriptingConsolePanelViewModel : ViewModelBase
{
    private const int HistoryCapacity = 20;

    private readonly IScriptingService?      _scripting;
    private readonly List<string>            _history = [];
    private int                              _historyIndex = -1;
    private string                           _code       = string.Empty;
    private bool                             _isBusy;
    private CancellationTokenSource?         _cts;

    public ObservableCollection<OutputEntry> Output { get; } = [];

    public string Code
    {
        get => _code;
        set { _code = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); }
    }

    public ScriptingConsolePanelViewModel(IScriptingService? scripting)
    {
        _scripting = scripting;
    }

    public async Task RunAsync()
    {
        var code = _code.Trim();
        if (string.IsNullOrEmpty(code) || _scripting is null) return;

        PushHistory(code);
        IsBusy = true;
        _cts   = new CancellationTokenSource();

        try
        {
            var result = await _scripting.RunAsync(code, _cts.Token).ConfigureAwait(true);

            if (!string.IsNullOrEmpty(result.Output))
                Append(result.Output, isError: false);

            foreach (var diag in result.Diagnostics)
                Append($"{(diag.IsWarning ? "warn" : "error")} ({diag.Line},{diag.Column}): {diag.Message}", isError: !diag.IsWarning);

            if (!result.Success && !result.HasErrors)
                Append("Script failed (no diagnostics returned).", isError: true);

            if (result.Success)
                Append($"Done ({result.Duration.TotalMilliseconds:F0} ms)", isError: false);
        }
        catch (OperationCanceledException)
        {
            Append("Cancelled.", isError: false);
        }
        catch (Exception ex)
        {
            Append($"Exception: {ex.Message}", isError: true);
        }
        finally
        {
            _cts?.Dispose();
            _cts   = null;
            IsBusy = false;
        }
    }

    public void Cancel() => _cts?.Cancel();

    public void ClearOutput() => Output.Clear();

    public string? HistoryUp()
    {
        if (_history.Count == 0) return null;
        _historyIndex = Math.Min(_historyIndex + 1, _history.Count - 1);
        return _history[_history.Count - 1 - _historyIndex];
    }

    public string? HistoryDown()
    {
        if (_historyIndex <= 0) { _historyIndex = -1; return string.Empty; }
        _historyIndex--;
        return _history[_history.Count - 1 - _historyIndex];
    }

    private void PushHistory(string code)
    {
        // Remove duplicate if already at top; ring-cap.
        if (_history.Count > 0 && _history[^1] == code) { _historyIndex = -1; return; }
        _history.Add(code);
        if (_history.Count > HistoryCapacity)
            _history.RemoveAt(0);
        _historyIndex = -1;
    }

    private void Append(string text, bool isError)
        => Output.Add(new OutputEntry(text, isError));
}

public sealed record OutputEntry(string Text, bool IsError);
