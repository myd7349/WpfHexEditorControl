// ==========================================================
// Project: WpfHexEditor.Editor.CodeEditor
// File: Controls/CodeEditor.Properties.cs
// Description: Dependency Properties, property-changed callbacks, and syntax color resolution for CodeEditor.
// Architecture notes: Partial class — see CodeEditor.cs for fields and class declaration.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexEditor.Editor.CodeEditor.Folding;
using WpfHexEditor.Editor.CodeEditor.Models;
using WpfHexEditor.Editor.CodeEditor.Helpers;
using WpfHexEditor.Editor.CodeEditor.Rendering;
using WpfHexEditor.Editor.CodeEditor.Services;
using WpfHexEditor.Editor.CodeEditor.Snippets;
using WpfHexEditor.Editor.CodeEditor.NavigationBar;
using WpfHexEditor.Core;
using WpfHexEditor.Core.Settings;
using WpfHexEditor.Editor.CodeEditor.Properties;
using WpfHexEditor.Editor.Core;
using WpfHexEditor.Editor.Core.Helpers;
using WpfHexEditor.Editor.Core.Documents;
using WpfHexEditor.Editor.Core.LSP;
using WpfHexEditor.Editor.CodeEditor.Options;
using WpfHexEditor.Core.ProjectSystem.Languages;
using WpfHexEditor.Editor.CodeEditor.Selection;
using WpfHexEditor.Editor.CodeEditor.Input;
using WpfHexEditor.Editor.CodeEditor.MultiCaret;
using WpfHexEditor.Editor.Core.Dialogs;

namespace WpfHexEditor.Editor.CodeEditor.Controls
{
    public partial class CodeEditor
    {
        #region Dependency Properties with [Category] Attributes

        // Properties are organized by category for auto-generated settings panel
        // Uses same pattern as HexEditor with DynamicSettingsGenerator

        public static readonly DependencyProperty ShowLineNumbersProperty =
            DependencyProperty.Register(nameof(ShowLineNumbers), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Show or hide line numbers in the left gutter
        /// </summary>
        [Category("Appearance")]
        [DisplayName("Show Line Numbers")]
        [Description("Display line numbers in the left gutter")]
        public bool ShowLineNumbers
        {
            get => (bool)GetValue(ShowLineNumbersProperty);
            set => SetValue(ShowLineNumbersProperty, value);
        }

        public static readonly DependencyProperty IsWordWrapEnabledProperty =
            DependencyProperty.Register(nameof(IsWordWrapEnabled), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(false, OnIsWordWrapEnabledChanged));

        private static void OnIsWordWrapEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor ed) return;
            ed._horizontalScrollOffset = 0;
            ed.RebuildWrapMap();
            ed.InvalidateMeasure();
            ed.InvalidateVisual();
        }

        /// <summary>
        /// When true, lines wrap visually at the viewport edge instead of scrolling horizontally.
        /// Disables the horizontal scrollbar. (ADR-049)
        /// </summary>
        [Category("Features")]
        [DisplayName("Word Wrap")]
        [Description("Wrap long lines at the viewport edge instead of scrolling horizontally")]
        public bool IsWordWrapEnabled
        {
            get => (bool)GetValue(IsWordWrapEnabledProperty);
            set => SetValue(IsWordWrapEnabledProperty, value);
        }

