// ==========================================================
// Project: WpfHexEditor.Plugins.LSPTools
// File: Panels/CallHierarchyPanel.xaml.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-30
// Description:
//     IDE panel displaying incoming / outgoing call hierarchy for a symbol.
//     Populated via LspClient.PrepareCallHierarchyAsync / GetIncomingCallsAsync /
//     GetOutgoingCallsAsync. Double-click navigates to the call site.
//
// Architecture Notes:
//     UserControl — stateless view. ViewModel = CallHierarchyNode tree.
//     NavigateRequested event raised to LspToolsPlugin for editor navigation.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WpfHexEditor.Editor.Core.LSP;

namespace WpfHexEditor.Plugins.LSPTools.Panels;

// ── ViewModel ─────────────────────────────────────────────────────────────────

/// <summary>A node in the call hierarchy tree (root item or a caller / callee).</summary>
public sealed class CallHierarchyNode : INotifyPropertyChanged
{
    public string DisplayName  { get; init; } = string.Empty;
    public string KindGlyph    { get; init; } = "\uE943";
    public string FilePath     { get; init; } = string.Empty;
    public int    Line         { get; init; }
    public string LocationHint { get; init; } = string.Empty;

    public ObservableCollection<CallHierarchyNode> Children { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// ── Code-behind ───────────────────────────────────────────────────────────────

/// <summary>IDE panel for call hierarchy navigation (Shift+Alt+H).</summary>
public partial class CallHierarchyPanel : UserControl
{
    public event Action<string, int>? NavigateRequested;
    public event Action? CloseRequested;

    private bool _showIncoming = true;
    private IReadOnlyList<LspCallHierarchyItem>? _rootItems;

    private Func<LspCallHierarchyItem, System.Threading.Tasks.Task<IReadOnlyList<LspIncomingCall>>>?  _getIncoming;
    private Func<LspCallHierarchyItem, System.Threading.Tasks.Task<IReadOnlyList<LspOutgoingCall>>>? _getOutgoing;

    public CallHierarchyPanel()
    {
        InitializeComponent();
    }

    public void SetCallbacks(
        Func<LspCallHierarchyItem, System.Threading.Tasks.Task<IReadOnlyList<LspIncomingCall>>>  getIncoming,
        Func<LspCallHierarchyItem, System.Threading.Tasks.Task<IReadOnlyList<LspOutgoingCall>>> getOutgoing)
    {
        _getIncoming = getIncoming;
        _getOutgoing = getOutgoing;
    }

    public void Refresh(IReadOnlyList<LspCallHierarchyItem> items, string symbolName)
    {
        _rootItems = items;
        SymbolNameText.Text = symbolName;
        RebuildTree();
    }

    // ── UI Handlers ───────────────────────────────────────────────────────────

    private void OnModeChanged(object sender, RoutedEventArgs e)
    {
        if (sender == IncomingToggle && IncomingToggle.IsChecked == true)
        {
            OutgoingToggle.IsChecked = false;
            _showIncoming = true;
        }
        else if (sender == OutgoingToggle && OutgoingToggle.IsChecked == true)
        {
            IncomingToggle.IsChecked = false;
            _showIncoming = false;
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
        if (e.NewValue is CallHierarchyNode node && node.Children.Count == 0)
            _ = ExpandNodeAsync(node);
    }

    private void OnTreeItemDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (HierarchyTree.SelectedItem is CallHierarchyNode node && !string.IsNullOrEmpty(node.FilePath))
            NavigateRequested?.Invoke(node.FilePath, node.Line + 1);
    }

    private void OnTreeKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && HierarchyTree.SelectedItem is CallHierarchyNode node)
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
            node.Children.Add(new CallHierarchyNode { DisplayName = "Loading…" });
            HierarchyTree.Items.Add(node);
        }
    }

    private async System.Threading.Tasks.Task ExpandNodeAsync(CallHierarchyNode node)
    {
        if (node.Children.Count != 1 || node.Children[0].DisplayName != "Loading…") return;
        if (node.FilePath is null) return;

        node.Children.Clear();

        var lspItem = new LspCallHierarchyItem
        {
            Name        = node.DisplayName,
            Kind        = GlyphToKind(node.KindGlyph),
            Uri         = node.FilePath,
            StartLine   = node.Line,
            StartColumn = 0,
        };

        try
        {
            if (_showIncoming && _getIncoming is not null)
            {
                var calls = await _getIncoming(lspItem).ConfigureAwait(true);
                foreach (var call in calls)
                {
                    var child = ItemToNode(call.From);
                    child.Children.Add(new CallHierarchyNode { DisplayName = "Loading…" });
                    node.Children.Add(child);
                }
            }
            else if (!_showIncoming && _getOutgoing is not null)
            {
                var calls = await _getOutgoing(lspItem).ConfigureAwait(true);
                foreach (var call in calls)
                {
                    var child = ItemToNode(call.To);
                    child.Children.Add(new CallHierarchyNode { DisplayName = "Loading…" });
                    node.Children.Add(child);
                }
            }
        }
        catch { /* silent — server may not support call hierarchy */ }
    }

    private static CallHierarchyNode ItemToNode(LspCallHierarchyItem item)
    {
        var fileName = string.IsNullOrEmpty(item.Uri)
            ? string.Empty
            : System.IO.Path.GetFileName(item.Uri);

        return new CallHierarchyNode
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
        "method"      => "\uF158",
        "function"    => "\uF158",
        "constructor" => "\uF158",
        "class"       => "\uE8A9",
        "interface"   => "\uE8A9",
        "property"    => "\uE713",
        "field"       => "\uE713",
        "variable"    => "\uE943",
        "constant"    => "\uE943",
        "namespace"   => "\uE8B7",
        "module"      => "\uE8B7",
        _             => "\uE943",
    };

    private static string GlyphToKind(string glyph) => glyph switch
    {
        "\uF158" => "method",
        "\uE8A9" => "class",
        "\uE713" => "property",
        "\uE8B7" => "namespace",
        _        => "symbol",
    };
}
