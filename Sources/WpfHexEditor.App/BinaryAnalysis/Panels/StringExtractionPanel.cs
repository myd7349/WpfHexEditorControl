//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfHexEditor.App.BinaryAnalysis.Services;
using WpfHexEditor.App.BinaryAnalysis.ViewModels;
using WpfHexEditor.SDK.Contracts;

namespace WpfHexEditor.App.BinaryAnalysis.Panels;

/// <summary>#110 String Extraction — code-behind-only panel.</summary>
public sealed class StringExtractionPanel : UserControl
{
    private readonly StringExtractionViewModel _vm = new();

    public StringExtractionPanel()
    {
        var runBtn    = new Button { Content = "Run",    Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(10, 2, 10, 2) };
        var cancelBtn = new Button { Content = "Cancel", Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(10, 2, 10, 2) };
        var minLabel  = new TextBlock { Text = "Min length:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };
        var minBox    = new Slider { Minimum = 4, Maximum = 20, Value = 4, Width = 80, VerticalAlignment = VerticalAlignment.Center };
        var asciiChk  = new CheckBox { Content = "ASCII",   IsChecked = true, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
        var utf16Chk  = new CheckBox { Content = "UTF-16",  IsChecked = true, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
        var filterBox = new TextBox { Width = 140, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };
        var statusTxt = new TextBlock { Margin = new Thickness(0, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 4, 4, 4) };
        toolbar.Children.Add(runBtn);
        toolbar.Children.Add(cancelBtn);
        toolbar.Children.Add(minLabel);
        toolbar.Children.Add(minBox);
        toolbar.Children.Add(asciiChk);
        toolbar.Children.Add(utf16Chk);
        toolbar.Children.Add(new TextBlock { Text = "Filter:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) });
        toolbar.Children.Add(filterBox);
        toolbar.Children.Add(statusTxt);

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            CanUserAddRows      = false,
            CanUserDeleteRows   = false,
            IsReadOnly          = true,
            VirtualizingPanel.IsVirtualizing = true,
        };
        grid.Columns.Add(MakeCol("Offset",   nameof(StringRun.Offset),   80,  "X8"));
        grid.Columns.Add(MakeCol("Length",   nameof(StringRun.Length),   60));
        grid.Columns.Add(MakeCol("Encoding", nameof(StringRun.Encoding), 70));
        grid.Columns.Add(MakeCol("Value",    nameof(StringRun.Value),    0));

        grid.ItemsSource = _vm.Results;

        var root = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);
        root.Children.Add(grid);
        Content = root;

        // Bindings
        statusTxt.SetBinding(TextBlock.TextProperty, new Binding(nameof(_vm.IsBusy)) { Source = _vm, Converter = new BusyToStatusConverter() });
        minBox.ValueChanged += (_, e) => _vm.MinLength = (int)e.NewValue;
        filterBox.TextChanged += (_, _) => _vm.Filter = filterBox.Text;
        asciiChk.Checked   += (_, _) => _vm.ShowAscii  = true;
        asciiChk.Unchecked += (_, _) => _vm.ShowAscii  = false;
        utf16Chk.Checked   += (_, _) => _vm.ShowUtf16  = true;
        utf16Chk.Unchecked += (_, _) => _vm.ShowUtf16  = false;
        runBtn.Click    += async (_, _) => await _vm.RunAsync();
        cancelBtn.Click += (_, _) => _vm.Cancel();
    }

    public void SetContext(IIDEHostContext context) => _vm.SetContext(context);
    public void OnFileOpened() { _vm.Results.Clear(); }

    private static DataGridTextColumn MakeCol(string header, string path, double width, string? format = null)
    {
        var binding = new Binding(path);
        if (format is not null) binding.StringFormat = $"{{0:{format}}}";
        return new DataGridTextColumn
        {
            Header  = header,
            Binding = binding,
            Width   = width > 0 ? width : double.NaN,
        };
    }
}

file sealed class BusyToStatusConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type t, object p, System.Globalization.CultureInfo c)
        => value is true ? "Running…" : string.Empty;
    public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
        => throw new NotSupportedException();
}
