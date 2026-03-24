// Project      : WpfHexEditorControl
// File         : Converters/DiffConverters.cs
// Description  : Value converters used by DiffHubPanel:
//                IntToStarConverter      — int 0-100 → proportional GridLength n*
//                IntToAntiStarConverter  — int 0-100 → complementary GridLength (100-n)*
//                BoolToVisibilityConverter — bool → Visibility (Visible / Collapsed)

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfHexEditor.Plugins.FileComparison.Converters;

/// <summary>Converts a 0-100 integer to a proportional GridLength n*.</summary>
[ValueConversion(typeof(int), typeof(GridLength))]
public sealed class IntToStarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int v and > 0 ? new GridLength(v, GridUnitType.Star) : new GridLength(0, GridUnitType.Star);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

/// <summary>Converts a 0-100 integer to the complementary GridLength (100-n)*.</summary>
[ValueConversion(typeof(int), typeof(GridLength))]
public sealed class IntToAntiStarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int anti = value is int v ? 100 - v : 100;
        return new GridLength(Math.Max(0, anti), GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

/// <summary>Converts a bool to Visibility — true → Visible, false → Collapsed.</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}
