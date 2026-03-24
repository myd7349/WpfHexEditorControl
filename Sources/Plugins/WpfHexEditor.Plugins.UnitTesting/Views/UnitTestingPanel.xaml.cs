// ==========================================================
// Project: WpfHexEditor.Plugins.UnitTesting
// File: Views/UnitTestingPanel.xaml.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-23
// Updated: 2026-03-24 (ADR-UT-06 — counters in filter buttons + layout dropdown)
// Description:
//     Code-behind for the Unit Testing Panel.
//     Binds the UnitTestingViewModel; delegates run/stop/clear/run-failed
//     to events; wires selection, filter, search, context menu, clipboard.
//     ApplyDetailLayout() switches ContentArea between TopBottom and LeftRight.
// ==========================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WpfHexEditor.Plugins.UnitTesting.Options;
using WpfHexEditor.Plugins.UnitTesting.ViewModels;

namespace WpfHexEditor.Plugins.UnitTesting.Views;

/// <summary>
/// Dockable Unit Testing Panel.
/// </summary>
public partial class UnitTestingPanel : UserControl
{
    public event EventHandler?                  RunAllRequested;
    public event EventHandler?                  StopRequested;
    public event EventHandler<string?>?         RunFailedRequested;
    public event EventHandler<TestResultRow?>?  RunThisTestRequested;
    public event EventHandler?                  SettingsRequested;

    private UnitTestingViewModel Vm => (UnitTestingViewModel)DataContext;

