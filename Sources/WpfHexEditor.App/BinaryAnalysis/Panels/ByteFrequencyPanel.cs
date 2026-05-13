//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using WpfHexEditor.App.BinaryAnalysis.Services;
using WpfHexEditor.App.BinaryAnalysis.ViewModels;
using WpfHexEditor.SDK.Contracts;

namespace WpfHexEditor.App.BinaryAnalysis.Panels;

/// <summary>#119 Byte Frequency &amp; Heatmap — code-behind-only panel.</summary>
public sealed class ByteFrequencyPanel : UserControl
{
    private readonly ByteFrequencyViewModel _vm = new();
    private readonly HeatmapGrid _heatmap = new();

    public ByteFrequencyPanel()
    {
        var analyzeBtn = new Button { Content = "Analyze",    Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(10, 2, 10, 2) };
        var cancelBtn  = new Button { Content = "Cancel",     Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(10, 2, 10, 2) };
        var exportBtn  = new Button { Content = "Export CSV", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(10, 2, 10, 2) };
        var statusTxt  = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        statusTxt.SetBinding(TextBlock.TextProperty, new Binding(nameof(_vm.StatusText)) { Source = _vm });

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 4, 4, 4) };
        toolbar.Children.Add(analyzeBtn);
        toolbar.Children.Add(cancelBtn);
        toolbar.Children.Add(exportBtn);
        toolbar.Children.Add(statusTxt);

        var scroll = new ScrollViewer
        {
            Content               = _heatmap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
        };

        var root = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);
        root.Children.Add(scroll);
        Content = root;

        analyzeBtn.Click += async (_, _) =>
        {
            await _vm.AnalyzeAsync();
            _heatmap.SetData(_vm.Result);
        };
        cancelBtn.Click += (_, _) => _vm.Cancel();
        exportBtn.Click += (_, _) =>
        {
            var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "byte_frequency.csv" };
            if (dlg.ShowDialog() == true) _vm.ExportCsv(dlg.FileName);
        };
    }

    public void SetContext(IIDEHostContext context) => _vm.SetContext(context);
    public void OnFileOpened() { _heatmap.SetData(null); }
}

/// <summary>16×16 grid of colored cells representing byte frequency.</summary>
file sealed class HeatmapGrid : FrameworkElement
{
    private FrequencyResult? _data;
    private readonly DrawingVisual _visual = new();

    // Cached resources — allocated once, reused across every Redraw call.
    private static readonly Typeface s_typeface = new("Consolas");
    private static readonly string[] s_labels = Enumerable.Range(0, 256).Select(i => $"{i:X2}").ToArray();

    public HeatmapGrid()
    {
        AddVisualChild(_visual);
        Width  = 16 * CellSize + 2;
        Height = 16 * CellSize + 2;
        ToolTip = new ToolTip();
        MouseMove += OnMouseMove;
    }

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => _visual;

    private const double CellSize = 24;

    public void SetData(FrequencyResult? result)
    {
        _data = result;
        Redraw();
    }

    private void Redraw()
    {
        using var dc = _visual.RenderOpen();
        if (_data is null) return;

        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        int maxCount = _data.Counts.Max();
        for (int i = 0; i < 256; i++)
        {
            int row = i / 16;
            int col = i % 16;
            double x = col * CellSize + 1;
            double y = row * CellSize + 1;
            double t = maxCount > 0 ? (double)_data.Counts[i] / maxCount : 0.0;
            var brush = new SolidColorBrush(InterpolateColor(t));
            brush.Freeze();
            dc.DrawRectangle(brush, null, new Rect(x, y, CellSize - 1, CellSize - 1));

            var ft = new FormattedText(
                s_labels[i],
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                s_typeface,
                7,
                t > 0.6 ? Brushes.Black : Brushes.DarkGray,
                dpi);
            dc.DrawText(ft, new Point(x + 2, y + 2));
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_data is null) return;
        var pos = e.GetPosition(this);
        int col = (int)((pos.X - 1) / CellSize);
        int row = (int)((pos.Y - 1) / CellSize);
        if (col is < 0 or >= 16 || row is < 0 or >= 16) return;
        int idx  = row * 16 + col;
        double pct = _data.TotalBytes > 0 ? (double)_data.Counts[idx] / _data.TotalBytes * 100.0 : 0;
        if (ToolTip is ToolTip tt)
            tt.Content = $"0x{idx:X2} = {_data.Counts[idx]:N0} ({pct:F1}%)";
    }

    private static Color InterpolateColor(double t)
    {
        // white (0,0) → #FF4040 (1.0)
        byte r = (byte)(255);
        byte g = (byte)(255 * (1 - t));
        byte b = (byte)(255 * (1 - t));
        return Color.FromRgb(r, g, b);
    }
}
