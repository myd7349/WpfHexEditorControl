//////////////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Project: WpfHexEditor.Editor.StructureEditor
// File: ViewModels/TestTabViewModel.cs
// Description: VM for the Test Panel tab — drives file selection, interpreter
//              invocation, and result binding.
//////////////////////////////////////////////////////

using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using WpfHexEditor.Core.ViewModels;
using WpfHexEditor.Editor.StructureEditor.Services;

namespace WpfHexEditor.Editor.StructureEditor.ViewModels;

internal sealed class TestTabViewModel : ViewModelBase
{
    // ── State ─────────────────────────────────────────────────────────────────

    private string _filePath   = "";
    private bool   _isBusy;
    private string _summary    = "";
    private int    _okCount;
    private int    _warnCount;
    private int    _errorCount;
    private int    _skipCount;

    public string FilePath
    {
        get => _filePath;
        set => SetField(ref _filePath, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public string Summary
    {
        get => _summary;
        private set => SetField(ref _summary, value);
    }

    public int OkCount    { get => _okCount;    private set => SetField(ref _okCount, value); }
    public int WarnCount  { get => _warnCount;  private set => SetField(ref _warnCount, value); }
    public int ErrorCount { get => _errorCount; private set => SetField(ref _errorCount, value); }
    public int SkipCount  { get => _skipCount;  private set => SetField(ref _skipCount, value); }

    public ObservableCollection<BlockTestResult> Results { get; } = [];

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand BrowseCommand { get; }
    public ICommand RunCommand    { get; }

    // ── Constructor ───────────────────────────────────────────────────────────

    public TestTabViewModel()
    {
        BrowseCommand = new RelayCommand(BrowseFile);
        RunCommand    = new RelayCommand(
            () => { /* invoked via code-behind */ },
            () => !string.IsNullOrEmpty(FilePath) && !IsBusy);
    }

    // ── File browse ───────────────────────────────────────────────────────────

    private void BrowseFile()
    {
        var dlg = new OpenFileDialog
        {
            Title       = "Select a binary file to test against",
            Filter      = "All files (*.*)|*.*",
            Multiselect = false,
        };
        if (dlg.ShowDialog() == true)
            FilePath = dlg.FileName;
    }

    // ── Run test ──────────────────────────────────────────────────────────────

    internal async Task RunAsync(WpfHexEditor.Core.FormatDetection.FormatDefinition def)
    {
        if (string.IsNullOrEmpty(FilePath) || IsBusy) return;

        IsBusy  = true;
        Summary = "Running…";
        Results.Clear();
        OkCount = WarnCount = ErrorCount = SkipCount = 0;

        try
        {
            var bytes   = await File.ReadAllBytesAsync(FilePath);
            var interp  = new SimpleBlockInterpreter(bytes);
            var results = await Task.Run(() => interp.Run(def));

            foreach (var r in results)
                Results.Add(r);

            OkCount    = results.Count(r => r.Status == "OK");
            WarnCount  = results.Count(r => r.Status == "Warning");
            ErrorCount = results.Count(r => r.Status == "Error");
            SkipCount  = results.Count(r => r.Status == "Skipped");

            Summary = ErrorCount > 0
                ? $"{ErrorCount} error(s), {WarnCount} warning(s), {SkipCount} skipped"
                : WarnCount > 0
                    ? $"Passed with {WarnCount} warning(s)"
                    : $"All {OkCount} field(s) OK — {SkipCount} complex block(s) skipped";
        }
        catch (Exception ex)
        {
            Summary = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
