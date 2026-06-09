// ==========================================================
// Project: WpfHexEditor.Plugins.ClassDiagram
// File: Panels/ClassToolboxPanel.xaml.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-19
// Description:
//     Code-behind for ClassToolboxPanel.
//     Instantiates and exposes ClassToolboxPanelViewModel.
//     Implements drag-and-drop initiation: PreviewMouseMove detects
//     a left-button drag and starts DragDrop with the selected
//     ToolboxEntry as the payload ("ClassDiagramToolboxEntry").
//     Right-click context menu: "Add to Diagram" / "Add at Center".
//
// Architecture Notes:
//     Pattern: View (MVVM).
//     Drop target is DiagramCanvas (AllowDrop=true, OnDrop handles the entry).
//     AddEntryRequested event is wired by ClassDiagramPlugin to the active host.
// ==========================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexEditor.Editor.ClassDiagram.Services;
using WpfHexEditor.Editor.ClassDiagram.ViewModels;

namespace WpfHexEditor.Plugins.ClassDiagram.Panels;

/// <summary>
/// Dockable toolbox panel listing draggable diagram element types.
/// </summary>
public partial class ClassToolboxPanel : UserControl
{
    /// <summary>Gets the ViewModel backing this panel.</summary>
    public ClassToolboxPanelViewModel ViewModel { get; }

    /// <summary>
    /// Fired when the user selects "Add to Diagram" or "Add at Center" from the context menu.
    /// The bool argument is true when the node should be placed at the viewport center.
    /// </summary>
    public event EventHandler<(ToolboxEntry Entry, bool AtCenter)>? AddEntryRequested;

    public ClassToolboxPanel()
    {
        ViewModel   = new ClassToolboxPanelViewModel();
        DataContext = ViewModel;
        InitializeComponent();
    }

    // ── Drag initiation ──────────────────────────────────────────────────────

    private Point _dragStart;
    private bool  _dragPending;

    private void OnListBoxPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || !_dragPending) return;
        if (sender is not ListBox lb) return;

        Point pt   = e.GetPosition(lb);
        double dx  = pt.X - _dragStart.X;
        double dy  = pt.Y - _dragStart.Y;
        if (Math.Abs(dx) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(dy) < SystemParameters.MinimumVerticalDragDistance) return;

        _dragPending = false;

        // Resolve the item under the drag-start point — SelectedItem may not yet be set
        var entry = ResolveEntryAt(lb, _dragStart);
        if (entry is null) return;

        var data = new DataObject("ClassDiagramToolboxEntry", entry);
        DragDrop.DoDragDrop(lb, data, DragDropEffects.Copy);
    }

    private void OnListBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox lb) return;
        _dragStart   = e.GetPosition(lb);
        _dragPending = ResolveEntryAt(lb, _dragStart) is not null;
    }

    private static ToolboxEntry? ResolveEntryAt(ListBox lb, Point pt)
    {
        // Walk up the visual tree from the hit element to find a ListBoxItem
        var hit = lb.InputHitTest(pt) as DependencyObject;
        while (hit is not null && hit is not ListBoxItem)
            hit = VisualTreeHelper.GetParent(hit);

        return hit is ListBoxItem { DataContext: ToolboxEntry entry } ? entry : null;
    }

    // ── Context menu ─────────────────────────────────────────────────────────

    private void OnListBoxMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox lb) return;
        var pt    = e.GetPosition(lb);
        var entry = ResolveEntryAt(lb, pt);
        if (entry is null) return;

        var menu = new ContextMenu();
        menu.Items.Add(MakeItem("", "Add to Diagram",  () => AddEntryRequested?.Invoke(this, (entry, false))));
        menu.Items.Add(MakeItem("", "Add at Center",   () => AddEntryRequested?.Invoke(this, (entry, true))));
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header    = $"{entry.Kind}  —  {entry.DefaultMembers.Count} default member(s)",
            IsEnabled = false,
            FontSize  = 10
        });
        menu.IsOpen = true;
        e.Handled   = true;
    }

    private static MenuItem MakeItem(string icon, string header, Action action)
    {
        var item = new MenuItem { Header = header };
        if (!string.IsNullOrEmpty(icon))
            item.Icon = new TextBlock
            {
                Text       = icon,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize   = 12
            };
        item.Click += (_, _) => action();
        return item;
    }
}
