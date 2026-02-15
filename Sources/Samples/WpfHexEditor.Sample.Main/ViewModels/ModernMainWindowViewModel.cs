//////////////////////////////////////////////
// Apache 2.0  2026
// HexEditor V2 - Modern Main Window ViewModel
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using WpfHexaEditor;

namespace WpfHexEditor.Sample.Main.ViewModels
{
    /// <summary>
    /// ViewModel for Modern Main Window
    /// </summary>
    public class ModernMainWindowViewModel : INotifyPropertyChanged
    {
        #region Fields

        private bool _isSettingsPanelVisible = false;
        private bool _isSearchPanelVisible = true;
        private bool _isFileLoaded = false;
        private string _currentFilePath;
        private string _statusMessage = "Ready";
        private string _searchQuery = "";
        private HexEditor _hexEditor;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<string> FileOpenRequested;
        public event EventHandler FileSaveRequested;

        #endregion

        #region Properties

        public SettingsPanelViewModel SettingsViewModel { get; }
        public SearchCommandCenterViewModel SearchViewModel { get; }

        public bool IsSettingsPanelVisible
        {
            get => _isSettingsPanelVisible;
            set
            {
                _isSettingsPanelVisible = value;
                OnPropertyChanged(nameof(IsSettingsPanelVisible));
            }
        }

        public bool IsSearchPanelVisible
        {
            get => _isSearchPanelVisible;
            set
            {
                _isSearchPanelVisible = value;
                OnPropertyChanged(nameof(IsSearchPanelVisible));
            }
        }

        public bool IsFileLoaded
        {
            get => _isFileLoaded;
            set
            {
                _isFileLoaded = value;
                OnPropertyChanged(nameof(IsFileLoaded));
            }
        }

        public string CurrentFilePath
        {
            get => _currentFilePath;
            set
            {
                _currentFilePath = value;
                OnPropertyChanged(nameof(CurrentFilePath));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public string WindowTitle
        {
            get
            {
                var title = "WPF HexEditor V2 - Modern Sample 2026";
                if (!string.IsNullOrEmpty(CurrentFilePath))
                {
                    title += $" - {System.IO.Path.GetFileName(CurrentFilePath)}";
                }
                return title;
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged(nameof(SearchQuery));
            }
        }

        #endregion

        #region Commands

        public ICommand OpenFileCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand CloseFileCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ToggleSettingsPanelCommand { get; }
        public ICommand ToggleSearchPanelCommand { get; }
        public ICommand ShowAboutCommand { get; }

        #endregion

        #region Constructor

        public ModernMainWindowViewModel()
        {
            SettingsViewModel = new SettingsPanelViewModel();
            SearchViewModel = new SearchCommandCenterViewModel();

            // Wire up settings events
            SettingsViewModel.ThemeChanged += OnThemeChanged;
            SettingsViewModel.LanguageChanged += OnLanguageChanged;

            // Commands
            OpenFileCommand = new RelayCommand(OpenFile);
            SaveFileCommand = new RelayCommand(SaveFile, () => IsFileLoaded);
            CloseFileCommand = new RelayCommand(CloseFile, () => IsFileLoaded);
            UndoCommand = new RelayCommand(Undo, () => IsFileLoaded);
            RedoCommand = new RelayCommand(Redo, () => IsFileLoaded);
            SearchCommand = new RelayCommand(Search, () => IsFileLoaded && !string.IsNullOrWhiteSpace(SearchQuery));
            ToggleSettingsPanelCommand = new RelayCommand(ToggleSettingsPanel);
            ToggleSearchPanelCommand = new RelayCommand(ToggleSearchPanel);
            ShowAboutCommand = new RelayCommand(ShowAbout);
        }

        #endregion

        #region Methods

        public void SetHexEditor(HexEditor hexEditor)
        {
            _hexEditor = hexEditor;
            if (_hexEditor != null)
            {
                _hexEditor.FileOpened += OnFileLoaded;
                _hexEditor.FileClosed += OnFileClosed;
            }
        }

        private void OpenFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open File",
                Filter = "All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                FileOpenRequested?.Invoke(this, dialog.FileName);
                CurrentFilePath = dialog.FileName;
                StatusMessage = $"Loaded: {System.IO.Path.GetFileName(dialog.FileName)}";
            }
        }

        private void SaveFile()
        {
            FileSaveRequested?.Invoke(this, EventArgs.Empty);
            StatusMessage = "File saved";
        }

        private void CloseFile()
        {
            _hexEditor?.Close();
            CurrentFilePath = null;
            IsFileLoaded = false;
            StatusMessage = "File closed";
        }

        private void Undo()
        {
            try
            {
                _hexEditor?.Undo();
                StatusMessage = "Undo completed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Undo failed: {ex.Message}";
            }
        }

        private void Redo()
        {
            try
            {
                _hexEditor?.Redo();
                StatusMessage = "Redo completed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Redo failed: {ex.Message}";
            }
        }

        private void Search()
        {
            if (_hexEditor == null || string.IsNullOrWhiteSpace(SearchQuery))
                return;

            try
            {
                // Use the SearchViewModel functionality if available
                if (SearchViewModel != null && SearchViewModel.SearchCommand.CanExecute(null))
                {
                    SearchViewModel.SearchPattern = SearchQuery;
                    SearchViewModel.SearchCommand.Execute(null);
                    StatusMessage = $"Searching for: {SearchQuery}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Search failed: {ex.Message}";
            }
        }

        private void ToggleSettingsPanel()
        {
            IsSettingsPanelVisible = !IsSettingsPanelVisible;
        }

        private void ToggleSearchPanel()
        {
            IsSearchPanelVisible = !IsSearchPanelVisible;
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "WPF HexEditor V2 - Modern Sample 2026\n\n" +
                "Ultra-performant hex editor with modern UI\n" +
                "• 99% faster search with V2 engine\n" +
                "• 4 modern themes\n" +
                "• Multi-language support\n" +
                "• Search-first architecture\n\n" +
                "Apache 2.0 License © 2026\n" +
                "Author: Derek Tremblay\n" +
                "Contributors: Claude Sonnet 4.5",
                "About",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void OnThemeChanged(object sender, string themeName)
        {
            StatusMessage = $"Theme changed to: {themeName}";
        }

        private void OnLanguageChanged(object sender, string languageCode)
        {
            StatusMessage = $"Language changed to: {languageCode}";
        }

        private void OnFileLoaded(object sender, EventArgs e)
        {
            IsFileLoaded = true;

            // Connect search to hex editor
            if (_hexEditor != null)
            {
                SearchViewModel.ByteProvider = _hexEditor.GetByteProvider();
                SearchViewModel.OnMatchFound += (s, match) =>
                {
                    _hexEditor.FindSelect(match.Position, match.Length);
                };
            }
        }

        private void OnFileClosed(object sender, EventArgs e)
        {
            IsFileLoaded = false;
            SearchViewModel.ByteProvider = null;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
