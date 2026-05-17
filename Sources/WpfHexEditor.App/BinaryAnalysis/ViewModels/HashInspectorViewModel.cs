//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.Collections.ObjectModel;
using WpfHexEditor.App.BinaryAnalysis.Services;
using WpfHexEditor.Core.ViewModels;
using WpfHexEditor.SDK.Contracts;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.App.BinaryAnalysis.ViewModels;

public sealed class HashInspectorViewModel : ViewModelBase
{
    private IIDEHostContext? _context;
    private CancellationTokenSource? _cts;
    private bool _isBusy;
    private string _statusText = string.Empty;

    public ObservableCollection<HashResult> Results { get; } = [];

    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        private set { _statusText = value; OnPropertyChanged(); }
    }

    public void SetContext(IIDEHostContext context) => _context = context;

    public async Task HashFileAsync()
    {
        if (_context is null || IsBusy || !_context.HexEditor.IsActive) return;
        var stream = new HexEditorStream(_context.HexEditor);
        await ComputeAsync(stream, 0, stream.Length, "Hashing file…");
    }

    public async Task HashSelectionAsync()
    {
        if (_context is null || IsBusy || !_context.HexEditor.IsActive) return;
        long start  = _context.HexEditor.SelectionStart;
        long length = _context.HexEditor.SelectionLength;
        if (length <= 0) { StatusText = "No selection."; return; }
        var stream = new HexEditorStream(_context.HexEditor);
        await ComputeAsync(stream, start, length, $"Hashing {length:N0} bytes…");
    }

    public void Cancel() => _cts?.Cancel();

    public void StatusText_Reset() => StatusText = string.Empty;

    private async Task ComputeAsync(HexEditorStream stream, long offset, long length, string label)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        IsBusy = true;
        StatusText = label;
        Results.Clear();

        var progress = new Progress<long>(done =>
            StatusText = $"{label} ({done * 100 / Math.Max(length, 1)}%)");

        try
        {
            using (stream)
            {
                var results = await HashComputeService.ComputeAsync(stream, offset, length, progress, _cts.Token);
                foreach (var r in results) Results.Add(r);
                StatusText = $"Done ({results[0].Elapsed.TotalMilliseconds:F0} ms)";
            }
        }
        catch (OperationCanceledException) { StatusText = "Cancelled."; }
        finally { IsBusy = false; }
    }
}
