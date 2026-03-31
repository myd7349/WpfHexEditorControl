// ==========================================================
// Project: WpfHexEditor.Plugins.LSPTools
// File: Panels/TypeHierarchyPanel.xaml.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-30
// Description:
//     IDE panel displaying supertypes / subtypes for a type symbol.
//     Populated via LspClient.PrepareTypeHierarchyAsync / GetSupertypesAsync /
//     GetSubtypesAsync. Double-click navigates to the type declaration.
//
// Architecture Notes:
//     Reuses CH_* tokens for theming (same visual style as CallHierarchyPanel).
//     NavigateRequested event raised to LspToolsPlugin for editor navigation.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Plugins.LSPTools.Panels;

// ── ViewModel ─────────────────────────────────────────────────────────────────

/// <summary>A node in the type hierarchy tree.</summary>
public sealed class TypeHierarchyNode : INotifyPropertyChanged
{
    public string DisplayName  { get; init; } = string.Empty;
    public string KindGlyph    { get; init; } = "\uE8A9";
    public string FilePath     { get; init; } = string.Empty;
    public int    Line         { get; init; }
    public string LocationHint { get; init; } = string.Empty;

    public ObservableCollection<TypeHierarchyNode> Children { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
}

// ── Code-behind ───────────────────────────────────────────────────────────────

/// <summary>IDE panel for type hierarchy navigation (Ctrl+F12).</summary>
public partial class TypeHierarchyPanel : UserControl
{
    public event Action<string, int>? NavigateRequested;
    public event Action? CloseRequested;

    private bool _showSupertypes = true;
    private IReadOnlyList<LspTypeHierarchyItem>? _rootItems;

    private Func<LspTypeHierarchyItem, System.Threading.Tasks.Task<IReadOnlyList<LspTypeHierarchyItem>>>?
        _getSupertypes;
    private Func<LspTypeHierarchyItem, System.Threading.Tasks.Task<IReadOnlyList<LspTypeHierarchyItem>>>?
        _getSubtypes;

    public TypeHierarchyPanel()
    {
        InitializeComponent();
    }

    public void SetCallbacks(
        Func<LspTypeHierarchyItem, System.Threading.Tasks.Task<IReadOnlyList<LspTypeHierarchyItem>>> getSupertypes,
        Func<LspTypeHierarchyItem, System.Threading.Tasks.Task<IReadOnlyList<LspTypeHierarchyItem>>> getSubtypes)
    {
        _getSupertypes = getSupertypes;
        _getSubtypes   = getSubtypes;
    }

    public void Refresh(IReadOnlyList<LspTypeHierarchyItem> items, string symbolName)
    {
        _rootItems = items;
        SymbolNameText.Text = symbolName;
        RebuildTree();
    }

    // ── UI handlers ───────────────────────────────────────────────────────────

    private void OnModeChanged(object sender, RoutedEventArgs e)
    {
        if (sender == SupertypesToggle && SupertypesToggle.IsChecked == true)
        {
            SubtypesToggle.IsChecked = false;
            _showSupertypes = true;
        }
        else if (sender == SubtypesToggle && SubtypesToggle.IsChecked == true)
        {
            SupertypesToggle.IsChecked = false;
            _showSupertypes = false;
        }
        else
        {
            if (sender is ToggleButton tb) tb.IsChecked = true;
            return;
        }

        RebuildTree();
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs e) => RebuildTree();

    private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TypeHierarchyNode node && node.Children.Count == 0)
            _ = ExpandNodeAsync(node);
    }

    private void OnTreeItemDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (HierarchyTree.SelectedItem is TypeHierarchyNode node && !string.IsNullOrEmpty(node.FilePath))
            NavigateRequested?.Invoke(node.FilePath, node.Line + 1);
    }

    private void OnTreeKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && HierarchyTree.SelectedItem is TypeHierarchyNode node)
        {
            if (!string.IsNullOrEmpty(node.FilePath))
                NavigateRequested?.Invoke(node.FilePath, node.Line + 1);
            e.Handled = true;
        }
    }

    // ── Tree building ─────────────────────────────────────────────────────────

    private void RebuildTree()
    {
        HierarchyTree.Items.Clear();

        if (_rootItems is null || _rootItems.Count == 0)
        {
            EmptyPlaceholder.Visibility = Visibility.Visible;
            return;
        }

        EmptyPlaceholder.Visibility = Visibility.Collapsed;

        foreach (var item in _rootItems)
        {
            var node = ItemToNode(item);
            node.Children.Add(new TypeHierarchyNode { DisplayName = "Loading…" });
            HierarchyTree.Items.Add(node);
        }
    }

    private async System.Threading.Tasks.Task ExpandNodeAsync(TypeHierarchyNode node)
    {
        if (node.Children.Count != 1 || node.Children[0].DisplayName != "Loading…") return;
        node.Children.Clear();

        var lspItem = new LspTypeHierarchyItem
        {
            Name        = node.DisplayName,
            Kind        = "class",
            Uri         = node.FilePath,
            StartLine   = node.Line,
            StartColumn = 0,
        };

        try
        {
            var items = _showSupertypes
                ? (_getSupertypes is not null ? await _getSupertypes(lspItem).ConfigureAwait(true)
                                              : Array.Empty<LspTypeHierarchyItem>())
                : (_getSubtypes  is not null ? await _getSubtypes(lspItem).ConfigureAwait(true)
                                              : Array.Empty<LspTypeHierarchyItem>());

            foreach (var item in items)
            {
                var child = ItemToNode(item);
                child.Children.Add(new TypeHierarchyNode { DisplayName = "Loading…" });
                node.Children.Add(child);
            }
        }
        catch { /* silent */ }
    }

    private static TypeHierarchyNode ItemToNode(LspTypeHierarchyItem item)
    {
        var fileName = string.IsNullOrEmpty(item.Uri)
            ? string.Empty
            : System.IO.Path.GetFileName(item.Uri);

        return new TypeHierarchyNode
        {
            DisplayName  = string.IsNullOrEmpty(item.ContainerName)
                           ? item.Name
                           : $"{item.ContainerName}.{item.Name}",
            KindGlyph    = KindToGlyph(item.Kind),
            FilePath     = item.Uri,
            Line         = item.StartLine,
            LocationHint = $"{fileName}:{item.StartLine + 1}",
        };
    }

    private static string KindToGlyph(string kind) => kind switch
    {
        "class"     => "\uE8A9",
        "interface" => "\uE8A9",
        "struct"    => "\uE8A9",
        "enum"      => "\uE8A9",
        _           => "\uE8A9",
    };
}
