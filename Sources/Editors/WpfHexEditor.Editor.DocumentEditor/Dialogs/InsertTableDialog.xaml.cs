// ==========================================================
// Project: WpfHexEditor.Editor.DocumentEditor
// File: Dialogs/InsertTableDialog.xaml.cs
// Description: Modal dialog for inserting a table block.
// ==========================================================

using System.Windows;

namespace WpfHexEditor.Editor.DocumentEditor.Dialogs;

public partial class InsertTableDialog : Window
{
    public int Rows    { get; private set; } = 3;
    public int Columns { get; private set; } = 3;

    public InsertTableDialog()
    {
        InitializeComponent();
        PART_Rows.Focus();
    }

    private void OnOkClicked(object sender, RoutedEventArgs e)
    {
        Rows         = int.TryParse(PART_Rows.Text,    out int r) ? Math.Clamp(r, 1, 20) : 3;
        Columns      = int.TryParse(PART_Columns.Text, out int c) ? Math.Clamp(c, 1, 10) : 3;
        DialogResult = true;
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e) => DialogResult = false;
}
