//////////////////////////////////////////////
// Apache 2.0  - 2026
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfHexEditor.Editor.Core;

/// <summary>
/// Common contract for any embeddable document editor (Hex, TBL, JSON, …).
/// Implemented by a UserControl or FrameworkElement; the host (docking, main window)
/// interacts via this interface to drive the editor uniformly.
/// </summary>
public interface IDocumentEditor
{
    // ── State ────────────────────────────────────────────────────────────
    bool IsDirty    { get; }
    bool CanUndo    { get; }
    bool CanRedo    { get; }
    bool IsReadOnly { get; set; }   // DP-backed in WPF implementations

    /// <summary>Title displayed in the host tab ("file.bin", "file.bin *" if dirty).</summary>
    string Title { get; }

    // ── Bindable commands (host: MenuItem.Command, toolbar…) ─────────────
    ICommand UndoCommand      { get; }
    ICommand RedoCommand      { get; }
    ICommand SaveCommand      { get; }
    ICommand CopyCommand      { get; }
    ICommand CutCommand       { get; }
    ICommand PasteCommand     { get; }
    ICommand DeleteCommand    { get; }
    ICommand SelectAllCommand { get; }

    // ── Methods ──────────────────────────────────────────────────────────
    void Undo();
    void Redo();
    void Save();
    Task SaveAsync(CancellationToken ct = default);
    Task SaveAsAsync(string filePath, CancellationToken ct = default);
    void Copy();
    void Cut();
    void Paste();
    void Delete();
    void SelectAll();
    void Close();

    // ── Events (host updates its own menu/statusbar) ─────────────────────
    event EventHandler?         ModifiedChanged;  // IsDirty changed
    event EventHandler?         CanUndoChanged;
    event EventHandler?         CanRedoChanged;
    event EventHandler<string>? TitleChanged;     // "file.tbl *" — host updates the tab
    event EventHandler<string>? StatusMessage;    // toast / host statusbar
    event EventHandler?         SelectionChanged; // host re-queries CanExecute on commands
}
