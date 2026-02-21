//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Windows;

namespace WpfHexaEditor.TBLEditorModule.Views
{
    /// <summary>
    /// Interaction logic for TblConflictDialog.xaml
    /// </summary>
    public partial class TblConflictDialog : Window
    {
        public TblConflictDialog()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
