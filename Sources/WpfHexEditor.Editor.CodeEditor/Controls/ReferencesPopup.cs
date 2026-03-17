// ==========================================================
// Project: WpfHexEditor.Editor.CodeEditor
// File: Controls/ReferencesPopup.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-17
// Description:
//     Inline "Find All References" popup anchored near the cursor.
//     Displays results grouped by file with VS Code–style collapsible
//     sections: folder path (dimmed) + bold filename + (count),
//     reference rows with icon + line number + highlighted snippet.
//
// Architecture Notes:
//     - Popup-derived (StaysOpen=true — explicit dismiss via click-outside
//       or Escape; CodeEditor.OnMouseDown closes it on any click)
//     - AllowsTransparency=true for drop-shadow effect
//     - All colours via SetResourceReference / DynamicResource
//       (CE_* / Panel_* / TE_* / PFP_* tokens) — theme-agnostic
//     - Fires NavigationRequested; CodeEditor routes cross-file navigation
//       to the host through its own ReferenceNavigationRequested event
//     - Group sections are independently collapsible (VS-style chevron toggle)
// ==========================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace WpfHexEditor.Editor.CodeEditor.Controls
{
    // ── Public data model ──────────────────────────────────────────────────────

    /// <summary>A single reference occurrence within a file.</summary>
    public sealed class ReferenceItem
    {
        /// <summary>0-based line index.</summary>
        public int Line { get; init; }

        /// <summary>0-based column of the symbol start in the original (non-trimmed) line.</summary>
        public int Column { get; init; }

        /// <summary>Source line text (may be TrimStart'd); max 200 chars.</summary>
        public string Snippet { get; init; } = string.Empty;
    }

    /// <summary>All references from a single file.</summary>
    public sealed class ReferenceGroup
    {
        /// <summary>Absolute file path.</summary>
        public string FilePath { get; init; } = string.Empty;

        /// <summary>Short label shown in the header (file name or relative path).</summary>
        public string DisplayLabel { get; init; } = string.Empty;

        /// <summary>Ordered list of reference occurrences in this file.</summary>
        public IReadOnlyList<ReferenceItem> Items { get; init; } = Array.Empty<ReferenceItem>();
    }

    /// <summary>Event args fired when the user clicks a reference entry.</summary>
    public sealed class ReferencesNavigationEventArgs : EventArgs
    {
        public string FilePath { get; init; } = string.Empty;
        public int    Line     { get; init; }
        public int    Column   { get; init; }
    }

    // ── ReferencesPopup control ────────────────────────────────────────────────

    /// <summary>
    /// Inline popup that lists all "Find References" results grouped by file,
    /// using a VS Code–style collapsible tree layout.
    /// Anchor it to the editor via <see cref="Show"/>.
    /// </summary>
    internal sealed class ReferencesPopup : Popup
    {
        #region Fields

        private StackPanel  _groupsPanel    = null!;
        private Button      _collapseAllBtn = null!;
        private Point       _anchor;
        private string      _symbolName     = string.Empty;
        private bool        _allCollapsed;

        // Frozen colour for the ◆ reference glyph (VS Code method purple).
        private static readonly Brush s_glyphBrush = MakeFrozenBrush(Color.FromRgb(0xC5, 0x86, 0xC0));

        // Frozen semi-transparent background for the highlighted symbol span.
        private static readonly Brush s_symbolHighlightBg = MakeFrozenBrush(Color.FromArgb(80, 0x20, 0x56, 0xA0));

        private readonly List<(StackPanel ItemsPanel, TextBlock Chevron)> _groups = new();

        #endregion

        #region Events

        /// <summary>Fired when the user clicks a reference row. Handle to navigate.</summary>
        public event EventHandler<ReferencesNavigationEventArgs>? NavigationRequested;

        #endregion

        #region Constructor

        internal ReferencesPopup()
        {
            StaysOpen          = true;   // Explicit dismiss: Escape or CodeEditor.OnMouseDown
            AllowsTransparency = true;

            BuildUI();

            PreviewKeyDown += OnPreviewKeyDown;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Populates and shows the popup anchored at <paramref name="anchor"/>
        /// (editor-relative coordinates, below the cursor).
        /// </summary>
        internal void Show(
            CodeEditor                   owner,
            IReadOnlyList<ReferenceGroup> groups,
            string                       symbolName,
            Point                        anchor)
        {
            _anchor     = anchor;
            _symbolName = symbolName ?? string.Empty;
            _allCollapsed = false;

            PlacementTarget              = owner;
            Placement                    = PlacementMode.Custom;
            CustomPopupPlacementCallback = CalculatePlacement;

            PopulateContent(groups);
            IsOpen = true;
        }

        /// <summary>Closes and clears the popup.</summary>
        internal new void Close()
        {
            IsOpen = false;
            _groups.Clear();
            _groupsPanel.Children.Clear();
        }

        #endregion

        #region UI Construction

        private void BuildUI()
        {
            // ── Outer border: shadow + rounded corners ─────────────────────────
            var outerBorder = new Border
            {
                MinWidth        = 480,
                MaxWidth        = 700,
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(4),
                Effect          = new DropShadowEffect
                {
                    Color       = Colors.Black,
                    Opacity     = 0.40,
                    BlurRadius  = 10,
                    ShadowDepth = 3
                }
            };
            outerBorder.SetResourceReference(Border.BackgroundProperty,  "TE_Background");
            outerBorder.SetResourceReference(Border.BorderBrushProperty, "Panel_ToolbarBorderBrush");

            // ── Root grid: scrollable body (row 0) + footer (row 1) ───────────
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ── Body: scrollable groups ────────────────────────────────────────
            _groupsPanel = new StackPanel();
            _groupsPanel.SetResourceReference(StackPanel.BackgroundProperty, "TE_Background");

            var scroll = new ScrollViewer
            {
                MaxHeight                     = 400,
                VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content                       = _groupsPanel
            };
            scroll.SetResourceReference(ScrollViewer.BackgroundProperty, "TE_Background");
            Grid.SetRow(scroll, 0);

            // ── Footer: "Tout réduire" link ────────────────────────────────────
            var footer = new Border
            {
                Padding         = new Thickness(10, 5, 10, 5),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };
            footer.SetResourceReference(Border.BackgroundProperty,  "Panel_ToolbarBrush");
            footer.SetResourceReference(Border.BorderBrushProperty, "Panel_ToolbarBorderBrush");
            Grid.SetRow(footer, 1);

            _collapseAllBtn = new Button
            {
                Content         = "Tout réduire",
                Padding         = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background      = Brushes.Transparent,
                Cursor          = Cursors.Hand,
                FontSize        = 11,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            _collapseAllBtn.SetResourceReference(Button.ForegroundProperty, "CE_Keyword");
            _collapseAllBtn.Click += OnCollapseAllClicked;
            footer.Child = _collapseAllBtn;

            root.Children.Add(scroll);
            root.Children.Add(footer);

            outerBorder.Child = root;
            Child = outerBorder;
        }

        #endregion

        #region Content Population

        private void PopulateContent(IReadOnlyList<ReferenceGroup> groups)
        {
            _groups.Clear();
            _groupsPanel.Children.Clear();
            _allCollapsed = false;
            _collapseAllBtn.Content = "Tout réduire";

            foreach (var group in groups)
                _groupsPanel.Children.Add(BuildGroupPanel(group));
        }

        private UIElement BuildGroupPanel(ReferenceGroup group)
        {
            var container = new StackPanel();

            // ── Split file path into folder portion and bold filename ──────────
            string fileName      = Path.GetFileName(group.FilePath);
            if (string.IsNullOrEmpty(fileName)) fileName = group.DisplayLabel;
            string folderFull    = Path.GetDirectoryName(group.FilePath) ?? string.Empty;
            string folderDisplay = BuildCompactFolderPath(folderFull);

            // ── Group header: left accent border + path + count ───────────────
            var groupHeader = new Border
            {
                Padding         = new Thickness(10, 5, 10, 5),
                BorderThickness = new Thickness(3, 0, 0, 0),  // left accent stripe
                Cursor          = Cursors.Hand
            };
            groupHeader.SetResourceReference(Border.BackgroundProperty,  "Panel_ToolbarBrush");
            groupHeader.SetResourceReference(Border.BorderBrushProperty, "CE_Keyword");

            var headerRow = new StackPanel { Orientation = Orientation.Horizontal };

            var chevron = new TextBlock
            {
                Text              = "▼",
                FontSize          = 9,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 6, 0)
            };
            chevron.SetResourceReference(TextBlock.ForegroundProperty, "TE_Foreground");

            // Path block: dimmed folder + bold filename
            var pathBlock = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip           = group.FilePath,
                TextTrimming      = TextTrimming.CharacterEllipsis,
                MaxWidth          = 580
            };
            if (!string.IsNullOrEmpty(folderDisplay))
            {
                var folderRun = new Run(folderDisplay);
                folderRun.SetResourceReference(Run.ForegroundProperty, "PFP_SubTextBrush");
                pathBlock.Inlines.Add(folderRun);
            }
            var fileRun = new Run(fileName)
            {
                FontWeight = FontWeights.Bold,
                FontSize   = 12
            };
            fileRun.SetResourceReference(Run.ForegroundProperty, "TE_Foreground");
            pathBlock.Inlines.Add(fileRun);

            var countTb = new TextBlock
            {
                Text              = $"  ({group.Items.Count})",
                FontSize          = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            countTb.SetResourceReference(TextBlock.ForegroundProperty, "PFP_SubTextBrush");

            headerRow.Children.Add(chevron);
            headerRow.Children.Add(pathBlock);
            headerRow.Children.Add(countTb);
            groupHeader.Child = headerRow;

            // ── Items panel (collapsible) ─────────────────────────────────────
            var itemsPanel = new StackPanel();
            foreach (var item in group.Items)
                itemsPanel.Children.Add(BuildReferenceRow(group.FilePath, item));

            groupHeader.MouseLeftButtonDown += (_, _) => ToggleGroup(itemsPanel, chevron);
            _groups.Add((itemsPanel, chevron));

            // Subtle separator between groups
            var sep = new Border { Height = 1 };
            sep.SetResourceReference(Border.BackgroundProperty, "Panel_ToolbarBorderBrush");

            container.Children.Add(groupHeader);
            container.Children.Add(itemsPanel);
            container.Children.Add(sep);

            return container;
        }

        private UIElement BuildReferenceRow(string filePath, ReferenceItem item)
        {
            var row = new Border
            {
                Padding = new Thickness(12, 3, 8, 3),
                Cursor  = Cursors.Hand
            };
            row.SetResourceReference(Border.BackgroundProperty, "TE_Background");

            row.MouseEnter += (_, _) => row.SetResourceReference(
                Border.BackgroundProperty, "Panel_ToolbarButtonHoverBrush");
            row.MouseLeave += (_, _) => row.SetResourceReference(
                Border.BackgroundProperty, "TE_Background");

            var rowContent = new DockPanel { LastChildFill = true };

            // ◆ reference glyph (method-purple, Segoe MDL2 style)
            var icon = new TextBlock
            {
                Text              = "◆",
                FontSize          = 9,
                Margin            = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground        = s_glyphBrush
            };
            DockPanel.SetDock(icon, Dock.Left);

            // Line number + colon — right-aligned in a fixed-width block
            var lineNumTb = new TextBlock
            {
                Text              = $"{item.Line + 1} : ",
                FontFamily        = new FontFamily("Consolas"),
                FontSize          = 11,
                MinWidth          = 52,
                TextAlignment     = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 4, 0)
            };
            lineNumTb.SetResourceReference(TextBlock.ForegroundProperty, "PFP_SubTextBrush");
            DockPanel.SetDock(lineNumTb, Dock.Left);

            // Code snippet with symbol occurrence highlighted
            var snippetTb = BuildSnippetTextBlock(item.Snippet, _symbolName);
            snippetTb.FontFamily        = new FontFamily("Consolas");
            snippetTb.FontSize          = 11;
            snippetTb.VerticalAlignment = VerticalAlignment.Center;
            snippetTb.TextTrimming      = TextTrimming.CharacterEllipsis;

            rowContent.Children.Add(icon);
            rowContent.Children.Add(lineNumTb);
            rowContent.Children.Add(snippetTb);
            row.Child = rowContent;

            row.MouseLeftButtonDown += (_, _) =>
                NavigationRequested?.Invoke(this, new ReferencesNavigationEventArgs
                {
                    FilePath = filePath,
                    Line     = item.Line,
                    Column   = item.Column
                });

            return row;
        }

        /// <summary>
        /// Builds a <see cref="TextBlock"/> with the first whole-word occurrence of
        /// <paramref name="symbol"/> rendered in keyword colour with a highlight background.
        /// </summary>
        private static TextBlock BuildSnippetTextBlock(string snippet, string symbol)
        {
            var tb = new TextBlock { TextWrapping = TextWrapping.NoWrap };
            tb.SetResourceReference(TextBlock.ForegroundProperty, "TE_Foreground");

            if (string.IsNullOrEmpty(snippet) || string.IsNullOrEmpty(symbol))
            {
                tb.Text = snippet;
                return tb;
            }

            int idx = snippet.IndexOf(symbol, StringComparison.Ordinal);
            if (idx < 0)
                idx = snippet.IndexOf(symbol, StringComparison.OrdinalIgnoreCase);

            if (idx < 0)
            {
                tb.Text = snippet;
                return tb;
            }

            // Pre-symbol text
            if (idx > 0)
            {
                var pre = new Run(snippet[..idx]);
                pre.SetResourceReference(Run.ForegroundProperty, "TE_Foreground");
                tb.Inlines.Add(pre);
            }

            // Highlighted symbol: keyword colour + semi-transparent blue background
            var match = new Run(snippet.Substring(idx, symbol.Length))
            {
                FontWeight = FontWeights.Bold,
                Background = s_symbolHighlightBg
            };
            match.SetResourceReference(Run.ForegroundProperty, "CE_Keyword");
            tb.Inlines.Add(match);

            // Post-symbol text
            if (idx + symbol.Length < snippet.Length)
            {
                var post = new Run(snippet[(idx + symbol.Length)..]);
                post.SetResourceReference(Run.ForegroundProperty, "TE_Foreground");
                tb.Inlines.Add(post);
            }

            return tb;
        }

        /// <summary>
        /// Returns a compact display string for the folder path.
        /// Shows the last 3 path segments with trailing backslash, or the full path
        /// if it has ≤3 segments.  Returns empty string for null/empty input.
        /// </summary>
        private static string BuildCompactFolderPath(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return string.Empty;

            folder = folder.Replace('/', '\\').TrimEnd('\\');
            var parts = folder.Split('\\', StringSplitOptions.RemoveEmptyEntries);

            return parts.Length == 0
                ? string.Empty
                : parts.Length > 3
                    ? $"…\\{string.Join("\\", parts[^3..])} \\"
                    : folder + "\\";
        }

        #endregion

        #region Collapse / Expand

        private static void ToggleGroup(StackPanel itemsPanel, TextBlock chevron)
        {
            bool isVisible    = itemsPanel.Visibility == Visibility.Visible;
            itemsPanel.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
            chevron.Text          = isVisible ? "▶" : "▼";
        }

        private void OnCollapseAllClicked(object sender, RoutedEventArgs e)
        {
            _allCollapsed = !_allCollapsed;
            foreach (var (panel, chevron) in _groups)
            {
                panel.Visibility = _allCollapsed ? Visibility.Collapsed : Visibility.Visible;
                chevron.Text     = _allCollapsed ? "▶" : "▼";
            }
            _collapseAllBtn.Content = _allCollapsed ? "Tout développer" : "Tout réduire";
        }

        #endregion

        #region Popup Placement

        private CustomPopupPlacement[] CalculatePlacement(
            Size popupSize, Size targetSize, Point offset)
        {
            // Position below cursor, clamped to the right/bottom edges.
            double x = Math.Min(_anchor.X, Math.Max(0, targetSize.Width  - popupSize.Width  - 8));
            double y = _anchor.Y;

            // If popup would overflow downward, show it above the cursor instead.
            if (y + popupSize.Height > targetSize.Height - 8)
                y = Math.Max(0, _anchor.Y - popupSize.Height - 4);

            return new[] { new CustomPopupPlacement(new Point(x, y), PopupPrimaryAxis.Vertical) };
        }

        #endregion

        #region Keyboard

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                IsOpen    = false;
                e.Handled = true;
            }
        }

        #endregion

        #region Helpers

        private static Brush MakeFrozenBrush(Color color)
        {
            var b = new SolidColorBrush(color);
            b.Freeze();
            return b;
        }

        #endregion
    }
}
