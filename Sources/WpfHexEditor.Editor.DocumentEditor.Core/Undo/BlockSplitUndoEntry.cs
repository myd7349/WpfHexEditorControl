// ==========================================================
// Project: WpfHexEditor.Editor.DocumentEditor.Core
// File: Undo/BlockSplitUndoEntry.cs
// Description:
//     Undo entry for splitting one block into two at a char offset.
//     Undo merges the two halves back; Redo re-splits.
// ==========================================================

using System.Diagnostics.CodeAnalysis;
using WpfHexEditor.Editor.Core.Undo;
using WpfHexEditor.Editor.DocumentEditor.Core.Model;

namespace WpfHexEditor.Editor.DocumentEditor.Core.Undo;

/// <summary>
/// Records a block split (e.g. Enter key). Undo merges; Redo re-splits.
/// </summary>
public sealed class BlockSplitUndoEntry : IUndoEntry
{
    public required DocumentModel Model      { get; init; }
    public required DocumentBlock First      { get; init; }
    public required DocumentBlock Second     { get; init; }
    public required int           BlockIndex { get; init; }
    public required string        FirstText  { get; init; }
    public required string        SecondText { get; init; }

    public string   Description => $"Split {First.Kind}";
    public long     Revision    { get; set; }
    public DateTime Timestamp   { get; } = DateTime.UtcNow;

    public void Undo()
    {
        // Restore first block to its full original text and remove the second.
        First.Text = FirstText + SecondText;
        if (BlockIndex + 1 < Model.Blocks.Count &&
            ReferenceEquals(Model.Blocks[BlockIndex + 1], Second))
        {
            Model.Blocks.RemoveAt(BlockIndex + 1);
        }
    }

    public void Redo()
    {
        First.Text  = FirstText;
        Second.Text = SecondText;
        var insertAt = Math.Min(BlockIndex + 1, Model.Blocks.Count);
        if (!Model.Blocks.Contains(Second))
            Model.Blocks.Insert(insertAt, Second);
    }

    public bool TryMerge(IUndoEntry next, [NotNullWhen(true)] out IUndoEntry? merged)
    {
        merged = null;
        return false;
    }
}
