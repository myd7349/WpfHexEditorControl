//////////////////////////////////////////////
// Apache 2.0  - 2026
// Custom JsonEditor - Main Editor Control (Phase 1 - Foundation)
// Author : Claude Sonnet 4.5
// Contributors: Derek Tremblay (derektremblay666@gmail.com)
// Inspired by HexViewport.cs custom rendering pattern
//////////////////////////////////////////////

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexaEditor.Models.JsonEditor;

namespace WpfHexaEditor.Controls.JsonEditor
{
    /// <summary>
    /// High-performance JSON text editor using custom rendering (FrameworkElement).
    /// Phase 1: Basic text display + keyboard input + line numbers
    /// Future phases will add: syntax highlighting, IntelliSense, validation
    /// </summary>
    public class JsonEditor : FrameworkElement
    {
        #region Fields - Document Model

        private JsonDocument _document;
        private int _cursorLine = 0;        // Current cursor line (0-based)
        private int _cursorColumn = 0;      // Current cursor column (0-based)
        private TextSelection _selection;   // Current text selection

        #endregion

        #region Fields - Rendering State

        private Typeface _typeface;
        private Typeface _boldTypeface;
        private double _fontSize = 12.0;
        private double _charWidth;          // Cached character width
        private double _charHeight;         // Cached character height
        private double _lineHeight;         // Line height with padding
        private int _firstVisibleLine = 0;  // Scrolling support (Phase 1: always 0)
        private int _lastVisibleLine = 0;   // Will be calculated in Phase 1

        #endregion

        #region Fields - Layout Constants

        private const double TopMargin = 2;
        private const double LeftMargin = 5;
        private const double LineNumberWidth = 60;
        private const double LineNumberMargin = 5;
        private const double TextAreaLeftOffset = 70; // LineNumberWidth + margin

        #endregion

        #region Fields - Colors (Brushes)

        private Brush _editorBackground = Brushes.White;
        private Brush _editorForeground = Brushes.Black;
        private Brush _lineNumberBackground;
        private Brush _lineNumberForeground;
        private Brush _currentLineBackground;
        private Brush _selectionBackground;

        #endregion

        #region Dependency Properties (Will expand in Phase 8)

        // Phase 1: Basic properties only
        // Phase 8 will add 110+ DPs for full configuration

