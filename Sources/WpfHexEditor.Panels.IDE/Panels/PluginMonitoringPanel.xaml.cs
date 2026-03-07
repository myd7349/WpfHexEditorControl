// ==========================================================
// Project: WpfHexEditor.Panels.IDE
// File: PluginMonitoringPanel.xaml.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-06
// Description:
//     Code-behind for the Plugin Monitoring docking panel.
//     Redraws the CPU% and Memory MB Canvas+Polyline charts
//     whenever the ViewModel's chart history collections change.
//
// Architecture Notes:
//     Pure WPF chart rendering: normalise values to canvas pixels,
//     update Polyline.Points directly. No external charting library.
// ==========================================================

using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WpfHexEditor.Panels.IDE.Panels.ViewModels;

namespace WpfHexEditor.Panels.IDE.Panels;

/// <summary>
/// IValueConverter used only in XAML to satisfy the converter key requirement.
/// Actual chart drawing is done in code-behind via CollectionChanged events.
/// </summary>
public sealed class ChartPointsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue; // not used — charts drawn in code-behind

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

public partial class PluginMonitoringPanel : UserControl
{
    public PluginMonitoringPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        CpuChartCanvas.SizeChanged += (_, _) => RedrawCharts();
        MemChartCanvas.SizeChanged += (_, _) => RedrawCharts();
    }

    private PluginMonitoringViewModel? _vm;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Unsubscribe from old VM
        if (_vm is not null)
        {
            ((INotifyCollectionChanged)_vm.CpuHistory).CollectionChanged    -= OnCpuHistoryChanged;
            ((INotifyCollectionChanged)_vm.MemoryHistory).CollectionChanged -= OnMemHistoryChanged;
        }

        _vm = e.NewValue as PluginMonitoringViewModel;

        if (_vm is not null)
        {
            ((INotifyCollectionChanged)_vm.CpuHistory).CollectionChanged    += OnCpuHistoryChanged;
            ((INotifyCollectionChanged)_vm.MemoryHistory).CollectionChanged += OnMemHistoryChanged;
        }

        RedrawCharts();
    }

    private void OnCpuHistoryChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RedrawChart(CpuChartCanvas, CpuPolyline, _vm?.CpuHistory, maxValue: 100.0);

    private void OnMemHistoryChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RedrawChart(MemChartCanvas, MemPolyline, _vm?.MemoryHistory, maxValue: null);

    private void OnIntervalSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_vm is null) return;
        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem item
            && int.TryParse(item.Tag?.ToString(), out var seconds))
        {
            _vm.SetIntervalCommand.Execute(seconds);
        }
    }

    private void RedrawCharts()
    {
        RedrawChart(CpuChartCanvas, CpuPolyline, _vm?.CpuHistory, maxValue: 100.0);
        RedrawChart(MemChartCanvas, MemPolyline, _vm?.MemoryHistory, maxValue: null);
    }

    /// <summary>
    /// Maps a ChartPoint collection onto a Polyline inside a Canvas.
    /// The X axis is time (linear, left→right), the Y axis is the value
    /// normalised to [0, maxValue] (or the observed max if maxValue is null).
    /// </summary>
    private static void RedrawChart(
        Canvas canvas,
        System.Windows.Shapes.Polyline polyline,
        IReadOnlyList<ChartPoint>? points,
        double? maxValue)
    {
        polyline.Points.Clear();

        if (points is null || points.Count < 2) return;

        var w = canvas.ActualWidth;
        var h = canvas.ActualHeight;

        if (w <= 0 || h <= 0) return;

        var peak = maxValue ?? points.Max(p => p.Value);
        if (peak <= 0) peak = 1; // avoid divide-by-zero when all values are 0

        var count = points.Count;
        for (int i = 0; i < count; i++)
        {
            var px = w * i / (count - 1);
            var py = h - (h * points[i].Value / peak); // Y is inverted (top = max)
            polyline.Points.Add(new Point(px, py));
        }
    }
}
