// Project      : WpfHexEditorControl
// File         : ViewModels/DiffHubViewModel.cs
// Description  : ViewModel for DiffHubPanel — tracks both file paths, comparison results,
//                filter state, history, and provides Compare/Swap commands.
// Architecture : INPC, no WPF dependency.  Uses DiffEngine from WpfHexEditor.Core.Diff.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfHexEditor.Core.Diff.Models;
using WpfHexEditor.Core.Diff.Services;
using WpfHexEditor.SDK.Commands;

namespace WpfHexEditor.Plugins.FileComparison.ViewModels;

public sealed class DiffHubViewModel : INotifyPropertyChanged
{
    // ── Backing fields ────────────────────────────────────────────────────────
    private string  _file1Path   = string.Empty;
    private string  _file2Path   = string.Empty;
    private string  _statusText  = "Select two files to compare";
    private bool    _isComparing;
    private string  _filterMode  = "All";   // All | Modified | Added | Removed
    private string  _searchText  = string.Empty;
    private DiffEngineResult? _lastResult;

    // ── Services ──────────────────────────────────────────────────────────────
    private readonly DiffEngine         _engine  = new();
    private CancellationTokenSource?    _cts;

    // ── Public properties ──────────────────────────────────────────────────────

    public string File1Path
    {
        get => _file1Path;
        set => SetField(ref _file1Path, value);
    }

