// ==========================================================
// Project: WpfHexEditor.Plugins.DataInspector
// File: DataInspectorOptionsPage.xaml.cs
// Author: Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Created: 2026-03-06
// Description:
//     Code-behind for DataInspectorOptionsPage.
//     Loads from and saves to DataInspectorOptions.Instance.
// ==========================================================

using System.Windows.Controls;

namespace WpfHexEditor.Plugins.DataInspector.Options;

public partial class DataInspectorOptionsPage : UserControl
{
    public DataInspectorOptionsPage()
    {
        // Wrap InitializeComponent() because Application.LoadComponent() can throw
        // NullReferenceException when the plugin assembly is loaded in a custom
        // AssemblyLoadContext and WPF's pack URI system can't resolve the resource stream.
        // If BAML loading fails, all x:Name fields remain null; Load() handles that
        // via its null guard and returns early without accessing any field.
        try { InitializeComponent(); }
        catch { /* BAML load failed in ALC — UI fields will be null; Load() guard handles it */ }
    }

    /// <summary>Populates the page controls from current options.</summary>
    public void Load()
    {
        // Guard: named fields may be null if InitializeComponent() failed to resolve
        // the BAML resource (e.g., custom AssemblyLoadContext in the plugin host).
        if (AutoRefreshCheckBox is null) return;

        var opts = DataInspectorOptions.Instance;
        AutoRefreshCheckBox.IsChecked   = opts.AutoRefresh;
        LittleEndianCheckBox.IsChecked  = opts.DefaultLittleEndian;
        ShowByteChartCheckBox.IsChecked = opts.ShowByteChart;
        MaxStringBytesSlider.Value      = opts.MaxStringBytes;
        SyncChartPositionCombo(opts.ChartPosition);
    }

    /// <summary>Persists page control values back to DataInspectorOptions and saves.</summary>
    public void Save()
    {
        var opts = DataInspectorOptions.Instance;
        opts.AutoRefresh         = AutoRefreshCheckBox.IsChecked == true;
        opts.DefaultLittleEndian = LittleEndianCheckBox.IsChecked == true;
        opts.ShowByteChart       = ShowByteChartCheckBox.IsChecked == true;
        opts.MaxStringBytes      = (int)MaxStringBytesSlider.Value;
        opts.ChartPosition       = SelectedChartPosition();
        opts.Save();
    }

    private void SyncChartPositionCombo(ChartPosition pos)
    {
        foreach (System.Windows.Controls.ComboBoxItem item in ChartPositionComboBox.Items)
        {
            if (item.Tag?.ToString() == pos.ToString())
            {
                ChartPositionComboBox.SelectedItem = item;
                return;
            }
        }
    }

    private ChartPosition SelectedChartPosition()
    {
        if (ChartPositionComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item &&
            Enum.TryParse<ChartPosition>(item.Tag?.ToString(), out var pos))
            return pos;
        return ChartPosition.Left;
    }
}
