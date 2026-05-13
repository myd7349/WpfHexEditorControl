// Project      : WpfHexEditor.App
// File         : Options/Snippets/SnippetVariablePicker.cs
// Description  : WrapPanel of chip buttons, one per known snippet variable.
//                Fires VariableChosen when a chip is clicked.
// Architecture : UserControl — code-behind only.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using WpfHexEditor.Editor.CodeEditor.Properties;
using WpfHexEditor.Editor.CodeEditor.Snippets;

namespace WpfHexEditor.App.Options.Snippets;

/// <summary>
/// Displays clickable chip buttons for each known snippet variable.
/// Subscribe to <see cref="VariableChosen"/> to receive the selected name.
/// </summary>
public sealed class SnippetVariablePicker : UserControl
{
    public event Action<string>? VariableChosen;

    public SnippetVariablePicker()
    {
        var label = new TextBlock
        {
            Text       = CodeEditorResources.Snippets_Page_VarPickerLabel,
            FontWeight = FontWeights.SemiBold,
            Margin     = new Thickness(0, 0, 0, 4),
        };

        var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var varName in SnippetVariableContext.KnownVariables)
            wrap.Children.Add(MakeChip(varName));

        var stack = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
        stack.Children.Add(label);
        stack.Children.Add(wrap);
        Content = stack;
    }

    private Button MakeChip(string varName)
    {
        var btn = new Button
        {
            Content = $"${{{varName}}}",
            Margin  = new Thickness(0, 0, 4, 4),
            Padding = new Thickness(6, 2, 6, 2),
            FontSize = 10,
            ToolTip  = ResolveDescription(varName),
        };
        btn.SetResourceReference(Control.BackgroundProperty, "CE_SnippetVarBrush");
        btn.SetResourceReference(Control.ForegroundProperty, "CE_Background");
        btn.Click += (_, _) => VariableChosen?.Invoke(varName);
        return btn;
    }

    private static string ResolveDescription(string varName)
    {
        var key   = $"Snippet_Var_{varName}";
        var value = CodeEditorResources.ResourceManager
            .GetString(key, CultureInfo.CurrentUICulture);
        return value ?? varName;
    }
}
