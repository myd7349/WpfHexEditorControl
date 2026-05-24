// Project     : WpfHexEditor.App
// File        : StringDiffPanel.cs
// Description : Diff panel — pick two snapshots, compare, view Added/Removed/Modified/Unchanged.
// Architecture: Code-behind UserControl; delegates diff logic to StringDiffService.
//               Columns use FrameworkElementFactory + IValueConverter (stateless) instead of
//               LoadedEvent + DataContextChanged, which caused an infinite-loop with
//               VirtualizationMode.Recycling (each recycle fired Loaded again, accumulating
//               handlers that fired on every subsequent DataContext swap).

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using WpfHexEditor.App.BinaryAnalysis.Services;
using WpfHexEditor.App.BinaryAnalysis.ViewModels;

namespace WpfHexEditor.App.BinaryAnalysis.Panels;

// ── Converters ────────────────────────────────────────────────────────────────

/// <summary>Maps <see cref="StringDiffEntry"/> → display text for a given column.</summary>
internal sealed class DiffTextConverter(Func<StringDiffEntry, string> selector) : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is StringDiffEntry e ? selector(e) : string.Empty;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => Binding.DoNothing;
}

/// <summary>Maps <see cref="StringDiffEntry.Status"/> → badge background brush.</summary>
internal sealed class DiffStatusBrushConverter : IValueConverter
{
    internal static readonly DiffStatusBrushConverter Instance = new();

    private static readonly SolidColorBrush AddedBrush    = Freeze(new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)));
    private static readonly SolidColorBrush RemovedBrush  = Freeze(new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)));
    private static readonly SolidColorBrush ModifiedBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xE6, 0x51, 0x00)));
    private static readonly SolidColorBrush NeutralBrush  = Freeze(new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60)));

    private static SolidColorBrush Freeze(SolidColorBrush b) { b.Freeze(); return b; }

    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is StringDiffStatus s ? StatusBrush(s) : NeutralBrush;

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => Binding.DoNothing;

    internal static SolidColorBrush StatusBrush(StringDiffStatus s) => s switch
    {
        StringDiffStatus.Added    => AddedBrush,
        StringDiffStatus.Removed  => RemovedBrush,
        StringDiffStatus.Modified => ModifiedBrush,
        _                         => NeutralBrush,
    };

    internal static string StatusLabel(StringDiffStatus s) => s switch
    {
        StringDiffStatus.Added    => "Added",
        StringDiffStatus.Removed  => "Removed",
        StringDiffStatus.Modified => "Modified",
        _                         => "Unchanged",
    };
}

/// <summary>Maps <see cref="StringDiffEntry.Status"/> → badge label string.</summary>
internal sealed class DiffStatusLabelConverter : IValueConverter
{
    internal static readonly DiffStatusLabelConverter Instance = new();
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is StringDiffStatus s ? DiffStatusBrushConverter.StatusLabel(s) : string.Empty;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => Binding.DoNothing;
}

// ── Panel ─────────────────────────────────────────────────────────────────────

internal sealed class StringDiffPanel : UserControl
{
    private readonly StringExtractionViewModel _vm;
    private DataGrid  _grid        = null!;
    private ComboBox  _snapACombo  = null!;
    private ComboBox  _snapBCombo  = null!;
    private TextBlock _summaryText = null!;

    public StringDiffPanel(StringExtractionViewModel vm)
    {
        _vm     = vm;
        Content = BuildLayout();
    }

    private UIElement BuildLayout()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.SetResourceReference(BackgroundProperty, "TE_Background");

        var toolbar = BuildToolbar();
        var grid    = BuildGrid();
        var legend  = BuildLegend();

