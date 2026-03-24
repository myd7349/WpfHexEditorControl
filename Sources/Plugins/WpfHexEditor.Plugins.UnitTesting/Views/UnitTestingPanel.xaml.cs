// ==========================================================
// Project: WpfHexEditor.Plugins.UnitTesting
// File: Views/UnitTestingPanel.xaml.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-23
// Description:
//     Code-behind for the Unit Testing Panel.
//     Binds the UnitTestingViewModel; delegates run/stop/clear to events.
// ==========================================================

using System.Windows;
using System.Windows.Controls;
using WpfHexEditor.Plugins.UnitTesting.ViewModels;

namespace WpfHexEditor.Plugins.UnitTesting.Views;

/// <summary>
/// Dockable Unit Testing Panel.
/// </summary>
public partial class UnitTestingPanel : UserControl
{
    public event EventHandler? RunAllRequested;
    public event EventHandler? StopRequested;

    public UnitTestingPanel(UnitTestingViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UnitTestingViewModel.IsRunning))
                UpdateToolbarState(vm.IsRunning);
        };
    }

    // ── Toolbar handlers ─────────────────────────────────────────────────────

    private void OnRunAllClicked(object sender, RoutedEventArgs e)
        => RunAllRequested?.Invoke(this, EventArgs.Empty);

    private void OnStopClicked(object sender, RoutedEventArgs e)
        => StopRequested?.Invoke(this, EventArgs.Empty);

    private void OnClearClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is UnitTestingViewModel vm) vm.Reset();
    }

    private void OnResultSelectionChanged(object sender, SelectionChangedEventArgs e) { }

    // ── Toolbar state sync ───────────────────────────────────────────────────

    private void UpdateToolbarState(bool isRunning)
    {
        RunButton.IsEnabled  = !isRunning;
        StopButton.IsEnabled =  isRunning;
    }
}
