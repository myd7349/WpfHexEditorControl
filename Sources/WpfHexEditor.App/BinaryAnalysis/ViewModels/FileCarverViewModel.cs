//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.Collections.ObjectModel;
using System.IO;
using WpfHexEditor.App.BinaryAnalysis.Services;
using WpfHexEditor.Core.ViewModels;
using WpfHexEditor.SDK.Contracts;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.App.BinaryAnalysis.ViewModels;

public sealed class FileCarverViewModel : ViewModelBase
{
    private readonly UserSignatureDbStore _sigStore;
    private IIDEHostContext? _context;
    private CancellationTokenSource? _cts;
    private bool _isBusy;
    private string _statusText = string.Empty;

    public ObservableCollection<CarvedEntry> Results { get; } = [];

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

    public FileCarverViewModel(UserSignatureDbStore sigStore) => _sigStore = sigStore;

    public void SetContext(IIDEHostContext context) => _context = context;

    public async Task ScanAsync()
    {
        if (_context is null || IsBusy || !_context.HexEditor.IsActive) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        IsBusy = true;
        StatusText = "Scanning…";
        Results.Clear();

        using var stream = new HexEditorStream(_context.HexEditor);
        var userSigs = _sigStore.GetAll();
        long len = stream.Length;
        var progress = new Progress<long>(done =>
            StatusText = $"Scanning… {done * 100 / Math.Max(len, 1)}%");

        try
        {
            var entries = await FileCarverService.ScanAsync(stream, userSigs, progress, _cts.Token);
            foreach (var e in entries) Results.Add(e);
            StatusText = $"Found {entries.Count} candidate(s).";
        }
        catch (OperationCanceledException) { StatusText = "Cancelled."; }
        finally { IsBusy = false; }
    }

    public void Cancel() => _cts?.Cancel();

    public async Task ExtractAsync(CarvedEntry entry, IIDEHostContext context)
    {
        if (!context.HexEditor.IsActive) return;

        using var stream = new HexEditorStream(context.HexEditor);
        var tempPath = Path.Combine(Path.GetTempPath(),
            $"carved_{entry.Offset:X8}_{entry.FormatName.Replace(" ", "_")}.bin");

        stream.Position = entry.Offset;
        var bytes = new byte[Math.Min(4 * 1024 * 1024, stream.Length - entry.Offset)];
        int read = await stream.ReadAsync(bytes);
        await File.WriteAllBytesAsync(tempPath, bytes.AsSpan(0, read).ToArray());

        context.DocumentHost.OpenDocument(tempPath);
    }
}
