// Project      : WpfHexEditorControl
// File         : Converters/FolderColorConverter.cs
// Description  : Returns AR_FolderBrush for folders, AR_FileBrush for files.
//                Singleton pattern for use in XAML x:Static binding.
//
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfHexEditor.Plugins.ArchiveExplorer.Converters;

/// <summary>
/// bool (IsFolder) → AR_FolderBrush (true) or AR_FileBrush (false).
/// </summary>
[ValueConversion(typeof(bool), typeof(Brush))]
public sealed class FolderColorConverter : IValueConverter
{
    public static readonly FolderColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value is true ? "AR_FolderBrush" : "AR_FileBrush";
        return Application.Current?.TryFindResource(key) as Brush
               ?? (value is true ? Brushes.Goldenrod : Brushes.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
