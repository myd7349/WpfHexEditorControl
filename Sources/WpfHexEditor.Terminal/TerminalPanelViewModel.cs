//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using WpfHexEditor.Core.Terminal;
using WpfHexEditor.Core.Terminal.BuiltInCommands;
using WpfHexEditor.Core.Terminal.Scripting;
using WpfHexEditor.SDK.Contracts;
using WpfHexEditor.SDK.Contracts.Focus;

namespace WpfHexEditor.Terminal;

/// <summary>
/// ViewModel for the Terminal dockable panel.
/// Manages command input, output lines, history navigation, execution lifecycle,
/// shell switching, and all UX feature toggles (auto-scroll, word wrap, timestamps, pause, find…).
/// </summary>
public sealed class TerminalPanelViewModel : INotifyPropertyChanged, IDisposable, ITerminalContext, ITerminalOutput
{
    // -- Core services ------------------------------------------------------------

    private readonly TerminalCommandRegistry _registry = new();
    private readonly CommandHistory _history = new();
    private CancellationTokenSource? _cts;

    // -- PowerShell process -------------------------------------------------------

    private Process? _psProcess;
    private StreamWriter? _psInput;

    // -- Pause buffer -------------------------------------------------------------

    private readonly Queue<TerminalOutputLine> _pauseBuffer = new();

    // -- Tab completion state -----------------------------------------------------

    private string _completionPrefix = string.Empty;
    private List<string> _completions = [];
    private int _completionIndex = -1;

    // -- ITerminalContext ----------------------------------------------------------

    public IIDEHostContext IDE { get; }
    public IDocument? ActiveDocument => IDE.FocusContext.ActiveDocument;
    public IPanel? ActivePanel => IDE.FocusContext.ActivePanel;

