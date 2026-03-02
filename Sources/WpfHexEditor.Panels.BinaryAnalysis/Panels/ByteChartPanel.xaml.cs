//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfHexEditor.Core.Interfaces;

namespace WpfHexEditor.Panels.BinaryAnalysis;

/// <summary>
/// Full-featured byte distribution panel.
/// Implements <see cref="IByteDistributionPanel"/> to connect automatically to
/// <see cref="WpfHexEditor.HexEditor.HexEditor.ByteDistributionPanel"/>.
/// Wraps <see cref="WpfHexEditor.BarChart.Controls.BarChartPanel"/> and adds:
///   - VS-style toolbar (labels/grid/stats toggles, mode selector, refresh, copy)
///   - Entropy sliding-window mode
///   - Footer status bar (entropy, most common byte, null%, ASCII%, total bytes)
///   - Click-to-navigate: fires <see cref="ByteSelected"/> with the clicked byte value
/// </summary>
public partial class ByteChartPanel : UserControl, IByteDistributionPanel
{
    #region Fields

    private byte[]? _lastData;
    private ChartMode _mode = ChartMode.Frequency;

    private enum ChartMode { Frequency, Entropy }

    #endregion

    #region Events

    /// <summary>
    /// Fired when the user clicks on a bar in the chart.
    /// The argument is the byte value (0x00–0xFF) that was clicked.
    /// The host (MainWindow) can use this to search for the first occurrence of that byte.
    /// </summary>
    public event EventHandler<byte>? ByteSelected;

    #endregion

    #region Constructor

    public ByteChartPanel()
    {
        InitializeComponent();
    }

    #endregion

    #region IByteDistributionPanel

    /// <summary>
    /// Called automatically by HexEditor when a file is opened (up to 1 MB sampled).
    /// Updates the chart and footer statistics.
    /// </summary>
    public void UpdateData(byte[] data)
    {
        _lastData = data;
        Chart.UpdateData(data);
        RefreshFooterStats();

        if (_mode == ChartMode.Entropy)
            RedrawEntropyCanvas();
    }

    /// <summary>
    /// Called automatically by HexEditor when the file is closed.
    /// </summary>
    public void Clear()
    {
        _lastData = null;
        Chart.Clear();
        ClearFooterStats();
        EntropyCanvas.Children.Clear();
    }

    #endregion

    #region Public helpers

    /// <summary>
    /// Call this after a theme switch so the BarChartPanel picks up the new accent colours.
    /// </summary>
    public void RefreshTheme()
    {
        ApplyChartColors();

        if (_mode == ChartMode.Entropy && _lastData != null)
            RedrawEntropyCanvas();
    }

    /// <summary>
    /// Returns the file offset of the first occurrence of <paramref name="value"/>
    /// within the sampled data, or −1 if not found.
    /// Used by the host to navigate HexEditor when a bar is clicked.
    /// </summary>
    public long GetFirstOccurrenceOffset(byte value)
    {
        if (_lastData is null) return -1;
        for (int i = 0; i < _lastData.Length; i++)
            if (_lastData[i] == value) return i;
        return -1;
    }

    #endregion

