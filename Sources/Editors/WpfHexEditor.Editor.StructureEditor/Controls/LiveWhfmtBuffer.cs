//////////////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
// Project: WpfHexEditor.Editor.StructureEditor
// File: Controls/LiveWhfmtBuffer.cs
// Description: Lightweight IDocumentBuffer implementation owned by StructureEditor.
//              Acts as a one-way bridge: StructureEditor serializes the full .whfmt
//              JSON and pushes it here; CodeEditorSplitHost reads it for live preview.
//////////////////////////////////////////////////////

using WpfHexEditor.Editor.Core;
using WpfHexEditor.Editor.Core.Documents;
using WpfHexEditor.Editor.Core.Undo;

namespace WpfHexEditor.Editor.StructureEditor.Controls;

/// <summary>
/// Minimal <see cref="IDocumentBuffer"/> implementation used to feed a live JSON
/// snapshot of the current .whfmt file to an embedded <c>CodeEditorSplitHost</c>.
/// StructureEditor is the sole producer; it calls <see cref="SetText"/> after every
/// debounced dirty change.
/// </summary>
internal sealed class LiveWhfmtBuffer : IDocumentBuffer
{
    private int    _version;
    private string _text = string.Empty;

    public LiveWhfmtBuffer(string filePath) => FilePath = filePath;

    /// <inheritdoc/>
    public string FilePath   { get; }

    /// <inheritdoc/>
    public string LanguageId => "json";

    /// <inheritdoc/>
    public int Version => _version;

    /// <inheritdoc/>
    public string Text => _text;

    /// <inheritdoc/>
    public UndoEngine? SharedUndoEngine => null;

    /// <inheritdoc/>
    public event EventHandler<DocumentBufferChangedEventArgs>? Changed;

    /// <summary>
    /// Replaces the buffer text and notifies all listeners (i.e. the attached CodeEditor).
    /// Must be called on the WPF Dispatcher thread.
    /// </summary>
    public void SetText(string text, IDocumentEditor? source = null)
    {
        _text = text;
        _version++;
        Changed?.Invoke(this, new DocumentBufferChangedEventArgs
        {
            NewText    = text,
            NewVersion = _version,
            Source     = source,
        });
    }
}
