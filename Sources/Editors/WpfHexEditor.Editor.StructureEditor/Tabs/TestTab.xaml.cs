//////////////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Project: WpfHexEditor.Editor.StructureEditor
// File: Tabs/TestTab.xaml.cs
// Description: Code-behind for the Test Panel tab.
//////////////////////////////////////////////////////

using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfHexEditor.Editor.StructureEditor.Services;
using WpfHexEditor.Editor.StructureEditor.ViewModels;

namespace WpfHexEditor.Editor.StructureEditor.Tabs;

/// <summary>
/// Converts a block offset of -1 (skipped/summary blocks) to "—"; other values to "0xNNNN".
/// </summary>
[ValueConversion(typeof(long), typeof(string))]
public sealed class NegativeOffsetConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is long l && l >= 0 ? $"0x{l:X}" : "—";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed partial class TestTab : UserControl
{
    public TestTab() => InitializeComponent();

    private TestTabViewModel? VM => DataContext as TestTabViewModel;

    private async void OnRunClicked(object sender, RoutedEventArgs e)
    {
        var vm = VM;
        if (vm is null) return;

        if (Tag is not WpfHexEditor.Core.FormatDetection.FormatDefinition def) return;

        SummaryBar.Visibility = Visibility.Visible;
        await vm.RunAsync(def);
    }

    private void OnCopyClicked(object sender, RoutedEventArgs e)
    {
        var vm = VM;
        if (vm is null || vm.Results.Count == 0) return;

        var sb = new StringBuilder();
        sb.AppendLine("Status\tBlock Name\tType\tOffset\tLen\tRaw (hex)\tParsed Value\tNote");

        foreach (BlockTestResult r in vm.Results)
        {
            string offset = r.Offset >= 0 ? $"0x{r.Offset:X}" : "—";
            sb.AppendLine($"{r.Status}\t{r.BlockName}\t{r.BlockType}\t{offset}\t{r.Length}\t{r.RawHex}\t{r.ParsedValue}\t{r.Note}");
        }

        try { Clipboard.SetText(sb.ToString()); }
        catch { /* Clipboard unavailable — silently ignore */ }
    }
}
