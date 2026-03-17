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
//     - Popup-derived (StaysOpen=true — explicit dismiss via Escape or
//       CodeEditor.OnMouseDown closing it on any click)
//     - AllowsTransparency=true for drop-shadow effect
//     - Tree rendering delegated to ReferencesTreeBuilder (shared with
//       FindReferencesPanel)
//     - Fires NavigationRequested, RefreshRequested, PinRequested
//     - Group sections are independently collapsible (chevron toggle)
//     - Header row shows symbol kind icon (Segoe MDL2) + symbol name
//     - Popup bottom is bottom-anchored: grows upward from the hint zone
// ==========================================================

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

    /// <summary>Event args for the pin-to-dock request.</summary>
    public sealed class FindAllReferencesDockEventArgs : EventArgs
    {
        public IReadOnlyList<ReferenceGroup> Groups     { get; init; } = Array.Empty<ReferenceGroup>();
        public string                        SymbolName { get; init; } = string.Empty;
    }

    // ── ReferencesPopup control ────────────────────────────────────────────────

    /// <summary>
    /// Floating popup listing "Find All References" results grouped by file.
    /// The popup bottom is bottom-anchored: it grows upward from the CodeLens hint zone.
    /// A header row shows the symbol's kind icon (Segoe MDL2) and name.
    /// </summary>
    internal sealed class ReferencesPopup : Popup
    {
        #region Fields

        private ScrollViewer _scroll        = null!;
        private TextBlock    _collapseLink  = null!;
        private TextBlock    _headerIconTb  = null!;
        private TextBlock    _headerNameTb  = null!;
        private Point        _anchor;
        private string       _symbolName    = string.Empty;
        private double       _lineHeight    = 16.0;
        private bool         _allCollapsed;

        private List<(StackPanel ItemsPanel, TextBlock Chevron)> _groups = new();

        #endregion

        #region Events

        /// <summary>Fired when the user clicks a reference row.</summary>
        public event EventHandler<ReferencesNavigationEventArgs>? NavigationRequested;

        /// <summary>Fired when "Refresh" is clicked — caller should re-run the search.</summary>
        public event EventHandler? RefreshRequested;

        /// <summary>Fired when the pin button is clicked — caller should dock results.</summary>
        public event EventHandler? PinRequested;

        #endregion

        #region Constructor

        internal ReferencesPopup()
        {
            StaysOpen          = true;
            AllowsTransparency = true;
            BuildUI();
            PreviewKeyDown += OnPreviewKeyDown;

            // Close when the application loses focus (user switches to another window).
            if (Application.Current is not null)
                Application.Current.Deactivated += OnApplicationDeactivated;
        }

        private void OnApplicationDeactivated(object? sender, EventArgs e)
            => Dispatcher.BeginInvoke(
                   System.Windows.Threading.DispatcherPriority.Background,
                   new Action(() => IsOpen = false));

        #endregion

        #region Public API

        internal void Show(
            CodeEditor                    owner,
            IReadOnlyList<ReferenceGroup> groups,
            string                        symbolName,
            Point                         anchor,
            double                        lineHeight,
            string                        iconGlyph,
            Brush?                        iconBrush)
        {
            _anchor     = anchor;
            _lineHeight = lineHeight > 0 ? lineHeight : 16.0;
            _symbolName = symbolName ?? string.Empty;
            _allCollapsed = false;

            // Update header with symbol kind icon and name.
            _headerIconTb.Text       = iconGlyph;
            _headerIconTb.Foreground = iconBrush ?? Brushes.Gray;
            _headerNameTb.Text       = _symbolName;

            PlacementTarget              = owner;
            Placement                    = PlacementMode.Custom;
            CustomPopupPlacementCallback = CalculatePlacement;

            PopulateContent(groups);
            IsOpen = true;
        }

        internal new void Close()
        {
            IsOpen = false;
            _groups.Clear();
            if (_scroll.Content is StackPanel old)
                old.Children.Clear();
        }

        internal void Dispose()
        {
            if (Application.Current is not null)
                Application.Current.Deactivated -= OnApplicationDeactivated;
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

            // ── Root grid: header (row 0) + body (row 1) + footer (row 2) ─────
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ── Header: kind icon + symbol name ────────────────────────────────
            var header = new Border
            {
                Padding         = new Thickness(10, 6, 10, 6),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            header.SetResourceReference(Border.BackgroundProperty,  "Panel_ToolbarBrush");
            header.SetResourceReference(Border.BorderBrushProperty, "Panel_ToolbarBorderBrush");

            var headerRow = new StackPanel { Orientation = Orientation.Horizontal };

            _headerIconTb = new TextBlock
            {
                FontFamily        = new FontFamily("Segoe MDL2 Assets"),
                FontSize          = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 6, 0)
            };

            _headerNameTb = new TextBlock
            {
                FontWeight        = FontWeights.SemiBold,
                FontSize          = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            _headerNameTb.SetResourceReference(TextBlock.ForegroundProperty, "TE_Foreground");

            headerRow.Children.Add(_headerIconTb);
            headerRow.Children.Add(_headerNameTb);
            header.Child = headerRow;
            Grid.SetRow(header, 0);

            // ── Scrollable body ────────────────────────────────────────────────
            _scroll = new ScrollViewer
            {
                MaxHeight                     = 400,
                VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            _scroll.SetResourceReference(ScrollViewer.BackgroundProperty, "TE_Background");
            Grid.SetRow(_scroll, 1);

            // ── Footer ────────────────────────────────────────────────────────
            var footer = new Border
            {
                Padding         = new Thickness(10, 5, 10, 5),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };
            footer.SetResourceReference(Border.BackgroundProperty,  "TE_Background");
            footer.SetResourceReference(Border.BorderBrushProperty, "Panel_ToolbarBorderBrush");
            Grid.SetRow(footer, 2);

            // Footer content: links left, pin right
            var footerRow = new DockPanel { LastChildFill = false };

            // Pin button — right-aligned
            var pinBtn = new TextBlock
            {
                Text              = "\uE718",    // Segoe MDL2 — pin (thumbtack)
                FontFamily        = new FontFamily("Segoe MDL2 Assets"),
                FontSize          = 13,
                Background        = Brushes.Transparent,
                Padding           = new Thickness(6, 0, 0, 0),
                Cursor            = Cursors.Hand,
                ToolTip           = "Pin to Find References panel",
                VerticalAlignment = VerticalAlignment.Center
            };
            pinBtn.SetResourceReference(TextBlock.ForegroundProperty, "TE_Foreground");
            pinBtn.MouseEnter += (_, _) => pinBtn.SetResourceReference(
                TextBlock.ForegroundProperty, "CE_Keyword");
            pinBtn.MouseLeave += (_, _) => pinBtn.SetResourceReference(
                TextBlock.ForegroundProperty, "TE_Foreground");
            pinBtn.MouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;    // stop bubbling to CodeEditor.OnMouseDown
                PinRequested?.Invoke(this, EventArgs.Empty);
            };
            DockPanel.SetDock(pinBtn, Dock.Right);

            // "Collapse all" — TextBlock link
            _collapseLink = new TextBlock
            {
                Text              = "Collapse all",
                FontSize          = 11,
                Background        = Brushes.Transparent,
                Cursor            = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            _collapseLink.SetResourceReference(TextBlock.ForegroundProperty, "CE_Keyword");
            _collapseLink.MouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;    // stop bubbling to CodeEditor.OnMouseDown
                OnCollapseAllClicked();
            };

            // "Refresh" — TextBlock link
            var refreshLink = new TextBlock
            {
                Text              = "Refresh",
                FontSize          = 11,
                Background        = Brushes.Transparent,
                Cursor            = Cursors.Hand,
                Margin            = new Thickness(16, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            refreshLink.SetResourceReference(TextBlock.ForegroundProperty, "CE_Keyword");
            refreshLink.MouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;    // stop bubbling to CodeEditor.OnMouseDown
                RefreshRequested?.Invoke(this, EventArgs.Empty);
            };

            footerRow.Children.Add(pinBtn);
            footerRow.Children.Add(_collapseLink);
            footerRow.Children.Add(refreshLink);
            footer.Child = footerRow;

            root.Children.Add(header);
            root.Children.Add(_scroll);
            root.Children.Add(footer);

            outerBorder.Child = root;

            // Swallow all unhandled left-clicks inside the popup so they do NOT bubble up
            // through the PlacementTarget (CodeEditor) logical-parent chain and trigger
            // CodeEditor.OnMouseDown, which unconditionally closes the popup.
            outerBorder.MouseLeftButtonDown += (_, e) => e.Handled = true;

            Child = outerBorder;
        }

        #endregion

        #region Content Population

        private void PopulateContent(IReadOnlyList<ReferenceGroup> groups)
        {
            _allCollapsed         = false;
            _collapseLink.Text    = "Collapse all";

            var panel = ReferencesTreeBuilder.BuildGroupsPanel(
                groups,
                _symbolName,
                e => NavigationRequested?.Invoke(this, e),
                out _groups);

            _scroll.Content = panel;
        }

        #endregion

        #region Collapse / Expand

        private void OnCollapseAllClicked()
        {
            _allCollapsed = !_allCollapsed;
            foreach (var (panel, chevron) in _groups)
            {
                panel.Visibility = _allCollapsed ? Visibility.Collapsed : Visibility.Visible;
                chevron.Text     = _allCollapsed ? "▶" : "▼";
            }
            _collapseLink.Text = _allCollapsed ? "Expand all" : "Collapse all";
        }

        #endregion

        #region Popup Placement

        private CustomPopupPlacement[] CalculatePlacement(
            Size popupSize, Size targetSize, Point offset)
        {
            double x = Math.Min(_anchor.X, Math.Max(0, targetSize.Width - popupSize.Width - 8));

            // _anchor.Y is the desired BOTTOM edge of the popup; derive the top-left Y.
            double y = _anchor.Y - popupSize.Height;

            // If the popup would clip above the editor top, fall back to opening below the hint.
            if (y < 8)
                y = _anchor.Y + _lineHeight * 3;

            // Clamp so the popup never overflows below the editor bottom.
            y = Math.Min(y, targetSize.Height - popupSize.Height - 8);
            y = Math.Max(y, 0);

            return [new CustomPopupPlacement(new Point(x, y), PopupPrimaryAxis.Vertical)];
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
    }
}
