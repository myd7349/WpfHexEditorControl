//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.TBLEditorModule.Models;

namespace WpfHexaEditor.TBLEditorModule.ViewModels
{
    /// <summary>
    /// ViewModel for TBL Conflict Dialog
    /// </summary>
    public class TblConflictViewModel : INotifyPropertyChanged
    {
        private TblConflict _selectedConflict;

        public TblConflictViewModel(System.Collections.Generic.List<TblConflict> conflicts)
        {
            Conflicts = new ObservableCollection<TblConflict>(conflicts);
            UpdateStatistics();
        }

        public ObservableCollection<TblConflict> Conflicts { get; }

        public TblConflict SelectedConflict
        {
            get => _selectedConflict;
            set
            {
                if (_selectedConflict != value)
                {
                    _selectedConflict = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int InfoCount { get; private set; }

        private void UpdateStatistics()
        {
            ErrorCount = Conflicts.Count(c => c.Severity == ConflictSeverity.Error);
            WarningCount = Conflicts.Count(c => c.Severity == ConflictSeverity.Warning);
            InfoCount = Conflicts.Count(c => c.Severity == ConflictSeverity.Info);

            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(InfoCount));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
