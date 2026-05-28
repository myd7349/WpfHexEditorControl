// ==========================================================
// Project: WpfHexEditor.Editor.CodeEditor
// File: Controls/CodeEditor.Init.cs
// Description: Constructor, initialization helpers, and character dimension calculation for CodeEditor.
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
        #region Constructor

        public CodeEditor()
        {
            // Prevent child visuals (LSP layers, caret DrawingVisual) from rendering
            // outside the control bounds when scroll / InlineHints push Y values past
            // the viewport.  The docking host does not clip its content presenter.
            ClipToBounds = true;


            // Initialize document
            _document = new CodeDocument();
            _selection = new TextSelection();

            // Subscribe to document changes
            _document.TextChanged += Document_TextChanged;

            // Initialize typefaces (will use DP values)
            UpdateTypefacesFromDPs();

            // Calculate character dimensions
            CalculateCharacterDimensions();

            // Initialize syntax highlighter (Phase 2)
            _highlighter = new CodeSyntaxHighlighter();
            UpdateSyntaxHighlighterColors();

            // Initialize undo engine — size from options (or default 500).
            _undoEngine.MaxHistorySize = 500; // updated via ApplyOptions when options are provided
            _undoEngine.MarkSaved();   // Initial state is clean.
            _undoEngine.StateChanged  += OnUndoEngineStateChanged;

            // Initialize SmartComplete popup (Phase 4)
            _smartCompletePopup = new SmartCompletePopup(this);

            // Initialize LSP Signature Help popup
            _signatureHelpPopup     = new SignatureHelpPopup(this);

            // Initialize Go-to-Symbol palette (Ctrl+T)
            _goToSymbolPopup = new GoToSymbolPopup();
            _goToSymbolPopup.NavigationRequested += OnGoToSymbolNavigation;

            // Initialize LSP Code Action popup (Ctrl+.) and Rename popup (F2)
            _lspCodeActionPopup     = new LspCodeActionPopup(this);
            _lspRenamePopup         = new LspRenamePopup(this);

            // Initialize validator (Phase 5)
            _validator = new FormatSchemaValidator();
            _validationTimer = new System.Windows.Threading.DispatcherTimer();
            _validationTimer.Interval = TimeSpan.FromMilliseconds(ValidationDelay);
            _validationTimer.Tick += ValidationTimer_Tick;

            // Folding debounce (P1-CE-01) — 500 ms after last keystroke
            _foldingDebounceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _foldingDebounceTimer.Tick += (_, _) =>
            {
                _foldingDebounceTimer!.Stop();
                if (IsFoldingEnabled && _foldingEngine != null)
                    _foldingEngine.Analyze(_document.Lines);
            };

            // Background highlight pipeline (P1-CE-06) — invalidate & re-render on completion.
            // Suppressed while smooth-scroll is animating to avoid a double-render per frame:
            // timer-tick → InvalidateVisual → OnRender would already redraw; we only need
            // the highlights render once the viewport has settled.
            _highlightPipeline.HighlightsComputed += (_, _) =>
            {
                if (!_smoothScrollTimer.IsEnabled)
                    InvalidateVisual();
                MinimapRefreshRequested?.Invoke(this, EventArgs.Empty);
            };

            // Resolve theme-aware word highlight brushes (fallback to static defaults).
            _wordHighlightBg  = s_wordHighlightBgFallback;
            _wordHighlightPen = s_wordHighlightPenFallback;

            // Make focusable for keyboard input
            Focusable = true;
            FocusVisualStyle = null; // No focus rectangle

            // Set minimum size
            MinWidth = 200;
            MinHeight = 100;

            // Initialize Virtual Scrolling (Phase 11)
            InitializeVirtualScrolling();

            // Initialize Context Menu (Phase C)
            InitializeContextMenu();

            // Initialize caret blink timer
            // Render priority (7) keeps the blink tick above Background LSP diagnostic work (4)
            // so the caret fires reliably even during Roslyn workspace init bursts.
            _caretTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render);
            _caretTimer.Tick += CaretTimer_Tick;
            UpdateCaretBlinkTimer();

            // Multi-caret: any change → redraw so secondary carets appear immediately.
            _caretManager.CaretsChanged += (_, _) => InvalidateVisual();

            // Diagnostics → refresh scroll-marker panel tick marks whenever validation runs.
            DiagnosticsChanged += (_, _) => UpdateDiagnosticScrollMarkers();

            // Initialize smooth scroll timer
            _smoothScrollTimer = new System.Windows.Threading.DispatcherTimer();
            _smoothScrollTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _smoothScrollTimer.Tick += SmoothScrollTimer_Tick;

            // Auto-scroll timer: scrolls the viewport while the mouse is held outside
            // bounds during a drag-selection (50 ms → ~20 scroll steps/s).
            _autoScrollTimer = new System.Windows.Threading.DispatcherTimer();
            _autoScrollTimer.Interval = TimeSpan.FromMilliseconds(50);
            _autoScrollTimer.Tick += AutoScrollTimer_Tick;

            // Middle-click pan mode controller (shared with all scroll-capable editors)
            _panMode = new PanModeController(this,
                (dx, dy) => { ScrollVertical(dy); ScrollHorizontal(dx); });

            // Initialize ScrollBar visual children (vertical + horizontal)
            _scrollBarChildren = new VisualCollection(this);
            _vScrollBar = new System.Windows.Controls.Primitives.ScrollBar
            {
                Orientation = Orientation.Vertical,
                SmallChange  = _lineHeight,
                LargeChange  = 100,
                Minimum      = 0,
                Maximum      = 0,
                Value        = 0,
                Cursor       = Cursors.Arrow  // override parent IBeam
            };
            _hScrollBar = new System.Windows.Controls.Primitives.ScrollBar
            {
                Orientation = Orientation.Horizontal,
                SmallChange  = _charWidth * 3,
                LargeChange  = 100,
                Minimum      = 0,
                Maximum      = 0,
                Value        = 0,
                Cursor       = Cursors.Arrow  // override parent IBeam
            };
            _vScrollBar.ValueChanged += VScrollBar_ValueChanged;
            _hScrollBar.ValueChanged += HScrollBar_ValueChanged;
            _scrollBarChildren.Add(_vScrollBar);
            _scrollBarChildren.Add(_hScrollBar);

            // Initialize folding subsystem (B3).
            // CompositeFoldingStrategy combines brace folding ({}) with #region/#endregion directive folding.
            _foldingEngine = new FoldingEngine(
                new CompositeFoldingStrategy(
                    new BraceFoldingStrategy(),
                    new RegionDirectiveFoldingStrategy()));
            _gutterControl = new GutterControl();
            _gutterControl.SetEngine(_foldingEngine);
            // Fold state change: re-arrange (→ UpdateScrollBars corrects the range) and re-render content.
            // Gutter re-renders internally via its own RegionsChanged handler.
            _foldingEngine.RegionsChanged += (_, _) => { _linePositionsDirty = true; InvalidateMeasure(); InvalidateVisual(); MinimapRefreshRequested?.Invoke(this, EventArgs.Empty); UpdateDiagnosticScrollMarkers(); SyncScrollMarkerCaretAndSelection(); };
            _scrollBarChildren.Add(_gutterControl);

            // Breakpoint gutter (ADR-DBG-01): positioned to the left of fold markers.
            _breakpointGutterControl = new BreakpointGutterControl();
            _breakpointGutterControl.RightClickRequested += OnBreakpointRightClick;
            _breakpointGutterControl.HoverBreakpointRequested += OnGutterBreakpointHover;
            _scrollBarChildren.Add(_breakpointGutterControl);

            // Blame gutter (Phase 2C): 6px color bar left of the breakpoint gutter.
            _blameGutterControl = new BlameGutterControl { Visibility = System.Windows.Visibility.Collapsed };
            _scrollBarChildren.Add(_blameGutterControl);

            // Change-marker gutter (#166): 4px strip between blame and breakpoint gutters.
            _changeMarkerGutterControl = new ChangeMarkerGutterControl();
            _scrollBarChildren.Add(_changeMarkerGutterControl);
            _changeTracker.Changed += (_, map) =>
            {
                _changeMap = map;
                _changeMarkerGutterControl?.Update(
                    _lineHeight, _firstVisibleLine, _lastVisibleLine,
                    TopMargin, _lineYLookup, _changeMap);
            };

            // Initialize word-highlight scroll marker overlay.
            _codeScrollMarkerPanel = new CodeScrollMarkerPanel();
            _scrollBarChildren.Add(_codeScrollMarkerPanel); // renders on top of _vScrollBar

            // Sticky scroll header (#160) — floats above the text area.
            _stickyScrollHeader          = new StickyScrollHeader();
            _stickyScrollHeader.Opacity  = _stickyScrollOpacity;
            _stickyScrollHeader.ScopeClicked += OnStickyScrollScopeClicked;
            _scrollBarChildren.Add(_stickyScrollHeader);

            // LSP overlay layers: inlay hints (parameter names) and declaration hints (test runner hints).
            // Added before caret so caret composites on top.
            _scrollBarChildren.Add(_lspInlayHintsLayer);
            _scrollBarChildren.Add(_lspDeclarationHintsLayer);
            _scrollBarChildren.Add(_semanticTokensLayer);
            _scrollBarChildren.Add(_debugValueHintsLayer);

            // Caret visual is always last so it composites on top of all content.
            _scrollBarChildren.Add(_caretVisual);

            // Debounce timer: update word highlights 250 ms after the caret stops moving.
            // Input priority (5) keeps the tick above Background LSP diagnostic dispatches (4)
            // so frequent diagnostic-driven renders don't starve the word highlight update.
            _wordHighlightTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Input) { Interval = TimeSpan.FromMilliseconds(250) };
            _wordHighlightTimer.Tick += (_, _) => { _wordHighlightTimer.Stop(); UpdateWordHighlights(); };

            // Attach InlineHints service to the initial document.
            _inlineHintsService.HintsDataRefreshed += OnLensDataRefreshed;
            _inlineHintsService.Attach(_document, _currentFilePath);

            // Initialize Quick Info and Ctrl+Click services.
            _hoverQuickInfoService                    = new Services.HoverQuickInfoService();
            _hoverQuickInfoService.QuickInfoResolved  += OnQuickInfoResolved;
            _ctrlClickService                         = new Services.CtrlClickNavigationService();
            _ctrlClickService.TargetResolved          += OnCtrlClickTargetResolved;

            // Fold peek timer — fires 1.5 s after the mouse settles over a fold label.
            _foldPeekTimer          = new System.Windows.Threading.DispatcherTimer
                                      { Interval = TimeSpan.FromMilliseconds(600) };
            _foldPeekTimer.Tick    += OnFoldPeekTimerTick;

            // End-of-block hint timer — fires after hover dwell on a closing token.
            _endBlockHintTimer       = new System.Windows.Threading.DispatcherTimer
                                       { Interval = TimeSpan.FromMilliseconds(600) }; // default; updated via EndOfBlockHintDelayMs DP
            _endBlockHintTimer.Tick += OnEndBlockHintTimerTick;

            // Apply theme resource bindings when connected to the visual tree
            Loaded += (_, _) =>
            {
                ApplyThemeResourceBindings();

                // Wire window-level mouse handlers so auto-scroll continues when the mouse
                // leaves the CodeEditor boundary (e.g. into docking tabs or the title bar).
                // Mouse.AddPreviewMouseMoveHandler uses tunneling — fires even when another
                // element has mouse capture, which is more reliable than relying solely on
                // CaptureMouse() across docking/WindowChrome containers.
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    Mouse.AddPreviewMouseMoveHandler(window, OnWindowPreviewMouseMove);
                    Mouse.AddPreviewMouseUpHandler(window, OnWindowPreviewMouseUp);
                }
            };

            Unloaded += (_, _) =>
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    Mouse.RemovePreviewMouseMoveHandler(window, OnWindowPreviewMouseMove);
                    Mouse.RemovePreviewMouseUpHandler(window, OnWindowPreviewMouseUp);
                }
            };
        }

        private void OnLensDataRefreshed(object? sender, EventArgs e)
        {
            _hintsData           = _inlineHintsService.HintsData;
            RebuildVisibleHintsCount();
            _linePositionsDirty = true; // OPT-D: InlineHints data affects per-line Y offsets
            InvalidateVisual();
        }

        /// <summary>
        /// Returns true when <paramref name="lineIndex"/> has a InlineHints entry whose
        /// <see cref="WpfHexEditor.Editor.Core.InlineHintsSymbolKinds"/> flag matches the
        /// current <see cref="InlineHintsVisibleKinds"/> filter.
        /// </summary>
        private bool IsHintEntryVisible(int lineIndex)
        {
            if (!_hintsData.TryGetValue(lineIndex, out var entry)) return false;
            return (entry.Kind & InlineHintsVisibleKinds) != 0;
        }

        /// <summary>
        /// Recounts filtered InlineHints entries and caches the result in <see cref="_visibleHintsCount"/>.
        /// Must be called whenever <see cref="_hintsData"/> or <see cref="InlineHintsVisibleKinds"/> changes
        /// so that scrollbar height calculations use the correct filtered count.
        /// </summary>
        private void RebuildVisibleHintsCount()
        {
            var kinds = InlineHintsVisibleKinds;
            int count = 0;
            foreach (var entry in _hintsData.Values)
            {
                if ((entry.Kind & kinds) != 0) count++;
            }
            _visibleHintsCount = count;

            // Rebuild cumulative hint array for O(1) scroll-offset → line conversion.
            int lineCount = _document?.Lines.Count ?? 0;
            if (_hintsCumulative.Length != lineCount + 1)
                _hintsCumulative = new int[lineCount + 1];
            int cum = 0;
            for (int i = 0; i < lineCount; i++)
            {
                _hintsCumulative[i] = cum;
                if (_hintsData.TryGetValue(i, out var entry) && (entry.Kind & kinds) != 0)
                    cum++;
            }
            _hintsCumulative[lineCount] = cum;
        }

        /// <summary>
        /// Binds all color DPs to the active theme's CE_* / TE_* resource keys via DynamicResource.
        /// Called on Loaded so the element is connected to the application resource tree.
        /// Safe to call multiple times — subsequent calls just re-register the same keys.
        /// CE_* keys provide Code Editor–specific palette (VS Dark/Light per theme).
        /// TE_* keys are shared with TextEditor for tokens that have no CE_* equivalent.
        /// </summary>
        private void ApplyThemeResourceBindings()
        {
            // Viewport — use CE_* dedicated keys so the code editor tracks its own palette
            SetResourceReference(EditorBackgroundProperty,      "CE_Background");
            SetResourceReference(EditorForegroundProperty,      "CE_Foreground");
            SetResourceReference(LineNumberBackgroundProperty,  "CE_LineNumBg");
            SetResourceReference(LineNumberForegroundProperty,  "CE_LineNumFg");
            SetResourceReference(CurrentLineBackgroundProperty, "CE_CurrentLine");
            SetResourceReference(SelectionBackgroundProperty,          "CE_Selection");
            SetResourceReference(InactiveSelectionBackgroundProperty,  "CE_SelectionInactive");
            SetResourceReference(CaretColorProperty,                   "TE_CaretColor");

            // Syntax highlighting — CE_* for semantically named token colors
            SetResourceReference(SyntaxKeyColorProperty,              "CE_Identifier");
            SetResourceReference(SyntaxKeywordColorProperty,          "CE_Keyword");
            SetResourceReference(SyntaxBooleanColorProperty,          "CE_Keyword");
            SetResourceReference(SyntaxStringValueColorProperty,      "CE_String");
            SetResourceReference(SyntaxNumberColorProperty,           "CE_Number");
            SetResourceReference(SyntaxCommentColorProperty,          "CE_Comment");
            SetResourceReference(SyntaxValueTypeColorProperty,        "CE_Type");
            SetResourceReference(SyntaxCalcExpressionColorProperty,   "TE_Directive");
            SetResourceReference(SyntaxVariableReferenceColorProperty,"CE_Identifier");
            SetResourceReference(SyntaxEscapeSequenceColorProperty,   "CE_String");
            SetResourceReference(SyntaxUrlColorProperty,              "CE_Attribute");
            SetResourceReference(SyntaxBraceColorProperty,            "CE_Brace");
            SetResourceReference(SyntaxBracketColorProperty,          "CE_Bracket");
            SetResourceReference(SyntaxCommaColorProperty,            "CE_Operator");
            SetResourceReference(SyntaxColonColorProperty,            "CE_Operator");
            SetResourceReference(SyntaxNullColorProperty,             "CE_Keyword");
            SetResourceReference(SyntaxDeprecatedColorProperty,       "CE_Operator");
            SetResourceReference(SyntaxErrorColorProperty,            "CE_Error");

            // Apply themed scrollbar style — CodeEditor creates scrollbars in C# before being
            // attached to the visual tree, so the implicit ScrollBar style from the theme
            // ResourceDictionary is not resolved at construction time. Apply it explicitly here,
            // after the element is loaded and the App resource tree is accessible.
            if (TryFindResource(typeof(System.Windows.Controls.Primitives.ScrollBar)) is Style sbStyle)
            {
                if (_vScrollBar != null) _vScrollBar.Style = sbStyle;
                if (_hScrollBar != null) _hScrollBar.Style = sbStyle;
            }

            // Invalidate rainbow scope guide pen cache so it picks up new theme colors.
            _rainbowGuidePens = null;
            _rainbowGuideActivePens = null;

            // Resolve theme-aware word highlight brushes.
            RefreshWordHighlightBrushes();
        }

        /// <summary>
        /// Resolves word highlight brushes from theme resources (CE_WordHighlightBackground,
        /// CE_WordHighlightBorder). Falls back to static defaults when tokens are missing.
        /// </summary>
        private void RefreshWordHighlightBrushes()
        {
            _wordHighlightBg = TryFindResource("CE_WordHighlightBackground") as Brush
                               ?? s_wordHighlightBgFallback;

            var borderBrush = TryFindResource("CE_WordHighlightBorder") as Brush;
            if (borderBrush is not null)
            {
                var pen = new Pen(borderBrush, 1.0);
                if (pen.CanFreeze) pen.Freeze();
                _wordHighlightPen = pen;
            }
            else
            {
                _wordHighlightPen = s_wordHighlightPenFallback;
            }
        }

        /// <summary>
        /// Applies settings from <paramref name="options"/> to this editor instance.
        /// Font, display, and feature settings are applied immediately.
        /// Per-token color overrides in <see cref="CodeEditorOptions.SyntaxColorOverrides"/>
        /// take precedence over the active theme's CE_* resources for as long as the
        /// override is set; clearing an override reverts to the theme resource.
        /// </summary>
        public void ApplyOptions(CodeEditorOptions options)
        {
            if (options is null) return;
            _codeEditorOptions = options;

            // Font / display
            if (!string.IsNullOrEmpty(options.FontFamily))
                EditorFontFamily = new FontFamily(options.FontFamily);

            if (options.FontSize is > 6 and < 72)
                EditorFontSize = options.FontSize;

            ShowLineNumbers          = options.ShowLineNumbers;
            ShowScopeGuides          = options.ShowScopeGuides;
            FoldToggleOnDoubleClick  = options.FoldToggleOnDoubleClick;
            IsWordWrapEnabled        = options.WordWrap;
            EnableFindAllReferences  = options.EnableFindAllReferences;
            ClickableLinksEnabled    = options.ClickableLinksEnabled;
            ClickableEmailsEnabled   = options.ClickableEmailsEnabled;
            ShowInlineHints             = options.ShowInlineHints;
            InlineHintsVisibleKinds     = options.InlineHintsVisibleKinds;
            InlineHintsSource           = options.InlineHintsSource;
            ShowVarTypeHints            = options.ShowVarTypeHints;
            ShowLambdaReturnTypeHints   = options.ShowLambdaReturnTypeHints;
            ShowLspInlayHints           = options.ShowLspInlayHints;
            ShowLspDeclarationHints             = options.ShowLspDeclarationHints;
            EnableWordHighlight      = options.EnableWordHighlight;
            ShowEndOfBlockHint      = options.ShowEndOfBlockHint;
            EndOfBlockHintDelayMs   = options.EndOfBlockHintDelayMs;

            // Auto-indent
            AutoIndentMode = options.AutoIndentMode;

            // Auto-close / smart editing
            EnableAutoClosingBrackets = options.AutoClosingBrackets;
            EnableAutoClosingQuotes   = options.AutoClosingQuotes;
            SkipOverClosingChar       = options.SkipOverClosingChar;
            WrapSelectionInPairs      = options.WrapSelectionInPairs;

            // Bracket pair depth colorization (#162)
            BracketPairColorizationEnabled = options.BracketPairColorization;

            // Rainbow scope guides (bracket-colored folding lines)
            RainbowScopeGuidesEnabled = options.RainbowScopeGuides;

            // Color swatch preview (#168)
            ColorSwatchPreviewEnabled = options.ColorSwatchPreview;

            // Code formatting (#159)
            _formatOnSave              = options.FormatOnSave;
            _xmlAttributeIndentLevels  = options.XmlAttributeIndentLevels ?? 2;
            _xmlOneAttributePerLine    = options.XmlOneAttributePerLine   ?? false;

            // Whitespace markers
            _whitespaceMode = options.WhitespaceMode;

            // Sticky scroll (#160)
            _stickyScrollEnabled         = options.StickyScrollEnabled;
            _stickyScrollMaxLines        = options.StickyScrollMaxLines;
            _stickyScrollSyntaxHighlight = options.StickyScrollSyntaxHighlight;
            _stickyScrollClickToNavigate = options.StickyScrollClickToNavigate;
            _stickyScrollOpacity         = options.StickyScrollOpacity;
            _stickyScrollMinScopeLines   = options.StickyScrollMinScopeLines;
            if (_stickyScrollHeader != null)
            {
                _stickyScrollHeader.Opacity = _stickyScrollOpacity;
                UpdateStickyScrollHeader();
                InvalidateMeasure();
            }

            // Syntax color overrides — set local value to override the DynamicResource binding.
            // A null override clears the local value so DynamicResource (CE_*) takes effect again.
            foreach (var kind in Enum.GetValues<SyntaxTokenKind>())
            {
                var dp    = SyntaxTokenKindToColorProperty(kind);
                var color = options.GetOverride(kind);

                if (dp is null) continue;

                if (color.HasValue)
                    SetValue(dp, new SolidColorBrush(color.Value));
                else
                    ClearValue(dp);  // revert to DynamicResource (CE_*) binding
            }

            // Re-establish DynamicResource bindings for all syntax color DPs.
            // ClearValue() above removes any SetResourceReference binding that
            // ApplyThemeResourceBindings() previously set. Calling it again is
            // idempotent and ensures CE_* theme resources are live for all
            // non-overridden token kinds (overridden ones keep their SetValue).
            ApplyThemeResourceBindings();
        }

        /// <summary>
        /// Applies or removes a per-kind syntax colour override on this editor instance.
        /// Calling with a non-null <paramref name="color"/> sets the DP local value to the
        /// specified brush, which wins over the active theme resource.
        /// Calling with null clears the local value and re-binds the DP to the CE_* theme
        /// resource so the editor tracks theme changes again.
        /// </summary>
        public void SetSyntaxColorOverride(SyntaxTokenKind kind, Color? color)
        {
            var dp = SyntaxTokenKindToColorProperty(kind);
            if (dp is null) return;

            if (color.HasValue)
            {
                SetValue(dp, new SolidColorBrush(color.Value));
            }
            else
            {
                ClearValue(dp);
                // Re-bind to the CE_* theme resource so colour tracks theme changes.
                if (s_kindToResourceKey.TryGetValue(kind, out var key))
                    SetResourceReference(dp, key);
            }
        }

        /// <summary>
        /// Maps a <see cref="SyntaxTokenKind"/> to the corresponding color <see cref="DependencyProperty"/>.
        /// Returns null for token kinds that have no direct color DP.
        /// </summary>
        private static DependencyProperty? SyntaxTokenKindToColorProperty(SyntaxTokenKind kind) => kind switch
        {
            SyntaxTokenKind.Keyword    => SyntaxKeywordColorProperty,
            SyntaxTokenKind.String     => SyntaxStringValueColorProperty,
            SyntaxTokenKind.Number     => SyntaxNumberColorProperty,
            SyntaxTokenKind.Comment    => SyntaxCommentColorProperty,
            SyntaxTokenKind.Type       => SyntaxValueTypeColorProperty,
            SyntaxTokenKind.Identifier => SyntaxKeyColorProperty,
            SyntaxTokenKind.Operator   => SyntaxColonColorProperty,   // maps to operator-class DP
            SyntaxTokenKind.Bracket    => SyntaxBracketColorProperty,
            SyntaxTokenKind.Attribute  => SyntaxUrlColorProperty,      // attribute/annotation DP
            _                          => null
        };

        /// <summary>
        /// Maps each <see cref="SyntaxTokenKind"/> directly to the CE_* theme resource key
        /// used in <see cref="ResolveBrushForKind"/>.
        /// Using TryFindResource bypasses the DP local-value layer (SetValue/ClearValue),
        /// so ApplyOptions() overrides or ClearValue calls never corrupt syntax colors.
        /// </summary>
        private static readonly Dictionary<SyntaxTokenKind, string> s_kindToResourceKey =
            new()
            {
                { SyntaxTokenKind.Keyword,    "CE_Keyword"    },
                { SyntaxTokenKind.String,     "CE_String"     },
                { SyntaxTokenKind.Number,     "CE_Number"     },
                { SyntaxTokenKind.Comment,    "CE_Comment"    },
                { SyntaxTokenKind.Type,       "CE_Type"       },
                { SyntaxTokenKind.Identifier, "CE_Identifier" },
                { SyntaxTokenKind.Operator,   "CE_Operator"   },
                { SyntaxTokenKind.Bracket,    "CE_Bracket"    },
                { SyntaxTokenKind.Attribute,   "CE_Attribute"   },
                { SyntaxTokenKind.ControlFlow, "CE_ControlFlow" },
            };

        /// <summary>
        /// Resolves the live theme brush for a <see cref="SyntaxTokenKind"/> by calling
        /// <see cref="FrameworkElement.TryFindResource"/> with the matching CE_* key.
        /// This walks the logical tree (CodeEditor → Window → Application), guaranteeing
        /// the current theme color is returned regardless of any DP local-value interference
        /// (e.g. from <see cref="ApplyOptions"/> calling ClearValue or SetValue).
        /// Returns <see langword="null"/> for <see cref="SyntaxTokenKind.Default"/>,
        /// letting callers fall back to any baked-in brush stored on the token itself.
        /// </summary>
        private Brush? ResolveBrushForKind(SyntaxTokenKind kind)
            => s_kindToResourceKey.TryGetValue(kind, out var key)
                ? TryFindResource(key) as Brush
                : null;

        private void UpdateTypefacesFromDPs()
        {
            _typeface           = new Typeface(EditorFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            _boldTypeface       = new Typeface(EditorFontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            _lineNumberTypeface = new Typeface(LineNumberFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            _baseFontSize       = EditorFontSize;
            _fontSize           = _baseFontSize * ZoomLevel;
        }

        #region Visual Children (ScrollBars)

        protected override int VisualChildrenCount => _scrollBarChildren?.Count ?? 0;

        protected override Visual GetVisualChild(int index) => _scrollBarChildren[index];

        #endregion

        private void UpdateSyntaxHighlighterColors()
        {
            if (_highlighter == null) return;

            _highlighter.DefaultColor = EditorForeground; // Default text color
            _highlighter.BraceColor = SyntaxBraceColor;
            _highlighter.BracketColor = SyntaxBracketColor;
            _highlighter.KeyColor = SyntaxKeyColor;
            _highlighter.StringValueColor = SyntaxStringValueColor;
            _highlighter.NumberColor = SyntaxNumberColor;
            _highlighter.BooleanColor = SyntaxBooleanColor;
            _highlighter.NullColor = SyntaxNullColor;
            _highlighter.CommentColor = SyntaxCommentColor;
            _highlighter.KeywordColor = SyntaxKeywordColor;
            _highlighter.ValueTypeColor = SyntaxValueTypeColor;
            _highlighter.CalcExpressionColor = SyntaxCalcExpressionColor;
            _highlighter.VariableReferenceColor = SyntaxVariableReferenceColor;
            _highlighter.ErrorColor = SyntaxErrorColor;

            // Phase 10.5: Additional syntax colors
            _highlighter.CommaColor = SyntaxCommaColor;
            _highlighter.ColonColor = SyntaxColonColor;

            // Phase 100%: Escape sequences, URLs, and deprecated keywords
            _highlighter.EscapeSequenceColor = SyntaxEscapeSequenceColor;
            _highlighter.UrlColor = SyntaxUrlColor;
            _highlighter.DeprecatedColor = SyntaxDeprecatedColor;
        }

        /// <summary>
        /// Update caret blink timer based on CaretBlinkRate DP
        /// </summary>
        private void UpdateCaretBlinkTimer()
        {
            if (_caretTimer == null) return;

            int blinkRate = CaretBlinkRate;

            if (blinkRate <= 0)
            {
                // No blinking - always visible
                _caretTimer.Stop();
                _caretVisible = true;
                InvalidateVisual();
            }
            else
            {
                _caretTimer.Interval = TimeSpan.FromMilliseconds(blinkRate);
                _caretTimer.Start();
                _caretVisible = true;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Redraws the caret DrawingVisual only — zero cost to the main OnRender pipeline.
        /// Must be called after every caret state change (blink toggle, position change, focus change).
        /// Also called at the end of OnRender to sync the caret over freshly rendered content.
        /// </summary>
        private void RenderCaretVisual()
        {
            if (_caretVisual is null || !IsLoaded) return;
            if (ActualWidth <= 0 || ActualHeight <= 0) return;

            using var dc = _caretVisual.RenderOpen();

            if (_document == null || _document.Lines.Count == 0)
                return;

            bool hasVBar = _vScrollBar?.Visibility == Visibility.Visible;
            bool hasHBar = _hScrollBar?.Visibility == Visibility.Visible;
            double contentW = Math.Max(0, ActualWidth  - (hasVBar ? ScrollBarThickness : 0));
            double contentH = Math.Max(0, ActualHeight - (hasHBar ? ScrollBarThickness : 0));
            double textLeft = ShowLineNumbers ? TextAreaLeftOffset : LeftMargin;

            // Mirror the exact clip + transform stack active when RenderCursor is called in OnRender.
            dc.PushClip(new System.Windows.Media.RectangleGeometry(
                new Rect(textLeft, 0, Math.Max(0, contentW - textLeft), contentH)));
            dc.PushTransform(new System.Windows.Media.TranslateTransform(-_horizontalScrollOffset, 0));

            RenderCursor(dc);

            dc.Pop(); // H-scroll transform
            dc.Pop(); // text-area clip
        }

        /// <summary>
        /// Caret blink timer tick handler — redraws only the caret DrawingVisual.
        /// Does NOT trigger InvalidateVisual on the main FrameworkElement.
        /// </summary>
        private void CaretTimer_Tick(object sender, EventArgs e)
        {
            _caretVisible = !_caretVisible;
            RenderCaretVisual();
        }

        /// <summary>
        /// Reset caret to visible and restart blink timer
        /// Called when user types or moves cursor
        /// </summary>
        private void ResetCaretBlink()
        {
            _caretVisible = true;
            if (_caretTimer != null && _caretTimer.IsEnabled)
            {
                _caretTimer.Stop();
                _caretTimer.Start();
            }
            RenderCaretVisual();
            SyncScrollMarkerCaretAndSelection();
        }

        /// <summary>
        /// Called from all cursor-movement sites (MoveCursor, mouse clicks) that do not
        /// trigger a full OnRender. Schedules the word-highlight debounce timer so the
        /// highlight updates without waiting for the next render frame.
        /// </summary>
        private void NotifyCursorMoved() => ScheduleWordHighlightUpdate();

        /// <summary>
        /// Pushes caret + selection positions to the scroll marker panel.
        /// Called when either changes — not from OnRender — to avoid per-frame panel redraws.
        /// </summary>
        private void SyncScrollMarkerCaretAndSelection()
        {
            if (_codeScrollMarkerPanel == null || _document == null) return;
            // BUG3-FIX: use visible (fold-compressed) indices so the caret and selection
            // markers on the scroll panel align with the scrollbar, which operates in
            // visible-line space (hidden lines are excluded from the pixel budget).
            bool hasSelection = !_selection.IsEmpty && _selection.NormalizedStart.Line != _selection.NormalizedEnd.Line;
            _codeScrollMarkerPanel.UpdateCaretAndSelection(
                PhysicalToVisibleLineIndex(_cursorLine),
                hasSelection ? PhysicalToVisibleLineIndex(_selection.NormalizedStart.Line) : -1,
                hasSelection ? PhysicalToVisibleLineIndex(_selection.NormalizedEnd.Line)   : -1,
                VisibleLineCount);
        }

        /// <summary>
        /// Smooth scroll timer tick handler - interpolates scroll position
        /// </summary>
        private void SmoothScrollTimer_Tick(object sender, EventArgs e)
        {
            if (!SmoothScrolling)
            {
                _smoothScrollTimer.Stop();
                return;
            }

            // Interpolate between current and target offset
            double diff = _targetScrollOffset - _currentScrollOffset;

            if (Math.Abs(diff) < 0.5)
            {
                // Close enough - snap to target and stop.
                _currentScrollOffset  = _targetScrollOffset;
                _verticalScrollOffset = _targetScrollOffset;
                _smoothScrollTimer.Stop();

                // Reset tracking so the next OnRender schedules a highlight pass for
                // the newly settled visible range (highlighting was suppressed during scroll).
                _lastHighlightFirst = -1;
                _lastHighlightLast  = -1;
            }
            else
            {
                // Interpolate (ease out)
                _currentScrollOffset += diff * SmoothScrollSpeed;
                _verticalScrollOffset = _currentScrollOffset;
            }

            // Update virtualization engine and repaint
            if (_virtualizationEngine != null)
            {
                _virtualizationEngine.ScrollOffset = _verticalScrollOffset;
                _virtualizationEngine.CalculateVisibleRange();
            }

            SyncVScrollBar();
            InvalidateVisual();
        }

        /// <summary>
        /// Auto-scroll timer tick handler.
        /// Fires at 50 ms intervals while the mouse is held outside the viewport
        /// during a drag-selection. Scrolls the viewport and extends the selection
        /// to the clamped text position matching the mouse location.
        /// </summary>
        private void AutoScrollTimer_Tick(object sender, EventArgs e)
        {
            if (!_isSelecting && !_isRectSelecting)
            {
                _autoScrollTimer.Stop();
                return;
            }

            double mouseY = _lastMousePosition.Y;

            // Scroll speed uses the same MouseWheelSpeed DP as the wheel handler so
            // the auto-scroll rate is consistent with the user's wheel preference.
            int    speedLines = MouseWheelSpeed == MouseWheelSpeed.System
                ? SystemParameters.WheelScrollLines
                : (int)MouseWheelSpeed;
            double maxDelta   = speedLines * _lineHeight;

            // Guarantee at least 1 line per tick then accelerate up to maxDelta over 80 px
            // of overshoot. The previous proportional formula (overshoot/height * max) produced
            // near-zero deltas when the mouse was just barely outside the viewport.
            double delta = 0;
            if (mouseY < 0)
            {
                double lines = Math.Clamp(1.0 + (-mouseY) / 80.0, 1.0, speedLines);
                delta = -lines * _lineHeight;
            }
            else if (mouseY > ActualHeight)
            {
                double lines = Math.Clamp(1.0 + (mouseY - ActualHeight) / 80.0, 1.0, speedLines);
                delta = lines * _lineHeight;
            }

            if (delta != 0)
                ScrollVertical(delta);

            // Extend selection to the clamped text position under the mouse.
            double clampedY = Math.Max(0, Math.Min(ActualHeight - 1, mouseY));
            var    textPos  = PixelToTextPosition(new Point(_lastMousePosition.X, clampedY));

            if (textPos != _selection.End)
            {
                _selection.End = textPos;
                _cursorLine    = textPos.Line;
                _cursorColumn  = textPos.Column;
                InvalidateVisual();
                NotifyCaretMovedIfChanged();
            }
        }

        #endregion

        #region Character Dimension Calculation

        /// <summary>
        /// Calculates character dimensions and (re)creates the <see cref="GlyphRunRenderer"/>
        /// for the current font, size and display DPI.
        /// </summary>
        private void CalculateCharacterDimensions()
        {
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            // Rebuild GlyphRunRenderer — this also caches char width / height
            // from the GlyphTypeface (fast path) or FormattedText (fallback).
            _glyphRenderer = new GlyphRunRenderer(
                _typeface, _boldTypeface, _fontSize, pixelsPerDip);

            _charWidth  = _glyphRenderer.CharWidth;
            _charHeight = _glyphRenderer.CharHeight;
            _lineHeight = (_charHeight + 2) * LineHeightMultiplier;

            // Font changed — line-number cache and GlyphRun cache are stale (P1-CE-03/05)
            _lineNumberCache.Clear();
            if (_document != null)
                foreach (var line in _document.Lines)
                { line.GlyphRunCache = null; line.IsGlyphCacheDirty = true; }
        }

        /// <summary>
        /// O(n) full rebuild of <see cref="_cachedMaxLineLength"/>. Called only at load time
        /// or document swap — not on every keystroke (P1-CE-02).
        /// </summary>
        private void RebuildMaxLineLength()
        {
            _cachedMaxLineLength = _document?.Lines.Count > 0
                ? _document.Lines.Max(l => l.Text.Length)
                : 0;
        }


        #endregion
    }
}
