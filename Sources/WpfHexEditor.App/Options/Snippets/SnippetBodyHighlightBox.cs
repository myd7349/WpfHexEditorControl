// Project      : WpfHexEditor.App
// File         : Options/Snippets/SnippetBodyHighlightBox.cs
// Description  : Snippet body editor with syntax-colored token highlighting.
//                Uses a RichTextBox; rebuilds the FlowDocument with typed Runs
//                on each TextChanged (debounced at 50 ms).
// Architecture : UserControl — no XAML file; code-behind only.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using WpfHexEditor.Editor.CodeEditor.Snippets;

namespace WpfHexEditor.App.Options.Snippets;

/// <summary>
/// Snippet body editor that highlights <c>${Variable}</c> and <c>$cursor</c>
/// tokens with theme-aware brushes.
/// </summary>
public sealed class SnippetBodyHighlightBox : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string),
            typeof(SnippetBodyHighlightBox),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnTextPropertyChanged));

    private readonly RichTextBox     _rtb;
    private readonly DispatcherTimer _debounce;
    private bool                     _updating;

    public event TextChangedEventHandler? TextChanged;

    public SnippetBodyHighlightBox()
    {
        _rtb      = CreateRtb();
        _debounce = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(50),
        };
        _debounce.Tick    += OnDebounceTick;
        _rtb.TextChanged  += OnRtbTextChanged;
        Unloaded          += OnUnloaded;
        Content = _rtb;
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static RichTextBox CreateRtb()
    {
        var rtb = new RichTextBox
        {
            AcceptsReturn = true,
            AcceptsTab    = true,
            Height        = 140,
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        rtb.SetResourceReference(Control.FontFamilyProperty, "CE_FontFamily");
        rtb.SetResourceReference(Control.FontSizeProperty,   "CE_FontSize");
        rtb.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Tab)
            {
                rtb.CaretPosition.InsertTextInRun("\t");
                e.Handled = true;
            }
        };
        return rtb;
    }

    private void OnRtbTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        _debounce.Stop();
        _debounce.Start();
        TextChanged?.Invoke(this, e);
    }

    private void OnDebounceTick(object? sender, EventArgs e)
    {
        _debounce.Stop();
        var plain = new TextRange(_rtb.Document.ContentStart, _rtb.Document.ContentEnd).Text;
        _updating = true;
        SetValue(TextProperty, plain.TrimEnd('\r', '\n'));
        _updating = false;
        RebuildHighlight(plain);
    }

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (SnippetBodyHighlightBox)d;
        if (box._updating) return;
        var text = (string?)e.NewValue ?? string.Empty;
        box.RebuildHighlight(text);
    }

    private void RebuildHighlight(string text)
    {
        _updating = true;
        try
        {
            var caret    = _rtb.CaretPosition;
            var offset   = caret.GetOffsetToPosition(_rtb.Document.ContentStart);
            var doc      = BuildDocument(text);
            _rtb.Document = doc;
            TryRestoreCaret(Math.Abs(offset));
        }
        finally
        {
            _updating = false;
        }
    }

    private FlowDocument BuildDocument(string text)
    {
        var para = new Paragraph { Margin = new Thickness(0) };
        foreach (var token in SnippetBodyTokenizer.Tokenize(text))
            para.Inlines.Add(CreateRun(token));

        return new FlowDocument(para)
        {
            PagePadding = new Thickness(4),
        };
    }

    private Run CreateRun(SnippetBodyToken token)
    {
        var run = new Run(token.Text);
        switch (token.Kind)
        {
            case SnippetTokenKind.Variable:
                run.SetResourceReference(TextElement.ForegroundProperty, "CE_SnippetVarBrush");
                break;
            case SnippetTokenKind.CursorMarker:
                run.SetResourceReference(TextElement.ForegroundProperty, "CE_SnippetCursorBrush");
                run.FontWeight = FontWeights.Bold;
                break;
        }
        return run;
    }

    private void TryRestoreCaret(int offset)
    {
        try
        {
            var pos = _rtb.Document.ContentStart.GetPositionAtOffset(offset);
            if (pos is not null) _rtb.CaretPosition = pos;
        }
        catch { /* best-effort */ }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _debounce.Stop();
        _debounce.Tick   -= OnDebounceTick;
        _rtb.TextChanged -= OnRtbTextChanged;
        Unloaded         -= OnUnloaded;
    }

    /// <summary>Inserts <paramref name="text"/> at the current caret position.</summary>
    public void InsertAtCaret(string text)
    {
        _rtb.CaretPosition.InsertTextInRun(text);
        _rtb.Focus();
    }
}
