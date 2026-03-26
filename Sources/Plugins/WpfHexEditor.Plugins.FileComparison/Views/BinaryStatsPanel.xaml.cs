// Project      : WpfHexEditorControl
// File         : Views/BinaryStatsPanel.xaml.cs
// Description  : Code-behind for BinaryStatsPanel — draws nibble-frequency histogram
//                and Shannon entropy polyline charts on WPF Canvas elements.
// Architecture : View-only: all data logic in BinaryStatsPanelViewModel.
//                Charts painted on SizeChanged + ViewModel.PropertyChanged.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfHexEditor.Plugins.FileComparison.ViewModels;

namespace WpfHexEditor.Plugins.FileComparison.Views;

public sealed partial class BinaryStatsPanel : UserControl
{
    private BinaryStatsPanelViewModel? _vm;

    public BinaryStatsPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded             += (_, _) => { PaintHistogram(); PaintEntropy(); };
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _vm = e.NewValue as BinaryStatsPanelViewModel;
        if (_vm is null) return;
        _vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(BinaryStatsPanelViewModel.Analysis)
                                  or nameof(BinaryStatsPanelViewModel.NibbleFreqLeft)
                                  or nameof(BinaryStatsPanelViewModel.EntropyLeft))
            {
                PaintHistogram();
                PaintEntropy();
            }
        };
    }

    // ── Nibble frequency histogram ────────────────────────────────────────────

    private void OnHistogramSizeChanged(object sender, SizeChangedEventArgs e) => PaintHistogram();

    private void PaintHistogram()
    {
        HistogramCanvas.Children.Clear();
        if (_vm is null) return;

        var w = HistogramCanvas.ActualWidth;
        var h = HistogramCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        var nibL = _vm.NibbleFreqLeft;
        var nibR = _vm.NibbleFreqRight;
        if (nibL.Length < 16 || nibR.Length < 16) return;

        int maxVal = 1;
        for (int i = 0; i < 16; i++)
            maxVal = Math.Max(maxVal, Math.Max(nibL[i], nibR[i]));

        var brushL = TryBrush("DF_InsertedLineBrush") ?? Brushes.Green;
        var brushR = TryBrush("DF_DeletedLineBrush")  ?? Brushes.Red;

        double groupW = w / 16.0;
        double barW   = Math.Max(2, groupW / 2 - 2);

        for (int i = 0; i < 16; i++)
        {
            double x = i * groupW;

            double hL = h * nibL[i] / maxVal;
            var rL = new Rectangle { Fill = brushL, Width = barW, Height = Math.Max(1, hL) };
            Canvas.SetLeft(rL, x + 1);
            Canvas.SetTop(rL, h - rL.Height);
            HistogramCanvas.Children.Add(rL);

            double hR = h * nibR[i] / maxVal;
            var rR = new Rectangle { Fill = brushR, Width = barW, Height = Math.Max(1, hR), Opacity = 0.7 };
            Canvas.SetLeft(rR, x + barW + 2);
            Canvas.SetTop(rR, h - rR.Height);
            HistogramCanvas.Children.Add(rR);

            // Nibble label
            var lbl = new TextBlock
            {
                Text       = i.ToString("X"),
                FontSize   = 8,
                FontFamily = new FontFamily("Consolas"),
                Foreground = TryBrush("DockMenuForegroundBrush") ?? Brushes.Gray,
                Opacity    = 0.5
            };
            Canvas.SetLeft(lbl, x + 2);
            Canvas.SetTop(lbl, h - 10);
            HistogramCanvas.Children.Add(lbl);
        }
    }

    // ── Entropy polyline chart ────────────────────────────────────────────────

    private void OnEntropySizeChanged(object sender, SizeChangedEventArgs e) => PaintEntropy();

    private void PaintEntropy()
    {
        EntropyCanvas.Children.Clear();
        if (_vm is null) return;

        var w = EntropyCanvas.ActualWidth;
        var h = EntropyCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        var entL = _vm.EntropyLeft;
        var entR = _vm.EntropyRight;

        const double MaxEntropy = 8.0;

        DrawEntropyLine(entL, w, h, MaxEntropy, TryBrush("DF_InsertedLineBrush") ?? Brushes.Green);
        DrawEntropyLine(entR, w, h, MaxEntropy, TryBrush("DF_DeletedLineBrush")  ?? Brushes.Red, opacity: 0.7);

        // Max-entropy guideline (H=8)
        var guide = new Line
        {
            X1 = 0, Y1 = 0, X2 = w, Y2 = 0,
            Stroke = TryBrush("DockBorderBrush") ?? Brushes.DimGray,
            StrokeThickness = 0.5,
            StrokeDashArray = [4, 4]
        };
        EntropyCanvas.Children.Add(guide);
    }

    private void DrawEntropyLine(double[] entropy, double w, double h,
        double maxEntropy, Brush brush, double opacity = 1.0)
    {
        if (entropy.Length == 0) return;

        var points = new PointCollection(entropy.Length);
        double step = w / Math.Max(1, entropy.Length - 1);
        for (int i = 0; i < entropy.Length; i++)
        {
            double x = i * step;
            double y = h - (h * Math.Clamp(entropy[i] / maxEntropy, 0, 1));
            points.Add(new Point(x, y));
        }

        var poly = new Polyline
        {
            Points          = points,
            Stroke          = brush,
            StrokeThickness = 1.5,
            Opacity         = opacity,
            StrokeLineJoin  = PenLineJoin.Round
        };
        EntropyCanvas.Children.Add(poly);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Brush? TryBrush(string key) => TryFindResource(key) as Brush;
}
