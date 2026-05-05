// ==========================================================
// Project: WpfHexEditor.Editor.DocumentEditor
// File: Dialogs/PageStyleDialog.xaml.cs
// Description:
//     6-tab page style dialog (Page / Background / Header / Footer / Border / Columns).
//     Emits SettingsApplied event on Apply/OK; caller updates the renderer.
// ==========================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfHexEditor.Editor.DocumentEditor.Core.Options;

namespace WpfHexEditor.Editor.DocumentEditor.Dialogs;

public partial class PageStyleDialog : Window
{
    public event EventHandler<DocumentPageSettings>? SettingsApplied;

    private DocumentPageSettings _current;
    private string? _bgColor;
    private string  _borderColor;

    public PageStyleDialog(DocumentPageSettings initial)
    {
        _current     = initial;
        _bgColor     = initial.BackgroundColor;
        _borderColor = initial.BorderColor;

        InitializeComponent();
        Loaded += (_, _) => PopulateFromSettings(initial);
    }

    private void PopulateFromSettings(DocumentPageSettings s)
    {
        // Page tab
        SelectComboByTag(PART_PageFormat, s.PageSize.ToString());
        PART_Portrait.IsChecked  = s.Orientation == DocumentPageOrientation.Portrait;
        PART_Landscape.IsChecked = s.Orientation == DocumentPageOrientation.Landscape;
        PART_PageWidth.Text  = ((int)s.EffectivePageWidth).ToString();
        PART_PageHeight.Text = ((int)s.EffectivePageHeight).ToString();
        PART_MarginLeft.Text   = ((int)s.MarginLeft).ToString();
        PART_MarginRight.Text  = ((int)s.MarginRight).ToString();
        PART_MarginTop.Text    = ((int)s.MarginTop).ToString();
        PART_MarginBottom.Text = ((int)s.MarginBottom).ToString();

        // Background
        UpdateBgPreview();

        // Header
        PART_HeaderEnabled.IsChecked = s.HeaderEnabled;
        PART_HeaderOptions.IsEnabled = s.HeaderEnabled;
        PART_HdrHeight.Text   = ((int)s.HeaderHeightPx).ToString();
        PART_HdrMargin.Text   = ((int)s.HeaderMarginPx).ToString();
        PART_HdrDiffFirst.IsChecked = s.HeaderDifferentFirstPage;
        PART_HdrSameLR.IsChecked    = s.HeaderSameLeftRight;

        // Footer
        PART_FooterEnabled.IsChecked = s.FooterEnabled;
        PART_FooterOptions.IsEnabled = s.FooterEnabled;
        PART_FtrHeight.Text   = ((int)s.FooterHeightPx).ToString();
        PART_FtrMargin.Text   = ((int)s.FooterMarginPx).ToString();
        PART_FtrDiffFirst.IsChecked = s.FooterDifferentFirstPage;
        PART_FtrSameLR.IsChecked    = s.FooterSameLeftRight;

        // Border
        SelectComboByTag(PART_BorderStyle, s.BorderStyle.ToString());
        _borderColor = s.BorderColor;
        UpdateBorderPreview();
        PART_BorderWidth.Text = s.BorderWidthPx.ToString("0.##");

        // Columns
        SelectComboByContent(PART_ColCount, s.ColumnCount.ToString());
        PART_ColGap.Text = s.ColumnGapPx.ToString("0.##");
        PART_ColSeparator.IsChecked = s.ShowColumnSeparatorLine;
    }