        Grid.SetRow(toolbar, 0);
        Grid.SetRow(grid,    1);
        Grid.SetRow(legend,  2);
        root.Children.Add(toolbar);
        root.Children.Add(grid);
        root.Children.Add(legend);
        return root;
    }

    // ── Toolbar ───────────────────────────────────────────────────────────────

    private UIElement BuildToolbar()
    {
        var toolbar = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin      = new Thickness(4),
        };
        toolbar.SetResourceReference(BackgroundProperty, "Panel_ToolbarBrush");

        _snapACombo = MakeSnapshotCombo();
        _snapBCombo = MakeSnapshotCombo();

        var compareBtn = new Button
        {
            Content          = "Compare",
            Padding          = new Thickness(10, 3, 10, 3),
            Margin           = new Thickness(8, 0, 0, 0),
            FontSize         = 11,
            FocusVisualStyle = null,
        };
        compareBtn.SetResourceReference(StyleProperty,      "PanelIconButtonStyle");
        compareBtn.SetResourceReference(ForegroundProperty, "Panel_ToolbarForegroundBrush");
        compareBtn.Click += (_, _) => RunDiff();

        _summaryText = new TextBlock
        {
            FontSize              = 10,
            VerticalAlignment     = VerticalAlignment.Center,
            Margin                = new Thickness(12, 0, 0, 0),
        };
        _summaryText.SetResourceReference(ForegroundProperty, "Panel_ToolbarForegroundBrush");

        toolbar.Children.Add(MakeLabel("Snapshot A:"));
        toolbar.Children.Add(_snapACombo);
        toolbar.Children.Add(MakeLabel("Snapshot B:"));
        toolbar.Children.Add(_snapBCombo);
        toolbar.Children.Add(compareBtn);
        toolbar.Children.Add(_summaryText);
        return toolbar;
    }

    // ── DataGrid ──────────────────────────────────────────────────────────────

    private DataGrid BuildGrid()
    {
        var grid = new DataGrid
        {
            AutoGenerateColumns     = false,
            CanUserAddRows          = false,
            CanUserDeleteRows       = false,
            IsReadOnly              = true,
            SelectionMode           = DataGridSelectionMode.Single,
            EnableRowVirtualization = true,
            GridLinesVisibility     = DataGridGridLinesVisibility.Horizontal,
            HeadersVisibility       = DataGridHeadersVisibility.Column,
            BorderThickness         = new Thickness(0),
        };
        VirtualizingPanel.SetIsVirtualizing(grid, true);
        VirtualizingPanel.SetVirtualizationMode(grid, VirtualizationMode.Recycling);
        _grid = grid;
        grid.SetResourceReference(BackgroundProperty, "TE_Background");
        grid.SetResourceReference(ForegroundProperty, "TE_Foreground");
        grid.ColumnHeaderStyle = BuildHeaderStyle();

        grid.Columns.Add(MakeStatusColumn());
        grid.Columns.Add(MakeTextCol("Offset",    e => $"0x{e.Run.Offset:X8}", 90));
        grid.Columns.Add(MakeTextCol("Encoding",  e => e.Run.Encoding.ToString(), 80));
        grid.Columns.Add(MakeTextCol("Value",     e => e.Run.Value, 0));
        grid.Columns.Add(MakeTextCol("Old Value", e => e.OldValue ?? string.Empty, 0));
        return grid;
    }

    private static Style BuildHeaderStyle()
    {
        var s = new Style(typeof(DataGridColumnHeader));
        s.Setters.Add(new Setter(BackgroundProperty,      new DynamicResourceExtension("Panel_ToolbarBrush")));
        s.Setters.Add(new Setter(ForegroundProperty,      new DynamicResourceExtension("Panel_ToolbarForegroundBrush")));
        s.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
        s.Setters.Add(new Setter(PaddingProperty,         new Thickness(6, 3, 6, 3)));
        s.Setters.Add(new Setter(FontSizeProperty,        11d));
        return s;
    }

    // ── Status column — badge with background from converter (no event subscriptions) ──

    private static DataGridTemplateColumn MakeStatusColumn()
    {
        var col = new DataGridTemplateColumn { Header = "Status", Width = 80, CanUserSort = false };

        // Border background bound to Entry.Status via DiffStatusBrushConverter
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty,         new CornerRadius(3));
        border.SetValue(Border.PaddingProperty,              new Thickness(4, 1, 4, 1));
        border.SetValue(Border.MarginProperty,               new Thickness(2, 1, 2, 1));
        border.SetValue(Border.HorizontalAlignmentProperty,  HorizontalAlignment.Left);
        border.SetValue(Border.VerticalAlignmentProperty,    VerticalAlignment.Center);
        border.SetBinding(Border.BackgroundProperty,
            new Binding(nameof(StringDiffEntry.Status)) { Converter = DiffStatusBrushConverter.Instance });

        var label = new FrameworkElementFactory(typeof(TextBlock));
        label.SetValue(TextBlock.FontSizeProperty,   9d);
        label.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        label.SetValue(TextBlock.ForegroundProperty, Brushes.White);
        label.SetBinding(TextBlock.TextProperty,
            new Binding(nameof(StringDiffEntry.Status)) { Converter = DiffStatusLabelConverter.Instance });

        border.AppendChild(label);
        col.CellTemplate = new DataTemplate { VisualTree = border };
        return col;
    }

    // ── Text column — bound via stateless DiffTextConverter (no event subscriptions) ──

    private static DataGridTemplateColumn MakeTextCol(string header, Func<StringDiffEntry, string> selector, double width)
    {
        var col = new DataGridTemplateColumn
        {
            Header = new TextBlock { Text = header },
            Width  = width > 0 ? new DataGridLength(width) : DataGridLength.Auto,
        };

        var tb = new FrameworkElementFactory(typeof(TextBlock));
        tb.SetValue(TextBlock.FontSizeProperty,            11d);
        tb.SetValue(TextBlock.VerticalAlignmentProperty,   VerticalAlignment.Center);
        // Bind the whole DataContext (StringDiffEntry) through a per-column converter instance.
        // A new converter instance per column is required because each captures a different selector.
        tb.SetBinding(TextBlock.TextProperty,
            new Binding { Converter = new DiffTextConverter(selector) });

        col.CellTemplate = new DataTemplate { VisualTree = tb };
        return col;
    }

    // ── Legend ────────────────────────────────────────────────────────────────

    private static UIElement BuildLegend()
    {
        var panel = new WrapPanel { Margin = new Thickness(4, 2, 4, 2) };
        panel.Children.Add(MakeLegendChip("Added",     StringDiffStatus.Added));
        panel.Children.Add(MakeLegendChip("Removed",   StringDiffStatus.Removed));
        panel.Children.Add(MakeLegendChip("Modified",  StringDiffStatus.Modified));
        panel.Children.Add(MakeLegendChip("Unchanged", StringDiffStatus.Unchanged));
        return panel;
    }

    private static Border MakeLegendChip(string label, StringDiffStatus status) =>
        new()
        {
            Background   = DiffStatusBrushConverter.StatusBrush(status),
            CornerRadius = new CornerRadius(3),
            Padding      = new Thickness(6, 1, 6, 1),
            Margin       = new Thickness(0, 0, 4, 0),
            Child        = new TextBlock
            {
                Text       = label,
                FontSize   = 9,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
            },
        };

    // ── Diff logic ────────────────────────────────────────────────────────────

    private void RunDiff()
    {
        var a = _snapACombo.SelectedItem as StringExtractionViewModel.ScanSnapshot;
        var b = _snapBCombo.SelectedItem as StringExtractionViewModel.ScanSnapshot;
        if (a is null || b is null) { _summaryText.Text = "Select two snapshots."; return; }

        var entries  = StringDiffService.Compare(a.Runs, b.Runs);
        _grid.ItemsSource = entries;

        int added = 0, removed = 0, modified = 0, unchanged = 0;
        foreach (var e in entries)
        {
            switch (e.Status)
            {
                case StringDiffStatus.Added:    added++;    break;
                case StringDiffStatus.Removed:  removed++;  break;
                case StringDiffStatus.Modified: modified++; break;
                default:                        unchanged++; break;
            }
        }
        _summaryText.Text = $"+{added}  -{removed}  ~{modified}  ={unchanged}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private ComboBox MakeSnapshotCombo()
    {
        var cb = new ComboBox
        {
            Width              = 220,
            Height             = 22,
            FontSize           = 11,
            Margin             = new Thickness(0, 0, 8, 0),
            DisplayMemberPath  = nameof(StringExtractionViewModel.ScanSnapshot.DisplayName),
        };
        cb.SetResourceReference(BackgroundProperty, "TE_Background");
        cb.SetResourceReference(ForegroundProperty, "TE_Foreground");
        cb.SetBinding(ItemsControl.ItemsSourceProperty,
            new Binding(nameof(_vm.Snapshots)) { Source = _vm });
        return cb;
    }

    private static TextBlock MakeLabel(string text)
    {
        var tb = new TextBlock
        {
            Text              = text,
            FontSize          = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(0, 0, 4, 0),
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, "Panel_ToolbarForegroundBrush");
        return tb;
    }
}
