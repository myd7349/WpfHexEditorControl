// ==========================================================
// Project: WpfHexEditor.Plugins.AssemblyExplorer
// File: Views/OpenAssemblyDialog.xaml.cs
// Author: Derek Tremblay
// Created: 2026-03-08
// License: GNU Affero General Public License v3.0 (AGPL-3.0)
// Description:
//     Code-behind for the "Open Assembly" dialog.
//     Accepts paths via: drop zone drag-and-drop, Ctrl+V clipboard paste
//     (both Explorer FileDrop and raw text), manual typing, Browse button,
//     and double-click on the recent files list.
//     Validates the path via File.Exists before enabling the OK button.
//
// Architecture Notes:
//     Pattern: Dialog (modal), returns selected FilePath via property.
//     RecentFileItem is a lightweight display model (FileName + FullPath).
//     No MVVM overhead — this is a single-use dialog; code-behind is appropriate.
// ==========================================================

using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using WpfHexEditor.Plugins.AssemblyExplorer.Options;

namespace WpfHexEditor.Plugins.AssemblyExplorer.Views;

/// <summary>
/// Display model for recent file list items.
/// </summary>
internal sealed class RecentFileItem
{
    public string FileName { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
}

/// <summary>
/// Dialog that allows the user to select a .dll or .exe for analysis.
/// Show with <see cref="ShowDialog"/>; read <see cref="SelectedFilePath"/> on true result.
/// </summary>
public partial class OpenAssemblyDialog : Window
{
    // ── Public output ──────────────────────────────────────────────────────────

    /// <summary>
    /// The absolute file path chosen by the user.
    /// Valid only when <see cref="ShowDialog"/> returns <see langword="true"/>.
    /// </summary>
    public string SelectedFilePath { get; private set; } = string.Empty;

    // ── Construction ──────────────────────────────────────────────────────────

    public OpenAssemblyDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PopulateRecentList();
        PathBox.Focus();
    }

    // ── Recent list ───────────────────────────────────────────────────────────

    private void PopulateRecentList()
    {
        var items = AssemblyExplorerOptions.Instance.RecentFiles
            .Where(File.Exists)
            .Select(p => new RecentFileItem
            {
                FileName = Path.GetFileName(p),
                FullPath = p
            })
            .ToList();

        RecentList.ItemsSource = items;
    }

    private void OnRecentDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RecentList.SelectedItem is RecentFileItem item)
            Accept(item.FullPath);
    }

    private void OnRemoveRecentClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string path })
        {
            AssemblyExplorerOptions.Instance.RecentFiles
                .RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            AssemblyExplorerOptions.Instance.Save();
            PopulateRecentList();
        }
    }

    // ── Drop zone ─────────────────────────────────────────────────────────────

    private void OnDropZoneDragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetDropPath(e) is not null
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDropZoneDragLeave(object sender, DragEventArgs e)
    {
        DropZone.BorderBrush = (Brush)FindResource("DockBorderBrush");
    }

    private void OnDropZoneDrop(object sender, DragEventArgs e)
    {
        DropZone.BorderBrush = (Brush)FindResource("DockBorderBrush");
        var path = GetDropPath(e);
        if (path is not null) SetPath(path);
    }

    private void OnWindowDragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetDropPath(e) is not null
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnWindowDrop(object sender, DragEventArgs e)
    {
        var path = GetDropPath(e);
        if (path is not null) SetPath(path);
    }

    private static string? GetDropPath(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            var first = files?.FirstOrDefault(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext is ".dll" or ".exe";
            });
            return first;
        }
        return null;
    }

    // ── Clipboard / Ctrl+V ────────────────────────────────────────────────────

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
        {
            TryPasteFromClipboard();
            e.Handled = true;
        }
    }

    private void OnPathBoxKeyDown(object sender, KeyEventArgs e)
    {
        // Enter in path box triggers Open if valid
        if (e.Key == Key.Return && OpenButton.IsEnabled)
        {
            Accept(PathBox.Text.Trim());
            e.Handled = true;
        }
    }

    private void TryPasteFromClipboard()
    {
        // Priority 1: Explorer FileDrop (copy a file in Explorer, then Ctrl+V here)
        if (Clipboard.ContainsFileDropList())
        {
            var list = Clipboard.GetFileDropList();
            foreach (string? entry in list)
            {
                if (entry is null) continue;
                var ext = Path.GetExtension(entry).ToLowerInvariant();
                if (ext is ".dll" or ".exe")
                {
                    SetPath(entry);
                    return;
                }
            }
        }

        // Priority 2: Plain text that looks like a path
        if (Clipboard.ContainsText())
        {
            var text = Clipboard.GetText().Trim().Trim('"');
            if (!string.IsNullOrEmpty(text))
            {
                SetPath(text);
                return;
            }
        }
    }

    // ── Browse button ─────────────────────────────────────────────────────────

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title            = "Select Assembly",
            Filter           = "Assembly files (*.dll;*.exe)|*.dll;*.exe|All files (*.*)|*.*",
            RestoreDirectory = true
        };

        if (!string.IsNullOrEmpty(PathBox.Text) && File.Exists(PathBox.Text))
            ofd.InitialDirectory = Path.GetDirectoryName(PathBox.Text);

        if (ofd.ShowDialog(this) == true)
            SetPath(ofd.FileName);
    }

    // ── Path validation ───────────────────────────────────────────────────────

    private void OnPathTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => ValidatePath(PathBox.Text.Trim());

    private void SetPath(string path)
    {
        PathBox.Text = path;
        PathBox.CaretIndex = path.Length;
        ValidatePath(path);
    }

    private void ValidatePath(string path)
    {
        OpenButton.IsEnabled = !string.IsNullOrEmpty(path) && File.Exists(path);
    }

    // ── Dialog result ─────────────────────────────────────────────────────────

    private void OnOpenClick(object sender, RoutedEventArgs e)
        => Accept(PathBox.Text.Trim());

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Accept(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        SelectedFilePath = path;
        AssemblyExplorerOptions.Instance.AddRecentFile(path);

        DialogResult = true;
        Close();
    }
}
