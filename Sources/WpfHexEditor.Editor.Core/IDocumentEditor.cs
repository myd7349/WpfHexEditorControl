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
/// Contrat commun pour tout éditeur de document embarquable (Hex, TBL, JSON, …).
/// Implémenté par un UserControl ou FrameworkElement; le host (docking, main window)
/// interagit via cette interface pour piloter l'éditeur de manière uniforme.
/// </summary>
public interface IDocumentEditor
{
    // ── État ─────────────────────────────────────────────────────────────
    bool IsDirty    { get; }
    bool CanUndo    { get; }
    bool CanRedo    { get; }
    bool IsReadOnly { get; set; }   // DP-backed dans les implémentations WPF

    /// <summary>Titre affiché dans l'onglet du host ("file.bin", "file.bin *" si dirty).</summary>
    string Title { get; }

    // ── Commandes bindables (host : MenuItem.Command, toolbar…) ──────────
    ICommand UndoCommand      { get; }
    ICommand RedoCommand      { get; }
    ICommand SaveCommand      { get; }
    ICommand CopyCommand      { get; }
    ICommand CutCommand       { get; }
    ICommand PasteCommand     { get; }
    ICommand DeleteCommand    { get; }
    ICommand SelectAllCommand { get; }

    // ── Méthodes ─────────────────────────────────────────────────────────
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

    // ── Événements (host met à jour son propre menu/statusbar) ────────────
    event EventHandler?         ModifiedChanged;  // IsDirty a changé
    event EventHandler?         CanUndoChanged;
    event EventHandler?         CanRedoChanged;
    event EventHandler<string>? TitleChanged;     // "file.tbl *" — host met à jour l'onglet
    event EventHandler<string>? StatusMessage;    // toast / statusbar du host
    event EventHandler?         SelectionChanged; // host re-query CanExecute des commandes
}
