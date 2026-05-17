// Project      : WpfHexEditor.App
// File         : Options/Snippets/SnippetPreviewPanel.cs
// Description  : Read-only panel showing the expanded snippet body using a
//                mock SnippetVariableContext with fixed example values.
// Architecture : UserControl — code-behind only.

using System;
using System.Windows;
using System.Windows.Controls;
using WpfHexEditor.Editor.CodeEditor.Properties;
using WpfHexEditor.Editor.CodeEditor.Snippets;
using static WpfHexEditor.Editor.CodeEditor.Snippets.SnippetBodyTokenizer;

namespace WpfHexEditor.App.Options.Snippets;

/// <summary>
/// Displays the expanded snippet body with example variable values substituted.
/// Call <see cref="Refresh"/> whenever the body changes.
/// </summary>
public sealed class SnippetPreviewPanel : UserControl
{
    private readonly TextBox _previewBox;

    public SnippetPreviewPanel()
    {
        var label = new TextBlock
        {
            Text       = CodeEditorResources.Snippets_Page_PreviewLabel,
            FontWeight = FontWeights.SemiBold,
            Margin     = new Thickness(0, 8, 0, 4),
        };
        _previewBox = CreatePreviewBox();
        var stack = new StackPanel();
        stack.Children.Add(label);
        stack.Children.Add(_previewBox);
        Content = stack;
    }

    private static TextBox CreatePreviewBox()
    {
        var box = new TextBox
        {
            IsReadOnly    = true,
            AcceptsReturn = true,
            Height        = 100,
            TextWrapping  = TextWrapping.NoWrap,
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        box.SetResourceReference(Control.FontFamilyProperty, "CE_FontFamily");
        box.SetResourceReference(Control.FontSizeProperty,   "CE_FontSize");
        box.SetResourceReference(Control.BackgroundProperty, "CE_Background");
        box.SetResourceReference(Control.ForegroundProperty, "CE_Foreground");
        return box;
    }

    /// <summary>Re-expands <paramref name="body"/> with mock values and updates the preview.</summary>
    public void Refresh(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            _previewBox.Clear();
            return;
        }
        var expanded = SnippetVariableExpander.Expand(body, BuildMockContext());
        _previewBox.Text = expanded.Replace(CursorMarker, "|");
    }

    private static SnippetVariableContext BuildMockContext() => new()
    {
        FilePath        = @"C:\MyProject\Program.cs",
        SelectedText    = "selectedText",
        CurrentLineText = "    public void Example()",
        CurrentLine     = 14,
        CurrentColumn   = 4,
        IndentText      = "    ",
        ProjectName     = "MyProject",
        ClipboardText   = "clipboard content",
        Timestamp       = DateTime.Today,
    };
}
