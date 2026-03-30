// Project      : WpfHexEditorControl
// File         : Converters/DiffConverters.cs
// Description  : Value converters used by DiffHubPanel, DiffViewerDocument, and FormatSelectionDialog:
//                IntToStarConverter             — int 0-100 → proportional GridLength n*
//                IntToAntiStarConverter         — int 0-100 → complementary GridLength (100-n)*
//                BoolToVisibilityConverter      — bool → Visibility (Visible / Collapsed)
//                KindToBrushConverter           — DiffLineRow.Kind → background SolidColorBrush
//                KindToGlyphConverter           — DiffLineRow.Kind → Segoe MDL2 Assets glyph char
//                ConfidenceToPercentageConverter — double 0.0-1.0 → double 0-100 (for ProgressBar)
//                NullToBooleanConverter         — object? → bool (null → false)

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

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

/// <summary>Converts a bool to Visibility — true → Collapsed, false → Visible (inverse).</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class NotBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}

/// <summary>
/// Converts a DiffLineRow.Kind string to the matching row-background brush
/// using the DF_* dynamic resources from the active theme.
/// Falls back to Transparent when the kind is Equal or Empty.
/// </summary>
[ValueConversion(typeof(string), typeof(Brush))]
public sealed class KindToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = (value as string) switch
        {
            "Modified"      => "DF_ModifiedLineBrush",
            "InsertedRight" => "DF_InsertedLineBrush",
            "DeletedLeft"   => "DF_DeletedLineBrush",
            _               => null
        };
        if (key is null) return Brushes.Transparent;
        return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

/// <summary>
/// Converts a confidence score (0.0–1.0) to a percentage value (0–100) for ProgressBar binding.
/// Used by FormatSelectionDialog.
/// </summary>
[ValueConversion(typeof(double), typeof(double))]
public sealed class ConfidenceToPercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double confidence ? confidence * 100.0 : 0.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

/// <summary>
/// Returns true when the bound value is non-null; false otherwise.
/// Used to enable/disable the Select button in FormatSelectionDialog.
/// </summary>
[ValueConversion(typeof(object), typeof(bool))]
public sealed class NullToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

/// <summary>
/// Converts a DiffLineRow.Kind string to a Segoe MDL2 Assets glyph character
/// suitable for a status icon beside a diff row.
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public sealed class KindToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value as string) switch
        {
            "Modified"      => "\uE8D4",   // Edit
            "InsertedRight" => "\uE710",   // Add
            "DeletedLeft"   => "\uE738",   // Delete
            _               => string.Empty
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
