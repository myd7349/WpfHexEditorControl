//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using WpfHexaEditor.Commands;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.TBLEditorModule.Models;
using WpfHexaEditor.TBLEditorModule.Services;
using WpfHexaEditor.TBLEditorModule.ViewModels;

namespace WpfHexaEditor.TBLEditorModule.ViewModels
{
    /// <summary>
    /// Main ViewModel for TBL Editor Dialog
    /// </summary>
    public class TblEditorViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly TblStream _tblStream;
        private readonly TblValidationService _validationService;
        private readonly TblConflictAnalyzer _conflictAnalyzer;
        private readonly TblSearchService _searchService;
        private readonly TblTemplateService _templateService;

        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDirty;
        private bool _isLoading;
        private TblEntryViewModel _selectedEntry;
        private string _statusMessage;
        private string _searchText;
        private DteType? _filterByType;
        private bool _showConflictsOnly;
        private TblStatistics _statistics;

        // Undo/Redo stacks
        private readonly Stack<ITblCommand> _undoStack = new Stack<ITblCommand>();
        private readonly Stack<ITblCommand> _redoStack = new Stack<ITblCommand>();
        private const int MaxUndoStackSize = 100;

        #endregion

        #region Properties

        public ObservableCollection<TblEntryViewModel> Entries { get; } = new ObservableCollection<TblEntryViewModel>();
        public ICollectionView FilteredEntries { get; private set; }

