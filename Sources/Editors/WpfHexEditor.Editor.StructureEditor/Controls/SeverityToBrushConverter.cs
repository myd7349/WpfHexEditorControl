//////////////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Project: WpfHexEditor.Editor.StructureEditor
// File: Controls/SeverityToBrushConverter.cs
// Description: IValueConverter mapping ValidationSeverity enum to a SolidColorBrush.
//////////////////////////////////////////////////////

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WpfHexEditor.Editor.Core.Validation;

namespace WpfHexEditor.Editor.StructureEditor.Controls;

[ValueConversion(typeof(ValidationSeverity), typeof(Brush))]
internal sealed class SeverityToBrushConverter : IValueConverter
{
    public static readonly SeverityToBrushConverter Instance = new();

    // Hardcoded fallbacks used only when theme resources are unavailable.
    private static readonly SolidColorBrush FallbackErrorBrush   = new(Color.FromRgb(0xFF, 0x4C, 0x4C));
    private static readonly SolidColorBrush FallbackWarningBrush = new(Color.FromRgb(0xFF, 0xA5, 0x00));
    private static readonly SolidColorBrush FallbackInfoBrush    = new(Color.FromRgb(0x88, 0xBB, 0xFF));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var severity = value is ValidationSeverity sev ? sev : ValidationSeverity.Info;
        return severity switch
        {
            ValidationSeverity.Error   => Resolve("SE_ValidationErrorBrush",   FallbackErrorBrush),
            ValidationSeverity.Warning => Resolve("SE_ValidationWarningBrush", FallbackWarningBrush),
            _                          => Resolve("SE_ValidationInfoBrush",    FallbackInfoBrush),
        };
    }

    private static Brush Resolve(string resourceKey, Brush fallback) =>
        Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
