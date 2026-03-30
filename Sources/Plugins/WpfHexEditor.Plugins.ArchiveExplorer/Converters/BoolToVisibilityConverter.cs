// Project      : WpfHexEditorControl
// File         : Converters/BoolToVisibilityConverter.cs
// Description  : bool → Visibility (true=Visible, false=Collapsed).
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}
