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
using System.ComponentModel;
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
    // Aspect-ratio threshold: wider than this factor → landscape (bottom-dock) mode.
    private const double LandscapeThreshold = 1.8;

    private PluginMonitoringViewModel? _vm;
    private bool _isLandscape;

    public PluginMonitoringPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        CpuChartCanvas.SizeChanged += (_, _) => RedrawCharts();
        MemChartCanvas.SizeChanged += (_, _) => RedrawCharts();
        SizeChanged += (_, _) => ApplyLayoutMode();
    }

    // ── DataContext wiring ───────────────────────────────────────────────────

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
        {
            ((INotifyCollectionChanged)_vm.CpuHistory).CollectionChanged    -= OnCpuHistoryChanged;
            ((INotifyCollectionChanged)_vm.MemoryHistory).CollectionChanged -= OnMemHistoryChanged;
            ((INotifyCollectionChanged)_vm.EventLog).CollectionChanged      -= OnEventLogChanged;
            _vm.PropertyChanged -= OnVmPropertyChanged;
        }

        _vm = e.NewValue as PluginMonitoringViewModel;

        if (_vm is not null)
        {
            ((INotifyCollectionChanged)_vm.CpuHistory).CollectionChanged    += OnCpuHistoryChanged;
            ((INotifyCollectionChanged)_vm.MemoryHistory).CollectionChanged += OnMemHistoryChanged;
            ((INotifyCollectionChanged)_vm.EventLog).CollectionChanged      += OnEventLogChanged;
            _vm.PropertyChanged += OnVmPropertyChanged;
        }

        RedrawCharts();
    }

    // Rebuild columns when the user toggles the list side via the toolbar button.
    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginMonitoringViewModel.ListSide))
            RebuildContentGrid(_isLandscape);
    }

    // ── Adaptive layout ──────────────────────────────────────────────────────

    private void ApplyLayoutMode()
    {
        var isLandscape = ActualWidth > ActualHeight * LandscapeThreshold;
        if (isLandscape == _isLandscape) return;

        _isLandscape = isLandscape;
        if (_vm is not null) _vm.IsLandscape = isLandscape;
        RebuildContentGrid(isLandscape);
    }

    /// <summary>
    /// Rebuilds ContentGrid row/column definitions and repositions the three
    /// child elements (ChartsArea, ContentSplitter, PluginsDataGrid) and the
    /// inner chart grid (ChartsInnerGrid) according to the current mode.
    /// </summary>
    private void RebuildContentGrid(bool isLandscape)
    {
        ContentGrid.RowDefinitions.Clear();
        ContentGrid.ColumnDefinitions.Clear();

        if (!isLandscape)
        {
            // Portrait: 3 rows — charts (2*) | splitter (4px) | table (*)
            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star), MinHeight = 60 });
            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });
            ContentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 30 });

            Grid.SetRow(ChartsArea,     0); Grid.SetColumn(ChartsArea,     0);
            Grid.SetRow(ContentSplitter, 1); Grid.SetColumn(ContentSplitter, 0);
            Grid.SetRow(PluginsDataGrid, 2); Grid.SetColumn(PluginsDataGrid, 0);

            ContentSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            ContentSplitter.VerticalAlignment   = VerticalAlignment.Stretch;
            ContentSplitter.Height              = 4;
            ContentSplitter.Width               = double.NaN;
            ContentSplitter.ResizeDirection     = GridResizeDirection.Rows;

            RebuildChartsInnerGrid(landscape: false);
        }
        else
        {
            // Landscape: 3 columns — charts (*) | splitter (4px) | table (*)
            // Column order depends on ListSide preference.
            var listSide  = _vm?.ListSide ?? PanelListSide.Right;
            var chartsCol = listSide == PanelListSide.Right ? 0 : 2;
            var tableCol  = listSide == PanelListSide.Right ? 2 : 0;

            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 120 });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 120 });

            Grid.SetRow(ChartsArea,      0); Grid.SetColumn(ChartsArea,      chartsCol);
            Grid.SetRow(ContentSplitter, 0); Grid.SetColumn(ContentSplitter, 1);
            Grid.SetRow(PluginsDataGrid, 0); Grid.SetColumn(PluginsDataGrid, tableCol);

            ContentSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            ContentSplitter.VerticalAlignment   = VerticalAlignment.Stretch;
            ContentSplitter.Height              = double.NaN;
            ContentSplitter.Width               = 4;
            ContentSplitter.ResizeDirection     = GridResizeDirection.Columns;

            RebuildChartsInnerGrid(landscape: true);
        }
    }

    /// <summary>
    /// Rebuilds the inner charts grid between portrait (side-by-side columns)
    /// and landscape (stacked rows) to make best use of the available space.
    /// </summary>
    private void RebuildChartsInnerGrid(bool landscape)
    {
        ChartsInnerGrid.RowDefinitions.Clear();
        ChartsInnerGrid.ColumnDefinitions.Clear();

        if (!landscape)
        {
            // Side-by-side: CPU | splitter | Memory
            ChartsInnerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ChartsInnerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            ChartsInnerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(CpuChartGrid,        0); Grid.SetColumn(CpuChartGrid,        0);
            Grid.SetRow(ChartsInnerSplitter, 0); Grid.SetColumn(ChartsInnerSplitter, 1);
            Grid.SetRow(MemChartGrid,        0); Grid.SetColumn(MemChartGrid,        2);

            ChartsInnerSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            ChartsInnerSplitter.VerticalAlignment   = VerticalAlignment.Stretch;
            ChartsInnerSplitter.Width               = 4;
            ChartsInnerSplitter.Height              = double.NaN;
            ChartsInnerSplitter.ResizeDirection     = GridResizeDirection.Columns;
        }
        else
        {
            // Stacked: CPU / splitter / Memory
            ChartsInnerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            ChartsInnerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });
            ChartsInnerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(CpuChartGrid,        0); Grid.SetColumn(CpuChartGrid,        0);
            Grid.SetRow(ChartsInnerSplitter, 1); Grid.SetColumn(ChartsInnerSplitter, 0);
            Grid.SetRow(MemChartGrid,        2); Grid.SetColumn(MemChartGrid,        0);

            ChartsInnerSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            ChartsInnerSplitter.VerticalAlignment   = VerticalAlignment.Stretch;
            ChartsInnerSplitter.Width               = double.NaN;
            ChartsInnerSplitter.Height              = 4;
            ChartsInnerSplitter.ResizeDirection     = GridResizeDirection.Rows;
        }
    }

    // ── Selection & event log ────────────────────────────────────────────────

    private void OnPluginSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_vm is not null && sender is DataGrid dg)
            _vm.SelectedPlugin = dg.SelectedItem as PluginMonitorRow;
    }

    private void OnEventLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Auto-scroll to the latest log entry.
        if (_vm?.EventLog.Count > 0)
            EventLogListBox.ScrollIntoView(_vm.EventLog[^1]);
    }

    // ── Chart drawing ────────────────────────────────────────────────────────

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
    /// X axis = linear time (left→right), Y axis = value normalised to canvas height
    /// (top = max). maxValue clamps the Y scale; null = auto-scale to observed peak.
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