    private DocumentPageSettings BuildSettings()
    {
        var pageSize = PART_PageFormat.SelectedItem is ComboBoxItem fmtItem
            ? Enum.TryParse<DocumentPageSize>(fmtItem.Tag?.ToString(), out var ps) ? ps : _current.PageSize
            : _current.PageSize;

        var orient = PART_Landscape.IsChecked == true
            ? DocumentPageOrientation.Landscape
            : DocumentPageOrientation.Portrait;

        double marginLeft   = ParsePx(PART_MarginLeft.Text,   _current.MarginLeft);
        double marginRight  = ParsePx(PART_MarginRight.Text,  _current.MarginRight);
        double marginTop    = ParsePx(PART_MarginTop.Text,    _current.MarginTop);
        double marginBottom = ParsePx(PART_MarginBottom.Text, _current.MarginBottom);

        double hdrH = ParsePx(PART_HdrHeight.Text, _current.HeaderHeightPx);
        double hdrM = ParsePx(PART_HdrMargin.Text, _current.HeaderMarginPx);
        double ftrH = ParsePx(PART_FtrHeight.Text, _current.FooterHeightPx);
        double ftrM = ParsePx(PART_FtrMargin.Text, _current.FooterMarginPx);

        var borderStyle = PART_BorderStyle.SelectedItem is ComboBoxItem bsi
            ? Enum.TryParse<DocumentPageBorderStyle>(bsi.Tag?.ToString(), out var bst) ? bst : _current.BorderStyle
            : _current.BorderStyle;

        int colCount = PART_ColCount.SelectedItem is ComboBoxItem cci &&
                       int.TryParse(cci.Content?.ToString(), out int cc) ? cc : _current.ColumnCount;
        double colGap = ParsePx(PART_ColGap.Text, _current.ColumnGapPx);

        return _current.WithAll(
            pageSize:                pageSize,
            orientation:             orient,
            marginLeft:              marginLeft,
            marginRight:             marginRight,
            marginTop:               marginTop,
            marginBottom:            marginBottom,
            backgroundColor:         _bgColor,
            headerEnabled:           PART_HeaderEnabled.IsChecked == true,
            headerHeightPx:          hdrH,
            headerMarginPx:          hdrM,
            headerDifferentFirstPage: PART_HdrDiffFirst.IsChecked == true,
            headerSameLeftRight:     PART_HdrSameLR.IsChecked == true,
            footerEnabled:           PART_FooterEnabled.IsChecked == true,
            footerHeightPx:          ftrH,
            footerMarginPx:          ftrM,
            footerDifferentFirstPage: PART_FtrDiffFirst.IsChecked == true,
            footerSameLeftRight:     PART_FtrSameLR.IsChecked == true,
            borderStyle:             borderStyle,
            borderColor:             _borderColor,
            borderWidthPx:           ParsePx(PART_BorderWidth.Text, _current.BorderWidthPx),
            columnCount:             colCount,
            columnGapPx:             colGap,
            showColumnSeparatorLine: PART_ColSeparator.IsChecked == true);
    }

    private void OnApplyClicked(object sender, RoutedEventArgs e)
    {
        _current = BuildSettings();
        SettingsApplied?.Invoke(this, _current);
    }

    private void OnOkClicked(object sender, RoutedEventArgs e)
    {
        _current = BuildSettings();
        SettingsApplied?.Invoke(this, _current);
        DialogResult = true;
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnPageFormatChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        if (PART_PageFormat.SelectedItem is not ComboBoxItem item) return;
        if (!Enum.TryParse<DocumentPageSize>(item.Tag?.ToString(), out var ps)) return;
        var tmp = _current.WithAll(pageSize: ps);
        PART_PageWidth.Text  = ((int)tmp.EffectivePageWidth).ToString();
        PART_PageHeight.Text = ((int)tmp.EffectivePageHeight).ToString();
    }

    private void OnHeaderEnabledChanged(object sender, RoutedEventArgs e)
        => PART_HeaderOptions.IsEnabled = PART_HeaderEnabled.IsChecked == true;

    private void OnFooterEnabledChanged(object sender, RoutedEventArgs e)
        => PART_FooterOptions.IsEnabled = PART_FooterEnabled.IsChecked == true;

    private void OnPickBgColorClicked(object sender, RoutedEventArgs e)
    {
        var color = ShowColorPicker(_bgColor ?? "#FFFFFF");
        if (color is null) return;
        _bgColor = color;
        UpdateBgPreview();
    }

    private void OnClearBgColorClicked(object sender, RoutedEventArgs e)
    {
        _bgColor = null;
        UpdateBgPreview();
    }

    private void OnPickBorderColorClicked(object sender, RoutedEventArgs e)
    {
        var color = ShowColorPicker(_borderColor);
        if (color is null) return;
        _borderColor = color;
        UpdateBorderPreview();
    }

    private void UpdateBgPreview()
    {
        PART_BgColorPreview.Background = _bgColor is not null && TryParseColor(_bgColor, out var c)
            ? new SolidColorBrush(c)
            : Brushes.White;
    }

    private void UpdateBorderPreview()
    {
        PART_BorderColorPreview.Background = TryParseColor(_borderColor, out var c)
            ? new SolidColorBrush(c)
            : Brushes.Black;
    }

    private static string? ShowColorPicker(string currentHex)
    {
        var input = new ColorInputDialog(currentHex) { Owner = Application.Current.MainWindow };
        return input.ShowDialog() == true ? input.HexColor : null;
    }

    private static bool TryParseColor(string hex, out Color color)
    {
        try { color = (Color)ColorConverter.ConvertFromString(hex); return true; }
        catch { color = Colors.Black; return false; }
    }

    private static void SelectComboByTag(ComboBox cb, string tag)
    {
        foreach (ComboBoxItem item in cb.Items)
            if (item.Tag?.ToString() == tag) { cb.SelectedItem = item; return; }
    }

    private static void SelectComboByContent(ComboBox cb, string content)
    {
        foreach (ComboBoxItem item in cb.Items)
            if (item.Content?.ToString() == content) { cb.SelectedItem = item; return; }
    }

    private static double ParsePx(string text, double fallback)
        => double.TryParse(text, System.Globalization.NumberStyles.Any,
                           System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;
}
