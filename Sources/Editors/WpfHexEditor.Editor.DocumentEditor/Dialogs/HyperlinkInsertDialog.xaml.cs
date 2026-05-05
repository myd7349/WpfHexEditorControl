// ==========================================================
// Project: WpfHexEditor.Editor.DocumentEditor
// File: Dialogs/HyperlinkInsertDialog.xaml.cs
// Description: Modal dialog for inserting a hyperlink block.
// ==========================================================

using System.Windows;

namespace WpfHexEditor.Editor.DocumentEditor.Dialogs;

public partial class HyperlinkInsertDialog : Window
{
    public string DisplayText { get; private set; } = string.Empty;
    public string Url         { get; private set; } = string.Empty;

    public HyperlinkInsertDialog(string initialText = "")
    {
        InitializeComponent();
        PART_DisplayText.Text = initialText;
        PART_DisplayText.Focus();
    }

    private void OnOkClicked(object sender, RoutedEventArgs e)
    {
        DisplayText  = PART_DisplayText.Text.Trim();
        Url          = PART_Url.Text.Trim();
        DialogResult = true;
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e) => DialogResult = false;
}