    public string File2Path
    {
        get => _file2Path;
        set => SetField(ref _file2Path, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public bool IsComparing
    {
        get => _isComparing;
        set => SetField(ref _isComparing, value);
    }

    public string FilterMode
    {
        get => _filterMode;
        set { SetField(ref _filterMode, value); ApplyFilter(); }
    }

    public string SearchText
    {
        get => _searchText;
        set { SetField(ref _searchText, value); ApplyFilter(); }
    }

    public DiffEngineResult? LastResult
    {
        get => _lastResult;
        private set { SetField(ref _lastResult, value); NotifyOf(nameof(SimilarityPercent)); }
    }

    public int SimilarityPercent => (int)((_lastResult?.Similarity ?? 0) * 100);

    // ── Flat result rows for the ListView ────────────────────────────────────
    public ObservableCollection<DiffResultRow> AllRows    { get; } = [];
    public ObservableCollection<DiffResultRow> FilteredRows { get; } = [];

    // ── History (from ComparisonSettings) ─────────────────────────────────────
    public ObservableCollection<ComparisonHistoryEntry> History { get; } = [];

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand CompareCommand    { get; }
    public ICommand SwapCommand       { get; }
    public ICommand ClearCommand      { get; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public DiffHubViewModel()
    {
        CompareCommand = new RelayCommand(
            _ => _ = CompareAsync(),
            _ => !string.IsNullOrEmpty(_file1Path) && !string.IsNullOrEmpty(_file2Path) && !_isComparing);

        SwapCommand = new RelayCommand(
            _ => { (File1Path, File2Path) = (_file2Path, _file1Path); },
            _ => !string.IsNullOrEmpty(_file1Path) || !string.IsNullOrEmpty(_file2Path));

        ClearCommand = new RelayCommand(_ => Clear());
    }

    // ── Operations ────────────────────────────────────────────────────────────

    public void SuggestFile1(string path)
    {
        if (!string.IsNullOrEmpty(File1Path)) return;
        File1Path = path;
    }

    public void LoadHistory(IEnumerable<ComparisonHistoryEntry> entries)
    {
        History.Clear();
        foreach (var e in entries)
            History.Add(e);
    }

    public async Task CompareAsync(CancellationToken externalCt = default)
    {
        if (string.IsNullOrEmpty(_file1Path) || string.IsNullOrEmpty(_file2Path)) return;

        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        var ct = _cts.Token;

        IsComparing = true;
        StatusText  = "Comparing…";
        AllRows.Clear();
        FilteredRows.Clear();
        LastResult  = null;

        try
        {
            var result = await _engine.CompareAsync(_file1Path, _file2Path, ct: ct);
            LastResult = result;

            BuildRows(result);
            ApplyFilter();
            BuildStatusText(result);
            RecordHistory(result);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsComparing = false;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void BuildRows(DiffEngineResult result)
    {
        AllRows.Clear();

        if (result.TextResult is { } text)
        {
            int rowIdx = 0;
            foreach (var line in text.Lines)
            {
                AllRows.Add(new DiffResultRow
                {
                    Index       = rowIdx++,
                    Kind        = line.Kind.ToString(),
                    LeftLine    = line.LeftLineNumber?.ToString() ?? "-",
                    RightLine   = line.RightLineNumber?.ToString() ?? "-",
                    Content     = line.Content
                });
            }
        }
        else if (result.BinaryResult is { } bin)
        {
            int rowIdx = 0;
            foreach (var region in bin.Regions)
            {
                AllRows.Add(new DiffResultRow
                {
                    Index    = rowIdx++,
                    Kind     = region.Kind.ToString(),
                    LeftLine = $"0x{region.LeftOffset:X8}",
                    RightLine = $"0x{region.RightOffset:X8}",
                    Content  = $"{region.Length} bytes"
                });
            }
        }
    }

    private void ApplyFilter()
    {
        FilteredRows.Clear();
        foreach (var row in AllRows)
        {
            if (!MatchesFilter(row)) continue;
            FilteredRows.Add(row);
        }
    }

    private bool MatchesFilter(DiffResultRow row)
    {
        if (!string.IsNullOrEmpty(_searchText) &&
            !row.Content.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            return false;

        return _filterMode switch
        {
            "Modified" => row.Kind is "Modified" or "Modified ",
            "Added"    => row.Kind is "InsertedRight" or "InsertedInRight",
            "Removed"  => row.Kind is "DeletedLeft"  or "DeletedInRight",
            _          => true
        };
    }

    private void BuildStatusText(DiffEngineResult result)
    {
        if (result.TextResult is { } text)
            StatusText = $"Text diff — {text.Stats.ModifiedLines} modified, " +
                         $"{text.Stats.InsertedLines} added, {text.Stats.DeletedLines} removed  " +
                         $"({SimilarityPercent}% similar)" +
                         (result.FallbackReason is not null ? $"  [{result.FallbackReason}]" : "");
        else if (result.BinaryResult is { } bin)
            StatusText = $"Binary diff — {bin.Stats.TotalRegions} regions  " +
                         $"({SimilarityPercent}% similar)" +
                         (bin.Truncated ? "  [truncated]" : "");
    }

    private void RecordHistory(DiffEngineResult result)
    {
        var mode = result.TextResult is not null ? "Text" : "Binary";
        // Update existing entry or prepend a new one (cap 20)
        var existing = History.FirstOrDefault(h =>
            string.Equals(h.LeftPath,  _file1Path, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(h.RightPath, _file2Path, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.LastUsed = DateTime.Now;
            existing.Mode     = mode;
        }
        else
        {
            History.Insert(0, new ComparisonHistoryEntry
            {
                LeftPath  = _file1Path,
                RightPath = _file2Path,
                Mode      = mode,
                LastUsed  = DateTime.Now
            });
            if (History.Count > 20) History.RemoveAt(History.Count - 1);
        }
    }

    private void Clear()
    {
        File1Path  = File2Path = string.Empty;
        StatusText = "Select two files to compare";
        AllRows.Clear();
        FilteredRows.Clear();
        LastResult = null;
    }

    private void NotifyOf(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ── INotifyPropertyChanged ────────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>A single row in the diff result list.</summary>
public sealed class DiffResultRow
{
    public int    Index     { get; init; }
    public string Kind      { get; init; } = string.Empty;
    public string LeftLine  { get; init; } = string.Empty;
    public string RightLine { get; init; } = string.Empty;
    public string Content   { get; init; } = string.Empty;
}

/// <summary>One entry in the comparison history list.</summary>
public sealed class ComparisonHistoryEntry
{
    public string   LeftPath  { get; set; } = string.Empty;
    public string   RightPath { get; set; } = string.Empty;
    public string   Mode      { get; set; } = string.Empty;
    public DateTime LastUsed  { get; set; }
}