        public static readonly DependencyProperty IsFoldingEnabledProperty =
            DependencyProperty.Register(nameof(IsFoldingEnabled), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, OnIsFoldingEnabledChanged));

        /// <summary>
        /// Enable or disable the code-folding gutter ([+]/[-] markers).
        /// When disabled the GutterControl is hidden and FoldingEngine analysis is skipped.
        /// </summary>
        [Category("Features")]
        [DisplayName("Enable Folding")]
        [Description("Show code-folding markers in the left gutter (brace pairs / indentation blocks)")]
        public bool IsFoldingEnabled
        {
            get => (bool)GetValue(IsFoldingEnabledProperty);
            set => SetValue(IsFoldingEnabledProperty, value);
        }

        private static void OnIsFoldingEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor editor) return;
            if (editor._gutterControl != null)
                editor._gutterControl.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            if ((bool)e.NewValue && editor._document != null)
                editor._foldingEngine?.Analyze(editor._document.Lines);
            editor.InvalidateMeasure();
        }

        public static readonly DependencyProperty FoldToggleOnDoubleClickProperty =
            DependencyProperty.Register(nameof(FoldToggleOnDoubleClick), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, OnFoldToggleOnDoubleClickChanged));

        /// <summary>
        /// When true, the inline collapsed block label ({…} / "Constructor") requires a
        /// double-click to expand. The gutter toggle triangle is unaffected (always single-click).
        /// </summary>
        [Category("Features")]
        [DisplayName("Fold Open on Double-Click")]
        [Description("When enabled, the inline collapsed block label requires a double-click to expand. The gutter triangle is unaffected.")]
        public bool FoldToggleOnDoubleClick
        {
            get => (bool)GetValue(FoldToggleOnDoubleClickProperty);
            set => SetValue(FoldToggleOnDoubleClickProperty, value);
        }

        private static void OnFoldToggleOnDoubleClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Inline label behavior only — gutter triangle is unaffected.
        }

        public static readonly DependencyProperty ShowScopeGuidesProperty =
            DependencyProperty.Register(nameof(ShowScopeGuides), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Show or hide the vertical scope guide lines that connect matching brace pairs.
        /// Toggled via the Code Editor options panel.
        /// </summary>
        public bool ShowScopeGuides
        {
            get => (bool)GetValue(ShowScopeGuidesProperty);
            set => SetValue(ShowScopeGuidesProperty, value);
        }

        public static readonly DependencyProperty EnableWordHighlightProperty =
            DependencyProperty.Register(nameof(EnableWordHighlight), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, OnEnableWordHighlightChanged));

        /// <summary>
        /// When true, places a subtle highlight box on every occurrence of the word under the
        /// caret and shows proportional tick marks on the vertical scrollbar.
        /// </summary>
        public bool EnableWordHighlight
        {
            get => (bool)GetValue(EnableWordHighlightProperty);
            set => SetValue(EnableWordHighlightProperty, value);
        }

        private static void OnEnableWordHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ce = (CodeEditor)d;
            if (!(bool)e.NewValue)
            {
                ce._wordHighlights.Clear();
                ce._wordHighlightLines.Clear();
                ce._wordHighlightLineSet.Clear();
                ce._codeScrollMarkerPanel?.ClearWordMarkers();
                ce.InvalidateVisual();
            }
            else
            {
                ce.ScheduleWordHighlightUpdate();
            }
        }

        /// <summary>
        /// Injectable <see cref="Snippets.SnippetManager"/> that provides language-specific snippets.
        /// When set, pressing Tab after a matching trigger word expands the snippet instead of
        /// inserting whitespace. Pass <c>null</c> to disable snippet expansion.
        /// </summary>
        public static readonly DependencyProperty SnippetManagerProperty =
            DependencyProperty.Register(nameof(SnippetManager), typeof(SnippetManager),
                typeof(CodeEditor), new System.Windows.PropertyMetadata(null));

        [Category("Features")]
        [DisplayName("Snippet Manager")]
        [Description("Provides Tab-triggered snippet expansion for the current language.")]
        public SnippetManager? SnippetManager
        {
            get => (SnippetManager?)GetValue(SnippetManagerProperty);
            set => SetValue(SnippetManagerProperty, value);
        }

        public static readonly DependencyProperty EnableSmartCompleteProperty =
            DependencyProperty.Register(nameof(EnableSmartComplete), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Enable SmartComplete context-aware autocomplete
        /// </summary>
        [Category("Features")]
        [DisplayName("Enable SmartComplete")]
        [Description("Enable context-aware autocomplete suggestions (Ctrl+Space to trigger manually)")]
        public bool EnableSmartComplete
        {
            get => (bool)GetValue(EnableSmartCompleteProperty);
            set
            {
                SetValue(EnableSmartCompleteProperty, value);
                _enableSmartComplete = value;
            }
        }

        public static readonly DependencyProperty ClickableLinksEnabledProperty =
            DependencyProperty.Register(nameof(ClickableLinksEnabled), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// When <see langword="true"/>, HTTP/HTTPS URLs are detected and Ctrl+Click opens them in the default browser.
        /// </summary>
        [Category("Features")]
        [DisplayName("Clickable Links")]
        [Description("Ctrl+Click on http(s):// URLs opens them in the default browser.")]
        public bool ClickableLinksEnabled
        {
            get => (bool)GetValue(ClickableLinksEnabledProperty);
            set => SetValue(ClickableLinksEnabledProperty, value);
        }

        public static readonly DependencyProperty ClickableEmailsEnabledProperty =
            DependencyProperty.Register(nameof(ClickableEmailsEnabled), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// When <see langword="true"/>, email addresses are detected and Ctrl+Click opens the default mail client.
        /// </summary>
        [Category("Features")]
        [DisplayName("Clickable Emails")]
        [Description("Ctrl+Click on email addresses opens the default mail client (mailto:).")]
        public bool ClickableEmailsEnabled
        {
            get => (bool)GetValue(ClickableEmailsEnabledProperty);
            set => SetValue(ClickableEmailsEnabledProperty, value);
        }

        public static readonly DependencyProperty EnableFindAllReferencesProperty =
            DependencyProperty.Register(nameof(EnableFindAllReferences), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// When false, the Find All References command (Shift+F12) and its
        /// context-menu item are disabled regardless of LSP availability.
        /// </summary>
        [Category("Features")]
        [DisplayName("Enable Find All References")]
        [Description("Enables the Find All References command (Shift+F12) via the language server.")]
        public bool EnableFindAllReferences
        {
            get => (bool)GetValue(EnableFindAllReferencesProperty);
            set => SetValue(EnableFindAllReferencesProperty, value);
        }

        // ── Blame Gutter ──────────────────────────────────────────────────────────

        public static readonly DependencyProperty ShowBlameGutterProperty =
            DependencyProperty.Register(nameof(ShowBlameGutter), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(false, OnShowBlameGutterChanged));

        [Category("Features")]
        [DisplayName("Show Blame Gutter")]
        [Description("Shows a 6-pixel color bar encoding commit age for each line.")]
        public bool ShowBlameGutter
        {
            get => (bool)GetValue(ShowBlameGutterProperty);
            set => SetValue(ShowBlameGutterProperty, value);
        }

        private static void OnShowBlameGutterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor ce || ce._blameGutterControl is null) return;
            ce._blameGutterControl.Visibility = (bool)e.NewValue
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
            ce.InvalidateMeasure();
        }

        /// <summary>
        /// Injects blame data from the Git plugin into the blame gutter.
        /// Called by the App layer after <c>GitBlameLoadedEvent</c> arrives.
        /// </summary>
        public void SetBlame(IReadOnlyList<WpfHexEditor.Editor.Core.LSP.BlameEntry>? entries)
            => _blameGutterControl?.SetBlame(entries);

        // ── Inline Peek Definition (#158) ────────────────────────────────────

        /// <summary>Closes the inline peek panel and restores normal line layout.</summary>
        internal void CloseInlinePeek()
        {
            if (_inlinePeekHost != null)
                _scrollBarChildren.Remove(_inlinePeekHost);
            _inlinePeekHost = null;
            _peekHostLine   = -1;
            _peekHostHeight = 0.0;
            _linePositionsDirty = true;
            InvalidateMeasure();
            InvalidateVisual();
        }

        /// <summary>
        /// Scrolls the editor vertically so the peek panel is fully visible
        /// when it would otherwise extend below the viewport.
        /// </summary>
        internal void EnsurePeekVisible(int anchorLine)
        {
            if (_lineHeight <= 0) return;
            if (!_lineYLookup.TryGetValue(anchorLine, out double anchorY))
                anchorY = TopMargin + (anchorLine - _firstVisibleLine) * _lineHeight;

            double panelTop    = anchorY + _lineHeight;
            double panelBottom = panelTop + _peekHostHeight;
            double viewBottom  = ActualHeight - (_hScrollBar?.ActualHeight ?? 0);

            if (panelBottom > viewBottom)
            {
                double needed = panelBottom - viewBottom;
                _verticalScrollOffset = Math.Min(
                    _verticalScrollOffset + needed,
                    Math.Max(0, (_document?.Lines.Count ?? 0) * _lineHeight - ActualHeight));
                _linePositionsDirty = true;
                InvalidateVisual();
            }
        }

        // ── Change Marker Gutter (#166) ───────────────────────────────────────

        public static readonly DependencyProperty ShowChangeMarkersProperty =
            DependencyProperty.Register(nameof(ShowChangeMarkers), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, OnShowChangeMarkersChanged));

        [Category("Features")]
        [DisplayName("Show Change Markers")]
        [Description("Shows Added / Modified / Deleted line indicators in the gutter.")]
        public bool ShowChangeMarkers
        {
            get => (bool)GetValue(ShowChangeMarkersProperty);
            set => SetValue(ShowChangeMarkersProperty, value);
        }

        private static void OnShowChangeMarkersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor ce || ce._changeMarkerGutterControl is null) return;
            ce._changeMarkerGutterControl.Visibility = (bool)e.NewValue
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
            ce.InvalidateMeasure();
        }

        public static readonly DependencyProperty ShowInlineHintsProperty =
            DependencyProperty.Register(nameof(ShowInlineHints), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnShowInlineHintsChanged));

        [Category("Features")]
        [DisplayName("Show Code Lens")]
        [Description("Shows inline reference counts above each declaration.")]
        public bool ShowInlineHints
        {
            get => (bool)GetValue(ShowInlineHintsProperty);
            set => SetValue(ShowInlineHintsProperty, value);
        }

        private static void OnShowInlineHintsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor ce) return;
            // Trigger layout/scrollbar recalculation — _lineHeight is no longer affected by InlineHints.
            // Only visible-line Y positions change (per-declaration extra space), handled in OnRender.
            ce.InvalidateMeasure();
        }

        public static readonly DependencyProperty ShowLspInlayHintsProperty =
            DependencyProperty.Register(nameof(ShowLspInlayHints), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    (d, _) => (d as CodeEditor)?._lspInlayHintsLayer.SetContext(null, 0, 0, 0, 0)));

        [Category("Features")]
        [DisplayName("Show LSP Inlay Hints")]
        [Description("Shows parameter-name hints inline before arguments (requires LSP).")]
        public bool ShowLspInlayHints
        {
            get => (bool)GetValue(ShowLspInlayHintsProperty);
            set => SetValue(ShowLspInlayHintsProperty, value);
        }

        public static readonly DependencyProperty ShowLspDeclarationHintsProperty =
            DependencyProperty.Register(nameof(ShowLspDeclarationHints), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    (d, _) => (d as CodeEditor)?._lspDeclarationHintsLayer.SetContext(null, 0, 0, 0, 0)));

        [Category("Features")]
        [DisplayName("Show LSP Code Lens")]
        [Description("Shows reference counts and test runner hints above declarations (requires LSP).")]
        public bool ShowLspDeclarationHints
        {
            get => (bool)GetValue(ShowLspDeclarationHintsProperty);
            set => SetValue(ShowLspDeclarationHintsProperty, value);
        }

        public static readonly DependencyProperty EnableSemanticHighlightingProperty =
            DependencyProperty.Register(nameof(EnableSemanticHighlighting), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(false,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    (o, e) =>
                    {
                        var ce      = (CodeEditor)o;
                        var enabled = (bool)e.NewValue;
                        ce._semanticTokensLayer.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
                        ce._semanticTokensLayer.SetLspClient(enabled ? ce._lspClient : null);
                        if (!enabled) ce._semanticTokensLayer.SetContext(null, 0, 0, 0, 0);
                    }));

        [Category("Features")]
        [DisplayName("Semantic Highlighting")]
        [Description("Overlays LSP semantic token colors above syntactic highlighting (requires Roslyn LSP). whfmt coloring is unchanged when disabled or when no LSP is active.")]
        public bool EnableSemanticHighlighting
        {
            get => (bool)GetValue(EnableSemanticHighlightingProperty);
            set => SetValue(EnableSemanticHighlightingProperty, value);
        }

        public static readonly DependencyProperty InlineHintsVisibleKindsProperty =
            DependencyProperty.Register(
                nameof(InlineHintsVisibleKinds),
                typeof(WpfHexEditor.Editor.Core.InlineHintsSymbolKinds),
                typeof(CodeEditor),
                new FrameworkPropertyMetadata(
                    WpfHexEditor.Editor.Core.InlineHintsSymbolKinds.All,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnInlineHintsVisibleKindsChanged));

        [Category("Features")]
        [DisplayName("Code Lens Visible Kinds")]
        [Description("Bitmask of symbol kinds for which inline reference-count hints are displayed.")]
        public WpfHexEditor.Editor.Core.InlineHintsSymbolKinds InlineHintsVisibleKinds
        {
            get => (WpfHexEditor.Editor.Core.InlineHintsSymbolKinds)GetValue(InlineHintsVisibleKindsProperty);
            set => SetValue(InlineHintsVisibleKindsProperty, value);
        }

        private static void OnInlineHintsVisibleKindsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor ce) return;
            ce.RebuildVisibleHintsCount();
            ce.InvalidateMeasure();
        }

        /// <summary>
        /// Reference-count source strategy: 0=Auto, 1=RoslynOnly, 2=RegexAlways.
        /// Setting this re-wires the InlineHints provider immediately.
        /// </summary>
        [Category("Features")]
        [DisplayName("Inline Hints Source")]
        [Description("0=Auto (Roslyn when available), 1=RoslynOnly, 2=RegexAlways.")]
        public int InlineHintsSource
        {
            get => _inlineHintsSource;
            set
            {
                if (_inlineHintsSource == value) return;
                _inlineHintsSource = value;
                _inlineHintsService.SetReferenceCountProvider(
                    _lspClient as WpfHexEditor.Editor.Core.LSP.IReferenceCountProvider,
                    value);
            }
        }

        private bool _showVarTypeHints = true;
        public bool ShowVarTypeHints
        {
            get => _showVarTypeHints;
            set
            {
                if (_showVarTypeHints == value) return;
                _showVarTypeHints = value;
                NotifyRoslynInlineHintsOptions();
            }
        }

        private bool _showLambdaReturnTypeHints = true;
        public bool ShowLambdaReturnTypeHints
        {
            get => _showLambdaReturnTypeHints;
            set
            {
                if (_showLambdaReturnTypeHints == value) return;
                _showLambdaReturnTypeHints = value;
                NotifyRoslynInlineHintsOptions();
            }
        }

        private void NotifyRoslynInlineHintsOptions()
        {
            if (_lspClient is WpfHexEditor.Editor.Core.LSP.IInlineHintsOptionsClient hintsClient)
                hintsClient.SetInlineHintsOptions(_showVarTypeHints, _showLambdaReturnTypeHints);
        }

        // ── Quick Info DPs ─────────────────────────────────────────────────────

        public static readonly DependencyProperty ShowQuickInfoProperty =
            DependencyProperty.Register(nameof(ShowQuickInfo), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, OnShowQuickInfoChanged));

        [Category("Features")]
        [DisplayName("Show Quick Info")]
        [Description("Shows an interactive VS-like hover tooltip with symbol information (Quick Info).")]
        public bool ShowQuickInfo
        {
            get => (bool)GetValue(ShowQuickInfoProperty);
            set => SetValue(ShowQuickInfoProperty, value);
        }

        private static void OnShowQuickInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor ce || (bool)e.NewValue) return;
            ce._quickInfoPopup?.Hide();
            ce._hoverQuickInfoService?.Cancel();
        }

        public static readonly DependencyProperty QuickInfoDelayMsProperty =
            DependencyProperty.Register(nameof(QuickInfoDelayMs), typeof(int), typeof(CodeEditor),
                new FrameworkPropertyMetadata(400, OnQuickInfoDelayChanged));

        [Category("Features")]
        [DisplayName("Quick Info Delay (ms)")]
        [Description("Hover dwell time in milliseconds before Quick Info appears (200–1000 ms).")]
        public int QuickInfoDelayMs
        {
            get => (int)GetValue(QuickInfoDelayMsProperty);
            set => SetValue(QuickInfoDelayMsProperty, value);
        }

        private static void OnQuickInfoDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor ce || ce._hoverQuickInfoService is null) return;
            ce._hoverQuickInfoService.SetDebounceInterval(
                TimeSpan.FromMilliseconds(Math.Max(200, (int)e.NewValue)));
        }

        // ── Breakpoint Line Highlight DP ──────────────────────────────────────

        public static readonly DependencyProperty ShowBreakpointLineHighlightProperty =
            DependencyProperty.Register(nameof(ShowBreakpointLineHighlight), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, (d, _) => (d as CodeEditor)?.InvalidateVisual()));

        [Category("Features")]
        [DisplayName("Highlight Breakpoint Lines")]
        [Description("When true, breakpoint lines are highlighted with a semi-transparent background tint.")]
        public bool ShowBreakpointLineHighlight
        {
            get => (bool)GetValue(ShowBreakpointLineHighlightProperty);
            set => SetValue(ShowBreakpointLineHighlightProperty, value);
        }

        // ── End-of-Block Hint DPs ─────────────────────────────────────────────

        public static readonly DependencyProperty ShowEndOfBlockHintProperty =
            DependencyProperty.Register(nameof(ShowEndOfBlockHint), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, OnShowEndOfBlockHintChanged));

        [Category("Features")]
        [DisplayName("Show End-of-Block Hint")]
        [Description("When true, hovering over }, #endregion, </Tag> shows the matching opening line(s).")]
        public bool ShowEndOfBlockHint
        {
            get => (bool)GetValue(ShowEndOfBlockHintProperty);
            set => SetValue(ShowEndOfBlockHintProperty, value);
        }

        private static void OnShowEndOfBlockHintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor ce || (bool)e.NewValue) return;
            ce.DismissEndBlockHint();
        }

        public static readonly DependencyProperty EndOfBlockHintDelayMsProperty =
            DependencyProperty.Register(nameof(EndOfBlockHintDelayMs), typeof(int), typeof(CodeEditor),
                new FrameworkPropertyMetadata(600, OnEndOfBlockHintDelayChanged));

        [Category("Features")]
        [DisplayName("End-of-Block Hint Delay (ms)")]
        [Description("Hover dwell time in milliseconds before the end-of-block hint popup appears (100–2000 ms).")]
        public int EndOfBlockHintDelayMs
        {
            get => (int)GetValue(EndOfBlockHintDelayMsProperty);
            set => SetValue(EndOfBlockHintDelayMsProperty, value);
        }

        private static void OnEndOfBlockHintDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor ce || ce._endBlockHintTimer is null) return;
            ce._endBlockHintTimer.Interval = TimeSpan.FromMilliseconds(
                Math.Clamp((int)e.NewValue, 100, 2000));
        }

        // ── Language Definition DP ─────────────────────────────────────────────

        public static readonly DependencyProperty LanguageProperty =
            DependencyProperty.Register(nameof(Language), typeof(LanguageDefinition), typeof(CodeEditor),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnLanguageChanged));

        [Category("Features")]
        [DisplayName("Language Definition")]
        [Description("Active language definition controlling InlineHints and Ctrl+Click navigation per language.")]
        public LanguageDefinition? Language
        {
            get => (LanguageDefinition?)GetValue(LanguageProperty);
            set => SetValue(LanguageProperty, value);
        }

        private static void OnLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor editor) return;
            var newLang = e.NewValue as LanguageDefinition;

            // Update column rulers from the language definition; null clears rulers.
            editor.ColumnRulers = newLang?.ColumnRulers;

            // Update bracket pair colorizer — drives CE_Bracket_1/2/3/4 depth colors.
            editor._bracketColorizer.SetPairs(newLang?.BracketPairs);
            editor._bracketDepthFirstLine = -1; // force depth rescan on next render

            // Re-evaluate InlineHints service attachment: only attach when the language declares support.
            if (newLang?.EnableInlineHints == true)
                editor._inlineHintsService.Attach(editor._document, editor._currentFilePath);
            else
                editor._inlineHintsService.Detach();

            // Rebuild folding strategy from language-specific FoldingRules.
            var foldingStrategy = Folding.LanguageFoldingStrategyBuilder.Build(newLang?.FoldingRules, newLang?.LineCommentPrefix);
            if (foldingStrategy is not null && editor._foldingEngine is not null)
            {
                editor._foldingEngine.ReplaceStrategy(foldingStrategy);
                editor._foldingEngine.Analyze(editor._document.Lines);
                editor.InvalidateVisual();
            }

            // Compile breakpoint placement validation regexes from BreakpointRules.
            editor.RebuildBreakpointValidation(newLang);

            // Keep SmartComplete popup aware of the current language (for local completions).
            if (editor._smartCompletePopup is not null)
                editor._smartCompletePopup.CurrentLanguage = newLang;
        }

        private void RebuildBreakpointValidation(LanguageDefinition? lang)
        {
            var patterns = lang?.BreakpointRules?.NonExecutablePatterns ?? Array.Empty<string>();
            var compiled = new List<Regex>(patterns.Count);
            foreach (var p in patterns)
            {
                try   { compiled.Add(new Regex(p, RegexOptions.Compiled)); }
                catch { /* malformed pattern — skip silently */              }
            }
            _bpNonExecutableRegexes = compiled;

            // Compile statement continuation regexes for multi-line highlight.
            var contPatterns = lang?.BreakpointRules?.StatementContinuationPatterns ?? Array.Empty<string>();
            var contCompiled = new List<Regex>(contPatterns.Count);
            foreach (var cp in contPatterns)
            {
                try   { contCompiled.Add(new Regex(cp, RegexOptions.Compiled)); }
                catch { /* malformed pattern — skip silently */                   }
            }
            _bpContinuationRegexes = contCompiled;
            _bpMaxScanLines        = lang?.BreakpointRules?.MaxStatementScanLines ?? 20;
            _bpBlockScopeHighlight = lang?.BreakpointRules?.BlockScopeHighlight ?? true;

            if (_breakpointGutterControl is null) return;
            _breakpointGutterControl.ValidateLine = compiled.Count == 0
                ? (Func<int, bool>?)null
                : line =>
                {
                    string text = (line >= 1 && line <= _document?.Lines.Count)
                        ? (_document.Lines[line - 1].Text ?? string.Empty)
                        : string.Empty;
                    foreach (var r in _bpNonExecutableRegexes)
                        if (r.IsMatch(text)) return false;
                    return true;
                };

            // Statement-span enforcement: 1 instruction = 1 breakpoint.
            // The BP is always anchored to the FIRST line of the statement,
            // regardless of which line the user clicked.
            _breakpointGutterControl.ResolveBreakpointLine = contCompiled.Count == 0
                ? null
                : clickedLine1 =>
                {
                    int line0 = clickedLine1 - 1;
                    var (start0, end0) = ResolveStatementSpan(line0);

                    int firstLine1 = start0 + 1; // 1-based first line of the statement

                    var existingBps = _bpSource?.GetBreakpointLines(_currentFilePath ?? string.Empty)
                                      ?? (IReadOnlyList<int>)Array.Empty<int>();

                    int? existingInSpan = null;
                    foreach (int bp1 in existingBps)
                    {
                        int bp0 = bp1 - 1;
                        if (bp0 >= start0 && bp0 <= end0)
                        {
                            existingInSpan = bp1;
                            break;
                        }
                    }

                    // No existing BP in this statement → place on the first line.
                    if (existingInSpan is null)
                        return firstLine1;

                    // BP already on the first line → toggle it off.
                    if (existingInSpan.Value == firstLine1)
                        return firstLine1;

                    // BP on a non-first line (anomalous state): move it to the first line.
                    _bpSource!.Delete(_currentFilePath!, existingInSpan.Value);
                    return firstLine1;
                };
        }

        // ── Column Rulers DP (#165) ───────────────────────────────────────────

        public static readonly DependencyProperty ColumnRulersProperty =
            DependencyProperty.Register(nameof(ColumnRulers), typeof(IReadOnlyList<int>), typeof(CodeEditor),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Column ruler positions driven by whfmt <c>"columnRulers"</c> (e.g. [80, 120]).
        /// Set automatically from <see cref="Language"/> when the active language changes.
        /// Null = no rulers.
        /// </summary>
        [Category("Features")]
        [DisplayName("Column Rulers")]
        [Description("Character columns at which vertical guide lines are drawn. Driven by whfmt 'columnRulers'.")]
        public IReadOnlyList<int>? ColumnRulers
        {
            get => (IReadOnlyList<int>?)GetValue(ColumnRulersProperty);
            set => SetValue(ColumnRulersProperty, value);
        }

        public static readonly DependencyProperty ShowColumnRulersProperty =
            DependencyProperty.Register(nameof(ShowColumnRulers), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Show or hide the vertical column ruler lines defined by the active language's whfmt.
        /// Toggled via the CodeEditor context menu.
        /// </summary>
        public bool ShowColumnRulers
        {
            get => (bool)GetValue(ShowColumnRulersProperty);
            set => SetValue(ShowColumnRulersProperty, value);
        }

        public static readonly DependencyProperty EnableValidationProperty =
            DependencyProperty.Register(nameof(EnableValidation), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Enable real-time format definition validation
        /// </summary>
        [Category("Features")]
        [DisplayName("Enable Validation")]
        [Description("Enable real-time validation with visual feedback (squiggly lines under errors)")]
        public bool EnableValidation
        {
            get => (bool)GetValue(EnableValidationProperty);
            set
            {
                SetValue(EnableValidationProperty, value);
                if (value)
                    TriggerValidation();
                else
                {
                    _validationErrors.Clear();
                    _validationByLine.Clear();
                    DiagnosticsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // ===== APPEARANCE - FONTS =====

        public static readonly DependencyProperty EditorFontFamilyProperty =
            DependencyProperty.Register(nameof(EditorFontFamily), typeof(FontFamily), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new FontFamily("Consolas"), FrameworkPropertyMetadataOptions.AffectsRender,
                    OnFontChanged));

        [Category("Appearance.Fonts")]
        [DisplayName("Editor Font Family")]
        [Description("Font family for editor text (monospace recommended)")]
        public FontFamily EditorFontFamily
        {
            get => (FontFamily)GetValue(EditorFontFamilyProperty);
            set => SetValue(EditorFontFamilyProperty, value);
        }

        public static readonly DependencyProperty EditorFontSizeProperty =
            DependencyProperty.Register(nameof(EditorFontSize), typeof(double), typeof(CodeEditor),
                new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnFontChanged));

        [Category("Appearance.Fonts")]
        [DisplayName("Editor Font Size")]
        [Description("Font size for editor text (points)")]
        [Range(8, 72)]
        public double EditorFontSize
        {
            get => (double)GetValue(EditorFontSizeProperty);
            set => SetValue(EditorFontSizeProperty, value);
        }

        // -- ZoomLevel (B6) -----------------------------------------------------

        public static readonly DependencyProperty ZoomLevelProperty =
            DependencyProperty.Register(nameof(ZoomLevel), typeof(double), typeof(CodeEditor),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnZoomLevelChanged),
                ValidateZoomLevel);

        private static bool ValidateZoomLevel(object value)
            => value is double d && d >= 0.5 && d <= 4.0;

        private static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor editor) return;
            var newZoom = (double)e.NewValue;
            var oldZoom = (double)e.OldValue;

            editor._fontSize = editor._baseFontSize * newZoom;
            editor._lineNumberCache.Clear();

            // Capture old line height BEFORE CalculateCharacterDimensions overwrites it.
            double oldLineHeight = editor._lineHeight;

            editor.CalculateCharacterDimensions();

            // Keep the first visible line anchored when zooming: rebase the pixel offset
            // so the same line stays at the top after the line height changes.
            if (oldLineHeight > 0 && editor._verticalScrollOffset > 0)
            {
                double firstVisibleFrac = editor._verticalScrollOffset / oldLineHeight;
                editor._verticalScrollOffset = firstVisibleFrac * editor._lineHeight;
                editor._currentScrollOffset  = editor._verticalScrollOffset;
                editor._targetScrollOffset   = editor._verticalScrollOffset;
                if (editor._virtualizationEngine != null)
                    editor._virtualizationEngine.ScrollOffset = editor._verticalScrollOffset;
                // Sync scrollbar Value immediately so it reflects the rebased offset
                // before the ArrangeOverride (which updates Maximum) has had a chance to run.
                // Without this, the user could interact with a stale scrollbar position.
                editor.SyncVScrollBar();
            }

            editor.InvalidateMeasure();
            editor.ZoomLevelChanged?.Invoke(editor, newZoom);
        }

        /// <summary>
        /// Current zoom multiplier applied on top of <see cref="EditorFontSize"/>.
        /// Range: 0.5 (50%) to 4.0 (400%), default 1.0 (100%).
        /// Ctrl+MouseWheel adjusts this value in 0.1 increments.
        /// </summary>
        [Category("Appearance.Fonts")]
        [DisplayName("Zoom Level")]
        [Description("Zoom multiplier applied to the base font size (0.5–4.0). Ctrl+wheel to adjust.")]
        public double ZoomLevel
        {
            get => (double)GetValue(ZoomLevelProperty);
            set => SetValue(ZoomLevelProperty, Math.Clamp(value, 0.5, 4.0));
        }

        /// <summary>Raised when <see cref="ZoomLevel"/> changes. Arg is the new zoom value.</summary>
        public event EventHandler<double>? ZoomLevelChanged;

        /// <summary>
        /// Raised when the minimap should refresh: scroll position changed, text changed,
        /// syntax highlights completed, or folding regions changed.
        /// Subscribe to this instead of <see cref="FrameworkElement.LayoutUpdated"/> to
        /// avoid a feedback loop where minimap InvalidateVisual re-triggers LayoutUpdated.
        /// </summary>
        public event EventHandler? MinimapRefreshRequested;

        public static readonly DependencyProperty LineNumberFontSizeProperty =
            DependencyProperty.Register(nameof(LineNumberFontSize), typeof(double), typeof(CodeEditor),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Fonts")]
        [DisplayName("Line Number Font Size")]
        [Description("Font size for line numbers (points)")]
        [Range(6, 24)]
        public double LineNumberFontSize
        {
            get => (double)GetValue(LineNumberFontSizeProperty);
            set => SetValue(LineNumberFontSizeProperty, value);
        }

        public static readonly DependencyProperty EditorFontWeightProperty =
            DependencyProperty.Register(nameof(EditorFontWeight), typeof(FontWeight), typeof(CodeEditor),
                new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnFontChanged));

        [Category("Appearance.Fonts")]
        [DisplayName("Editor Font Weight")]
        [Description("Font weight for editor text (Normal, Bold, etc.)")]
        public FontWeight EditorFontWeight
        {
            get => (FontWeight)GetValue(EditorFontWeightProperty);
            set => SetValue(EditorFontWeightProperty, value);
        }

        public static readonly DependencyProperty LineNumberFontFamilyProperty =
            DependencyProperty.Register(nameof(LineNumberFontFamily), typeof(FontFamily), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new FontFamily("Consolas"), FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Fonts")]
        [DisplayName("Line Number Font Family")]
        [Description("Font family for line numbers")]
        public FontFamily LineNumberFontFamily
        {
            get => (FontFamily)GetValue(LineNumberFontFamilyProperty);
            set => SetValue(LineNumberFontFamilyProperty, value);
        }

        public static readonly DependencyProperty LineHeightMultiplierProperty =
            DependencyProperty.Register(nameof(LineHeightMultiplier), typeof(double), typeof(CodeEditor),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnFontChanged));

        [Category("Appearance.Fonts")]
        [DisplayName("Line Height Multiplier")]
        [Description("Line height as a multiple of font size (1.0-3.0)")]
        [Range(1.0, 3.0)]
        public double LineHeightMultiplier
        {
            get => (double)GetValue(LineHeightMultiplierProperty);
            set => SetValue(LineHeightMultiplierProperty, Math.Max(1.0, Math.Min(3.0, value)));
        }

        public static readonly DependencyProperty BoldKeywordsProperty =
            DependencyProperty.Register(nameof(BoldKeywords), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Fonts")]
        [DisplayName("Bold Keywords")]
        [Description("Render keywords (signature, field, etc.) in bold font")]
        public bool BoldKeywords
        {
            get => (bool)GetValue(BoldKeywordsProperty);
            set => SetValue(BoldKeywordsProperty, value);
        }

        public static readonly DependencyProperty ItalicCommentsProperty =
            DependencyProperty.Register(nameof(ItalicComments), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Fonts")]
        [DisplayName("Italic Comments")]
        [Description("Render comments in italic font")]
        public bool ItalicComments
        {
            get => (bool)GetValue(ItalicCommentsProperty);
            set => SetValue(ItalicCommentsProperty, value);
        }

        // ===== APPEARANCE - COLORS =====

        public static readonly DependencyProperty EditorBackgroundProperty =
            DependencyProperty.Register(nameof(EditorBackground), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Colors")]
        [DisplayName("Editor Background")]
        [Description("Background color of the editor")]
        public Brush EditorBackground
        {
            get => (Brush)GetValue(EditorBackgroundProperty);
            set => SetValue(EditorBackgroundProperty, value);
        }

        public static readonly DependencyProperty EditorForegroundProperty =
            DependencyProperty.Register(nameof(EditorForeground), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Colors")]
        [DisplayName("Editor Foreground")]
        [Description("Default text color")]
        public Brush EditorForeground
        {
            get => (Brush)GetValue(EditorForegroundProperty);
            set => SetValue(EditorForegroundProperty, value);
        }

        public static readonly DependencyProperty LineNumberBackgroundProperty =
            DependencyProperty.Register(nameof(LineNumberBackground), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Colors")]
        [DisplayName("Line Number Background")]
        [Description("Background color of line number gutter")]
        public Brush LineNumberBackground
        {
            get => (Brush)GetValue(LineNumberBackgroundProperty);
            set => SetValue(LineNumberBackgroundProperty, value);
        }

        public static readonly DependencyProperty LineNumberForegroundProperty =
            DependencyProperty.Register(nameof(LineNumberForeground), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Colors")]
        [DisplayName("Line Number Foreground")]
        [Description("Text color of line numbers")]
        public Brush LineNumberForeground
        {
            get => (Brush)GetValue(LineNumberForegroundProperty);
            set => SetValue(LineNumberForegroundProperty, value);
        }

        public static readonly DependencyProperty CurrentLineBackgroundProperty =
            DependencyProperty.Register(nameof(CurrentLineBackground), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(30, 0, 120, 215)),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Colors")]
        [DisplayName("Current Line Background")]
        [Description("Highlight color for current line")]
        public Brush CurrentLineBackground
        {
            get => (Brush)GetValue(CurrentLineBackgroundProperty);
            set => SetValue(CurrentLineBackgroundProperty, value);
        }

        public static readonly DependencyProperty SelectionBackgroundProperty =
            DependencyProperty.Register(nameof(SelectionBackground), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(173, 214, 255)),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Colors")]
        [DisplayName("Selection Background")]
        [Description("Background color for selected text")]
        public Brush SelectionBackground
        {
            get => (Brush)GetValue(SelectionBackgroundProperty);
            set => SetValue(SelectionBackgroundProperty, value);
        }

        // ===== APPEARANCE - EDITOR COLORS (Advanced) =====

        public static readonly DependencyProperty CaretColorProperty =
            DependencyProperty.Register(nameof(CaretColor), typeof(Color), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Colors")]
        [DisplayName("Caret Color")]
        [Description("Color of the text cursor")]
        public Color CaretColor
        {
            get => (Color)GetValue(CaretColorProperty);
            set => SetValue(CaretColorProperty, value);
        }

        public static readonly DependencyProperty CurrentLineBorderColorProperty =
            DependencyProperty.Register(nameof(CurrentLineBorderColor), typeof(Color), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Color.FromArgb(80, 0, 120, 215), FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Colors")]
        [DisplayName("Current Line Border Color")]
        [Description("Border color around the current line (when ShowCurrentLineBorder is enabled)")]
        public Color CurrentLineBorderColor
        {
            get => (Color)GetValue(CurrentLineBorderColorProperty);
            set => SetValue(CurrentLineBorderColorProperty, value);
        }

        public static readonly DependencyProperty InactiveSelectionBackgroundProperty =
            DependencyProperty.Register(nameof(InactiveSelectionBackground), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(62, 62, 66)),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Colors")]
        [DisplayName("Inactive Selection Background")]
        [Description("Selection background color when editor loses focus (VS-like grayed selection)")]
        public Brush InactiveSelectionBackground
        {
            get => (Brush)GetValue(InactiveSelectionBackgroundProperty);
            set => SetValue(InactiveSelectionBackgroundProperty, value);
        }

        public static readonly DependencyProperty ValidationErrorGlyphColorProperty =
            DependencyProperty.Register(nameof(ValidationErrorGlyphColor), typeof(Color), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Colors.Red, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Colors")]
        [DisplayName("Validation Error Glyph")]
        [Description("Color for error icons in gutter")]
        public Color ValidationErrorGlyphColor
        {
            get => (Color)GetValue(ValidationErrorGlyphColorProperty);
            set => SetValue(ValidationErrorGlyphColorProperty, value);
        }

        public static readonly DependencyProperty ValidationWarningGlyphColorProperty =
            DependencyProperty.Register(nameof(ValidationWarningGlyphColor), typeof(Color), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Color.FromRgb(255, 165, 0), FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Appearance.Colors")]
        [DisplayName("Validation Warning Glyph")]
        [Description("Color for warning icons in gutter")]
        public Color ValidationWarningGlyphColor
        {
            get => (Color)GetValue(ValidationWarningGlyphColorProperty);
            set => SetValue(ValidationWarningGlyphColorProperty, value);
        }

        // ===== BEHAVIOR =====

        public static readonly DependencyProperty IndentSizeProperty =
            DependencyProperty.Register(nameof(IndentSize), typeof(int), typeof(CodeEditor),
                new FrameworkPropertyMetadata(2, OnIndentSizeChanged));

        [Category("Behavior")]
        [DisplayName("Indent Size")]
        [Description("Number of spaces per indentation level")]
        public int IndentSize
        {
            get => (int)GetValue(IndentSizeProperty);
            set => SetValue(IndentSizeProperty, value);
        }

        public static readonly DependencyProperty AutoIndentModeProperty =
            DependencyProperty.Register(nameof(AutoIndentMode), typeof(AutoIndentMode), typeof(CodeEditor),
                new FrameworkPropertyMetadata(AutoIndentMode.KeepIndent));

        [Category("Behavior")]
        [DisplayName("Auto-Indent Mode")]
        [Description("Controls automatic indentation when Enter is pressed: None, KeepIndent, or Smart.")]
        public AutoIndentMode AutoIndentMode
        {
            get => (AutoIndentMode)GetValue(AutoIndentModeProperty);
            set => SetValue(AutoIndentModeProperty, value);
        }

        public static readonly DependencyProperty SmartCompleteDelayProperty =
            DependencyProperty.Register(nameof(SmartCompleteDelay), typeof(int), typeof(CodeEditor),
                new FrameworkPropertyMetadata(300));

        [Category("Behavior")]
        [DisplayName("SmartComplete Delay (ms)")]
        [Description("Delay before showing SmartComplete popup (milliseconds)")]
        public int SmartCompleteDelay
        {
            get => (int)GetValue(SmartCompleteDelayProperty);
            set => SetValue(SmartCompleteDelayProperty, value);
        }

        public static readonly DependencyProperty ValidationDelayProperty =
            DependencyProperty.Register(nameof(ValidationDelay), typeof(int), typeof(CodeEditor),
                new FrameworkPropertyMetadata(1000));

        [Category("Behavior")]
        [DisplayName("Validation Delay (ms)")]
        [Description("Delay before running validation after text change (milliseconds)")]
        public int ValidationDelay
        {
            get => (int)GetValue(ValidationDelayProperty);
            set => SetValue(ValidationDelayProperty, value);
        }

        // ===== BEHAVIOR - SELECTION & CURSOR =====

        public static readonly DependencyProperty ShowCurrentLineHighlightProperty =
            DependencyProperty.Register(nameof(ShowCurrentLineHighlight), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Behavior.Selection")]
        [DisplayName("Show Current Line Highlight")]
        [Description("Highlight the line where the cursor is located")]
        public bool ShowCurrentLineHighlight
        {
            get => (bool)GetValue(ShowCurrentLineHighlightProperty);
            set => SetValue(ShowCurrentLineHighlightProperty, value);
        }

        public static readonly DependencyProperty ShowCurrentLineBorderProperty =
            DependencyProperty.Register(nameof(ShowCurrentLineBorder), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Behavior.Selection")]
        [DisplayName("Show Current Line Border")]
        [Description("Show border around the current line")]
        public bool ShowCurrentLineBorder
        {
            get => (bool)GetValue(ShowCurrentLineBorderProperty);
            set => SetValue(ShowCurrentLineBorderProperty, value);
        }

        public static readonly DependencyProperty CaretBlinkRateProperty =
            DependencyProperty.Register(nameof(CaretBlinkRate), typeof(int), typeof(CodeEditor),
                new FrameworkPropertyMetadata(500, OnCaretBlinkRateChanged));

        [Category("Behavior.Selection")]
        [DisplayName("Caret Blink Rate (ms)")]
        [Description("Cursor blink speed in milliseconds (0 = no blink)")]
        public int CaretBlinkRate
        {
            get => (int)GetValue(CaretBlinkRateProperty);
            set => SetValue(CaretBlinkRateProperty, Math.Max(0, Math.Min(2000, value)));
        }

        private static void OnCaretBlinkRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CodeEditor editor)
            {
                editor.UpdateCaretBlinkTimer();
            }
        }

        public static readonly DependencyProperty CaretWidthProperty =
            DependencyProperty.Register(nameof(CaretWidth), typeof(double), typeof(CodeEditor),
                new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Behavior.Selection")]
        [DisplayName("Caret Width")]
        [Description("Width of the text cursor in pixels")]
        public double CaretWidth
        {
            get => (double)GetValue(CaretWidthProperty);
            set => SetValue(CaretWidthProperty, Math.Max(1.0, Math.Min(5.0, value)));
        }

        public static readonly DependencyProperty SmartBackspaceProperty =
            DependencyProperty.Register(nameof(SmartBackspace), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true));

        [Category("Behavior.Selection")]
        [DisplayName("Smart Backspace")]
        [Description("Backspace removes full indent when at start of indented line")]
        public bool SmartBackspace
        {
            get => (bool)GetValue(SmartBackspaceProperty);
            set => SetValue(SmartBackspaceProperty, value);
        }

        // ===== BEHAVIOR - ADVANCED FEATURES =====

        public static readonly DependencyProperty BracketPairColorizationEnabledProperty =
            DependencyProperty.Register(nameof(BracketPairColorizationEnabled), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When true and the active language defines <c>bracketPairs</c> in its whfmt,
        /// brackets are colored with CE_Bracket_1/2/3/4 based on nesting depth.
        /// False = all brackets use the single <c>CE_Bracket</c> token.
        /// </summary>
        [Category("Behavior.Advanced")]
        [DisplayName("Bracket Pair Colorization")]
        [Description("Color brackets with CE_Bracket_1/2/3/4 based on nesting depth (requires bracketPairs in whfmt).")]
        public bool BracketPairColorizationEnabled
        {
            get => (bool)GetValue(BracketPairColorizationEnabledProperty);
            set => SetValue(BracketPairColorizationEnabledProperty, value);
        }

        // -- Rainbow Scope Guides -------------------------------------------------

        public static readonly DependencyProperty RainbowScopeGuidesEnabledProperty =
            DependencyProperty.Register(nameof(RainbowScopeGuidesEnabled), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When true and bracket pair colorization is enabled, scope guide lines
        /// are colored with CE_Bracket_1/2/3/4 based on nesting depth.
        /// </summary>
        [Category("Behavior.Advanced")]
        [DisplayName("Rainbow Scope Guides")]
        [Description("Color scope guide lines by bracket depth using CE_Bracket_1/2/3/4.")]
        public bool RainbowScopeGuidesEnabled
        {
            get => (bool)GetValue(RainbowScopeGuidesEnabledProperty);
            set => SetValue(RainbowScopeGuidesEnabledProperty, value);
        }

        // Rainbow scope guide pen cache — instance fields because they depend on theme resources.
        private Pen[]? _rainbowGuidePens;
        private Pen[]? _rainbowGuideActivePens;

        /// <summary>
        /// Lazily creates 4 inactive + 4 active frozen pens from the CE_Bracket_1..4 theme resources.
        /// </summary>
        private void EnsureRainbowGuidePens()
        {
            if (_rainbowGuidePens != null) return;

            _rainbowGuidePens = new Pen[4];
            _rainbowGuideActivePens = new Pen[4];

            for (int i = 0; i < 4; i++)
            {
                var res = TryFindResource($"CE_Bracket_{i + 1}");
                Color color = res is SolidColorBrush scb ? scb.Color
                            : res is Color c             ? c
                            : Colors.Gray;

                _rainbowGuidePens[i] = MakeFrozenPen(Color.FromArgb(120, color.R, color.G, color.B), 1.0);
                _rainbowGuideActivePens[i] = MakeFrozenPen(Color.FromArgb(220, color.R, color.G, color.B), 1.5);
            }
        }

        // -- Color Swatch Preview (#168) -----------------------------------------

        public static readonly DependencyProperty ColorSwatchPreviewEnabledProperty =
            DependencyProperty.Register(nameof(ColorSwatchPreviewEnabled), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// When true and the active language has <c>colorLiteralPatterns</c> in its whfmt definition,
        /// a 12×12 colour preview swatch is rendered to the left of each colour literal.
        /// Click a swatch to raise <see cref="ColorSwatchClicked"/>.
        /// </summary>
        public bool ColorSwatchPreviewEnabled
        {
            get => (bool)GetValue(ColorSwatchPreviewEnabledProperty);
            set => SetValue(ColorSwatchPreviewEnabledProperty, value);
        }

        /// <summary>
        /// Raised when the user clicks a color swatch.
        /// The host can open a color picker and apply the edited color back into the document.
        /// </summary>
        public event EventHandler<ColorSwatchClickedEventArgs>? ColorSwatchClicked;

        /// <summary>
        /// Raised when the user clicks "Options…" in the Formatting context menu.
        /// The host should open the Code Editor options page.
        /// </summary>
        public event EventHandler? FormattingOptionsRequested;

        public static readonly DependencyProperty EnableBracketMatchingProperty =
            DependencyProperty.Register(nameof(EnableBracketMatching), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Behavior.Advanced")]
        [DisplayName("Enable Bracket Matching")]
        [Description("Highlight matching brackets/braces when cursor is on one")]
        public bool EnableBracketMatching
        {
            get => (bool)GetValue(EnableBracketMatchingProperty);
            set => SetValue(EnableBracketMatchingProperty, value);
        }

        public static readonly DependencyProperty EnableAutoClosingBracketsProperty =
            DependencyProperty.Register(nameof(EnableAutoClosingBrackets), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true));

        [Category("Behavior.Advanced")]
        [DisplayName("Auto-Close Brackets")]
        [Description("Automatically insert closing bracket/brace when typing opening one")]
        public bool EnableAutoClosingBrackets
        {
            get => (bool)GetValue(EnableAutoClosingBracketsProperty);
            set => SetValue(EnableAutoClosingBracketsProperty, value);
        }

        public static readonly DependencyProperty EnableAutoClosingQuotesProperty =
            DependencyProperty.Register(nameof(EnableAutoClosingQuotes), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true));

        [Category("Behavior.Advanced")]
        [DisplayName("Auto-Close Quotes")]
        [Description("Automatically insert closing quote when typing opening quote")]
        public bool EnableAutoClosingQuotes
        {
            get => (bool)GetValue(EnableAutoClosingQuotesProperty);
            set => SetValue(EnableAutoClosingQuotesProperty, value);
        }

        public static readonly DependencyProperty SkipOverClosingCharProperty =
            DependencyProperty.Register(nameof(SkipOverClosingChar), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true));

        [Category("Behavior.Advanced")]
        [DisplayName("Skip Over Closing Char")]
        [Description("When a closing bracket/quote already exists at cursor, advance over it instead of inserting a duplicate.")]
        public bool SkipOverClosingChar
        {
            get => (bool)GetValue(SkipOverClosingCharProperty);
            set => SetValue(SkipOverClosingCharProperty, value);
        }

        public static readonly DependencyProperty WrapSelectionInPairsProperty =
            DependencyProperty.Register(nameof(WrapSelectionInPairs), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true));

        [Category("Behavior.Advanced")]
        [DisplayName("Wrap Selection In Pairs")]
        [Description("When text is selected and an opening bracket/quote is typed, surround the selection with the matching pair.")]
        public bool WrapSelectionInPairs
        {
            get => (bool)GetValue(WrapSelectionInPairsProperty);
            set => SetValue(WrapSelectionInPairsProperty, value);
        }

        // ===== BEHAVIOR - SCROLLING & PERFORMANCE =====

        public static readonly DependencyProperty EnableVirtualScrollingProperty =
            DependencyProperty.Register(nameof(EnableVirtualScrolling), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Behavior.Scrolling")]
        [DisplayName("Enable Virtual Scrolling")]
        [Description("Use virtualization to handle large documents (100K+ lines) efficiently")]
        public bool EnableVirtualScrolling
        {
            get => (bool)GetValue(EnableVirtualScrollingProperty);
            set => SetValue(EnableVirtualScrollingProperty, value);
        }

        public static readonly DependencyProperty SmoothScrollingProperty =
            DependencyProperty.Register(nameof(SmoothScrolling), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(false)); // Off by default — instant scroll matches HexEditor/MouseWheelSpeed DP.

        [Category("Behavior.Scrolling")]
        [DisplayName("Smooth Scrolling")]
        [Description("Enable smooth animated scrolling")]
        public bool SmoothScrolling
        {
            get => (bool)GetValue(SmoothScrollingProperty);
            set => SetValue(SmoothScrollingProperty, value);
        }

        public static readonly DependencyProperty ScrollSpeedMultiplierProperty =
            DependencyProperty.Register(nameof(ScrollSpeedMultiplier), typeof(double), typeof(CodeEditor),
                new FrameworkPropertyMetadata(1.0));

        [Category("Behavior.Scrolling")]
        [DisplayName("Scroll Speed Multiplier")]
        [Description("Multiplier for scroll speed (0.5 = slower, 3.0 = faster)")]
        [Range(0.5, 3.0, Step = 0.1)]
        public double ScrollSpeedMultiplier
        {
            get => (double)GetValue(ScrollSpeedMultiplierProperty);
            set => SetValue(ScrollSpeedMultiplierProperty, Math.Max(0.5, Math.Min(3.0, value)));
        }

        public static readonly DependencyProperty MouseWheelSpeedProperty =
            DependencyProperty.Register(nameof(MouseWheelSpeed), typeof(MouseWheelSpeed), typeof(CodeEditor),
                new FrameworkPropertyMetadata(MouseWheelSpeed.System));

        [Category("Behavior.Scrolling")]
        [DisplayName("Mouse Wheel Speed")]
        [Description("Lines scrolled per wheel notch. System = Windows setting (typically 3). VerySlow=1, Slow=3, Normal=5, Fast=7, VeryFast=9.")]
        public MouseWheelSpeed MouseWheelSpeed
        {
            get => (MouseWheelSpeed)GetValue(MouseWheelSpeedProperty);
            set => SetValue(MouseWheelSpeedProperty, value);
        }

        public static readonly DependencyProperty HorizontalScrollSensitivityProperty =
            DependencyProperty.Register(nameof(HorizontalScrollSensitivity), typeof(double), typeof(CodeEditor),
                new FrameworkPropertyMetadata(1.0));

        [Category("Behavior.Scrolling")]
        [DisplayName("Horizontal Scroll Sensitivity")]
        [Description("Sensitivity for horizontal scrolling (0.5 = less sensitive, 3.0 = more sensitive)")]
        [Range(0.5, 3.0, Step = 0.1)]
        public double HorizontalScrollSensitivity
        {
            get => (double)GetValue(HorizontalScrollSensitivityProperty);
            set => SetValue(HorizontalScrollSensitivityProperty, Math.Max(0.5, Math.Min(3.0, value)));
        }

        // -- External scroll control (used by CodeEditorHost) ------------------

        /// <summary>
        /// When true, internal scrollbars are always hidden.
        /// The host (CodeEditorHost) manages scrollbars externally and drives scroll
        /// via <see cref="VerticalScrollOffset"/> and <see cref="HorizontalScrollOffset"/>.
        /// </summary>
        public bool HideScrollBars { get; set; }

        /// <summary>
        /// Gets or sets the vertical scroll offset in pixels.
        /// Setting this triggers a re-render without going through the internal ScrollBar control.
        /// </summary>
        public double VerticalScrollOffset
        {
            get => _verticalScrollOffset;
            set
            {
                _verticalScrollOffset = value;
                _currentScrollOffset  = value;
                _targetScrollOffset   = value;
                if (_virtualizationEngine != null)
                {
                    _virtualizationEngine.ScrollOffset = value;
                    _virtualizationEngine.CalculateVisibleRange();
                }
                SyncVScrollBar();
                InvalidateVisual();
                MinimapRefreshRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the horizontal scroll offset in pixels.
        /// </summary>
        public double HorizontalScrollOffset
        {
            get => _horizontalScrollOffset;
            set { _horizontalScrollOffset = value; SyncHScrollBar(); InvalidateVisual(); }
        }

        /// <summary>Total scrollable content height in pixels (updated each arrange pass).</summary>
        public double TotalContentHeight { get; private set; }

        /// <summary>Total scrollable content width in pixels (updated each arrange pass).</summary>
        public double TotalContentWidth  { get; private set; }

        /// <summary>Current line height in pixels (for SmallChange on external scrollbar).</summary>
        public double LineHeightValue => _lineHeight;

        /// <summary>Current character width in pixels (for SmallChange on external H scrollbar).</summary>
        public double CharWidthValue => _charWidth;

        /// <summary>Raised when TotalContentHeight or TotalContentWidth changes (host updates scrollbars).</summary>
        public event EventHandler? ContentSizeChanged;

        public static readonly DependencyProperty ScrollBarVisibilityModeProperty =
            DependencyProperty.Register(nameof(ScrollBarVisibilityMode), typeof(ScrollBarVisibility), typeof(CodeEditor),
                new FrameworkPropertyMetadata(ScrollBarVisibility.Auto));

        [Category("Behavior.Scrolling")]
        [DisplayName("Scroll Bar Visibility")]
        [Description("When to show scroll bars (Auto, Visible, Hidden, Disabled)")]
        public ScrollBarVisibility ScrollBarVisibilityMode
        {
            get => (ScrollBarVisibility)GetValue(ScrollBarVisibilityModeProperty);
            set => SetValue(ScrollBarVisibilityModeProperty, value);
        }

        public static readonly DependencyProperty RenderBufferProperty =
            DependencyProperty.Register(nameof(RenderBuffer), typeof(int), typeof(CodeEditor),
                new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Behavior.Scrolling")]
        [DisplayName("Render Buffer (extra lines)")]
        [Description("Number of extra lines to render above/below viewport for smooth scrolling")]
        [Range(5, 50)]
        public int RenderBuffer
        {
            get => (int)GetValue(RenderBufferProperty);
            set => SetValue(RenderBufferProperty, Math.Max(5, Math.Min(50, value)));
        }

        public static readonly DependencyProperty MaxCachedLinesProperty =
            DependencyProperty.Register(nameof(MaxCachedLines), typeof(int), typeof(CodeEditor),
                new FrameworkPropertyMetadata(1000));

        [Category("Behavior.Scrolling")]
        [DisplayName("Max Cached Lines")]
        [Description("Maximum number of tokenized lines to keep in cache")]
        [Range(100, 10000)]
        public int MaxCachedLines
        {
            get => (int)GetValue(MaxCachedLinesProperty);
            set => SetValue(MaxCachedLinesProperty, Math.Max(100, Math.Min(10000, value)));
        }

        public static readonly DependencyProperty UseHardwareAccelerationProperty =
            DependencyProperty.Register(nameof(UseHardwareAcceleration), typeof(bool), typeof(CodeEditor),
                new FrameworkPropertyMetadata(true, OnUseHardwareAccelerationChanged));

        [Category("Behavior.Scrolling")]
        [DisplayName("Use Hardware Acceleration")]
        [Description("Enable GPU acceleration for rendering (recommended)")]
        public bool UseHardwareAcceleration
        {
            get => (bool)GetValue(UseHardwareAccelerationProperty);
            set => SetValue(UseHardwareAccelerationProperty, value);
        }

        // ===== SYNTAX HIGHLIGHTING COLORS =====

        public static readonly DependencyProperty SyntaxBraceColorProperty =
            DependencyProperty.Register(nameof(SyntaxBraceColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Brace Color { }")]
        [Description("Color for curly braces")]
        public Brush SyntaxBraceColor
        {
            get => (Brush)GetValue(SyntaxBraceColorProperty);
            set => SetValue(SyntaxBraceColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxBracketColorProperty =
            DependencyProperty.Register(nameof(SyntaxBracketColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Bracket Color [ ]")]
        [Description("Color for square brackets")]
        public Brush SyntaxBracketColor
        {
            get => (Brush)GetValue(SyntaxBracketColorProperty);
            set => SetValue(SyntaxBracketColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxKeyColorProperty =
            DependencyProperty.Register(nameof(SyntaxKeyColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 0, 255)),
                    FrameworkPropertyMetadataOptions.AffectsRender, OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Key Color")]
        [Description("Color for JSON property keys")]
        public Brush SyntaxKeyColor
        {
            get => (Brush)GetValue(SyntaxKeyColorProperty);
            set => SetValue(SyntaxKeyColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxStringValueColorProperty =
            DependencyProperty.Register(nameof(SyntaxStringValueColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(163, 21, 21)),
                    FrameworkPropertyMetadataOptions.AffectsRender, OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("String Value Color")]
        [Description("Color for string values")]
        public Brush SyntaxStringValueColor
        {
            get => (Brush)GetValue(SyntaxStringValueColorProperty);
            set => SetValue(SyntaxStringValueColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxNumberColorProperty =
            DependencyProperty.Register(nameof(SyntaxNumberColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(9, 134, 88)),
                    FrameworkPropertyMetadataOptions.AffectsRender, OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Number Color")]
        [Description("Color for numeric values")]
        public Brush SyntaxNumberColor
        {
            get => (Brush)GetValue(SyntaxNumberColorProperty);
            set => SetValue(SyntaxNumberColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxBooleanColorProperty =
            DependencyProperty.Register(nameof(SyntaxBooleanColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 0, 255)),
                    FrameworkPropertyMetadataOptions.AffectsRender, OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Boolean Color")]
        [Description("Color for true/false values")]
        public Brush SyntaxBooleanColor
        {
            get => (Brush)GetValue(SyntaxBooleanColorProperty);
            set => SetValue(SyntaxBooleanColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxNullColorProperty =
            DependencyProperty.Register(nameof(SyntaxNullColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Null Color")]
        [Description("Color for null values")]
        public Brush SyntaxNullColor
        {
            get => (Brush)GetValue(SyntaxNullColorProperty);
            set => SetValue(SyntaxNullColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxCommentColorProperty =
            DependencyProperty.Register(nameof(SyntaxCommentColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Brushes.Green, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Comment Color")]
        [Description("Color for // and /* */ comments")]
        public Brush SyntaxCommentColor
        {
            get => (Brush)GetValue(SyntaxCommentColorProperty);
            set => SetValue(SyntaxCommentColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxKeywordColorProperty =
            DependencyProperty.Register(nameof(SyntaxKeywordColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 0, 255)),
                    FrameworkPropertyMetadataOptions.AffectsRender, OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Keyword Color")]
        [Description("Color for keywords (signature, field, conditional, loop, action)")]
        public Brush SyntaxKeywordColor
        {
            get => (Brush)GetValue(SyntaxKeywordColorProperty);
            set => SetValue(SyntaxKeywordColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxValueTypeColorProperty =
            DependencyProperty.Register(nameof(SyntaxValueTypeColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(43, 145, 175)),
                    FrameworkPropertyMetadataOptions.AffectsRender, OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Value Type Color")]
        [Description("Color for value types (uint8, int32, string, etc.)")]
        public Brush SyntaxValueTypeColor
        {
            get => (Brush)GetValue(SyntaxValueTypeColorProperty);
            set => SetValue(SyntaxValueTypeColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxCalcExpressionColorProperty =
            DependencyProperty.Register(nameof(SyntaxCalcExpressionColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(128, 0, 128)),
                    FrameworkPropertyMetadataOptions.AffectsRender, OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Calc Expression Color")]
        [Description("Color for calc: expressions")]
        public Brush SyntaxCalcExpressionColor
        {
            get => (Brush)GetValue(SyntaxCalcExpressionColorProperty);
            set => SetValue(SyntaxCalcExpressionColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxVariableReferenceColorProperty =
            DependencyProperty.Register(nameof(SyntaxVariableReferenceColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(128, 0, 128)),
                    FrameworkPropertyMetadataOptions.AffectsRender, OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Variable Reference Color")]
        [Description("Color for var: references")]
        public Brush SyntaxVariableReferenceColor
        {
            get => (Brush)GetValue(SyntaxVariableReferenceColorProperty);
            set => SetValue(SyntaxVariableReferenceColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxErrorColorProperty =
            DependencyProperty.Register(nameof(SyntaxErrorColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Error Color")]
        [Description("Color for syntax errors")]
        public Brush SyntaxErrorColor
        {
            get => (Brush)GetValue(SyntaxErrorColorProperty);
            set => SetValue(SyntaxErrorColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxCommaColorProperty =
            DependencyProperty.Register(nameof(SyntaxCommaColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Comma Color")]
        [Description("Color for commas in JSON")]
        public Brush SyntaxCommaColor
        {
            get => (Brush)GetValue(SyntaxCommaColorProperty);
            set => SetValue(SyntaxCommaColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxColonColorProperty =
            DependencyProperty.Register(nameof(SyntaxColonColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Colon Color")]
        [Description("Color for colons in JSON (key:value separator)")]
        public Brush SyntaxColonColor
        {
            get => (Brush)GetValue(SyntaxColonColorProperty);
            set => SetValue(SyntaxColonColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxEscapeSequenceColorProperty =
            DependencyProperty.Register(nameof(SyntaxEscapeSequenceColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(215, 186, 125)),
                    FrameworkPropertyMetadataOptions.AffectsRender, OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Escape Sequence Color")]
        [Description("Color for escape sequences in strings (\\n, \\t, \\u0000)")]
        public Brush SyntaxEscapeSequenceColor
        {
            get => (Brush)GetValue(SyntaxEscapeSequenceColorProperty);
            set => SetValue(SyntaxEscapeSequenceColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxUrlColorProperty =
            DependencyProperty.Register(nameof(SyntaxUrlColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 102, 204)),
                    FrameworkPropertyMetadataOptions.AffectsRender, OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("URL Color")]
        [Description("Color for URLs detected in string values")]
        public Brush SyntaxUrlColor
        {
            get => (Brush)GetValue(SyntaxUrlColorProperty);
            set => SetValue(SyntaxUrlColorProperty, value);
        }

        public static readonly DependencyProperty SyntaxDeprecatedColorProperty =
            DependencyProperty.Register(nameof(SyntaxDeprecatedColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                    FrameworkPropertyMetadataOptions.AffectsRender, OnSyntaxColorChanged));

        [Category("Syntax Highlighting")]
        [DisplayName("Deprecated Color")]
        [Description("Color for deprecated properties or values (strikethrough)")]
        public Brush SyntaxDeprecatedColor
        {
            get => (Brush)GetValue(SyntaxDeprecatedColorProperty);
            set => SetValue(SyntaxDeprecatedColorProperty, value);
        }

        public static readonly DependencyProperty HighlightMatchColorProperty =
            DependencyProperty.Register(nameof(HighlightMatchColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(80, 255, 255, 0)),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Syntax Highlighting")]
        [DisplayName("Highlight Match Color")]
        [Description("Background color for matching brackets/words highlight")]
        public Brush HighlightMatchColor
        {
            get => (Brush)GetValue(HighlightMatchColorProperty);
            set => SetValue(HighlightMatchColorProperty, value);
        }

        public static readonly DependencyProperty FindResultColorProperty =
            DependencyProperty.Register(nameof(FindResultColor), typeof(Brush), typeof(CodeEditor),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(100, 255, 165, 0)),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        [Category("Syntax Highlighting")]
        [DisplayName("Find Result Color")]
        [Description("Background color for search/find results")]
        public Brush FindResultColor
        {
            get => (Brush)GetValue(FindResultColorProperty);
            set => SetValue(FindResultColorProperty, value);
        }

        #endregion

        #region Property Changed Callbacks

        private static void OnFontChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CodeEditor editor)
            {
                // Update typefaces
                var fontFamily = editor.EditorFontFamily;
                editor._typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                editor._boldTypeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
                editor._baseFontSize = editor.EditorFontSize;
                editor._fontSize     = editor._baseFontSize * editor.ZoomLevel;

                // Recalculate character dimensions
                editor.CalculateCharacterDimensions();
                // Full layout pass: _maxContentWidth and scrollbar ranges depend on _charWidth / _lineHeight
                editor.InvalidateMeasure();
            }
        }

        private static void OnIndentSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CodeEditor editor && editor._document != null)
            {
                editor._document.IndentSize = (int)e.NewValue;
            }
        }

        private static void OnSyntaxColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CodeEditor editor && editor._highlighter != null)
            {
                // Update highlighter colors from DPs
                editor.UpdateSyntaxHighlighterColors();

                // Invalidate all line caches to force re-highlighting
                if (editor._document != null)
                {
                    editor._document.InvalidateAllCache();
                }

                editor.InvalidateVisual();
            }
        }

        private static void OnUseHardwareAccelerationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CodeEditor editor)
            {
                // Update RenderOptions hints for hardware acceleration
                bool useAcceleration = (bool)e.NewValue;

                if (useAcceleration)
                {
                    // Enable hardware acceleration hints
                    RenderOptions.SetBitmapScalingMode(editor, BitmapScalingMode.HighQuality);
                    RenderOptions.SetCachingHint(editor, CachingHint.Cache);
                }
                else
                {
                    // Disable caching hints (forces software rendering)
                    RenderOptions.SetBitmapScalingMode(editor, BitmapScalingMode.Linear);
                    RenderOptions.SetCachingHint(editor, CachingHint.Unspecified);
                }

                editor.InvalidateVisual();
            }
        }

        #endregion
    }
}
