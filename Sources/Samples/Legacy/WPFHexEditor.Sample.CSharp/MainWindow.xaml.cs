//////////////////////////////////////////////
// Apache 2.0  - 2016-2020
// Author : Derek Tremblay (derektremblay666@gmail.com)
//
//
// NOT A TRUE PROJECT! IT'S JUST FOR TESTING THE HEXEDITOR... DO NOT WATCH THE CODE LOL ;) 
//////////////////////////////////////////////

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Win32;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.Dialog;
using WpfHexEditor.Sample.Properties;

namespace WPFHexaEditorExample
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            //FORCE CULTURE
            //System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en");

            InitializeComponent();

            // Load saved settings when window is loaded
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        /// <summary>
        /// Load user settings from Properties.Settings
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] Loading settings...");
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] CanInsertAnywhere: {Settings.Default.CanInsertAnywhere}");
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] IsInsertMode: {Settings.Default.IsInsertMode}");
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] HideByteDeleted: {Settings.Default.HideByteDeleted}");

                // View settings
                HexEdit.HeaderVisibility = Settings.Default.HeaderVisible ? Visibility.Visible : Visibility.Collapsed;
                HexEdit.HexDataVisibility = Settings.Default.HexDataVisible ? Visibility.Visible : Visibility.Collapsed;
                HexEdit.StringDataVisibility = Settings.Default.StringDataVisible ? Visibility.Visible : Visibility.Collapsed;
                HexEdit.LineInfoVisibility = Settings.Default.LineInfoVisible ? Visibility.Visible : Visibility.Collapsed;
                HexEdit.StatusBarVisibility = Settings.Default.StatusBarVisible ? Visibility.Visible : Visibility.Collapsed;

                // Option settings
                HexEdit.AllowContextMenu = Settings.Default.AllowContextMenu;
                HexEdit.ShowByteToolTip = Settings.Default.ShowByteToolTip;
                HexEdit.AllowAutoHighLightSelectionByte = Settings.Default.AllowAutoHighLightSelectionByte;
                HexEdit.AllowAutoSelectSameByteAtDoubleClick = Settings.Default.AllowAutoSelectSameByteAtDoubleClick;
                HexEdit.AllowByteCount = Settings.Default.AllowByteCount;
                HexEdit.FileDroppingConfirmation = Settings.Default.FileDroppingConfirmation;
                HexEdit.AllowTextDrop = Settings.Default.AllowTextDrop;
                HexEdit.AllowFileDrop = Settings.Default.AllowFileDrop;
                HexEdit.HideByteDeleted = Settings.Default.HideByteDeleted;
                HexEdit.AllowDeleteByte = Settings.Default.AllowDeleteByte;
                HexEdit.AppendNeedConfirmation = Settings.Default.AppendNeedConfirmation;
                HexEdit.AllowExtend = Settings.Default.AllowExtend;
                HexEdit.AllowDrop = Settings.Default.AllowDrop;
                HexEdit.AllowZoom = Settings.Default.AllowZoom;

                // INSERT MODE settings (NEW for Issue #31)
                HexEdit.CanInsertAnywhere = Settings.Default.CanInsertAnywhere;

                // Load Insert mode from settings (V2 only - commented out for V1)
                // if (Settings.Default.IsInsertMode)
                // {
                //     HexEdit.EditMode = WpfHexaEditor.V2.Models.EditMode.Insert;
                // }

                // Editor settings
                HexEdit.ReadOnlyMode = Settings.Default.ReadOnlyMode;
                SetReadOnlyMenu.IsChecked = Settings.Default.ReadOnlyMode;
                HexEdit.BytePerLine = Settings.Default.BytePerLine;

                System.Diagnostics.Debug.WriteLine($"[SETTINGS] Settings loaded successfully");
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] HexEdit.CanInsertAnywhere: {HexEdit.CanInsertAnywhere}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] ERROR loading settings: {ex.Message}");
                MessageBox.Show($"Failed to load settings: {ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Save user settings to Properties.Settings
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] Saving settings...");
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] HexEdit.CanInsertAnywhere: {HexEdit.CanInsertAnywhere}");
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] HexEdit.HideByteDeleted: {HexEdit.HideByteDeleted}");

                // View settings
                Settings.Default.HeaderVisible = HexEdit.HeaderVisibility == Visibility.Visible;
                Settings.Default.HexDataVisible = HexEdit.HexDataVisibility == Visibility.Visible;
                Settings.Default.StringDataVisible = HexEdit.StringDataVisibility == Visibility.Visible;
                Settings.Default.LineInfoVisible = HexEdit.LineInfoVisibility == Visibility.Visible;
                Settings.Default.StatusBarVisible = HexEdit.StatusBarVisibility == Visibility.Visible;

                // Option settings
                Settings.Default.AllowContextMenu = HexEdit.AllowContextMenu;
                Settings.Default.ShowByteToolTip = HexEdit.ShowByteToolTip;
                Settings.Default.AllowAutoHighLightSelectionByte = HexEdit.AllowAutoHighLightSelectionByte;
                Settings.Default.AllowAutoSelectSameByteAtDoubleClick = HexEdit.AllowAutoSelectSameByteAtDoubleClick;
                Settings.Default.AllowByteCount = HexEdit.AllowByteCount;
                Settings.Default.FileDroppingConfirmation = HexEdit.FileDroppingConfirmation;
                Settings.Default.AllowTextDrop = HexEdit.AllowTextDrop;
                Settings.Default.AllowFileDrop = HexEdit.AllowFileDrop;
                Settings.Default.HideByteDeleted = HexEdit.HideByteDeleted;
                Settings.Default.AllowDeleteByte = HexEdit.AllowDeleteByte;
                Settings.Default.AppendNeedConfirmation = HexEdit.AppendNeedConfirmation;
                Settings.Default.AllowExtend = HexEdit.AllowExtend;
                Settings.Default.AllowDrop = HexEdit.AllowDrop;
                Settings.Default.AllowZoom = HexEdit.AllowZoom;

                // INSERT MODE settings (NEW for Issue #31)
                Settings.Default.CanInsertAnywhere = HexEdit.CanInsertAnywhere;
                // NOTE: VisualCaretMode NOT saved - always starts in Overwrite mode

                // Editor settings
                Settings.Default.ReadOnlyMode = HexEdit.ReadOnlyMode;
                Settings.Default.BytePerLine = HexEdit.BytePerLine;

                // Save to disk
                Settings.Default.Save();

                System.Diagnostics.Debug.WriteLine($"[SETTINGS] Settings saved successfully");
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] Saved CanInsertAnywhere: {Settings.Default.CanInsertAnywhere}");
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] Saved IsInsertMode: {Settings.Default.IsInsertMode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SETTINGS] ERROR saving settings: {ex.Message}");
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenMenu_Click(object sender, RoutedEventArgs e)
        {
            #region Create file dialog
            var fileDialog = new OpenFileDialog
            {
                Multiselect = true,
                CheckFileExists = true
            };

            if (fileDialog.ShowDialog() == null || !File.Exists(fileDialog.FileName)) return;
            #endregion

            #region if file already open do not open again
            foreach (TabItem ti in FileTab.Items)
                if (ti.ToolTip.ToString() == fileDialog.FileName)
                {
                    ti.IsSelected = true;
                    return;
                }
            #endregion

            #region Open multiple file and add tabs
            Application.Current.MainWindow.Cursor = Cursors.Wait;

            foreach (var file in fileDialog.FileNames)
                FileTab.Items.Add(new TabItem
                {
                    Header = Path.GetFileName(file),
                    ToolTip = file
                });

            FileTab.SelectedIndex = FileTab.Items.Count - 1;
            #endregion

            Application.Current.MainWindow.Cursor = null;
        }

        private void SaveMenu_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Cursor = Cursors.Wait;

            HexEdit.SubmitChanges();

            Application.Current.MainWindow.Cursor = null;
        }

        private void CloseFileMenu_Click(object sender, RoutedEventArgs e) => CloseFile();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save user settings before closing
            SaveSettings();
            HexEdit.CloseProvider();
        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e) => Close();

        private void CopyHexaMenu_Click(object sender, RoutedEventArgs e) => HexEdit.CopyToClipboard(CopyPasteMode.HexaString);

        private void CopyStringMenu_Click(object sender, RoutedEventArgs e) => HexEdit.CopyToClipboard();

        private void DeleteSelectionMenu_Click(object sender, RoutedEventArgs e) => HexEdit.DeleteSelection();

        private void GOPosition_Click(object sender, RoutedEventArgs e)
        {
            if (long.TryParse(PositionText.Text, out var position))
                HexEdit.SetPosition(position, 1);
            else
                MessageBox.Show("Enter long value.");

            ViewMenu.IsSubmenuOpen = false;
        }

        private void PositionText_TextChanged(object sender, TextChangedEventArgs e) =>
            GoPositionButton.IsEnabled = long.TryParse(PositionText.Text, out var _);

        private void UndoMenu_Click(object sender, RoutedEventArgs e) => HexEdit.Undo();

        private void RedoMenu_Click(object sender, RoutedEventArgs e) => HexEdit.Redo();

        private void SetBookMarkButton_Click(object sender, RoutedEventArgs e) => HexEdit.SetBookMark();

        private void DeleteBookmark_Click(object sender, RoutedEventArgs e) => HexEdit.ClearScrollMarker(ScrollMarker.Bookmark);

        private void FindAllSelection_Click(object sender, RoutedEventArgs e) => HexEdit.FindAllSelection(true);

        private void SelectAllButton_Click(object sender, RoutedEventArgs e) => HexEdit.SelectAll();

        private void CTableASCIIButton_Click(object sender, RoutedEventArgs e)
        {
            HexEdit.TypeOfCharacterTable = CharacterTableType.Ascii;
            CTableAsciiButton.IsChecked = true;
            CTableTblButton.IsChecked = false;
            CTableTblDefaultAsciiButton.IsChecked = false;
        }

        private void CTableTBLButton_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();

            if (fileDialog.ShowDialog() == null) return;
            if (!File.Exists(fileDialog.FileName)) return;

            Application.Current.MainWindow.Cursor = Cursors.Wait;

            HexEdit.LoadTblFile(fileDialog.FileName);
            HexEdit.TypeOfCharacterTable = CharacterTableType.TblFile;
            CTableAsciiButton.IsChecked = false;
            CTableTblButton.IsChecked = true;
            CTableTblDefaultAsciiButton.IsChecked = false;

            Application.Current.MainWindow.Cursor = null;
        }

        private void CTableTBLDefaultASCIIButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Cursor = Cursors.Wait;

            HexEdit.TypeOfCharacterTable = CharacterTableType.TblFile;
            HexEdit.LoadDefaultTbl();

            Application.Current.MainWindow.Cursor = null;
        }

        private void SaveAsMenu_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new SaveFileDialog();

            if (fileDialog.ShowDialog() is not null)
                HexEdit.SubmitChanges(fileDialog.FileName, true);
        }

        private void CTableTblDefaultEBCDICButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Cursor = Cursors.Wait;

            HexEdit.TypeOfCharacterTable = CharacterTableType.TblFile;
            HexEdit.LoadDefaultTbl(DefaultCharacterTableType.EbcdicWithSpecialChar);

            Application.Current.MainWindow.Cursor = null;
        }

        private void CTableTblDefaultEBCDICNoSPButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Cursor = Cursors.Wait;

            HexEdit.TypeOfCharacterTable = CharacterTableType.TblFile;
            HexEdit.LoadDefaultTbl(DefaultCharacterTableType.EbcdicNoSpecialChar);

            Application.Current.MainWindow.Cursor = null;
        }

        private void FindMenu_Click(object sender, RoutedEventArgs e) =>
            new FindWindow(HexEdit, HexEdit.GetSelectionByteArray())
            {
                Owner = this
            }.Show();

        private void ReplaceMenu_Click(object sender, RoutedEventArgs e) =>
            new FindReplaceWindow(HexEdit, HexEdit.GetSelectionByteArray())
            {
                Owner = this
            }.Show();

        private void ReverseSelection_Click(object sender, RoutedEventArgs e) => HexEdit.ReverseSelection();

        private void FileTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not TabControl tc) return;
            if (tc.SelectedValue is not TabItem ti) return;

            //Set the tag of last selected ta to currentstate
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabItem lastSelectedTabItem)
                lastSelectedTabItem.Tag = HexEdit.CurrentState;

            //Change loaded file and update the current state
            var filename = ti.ToolTip.ToString();
            if (!File.Exists(filename)) return;

            HexEdit.FileName = filename;

            //Setstate 
            if (ti.Tag is XDocument doc)
            {
                HexEdit.CurrentState = doc;
                SetReadOnlyMenu.IsChecked = HexEdit.ReadOnlyMode;
            }
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e) => CloseFile();

        private void CloseFile()
        {
            if (FileTab.SelectedIndex == -1) return;

            HexEdit.CloseProvider();
            FileTab.Items.RemoveAt(FileTab.SelectedIndex);
        }

        private void CloseAllFileMenu_Click(object sender, RoutedEventArgs e)
        {
            FileTab.Items.Clear();
            HexEdit.CloseProvider();
        }

        private void SetReadOnlyMenu_Click(object sender, RoutedEventArgs e) =>
            HexEdit.ReadOnlyMode = SetReadOnlyMenu.IsChecked;
    }
}