    #region Event handlers — lifecycle

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyChartColors();
    }

    #endregion

    #region Event handlers — toolbar

    private void OnToggleLabels(object sender, RoutedEventArgs e)
    {
        if (Chart is null) return;
        Chart.ShowAxisLabels = ToggleLabels.IsChecked == true;
    }

    private void OnToggleGrid(object sender, RoutedEventArgs e)
    {
        if (Chart is null) return;
        Chart.ShowGridLines = ToggleGrid.IsChecked == true;
    }

    private void OnToggleStats(object sender, RoutedEventArgs e)
    {
        if (Chart is null) return;
        Chart.ShowStatistics = ToggleStats.IsChecked == true;
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        if (_lastData != null) UpdateData(_lastData);
    }

    private void OnCopyChart(object sender, RoutedEventArgs e)
    {
        FrameworkElement source = _mode == ChartMode.Frequency ? Chart : (FrameworkElement)EntropyCanvas;
        if (source.ActualWidth <= 0 || source.ActualHeight <= 0) return;

        var rtb = new RenderTargetBitmap(
            (int)source.ActualWidth, (int)source.ActualHeight,
            96, 96, PixelFormats.Pbgra32);
        rtb.Render(source);
        Clipboard.SetImage(rtb);
    }

    private void OnModeButtonClick(object sender, RoutedEventArgs e)
    {
        if (ModeButton.ContextMenu is { } cm)
        {
            cm.PlacementTarget = ModeButton;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            cm.IsOpen = true;
        }
    }

    private void OnModeFrequency(object sender, RoutedEventArgs e)
    {
        _mode = ChartMode.Frequency;
        Chart.Visibility        = Visibility.Visible;
        EntropyCanvas.Visibility = Visibility.Collapsed;
        ModeLabel.Text           = "Frequency";
        ModeFrequencyItem.IsChecked = true;
        ModeEntropyItem.IsChecked   = false;
    }

    private void OnModeEntropy(object sender, RoutedEventArgs e)
    {
        _mode = ChartMode.Entropy;
        Chart.Visibility        = Visibility.Collapsed;
        EntropyCanvas.Visibility = Visibility.Visible;
        ModeLabel.Text           = "Entropy";
        ModeFrequencyItem.IsChecked = false;
        ModeEntropyItem.IsChecked   = true;

        if (_lastData != null)
            RedrawEntropyCanvas();
    }

    #endregion

    #region Event handlers — chart interaction

    private void OnChartMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Chart.ActualWidth <= 0) return;
        var pos = e.GetPosition(Chart);
        var idx = (int)(pos.X / (Chart.ActualWidth / 256.0));
        idx = Math.Clamp(idx, 0, 255);
        ByteSelected?.Invoke(this, (byte)idx);
    }

    private void OnEntropyCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_mode == ChartMode.Entropy && _lastData != null)
            RedrawEntropyCanvas();
    }

    #endregion

    #region Private helpers

    /// <summary>
    /// Pulls theme-aware colours from Application.Current.Resources and pushes
    /// them into BarChartPanel (which uses plain CLR Color properties, not DPs).
    /// </summary>
    private void ApplyChartColors()
    {
        var res = Application.Current?.Resources;
        if (res is null) return;

        if (res["Panel_ToolbarButtonActiveBrush"] is SolidColorBrush accent)
            Chart.BarColor = accent.Color;

        if (res["PFP_PanelBackgroundBrush"] is SolidColorBrush bg)
            Chart.BackgroundColor = bg.Color;
        else if (res["DockBackgroundBrush"] is SolidColorBrush dockBg)
            Chart.BackgroundColor = dockBg.Color;

        if (res["DockMenuForegroundBrush"] is SolidColorBrush fg)
            Chart.TextColor = fg.Color;

        if (res["DockBorderBrush"] is SolidColorBrush border)
            Chart.GridLineColor = border.Color;

        Chart.InvalidateVisual();
    }

    private void RefreshFooterStats()
    {
        var entropy = Chart.CalculateEntropy();
        EntropyText.Text = $"Entropy: {entropy:F3} bits";

        // Most common byte
        long maxFreq = 0;
        byte mostCommon = 0;
        for (int i = 0; i < 256; i++)
        {
            var freq = Chart.GetFrequency((byte)i);
            if (freq > maxFreq) { maxFreq = freq; mostCommon = (byte)i; }
        }
        MostCommonText.Text = $"Most common: 0x{mostCommon:X2} ({Chart.GetPercentage(mostCommon):F1}%)";

        // Null %
        NullPctText.Text = $"Null: {Chart.GetPercentage(0):F1}%";

        // Printable ASCII %  (0x20–0x7E)
        double asciiPct = 0;
        for (int i = 0x20; i <= 0x7E; i++)
            asciiPct += Chart.GetPercentage((byte)i);
        AsciiPctText.Text = $"ASCII: {asciiPct:F1}%";

        TotalBytesText.Text = $"{Chart.TotalBytes:N0} bytes";

        // Show separators
        SetSeparatorsVisibility(Visibility.Visible);
    }

    private void ClearFooterStats()
    {
        EntropyText.Text    = string.Empty;
        MostCommonText.Text = string.Empty;
        NullPctText.Text    = string.Empty;
        AsciiPctText.Text   = string.Empty;
        TotalBytesText.Text = "No data";
        SetSeparatorsVisibility(Visibility.Collapsed);
    }

    private void SetSeparatorsVisibility(Visibility v)
    {
        SepEntropy.Visibility    = v;
        SepMostCommon.Visibility = v;
        SepNull.Visibility       = v;
        SepAscii.Visibility      = v;
    }

    /// <summary>
    /// Renders entropy bars on <see cref="EntropyCanvas"/> using 512-byte sliding windows.
    /// Each bar is colour-coded: green (low) → orange (mid) → red (high).
    /// </summary>
    private void RedrawEntropyCanvas()
    {
        EntropyCanvas.Children.Clear();
        if (_lastData is null || _lastData.Length == 0) return;

        const int WindowSize = 512;
        var h = EntropyCanvas.ActualHeight;
        if (h <= 0) h = 80;

        var blocks  = Math.Max(1, (_lastData.Length + WindowSize - 1) / WindowSize);
        var barW    = EntropyCanvas.ActualWidth / blocks;

        for (int b = 0; b < blocks; b++)
        {
            var start  = b * WindowSize;
            var len    = Math.Min(WindowSize, _lastData.Length - start);

            // Calculate entropy of this window
            var freq = new long[256];
            for (int i = start; i < start + len; i++) freq[_lastData[i]]++;

            double ent = 0;
            for (int i = 0; i < 256; i++)
            {
                if (freq[i] <= 0) continue;
                var p = freq[i] / (double)len;
                ent -= p * Math.Log(p, 2);
            }

            var barH = (ent / 8.0) * (h - 4);
            var rect = new Rectangle
            {
                Width  = Math.Max(1, barW - 0.5),
                Height = Math.Max(1, barH),
                Fill   = EntropyBarColor(ent)
            };
            Canvas.SetLeft(rect, b * barW);
            Canvas.SetBottom(rect, 2);
            EntropyCanvas.Children.Add(rect);
        }
    }

    /// <summary>
    /// Returns a colour mapped from entropy 0–8: green → orange → red.
    /// </summary>
    private static Brush EntropyBarColor(double entropy)
    {
        var t = entropy / 8.0;
        var r = (byte)(t * 220);
        var g = (byte)((1.0 - t) * 180);
        return new SolidColorBrush(Color.FromRgb(r, g, 60)) { Opacity = 0.9 };
    }

    #endregion
}
