// Project      : WpfHexEditorControl
// File         : Converters/ArchiveFormatToIconGlyphConverter.cs
// Description  : Maps ArchiveFormat enum to a Segoe MDL2 Assets glyph for toolbar/info.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.Globalization;
using System.Windows.Data;
using WpfHexEditor.Plugins.ArchiveExplorer.Models;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Converters;

[ValueConversion(typeof(ArchiveFormat), typeof(string))]
public sealed class ArchiveFormatToIconGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is ArchiveFormat fmt ? fmt switch
        {
            ArchiveFormat.Zip      => "\uE7C3",   // ZipFolder
            ArchiveFormat.SevenZip => "\uE7C3",
            ArchiveFormat.Rar      => "\uE7C3",
            ArchiveFormat.Tar      => "\uE8B7",   // Package
            ArchiveFormat.GZip     => "\uE8B7",
            ArchiveFormat.BZip2    => "\uE8B7",
            ArchiveFormat.Xz       => "\uE8B7",
            _                      => "\uE8F4",   // Document
        } : "\uE8F4";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => System.Windows.DependencyProperty.UnsetValue;
}
