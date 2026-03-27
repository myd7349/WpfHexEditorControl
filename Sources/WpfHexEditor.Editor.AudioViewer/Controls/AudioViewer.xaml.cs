//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using WpfHexEditor.Editor.Core;

namespace WpfHexEditor.Editor.AudioViewer.Controls;

/// <summary>
/// Stub audio viewer — planned for a future sprint (requires NAudio).
/// Implements <see cref="IDocumentEditor"/> and <see cref="IOpenableDocument"/>.
/// </summary>
public sealed partial class AudioViewer : UserControl, IDocumentEditor, IOpenableDocument
{
    private string _filePath = string.Empty;

    /// <summary>
    /// Creates a new <see cref="AudioViewer"/>.
    /// </summary>
    public AudioViewer()
    {
        InitializeComponent();

        UndoCommand      = new RelayCommand(() => { }, () => false);
        RedoCommand      = new RelayCommand(() => { }, () => false);
        SaveCommand      = new RelayCommand(() => { }, () => false);
        CopyCommand      = new RelayCommand(() => { }, () => false);
        CutCommand       = new RelayCommand(() => { }, () => false);
        PasteCommand     = new RelayCommand(() => { }, () => false);
        DeleteCommand    = new RelayCommand(() => { }, () => false);
        SelectAllCommand = new RelayCommand(() => { }, () => false);
    }

    // -- IDocumentEditor — State ------------------------------------------

    /// <inheritdoc/>
    public bool IsDirty    => false;

    /// <inheritdoc/>
    public bool CanUndo    => false;

    /// <inheritdoc/>
    public bool CanRedo    => false;

    /// <inheritdoc/>
    public bool IsReadOnly { get => true; set { } }

    /// <inheritdoc/>
    public string Title { get; private set; } = "";

    /// <inheritdoc/>
    public bool IsBusy { get; private set; }

    // -- IDocumentEditor — Commands ---------------------------------------

    /// <inheritdoc/>
    public ICommand UndoCommand      { get; }

    /// <inheritdoc/>
    public ICommand RedoCommand      { get; }

    /// <inheritdoc/>
    public ICommand SaveCommand      { get; }

    /// <inheritdoc/>
    public ICommand CopyCommand      { get; }

    /// <inheritdoc/>
    public ICommand CutCommand       { get; }

    /// <inheritdoc/>
    public ICommand PasteCommand     { get; }

    /// <inheritdoc/>
    public ICommand DeleteCommand    { get; }

    /// <inheritdoc/>
    public ICommand SelectAllCommand { get; }

    // -- IDocumentEditor — Events -----------------------------------------

#pragma warning disable CS0067
    /// <inheritdoc/>
    public event EventHandler?         ModifiedChanged;

    /// <inheritdoc/>
    public event EventHandler?         CanUndoChanged;

    /// <inheritdoc/>
    public event EventHandler?         CanRedoChanged;

    /// <inheritdoc/>
    public event EventHandler<string>? TitleChanged;

    /// <inheritdoc/>
    public event EventHandler<string>? StatusMessage;
    /// <inheritdoc/>
    public event EventHandler<string>? OutputMessage;

    /// <inheritdoc/>
    public event EventHandler?         SelectionChanged;

    /// <inheritdoc/>
    public event EventHandler<DocumentOperationEventArgs>?          OperationStarted;

    /// <inheritdoc/>
    public event EventHandler<DocumentOperationEventArgs>?          OperationProgress;

    /// <inheritdoc/>
    public event EventHandler<DocumentOperationCompletedEventArgs>? OperationCompleted;
#pragma warning restore CS0067

    // -- IDocumentEditor — Methods (no-ops for stub) ----------------------

    /// <inheritdoc/>
    public void Undo() { }

    /// <inheritdoc/>
    public void Redo() { }

    /// <inheritdoc/>
    public void Save() { }

