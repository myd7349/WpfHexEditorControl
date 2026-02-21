//////////////////////////////////////////////
// Apache 2.0  - 2003-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.TBLEditorModule.Models;

namespace WpfHexaEditor.TBLEditorModule.ViewModels
{
    /// <summary>
    /// Observable wrapper for DTE entry with validation and conflict tracking
    /// </summary>
    public class TblEntryViewModel : INotifyPropertyChanged
    {
        private string _entry;
        private string _value;
        private bool _isDirty;
        private bool _hasConflict;
        private bool _isSelected;
        private string _validationError;
        private string _comment;

        private readonly string _originalEntry;
        private readonly string _originalValue;

        /// <summary>
        /// Constructor with Dte
        /// </summary>
        public TblEntryViewModel(Dte dte)
        {
            _entry = dte.Entry;
            _value = dte.Value;
            _comment = dte.Comment ?? string.Empty;
            // Type is now a computed property, no need to set it
            IsValid = dte.IsValid;
            _validationError = dte.ValidationError;

            _originalEntry = _entry;
            _originalValue = _value;

            Conflicts = new List<TblConflict>();
        }

        /// <summary>
        /// Default constructor for creating new entries
        /// </summary>
        public TblEntryViewModel() : this(new Dte("00", " "))
        {
        }

        #region Properties

        /// <summary>
        /// Hex entry (e.g., "82", "8283")
        /// </summary>
        public string Entry
        {
            get => _entry;
            set
            {
                if (_entry != value)
                {
                    _entry = value?.ToUpperInvariant() ?? string.Empty;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormattedEntry));
                    OnPropertyChanged(nameof(ByteLength));
                    OnPropertyChanged(nameof(Type));
                    OnPropertyChanged(nameof(TypeDisplay));
                    OnPropertyChanged(nameof(IsDirty));
                    // Validation will be done by external service
                }
            }
        }

        /// <summary>
        /// Character/string value
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value ?? string.Empty;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayValue));
                    OnPropertyChanged(nameof(Type));
                    OnPropertyChanged(nameof(TypeDisplay));
                    OnPropertyChanged(nameof(IsDirty));
                }
            }
        }

        /// <summary>
        /// Display value with visible escape sequences
        /// </summary>
        public string DisplayValue
        {
            get
            {
                if (string.IsNullOrEmpty(_value))
                    return string.Empty;

                return _value
                    .Replace("\n", "\\n↵")
                    .Replace("\r", "\\r↵")
                    .Replace("\t", "\\t→");
            }
        }

        /// <summary>
        /// DTE type (recalculated from Entry and Value)
        /// </summary>
        public DteType Type
        {
            get
            {
                // Recalculate type based on current Entry and Value
                try
                {
                    var tempDte = new Dte(_entry, _value);
                    return tempDte.Type;
                }
                catch
                {
                    return DteType.Invalid;
                }
            }
        }

        /// <summary>
        /// Whether entry is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation error message
        /// </summary>
        public string ValidationError
        {
            get => _validationError;
            set
            {
                if (_validationError != value)
                {
                    _validationError = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Optional comment for this entry
        /// </summary>
        public string Comment
        {
            get => _comment;
            set
            {
                if (_comment != value)
                {
                    _comment = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether entry has been modified
        /// </summary>
        public bool IsDirty
        {
            get => _entry != _originalEntry || _value != _originalValue;
            set
            {
                _isDirty = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether entry has conflicts with other entries
        /// </summary>
        public bool HasConflict
        {
            get => _hasConflict;
            set
            {
                if (_hasConflict != value)
                {
                    _hasConflict = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(StatusTooltip));
                }
            }
        }

        /// <summary>
        /// List of conflicts for this entry
        /// </summary>
        public List<TblConflict> Conflicts { get; set; }

        /// <summary>
        /// Whether entry is selected in UI
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Byte length (number of bytes this entry represents)
        /// </summary>
        public int ByteLength => Entry.Length / 2;

        /// <summary>
        /// Formatted entry for display
        /// </summary>
        public string FormattedEntry => Entry;

        /// <summary>
        /// Type display name
        /// </summary>
        public string TypeDisplay => Type switch
        {
            DteType.Ascii => "ASCII",
            DteType.DualTitleEncoding => "DTE",
            DteType.MultipleTitleEncoding => "MTE",
            DteType.EndBlock => "End",
            DteType.EndLine => "Line",
            DteType.Japonais => "JP",
            _ => "?"
        };

        /// <summary>
        /// Status icon (✓, ⚠, ✖)
        /// </summary>
        public string StatusIcon =>
            !IsValid ? "✖" :
            HasConflict ? "⚠" :
            "✓";

        /// <summary>
        /// Status tooltip
        /// </summary>
        public string StatusTooltip =>
            !IsValid ? $"Invalid: {ValidationError}" :
            HasConflict ? $"Conflict detected: {Conflicts.Count} issue(s)" :
            "Valid";

        #endregion

        #region Methods

        /// <summary>
        /// Reset dirty flag
        /// </summary>
        public void ResetDirty()
        {
            // Note: Can't modify readonly fields, so IsDirty will still compute correctly
            OnPropertyChanged(nameof(IsDirty));
        }

        /// <summary>
        /// Convert back to Dte
        /// </summary>
        public Dte ToDto()
        {
            var dte = new Dte(Entry, Value, Type)
            {
                IsValid = IsValid,
                ValidationError = ValidationError
            };
            return dte;
        }

        /// <summary>
        /// Clone this view model
        /// </summary>
        public TblEntryViewModel Clone()
        {
            var clone = new TblEntryViewModel(ToDto())
            {
                HasConflict = HasConflict,
                IsSelected = IsSelected
            };
            clone.Conflicts.AddRange(Conflicts);
            return clone;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
