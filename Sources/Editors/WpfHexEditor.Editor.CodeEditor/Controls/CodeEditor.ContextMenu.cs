// ==========================================================
// Project: WpfHexEditor.Editor.CodeEditor
// File: Controls/CodeEditor.ContextMenu.cs
// Description: Context menu initialization, commands, find/replace, and ISearchTarget implementation for CodeEditor.
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
        #region Context Menu (Phase C)

        /// <summary>
        /// Initialize context menu with standard editing commands
        /// </summary>
        private void InitializeContextMenu()
        {
            var contextMenu = new ContextMenu();

            // Cut
            var cutMenuItem = new MenuItem
            {
                Header           = "Cu_t",
                InputGestureText = "Ctrl+X",
                Command          = ApplicationCommands.Cut,
                CommandTarget    = this,
                Icon             = MakeMenuIcon("")
            };
            contextMenu.Items.Add(cutMenuItem);

            // Copy
            var copyMenuItem = new MenuItem
            {
                Header           = "_Copy",
                InputGestureText = "Ctrl+C",
                Command          = ApplicationCommands.Copy,
                CommandTarget    = this,
                Icon             = MakeMenuIcon("")
            };
            contextMenu.Items.Add(copyMenuItem);

            // Paste
            var pasteMenuItem = new MenuItem
            {
                Header           = "_Paste",
                InputGestureText = "Ctrl+V",
                Command          = ApplicationCommands.Paste,
                CommandTarget    = this,
                Icon             = MakeMenuIcon("")
            };
            contextMenu.Items.Add(pasteMenuItem);

            // Separator
            contextMenu.Items.Add(new Separator());

            // Undo — stored as field so the Opened handler can update the header dynamically.
            _undoMenuItem = new MenuItem
            {
                Header           = "_Undo",
                InputGestureText = "Ctrl+Z",
                Command          = ApplicationCommands.Undo,
                CommandTarget    = this,
                Icon             = MakeMenuIcon("")
            };
            contextMenu.Items.Add(_undoMenuItem);

            // Redo
            _redoMenuItem = new MenuItem
            {
                Header           = "_Redo",
                InputGestureText = "Ctrl+Y / Ctrl+Shift+Z",
                Command          = ApplicationCommands.Redo,
                CommandTarget    = this,
                Icon             = MakeMenuIcon("")
            };
            contextMenu.Items.Add(_redoMenuItem);

            // Update dynamic headers just before the menu appears.
            contextMenu.Opened += (_, _) =>
            {
                if (_undoMenuItem != null)
                    _undoMenuItem.Header = CanUndo
                        ? $"_Undo ({(_sharedUndoEngine?.UndoCount ?? _undoEngine.UndoCount)})"
                        : "_Undo";
                if (_redoMenuItem != null)
                    _redoMenuItem.Header = CanRedo
                        ? $"_Redo ({(_sharedUndoEngine?.RedoCount ?? _undoEngine.RedoCount)})"
                        : "_Redo";
            };

            // Separator
            contextMenu.Items.Add(new Separator());

            // Select All
            var selectAllMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuSelectAll,
                InputGestureText = "Ctrl+A",
                Command          = ApplicationCommands.SelectAll,
                CommandTarget    = this,
                Icon             = MakeMenuIcon("")
            };
            contextMenu.Items.Add(selectAllMenuItem);

            // Delete
            var deleteMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuDelete,
                InputGestureText = "Del",
                Command          = ApplicationCommands.Delete,
                CommandTarget    = this,
                Icon             = MakeMenuIcon("")
            };
            contextMenu.Items.Add(deleteMenuItem);

            // Separator
            contextMenu.Items.Add(new Separator());

            // Find
            var findMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuFind,
                InputGestureText = "Ctrl+F",
                Command          = ApplicationCommands.Find,
                CommandTarget    = this,
                Icon             = MakeMenuIcon("")
            };
            contextMenu.Items.Add(findMenuItem);

            // Replace
            var replaceMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuReplace,
                InputGestureText = "Ctrl+H",
                Command          = ApplicationCommands.Replace,
                CommandTarget    = this,
                Icon             = MakeMenuIcon("")
            };
            contextMenu.Items.Add(replaceMenuItem);

            // Separator
            contextMenu.Items.Add(new Separator());

            // Find All References (LSP)
            var findRefsMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuFindAllRefs,
                InputGestureText = "Shift+F12",
                Command          = FindAllReferencesCommand,
                CommandTarget    = this,
                Icon             = MakeMenuIcon("")
            };
            contextMenu.Items.Add(findRefsMenuItem);

            // Dynamically enable/disable the item when the menu opens.
            contextMenu.Opened += (_, _) =>
                findRefsMenuItem.IsEnabled = EnableFindAllReferences
                                             && _document is not null;

            // Quick Fix (LSP Code Actions)
            var quickFixMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuQuickFix,
                InputGestureText = "Ctrl+.",
                Icon             = MakeMenuIcon(""),
            };
            quickFixMenuItem.Click += (_, _) => _ = ShowCodeActionsAsync();
            contextMenu.Items.Add(quickFixMenuItem);

            // Rename Symbol (LSP Rename)
            var renameMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuRenameSymbol,
                InputGestureText = "F2",
                Icon             = MakeMenuIcon(""),
            };
            renameMenuItem.Click += (_, _) => _ = StartRenameAsync();
            contextMenu.Items.Add(renameMenuItem);

            // Go to Definition (F12)
            var goToDefMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuGoToDefinition,
                InputGestureText = "F12",
                Icon             = MakeMenuIcon(""),
            };
            goToDefMenuItem.Click += (_, _) => _ = GoToDefinitionAtCaretAsync();
            contextMenu.Items.Add(goToDefMenuItem);

            // Go to Implementation (Ctrl+F12)
            var goToImplMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuGoToImplementation,
                InputGestureText = "Ctrl+F12",
                Icon             = MakeMenuIcon(""),
            };
            goToImplMenuItem.Click += (_, _) => _ = GoToImplementationAtCaretAsync();
            contextMenu.Items.Add(goToImplMenuItem);

            // Peek Definition (Alt+F12)
            var peekDefMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuPeekDefinition,
                InputGestureText = "Alt+F12",
                Icon             = MakeMenuIcon(""),
            };
            peekDefMenuItem.Click += (_, _) => _ = ShowPeekDefinitionAsync();
            contextMenu.Items.Add(peekDefMenuItem);

            // Show Call Hierarchy (Shift+Alt+H)
            var callHierarchyMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuShowCallHierarchy,
                InputGestureText = "Shift+Alt+H",
                Icon             = MakeMenuIcon(""),
            };
            callHierarchyMenuItem.Click += (_, _) => _ = PrepareCallHierarchyAtCaretAsync();
            contextMenu.Items.Add(callHierarchyMenuItem);

            // Show Type Hierarchy (Ctrl+Alt+F12)
            var typeHierarchyMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuShowTypeHierarchy,
                InputGestureText = "Ctrl+Alt+F12",
                Icon             = MakeMenuIcon(""),
            };
            typeHierarchyMenuItem.Click += (_, _) => _ = PrepareTypeHierarchyAtCaretAsync();
            contextMenu.Items.Add(typeHierarchyMenuItem);

            // Enable/disable LSP items based on whether a client is active.
            contextMenu.Opened += (_, _) =>
            {
                var lspActive = _lspClient is not null;
                quickFixMenuItem.IsEnabled       = lspActive;
                renameMenuItem.IsEnabled         = lspActive;
                goToDefMenuItem.IsEnabled        = lspActive;
                goToImplMenuItem.IsEnabled       = lspActive;
                peekDefMenuItem.IsEnabled        = lspActive;
                callHierarchyMenuItem.IsEnabled  = lspActive;
                typeHierarchyMenuItem.IsEnabled  = lspActive;
            };

            // Separator
            contextMenu.Items.Add(new Separator());

            // ── Formatting submenu ──────────────────────────────────────────────
            var formattingMenu = new MenuItem { Header = CodeEditorResources.CodeEditor_ContextMenuFormatting, Icon = MakeMenuIcon("") };

            // Format Document (Ctrl+K, Ctrl+D)
            var formatDocMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuFormatDocument,
                InputGestureText = "Ctrl+K, Ctrl+D",
                Icon             = MakeMenuIcon("")
            };
            formatDocMenuItem.Click += (_, _) => _ = FormatDocumentAsync();

            // Format Selection (Ctrl+K, Ctrl+F)
            var formatSelMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuFormatSelection,
                InputGestureText = "Ctrl+K, Ctrl+F",
                Icon             = MakeMenuIcon("")
            };
            formatSelMenuItem.Click += (_, _) => _ = FormatSelectionAsync();

            formattingMenu.Items.Add(formatDocMenuItem);
            formattingMenu.Items.Add(formatSelMenuItem);
            formattingMenu.Items.Add(new Separator());

            // Format JSON
            var formatJsonMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuFormatJson,
                InputGestureText = "Ctrl+Shift+F",
                Icon             = MakeMenuIcon("")
            };
            formatJsonMenuItem.Click += FormatJsonMenuItem_Click;

            // Validate JSON
            var validateMenuItem = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuValidateJson,
                InputGestureText = "F5",
                Icon             = MakeMenuIcon("")
            };
            validateMenuItem.Click += ValidateMenuItem_Click;

            formattingMenu.Items.Add(formatJsonMenuItem);
            formattingMenu.Items.Add(validateMenuItem);
            formattingMenu.Items.Add(new Separator());

            // Options...
            var formattingOptionsMenuItem = new MenuItem
            {
                Header = CodeEditorResources.CodeEditor_ContextMenuFormattingOptions,
                Icon   = MakeMenuIcon("")   // Settings gear
            };
            formattingOptionsMenuItem.Click += (_, _) =>
                FormattingOptionsRequested?.Invoke(this, EventArgs.Empty);
            formattingMenu.Items.Add(formattingOptionsMenuItem);

            // Enable/disable formatting items based on edit state and selection.
            contextMenu.Opened += (_, _) =>
            {
                formatDocMenuItem.IsEnabled = !IsReadOnly;
                formatSelMenuItem.IsEnabled = !IsReadOnly && !_selection.IsEmpty;
            };

            contextMenu.Items.Add(formattingMenu);

            // Separator
            contextMenu.Items.Add(new Separator());

            // ── Outlining submenu — mirrors Visual Studio outlining menu ──
            var outlineMenu = new MenuItem { Header = CodeEditorResources.CodeEditor_ContextMenuOutlining };

            var miToggleCurrent = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuToggleOutlining,
                InputGestureText = "Ctrl+M, Ctrl+M",
                Icon             = MakeMenuIcon("")
            };
            miToggleCurrent.Click += (_, _) => OutlineToggleCurrent();

            var miToggleAll = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuToggleAllOutlining,
                InputGestureText = "Ctrl+M, Ctrl+L",
                Icon             = MakeMenuIcon("")
            };
            miToggleAll.Click += (_, _) => OutlineToggleAll();

            var miStop = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuStopOutlining,
                InputGestureText = "Ctrl+M, Ctrl+P",
                Icon             = MakeMenuIcon("")
            };
            miStop.Click += (_, _) => OutlineStop();

            var miStopHiding = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuStopHidingCurrent,
                InputGestureText = "Ctrl+M, Ctrl+U",
                Icon             = MakeMenuIcon("")
            };
            miStopHiding.Click += (_, _) => OutlineStopHidingCurrent();

            var miCollapseDefs = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuCollapseToDefs,
                InputGestureText = "Ctrl+M, Ctrl+O",
                Icon             = MakeMenuIcon("")
            };
            miCollapseDefs.Click += (_, _) => OutlineCollapseToDefinitions();

            outlineMenu.Items.Add(miToggleCurrent);
            outlineMenu.Items.Add(miToggleAll);
            outlineMenu.Items.Add(new Separator());
            outlineMenu.Items.Add(miStop);
            outlineMenu.Items.Add(miStopHiding);
            outlineMenu.Items.Add(new Separator());
            outlineMenu.Items.Add(miCollapseDefs);

            // Enable the submenu only when folding is active.
            contextMenu.Opened += (_, _) => outlineMenu.IsEnabled = IsFoldingEnabled;

            contextMenu.Items.Add(outlineMenu);
            // ─────────────────────────────────────────────────────────────────────────

            // Word Wrap toggle
            contextMenu.Items.Add(new Separator());
            var miWordWrap = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuWordWrap,
                IsCheckable      = true,
                InputGestureText = "Alt+Z",
                Icon             = MakeMenuIcon("")
            };
            miWordWrap.SetBinding(MenuItem.IsCheckedProperty,
                new System.Windows.Data.Binding(nameof(IsWordWrapEnabled)) { Source = this, Mode = System.Windows.Data.BindingMode.TwoWay });
            contextMenu.Items.Add(miWordWrap);

            // Column Rulers toggle
            var miColumnRulers = new MenuItem
            {
                Header      = CodeEditorResources.CodeEditor_ContextMenuColumnRulers,
                IsCheckable = true,
                Icon        = MakeMenuIcon("")
            };
            miColumnRulers.SetBinding(MenuItem.IsCheckedProperty,
                new System.Windows.Data.Binding(nameof(ShowColumnRulers)) { Source = this, Mode = System.Windows.Data.BindingMode.TwoWay });
            contextMenu.Items.Add(miColumnRulers);

            // Show Whitespace submenu (radio-style: None / Selection / Always)
            var wsMenu = new MenuItem { Header = CodeEditorResources.CodeEditor_ContextMenuShowWhitespace, Icon = MakeMenuIcon("") };
            var wsNone = new MenuItem { Header = CodeEditorResources.CodeEditor_ContextMenuWhitespaceNone,      IsCheckable = true };
            var wsSel  = new MenuItem { Header = CodeEditorResources.CodeEditor_ContextMenuWhitespaceSelection, IsCheckable = true };
            var wsAll  = new MenuItem { Header = CodeEditorResources.CodeEditor_ContextMenuWhitespaceAlways,    IsCheckable = true };

            wsNone.Click += (_, _) => { _whitespaceMode = Options.WhitespaceDisplayMode.None;      InvalidateVisual(); };
            wsSel.Click  += (_, _) => { _whitespaceMode = Options.WhitespaceDisplayMode.Selection;  InvalidateVisual(); };
            wsAll.Click  += (_, _) => { _whitespaceMode = Options.WhitespaceDisplayMode.Always;     InvalidateVisual(); };

            wsMenu.Items.Add(wsNone);
            wsMenu.Items.Add(wsSel);
            wsMenu.Items.Add(wsAll);

            contextMenu.Opened += (_, _) =>
            {
                wsNone.IsChecked = _whitespaceMode == Options.WhitespaceDisplayMode.None;
                wsSel.IsChecked  = _whitespaceMode == Options.WhitespaceDisplayMode.Selection;
                wsAll.IsChecked  = _whitespaceMode == Options.WhitespaceDisplayMode.Always;
            };
            contextMenu.Items.Add(wsMenu);

            // Refresh Highlights — Ctrl+Shift+R
            contextMenu.Items.Add(new Separator());
            var miRefreshHighlights = new MenuItem
            {
                Header           = CodeEditorResources.CodeEditor_ContextMenuRefreshHighlights,
                InputGestureText = "Ctrl+Shift+R",
                Icon             = MakeMenuIcon("")
            };
            miRefreshHighlights.Click += (_, _) => RefreshHighlights();
            // RoutedUICommand.CanExecute cannot route back to the editor once the ContextMenu
            // takes focus. Update IsEnabled directly when the menu opens instead.
            contextMenu.Opened += (_, _) => miRefreshHighlights.IsEnabled = _currentFilePath is not null;
            contextMenu.Items.Add(miRefreshHighlights);

            // Re-analyze Folding
            var miReanalyzeFolding = new MenuItem
            {
                Header = CodeEditorResources.CodeEditor_ContextMenuReanalyzeFolding,
                Icon   = MakeMenuIcon("")
            };
            miReanalyzeFolding.Click += (_, _) => ReanalyzeFolding();
            contextMenu.Opened += (_, _) => miReanalyzeFolding.IsEnabled = IsFoldingEnabled && _currentFilePath is not null;
            contextMenu.Items.Add(miReanalyzeFolding);

            // Toggle Word Highlight
            var miWordHighlight = new MenuItem
            {
                Header    = CodeEditorResources.CodeEditor_ContextMenuWordHighlight,
                Icon      = MakeMenuIcon(""),  // Highlight glyph
                IsCheckable = true,
            };
            miWordHighlight.Click += (_, _) => EnableWordHighlight = miWordHighlight.IsChecked;
            contextMenu.Opened += (_, _) => miWordHighlight.IsChecked = EnableWordHighlight;
            contextMenu.Items.Add(miWordHighlight);

            // Refactor submenu (Rename / Extract Method / Extract Class /
            // Introduce Variable / Inline Method). All wired through
            // RefactoringMenuRequested so a host can present its own UI.
            contextMenu.Items.Add(new Separator());
            var miRefactor = new MenuItem
            {
                Header = CodeEditorResources.CodeEditor_ContextMenuRefactor,
                Icon   = MakeMenuIcon(""),  // edit/pen glyph
            };
            void AddRefactorItem(string label, RefactoringKind kind)
            {
                var mi = new MenuItem { Header = label };
                mi.Click += (_, _) => RefactoringMenuRequested?.Invoke(this, BuildRefactorEventArgs(kind));
                miRefactor.Items.Add(mi);
            }
            AddRefactorItem(CodeEditorResources.CodeEditor_RefactorRename,            RefactoringKind.Rename);
            AddRefactorItem(CodeEditorResources.CodeEditor_RefactorExtractMethod,     RefactoringKind.ExtractMethod);
            AddRefactorItem(CodeEditorResources.CodeEditor_RefactorExtractClass,      RefactoringKind.ExtractClass);
            AddRefactorItem(CodeEditorResources.CodeEditor_RefactorIntroduceVariable, RefactoringKind.IntroduceVariable);
            AddRefactorItem(CodeEditorResources.CodeEditor_RefactorInlineMethod,      RefactoringKind.InlineMethod);
            contextMenu.Items.Add(miRefactor);

            // Set context menu
            ContextMenu = contextMenu;

            // Register command bindings
            RegisterContextMenuCommands();
        }

        /// <summary>Raised when the user picks an item under Refactor ▶.</summary>
        public event EventHandler<RefactoringMenuRequestedEventArgs>? RefactoringMenuRequested;

        private RefactoringMenuRequestedEventArgs BuildRefactorEventArgs(RefactoringKind kind)
        {
            var fullText = _document?.SaveToString() ?? string.Empty;
            var startOff = TextPositionToOffset(fullText, _selection?.NormalizedStart);
            var endOff   = TextPositionToOffset(fullText, _selection?.NormalizedEnd);
            var caret    = TextPositionToOffset(fullText, _selection?.End);

            return new RefactoringMenuRequestedEventArgs(kind)
            {
                DocumentText    = fullText,
                FilePath        = _currentFilePath ?? string.Empty,
                CaretOffset     = caret,
                SelectionStart  = startOff,
                SelectionLength = Math.Max(0, endOff - startOff),
            };
        }

        private static int TextPositionToOffset(string text, TextPosition? pos)
        {
            if (pos is null || string.IsNullOrEmpty(text)) return 0;
            var p = pos.Value;
            int line = 0, offset = 0;
            for (int i = 0; i < text.Length && line < p.Line; i++)
                if (text[i] == '\n') { line++; offset = i + 1; }
            return Math.Min(text.Length, offset + p.Column);
        }

        /// <summary>
        /// Register command bindings for context menu commands
        /// </summary>
        private void RegisterContextMenuCommands()
        {
            // Cut — enabled for both normal and rectangular selection.
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut,
                (sender, e) => CutToClipboard(),
                (sender, e) => e.CanExecute = !IsReadOnly && (!_selection.IsEmpty || !_rectSelection.IsEmpty)));

            // Copy — enabled for both normal and rectangular selection.
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy,
                (sender, e) => CopyToClipboard(),
                (sender, e) => e.CanExecute = !_selection.IsEmpty || !_rectSelection.IsEmpty));

            // Paste — disabled when a rectangular selection is active (no block-paste support).
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste,
                (sender, e) => PasteFromClipboard(),
                (sender, e) => e.CanExecute = Clipboard.ContainsText() && _rectSelection.IsEmpty));

            // Undo
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo,
                (sender, e) => Undo(),
                (sender, e) => e.CanExecute = CanUndo));

            // Redo
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo,
                (sender, e) => Redo(),
                (sender, e) => e.CanExecute = CanRedo));

            // Select All
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll,
                (sender, e) => SelectAll()));

            // Delete
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete,
                (sender, e) => { if (!_selection.IsEmpty) DeleteSelection(); else DeleteCharAfter(); },
                (sender, e) => e.CanExecute = !IsReadOnly));

            // Find (Ctrl+F) and Replace (Ctrl+H) are handled by CodeEditorSplitHost
            // via PreviewKeyDown → ShowSearch(), which binds the shared QuickSearchBar
            // to this editor through ISearchTarget. No modeless Window is needed here.

            // Find All References (Shift+F12) — works with or without an LSP client.
            CommandBindings.Add(new CommandBinding(
                FindAllReferencesCommand,
                async (_, _) => await FindAllReferencesAsync(),
                (_, e) => e.CanExecute = EnableFindAllReferences
                                         && _document is not null));

            // Select Next Occurrence (Ctrl+D) — multi-caret word selection.
            CommandBindings.Add(new CommandBinding(
                SelectNextOccurrenceCommand,
                (_, _) => SelectNextOccurrence(),
                (_, e) => e.CanExecute = _document is not null));

            // Refresh Highlights (Ctrl+Shift+R) — force clear + re-request all highlight layers.
            CommandBindings.Add(new CommandBinding(
                RefreshHighlightsCommand,
                (_, _) => RefreshHighlights(),
                (_, e) => e.CanExecute = _currentFilePath is not null));
        }

        /// <summary>
        /// Selects the next occurrence of the word at the caret (or the current selection text)
        /// and adds a secondary caret at that position. VS Code Ctrl+D behaviour.
        /// </summary>
        private void SelectNextOccurrence()
        {
            if (_document == null) return;

            string word = _selection.IsEmpty
                ? GetWordAtCursor()
                : _document.GetText(_selection.NormalizedStart, _selection.NormalizedEnd);

            if (string.IsNullOrEmpty(word)) return;

            // Start search from the current caret position (or end of the last caret).
            int startLine = _caretManager.IsMultiCaret
                ? _caretManager.Carets[^1].Line
                : _cursorLine;
            int startCol  = _caretManager.IsMultiCaret
                ? _caretManager.Carets[^1].Column + 1
                : _cursorColumn + 1;

            int totalLines = _document.Lines.Count;
            for (int pass = 0; pass < 2; pass++) // allow wrap-around once
            {
                for (int li = (pass == 0 ? startLine : 0);
                     li < totalLines;
                     li++)
                {
                    string lineText = _document.Lines[li].Text;
                    int searchFrom  = (pass == 0 && li == startLine) ? Math.Min(startCol, lineText.Length) : 0;
                    int idx         = lineText.IndexOf(word, searchFrom, StringComparison.Ordinal);

                    if (idx >= 0)
                    {
                        _caretManager.AddCaret(li, idx + word.Length);
                        // Also update primary caret to the new position.
                        _cursorLine   = li;
                        _cursorColumn = idx + word.Length;
                        EnsureCursorVisible();
                        NotifyCaretMovedIfChanged();
                        InvalidateVisual();
                        return;
                    }
                }
                // Wrap: restart from top on second pass.
                startLine = 0;
                startCol  = 0;
            }
        }

        private void FormatJsonMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FormatJson();
        }

        private void ValidateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            RunValidation();
        }

        private void ExecuteFind(string query)
        {
            _findResults.Clear();
            _findMatchLength = 0;
            if (string.IsNullOrEmpty(query) || _document == null)
            { InvalidateVisual(); return; }

            _findMatchLength = query.Length;
            for (int line = 0; line < _document.Lines.Count; line++)
            {
                var lineText = _document.Lines[line].Text;
                int col = 0;
                while (true)
                {
                    int idx = lineText.IndexOf(query, col, StringComparison.OrdinalIgnoreCase);
                    if (idx < 0) break;
                    _findResults.Add(new Models.TextPosition(line, idx));
                    col = idx + 1;
                }
            }
            InvalidateVisual();
        }

        /// <summary>
        /// Navigates to the next find result (wraps around).
        /// </summary>
        public void FindNext()
        {
            if (_findResults.Count == 0 && !string.IsNullOrEmpty(_lastFindQuery))
                ExecuteFind(_lastFindQuery);
            if (_findResults.Count == 0) return;
            _currentFindMatchIndex = (_currentFindMatchIndex + 1) % _findResults.Count;
            NavigateToFindMatch();
            SearchResultsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Navigates to the previous find result (wraps around).
        /// </summary>
        public void FindPrevious()
        {
            if (_findResults.Count == 0 && !string.IsNullOrEmpty(_lastFindQuery))
                ExecuteFind(_lastFindQuery);
            if (_findResults.Count == 0) return;
            _currentFindMatchIndex = (_currentFindMatchIndex - 1 + _findResults.Count) % _findResults.Count;
            NavigateToFindMatch();
            SearchResultsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void NavigateToFindMatch()
        {
            if (_currentFindMatchIndex < 0 || _currentFindMatchIndex >= _findResults.Count) return;
            var match = _findResults[_currentFindMatchIndex];
            _cursorLine   = match.Line;
            _cursorColumn = match.Column + _findMatchLength;
            EnsureCursorVisible();
            InvalidateVisual();
        }

        private void ClearFind()
        {
            _findResults.Clear();
            _currentFindMatchIndex = -1;
            _findMatchLength = 0;
            InvalidateVisual();
        }

        #region ISearchTarget

        public event EventHandler? SearchResultsChanged;

        SearchBarCapabilities ISearchTarget.Capabilities =>
            SearchBarCapabilities.CaseSensitive | SearchBarCapabilities.Replace;

        int ISearchTarget.MatchCount        => _findResults.Count;
        int ISearchTarget.CurrentMatchIndex => _currentFindMatchIndex;

        void ISearchTarget.Find(string query, SearchTargetOptions options)
        {
            _lastFindQuery = query;
            ExecuteFind(query);
            if (_findResults.Count > 0)
            {
                _currentFindMatchIndex = 0;
                NavigateToFindMatch();
            }
            else
            {
                _currentFindMatchIndex = -1;
                InvalidateVisual();
            }
            SearchResultsChanged?.Invoke(this, EventArgs.Empty);
        }

        void ISearchTarget.FindNext()     => FindNext();
        void ISearchTarget.FindPrevious() => FindPrevious();

        void ISearchTarget.ClearSearch()
        {
            ClearFind();
            SearchResultsChanged?.Invoke(this, EventArgs.Empty);
        }

        void ISearchTarget.Replace(string replacement)
        {
            // Replace the current find match with the replacement text
            if (_currentFindMatchIndex < 0 || _currentFindMatchIndex >= _findResults.Count) return;
            var match = _findResults[_currentFindMatchIndex];
            _selection.Start = match;
            _selection.End   = new Models.TextPosition(match.Line, match.Column + _findMatchLength);
            DeleteSelection();
            foreach (var ch in replacement) InsertChar(ch);
            // Re-run find and advance to next match
            ExecuteFind(_lastFindQuery ?? string.Empty);
            if (_findResults.Count > 0)
            {
                _currentFindMatchIndex = Math.Min(_currentFindMatchIndex, _findResults.Count - 1);
                NavigateToFindMatch();
            }
            SearchResultsChanged?.Invoke(this, EventArgs.Empty);
        }

        void ISearchTarget.ReplaceAll(string replacement)
        {
            if (string.IsNullOrEmpty(_lastFindQuery)) return;
            ExecuteFind(_lastFindQuery);
            if (_findResults.Count == 0) return;

            // Iterate results in reverse to preserve column / line offsets
            for (int i = _findResults.Count - 1; i >= 0; i--)
            {
                var match = _findResults[i];
                _selection.Start = match;
                _selection.End   = new Models.TextPosition(match.Line, match.Column + _findMatchLength);
                DeleteSelection();
                _isInternalEdit = true; // suppress undo coalescing inside loop
                try { foreach (var ch in replacement) InsertChar(ch); }
                finally { _isInternalEdit = false; }
            }
            ClearFind();
            _isDirty = true;
            ModifiedChanged?.Invoke(this, EventArgs.Empty);
            SearchResultsChanged?.Invoke(this, EventArgs.Empty);
        }

        UIElement? ISearchTarget.GetCustomFiltersContent() => null;

        #endregion

        // -- Format JSON --------------------------------------------------

        private void FormatJson()
        {
            var text = GetText();
            try
            {
                using var jdoc = System.Text.Json.JsonDocument.Parse(text,
                    new System.Text.Json.JsonDocumentOptions { AllowTrailingCommas = true });
                var formatted = System.Text.Json.JsonSerializer.Serialize(
                    jdoc.RootElement,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                if (formatted != text)
                {
                    LoadText(formatted);
                    _isDirty = true;
                    ModifiedChanged?.Invoke(this, EventArgs.Empty);
                }
                StatusMessage?.Invoke(this, "JSON formatted.");
            }
            catch (System.Text.Json.JsonException ex)
            {
                IdeMessageBox.Show(string.Format(CodeEditorResources.CodeEditor_FormatJsonError, ex.Message),
                    CodeEditorResources.CodeEditor_FormatJsonErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // -- Validate JSON ------------------------------------------------

        private void RunValidation()
        {
            var text = GetText();
            try
            {
                using var _ = System.Text.Json.JsonDocument.Parse(text,
                    new System.Text.Json.JsonDocumentOptions { AllowTrailingCommas = true });
                StatusMessage?.Invoke(this, CodeEditorResources.CodeEditor_ValidateJsonSuccess);
                IdeMessageBox.Show(CodeEditorResources.CodeEditor_ValidateJsonSuccess, CodeEditorResources.CodeEditor_ValidateJsonTitle,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Text.Json.JsonException ex)
            {
                var msg = string.Format(CodeEditorResources.CodeEditor_ValidateJsonError, ex.Message);
                StatusMessage?.Invoke(this, msg);
                IdeMessageBox.Show(msg, CodeEditorResources.CodeEditor_ValidateJsonErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion
    }
}
