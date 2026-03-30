// Project      : WpfHexEditorControl
// File         : Converters/FileSizeConverter.cs
// Description  : Converts a long (byte count) to a human-readable size string.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Converters;

[ValueConversion(typeof(long), typeof(string))]
public sealed class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long bytes) return string.Empty;
        return bytes switch
        {
            0                      => string.Empty,
            < 1024L                => $"{bytes} B",
            < 1024L * 1024         => $"{bytes / 1024.0:F1} KB",
            < 1024L * 1024 * 1024  => $"{bytes / 1024.0 / 1024:F1} MB",
            _                      => $"{bytes / 1024.0 / 1024 / 1024:F2} GB",
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
