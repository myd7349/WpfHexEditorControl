// ==========================================================
// Project: WpfHexEditor.Editor.CodeEditor
// File: Controls/ChangeMarkerGutterControl.cs
// Description:
//     4px gutter strip that renders per-line change indicators (Added / Modified /
//     Deleted) using DrawingContext — zero per-frame allocations.
//     Mirrors the BlameGutterControl pattern: Update() + OnRender() override.
//     Theme brushes resolved lazily via TryFindResource("CE_Gutter*").
// Architecture:
//     FrameworkElement; receives change map from GutterChangeTracker via Update().
//     No dependency on LSP or git; purely visual.
// ==========================================================

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using WpfHexEditor.Editor.CodeEditor.Models;

namespace WpfHexEditor.Editor.CodeEditor.Controls;

/// <summary>
/// A 4px gutter strip drawn to the left of the breakpoint gutter.
/// Shows Added (green), Modified (gold) and Deleted (red) line indicators.
/// </summary>
internal sealed class ChangeMarkerGutterControl : FrameworkElement
{
    // ── Layout constant ───────────────────────────────────────────────────────

    /// <summary>Pixel width of this gutter strip.</summary>
    internal const double GutterWidth = 4.0;

    // ── Frozen fallback brushes (used when theme resources are not yet available) ──

    private static readonly Brush FallbackAdded    = Freeze(new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x94)));
    private static readonly Brush FallbackModified = Freeze(new SolidColorBrush(Color.FromRgb(0xE2, 0xC0, 0x8D)));
    private static readonly Brush FallbackDeleted  = Freeze(new SolidColorBrush(Color.FromRgb(0xC7, 0x4E, 0x39)));

    // ── Layout state (written by Update, read by OnRender) ───────────────────

    private double _lineHeight;
    private int    _firstVisibleLine;
    private int    _lastVisibleLine;
    private double _topMargin;
    private IReadOnlyDictionary<int, double>      _lineYLookup = new Dictionary<int, double>();
    private IReadOnlyDictionary<int, LineChangeKind> _changeMap = new Dictionary<int, LineChangeKind>();

    // ── Constructor ───────────────────────────────────────────────────────────

    internal ChangeMarkerGutterControl()
    {
        Width = GutterWidth;
    }

    // ── Public update API ─────────────────────────────────────────────────────

    /// <summary>
    /// Called from the CodeEditor rendering loop with the latest layout parameters
    /// and change map.  Triggers a repaint.
    /// </summary>
    internal void Update(
        double lineHeight,
        int    firstVisible,
        int    lastVisible,
        double topMargin,
        IReadOnlyDictionary<int, double>         lineYLookup,
        IReadOnlyDictionary<int, LineChangeKind> changeMap)
    {
        _lineHeight       = lineHeight;
        _firstVisibleLine = firstVisible;
        _lastVisibleLine  = lastVisible;
        _topMargin        = topMargin;
        _lineYLookup      = lineYLookup;
        _changeMap        = changeMap;
        InvalidateVisual();
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    protected override void OnRender(DrawingContext dc)
    {
        if (_lineHeight <= 0 || _changeMap.Count == 0) return;

        var added    = ResolveAdded();
        var modified = ResolveModified();
        var deleted  = ResolveDeleted();

        for (int line = _firstVisibleLine; line <= _lastVisibleLine; line++)
        {
            if (!_changeMap.TryGetValue(line, out var kind) || kind == LineChangeKind.None)
                continue;

            if (!_lineYLookup.TryGetValue(line, out double y))
                y = _topMargin + (line - _firstVisibleLine) * _lineHeight;

            var brush = kind switch
            {
                LineChangeKind.Added    => added,
                LineChangeKind.Modified => modified,
                LineChangeKind.Deleted  => deleted,
                _                       => null,
            };

            if (brush is null) continue;

            if (kind == LineChangeKind.Deleted)
            {
                // Deletion hint: small 3px triangle at the bottom edge of the predecessor line.
                DrawDeletedHint(dc, deleted, y + _lineHeight);
            }
            else
            {
                // Full-height bar for Added and Modified.
                dc.DrawRectangle(brush, null, new Rect(0, y, GutterWidth, _lineHeight));
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void DrawDeletedHint(DrawingContext dc, Brush brush, double tipY)
    {
        // Small downward-pointing triangle (3×3 px) at the bottom edge.
        const double hw = 3.0;
        const double hh = 3.0;
        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(new Point(0, tipY - hh), isFilled: true, isClosed: true);
            ctx.LineTo(new Point(hw, tipY),          isStroked: false, isSmoothJoin: false);
            ctx.LineTo(new Point(hw * 2, tipY - hh), isStroked: false, isSmoothJoin: false);
        }
        geom.Freeze();
        dc.DrawGeometry(brush, null, geom);
    }

    // Resolved fresh per render — TryFindResource is a dictionary lookup; 3 calls/frame is negligible.
    // Automatically picks up theme switches without any cache invalidation.
    private Brush ResolveAdded()
        => TryFindResource("CE_GutterAdded")    as Brush ?? FallbackAdded;

    private Brush ResolveModified()
        => TryFindResource("CE_GutterModified") as Brush ?? FallbackModified;

    private Brush ResolveDeleted()
        => TryFindResource("CE_GutterDeleted")  as Brush ?? FallbackDeleted;

    private static Brush Freeze(SolidColorBrush b) { b.Freeze(); return b; }
}
