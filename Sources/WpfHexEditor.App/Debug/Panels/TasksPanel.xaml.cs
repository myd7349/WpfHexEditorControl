// ==========================================================
// Project: WpfHexEditor.App.Debug
// File: Panels/TasksPanel.xaml.cs
// ==========================================================

using System.Windows.Controls;
using WpfHexEditor.App.Debug.ViewModels;

namespace WpfHexEditor.App.Debug.Panels;

public partial class TasksPanel : UserControl
{
    private TasksPanelViewModel? Vm => DataContext as TasksPanelViewModel;

    public TasksPanel()
    {
        InitializeComponent();
    }

    private void OnRefreshClick(object sender, System.Windows.RoutedEventArgs e)
        => _ = Vm?.RefreshAsync();
}