    private string _workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public string WorkingDirectory
    {
        get => _workingDirectory;
        set
        {
            _workingDirectory = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WorkingDirectoryLabel));
        }
    }

    /// <summary>Shortened version of WorkingDirectory for display in the input row prompt.</summary>
    public string WorkingDirectoryLabel
    {
        get
        {
            var dir = _workingDirectory;
            if (dir.Length <= 38) return dir;
            var parts = dir.TrimEnd('\\', '/').Split(['\\', '/']);
            return parts.Length >= 3
                ? parts[0] + "\\…\\" + string.Join('\\', parts[^2..])
                : "…" + dir[^36..];
        }
    }

    // -- Observable state ---------------------------------------------------------

    public ObservableCollection<TerminalOutputLine> OutputLines { get; } = [];

    private string _commandInput = string.Empty;
    public string CommandInput
    {
        get => _commandInput;
        set { _commandInput = value; OnPropertyChanged(); ResetCompletion(); }
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        private set { _isRunning = value; OnPropertyChanged(); }
    }

    // -- Shell mode ---------------------------------------------------------------

    private TerminalMode _currentMode = TerminalMode.HxTerminal;
    public TerminalMode CurrentMode
    {
        get => _currentMode;
        private set
        {
            if (_currentMode == value) return;
            _currentMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentModeLabel));
            OnPropertyChanged(nameof(IsPowerShellMode));
        }
    }

    public string CurrentModeLabel => CurrentMode == TerminalMode.PowerShell ? "PowerShell" : "HxTerminal";
    public bool IsPowerShellMode => CurrentMode == TerminalMode.PowerShell;

    // -- Encoding (PowerShell mode) -----------------------------------------------

    public ObservableCollection<string> AvailableEncodings { get; } = ["UTF-8", "Windows-1252", "ASCII"];

    private string _selectedEncoding = "UTF-8";
    public string SelectedEncoding
    {
        get => _selectedEncoding;
        set { _selectedEncoding = value; OnPropertyChanged(); }
    }

    // -- UX feature toggles -------------------------------------------------------

    private bool _isAutoScrollEnabled = true;
    public bool IsAutoScrollEnabled
    {
        get => _isAutoScrollEnabled;
        set { _isAutoScrollEnabled = value; OnPropertyChanged(); }
    }

    private bool _isWordWrap = true;
    public bool IsWordWrap
    {
        get => _isWordWrap;
        set { _isWordWrap = value; OnPropertyChanged(); }
    }

    private double _outputFontSize = 12;
    public double OutputFontSize
    {
        get => _outputFontSize;
        set { _outputFontSize = Math.Clamp(value, 8, 28); OnPropertyChanged(); }
    }

    private bool _showTimestamps;
    public bool ShowTimestamps
    {
        get => _showTimestamps;
        set { _showTimestamps = value; OnPropertyChanged(); }
    }

    private bool _isOutputPaused;
    public bool IsOutputPaused
    {
        get => _isOutputPaused;
        set { _isOutputPaused = value; OnPropertyChanged(); }
    }

    private bool _isFindVisible;
    public bool IsFindVisible
    {
        get => _isFindVisible;
        set { _isFindVisible = value; OnPropertyChanged(); }
    }

    private bool _copyOnSelect;
    public bool CopyOnSelect
    {
        get => _copyOnSelect;
        set { _copyOnSelect = value; OnPropertyChanged(); }
    }

    private string _findText = string.Empty;
    public string FindText
    {
        get => _findText;
        set { _findText = value; OnPropertyChanged(); }
    }

    private string _findStatusLabel = string.Empty;
    public string FindStatusLabel
    {
        get => _findStatusLabel;
        set { _findStatusLabel = value; OnPropertyChanged(); }
    }

    /// <summary>Shows the number of output lines in the input row status area.</summary>
    public string OutputLineCountLabel => $"{OutputLines.Count} lines";

    // -- Commands -----------------------------------------------------------------

    public ICommand RunCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ClearOutputCommand { get; }
    public ICommand SwitchModeCommand { get; }
    public ICommand ToggleWordWrapCommand { get; }
    public ICommand IncreaseFontCommand { get; }
    public ICommand DecreaseFontCommand { get; }
    public ICommand SaveOutputCommand { get; }
    public ICommand CopyAllCommand { get; }
    public ICommand ToggleTimestampsCommand { get; }
    public ICommand TogglePauseCommand { get; }
    public ICommand ToggleFindCommand { get; }

    // -- Line limit ---------------------------------------------------------------

    private const int MaxOutputLines = 5000;

    // -- Constructor --------------------------------------------------------------

    public TerminalPanelViewModel(IIDEHostContext hostContext)
    {
        IDE = hostContext ?? throw new ArgumentNullException(nameof(hostContext));

        var engine = new HxScriptEngine(_registry);

        RunCommand             = new RelayCommand(_ => _ = ExecuteInputAsync(), _ => !IsRunning);
        CancelCommand          = new RelayCommand(_ => CancelCurrentOperation(), _ => IsRunning || _psProcess is not null);
        ClearOutputCommand     = new RelayCommand(_ => ClearOutput());
        SwitchModeCommand      = new RelayCommand(p => _ = SwitchModeAsync(ParseMode(p)));
        ToggleWordWrapCommand  = new RelayCommand(_ => IsWordWrap = !IsWordWrap);
        IncreaseFontCommand    = new RelayCommand(_ => OutputFontSize++);
        DecreaseFontCommand    = new RelayCommand(_ => OutputFontSize--);
        SaveOutputCommand      = new RelayCommand(_ => SaveOutput());
        CopyAllCommand         = new RelayCommand(_ => CopyAllToClipboard());
        ToggleTimestampsCommand = new RelayCommand(_ => ShowTimestamps = !ShowTimestamps);
        TogglePauseCommand     = new RelayCommand(_ => TogglePause());
        ToggleFindCommand      = new RelayCommand(_ => IsFindVisible = !IsFindVisible);

        RegisterBuiltIns(engine);
        _ = _history.LoadAsync();
    }

    // -- Public API ---------------------------------------------------------------

    public void RegisterCommand(ITerminalCommandProvider command) => _registry.Register(command);
    public void UnregisterCommand(string commandName) => _registry.Unregister(commandName);

    // -- Keyboard navigation -------------------------------------------------------

    public void NavigateHistoryUp()
    {
        var entry = _history.NavigatePrevious();
        if (entry is not null) CommandInput = entry;
    }

    public void NavigateHistoryDown()
    {
        var entry = _history.NavigateNext();
        if (entry is not null) CommandInput = entry;
    }

    /// <summary>
    /// Cycles through tab-completion candidates matching the current CommandInput prefix.
    /// Resets the cycle when CommandInput changes (handled by ResetCompletion).
    /// </summary>
    public void CycleCompletion()
    {
        if (CurrentMode != TerminalMode.HxTerminal) return;

        var input = CommandInput;
        if (input != _completionPrefix || _completions.Count == 0)
        {
            _completionPrefix = input;
            _completions = [.. _registry.GetCompletions(input)];
            _completionIndex = -1;
        }

        if (_completions.Count == 0) return;

        _completionIndex = (_completionIndex + 1) % _completions.Count;
        // Temporarily bypass ResetCompletion by setting the backing field directly.
        _commandInput = _completions[_completionIndex];
        OnPropertyChanged(nameof(CommandInput));
    }

    private void ResetCompletion()
    {
        _completions = [];
        _completionIndex = -1;
        _completionPrefix = string.Empty;
    }

    // -- ITerminalOutput -----------------------------------------------------------

    public void Write(string text) => AppendLine(text, TerminalOutputKind.Standard);
    public void WriteLine(string text = "") => AppendLine(text, TerminalOutputKind.Standard);
    public void WriteError(string text)   => AppendLine(text, TerminalOutputKind.Error);
    public void WriteWarning(string text) => AppendLine(text, TerminalOutputKind.Warning);
    public void WriteInfo(string text)    => AppendLine(text, TerminalOutputKind.Info);
    public void Clear() => Application.Current?.Dispatcher.InvokeAsync(ClearOutput);

    // -- Shell-mode switching ------------------------------------------------------

    private static TerminalMode ParseMode(object? parameter) =>
        parameter is TerminalMode m ? m :
        parameter is string s && Enum.TryParse<TerminalMode>(s, out var parsed) ? parsed :
        TerminalMode.HxTerminal;

    private async Task SwitchModeAsync(TerminalMode newMode)
    {
        if (CurrentMode == newMode) return;

        if (newMode == TerminalMode.PowerShell)
            await StartPowerShellAsync().ConfigureAwait(false);
        else
            await StopPowerShellAsync().ConfigureAwait(false);
    }

    private async Task StartPowerShellAsync()
    {
        _cts?.Cancel();

        var exe = ResolveShellExecutable("pwsh.exe", "powershell.exe");
        if (exe is null)
        {
            WriteError("PowerShell not found (pwsh.exe / powershell.exe). Staying in HxTerminal mode.");
            return;
        }

        var encoding = SelectedEncoding switch
        {
            "Windows-1252" => Encoding.GetEncoding(1252),
            "ASCII"        => Encoding.ASCII,
            _              => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        var psi = new ProcessStartInfo(exe)
        {
            Arguments               = "-NoLogo -NoExit",
            UseShellExecute         = false,
            CreateNoWindow          = true,
            RedirectStandardInput   = true,
            RedirectStandardOutput  = true,
            RedirectStandardError   = true,
            StandardOutputEncoding  = encoding,
            StandardErrorEncoding   = encoding,
        };

        _psProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _psProcess.Exited += OnPsProcessExited;

        try
        {
            _psProcess.Start();
        }
        catch (Exception ex)
        {
            WriteError($"Failed to start PowerShell: {ex.Message}");
            _psProcess.Dispose();
            _psProcess = null;
            return;
        }

        _psInput = _psProcess.StandardInput;

        _ = PipeReaderAsync(_psProcess.StandardOutput, TerminalOutputKind.Standard);
        _ = PipeReaderAsync(_psProcess.StandardError,  TerminalOutputKind.Error);

        CurrentMode = TerminalMode.PowerShell;
        AppendLine($"[PowerShell started: {exe}]", TerminalOutputKind.Info);
        await Task.CompletedTask;
    }

    private async Task StopPowerShellAsync()
    {
        if (_psProcess is null) { CurrentMode = TerminalMode.HxTerminal; return; }

        try
        {
            await _psInput!.WriteLineAsync("exit").ConfigureAwait(false);
            await _psInput.FlushAsync().ConfigureAwait(false);

            var exited = await Task.Run(() => _psProcess.WaitForExit(300)).ConfigureAwait(false);
            if (!exited) _psProcess.Kill(entireProcessTree: true);
        }
        catch { /* process may already be gone */ }
        finally
        {
            CleanupPsProcess();
        }

        CurrentMode = TerminalMode.HxTerminal;
        AppendLine("[Switched to HxTerminal]", TerminalOutputKind.Info);
    }

    private void OnPsProcessExited(object? sender, EventArgs e)
    {
        CleanupPsProcess();
        AppendLine("[PowerShell process exited]", TerminalOutputKind.Warning);
        Application.Current?.Dispatcher.InvokeAsync(() => CurrentMode = TerminalMode.HxTerminal);
    }

    private void CleanupPsProcess()
    {
        if (_psProcess is not null)
        {
            _psProcess.Exited -= OnPsProcessExited;
            _psProcess.Dispose();
            _psProcess = null;
        }
        _psInput?.Dispose();
        _psInput = null;
    }

    private async Task PipeReaderAsync(StreamReader reader, TerminalOutputKind kind)
    {
        try
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
                AppendLine(line, kind);
        }
        catch { /* stream closed when process exits */ }
    }

    // -- UX feature helpers --------------------------------------------------------

    private void TogglePause()
    {
        IsOutputPaused = !IsOutputPaused;

        if (!IsOutputPaused)
        {
            // Flush buffered lines accumulated while paused.
            while (_pauseBuffer.TryDequeue(out var buffered))
                AddLineAndTrim(buffered);
        }
    }

    private void ClearOutput()
    {
        OutputLines.Clear();
        OnPropertyChanged(nameof(OutputLineCountLabel));
    }

    private void SaveOutput()
    {
        var dlg = new SaveFileDialog
        {
            Filter      = "Plain Text|*.txt"
                        + "|HTML|*.html"
                        + "|RTF / Word|*.rtf"
                        + "|ANSI Text|*.ansi"
                        + "|Markdown|*.md"
                        + "|Excel / LibreOffice Calc|*.xml"
                        + "|All Files|*.*",
            FileName    = "terminal-output",
            DefaultExt  = "txt",
            FilterIndex = 1
        };

        if (dlg.ShowDialog() != true) return;

        var lines = (IReadOnlyList<TerminalOutputLine>)OutputLines;
        var ts    = ShowTimestamps;

        var content = dlg.FilterIndex switch
        {
            2 => TerminalExportService.ToHtml         (lines, ts),
            3 => TerminalExportService.ToRtf          (lines, ts),
            4 => TerminalExportService.ToAnsi         (lines, ts),
            5 => TerminalExportService.ToMarkdown     (lines, ts),
            6 => TerminalExportService.ToSpreadsheetMl(lines, ts),
            _ => TerminalExportService.ToPlainText    (lines, ts)
        };

        File.WriteAllText(dlg.FileName, content, System.Text.Encoding.UTF8);
    }

    private void CopyAllToClipboard()
    {
        var text = string.Join(
            Environment.NewLine,
            OutputLines.Select(l => ShowTimestamps ? $"[{l.Timestamp:HH:mm:ss}] {l.Text}" : l.Text));

        if (!string.IsNullOrEmpty(text))
            Clipboard.SetText(text);
    }

    private void CancelCurrentOperation()
    {
        if (CurrentMode == TerminalMode.PowerShell && _psProcess is { HasExited: false })
            try { _psProcess.Kill(entireProcessTree: false); } catch { /* ignore */ }
        else
            _cts?.Cancel();
    }

    // -- Private helpers -----------------------------------------------------------

    private async Task ExecuteInputAsync()
    {
        var input = CommandInput.Trim();
        if (string.IsNullOrEmpty(input)) return;

        _history.Push(input);
        CommandInput = string.Empty;
        AppendLine($"> {input}", TerminalOutputKind.Info);

        // PowerShell mode: forward to PS stdin.
        if (CurrentMode == TerminalMode.PowerShell && _psInput is not null)
        {
            try
            {
                await _psInput.WriteLineAsync(input).ConfigureAwait(false);
                await _psInput.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex) { WriteError($"PowerShell I/O error: {ex.Message}"); }
            return;
        }

        // HxTerminal mode: built-in registry.
        var parts = TokenizeInput(input);
        if (parts.Length == 0) return;

        _cts = new CancellationTokenSource();
        IsRunning = true;

        try
        {
            var cmd = _registry.FindCommand(parts[0]);
            if (cmd is null)
            {
                WriteError($"Unknown command: {parts[0]}. Type 'help' for a list of commands.");
                return;
            }

            await cmd.ExecuteAsync(parts[1..], this, this, _cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            AppendLine("[Cancelled]", TerminalOutputKind.Warning);
        }
        catch (Exception ex)
        {
            WriteError($"Error: {ex.Message}");
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void AppendLine(string text, TerminalOutputKind kind)
    {
        var line = new TerminalOutputLine(text, kind);

        if (IsOutputPaused)
        {
            _pauseBuffer.Enqueue(line);
            return;
        }

        if (Application.Current?.Dispatcher.CheckAccess() == true)
            AddLineAndTrim(line);
        else
            Application.Current?.Dispatcher.InvokeAsync(() => AddLineAndTrim(line));
    }

    private void AddLineAndTrim(TerminalOutputLine line)
    {
        while (OutputLines.Count >= MaxOutputLines)
            OutputLines.RemoveAt(0);

        OutputLines.Add(line);
        OnPropertyChanged(nameof(OutputLineCountLabel));
    }

    private static string[] TokenizeInput(string input)
    {
        var args = new List<string>();
        bool inQuote = false;
        var current = new System.Text.StringBuilder();
        foreach (var ch in input)
        {
            if (ch == '"') { inQuote = !inQuote; continue; }
            if (ch == ' ' && !inQuote) { if (current.Length > 0) { args.Add(current.ToString()); current.Clear(); } continue; }
            current.Append(ch);
        }
        if (current.Length > 0) args.Add(current.ToString());
        return [.. args];
    }

    private static string? ResolveShellExecutable(params string[] candidates)
    {
        foreach (var name in candidates)
        {
            var located = SearchInPath(name);
            if (located is not null) return located;
        }
        return null;
    }

    private static string? SearchInPath(string fileName)
    {
        if (File.Exists(fileName)) return Path.GetFullPath(fileName);

        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        foreach (var dir in paths)
        {
            var full = Path.Combine(dir.Trim(), fileName);
            if (File.Exists(full)) return full;
        }
        return null;
    }

    private void RegisterBuiltIns(HxScriptEngine engine)
    {
        _registry.Register(new HelpCommand(_registry));
        _registry.Register(new ClearCommand());
        _registry.Register(new EchoCommand());
        _registry.Register(new VersionCommand());
        _registry.Register(new ExitCommand());
        _registry.Register(new PluginListCommand());
        _registry.Register(new StatusCommand());
        _registry.Register(new HistoryCommand(_history));
        _registry.Register(new SendOutputCommand());
        _registry.Register(new SendErrorCommand());
        _registry.Register(new RunScriptCommand(engine));
        _registry.Register(new OpenFileCommand());
        _registry.Register(new ListOpenFilesCommand());
        _registry.Register(new ReadHexCommand());
        _registry.Register(new SearchCommand());
        _registry.Register(new SaveFileCommand());
        _registry.Register(new SaveAsCommand());
        _registry.Register(new CloseFileCommand());
        _registry.Register(new OpenFolderCommand());
        _registry.Register(new OpenProjectCommand());
        _registry.Register(new CloseProjectCommand());
        _registry.Register(new OpenSolutionCommand());
        _registry.Register(new CloseSolutionCommand());
        _registry.Register(new ReloadSolutionCommand());
        _registry.Register(new ListFilesCommand());
        _registry.Register(new SelectFileCommand());
        _registry.Register(new CopyFileCommand());
        _registry.Register(new DeleteFileCommand());
        _registry.Register(new OpenPanelCommand());
        _registry.Register(new ClosePanelCommand());
        _registry.Register(new TogglePanelCommand());
        _registry.Register(new FocusPanelCommand());
        _registry.Register(new ClearPanelCommand());
        _registry.Register(new AppendPanelCommand());
        _registry.Register(new ShowLogsCommand());
        _registry.Register(new ShowErrorsCommand());
        _registry.Register(new WriteHexCommand());
        _registry.Register(new RunPluginCommand());
    }

    // -- INotifyPropertyChanged ----------------------------------------------------

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // -- IDisposable ---------------------------------------------------------------

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        try { _psProcess?.Kill(entireProcessTree: true); } catch { /* ignore */ }
        CleanupPsProcess();
        _ = _history.SaveAsync();
    }

    // -- RelayCommand --------------------------------------------------------------

    private sealed class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => execute(parameter);
    }
}
