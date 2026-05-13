//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using WpfHexEditor.App.BinaryAnalysis.Services;
using WpfHexEditor.Core.ViewModels;
using WpfHexEditor.SDK.Contracts;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.App.BinaryAnalysis.ViewModels;

public sealed class ByteFrequencyViewModel : ViewModelBase
{
    private IIDEHostContext? _context;
    private CancellationTokenSource? _cts;
    private bool _isBusy;
    private string _statusText = string.Empty;
    private FrequencyResult? _result;

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

    public FrequencyResult? Result
    {
        get => _result;
        private set { _result = value; OnPropertyChanged(); }
    }

    public void SetContext(IIDEHostContext context) => _context = context;

    public async Task AnalyzeAsync()
    {
        if (_context is null || IsBusy || !_context.HexEditor.IsActive) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        IsBusy = true;
        StatusText = "Analyzing…";
        Result = null;

        using var stream = new HexEditorStream(_context.HexEditor);
        long len = stream.Length;
        var progress = new Progress<long>(done =>
            StatusText = $"Analyzing… {done * 100 / Math.Max(len, 1)}%");

        try
        {
            var result = await ByteFrequencyService.AnalyzeAsync(stream, progress, _cts.Token);
            Result = result;
            StatusText = $"Done — Entropy: {result.Entropy:F4} bits/byte";
        }
        catch (OperationCanceledException) { StatusText = "Cancelled."; }
        finally { IsBusy = false; }
    }

    public void Cancel() => _cts?.Cancel();

    public void ExportCsv(string path)
    {
        if (Result is null) return;
        System.IO.File.WriteAllText(path, ByteFrequencyService.ToCsv(Result));
    }
}
