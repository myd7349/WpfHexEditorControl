// ==========================================================
// Project: WpfHexEditor.App
// File: BinaryAnalysis/Panels/ByteBigramHeatmapPanel.cs
// Description:
//     #119 Byte Bigram Heatmap — 256×256 dot-plot showing frequency of
//     consecutive byte pairs (i, j) in the loaded file.
//     Color mapping: log-scale black → yellow → white.
// Architecture: code-behind-only UserControl; no XAML file.
// ==========================================================

using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfHexEditor.App.BinaryAnalysis.Services;
using WpfHexEditor.SDK.Contracts;

namespace WpfHexEditor.App.BinaryAnalysis.Panels;

/// <summary>256×256 byte-pair frequency heatmap (ADR-119).</summary>
public sealed class ByteBigramHeatmapPanel : UserControl
{
    // ── State ─────────────────────────────────────────────────────────────────

    private IIDEHostContext? _context;
    private CancellationTokenSource? _cts;

    // Backing pixel data for the 256×256 bitmap.
    private readonly WriteableBitmap _bitmap = new(
        256, 256, 96, 96, PixelFormats.Bgr32, null);

    // Count table [i, j] = frequency of bigram (i, j).
    private readonly long[,] _counts = new long[256, 256];

    // ── UI elements ───────────────────────────────────────────────────────────

    private readonly Button    _analyzeBtn;
    private readonly Button    _cancelBtn;
    private readonly TextBlock _statusTxt;
    private readonly Image     _image;
    private readonly ToolTip   _tooltip;

    // ── Constructor ───────────────────────────────────────────────────────────

    public ByteBigramHeatmapPanel()
    {
        _analyzeBtn = new Button
        {
            Content = "Analyze",
            Margin  = new Thickness(0, 0, 4, 0),
            Padding = new Thickness(10, 2, 10, 2),
        };
        _cancelBtn = new Button
        {
            Content   = "Cancel",
            Margin    = new Thickness(0, 0, 8, 0),
            Padding   = new Thickness(10, 2, 10, 2),
            IsEnabled = false,
        };
        _statusTxt = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Text              = "Open a file and click Analyze.",
        };

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin      = new Thickness(4, 4, 4, 4),
        };
        toolbar.Children.Add(_analyzeBtn);
        toolbar.Children.Add(_cancelBtn);
        toolbar.Children.Add(_statusTxt);

        _tooltip = new ToolTip { Content = "" };

        _image = new Image
        {
            Source              = _bitmap,
            Width               = 256,
            Height              = 256,
            Stretch             = Stretch.None,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment   = VerticalAlignment.Top,
            ToolTip             = _tooltip,
        };
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.NearestNeighbor);

        _image.MouseMove += OnImageMouseMove;

        var scroll = new ScrollViewer
        {
            Content                       = _image,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            Margin                        = new Thickness(4, 0, 4, 4),
        };

        var root = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);
        root.Children.Add(scroll);
        Content = root;

        _analyzeBtn.Click += async (_, _) => await RunAnalysisAsync();
        _cancelBtn.Click  += (_, _) => _cts?.Cancel();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetContext(IIDEHostContext context) => _context = context;

    public void OnFileOpened()
    {
        _cts?.Cancel();
        ClearBitmap();
        _statusTxt.Text = "File opened. Click Analyze.";
    }

    // ── Analysis ──────────────────────────────────────────────────────────────

    private async Task RunAnalysisAsync()
    {
        if (_context is null) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        _analyzeBtn.IsEnabled = false;
        _cancelBtn.IsEnabled  = true;
        _statusTxt.Text       = "Reading file…";

        try
        {
            var data = await Task.Run(() => ReadAllBytesFromEditor(), ct);
            if (data is null || data.Length < 2)
            {
                _statusTxt.Text = "No data (file too small or not open).";
                return;
            }

            _statusTxt.Text = $"Computing bigrams over {data.Length:N0} bytes…";

            await Task.Run(() => ComputeCounts(data, ct), ct);

            ct.ThrowIfCancellationRequested();

            await Dispatcher.InvokeAsync(() =>
            {
                RenderBitmap();
                _statusTxt.Text = $"Done — {data.Length - 1:N0} bigrams.";
            });
        }
        catch (OperationCanceledException)
        {
            _statusTxt.Text = "Canceled.";
        }
        finally
        {
            _analyzeBtn.IsEnabled = true;
            _cancelBtn.IsEnabled  = false;
        }
    }

    private byte[]? ReadAllBytesFromEditor()
    {
        if (_context?.HexEditor is null) return null;
        try
        {
            using var stream = new HexEditorStream(_context.HexEditor);
            if (stream.Length < 2) return null;
            var buffer = new byte[stream.Length];
            var offset = 0;
            int read;
            while ((read = stream.Read(buffer, offset, buffer.Length - offset)) > 0)
                offset += read;
            return buffer;
        }
        catch
        {
            return null;
        }
    }

    private void ComputeCounts(byte[] data, CancellationToken ct)
    {
        Array.Clear(_counts);

        var n = data.Length - 1;
        for (var i = 0; i < n; i++)
        {
            if ((i & 0xFFFF) == 0) ct.ThrowIfCancellationRequested();
            _counts[data[i], data[i + 1]]++;
        }
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    private void RenderBitmap()
    {
        // Find max count for log-scale normalization.
        long max = 1;
        for (var i = 0; i < 256; i++)
            for (var j = 0; j < 256; j++)
                if (_counts[i, j] > max) max = _counts[i, j];

        var logMax = Math.Log(max + 1);

        // Build a 256×256 BGRA pixel buffer.
        var pixels = new int[256 * 256];
        for (var j = 0; j < 256; j++)
        {
            for (var i = 0; i < 256; i++)
            {
                var c     = _counts[i, j];
                var t     = c == 0 ? 0.0 : Math.Log(c + 1) / logMax; // 0..1
                pixels[j * 256 + i] = MapColor(t);
            }
        }

        _bitmap.Lock();
        try
        {
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, _bitmap.BackBuffer, 256 * 256);
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, 256, 256));
        }
        finally
        {
            _bitmap.Unlock();
        }
    }

    private static int MapColor(double t)
    {
        // black(0,0,0) → yellow(255,255,0) → white(255,255,255)
        int r, g, b;
        if (t < 0.5)
        {
            var s = t * 2.0;          // 0..1
            r = (int)(255 * s);
            g = (int)(255 * s);
            b = 0;
        }
        else
        {
            var s = (t - 0.5) * 2.0; // 0..1
            r = 255;
            g = 255;
            b = (int)(255 * s);
        }
        return (b << 16) | (g << 8) | r; // Bgr32 layout
    }

    private static readonly int[] _zeroes = new int[256 * 256];

    private void ClearBitmap()
    {
        Array.Clear(_counts);
        _bitmap.Lock();
        try
        {
            System.Runtime.InteropServices.Marshal.Copy(_zeroes, 0, _bitmap.BackBuffer, 256 * 256);
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, 256, 256));
        }
        finally
        {
            _bitmap.Unlock();
        }
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    private void OnImageMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(_image);
        var i   = (int)Math.Clamp(pos.X, 0, 255);
        var j   = (int)Math.Clamp(pos.Y, 0, 255);
        var cnt = _counts[i, j];

        _tooltip.Content = $"(0x{i:X2}, 0x{j:X2}) = {cnt:N0} occurrence{(cnt == 1 ? "" : "s")}";
    }
}