        public static readonly DependencyProperty ShowLineNumbersProperty =
            DependencyProperty.Register(nameof(ShowLineNumbers), typeof(bool), typeof(JsonEditor),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ShowLineNumbers
        {
            get => (bool)GetValue(ShowLineNumbersProperty);
            set => SetValue(ShowLineNumbersProperty, value);
        }

        #endregion

        #region Constructor

        public JsonEditor()
        {
            // Initialize document
            _document = new JsonDocument();
            _selection = new TextSelection();

            // Subscribe to document changes
            _document.TextChanged += Document_TextChanged;

            // Initialize typefaces
            _typeface = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            _boldTypeface = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

            // Calculate character dimensions
            CalculateCharacterDimensions();

            // Initialize brushes
            InitializeBrushes();

            // Make focusable for keyboard input
            Focusable = true;
            FocusVisualStyle = null; // No focus rectangle

            // Set minimum size
            MinWidth = 200;
            MinHeight = 100;
        }

        #endregion

        #region Character Dimension Calculation

        /// <summary>
        /// Calculate character dimensions using FormattedText
        /// Same pattern as HexViewport
        /// </summary>
        private void CalculateCharacterDimensions()
        {
            var formattedText = new FormattedText(
                "M", // Use 'M' as reference (monospace width)
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _typeface,
                _fontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            _charWidth = formattedText.Width;
            _charHeight = formattedText.Height;
            _lineHeight = _charHeight + 4; // Add 4px padding
        }

        /// <summary>
        /// Initialize color brushes (frozen for performance)
        /// </summary>
        private void InitializeBrushes()
        {
            _lineNumberBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            _lineNumberBackground.Freeze();

            _lineNumberForeground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
            _lineNumberForeground.Freeze();

            _currentLineBackground = new SolidColorBrush(Color.FromArgb(30, 0, 120, 215)); // 12% opacity blue
            _currentLineBackground.Freeze();

            _selectionBackground = new SolidColorBrush(Color.FromRgb(173, 214, 255)); // Light blue
            _selectionBackground.Freeze();

            _editorBackground.Freeze();
            _editorForeground.Freeze();
        }

        #endregion

        #region Rendering - OnRender Override

        /// <summary>
        /// Main rendering method - draws all visual elements
        /// Called by WPF when visual update is needed
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (_document == null || _document.Lines.Count == 0)
                return;

            // Calculate visible line range (Phase 1: simple - show all lines up to viewport height)
            CalculateVisibleLines();

            // 1. Draw editor background
            dc.DrawRectangle(_editorBackground, null, new Rect(0, 0, ActualWidth, ActualHeight));

            // 2. Draw line number gutter background
            if (ShowLineNumbers)
            {
                dc.DrawRectangle(_lineNumberBackground, null, new Rect(0, 0, LineNumberWidth, ActualHeight));
            }

            // 3. Draw current line highlight (before text)
            RenderCurrentLineHighlight(dc);

            // 4. Draw selection (before text)
            RenderSelection(dc);

            // 5. Draw line numbers
            if (ShowLineNumbers)
            {
                RenderLineNumbers(dc);
            }

            // 6. Draw text content
            RenderTextContent(dc);

            // 7. Draw cursor (blinking will be added later)
            RenderCursor(dc);
        }

        /// <summary>
        /// Calculate which lines are visible in the viewport
        /// Phase 1: Simple calculation (no virtual scrolling yet)
        /// </summary>
        private void CalculateVisibleLines()
        {
            _firstVisibleLine = 0; // Phase 1: always start at 0
            _lastVisibleLine = Math.Min(_document.Lines.Count - 1,
                (int)((ActualHeight - TopMargin) / _lineHeight));
        }

        /// <summary>
        /// Render line numbers in left gutter
        /// </summary>
        private void RenderLineNumbers(DrawingContext dc)
        {
            double y = TopMargin;

            for (int i = _firstVisibleLine; i <= _lastVisibleLine && i < _document.Lines.Count; i++)
            {
                var lineNumber = (i + 1).ToString(); // Display as 1-based
                var formattedText = new FormattedText(
                    lineNumber,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    _typeface,
                    _fontSize * 0.9, // Slightly smaller than main text
                    _lineNumberForeground,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                // Right-align line numbers
                double x = LineNumberWidth - formattedText.Width - LineNumberMargin;

                dc.DrawText(formattedText, new Point(x, y));
                y += _lineHeight;
            }

            // Draw separator line between line numbers and text
            var pen = new Pen(new SolidColorBrush(Color.FromRgb(200, 200, 200)), 1);
            pen.Freeze();
            dc.DrawLine(pen, new Point(LineNumberWidth, 0), new Point(LineNumberWidth, ActualHeight));
        }

        /// <summary>
        /// Render current line highlight
        /// </summary>
        private void RenderCurrentLineHighlight(DrawingContext dc)
        {
            if (_cursorLine < _firstVisibleLine || _cursorLine > _lastVisibleLine)
                return;

            double y = TopMargin + (_cursorLine - _firstVisibleLine) * _lineHeight;
            double x = ShowLineNumbers ? TextAreaLeftOffset : LeftMargin;

            dc.DrawRectangle(_currentLineBackground, null,
                new Rect(x, y, ActualWidth - x, _lineHeight));
        }

        /// <summary>
        /// Render text selection overlay
        /// Phase 1: Basic implementation (will be enhanced in Phase 3)
        /// </summary>
        private void RenderSelection(DrawingContext dc)
        {
            if (_selection.IsEmpty)
                return;

            var start = _selection.NormalizedStart;
            var end = _selection.NormalizedEnd;

            // Phase 1: Only handle single-line selection
            if (start.Line == end.Line && start.Line >= _firstVisibleLine && start.Line <= _lastVisibleLine)
            {
                double y = TopMargin + (start.Line - _firstVisibleLine) * _lineHeight;
                double x1 = (ShowLineNumbers ? TextAreaLeftOffset : LeftMargin) + (start.Column * _charWidth);
                double x2 = (ShowLineNumbers ? TextAreaLeftOffset : LeftMargin) + (end.Column * _charWidth);

                dc.DrawRectangle(_selectionBackground, null, new Rect(x1, y, x2 - x1, _lineHeight));
            }

            // Multi-line selection will be added in Phase 3
        }

        /// <summary>
        /// Render text content (Phase 1: single color, no syntax highlighting)
        /// </summary>
        private void RenderTextContent(DrawingContext dc)
        {
            double x = ShowLineNumbers ? TextAreaLeftOffset : LeftMargin;
            double y = TopMargin;

            for (int i = _firstVisibleLine; i <= _lastVisibleLine && i < _document.Lines.Count; i++)
            {
                var line = _document.Lines[i];

                if (!string.IsNullOrEmpty(line.Text))
                {
                    var formattedText = new FormattedText(
                        line.Text,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        _typeface,
                        _fontSize,
                        _editorForeground,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    dc.DrawText(formattedText, new Point(x, y));
                }

                y += _lineHeight;
            }
        }

        /// <summary>
        /// Render cursor (simple rectangle for Phase 1)
        /// Phase 1: Static cursor, blinking will be added later
        /// </summary>
        private void RenderCursor(DrawingContext dc)
        {
            if (!IsFocused)
                return;

            if (_cursorLine < _firstVisibleLine || _cursorLine > _lastVisibleLine)
                return;

            double y = TopMargin + (_cursorLine - _firstVisibleLine) * _lineHeight;
            double x = (ShowLineNumbers ? TextAreaLeftOffset : LeftMargin) + (_cursorColumn * _charWidth);

            // Draw cursor as vertical line (Insert mode style)
            var cursorPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 0, 0)), 1.5);
            cursorPen.Freeze();

            dc.DrawLine(cursorPen,
                new Point(x, y),
                new Point(x, y + _lineHeight - 2));
        }

