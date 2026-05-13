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

public sealed class StringExtractionViewModel : ViewModelBase
{
    private IIDEHostContext? _context;
    private CancellationTokenSource? _cts;
    private bool _isBusy;
    private int _minLength = 4;
    private string _filter = string.Empty;
    private bool _showAscii = true;
    private bool _showUtf16 = true;

    public ObservableCollection<StringRun> Results { get; } = [];

    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); }
    }

    public int MinLength
    {
        get => _minLength;
        set { _minLength = Math.Clamp(value, 1, 64); OnPropertyChanged(); }
    }

    public string Filter
    {
        get => _filter;
        set { _filter = value; OnPropertyChanged(); }
    }

    public bool ShowAscii
    {
        get => _showAscii;
        set { _showAscii = value; OnPropertyChanged(); }
    }

    public bool ShowUtf16
    {
        get => _showUtf16;
        set { _showUtf16 = value; OnPropertyChanged(); }
    }

    public void SetContext(IIDEHostContext context) => _context = context;

    public async Task RunAsync()
    {
        if (_context is null || IsBusy) return;
        if (!_context.HexEditor.IsActive) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        IsBusy = true;
        Results.Clear();

        try
        {
            using var stream = new HexEditorStream(_context.HexEditor);
            var buffer = new byte[stream.Length];
            stream.Position = 0;
            await stream.ReadExactlyAsync(buffer, _cts.Token);

            var runs = await Task.Run(
                () => StringExtractor.Extract(buffer.AsSpan(), _minLength),
                _cts.Token);

            foreach (var run in runs)
            {
                if (!_showAscii  && run.Encoding == StringEncoding.Ascii)   continue;
                if (!_showUtf16  && run.Encoding == StringEncoding.Utf16Le) continue;
                if (!string.IsNullOrEmpty(_filter) &&
                    !run.Value.Contains(_filter, StringComparison.OrdinalIgnoreCase)) continue;
                Results.Add(run);
            }
        }
        catch (OperationCanceledException) { }
        finally { IsBusy = false; }
    }

    public void Cancel() => _cts?.Cancel();
}
