// ==========================================================
// Project: WpfHexEditor.Editor.CodeEditor
// File: Controls/CodeEditor.Viewport.cs
// Description: Virtual scrolling, viewport calculations, and navigation for CodeEditor.
// Architecture notes: Partial class — see CodeEditor.cs for fields and class declaration.
// ==========================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexEditor.Editor.CodeEditor.Models;
using WpfHexEditor.Editor.CodeEditor.Services;
using WpfHexEditor.Editor.Core;
using WpfHexEditor.Editor.Core.Documents;
using WpfHexEditor.Editor.CodeEditor.Helpers;
using WpfHexEditor.Editor.CodeEditor.Options;

namespace WpfHexEditor.Editor.CodeEditor.Controls
{
    public partial class CodeEditor
    {
        #region Virtual Scrolling (Phase 11)

        /// <summary>
        /// Initialize virtual scrolling engine
        /// </summary>
        private void InitializeVirtualScrolling()
        {
            _virtualizationEngine = new VirtualizationEngine
            {
                TotalLines = _document?.Lines.Count ?? 0,
                ViewportHeight = ActualHeight,
                LineHeight = _lineHeight,
                ScrollOffset = 0,
                RenderBuffer = RenderBuffer
            };

            // Subscribe to size changed for viewport updates
            SizeChanged += CodeEditor_SizeChanged;
        }