        #endregion

        #region Keyboard Input Handling (Phase 1)

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            bool ctrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool shiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

            switch (e.Key)
            {
                case Key.Left:
                    MoveCursor(-1, 0, shiftPressed);
                    e.Handled = true;
                    break;

                case Key.Right:
                    MoveCursor(1, 0, shiftPressed);
                    e.Handled = true;
                    break;

                case Key.Up:
                    MoveCursor(0, -1, shiftPressed);
                    e.Handled = true;
                    break;

                case Key.Down:
                    MoveCursor(0, 1, shiftPressed);
                    e.Handled = true;
                    break;

                case Key.Home:
                    MoveCursorToLineStart(shiftPressed);
                    e.Handled = true;
                    break;

                case Key.End:
                    MoveCursorToLineEnd(shiftPressed);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    InsertNewLine();
                    e.Handled = true;
                    break;

                case Key.Back:
                    DeleteCharBefore();
                    e.Handled = true;
                    break;

                case Key.Delete:
                    DeleteCharAfter();
                    e.Handled = true;
                    break;

                case Key.Tab:
                    InsertTab();
                    e.Handled = true;
                    break;
            }

            InvalidateVisual();
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            base.OnTextInput(e);

            if (!string.IsNullOrEmpty(e.Text))
            {
                foreach (char ch in e.Text)
                {
                    // Skip control characters
                    if (char.IsControl(ch))
                        continue;

                    InsertChar(ch);
                }
                InvalidateVisual();
            }
        }

        #endregion

        #region Cursor Movement

        private void MoveCursor(int deltaColumn, int deltaLine, bool extendSelection)
        {
            // Save old position for selection
            var oldPosition = new TextPosition(_cursorLine, _cursorColumn);

            // Move line
            if (deltaLine != 0)
            {
                _cursorLine = Math.Max(0, Math.Min(_document.Lines.Count - 1, _cursorLine + deltaLine));
                // Clamp column to line length
                _cursorColumn = Math.Min(_cursorColumn, _document.Lines[_cursorLine].Length);
            }

            // Move column
            if (deltaColumn != 0)
            {
                _cursorColumn += deltaColumn;

                // Handle line boundaries
                if (_cursorColumn < 0 && _cursorLine > 0)
                {
                    // Move to end of previous line
                    _cursorLine--;
                    _cursorColumn = _document.Lines[_cursorLine].Length;
                }
                else if (_cursorColumn > _document.Lines[_cursorLine].Length && _cursorLine < _document.Lines.Count - 1)
                {
                    // Move to start of next line
                    _cursorLine++;
                    _cursorColumn = 0;
                }
                else
                {
                    // Clamp to line bounds
                    _cursorColumn = Math.Max(0, Math.Min(_document.Lines[_cursorLine].Length, _cursorColumn));
                }
            }

            // Handle selection
            if (extendSelection)
            {
                if (_selection.IsEmpty)
                    _selection.Start = oldPosition;
                _selection.End = new TextPosition(_cursorLine, _cursorColumn);
            }
            else
            {
                _selection.Clear();
            }
        }

