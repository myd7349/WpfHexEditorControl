//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfHexEditor.App.BinaryAnalysis.ViewModels;
using WpfHexEditor.SDK.Contracts;

namespace WpfHexEditor.App.BinaryAnalysis.Panels;

/// <summary>#111 Cryptographic Hash Inspector — code-behind-only panel.</summary>
public sealed class HashInspectorPanel : UserControl
{
    private readonly HashInspectorViewModel _vm = new();

    public HashInspectorPanel()
    {
        var hashFileBtn = new Button { Content = "Hash File",      Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(10, 2, 10, 2) };
        var hashSelBtn  = new Button { Content = "Hash Selection", Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(10, 2, 10, 2) };
        var cancelBtn   = new Button { Content = "Cancel",         Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(10, 2, 10, 2) };
        var statusTxt   = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        statusTxt.SetBinding(TextBlock.TextProperty, new Binding(nameof(_vm.StatusText)) { Source = _vm });

        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 4, 4, 4) };
        toolbar.Children.Add(hashFileBtn);
        toolbar.Children.Add(hashSelBtn);
        toolbar.Children.Add(cancelBtn);
        toolbar.Children.Add(statusTxt);

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            CanUserAddRows      = false,
            CanUserDeleteRows   = false,
            IsReadOnly          = true,
            HeadersVisibility   = DataGridHeadersVisibility.Column,
        };
        grid.Columns.Add(new DataGridTextColumn { Header = "Algorithm", Binding = new Binding("Algorithm"), Width = 80 });
        grid.Columns.Add(new DataGridTextColumn { Header = "Digest",    Binding = new Binding("HexDigest"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        grid.Columns.Add(new DataGridTemplateColumn
        {
            Header        = "Copy",
            Width         = 50,
            CellTemplate  = BuildCopyTemplate(),
        });
        grid.ItemsSource = _vm.Results;

        var root = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);
        root.Children.Add(grid);
        Content = root;

        hashFileBtn.Click += async (_, _) => await _vm.HashFileAsync();
        hashSelBtn.Click  += async (_, _) => await _vm.HashSelectionAsync();
        cancelBtn.Click   += (_, _) => _vm.Cancel();
    }

    public void SetContext(IIDEHostContext context) => _vm.SetContext(context);
    public void OnFileOpened() { _vm.Results.Clear(); _vm.StatusText_Reset(); }

    private static DataTemplate BuildCopyTemplate()
    {
        var factory = new FrameworkElementFactory(typeof(Button));
        factory.SetValue(Button.ContentProperty, "Copy");
        factory.SetValue(Button.PaddingProperty, new Thickness(4, 1, 4, 1));
        factory.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, _) =>
        {
            if (s is Button b && b.DataContext is Services.HashResult r)
                Clipboard.SetText(r.HexDigest);
        }));
        return new DataTemplate { VisualTree = factory };
    }
}
