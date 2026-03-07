//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfHexEditor.PluginHost.UI;

/// <summary>
/// Plugin Manager document tab â€” lists all plugins with live metrics and lifecycle actions.
/// </summary>
public sealed partial class PluginManagerControl : UserControl
{
    public PluginManagerControl(PluginManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // Theme-aware foreground (rule 7b)
        SetResourceReference(ForegroundProperty, "DockMenuForegroundBrush");

        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IDisposable d) d.Dispose();
        Unloaded -= OnUnloaded;
    }
}