        public TblStream SourceTblStream => _tblStream;

        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public TblEntryViewModel SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (_selectedEntry != value)
                {
                    _selectedEntry = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public DteType? FilterByType
        {
            get => _filterByType;
            set
            {
                if (_filterByType != value)
                {
                    _filterByType = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public bool ShowConflictsOnly
        {
            get => _showConflictsOnly;
            set
            {
                if (_showConflictsOnly != value)
                {
                    _showConflictsOnly = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public TblStatistics Statistics
        {
            get => _statistics;
            private set
            {
                if (_statistics != value)
                {
                    _statistics = value;
                    OnPropertyChanged();
                }
            }
        }

        public int UndoStackCount => _undoStack.Count;
        public int RedoStackCount => _redoStack.Count;

        #endregion

        #region Commands

        public ICommand AddEntryCommand { get; }
        public ICommand EditEntryCommand { get; }
        public ICommand DeleteEntryCommand { get; }
        public ICommand DuplicateEntryCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand DetectConflictsCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand RefreshStatisticsCommand { get; }
        public ICommand LoadTemplateCommand { get; }
        public ICommand SaveTemplateCommand { get; }
        public ICommand AddEndBlockCommand { get; }
        public ICommand AddEndLineCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }

        #endregion

        #region Constructor

        public TblEditorViewModel(TblStream tblStream)
        {
            _tblStream = tblStream ?? throw new ArgumentNullException(nameof(tblStream));

            // Initialize services
            _validationService = new TblValidationService();
            _conflictAnalyzer = new TblConflictAnalyzer();
            _searchService = new TblSearchService();
            _templateService = new TblTemplateService();

            // Setup collection view for filtering and grouping
            FilteredEntries = CollectionViewSource.GetDefaultView(Entries);
            FilteredEntries.Filter = FilterEntries;

            // Add grouping by Type
            FilteredEntries.GroupDescriptions.Add(new PropertyGroupDescription("TypeDisplay"));

            // Initialize commands
            AddEntryCommand = new RelayCommand(() => ExecuteAddEntry(null), () => CanExecuteAddEntry(null));
            EditEntryCommand = new RelayCommand(() => ExecuteEditEntry(null), () => CanExecuteEditEntry(null));
            DeleteEntryCommand = new RelayCommand(() => ExecuteDeleteEntry(null), () => CanExecuteDeleteEntry(null));
            DuplicateEntryCommand = new RelayCommand(() => ExecuteDuplicateEntry(null), () => CanExecuteDuplicateEntry(null));
            SaveCommand = new RelayCommand(() => ExecuteSave(null), () => CanExecuteSave(null));
            SaveAsCommand = new RelayCommand(() => ExecuteSaveAs(null), () => CanExecuteSaveAs(null));
            DetectConflictsCommand = new RelayCommand(() => ExecuteDetectConflicts(null), () => CanExecuteDetectConflicts(null));
            ClearSearchCommand = new RelayCommand(() => ExecuteClearSearch(null), () => CanExecuteClearSearch(null));
            UndoCommand = new RelayCommand(() => ExecuteUndo(null), () => CanExecuteUndo(null));
            RedoCommand = new RelayCommand(() => ExecuteRedo(null), () => CanExecuteRedo(null));
            RefreshStatisticsCommand = new RelayCommand(() => ExecuteRefreshStatistics(null));
            LoadTemplateCommand = new RelayCommand(() => ExecuteLoadTemplate(null), () => CanExecuteLoadTemplate(null));
            SaveTemplateCommand = new RelayCommand(() => ExecuteSaveTemplate(null), () => CanExecuteSaveTemplate(null));
            AddEndBlockCommand = new RelayCommand(() => ExecuteAddEndBlock(null), () => CanExecuteAddEndBlock(null));
            AddEndLineCommand = new RelayCommand(() => ExecuteAddEndLine(null), () => CanExecuteAddEndLine(null));
            ImportCommand = new RelayCommand(() => ExecuteImport(null), () => CanExecuteImport(null));
            ExportCommand = new RelayCommand(() => ExecuteExport(null), () => CanExecuteExport(null));

            // Load initial data
            LoadFromTblStream();
        }

        #endregion

        #region Loading

        private void LoadFromTblStream()
        {
            IsLoading = true;
            StatusMessage = "Loading TBL entries...";

            try
            {
                Entries.Clear();

                foreach (var dte in _tblStream.GetAllEntries())
                {
                    var entryVm = new TblEntryViewModel
                    {
                        Entry = dte.Entry,
                        Value = dte.Value
                    };

                    entryVm.PropertyChanged += Entry_PropertyChanged;
                    Entries.Add(entryVm);
                }

                // Build search index
                _searchService.BuildIndexAsync(Entries).ConfigureAwait(false);

                // Update statistics
                RefreshStatistics();

                // Detect conflicts in background
                Task.Run(() => DetectConflictsAsync());

                IsDirty = false;
                StatusMessage = $"Loaded {Entries.Count} entries";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading TBL: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Entry Management

        private void ExecuteAddEntry(object parameter)
        {
            var newEntry = new TblEntryViewModel
            {
                Entry = "00",
                Value = " "
            };

            ExecuteCommand(new AddEntryCommand(this, newEntry));
        }

        private bool CanExecuteAddEntry(object parameter) => !IsLoading;

        private void ExecuteEditEntry(object parameter)
        {
            // Editing happens inline in DataGrid
            if (SelectedEntry != null)
            {
                SelectedEntry.IsDirty = true;
                IsDirty = true;
            }
        }

        private bool CanExecuteEditEntry(object parameter) => SelectedEntry != null && !IsLoading;

        private void ExecuteDeleteEntry(object parameter)
        {
            if (SelectedEntry != null)
            {
                ExecuteCommand(new DeleteEntryCommand(this, SelectedEntry));
            }
        }

        private bool CanExecuteDeleteEntry(object parameter) => SelectedEntry != null && !IsLoading;

        private void ExecuteDuplicateEntry(object parameter)
        {
            if (SelectedEntry != null)
            {
                var duplicate = SelectedEntry.Clone();
                duplicate.Entry = "00"; // User will need to change this
                ExecuteCommand(new AddEntryCommand(this, duplicate));
            }
        }

        private bool CanExecuteDuplicateEntry(object parameter) => SelectedEntry != null && !IsLoading;

        #endregion

        #region Save Operations

        private void ExecuteSave(object parameter)
        {
            SyncToTblStream();

            try
            {
                _tblStream.Save();
                IsDirty = false;
                StatusMessage = "TBL saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving TBL: {ex.Message}";
            }
        }

        private bool CanExecuteSave(object parameter) => IsDirty && !IsLoading;

        private void ExecuteSaveAs(object parameter)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "TBL Files (*.tbl)|*.tbl|All Files (*.*)|*.*",
                DefaultExt = ".tbl",
                Title = "Save TBL As"
            };

            if (saveDialog.ShowDialog() == true)
            {
                SyncToTblStream();

                try
                {
                    // Save manually since TblStream.Save() doesn't take filename parameter
                    var originalFileName = _tblStream.FileName;
                    var sb = new System.Text.StringBuilder();

                    foreach (var dte in _tblStream.GetAllEntries())
                    {
                        // Escape special characters
                        string escapedValue = dte.Value
                            .Replace("\\", "\\\\")
                            .Replace("\n", "\\n")
                            .Replace("\r", "\\r")
                            .Replace("\t", "\\t");

                        // Handle special types
                        if (dte.Type == DteType.EndBlock)
                            sb.AppendLine($"/{dte.Entry}={escapedValue}");
                        else if (dte.Type == DteType.EndLine)
                            sb.AppendLine($"*{dte.Entry}={escapedValue}");
                        else
                            sb.AppendLine($"{dte.Entry}={escapedValue}");
                    }

                    System.IO.File.WriteAllText(saveDialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    IsDirty = false;
                    StatusMessage = $"TBL saved to {saveDialog.FileName}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error saving TBL: {ex.Message}";
                }
            }
        }

        private bool CanExecuteSaveAs(object parameter) => !IsLoading;

        public void SyncToTblStream()
        {
            _tblStream.Clear();

            foreach (var entryVm in Entries.Where(e => e.IsValid))
            {
                try
                {
                    var dte = entryVm.ToDto();
                    _tblStream.Add(dte);
                }
                catch (Exception)
                {
                    // Skip invalid entries
                }
            }
        }

        private void ExecuteImport(object parameter)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "All Supported Formats|*.tbl;*.tblx;*.csv;*.json|" +
                         "TBL Files (*.tbl)|*.tbl|" +
                         "Extended TBL Files (*.tblx)|*.tblx|" +
                         "CSV Files (*.csv)|*.csv|" +
                         "JSON Files (*.json)|*.json|" +
                         "All Files (*.*)|*.*",
                Title = "Import TBL from File"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    StatusMessage = "Importing...";

                    var result = _tblStream.LoadFromFile(openDialog.FileName);

                    if (result.Success)
                    {
                        // Reload UI
                        LoadFromTblStream();

                        StatusMessage = $"Imported {result.ImportedCount} entries";

                        if (result.Warnings.Count > 0)
                        {
                            StatusMessage += $" ({result.SkippedCount} skipped)";
                        }

                        IsDirty = true;
                    }
                    else
                    {
                        var errors = string.Join("\n", result.Errors);
                        StatusMessage = $"Import failed: {errors}";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Import error: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private bool CanExecuteImport(object parameter) => !IsLoading;

        private void ExecuteExport(object parameter)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "TBL Files (*.tbl)|*.tbl|" +
                         "Extended TBL Files (*.tblx)|*.tblx|" +
                         "CSV Files (*.csv)|*.csv|" +
                         "JSON Files (*.json)|*.json|" +
                         "All Files (*.*)|*.*",
                DefaultExt = ".tbl",
                Title = "Export TBL to File"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    SyncToTblStream();

                    var extension = System.IO.Path.GetExtension(saveDialog.FileName)?.ToLowerInvariant();

                    // Create metadata for .tblx export
                    TblxMetadata metadata = null;
                    if (extension == ".tblx")
                    {
                        metadata = new TblxMetadata
                        {
                            Name = System.IO.Path.GetFileNameWithoutExtension(saveDialog.FileName),
                            CreatedDate = DateTime.Now,
                            Author = Environment.UserName,
                            Description = "Exported from WPF Hex Editor"
                        };
                    }

                    _tblStream.SaveToFile(saveDialog.FileName, tblxMetadata: metadata);

                    StatusMessage = $"Exported to {saveDialog.FileName}";
                    IsDirty = false;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Export error: {ex.Message}";
                }
            }
        }

        private bool CanExecuteExport(object parameter) => !IsLoading && Entries.Count > 0;

        #endregion

        #region Conflict Detection

        private async void ExecuteDetectConflicts(object parameter)
        {
            await DetectConflictsAsync();
        }

        private bool CanExecuteDetectConflicts(object parameter) => !IsLoading;

        private async Task DetectConflictsAsync()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var conflicts = await _conflictAnalyzer.AnalyzeConflictsAsync(Entries, _cancellationTokenSource.Token);

                // Update conflict status on entries
                foreach (var entry in Entries)
                {
                    entry.Conflicts.Clear();
                    entry.HasConflict = false;
                }

                foreach (var conflict in conflicts)
                {
                    foreach (var conflictEntry in conflict.ConflictingEntries)
                    {
                        var entryVm = Entries.FirstOrDefault(e => e.Entry.Equals(conflictEntry.Entry, StringComparison.OrdinalIgnoreCase));
                        if (entryVm != null)
                        {
                            entryVm.Conflicts.Add(conflict);
                            entryVm.HasConflict = true;
                        }
                    }
                }

                StatusMessage = conflicts.Count == 0
                    ? "No conflicts detected"
                    : $"{conflicts.Count} conflicts detected";
            }
            catch (OperationCanceledException)
            {
                // Cancelled
            }
        }

        #endregion

        #region Filtering

        private bool FilterEntries(object obj)
        {
            if (obj is not TblEntryViewModel entry)
                return false;

            // Filter by conflict
            if (ShowConflictsOnly && !entry.HasConflict)
                return false;

            // Filter by type
            if (FilterByType.HasValue && entry.Type != FilterByType.Value)
                return false;

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                return entry.Entry.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                       entry.Value.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        private void ApplyFilters()
        {
            FilteredEntries?.Refresh();
        }

        private void ExecuteClearSearch(object parameter)
        {
            SearchText = string.Empty;
            FilterByType = null;
            ShowConflictsOnly = false;
        }

        private bool CanExecuteClearSearch(object parameter) =>
            !string.IsNullOrWhiteSpace(SearchText) || FilterByType.HasValue || ShowConflictsOnly;

        #endregion

        #region Statistics

        private void RefreshStatistics()
        {
            Statistics = _tblStream.GetStatistics();

            // Add conflict count from entries
            if (Statistics != null)
            {
                Statistics.ConflictCount = Entries.Count(e => e.HasConflict);
            }

            OnPropertyChanged(nameof(Statistics));
        }

        private void ExecuteRefreshStatistics(object parameter)
        {
            RefreshStatistics();
        }

        #endregion

        #region Templates

        private void ExecuteLoadTemplate(object parameter)
        {
            try
            {
                var dialog = new TBLEditorModule.Views.TblTemplateDialog
                {
                    Owner = System.Windows.Application.Current.MainWindow,
                    DataContext = new TBLEditorModule.ViewModels.TblTemplateViewModel()
                };

                if (dialog.ShowDialog() == true)
                {
                    var viewModel = dialog.DataContext as TBLEditorModule.ViewModels.TblTemplateViewModel;
                    if (viewModel?.SelectedTemplate != null)
                    {
                        // Load template
                        var templateTbl = viewModel.SelectedTemplate.Load();
                        if (templateTbl != null)
                        {
                            if (dialog.LoadTemplate)
                            {
                                // Replace current TBL
                                Entries.Clear();
                                _tblStream.Clear();

                                foreach (var entry in templateTbl.GetAllEntries())
                                {
                                    var entryVm = new TblEntryViewModel(entry);
                                    Entries.Add(entryVm);
                                }

                                SyncToTblStream();
                                StatusMessage = $"Template '{viewModel.SelectedTemplate.Name}' loaded ({Entries.Count} entries)";
                            }
                            else if (dialog.MergeTemplate)
                            {
                                // Merge with current TBL
                                int addedCount = 0;
                                foreach (var entry in templateTbl.GetAllEntries())
                                {
                                    if (!_tblStream.ContainsEntry(entry.Entry))
                                    {
                                        var entryVm = new TblEntryViewModel(entry);
                                        Entries.Add(entryVm);
                                        addedCount++;
                                    }
                                }

                                SyncToTblStream();
                                StatusMessage = $"Template merged: {addedCount} new entries added";
                            }

                            RefreshStatistics();
                            IsDirty = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading template: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Failed to load template: {ex.Message}",
                    "Template Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private bool CanExecuteLoadTemplate(object parameter)
        {
            return !IsLoading;
        }

        private void ExecuteSaveTemplate(object parameter)
        {
            try
            {
                // Save current TBL to template
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "TBL Template (*.tbltemplate)|*.tbltemplate",
                    DefaultExt = ".tbltemplate",
                    Title = "Save as Template"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Sync current entries to TblStream first
                    SyncToTblStream();

                    // Create template metadata
                    var templateName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                    var sb = new System.Text.StringBuilder();

                    sb.AppendLine("[TEMPLATE]");
                    sb.AppendLine($"Id={templateName.ToLowerInvariant().Replace(" ", "-")}");
                    sb.AppendLine($"Name={templateName}");
                    sb.AppendLine($"Description=Custom TBL template");
                    sb.AppendLine($"Author={Environment.UserName}");
                    sb.AppendLine($"Category=Custom");
                    sb.AppendLine($"CreatedDate={DateTime.Now:yyyy-MM-dd}");
                    sb.AppendLine();
                    sb.AppendLine("[TBL]");

                    // Add all entries
                    foreach (var entry in Entries.OrderBy(e => e.ByteLength).ThenBy(e => e.Entry))
                    {
                        sb.AppendLine($"{entry.Entry}={entry.Value}");
                    }

                    System.IO.File.WriteAllText(dialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);

                    StatusMessage = $"Template saved: {templateName}";
                    System.Windows.MessageBox.Show(
                        $"Template saved successfully:\n{dialog.FileName}",
                        "Template Saved",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving template: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Failed to save template: {ex.Message}",
                    "Template Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private bool CanExecuteSaveTemplate(object parameter)
        {
            return !IsLoading && Entries.Count > 0;
        }

        private void ExecuteAddEndBlock(object parameter)
        {
            // Prompt user for hex value using simple input dialog
            var result = Views.InputDialog.Show(
                "Enter hex value for End Block marker (e.g., 00):",
                "Add End Block Marker",
                "00",
                System.Windows.Application.Current.MainWindow);

            if (string.IsNullOrWhiteSpace(result))
                return;

            var hexValue = result.Trim().ToUpperInvariant();

            // Validate hex
            if (!System.Text.RegularExpressions.Regex.IsMatch(hexValue, "^[0-9A-F]{2}$"))
            {
                System.Windows.MessageBox.Show(
                    "Invalid hex value. Please enter exactly 2 hex digits (00-FF).",
                    "Invalid Input",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var entry = new TblEntryViewModel(new Dte($"/{hexValue}", Properties.Resources.EndTagString, DteType.EndBlock));
            entry.PropertyChanged += Entry_PropertyChanged;

            var command = new AddEntryCommand(this, entry);
            ExecuteCommand(command);

            StatusMessage = $"End Block marker added: /{hexValue}";
        }

        private bool CanExecuteAddEndBlock(object parameter)
        {
            return !IsLoading;
        }

        private void ExecuteAddEndLine(object parameter)
        {
            // Prompt user for hex value using simple input dialog
            var result = Views.InputDialog.Show(
                "Enter hex value for End Line marker (e.g., 0A):",
                "Add End Line Marker",
                "0A",
                System.Windows.Application.Current.MainWindow);

            if (string.IsNullOrWhiteSpace(result))
                return;

            var hexValue = result.Trim().ToUpperInvariant();

            // Validate hex
            if (!System.Text.RegularExpressions.Regex.IsMatch(hexValue, "^[0-9A-F]{2}$"))
            {
                System.Windows.MessageBox.Show(
                    "Invalid hex value. Please enter exactly 2 hex digits (00-FF).",
                    "Invalid Input",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var entry = new TblEntryViewModel(new Dte($"*{hexValue}", Properties.Resources.LineTagString, DteType.EndLine));
            entry.PropertyChanged += Entry_PropertyChanged;

            var command = new AddEntryCommand(this, entry);
            ExecuteCommand(command);

            StatusMessage = $"End Line marker added: *{hexValue}";
        }

        private bool CanExecuteAddEndLine(object parameter)
        {
            return !IsLoading;
        }

        #endregion

        #region Undo/Redo

        public void ExecuteCommand(ITblCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();

            // Limit stack size
            if (_undoStack.Count > MaxUndoStackSize)
            {
                var list = _undoStack.ToList();
                list.RemoveAt(list.Count - 1);
                _undoStack.Clear();
                foreach (var cmd in list.AsEnumerable().Reverse())
                    _undoStack.Push(cmd);
            }

            IsDirty = true;
            OnPropertyChanged(nameof(UndoStackCount));
            OnPropertyChanged(nameof(RedoStackCount));
            CommandManager.InvalidateRequerySuggested();
        }

        private void ExecuteUndo(object parameter)
        {
            if (_undoStack.Count > 0)
            {
                var command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);

                OnPropertyChanged(nameof(UndoStackCount));
                OnPropertyChanged(nameof(RedoStackCount));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool CanExecuteUndo(object parameter) => _undoStack.Count > 0 && !IsLoading;

        private void ExecuteRedo(object parameter)
        {
            if (_redoStack.Count > 0)
            {
                var command = _redoStack.Pop();
                command.Execute();
                _undoStack.Push(command);

                OnPropertyChanged(nameof(UndoStackCount));
                OnPropertyChanged(nameof(RedoStackCount));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool CanExecuteRedo(object parameter) => _redoStack.Count > 0 && !IsLoading;

        #endregion

        #region Event Handlers

        public void Entry_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TblEntryViewModel.Entry) ||
                e.PropertyName == nameof(TblEntryViewModel.Value))
            {
                IsDirty = true;

                // Re-run conflict detection (debounced)
                Task.Run(async () =>
                {
                    await Task.Delay(300);
                    await DetectConflictsAsync();
                });
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        #endregion
    }

    #region Command Pattern

    /// <summary>
    /// Interface for undoable TBL commands
    /// </summary>
    public interface ITblCommand
    {
        void Execute();
        void Undo();
        string Description { get; }
    }

    /// <summary>
    /// Command to add an entry
    /// </summary>
    public class AddEntryCommand : ITblCommand
    {
        private readonly TblEditorViewModel _viewModel;
        private readonly TblEntryViewModel _entry;

        public AddEntryCommand(TblEditorViewModel viewModel, TblEntryViewModel entry)
        {
            _viewModel = viewModel;
            _entry = entry;
        }

        public string Description => $"Add entry {_entry.Entry}";

        public void Execute()
        {
            _entry.PropertyChanged += _viewModel.Entry_PropertyChanged;
            _viewModel.Entries.Add(_entry);
        }

        public void Undo()
        {
            _viewModel.Entries.Remove(_entry);
        }
    }

    /// <summary>
    /// Command to delete an entry
    /// </summary>
    public class DeleteEntryCommand : ITblCommand
    {
        private readonly TblEditorViewModel _viewModel;
        private readonly TblEntryViewModel _entry;
        private int _originalIndex;

        public DeleteEntryCommand(TblEditorViewModel viewModel, TblEntryViewModel entry)
        {
            _viewModel = viewModel;
            _entry = entry;
        }

        public string Description => $"Delete entry {_entry.Entry}";

        public void Execute()
        {
            _originalIndex = _viewModel.Entries.IndexOf(_entry);
            _viewModel.Entries.Remove(_entry);
        }

        public void Undo()
        {
            _viewModel.Entries.Insert(_originalIndex, _entry);
        }
    }

    #endregion
}
