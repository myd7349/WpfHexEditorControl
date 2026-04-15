//////////////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Project: WpfHexEditor.Editor.StructureEditor
// File: Tabs/TestTab.xaml.cs
// Description: Code-behind for the Test Panel tab.
//////////////////////////////////////////////////////

using System.Windows;
using System.Windows.Controls;
using WpfHexEditor.Editor.StructureEditor.ViewModels;

namespace WpfHexEditor.Editor.StructureEditor.Tabs;

public sealed partial class TestTab : UserControl
{
    public TestTab() => InitializeComponent();

    private TestTabViewModel? VM => DataContext as TestTabViewModel;

    private async void OnRunClicked(object sender, RoutedEventArgs e)
    {
        var vm = VM;
        if (vm is null) return;

        // The def is passed from the parent StructureEditor via the Tag property
        if (Tag is not WpfHexEditor.Core.FormatDetection.FormatDefinition def) return;

        SummaryBar.Visibility = Visibility.Visible;
        await vm.RunAsync(def);
    }
}
