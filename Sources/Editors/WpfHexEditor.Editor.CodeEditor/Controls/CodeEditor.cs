//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Custom CodeEditor - Main Editor Control (Phase 1 - Foundation)
// Author : Claude Sonnet 4.5
// Contributors: Derek Tremblay (derektremblay666@gmail.com), Claude Sonnet 4.6
// Inspired by HexViewport.cs custom rendering pattern
//////////////////////////////////////////////

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
    /// <summary>
    /// High-performance JSON text editor using custom rendering (FrameworkElement).
    /// Phase 1: Basic text display + keyboard input + line numbers
    /// Phase 2: Syntax highlighting with CodeSyntaxHighlighter
    /// Future phases will add: SmartComplete, validation
    /// </summary>
    public partial class CodeEditor : FrameworkElement, IDocumentEditor, IBufferAwareEditor, IUndoAwareEditor, ILspAwareEditor, IDiagnosticSource, IPropertyProviderSource, IOpenableDocument, INavigableDocument, IStatusBarContributor, IRefreshTimeReporter, ISearchTarget, IEditorPersistable
    {
        #region Fields - Document Model

        private CodeDocument _document;

        // -- IBufferAwareEditor -------------------------------------------
        private IDocumentBuffer? _buffer;
        private bool             _suppressBufferSync;
        private int _cursorLine = 0;        // Current cursor line (0-based)
        private int _cursorColumn = 0;      // Current cursor column (0-based)
        private TextSelection _selection;   // Current text selection
        private int _lastNotifiedCursorLine = -1; // Tracks last CaretMoved notification line

        // Multi-caret manager — index 0 is always the primary caret.
        private readonly CaretManager _caretManager = new();

        #endregion

        #region Fields - Syntax Highlighting (Phase 2)

        private CodeSyntaxHighlighter _highlighter;

        // Link hit-zones (URLs + emails): rebuilt on every render pass; used for cursor + Ctrl+Click.
        private readonly List<LinkHitZone> _linkHitZones = new();

        // Fold-label hit-zones: rebuilt on every render pass; used for click-to-toggle.
        private readonly List<(Rect rect, int line)> _foldLabelHitZones = new();

        // Fold peek — VS-style hover preview of collapsed region content.
        private FoldPeekPopup?   _foldPeekPopup;
        private System.Windows.Threading.DispatcherTimer? _foldPeekTimer;
        private int              _foldPeekTargetLine = -1;

        // End-of-block hint popup — shown on hover over }, #endregion, </Tag> etc.
        private EndBlockHintPopup? _endBlockHintPopup;
        private System.Windows.Threading.DispatcherTimer? _endBlockHintTimer;
        private int                _endBlockHintHoveredLine = -1;
        private FoldingRegion?     _endBlockHintActiveRegion;

        // The link zone (URL or email) currently under the mouse pointer (null = none).
        // Drives hover underline; changing it triggers InvalidateVisual().
        private LinkHitZone? _hoveredLinkZone;

        // Explicit tooltip object opened/closed in OnMouseMove.
        // Using ToolTip directly (instead of the ToolTip property) ensures the tooltip
        // appears even when the mouse is already inside the CodeEditor control.
        private ToolTip? _urlTooltip;
        private ToolTip? _hintTooltip;

        // Compiled URL regex — re-used across all render passes (thread-safe read-only after init).
        private static readonly Regex s_urlRegex = new(
            @"https?://[^\s""'<>\[\]{}|\\^`]+",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Compiled email regex — same lifetime guarantee as s_urlRegex.
        private static readonly Regex s_emailRegex = new(
            @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
            RegexOptions.Compiled);

        /// <summary>
        /// Optional external syntax highlighter (e.g. RegexBasedSyntaxHighlighter for .whlang languages).
        /// When set, overrides the built-in JSON highlighter for all text rendering.
        /// </summary>
        public static readonly DependencyProperty ExternalHighlighterProperty =
            DependencyProperty.Register(nameof(ExternalHighlighter), typeof(ISyntaxHighlighter),
                typeof(CodeEditor), new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnExternalHighlighterChanged));

        public ISyntaxHighlighter? ExternalHighlighter
        {
            get => (ISyntaxHighlighter?)GetValue(ExternalHighlighterProperty);
            set => SetValue(ExternalHighlighterProperty, value);
        }

        /// <summary>
        /// The active syntax highlighter: <see cref="ExternalHighlighter"/> if set,
        /// otherwise the built-in <see cref="CodeSyntaxHighlighter"/>.
        /// Used by the minimap for on-demand token fallback when TokensCache is not ready.
        /// </summary>
        public ISyntaxHighlighter? ActiveHighlighter
            => (ISyntaxHighlighter?)ExternalHighlighter ?? _highlighter;

        private static void OnExternalHighlighterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CodeEditor editor) return;

            // Reset block-comment state carried over from the previous file.
            if (e.OldValue is ISyntaxHighlighter old) old.Reset();
            if (e.NewValue is ISyntaxHighlighter h)   h.Reset();

            // Invalidate all token caches so the pipeline re-highlights with the new highlighter.
            // Required for bracket colorization: new highlighter may produce different token Kinds
            // (e.g. Kind.Bracket from the synthetic bracket rule added in BuildHighlighter).
            // Without this, IsCacheDirty stays false and ColorizeLine never sees Kind.Bracket tokens.
            editor._document?.InvalidateAllCache();

            // Force a full repaint so the new (or cleared) highlighter is applied
            // to all currently visible lines immediately, without waiting for a scroll.
            editor.InvalidateVisual();
            editor.RefreshJsonStatusBarItems();
        }

        #endregion

        #region Fields - Undo/Redo

        private readonly WpfHexEditor.Editor.Core.Undo.UndoEngine _undoEngine = new();
        private bool _isInternalEdit = false; // Prevent undo recording during undo/redo
        private bool _isDirty = false;        // IDocumentEditor: unsaved changes flag
        private bool _isDocumentLoaded = false; // True once content has been fully loaded into the buffer
        private string? _currentFilePath;     // IDocumentEditor: last saved file path

        /// <inheritdoc/>
        public Action<string>? BeforeSaveCallback { get; set; }

        // Context-menu items that need dynamic headers (Undo (3) / Redo (0)).
        private System.Windows.Controls.MenuItem? _undoMenuItem;
        private System.Windows.Controls.MenuItem? _redoMenuItem;

        #endregion

        #region Fields - Mouse Selection (Phase 3)

        private bool _isSelecting = false;
        private bool _isOverwriteMode = false;
        private TextPosition _mouseDownPosition;

        // Coalesces InvalidateVisual() calls during mouse-drag selection.
        // Prevents dispatcher queue flooding at high mouse-event rates (200–1000 Hz).
        private bool _selectionRenderPending;

        // Auto-scroll during drag: fires at 50 ms intervals when the mouse is outside
        // the visible viewport while a selection drag is in progress.
        private System.Windows.Threading.DispatcherTimer _autoScrollTimer;
        private Point _lastMousePosition;

        // Feature A — Rectangular (block/column) selection (Alt+LeftClick+drag)
        private readonly RectangularSelection _rectSelection = new();
        private bool _isRectSelecting;

        // Feature B — Text drag-and-drop (move selection by dragging)
        private readonly DragDropState _dragDrop = new();
        private bool _isRectDrag; // true when the active drag originates from a rect selection block

        // Middle-click auto-scroll (pan mode)
        private PanModeController _panMode = null!;

        #endregion

        #region Fields - SmartComplete (Phase 4)

        private SmartCompletePopup _smartCompletePopup;
        private bool _enableSmartComplete = true;

        #endregion

        #region Fields - LSP Client (Phase 4 — LSP Integration)

        // Optional LSP client injected by the IDE host (via SetLspClient).
        // Null when no language server is configured for the current language.
        private WpfHexEditor.Editor.Core.LSP.ILspClient? _lspClient;

        // Signature help popup — shown on '(' keystroke when an LSP client is active.
        private SignatureHelpPopup?     _signatureHelpPopup;

        // Code action popup — shown on Ctrl+. when an LSP client is active.
        private LspCodeActionPopup?     _lspCodeActionPopup;

        // Rename popup — shown on F2 when an LSP client is active.
        private LspRenamePopup?         _lspRenamePopup;

        // Document manager injected by LspDocumentBridgeService for workspace-wide edits.
        private IDocumentManager?       _lspDocumentManager;

        // Monotonically increasing document version sent with every didChange notification.
        private int _lspDocVersion;

        // Tracks nested call depth for SignatureHelp — incremented on '(', decremented on ')'.
        // When it reaches 0 the SignatureHelp popup is dismissed.
        private int _signatureParamDepth;

        // Lightbulb gutter: -1 = no bulb; ≥0 = line index where code actions are available.
        private int _lightbulbLine = -1;
        // Debounce timer for CheckCodeActionsAtCursorAsync (600 ms after cursor movement).
        private System.Windows.Threading.DispatcherTimer? _lightbulbTimer;

        // Go-to-Symbol palette (Ctrl+T)
        private GoToSymbolPopup? _goToSymbolPopup;

        // Debounce timer for textDocument/didChange — 300 ms after last keystroke.
        private System.Windows.Threading.DispatcherTimer? _lspChangeTimer;

        // Pending incremental change for the next timer tick.
        // Null = multiple changes collapsed → use full-text DidChange fallback.
        private (int SL, int SC, int EL, int EC, int OldLen, string NewText)? _pendingLspChange;

        // Inline "Find All References" popup (lazily created on first use).
        private ReferencesPopup?           _referencesPopup;

        // Last reference results — used when the user pins the popup to a docked panel.
        private List<ReferenceGroup> _lastReferenceGroups = new();
        private string               _lastReferenceSymbol = string.Empty;

        // ── Bracket Pair Colorization (#162) ────────────────────────────────────
        private readonly Services.BracketDepthColorizer _bracketColorizer      = new();
        private          int                            _bracketDepthFirstLine = -1;

        // ── InlineHints ──────────────────────────────────────────────────────────
        private          int                            _inlineHintsSource   = 0; // 0=Auto, 1=RoslynOnly, 2=RegexAlways
        private readonly Services.InlineHintsService                                                                                              _inlineHintsService  = new();
        private readonly Layers.LspInlayHintsLayer                                                                                                 _lspInlayHintsLayer      = new();
        private readonly Layers.LspDeclarationHintsLayer                                                                                                   _lspDeclarationHintsLayer    = new();
        private readonly Layers.LspSemanticTokensLayer                                                                                                     _semanticTokensLayer         = new();
        private readonly Layers.DebugValueHintsLayer                                                                                                       _debugValueHintsLayer        = new();
        private          IReadOnlyDictionary<int, (int Count, string Symbol, string IconGlyph, System.Windows.Media.Brush IconBrush, WpfHexEditor.Editor.Core.InlineHintsSymbolKinds Kind, bool IsRoslyn)> _hintsData = new Dictionary<int, (int, string, string, System.Windows.Media.Brush, WpfHexEditor.Editor.Core.InlineHintsSymbolKinds, bool)>();
        private          int                                                                                                                   _visibleHintsCount = 0;
        /// <summary>Cumulative hint count before each line: _hintsCumulative[i] = number of visible hints on lines 0..i-1.</summary>
        private          int[]                                                                                                                  _hintsCumulative = System.Array.Empty<int>();
        private readonly List<(Rect Zone, int LineIndex, string Symbol)>                                                                       _hintsHitZones     = new();
        private readonly List<(int LineIndex, double Y)>                                                                                       _visLinePositions = new();
        private readonly List<int>                                                                                                              _visLineSubRows   = new(); // parallel to _visLinePositions — sub-row index within the logical line (word wrap)
        private readonly Dictionary<int, double>                                                                                               _lineYLookup      = new();
        private          int                                                                                                                   _hoveredHintsLine  = -1;

        // ── Quick Info Hover ──────────────────────────────────────────────────
        private Services.HoverQuickInfoService?      _hoverQuickInfoService;
        private QuickInfoPopup?                      _quickInfoPopup;
        private Point                                _lastHoverPixel;
        private TextPosition                         _lastHoverTextPos = new(-1, -1);

        // ── Ctrl+Click Navigation ─────────────────────────────────────────────
        private Services.CtrlClickNavigationService? _ctrlClickService;
        private bool                                 _ctrlDown;
        private readonly List<SymbolHitZone>         _symbolHitZones    = new();
        private SymbolHitZone?                       _hoveredSymbolZone;

        /// <summary>
        /// Routed command for "Find All References" — default gesture Shift+F12,
        /// matching the Visual Studio keyboard binding.
        /// </summary>
        public static readonly RoutedUICommand FindAllReferencesCommand = new(
            CodeEditorResources.CodeCtx_FindAllReferences,
            "FindAllReferences",
            typeof(CodeEditor),
            new InputGestureCollection { new KeyGesture(Key.F12, ModifierKeys.Shift) });

        /// <summary>
        /// Routed command for "Select Next Occurrence" — Ctrl+D (VS Code behaviour).
        /// Adds the next occurrence of the word/selection as a secondary caret selection.
        /// </summary>
        public static readonly RoutedUICommand SelectNextOccurrenceCommand = new(
            "Select Next Occurrence",
            "SelectNextOccurrence",
            typeof(CodeEditor),
            new InputGestureCollection { new KeyGesture(Key.D, ModifierKeys.Control) });

        /// <summary>
        /// Routed command for "Refresh Highlights" — Ctrl+Shift+R.
        /// Forces an immediate clear and re-request of all highlight layers.
        /// </summary>
        public static readonly RoutedUICommand RefreshHighlightsCommand = new(
            CodeEditorResources.CodeCtx_RefreshHighlights,
            "RefreshHighlights",
            typeof(CodeEditor),
            new InputGestureCollection { new KeyGesture(Key.R, ModifierKeys.Control | ModifierKeys.Shift) });

        #endregion

        #region Fields - Validation (Phase 5)

        private List<Models.ValidationError> _validationErrors = new List<Models.ValidationError>();
        // O(1) lookup for RenderValidationGlyph — rebuilt whenever _validationErrors changes (OPT-PERF-01).
        private Dictionary<int, List<Models.ValidationError>> _validationByLine = new();
        private FormatSchemaValidator _validator;
        private System.Windows.Threading.DispatcherTimer _validationTimer;

        // Cached frozen pens for squiggly-line rendering — avoids per-error Pen allocations (OPT-PERF-02).
        private static readonly Pen s_squigglyError   = MakeSquigglyPen(Colors.Red);
        private static readonly Pen s_squigglyWarning = MakeSquigglyPen(Color.FromRgb(255, 165, 0));
        private static readonly Pen s_squigglyInfo    = MakeSquigglyPen(Colors.Blue);

        private static readonly Pen s_lineNumberSeparatorPen = MakeFrozenPen(Color.FromRgb(200, 200, 200), 1.0);
        private static readonly Pen s_glyphInnerPen          = MakeFrozenPen(Colors.White, 1.5);

        // Fold-collapse inline label rendering assets.
        // Static brushes are used as fallback when theme tokens are absent.
        private static readonly Brush    s_foldLabelPenBrush  = MakeFrozenBrush(Color.FromRgb(86, 156, 214));   // VS blue border
        private static readonly Brush    s_foldLabelTextBrush = MakeFrozenBrush(Color.FromRgb(200, 200, 200));
        private static readonly Brush    s_foldLabelBgBrush   = MakeFrozenBrush(Color.FromArgb(30, 86, 156, 214)); // translucent blue
        private static readonly Typeface s_foldLabelTypeface  = new("Segoe UI");

        // Scope guide line pen — semi-transparent so it doesn't obscure text.
        private static readonly Pen s_scopeGuidePen = MakeFrozenPen(Color.FromArgb(60, 128, 128, 128), 1.0);

        // Active (cursor-containing) scope guide — thicker + more opaque than the passive one.
        private static readonly Pen s_scopeGuideActivePen = MakeFrozenPen(Color.FromArgb(140, 180, 180, 180), 1.5);

        // Word-under-caret highlight assets — resolved from theme resources (CE_WordHighlightBackground / CE_WordHighlightBorder).
        // Static fallbacks used when theme tokens are missing.
        private static readonly Brush s_wordHighlightBgFallback  = MakeFrozenBrush(Color.FromArgb(26, 86, 156, 214));
        private static readonly Pen   s_wordHighlightPenFallback = MakeFrozenPen(Color.FromArgb(180, 86, 156, 214), 1.0);
        private Brush _wordHighlightBg;
        private Pen   _wordHighlightPen;

        // Bracket matching highlight assets — fixed colors, safe as static frozen fields (OPT-PERF-03).
        private static readonly Brush s_bracketHighlightBrush = MakeFrozenBrush(Color.FromArgb(80, 0, 120, 215));
        private static readonly Pen   s_bracketBorderPen      = MakeFrozenPen(Color.FromRgb(0, 120, 215), 1.5);

        private static Pen MakeSquigglyPen(Color color) => MakeFrozenPen(color, 1.5);

        private static Pen MakeFrozenPen(Color color, double thickness)
        {
            var pen = new Pen(new SolidColorBrush(color), thickness);
            pen.Brush.Freeze();
            pen.Freeze();
            return pen;
        }

        private static Brush MakeFrozenBrush(Color color)
        {
            var b = new SolidColorBrush(color);
            b.Freeze();
            return b;
        }

        /// <summary>Creates a theme-aware Segoe MDL2 Assets icon TextBlock for context menu items.</summary>
        private static TextBlock MakeMenuIcon(string glyph)
        {
            var tb = new TextBlock
            {
                Text                = glyph,
                FontFamily          = new FontFamily("Segoe MDL2 Assets"),
                FontSize            = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            };
            tb.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "DockMenuForegroundBrush");
            return tb;
        }

        #endregion

        #region Fields - Virtual Scrolling (Phase 11)

        private VirtualizationEngine _virtualizationEngine;

        /// <summary>Exposes virtualization engine for minimap viewport tracking.</summary>
        internal VirtualizationEngine VirtualizationEngine => _virtualizationEngine;

        private double _verticalScrollOffset = 0;
        private double _horizontalScrollOffset = 0;
        private double _maxContentWidth = 0;

        #endregion

        #region Fields - Word Wrap (ADR-049)

        private int   _charsPerVisualLine;
        private int[] _wrapHeights  = Array.Empty<int>(); // visual rows per logical line
        private int[] _wrapOffsets  = Array.Empty<int>(); // first visual row of logical line i (prefix sum)
        private int   _totalVisualRows;
        private double _lastWrapArrangedWidth = -1;

        #endregion

        #region Fields - ScrollBar Children

        private System.Windows.Controls.Primitives.ScrollBar _vScrollBar;
        private System.Windows.Controls.Primitives.ScrollBar _hScrollBar;
        private VisualCollection _scrollBarChildren;
        private bool _updatingScrollBar = false;

        #endregion

        #region Fields - Smooth Scrolling

        private System.Windows.Threading.DispatcherTimer _smoothScrollTimer;
        private double _targetScrollOffset = 0;
        private double _currentScrollOffset = 0;
        private const double SmoothScrollSpeed = 0.35; // Interpolation factor (0-1) — higher = snappier

        #endregion

        #region Fields - Find/Replace

        private List<TextPosition> _findResults = new List<TextPosition>();
        private int _currentFindMatchIndex = -1;
        private int _findMatchLength = 0;
        private string? _lastFindQuery;

        #endregion

        #region Fields - Word Highlight

        private readonly List<TextPosition> _wordHighlights           = new();
        // OPT-PERF: pre-computed distinct line list reused by scroll marker panel — avoids
        // Select().Distinct().ToList() allocation on every cursor move.
        private readonly List<int>          _wordHighlightLines       = new();
        private readonly HashSet<int>       _wordHighlightLineSet     = new();
        private string                      _wordHighlightWord        = string.Empty;
        private int                         _wordHighlightLen         = 0;
        private int                         _wordHighlightTrackedLine = -1; // last cursor line seen in OnRender
        private int                         _wordHighlightTrackedCol  = -1; // last cursor column seen in OnRender
        private System.Windows.Threading.DispatcherTimer? _wordHighlightTimer;
        private CodeScrollMarkerPanel?      _codeScrollMarkerPanel;

        // ── External line highlights (e.g. StringExtraction) ─────────────────
        private readonly List<LineHighlightEntry> _lineHighlights = [];

        #endregion

        #region Fields - Rendering State

        private Typeface _typeface;
        private Typeface _boldTypeface;
        private Typeface _lineNumberTypeface;
        private double _fontSize     = 12.0; // Effective size = _baseFontSize * ZoomLevel
        private double _baseFontSize = 12.0; // EditorFontSize DP value (zoom-independent)
        private double _charWidth;          // Cached character width
        private double _charHeight;         // Cached character height
        private double _lineHeight;         // Line height with padding

        // GlyphRun renderer — recreated whenever font or DPI changes.
        private GlyphRunRenderer? _glyphRenderer;

        // Per-frame pen/brush caches — rebuilt only when the corresponding value changes (OPT-PERF-03).
        private Pen?    _cachedCaretPen;
        private Pen?    _cachedCaretSecondaryPen;
        private Color   _cachedCaretColor;
        private Pen?    _cachedCurrentLineBorderPen;
        private Color   _cachedCurrentLineBorderColor;
        private Pen?    _cachedUrlPen;
        private Brush?  _cachedUrlBrush;
        private Pen?    _cachedFoldLabelPen;
        private Pen?    _cachedFoldLabelHoverPen;
        private Brush?  _cachedFoldLabelBorderBrush;
        private Brush?  _cachedFoldLabelHoverBrush;

        // DPI value for the current render pass — set once at OnRender entry (OPT-PERF-03).
        private double _renderPixelsPerDip = 1.0;

        // Visible folding regions cache — populated in CalculateVisibleLines to avoid
        // O(total-regions) iteration in RenderScopeGuides every frame (OPT-PERF-03).
        private readonly List<FoldingRegion> _visibleRegions = new();

        // Bracket match cache — avoids O(distance) search on every render frame (OPT-PERF-03).
        private int          _cachedBracketCursorLine = -1;
        private int          _cachedBracketCursorCol  = -1;
        private int          _cachedBracketColumn     = -1;
        private TextPosition? _cachedBracketMatchResult;

        // Multi-line selection geometry cache — avoids O(n²) Geometry.Combine each frame (OPT-PERF-04).
        private Geometry?    _cachedSelectionGeometry;
        private TextPosition _cachedSelGeomStart;
        private TextPosition _cachedSelGeomEnd;
        private int          _cachedSelGeomFirstLine;
        private int          _cachedSelGeomLastLine;

        // LSP diagnostics render coalescing — avoids redundant InvalidateVisual() on rapid batches (OPT-PERF-05).
        private bool _diagnosticsRenderPending;

        // Folding support (Phase B3).
        private FoldingEngine?  _foldingEngine;
        private GutterControl?  _gutterControl;

        // Breakpoint gutter (ADR-DBG-01).
        private BreakpointGutterControl? _breakpointGutterControl;
        private BlameGutterControl?      _blameGutterControl;

        // Change-marker gutter (#166): 4px strip left of the breakpoint gutter.
        private ChangeMarkerGutterControl?                              _changeMarkerGutterControl;
        private readonly Services.GutterChangeTracker                  _changeTracker = new();
        private IReadOnlyDictionary<int, Models.LineChangeKind>        _changeMap
            = new Dictionary<int, Models.LineChangeKind>();

        // Inline peek definition host (#158 — VS2026 style)
        private InlinePeekHost? _inlinePeekHost;
        private int             _peekHostLine   = -1;
        private double          _peekHostHeight = 0.0;

        // 1-based execution line (null when no debug session is paused).
        private int? _executionLineOneBased;

        // Reusable geometry segment list — avoids per-frame allocation during execution/breakpoint rendering.
        private readonly List<Geometry> _renderSegments = new();
        // Reusable set for tracking highlighted breakpoint lines within a single render pass.
        private readonly HashSet<int> _renderHighlightedLines = new();

        // Dedicated DrawingVisual for the caret so blink ticks never trigger a full OnRender.
        // Added last to _scrollBarChildren so it composites on top of all content.
        private readonly DrawingVisual _caretVisual = new();

        // Dirty line range captured from _document.DirtyLines when TextLines flag is set.
        // (from, to) are 0-based logical line indices. Expanded by 1 line each side at render time.
        private (int From, int To) _dirtyLineRange = (0, int.MaxValue);

        // ── Dirty-region rendering (Phase 4) ────────────────────────────────────
        // Tracks which parts of the viewport need repainting; avoids full-frame
        // redraws for caret blinks, selection changes, and other partial updates.
        [Flags]
        private enum RenderDirtyFlags
        {
            None      = 0,
            Caret     = 1 << 0,   // only caret rect changed (blink tick)
            Selection = 1 << 1,   // selection overlay changed
            TextLines = 1 << 2,   // one or more line contents changed
            Overlays  = 1 << 3,   // breakpoints, execution marker, find highlights
            FullFrame = 1 << 7    // scroll, fold, theme — full repaint required
        }

        private RenderDirtyFlags _dirtyFlags = RenderDirtyFlags.FullFrame;

        /// <summary>
        /// Marks the given dirty region and schedules a WPF visual update.
        /// Prefer over <c>InvalidateVisual()</c> — accumulates flags so multiple
        /// callers in one frame are coalesced into a single render pass.
        /// </summary>
        private void InvalidateRegion(RenderDirtyFlags flags)
        {
            _dirtyFlags |= flags;
            InvalidateVisual();
        }

        // Sticky scroll header (#160).
        private StickyScrollHeader? _stickyScrollHeader;
        private bool   _stickyScrollEnabled         = true;
        private int    _stickyScrollMaxLines        = 4;
        private bool   _stickyScrollSyntaxHighlight = true;
        private bool   _stickyScrollClickToNavigate = true;
        private double _stickyScrollOpacity         = 0.95;
        private int    _stickyScrollMinScopeLines   = 5;
        // Perf: track last known sticky state to gate InvalidateArrange() (ADR-IH-PERF-02).
        private int    _stickyScrollLastEntryCount  = -1;
        private int    _lastStickyFirstLine         = -1;

        // Breakpoint placement validation + info popup (ADR-DBG-BP-01).
        private IBreakpointSource?      _bpSource;
        private BreakpointInfoPopup?    _bpInfoPopup;
        private IReadOnlyList<Regex>    _bpNonExecutableRegexes = Array.Empty<Regex>();
        private IReadOnlyList<Regex>    _bpContinuationRegexes  = Array.Empty<Regex>();
        private int                     _bpMaxScanLines         = 20;
        private bool                    _bpBlockScopeHighlight  = true;

        // True between the first Ctrl+M press and the second chord key (outlining commands).
        private bool _outlineChordPending;

        // True between the first Ctrl+K press and the second chord key (formatting commands).
        private bool _formatChordPending;

        // When true, document is formatted automatically on every save.
        private bool _formatOnSave;

        /// <summary>When true, the document is formatted on every Ctrl+S save.</summary>
        public bool FormatOnSave
        {
            get => _formatOnSave;
            set => _formatOnSave = value;
        }

        // XML / XAML attribute formatting overrides.
        private int  _xmlAttributeIndentLevels = 2;
        private bool _xmlOneAttributePerLine;

        /// <summary>
        /// Number of extra indent levels applied to attribute continuation lines in XML/XAML.
        /// Default 2 matches VS XAML formatting (double-indent).
        /// </summary>
        public int XmlAttributeIndentLevels
        {
            get => _xmlAttributeIndentLevels;
            set
            {
                _xmlAttributeIndentLevels = value;
                if (_codeEditorOptions is not null)
                    _codeEditorOptions.XmlAttributeIndentLevels = value;
            }
        }

        /// <summary>
        /// When true, each XML/XAML attribute is placed on its own line during formatting.
        /// </summary>
        public bool XmlOneAttributePerLine
        {
            get => _xmlOneAttributePerLine;
            set
            {
                _xmlOneAttributePerLine = value;
                if (_codeEditorOptions is not null)
                    _codeEditorOptions.XmlOneAttributePerLine = value;
            }
        }

        // Stored reference to CodeEditorOptions for formatting overrides.
        private Options.CodeEditorOptions? _codeEditorOptions;

        // Formatting service — LSP-first, fallback StructuralFormatter.
        private readonly Services.CodeFormattingService _codeFormattingService = new();

        // Color swatch preview (#168) — renders + tracks hit areas.
        private readonly Services.ColorSwatchRenderer _colorSwatchRenderer = new();

        // Whitespace markers — rendered as pale dots/arrows in selection or always.
        private Rendering.WhitespaceRenderer? _whitespaceRenderer;
        private Options.WhitespaceDisplayMode _whitespaceMode = Options.WhitespaceDisplayMode.Selection;

        // 500ms folding debounce timer (P1-CE-01) — prevents O(n) scan on every keystroke
        private System.Windows.Threading.DispatcherTimer? _foldingDebounceTimer;

        // Incremental max-width tracking (P1-CE-02) — O(1) on growth, O(n) only on shrink
        private int _cachedMaxLineLength;
        // Number of lines whose length equals _cachedMaxLineLength.
        // When it drops to 0 a rescan is needed; avoids LINQ allocation on every shrink.
        private int _maxLengthCount;

        // Per-line-number FormattedText cache (P1-CE-03) — eliminates 2,400 allocs/s at 60Hz
        private readonly Dictionary<int, FormattedText> _lineNumberCache = new();
        private Typeface? _cachedLineNumberTypeface;
        private double _cachedLineNumberFontSize = -1;



        // Background highlight pipeline (P1-CE-06)
        private readonly Services.HighlightPipelineService _highlightPipeline = new();
        // Last visible range that was submitted to the pipeline — avoids re-scheduling when unchanged.
        private int _lastHighlightFirst = -1;
        private int _lastHighlightLast  = -1;
        // Last text passed to EmbeddedSyntaxHighlighter.SetFullText — avoids O(n) rebuild when unchanged.
        private string? _embeddedTextCache;

        private int _firstVisibleLine = 0;  // Physical line index of first rendered line
        private int _lastVisibleLine = 0;   // Physical line index of last rendered line
        private int _firstVisibleRank = 0;  // Visible rank of _firstVisibleLine (for VE pixel math)

        // OPT-D: lineYLookup dirty flag — avoids rebuilding per-line Y positions on every
        // render frame (e.g. caret blink at 530 ms).  Rebuilt only when the visible range,
        // InlineHints data, folding regions, or scroll offset actually change.
        private bool   _linePositionsDirty       = true;
        private double _lastRenderedScrollOffset = -1.0;

        #endregion

        #region Fields - Caret Blinking

        private System.Windows.Threading.DispatcherTimer _caretTimer;
        private bool _caretVisible = true;

        #endregion

        #region Fields - Layout Constants

        private const double TopMargin = 2;
        private const double LeftMargin = 5;
        private const double LineNumberWidth = 60;
        private const double LineNumberMargin = 5;
        private const double TextAreaLeftOffset = 70; // LineNumberWidth + margin
        private const double ScrollBarThickness = 17.0;
        private const double SelectionCornerRadius = 3.0;

        // Extra vertical space reserved at the top of each line slot for InlineHints hints.
        private const double HintLineHeight = 16.0;

        #endregion

        #region Fields - Colors (Brushes)

        #endregion

        // ── Partial class split (SRP decomposition) ─────────────────────────────
        // CodeEditor.Properties.cs  — Dependency Properties + property-changed callbacks
        // CodeEditor.Init.cs        — Constructor, initialization helpers, character dimension calculation
        // CodeEditor.ContextMenu.cs — Context menu, ISearchTarget, find/replace helpers
        // CodeEditor.Viewport.cs    — Virtual scrolling, viewport math, navigation
        // CodeEditor.Formatting.cs  — FormatDocumentAsync, FormatSelectionAsync, sticky scroll
        // ──────────────────────────────────────────────────────────────────────────

    }   // end partial class CodeEditor

    /// <summary>One externally-added line background highlight entry.</summary>
    public sealed class LineHighlightEntry
    {
        public int    Line        { get; }
        public string Description { get; }
        public string Tag         { get; }
        /// <summary>Pre-frozen brush with opacity already applied — zero allocation per render frame.</summary>
        public Brush  FrozenBrush { get; }

        public LineHighlightEntry(int line, SolidColorBrush color, double opacity, string description, string tag)
        {
            Line        = line;
            Description = description;
            Tag         = tag;
            var b = color.Clone();
            b.Opacity = opacity;
            b.Freeze();
            FrozenBrush = b;
        }
    }

    /// <summary>
    /// Event arguments raised when the user clicks a colour swatch in the editor.
    /// </summary>
    public sealed class ColorSwatchClickedEventArgs : EventArgs
    {
        /// <summary>The colour value shown by the swatch that was clicked.</summary>
        public System.Windows.Media.Color Color { get; }

        /// <summary>0-based document line index of the colour literal.</summary>
        public int Line { get; }

        internal ColorSwatchClickedEventArgs(System.Windows.Media.Color color, int line)
        {
            Color = color;
            Line  = line;
        }
    }
}
