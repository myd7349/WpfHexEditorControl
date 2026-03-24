// Project      : WpfHexEditorControl
// File         : Views/DiffHubPanel.xaml.cs
// Description  : Code-behind for DiffHubPanel. Handles browse dialogs, drag-and-drop,
//                history re-open, and the "Open in Viewer" delegate callback.
// Architecture : Thin code-behind — delegates work to DiffHubViewModel.
//                "Open in Viewer" is wired by FileComparisonPlugin via OpenInViewerRequested.

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using WpfHexEditor.Plugins.FileComparison.ViewModels;

namespace WpfHexEditor.Plugins.FileComparison.Views;

public sealed partial class DiffHubPanel : UserControl
{
    private readonly DiffHubViewModel _vm;

    /// <summary>
    /// Called when the user clicks "Open in Viewer" — the application shell subscribes
    /// to this to create a DiffViewer document tab in the main area.
    /// </summary>
    public event EventHandler<(string Left, string Right)>? OpenInViewerRequested;

    public DiffHubPanel()
    {
        InitializeComponent();
        _vm = new DiffHubViewModel();
        DataContext = _vm;

        // Ctrl+Enter shortcut to compare
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                _vm.CompareCommand.Execute(null);
        };
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SuggestFile1(string path) => _vm.SuggestFile1(path);

    public void LoadHistory(IEnumerable<ComparisonHistoryEntry> history) => _vm.LoadHistory(history);

    // ── Browse buttons ────────────────────────────────────────────────────────

    private void OnBrowseFile1_Click(object sender, RoutedEventArgs e)
    {
        var path = PickFile("Select File 1 (Left)");
        if (path is not null) _vm.File1Path = path;
    }

    private void OnBrowseFile2_Click(object sender, RoutedEventArgs e)
    {
        var path = PickFile("Select File 2 (Right)");
        if (path is not null) _vm.File2Path = path;
    }

    private static string? PickFile(string title)
    {
        var dlg = new OpenFileDialog { Title = title };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    // ── Drag-and-drop ─────────────────────────────────────────────────────────

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnFile1Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            _vm.File1Path = files[0];
    }

    private void OnFile2Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            _vm.File2Path = files[0];
    }

    // ── Open in Viewer ────────────────────────────────────────────────────────

    private void OnOpenInViewer_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_vm.File1Path) || string.IsNullOrEmpty(_vm.File2Path)) return;
        OpenInViewerRequested?.Invoke(this, (_vm.File1Path, _vm.File2Path));
    }

    // ── Results double-click ──────────────────────────────────────────────────

    private void OnResultDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Double-clicking a result row opens the full viewer at the diff location
        if (!string.IsNullOrEmpty(_vm.File1Path) && !string.IsNullOrEmpty(_vm.File2Path))
            OpenInViewerRequested?.Invoke(this, (_vm.File1Path, _vm.File2Path));
    }

    // ── History double-click ─────────────────────────────────────────────────

    private void OnHistoryDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (HistoryList.SelectedItem is ComparisonHistoryEntry entry)
        {
            _vm.File1Path = entry.LeftPath;
            _vm.File2Path = entry.RightPath;
            _ = _vm.CompareAsync();
        }
    }
}
