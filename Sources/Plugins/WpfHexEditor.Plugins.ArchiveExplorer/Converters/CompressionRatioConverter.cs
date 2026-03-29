// Project      : WpfHexEditorControl
// File         : Converters/CompressionRatioConverter.cs
// Description  : Converts (Size, CompressedSize) pair → formatted ratio string.
//                Used as a MultiBinding converter on ratio badge TextBlocks.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Converters;

/// <summary>
/// IMultiValueConverter: values[0] = Size (long), values[1] = CompressedSize (long).
/// Returns e.g. "42%" or empty string when size is 0 or compression is trivial.
/// </summary>
[ValueConversion(typeof(long[]), typeof(string))]
public sealed class CompressionRatioConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is not [long size, long compressed]) return string.Empty;
        if (size <= 0) return string.Empty;
        var ratio = 1.0 - (double)compressed / size;
        return ratio < 0.01 ? string.Empty : $"{ratio:P0}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => [DependencyProperty.UnsetValue, DependencyProperty.UnsetValue];
}
