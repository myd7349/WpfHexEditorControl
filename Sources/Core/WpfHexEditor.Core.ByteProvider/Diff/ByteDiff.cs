// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: ByteDiff.cs
// Description:
//     Models for the binary diff engine (Phase 3).
//     DiffKind mirrors the four states a byte position can have
//     when comparing two binary sources.
// ==========================================================

namespace WpfHexEditor.Core.Diff
{
    /// <summary>The kind of change at a diff chunk boundary.</summary>
    public enum DiffKind
    {
        Equal,
        Modified,
        Inserted,
        Deleted
    }

    /// <summary>
    /// A contiguous chunk of bytes that share the same <see cref="DiffKind"/>.
    /// For <see cref="DiffKind.Equal"/> only <see cref="Position"/> and <see cref="Length"/> are meaningful.
    /// </summary>
    public sealed record ByteDiffChunk(
        long Position,
        long Length,
        DiffKind Kind,
        byte[]? SourceBytes,
        byte[]? TargetBytes);
}
