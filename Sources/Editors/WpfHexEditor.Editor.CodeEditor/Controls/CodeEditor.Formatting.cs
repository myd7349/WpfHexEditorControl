// ==========================================================
// Project: WpfHexEditor.Editor.CodeEditor
// File: Controls/CodeEditor.Formatting.cs
// Description: Code formatting public API (FormatDocumentAsync, FormatSelectionAsync) and sticky scroll settings for CodeEditor.
// Architecture notes: Partial class — see CodeEditor.cs for fields and class declaration.
// ==========================================================

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WpfHexEditor.Editor.CodeEditor.Options;
using WpfHexEditor.Editor.CodeEditor.Properties;
using WpfHexEditor.Editor.CodeEditor.Services;
using WpfHexEditor.Core.ProjectSystem.Languages;

namespace WpfHexEditor.Editor.CodeEditor.Controls
{
    public partial class CodeEditor
    {
        // ── Code Formatting public API (#159) ─────────────────────────────────────

        /// <summary>
        /// Formats the full document (Ctrl+K, Ctrl+D).
        /// LSP textDocument/formatting is tried first; falls back to StructuralFormatter.
        /// The change is applied as a single undoable transaction.
        /// </summary>
        public async System.Threading.Tasks.Task FormatDocumentAsync(
            System.Threading.CancellationToken ct = default)
        {
            if (IsReadOnly || _document is null) return;

            string original = GetText();

            // Build merged rules: whfmt base → user overrides
            var baseRules   = Language?.FormattingRules ?? new FormattingRules();
            var mergedRules = baseRules.WithOverrides(_codeEditorOptions?.BuildOverrides());
            bool insertSpaces = !mergedRules.UseTabs;
            int  tabSize      = mergedRules.IndentSize;

            string formatted = await _codeFormattingService
                .FormatDocumentAsync(
                    _currentFilePath ?? string.Empty,
                    original,
                    mergedRules,
                    _lspClient,
                    tabSize,
                    insertSpaces,
                    ct)
                .ConfigureAwait(true); // resume on UI thread

            if (formatted == original) return;

            using (_undoEngine.BeginTransaction(CodeEditorResources.CodeEditor_FormatDocumentTransaction))
            {
                SelectAll();
                DeleteSelection();
                _document.InsertText(new Models.TextPosition(0, 0), formatted);
            }

            _selection.Clear();
            _cursorLine   = 0;
            _cursorColumn = 0;
            EnsureCursorVisible();

            // Formatting is a bulk replace — skip the 500 ms debounce and re-analyse
            // folding immediately so toggle arrows and fold lines reflect the new structure.
            if (IsFoldingEnabled && _foldingEngine != null)
            {
                _foldingDebounceTimer?.Stop();
                _foldingEngine.Analyze(_document.Lines);
            }

            InvalidateMeasure();
            InvalidateVisual();
        }

        /// <summary>
        /// Formats only the current selection (Ctrl+K, Ctrl+F).
        /// Falls back to FormatDocumentAsync when there is no active selection.
        /// </summary>
        public async System.Threading.Tasks.Task FormatSelectionAsync(
            System.Threading.CancellationToken ct = default)
        {
            if (IsReadOnly || _document is null) return;

            if (_selection.IsEmpty)
            {
                await FormatDocumentAsync(ct).ConfigureAwait(true);
                return;
            }

            string original    = GetText();
            var    baseRules   = Language?.FormattingRules ?? new FormattingRules();
            var    mergedRules = baseRules.WithOverrides(_codeEditorOptions?.BuildOverrides());
            bool   insertSpaces = !mergedRules.UseTabs;
            int    tabSize     = mergedRules.IndentSize;
            var    start       = _selection.NormalizedStart;
            var    end         = _selection.NormalizedEnd;

            string formatted = await _codeFormattingService
                .FormatSelectionAsync(
                    _currentFilePath ?? string.Empty,
                    original,
                    start.Line, start.Column,
                    end.Line,   end.Column,
                    mergedRules,
                    _lspClient,
                    tabSize,
                    insertSpaces,
                    ct)
                .ConfigureAwait(true); // resume on UI thread

            if (formatted == original) return;

            using (_undoEngine.BeginTransaction(CodeEditorResources.CodeEditor_FormatSelectionTransaction))
            {
                // Replace full text; the service has already scoped the change.
                SelectAll();
                DeleteSelection();
                _document.InsertText(new Models.TextPosition(0, 0), formatted);
            }

            _selection.Clear();

            // Formatting is a bulk replace — skip the 500 ms debounce and re-analyse
            // folding immediately so toggle arrows and fold lines reflect the new structure.
            if (IsFoldingEnabled && _foldingEngine != null)
            {
                _foldingDebounceTimer?.Stop();
                _foldingEngine.Analyze(_document.Lines);
            }

            InvalidateMeasure();
            InvalidateVisual();
        }

        // ── Sticky Scroll public API ───────────────────────────────────────────

        /// <summary>
        /// Applies sticky-scroll settings from the host (MainWindow/options page).
        /// </summary>
        public void ApplyStickyScrollSettings(
            bool enabled, int maxLines, bool syntaxHighlight,
            bool clickToNavigate, double opacity, int minScopeLines)
        {
            _stickyScrollEnabled         = enabled;
            _stickyScrollMaxLines        = Math.Clamp(maxLines, 1, 10);
            _stickyScrollSyntaxHighlight = syntaxHighlight;
            _stickyScrollClickToNavigate = clickToNavigate;
            _stickyScrollOpacity         = Math.Clamp(opacity, 0.5, 1.0);
            _stickyScrollMinScopeLines   = Math.Clamp(minScopeLines, 2, 20);

            if (_stickyScrollHeader != null)
            {
                _stickyScrollHeader.Opacity = _stickyScrollOpacity;
                UpdateStickyScrollHeader();
                InvalidateMeasure();
            }
        }

        private void OnStickyScrollScopeClicked(object? sender, int startLine)
            => NavigateToLine(startLine);

    }
}