        private void MoveCursorToLineStart(bool extendSelection)
        {
            var oldPosition = new TextPosition(_cursorLine, _cursorColumn);
            _cursorColumn = 0;

            if (extendSelection)
            {
                if (_selection.IsEmpty)
                    _selection.Start = oldPosition;
                _selection.End = new TextPosition(_cursorLine, _cursorColumn);
            }
            else
            {
                _selection.Clear();
            }
        }

        private void MoveCursorToLineEnd(bool extendSelection)
        {
            var oldPosition = new TextPosition(_cursorLine, _cursorColumn);
            _cursorColumn = _document.Lines[_cursorLine].Length;

            if (extendSelection)
            {
                if (_selection.IsEmpty)
                    _selection.Start = oldPosition;
                _selection.End = new TextPosition(_cursorLine, _cursorColumn);
            }
            else
            {
                _selection.Clear();
            }
        }

        #endregion

        #region Text Editing Operations

        private void InsertChar(char ch)
        {
            _document.InsertChar(_cursorLine, _cursorColumn, ch);
            _cursorColumn++;
        }

        private void InsertNewLine()
        {
            _document.InsertNewLine(_cursorLine, _cursorColumn);
            _cursorLine++;
            _cursorColumn = CalculateAutoIndentColumn();
        }

        private void InsertTab()
        {
            // Insert spaces for tab (respects IndentSize)
            int spacesToInsert = _document.IndentSize - (_cursorColumn % _document.IndentSize);
            for (int i = 0; i < spacesToInsert; i++)
            {
                InsertChar(' ');
            }
        }

        private void DeleteCharBefore()
        {
            if (_cursorColumn > 0)
            {
                _cursorColumn--;
                _document.DeleteChar(_cursorLine, _cursorColumn);
            }
            else if (_cursorLine > 0)
            {
                // Delete newline - merge with previous line
                int prevLineLength = _document.Lines[_cursorLine - 1].Length;
                _document.Lines[_cursorLine - 1].Text += _document.Lines[_cursorLine].Text;
                _document.DeleteLine(_cursorLine);
                _cursorLine--;
                _cursorColumn = prevLineLength;
            }
        }

        private void DeleteCharAfter()
        {
            var currentLine = _document.Lines[_cursorLine];

            if (_cursorColumn < currentLine.Length)
            {
                _document.DeleteChar(_cursorLine, _cursorColumn);
            }
            else if (_cursorLine < _document.Lines.Count - 1)
            {
                // Delete newline - merge with next line
                currentLine.Text += _document.Lines[_cursorLine + 1].Text;
                _document.DeleteLine(_cursorLine + 1);
            }
        }

        private int CalculateAutoIndentColumn()
        {
            if (_cursorLine >= _document.Lines.Count)
                return 0;

            var line = _document.Lines[_cursorLine];
            int spaces = 0;

            foreach (char ch in line.Text)
            {
                if (ch == ' ')
                    spaces++;
                else
                    break;
            }

            return spaces;
        }

        #endregion

        #region Document Event Handlers

        private void Document_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Phase 1: Just invalidate visual
            // Phase 3: Will add undo/redo stack here
            InvalidateVisual();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the document model
        /// </summary>
        public JsonDocument Document => _document;

        /// <summary>
        /// Load text content
        /// </summary>
        public void LoadText(string text)
        {
            _document.LoadFromString(text);
            _cursorLine = 0;
            _cursorColumn = 0;
            _selection.Clear();
            InvalidateVisual();
        }

        /// <summary>
        /// Get current text content
        /// </summary>
        public string GetText()
        {
            return _document.SaveToString();
        }

        /// <summary>
        /// Get current cursor position
        /// </summary>
        public TextPosition CursorPosition => new TextPosition(_cursorLine, _cursorColumn);

        #endregion
    }
}
