//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Windows;
using System.Windows.Controls;
using WpfHexaEditor.TBLEditorModule.ViewModels;

namespace WpfHexaEditor.TBLEditorModule.Views
{
    /// <summary>
    /// Interaction logic for TblTemplateDialog.xaml
    /// </summary>
    public partial class TblTemplateDialog : Window
    {
        public bool LoadTemplate { get; private set; }
        public bool MergeTemplate { get; private set; }

        public TblTemplateDialog()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is TblTemplateViewModel viewModel)
            {
                viewModel.OnCategorySelected(e.NewValue);
            }
        }

        private void LoadTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            LoadTemplate = true;
            MergeTemplate = false;
            DialogResult = true;
            Close();
        }

        private void MergeTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            LoadTemplate = false;
            MergeTemplate = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
