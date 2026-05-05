// ==========================================================
// Project: WpfHexEditor.Editor.DocumentEditor
// File: Dialogs/ColorInputDialog.cs
// Description: Lightweight hex-color input dialog (no WinForms dependency).
// ==========================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfHexEditor.Editor.DocumentEditor.Dialogs;

internal sealed class ColorInputDialog : Window
{
    private readonly TextBox _input;
    private readonly Border  _preview;

    public string HexColor { get; private set; }

    public ColorInputDialog(string initialHex)
    {
        HexColor = initialHex;
        Title    = "Color";
        Width    = 280;
        SizeToContent = SizeToContent.Height;
        ResizeMode    = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        _preview = new Border
        {
            Width           = 40,
            Height          = 24,
            BorderThickness = new Thickness(1),
            BorderBrush     = SystemColors.ActiveBorderBrush,
            Margin          = new Thickness(0, 0, 8, 0)
        };

        _input = new TextBox
        {
            Text = initialHex,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinWidth = 100
        };
        _input.TextChanged += (_, _) =>
        {
            try { _preview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_input.Text)); }
            catch { _preview.Background = null; }
        };

        var ok     = new Button { Content = "OK",     Width = 60, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "Cancel", Width = 60, IsCancel  = true };
        ok.Click     += (_, _) => { HexColor = _input.Text; DialogResult = true; };
        cancel.Click += (_, _) => DialogResult = false;

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        btnRow.Children.Add(ok);
        btnRow.Children.Add(cancel);

        var row1 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        row1.Children.Add(_preview);
        row1.Children.Add(_input);

        var root = new StackPanel { Margin = new Thickness(16) };
        root.Children.Add(row1);
        root.Children.Add(btnRow);

        Content = root;

        // Trigger preview for initial color
        try { _preview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(initialHex)); }
        catch { _preview.Background = null; }
    }
}