        private void CodeEditor_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_virtualizationEngine != null)
            {
                bool hasHBar = _hScrollBar?.Visibility == Visibility.Visible;
                // Refresh TotalLines in case this editor shares a document that was populated
                // after SetDocument() was called (e.g. secondary pane in split view where
                // OpenAsync runs after Loaded and only updates the primary editor).
                _virtualizationEngine.TotalLines = _document?.Lines.Count ?? 0;
                _virtualizationEngine.ViewportHeight = ActualHeight - (hasHBar ? ScrollBarThickness : 0);
                _virtualizationEngine.CalculateVisibleRange();
            }
            // Word wrap: rebuild map when size changes (viewport width affects wrap width).
            if (IsWordWrapEnabled) RebuildWrapMap();
            // Scrollbar ranges depend on viewport size — trigger layout pass.
            // InvalidateVisual() is required here: InvalidateArrange() alone does NOT call
            // OnRender, so after the first real SizeChanged (ActualHeight goes from 0 to N),
            // the visible-line range is recalculated but the canvas stays blank until the
            // next user interaction forces a repaint. (ADR-002)
            InvalidateArrange();
            InvalidateVisual();
        }

        /// <summary>
        /// Update virtualization engine when document changes
        /// </summary>
        private void UpdateVirtualization()
        {
            if (_virtualizationEngine == null || _document == null)
                return;

            _virtualizationEngine.TotalLines = _document.Lines.Count;
            _virtualizationEngine.LineHeight = _lineHeight;
            _virtualizationEngine.RenderBuffer = RenderBuffer;
            _virtualizationEngine.CalculateVisibleRange();

            // Word wrap: rebuild map after document content changes.
            if (IsWordWrapEnabled) RebuildWrapMap();
        }

        /// <summary>
        /// Scroll viewport vertically by pixel amount
        /// </summary>
        public void ScrollVertical(double delta)
        {
            if (_virtualizationEngine == null || !EnableVirtualScrolling)
                return;

            // delta is already speed * _lineHeight from the caller (MouseWheelSpeed controls line count).
            // Clamp against the scrollbar maximum (which includes VS-style padding)
            // instead of the VE's own TotalHeight (which does not).
            double maxV = _vScrollBar?.Maximum ?? double.MaxValue;
            double newOffset = Math.Max(0, Math.Min(_verticalScrollOffset + delta, maxV));

            if (SmoothScrolling)
            {
                // Smooth scrolling - animate to target
                _targetScrollOffset = newOffset;

                // Initialize current offset if first scroll
                if (_currentScrollOffset == 0 && _verticalScrollOffset == 0)
                    _currentScrollOffset = _verticalScrollOffset;

                // Start animation timer
                if (!_smoothScrollTimer.IsEnabled)
                    _smoothScrollTimer.Start();
            }
            else
            {
                // Instant scrolling - jump directly
                _verticalScrollOffset = newOffset;
                _currentScrollOffset = newOffset;
                _targetScrollOffset = newOffset;
                _virtualizationEngine.ScrollOffset = newOffset;
                _virtualizationEngine.CalculateVisibleRange();
                SyncVScrollBar();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Scrolls the viewport so the given 0-based line is at the top.
        /// Does NOT move the caret or clear selection.
        /// Used by the minimap for scroll-only interaction.
        /// </summary>
        public void ScrollViewToLine(int lineIndex)
        {
            if (_virtualizationEngine == null) return;
            var newOffset = _virtualizationEngine.ScrollToLine(lineIndex);
            _verticalScrollOffset = newOffset;
            _currentScrollOffset = newOffset;
            _targetScrollOffset = newOffset;
            _virtualizationEngine.ScrollOffset = newOffset;
            _virtualizationEngine.CalculateVisibleRange();
            SyncVScrollBar();
            InvalidateVisual();
            MinimapRefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Maximum scroll offset in pixels. Matches <c>_vScrollBar.Maximum</c> which
        /// accounts for TopMargin, folded lines, and inline hints.
        /// Used by the minimap to match the scrollbar range exactly.
        /// </summary>
        public double MaxScrollOffset
            => _vScrollBar?.Maximum
               ?? Math.Max(0, _virtualizationEngine?.TotalHeight - _virtualizationEngine?.ViewportHeight ?? 0);

        /// <summary>
        /// Scrolls the viewport to an exact pixel offset.
        /// Does NOT move the caret. No line-boundary quantization.
        /// Used by the minimap for sub-line-precision scrolling.
        /// </summary>
        public void ScrollViewToOffset(double pixelOffset)
        {
            if (_virtualizationEngine == null) return;
            double maxOffset = MaxScrollOffset;
            var newOffset = Math.Clamp(pixelOffset, 0, maxOffset);
            _verticalScrollOffset = newOffset;
            _currentScrollOffset = newOffset;
            _targetScrollOffset = newOffset;
            _virtualizationEngine.ScrollOffset = newOffset;
            _virtualizationEngine.CalculateVisibleRange();
            SyncVScrollBar();
            InvalidateVisual();
            MinimapRefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Scroll viewport horizontally by pixel amount (used by pan mode and Shift+Wheel).
        /// </summary>
        private void ScrollHorizontal(double delta)
        {
            if (_hScrollBar == null || IsWordWrapEnabled) return;

            double maxH = Math.Max(0, _maxContentWidth - (ActualWidth - TextAreaLeftOffset));
            _horizontalScrollOffset = Math.Max(0, Math.Min(_horizontalScrollOffset + delta, maxH));
            SyncHScrollBar();
            InvalidateVisual();
        }

        /// <summary>
        /// Ensure cursor line is visible in viewport
        /// </summary>
        private void EnsureCursorVisible()
        {
            // Horizontal scroll must always run, regardless of virtual scrolling mode.
            EnsureCursorColumnVisible();

            // Word wrap: scroll to the visual row of the caret rather than the logical line.
            if (IsWordWrapEnabled && _wrapOffsets.Length > _cursorLine && _charsPerVisualLine > 0)
            {
                int caretVisRow = _wrapOffsets[_cursorLine] + _cursorColumn / _charsPerVisualLine;
                double caretY   = caretVisRow * _lineHeight;
                bool hasHBar    = _hScrollBar?.Visibility == Visibility.Visible;
                double viewportH = ActualHeight - TopMargin - (hasHBar ? ScrollBarThickness : 0);
                if (caretY < _verticalScrollOffset)
                {
                    _verticalScrollOffset = Math.Max(0, caretY);
                    SyncVScrollBar();
                    InvalidateVisual();
                }
                else if (caretY + _lineHeight > _verticalScrollOffset + viewportH)
                {
                    _verticalScrollOffset = caretY + _lineHeight - viewportH;
                    SyncVScrollBar();
                    InvalidateVisual();
                }
                NotifyCaretMovedIfChanged();
                return;
            }

            if (_virtualizationEngine == null || !EnableVirtualScrolling)
            {
                NotifyCaretMovedIfChanged();
                return;
            }

            // BUG3-FIX: The VirtualizationEngine maps scroll offsets to lines via
            // (offset / lineHeight), treating every line as equidistant.  When folding
            // is active the scroll space is compressed: hidden lines are removed from the
            // pixel budget, so the visible-pixel position of physical line N is
            // (N - hiddenLinesBefore(N)) * lineHeight, NOT (N * lineHeight).
            // Passing the raw _cursorLine index to VE.EnsureLineVisible() therefore
            // produces an offset that is too large by (hiddenBefore * lineHeight), which
            // either freezes the viewport or scrolls past the actual cursor position.
            //
            // Fix: count visible lines before _cursorLine to compute the correct pixel Y,
            // then use that pixel directly for the viewport boundary check.
            double caretPixelY = ComputeVisiblePixelY(_cursorLine);
            bool hasHBarEcv = _hScrollBar?.Visibility == Visibility.Visible;
            double viewportHEcv = ActualHeight - TopMargin - (hasHBarEcv ? ScrollBarThickness : 0);

            double newOffset;
            if (caretPixelY < _verticalScrollOffset)
                newOffset = Math.Max(0, caretPixelY);
            else if (caretPixelY + _lineHeight > _verticalScrollOffset + viewportHEcv)
                newOffset = caretPixelY + _lineHeight - viewportHEcv;
            else
                newOffset = _verticalScrollOffset; // already visible — no scroll needed

            double maxV = _vScrollBar?.Maximum ?? double.MaxValue;
            newOffset = Math.Clamp(newOffset, 0, maxV);

            if (Math.Abs(newOffset - _verticalScrollOffset) > 0.1)
            {
                _verticalScrollOffset = newOffset;
                _virtualizationEngine.ScrollOffset = newOffset;
                _virtualizationEngine.CalculateVisibleRange();
                SyncVScrollBar();
                InvalidateVisual();
            }

            NotifyCaretMovedIfChanged();
        }

        /// <summary>
        /// Returns the pixel Y position (from document top, in scroll-offset space) of
        /// <paramref name="physicalLine"/>, accounting for hidden (folded) lines.
        /// Each visible line contributes exactly <c>_lineHeight</c> pixels; hidden lines
        /// contribute nothing, matching the compressed scroll space used by the scrollbar.
        /// O(physicalLine) — only called from <see cref="EnsureCursorVisible"/>.
        /// </summary>
        private double ComputeVisiblePixelY(int physicalLine)
        {
            if (_foldingEngine == null || _foldingEngine.TotalHiddenLineCount == 0)
                return physicalLine * _lineHeight;

            int visibleBefore = 0;
            for (int i = 0; i < physicalLine && i < (_document?.Lines.Count ?? 0); i++)
            {
                if (!_foldingEngine.IsLineHidden(i))
                    visibleBefore++;
            }
            return visibleBefore * _lineHeight;
        }

        /// <summary>
        /// Converts a physical (document) line index to its visible (fold-compressed) line index.
        /// Hidden lines are skipped; the result is the 0-based rank of <paramref name="physicalLine"/>
        /// among non-hidden lines.  Returns the same value as the input when no folding is active.
        /// Used to align scroll-marker panel positions with the compressed scrollbar space.
        /// O(physicalLine).
        /// </summary>
        internal int PhysicalToVisibleLineIndex(int physicalLine)
        {
            if (_foldingEngine == null || _foldingEngine.TotalHiddenLineCount == 0)
                return physicalLine;

            int vis = 0;
            int count = Math.Min(physicalLine, _document?.Lines.Count - 1 ?? 0);
            for (int i = 0; i < count; i++)
            {
                if (!_foldingEngine.IsLineHidden(i))
                    vis++;
            }
            return vis;
        }

        /// <summary>
        /// Total number of visible (non-hidden) lines in the current document.
        /// Matches the scroll space denominator used by the scrollbar.
        /// </summary>
        internal int VisibleLineCount
            => Math.Max(1, (_document?.Lines.Count ?? 1) - (_foldingEngine?.TotalHiddenLineCount ?? 0));

        /// <summary>
        /// Returns the true Y pixel position of a line from the document top,
        /// accounting for InlineHints extra height. O(1) via <see cref="_hintsCumulative"/>.
        /// Does not account for folded lines (fold adjustments happen in CalculateVisibleLines).
        /// </summary>
        private double GetTrueLineY(int lineIndex)
        {
            int hintsAbove = (ShowInlineHints && lineIndex < _hintsCumulative.Length)
                ? _hintsCumulative[lineIndex]
                : 0;
            return TopMargin + lineIndex * _lineHeight + hintsAbove * HintLineHeight;
        }

        /// <summary>
        /// Inverse of <see cref="GetTrueLineY"/>: converts a scroll pixel offset
        /// to a line index using binary search on the cumulative hint array. O(log n).
        /// </summary>
        private int ScrollOffsetToLine(double offset)
        {
            int lineCount = _document?.Lines.Count ?? 0;
            if (lineCount == 0) return 0;
            int lo = 0, hi = lineCount - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (GetTrueLineY(mid) - TopMargin <= offset)
                    lo = mid;
                else
                    hi = mid - 1;
            }
            return lo;
        }

        /// <summary>
        /// Fires <see cref="CaretMoved"/> when the caret has moved to a different line.
        /// Call after every operation that may change <c>_cursorLine</c>.
        /// </summary>
        private void NotifyCaretMovedIfChanged()
        {
            if (_cursorLine != _lastNotifiedCursorLine)
            {
                _lastNotifiedCursorLine = _cursorLine;
                CaretMoved?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Scrolls to <paramref name="line"/> and places the caret at column 0.
        /// Used by the navigation bar ComboBox selection.
        /// </summary>
        public void NavigateToLine(int line)
        {
            if (_document == null || line < 0 || line >= _document.Lines.Count) return;
            _cursorLine   = line;
            _cursorColumn = 0;
            _selection.Clear();
            EnsureCursorVisible();
            InvalidateVisual();
        }

        // ── SDK service surface ───────────────────────────────────────────────
        // Thin read-only accessors so CodeEditorServiceImpl does not reach into private fields.

        /// <summary>Language identifier of the loaded document (e.g. "csharp"), or null.</summary>
        public string? CurrentLanguage => Language?.Id;

        /// <summary>Absolute path of the currently open file, or null.</summary>
        public string? CurrentFilePath => _currentFilePath;

        /// <summary>Current caret line (1-based).</summary>
        public int CaretLine   => _cursorLine + 1;

        /// <summary>Current caret column (1-based).</summary>
        public int CaretColumn => _cursorColumn + 1;

        /// <summary>Full document text.</summary>
        public string? GetContent() => _document is not null ? GetText() : null;

        /// <summary>Currently selected text, or empty string.</summary>
        public string GetSelectedText() =>
            _document is not null && !_selection.IsEmpty
                ? _document.GetText(_selection.NormalizedStart, _selection.NormalizedEnd)
                : string.Empty;

        /// <summary>
        /// Public 1-based overload — mirrors <see cref="INavigableDocument.NavigateTo"/> convention.
        /// Intended for SDK consumers (e.g. StringExtraction panel).
        /// </summary>
        public void NavigateToLine(int line, int column) =>
            ((INavigableDocument)this).NavigateTo(line, column);

        // ── External line highlights ──────────────────────────────────────────

        /// <summary>Add a background highlight on a 1-based line, grouped by tag for bulk removal.</summary>
        public void AddLineHighlight(int line, SolidColorBrush color, string description, string tag, double opacity = 0.35)
        {
            if (line < 1) return;
            _lineHighlights.Add(new LineHighlightEntry(line - 1, color, opacity, description, tag));
            InvalidateVisual();
        }

        /// <summary>Remove all highlights whose tag equals <paramref name="tag"/>.</summary>
        public void ClearLineHighlightsByTag(string tag)
        {
            int removed = _lineHighlights.RemoveAll(h => h.Tag == tag);
            if (removed > 0) InvalidateVisual();
        }

        /// <summary>
        /// <see cref="INavigableDocument"/> implementation.
        /// Accepts 1-based line/column (IDE convention) and converts to 0-based internal coords.
        /// Used by the host when navigating from the References popup or the Error List.
        /// </summary>
        void INavigableDocument.NavigateTo(int line, int column)
        {
            int zeroLine = Math.Max(0, line - 1);
            int zeroCol  = Math.Max(0, column - 1);
            if (_document == null || zeroLine >= _document.Lines.Count) return;

            _cursorLine   = zeroLine;
            _cursorColumn = Math.Min(zeroCol, _document.Lines[zeroLine].Length);
            _selection.Clear();
            EnsureCursorVisible();
            InvalidateVisual();
        }

        #endregion
    }
}
