// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: ByteDiffResult.cs
// Description:
//     Result of a binary diff operation. Holds the ordered list of chunks
//     and aggregate statistics.
// ==========================================================

using System.Collections.Generic;
using System.Linq;

namespace WpfHexEditor.Core.Diff
{
    /// <summary>Result of comparing two binary sources.</summary>
    public sealed class ByteDiffResult
    {
        public IReadOnlyList<ByteDiffChunk> Chunks { get; }
        public long SourceLength { get; }
        public long TargetLength { get; }
        public long EqualBytes => Chunks.Where(c => c.Kind == DiffKind.Equal).Sum(c => c.Length);
        public long ModifiedBytes => Chunks.Where(c => c.Kind == DiffKind.Modified).Sum(c => c.Length);
        public long InsertedBytes => Chunks.Where(c => c.Kind == DiffKind.Inserted).Sum(c => c.Length);
        public long DeletedBytes => Chunks.Where(c => c.Kind == DiffKind.Deleted).Sum(c => c.Length);
        public bool IsIdentical => !Chunks.Any(c => c.Kind != DiffKind.Equal);

        internal ByteDiffResult(IReadOnlyList<ByteDiffChunk> chunks, long sourceLength, long targetLength)
        {
            Chunks = chunks;
            SourceLength = sourceLength;
            TargetLength = targetLength;
        }
    }
}
