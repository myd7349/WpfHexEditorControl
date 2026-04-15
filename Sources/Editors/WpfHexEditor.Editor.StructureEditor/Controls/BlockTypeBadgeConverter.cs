//////////////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Project: WpfHexEditor.Editor.StructureEditor
// File: Controls/BlockTypeBadgeConverter.cs
// Description: Maps a WHFMT block type string to a 2-char badge code.
//////////////////////////////////////////////////////

using System.Globalization;
using System.Windows.Data;

namespace WpfHexEditor.Editor.StructureEditor.Controls;

[ValueConversion(typeof(string), typeof(string))]
internal sealed class BlockTypeBadgeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string s ? s switch
        {
            "field"                => "FD",
            "conditional"          => "IF",
            "loop"                 => "LP",
            "pointer"              => "PT",
            "repeating"            => "RP",
            "computeFromVariables" => "CV",
            "metadata"             => "MD",
            "signature"            => "SG",
            "action"               => "AC",
            "data"                 => "DA",
            "header"               => "HD",
            _                      => "??"
        } : "??";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Windows.DependencyProperty.UnsetValue;
}