    public UnitTestingPanel(UnitTestingViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UnitTestingViewModel.IsRunning))
                UpdateToolbarState(vm.IsRunning);
        };

        ApplyDetailLayout(UnitTestingOptions.Instance.DetailPaneLayout);
    }

    // ── Toolbar handlers ─────────────────────────────────────────────────────

    private void OnRunAllClicked(object sender, RoutedEventArgs e)
        => RunAllRequested?.Invoke(this, EventArgs.Empty);

    private void OnStopClicked(object sender, RoutedEventArgs e)
        => StopRequested?.Invoke(this, EventArgs.Empty);

    private void OnClearClicked(object sender, RoutedEventArgs e)
        => Vm.Reset();

    private void OnRunFailedClicked(object sender, RoutedEventArgs e)
        => RunFailedRequested?.Invoke(this, null);

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        var opts = UnitTestingOptions.Instance;
        MiAutoRun.IsChecked      = opts.AutoRunOnBuild;
        MiAutoExpand.IsChecked   = opts.AutoExpandDetailOnFailure;
        MiRatioBar.IsChecked     = opts.ShowRatioBar;
        MiGroupByClass.IsChecked = opts.GroupByClass;

        MiSortName.IsChecked     = opts.SortBy == SortOrder.Name;
        MiSortOutcome.IsChecked  = opts.SortBy == SortOrder.Outcome;
        MiSortDuration.IsChecked = opts.SortBy == SortOrder.Duration;

        var menu = SettingsButton.ContextMenu;
        menu.PlacementTarget = SettingsButton;
        menu.Placement       = PlacementMode.Bottom;
        menu.IsOpen          = true;
    }

    private void OnOptionChanged(object sender, RoutedEventArgs e)
    {
        var opts = UnitTestingOptions.Instance;
        opts.AutoRunOnBuild            = MiAutoRun.IsChecked    == true;
        opts.AutoExpandDetailOnFailure = MiAutoExpand.IsChecked == true;
        opts.ShowRatioBar              = MiRatioBar.IsChecked   == true;
        opts.GroupByClass              = MiGroupByClass.IsChecked == true;
        opts.Save();
        Vm.ApplyOptions();
    }

    private void OnSortChanged(object sender, RoutedEventArgs e)
    {
        MiSortName.IsChecked     = sender == MiSortName;
        MiSortOutcome.IsChecked  = sender == MiSortOutcome;
        MiSortDuration.IsChecked = sender == MiSortDuration;

        var opts    = UnitTestingOptions.Instance;
        opts.SortBy = sender == MiSortOutcome  ? SortOrder.Outcome  :
                      sender == MiSortDuration ? SortOrder.Duration :
                      SortOrder.Name;
        opts.Save();
        Vm.ApplyOptions();
    }

    // ── Filter bar ───────────────────────────────────────────────────────────

    private void OnFilterAll(object sender, RoutedEventArgs e)     => Vm.FilterMode = "All";
    private void OnFilterPassed(object sender, RoutedEventArgs e)  => Vm.FilterMode = "Passed";
    private void OnFilterFailed(object sender, RoutedEventArgs e)  => Vm.FilterMode = "Failed";
    private void OnFilterSkipped(object sender, RoutedEventArgs e) => Vm.FilterMode = "Skipped";

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        => Vm.SearchText = SearchBox.Text;

    private void OnClearSearch(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        Vm.SearchText = string.Empty;
    }

    // ── Selection ────────────────────────────────────────────────────────────

    private void OnResultSelectionChanged(object sender, SelectionChangedEventArgs e)
        => Vm.SelectedResult = ResultsList.SelectedItem as TestResultRow;

    // ── Context menu ─────────────────────────────────────────────────────────

    private void OnRunThisTestClicked(object sender, RoutedEventArgs e)
    {
        if (ResultsList.SelectedItem is TestResultRow row)
            RunThisTestRequested?.Invoke(this, row);
    }

    private void OnCopyTestNameClicked(object sender, RoutedEventArgs e)
    {
        if (ResultsList.SelectedItem is TestResultRow row)
            Clipboard.SetText(row.Display);
    }

    private void OnCopyStackTraceClicked(object sender, RoutedEventArgs e)
    {
        var trace = Vm.SelectedResult?.StackTrace;
        if (!string.IsNullOrEmpty(trace))
            Clipboard.SetText(trace);
    }

    // ── Layout dropdown ──────────────────────────────────────────────────────

    private void OnLayoutButtonClicked(object sender, RoutedEventArgs e)
    {
        var opts = UnitTestingOptions.Instance;
        MiLayoutTopBottom.IsChecked = opts.DetailPaneLayout == DetailLayout.TopBottom;
        MiLayoutLeftRight.IsChecked = opts.DetailPaneLayout == DetailLayout.LeftRight;

        var menu = LayoutButton.ContextMenu;
        menu.PlacementTarget = LayoutButton;
        menu.Placement       = PlacementMode.Bottom;
        menu.IsOpen          = true;
    }

    private void OnLayoutChanged(object sender, RoutedEventArgs e)
    {
        MiLayoutTopBottom.IsChecked = sender == MiLayoutTopBottom;
        MiLayoutLeftRight.IsChecked = sender == MiLayoutLeftRight;

        var layout = sender == MiLayoutLeftRight
            ? DetailLayout.LeftRight
            : DetailLayout.TopBottom;

        var opts = UnitTestingOptions.Instance;
        opts.DetailPaneLayout = layout;
        opts.Save();
        ApplyDetailLayout(layout);
    }

    private void ApplyDetailLayout(DetailLayout layout)
    {
        ContentArea.RowDefinitions.Clear();
        ContentArea.ColumnDefinitions.Clear();

        if (layout == DetailLayout.LeftRight)
        {
            ContentArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ContentArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            ContentArea.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width    = new GridLength(260),
                MaxWidth = 420,
                MinWidth = 160,
            });

            Grid.SetRow(ResultsList,    0); Grid.SetColumn(ResultsList,    0);
            Grid.SetRow(ContentSplitter,0); Grid.SetColumn(ContentSplitter,1);
            Grid.SetRow(DetailPane,     0); Grid.SetColumn(DetailPane,     2);

            ContentSplitter.Width               = 4;
            ContentSplitter.Height              = double.NaN;
            ContentSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            ContentSplitter.VerticalAlignment   = VerticalAlignment.Stretch;
            ContentSplitter.ResizeDirection     = GridResizeDirection.Columns;

            DetailPane.MinHeight = 0;
            DetailPane.MaxHeight = double.PositiveInfinity;
            DetailPane.MinWidth  = 160;
            DetailPane.MaxWidth  = 420;
            DetailPane.BorderThickness = new Thickness(1, 0, 0, 0);
        }
        else // TopBottom
        {
            ContentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            ContentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });
            ContentArea.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MinHeight = 0 });

            Grid.SetRow(ResultsList,    0); Grid.SetColumn(ResultsList,    0);
            Grid.SetRow(ContentSplitter,1); Grid.SetColumn(ContentSplitter,0);
            Grid.SetRow(DetailPane,     2); Grid.SetColumn(DetailPane,     0);

            ContentSplitter.Height              = 4;
            ContentSplitter.Width               = double.NaN;
            ContentSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            ContentSplitter.VerticalAlignment   = VerticalAlignment.Stretch;
            ContentSplitter.ResizeDirection     = GridResizeDirection.Rows;

            DetailPane.MinHeight = 100;
            DetailPane.MaxHeight = 260;
            DetailPane.MinWidth  = 0;
            DetailPane.MaxWidth  = double.PositiveInfinity;
            DetailPane.BorderThickness = new Thickness(0, 1, 0, 0);
        }
    }

    // ── Toolbar state sync ───────────────────────────────────────────────────

    private void UpdateToolbarState(bool isRunning)
    {
        RunButton.IsEnabled  = !isRunning;
        StopButton.IsEnabled =  isRunning;
    }
}
