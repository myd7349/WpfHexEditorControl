// Project      : WpfHexEditorControl
// File         : Converters/BinaryByteKindToBrushConverter.cs
// Description  : Maps a BinaryByteKind to the corresponding BDiff_* background brush
//                from the active theme (dynamic resource lookup at render time).
// Architecture : WPF converter — stateless, thread-safe.

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WpfHexEditor.Core.Diff.Models;

namespace WpfHexEditor.Plugins.FileComparison.Converters;

/// <summary>
/// Converts a <see cref="BinaryByteKind"/> to the matching per-byte background
/// <see cref="Brush"/> using the <c>BDiff_*</c> dynamic resources from the active theme.
/// Returns <see cref="Brushes.Transparent"/> for <see cref="BinaryByteKind.Equal"/>
/// so that the row background shows through unmodified bytes.
/// </summary>
[ValueConversion(typeof(BinaryByteKind), typeof(Brush))]
public sealed class BinaryByteKindToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value is BinaryByteKind kind ? kind switch
        {
            BinaryByteKind.Modified      => "BDiff_ModifiedByteBrush",
            BinaryByteKind.InsertedRight => "BDiff_InsertedByteBrush",
            BinaryByteKind.DeletedLeft   => "BDiff_DeletedByteBrush",
            BinaryByteKind.Padding       => "BDiff_PaddingBrush",
            _                            => null   // Equal → transparent
        } : null;

        if (key is null) return Brushes.Transparent;
        return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}

/// <summary>
/// Converts an <see cref="BinaryHexByteCell.IsPrintable"/> boolean to the appropriate
/// foreground brush: <c>BDiff_AsciiForegroundBrush</c> for printable chars,
/// <c>BDiff_AsciiNonPrintableBrush</c> for dots.
/// </summary>
[ValueConversion(typeof(bool), typeof(Brush))]
public sealed class AsciiPrintableToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value is true ? "BDiff_AsciiForegroundBrush" : "BDiff_AsciiNonPrintableBrush";
        return Application.Current?.TryFindResource(key) as Brush ?? Brushes.DimGray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