    /// <inheritdoc/>
    public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task SaveAsAsync(string filePath, CancellationToken ct = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public void Copy() { }

    /// <inheritdoc/>
    public void Cut() { }

    /// <inheritdoc/>
    public void Paste() { }

    /// <inheritdoc/>
    public void Delete() { }

    /// <inheritdoc/>
    public void SelectAll() { }

    /// <inheritdoc/>
    public void Close() { }

    /// <inheritdoc/>
    public void CancelOperation() { }

    // -- IOpenableDocument ------------------------------------------------

    /// <inheritdoc/>
    public async Task OpenAsync(string filePath, CancellationToken ct = default)
    {
        _filePath = filePath;
        Title = Path.GetFileName(filePath);
        TitleChanged?.Invoke(this, Title);

        await Task.Run(() => LoadAndRender(filePath, ct), ct);

        OperationCompleted?.Invoke(this, new DocumentOperationCompletedEventArgs { Success = true });
    }

    private void LoadAndRender(string filePath, CancellationToken ct)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(fs);

            // Try to parse WAV header
            if (fs.Length >= 44)
            {
                var riff = new string(br.ReadChars(4));
                br.ReadInt32(); // file size
                var wave = new string(br.ReadChars(4));

                if (riff == "RIFF" && wave == "WAVE")
                {
                    // Skip to data chunk
                    while (fs.Position < fs.Length - 8)
                    {
                        ct.ThrowIfCancellationRequested();
                        var chunkId = new string(br.ReadChars(4));
                        var chunkSize = br.ReadInt32();

                        if (chunkId == "fmt ")
                        {
                            var audioFormat = br.ReadInt16();
                            var channels = br.ReadInt16();
                            var sampleRate = br.ReadInt32();
                            var byteRate = br.ReadInt32();
                            var blockAlign = br.ReadInt16();
                            var bitsPerSample = br.ReadInt16();
                            var remaining = chunkSize - 16;
                            if (remaining > 0) fs.Seek(remaining, SeekOrigin.Current);

                            Dispatcher.Invoke(() =>
                            {
                                FormatInfo.Text = $"WAV | {channels}ch | {sampleRate} Hz | {bitsPerSample}-bit";
                            });
                        }
                        else if (chunkId == "data")
                        {
                            // Read samples for waveform (cap at 2MB)
                            var dataLen = Math.Min(chunkSize, 2 * 1024 * 1024);
                            var data = br.ReadBytes(dataLen);
                            Dispatcher.Invoke(() => RenderWaveform(data));
                            Dispatcher.Invoke(() => StatusText.Text = $"{filePath} | {fs.Length:N0} bytes");
                            return;
                        }
                        else
                        {
                            fs.Seek(chunkSize, SeekOrigin.Current);
                        }
                    }
                }
            }

            // Not a WAV — show raw byte waveform
            fs.Seek(0, SeekOrigin.Begin);
            var rawLen = (int)Math.Min(fs.Length, 2 * 1024 * 1024);
            var raw = br.ReadBytes(rawLen);
            Dispatcher.Invoke(() =>
            {
                FormatInfo.Text = $"Raw audio bytes | {Path.GetExtension(filePath).ToUpperInvariant()}";
                RenderWaveform(raw);
                StatusText.Text = $"{filePath} | {fs.Length:N0} bytes";
            });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => StatusText.Text = $"Error: {ex.Message}");
        }
    }

    private void RenderWaveform(byte[] data)
    {
        WaveformCanvas.Children.Clear();
        if (data.Length == 0) return;

        var w = WaveformCanvas.ActualWidth;
        var h = WaveformCanvas.ActualHeight;
        if (w < 1 || h < 1) { w = 800; h = 200; } // fallback before layout

        var midY = h / 2;
        var samplesPerPixel = Math.Max(1, data.Length / (int)w);

        var geo = new System.Windows.Media.StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(new System.Windows.Point(0, midY), false, false);
            for (int x = 0; x < (int)w && x * samplesPerPixel < data.Length; x++)
            {
                // Find min/max in this pixel's sample range
                int start = x * samplesPerPixel;
                int end = Math.Min(start + samplesPerPixel, data.Length);
                byte min = 128, max = 128;
                for (int i = start; i < end; i++)
                {
                    if (data[i] < min) min = data[i];
                    if (data[i] > max) max = data[i];
                }

                double yTop = midY - ((max - 128.0) / 128.0) * midY;
                double yBot = midY - ((min - 128.0) / 128.0) * midY;
                ctx.LineTo(new System.Windows.Point(x, yTop), true, false);
                ctx.LineTo(new System.Windows.Point(x, yBot), true, false);
            }
        }
        geo.Freeze();

        var path = new System.Windows.Shapes.Path
        {
            Data = geo,
            Stroke = System.Windows.Media.Brushes.LimeGreen,
            StrokeThickness = 1,
        };
        WaveformCanvas.Children.Add(path);

        // Center line
        var centerLine = new System.Windows.Shapes.Line
        {
            X1 = 0, X2 = w, Y1 = midY, Y2 = midY,
            Stroke = System.Windows.Media.Brushes.Gray,
            StrokeThickness = 0.5,
            Opacity = 0.5,
        };
        WaveformCanvas.Children.Add(centerLine);
    }
}

// -- Minimal RelayCommand (no external dep) -----------------------------------

internal sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add    => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter)    => execute();
}
