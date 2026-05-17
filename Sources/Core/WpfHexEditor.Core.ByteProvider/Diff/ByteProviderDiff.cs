// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: ByteProviderDiff.cs
// Description:
//     Binary diff engine for ByteProvider — Phase 3.
//     Strategy: block-level comparison (64 KB chunks).
//     Equal blocks are detected first (fast-path via hash), then individual
//     bytes within differing blocks are compared to classify Modified/Inserted/Deleted.
//     ApplyDiff replays a ByteDiffResult onto a target ByteProvider.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using WpfHexEditor.Core.Bytes;

namespace WpfHexEditor.Core.Diff
{
    /// <summary>
    /// Compares two <see cref="ByteProvider"/> instances and produces a <see cref="ByteDiffResult"/>.
    /// </summary>
    public static class ByteProviderDiff
    {
        private const int BlockSize = 64 * 1024;

        /// <summary>
        /// Compare <paramref name="source"/> and <paramref name="target"/> byte-by-byte.
        /// Returns a <see cref="ByteDiffResult"/> with ordered chunks.
        /// </summary>
        public static ByteDiffResult Compare(
            ByteProvider source,
            ByteProvider target,
            CancellationToken ct = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            long srcLen = source.VirtualLength;
            long tgtLen = target.VirtualLength;
            long commonLen = Math.Min(srcLen, tgtLen);

            var chunks = new List<ByteDiffChunk>();
            long pos = 0;

            while (pos < commonLen)
            {
                ct.ThrowIfCancellationRequested();
                int block = (int)Math.Min(BlockSize, commonLen - pos);

                byte[] srcBlock = source.GetBytes(pos, block);
                byte[] tgtBlock = target.GetBytes(pos, block);

                if (BlocksEqual(srcBlock, tgtBlock))
                {
                    AppendOrMerge(chunks, new ByteDiffChunk(pos, block, DiffKind.Equal, null, null));
                }
                else
                {
                    // Byte-level classification within the differing block
                    ClassifyBlock(chunks, srcBlock, tgtBlock, pos);
                }

                pos += block;
            }

            // Handle length difference
            if (srcLen > tgtLen)
            {
                long extra = srcLen - tgtLen;
                byte[] deleted = source.GetBytes(pos, (int)Math.Min(extra, int.MaxValue));
                chunks.Add(new ByteDiffChunk(pos, extra, DiffKind.Deleted, deleted, null));
            }
            else if (tgtLen > srcLen)
            {
                long extra = tgtLen - srcLen;
                byte[] inserted = target.GetBytes(pos, (int)Math.Min(extra, int.MaxValue));
                chunks.Add(new ByteDiffChunk(pos, extra, DiffKind.Inserted, null, inserted));
            }

            return new ByteDiffResult(chunks, srcLen, tgtLen);
        }

        /// <summary>
        /// Apply a <see cref="ByteDiffResult"/> onto <paramref name="target"/> so it matches source.
        /// Assumes <paramref name="target"/> has the same content as the original target used in <see cref="Compare"/>.
        /// </summary>
        public static void ApplyDiff(ByteProvider target, ByteDiffResult diff, CancellationToken ct = default)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (diff == null) throw new ArgumentNullException(nameof(diff));

            target.BeginUndoTransaction("Apply diff");
            try
            {
                long virtualOffset = 0;

                foreach (var chunk in diff.Chunks)
                {
                    ct.ThrowIfCancellationRequested();

                    switch (chunk.Kind)
                    {
                        case DiffKind.Equal:
                            virtualOffset += chunk.Length;
                            break;

                        case DiffKind.Modified:
                            if (chunk.SourceBytes != null)
                                target.ModifyBytes(virtualOffset, chunk.SourceBytes);
                            virtualOffset += chunk.Length;
                            break;

                        case DiffKind.Deleted:
                            // Target had these bytes, remove them
                            target.DeleteBytes(virtualOffset, chunk.Length);
                            // virtualOffset does NOT advance — bytes shift
                            break;

                        case DiffKind.Inserted:
                            // Target is missing these bytes, insert them
                            if (chunk.TargetBytes != null)
                                target.InsertBytes(virtualOffset, chunk.TargetBytes);
                            virtualOffset += chunk.Length;
                            break;
                    }
                }

                target.CommitUndoTransaction();
            }
            catch
            {
                target.RollbackUndoTransaction();
                throw;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool BlocksEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            return a.AsSpan().SequenceEqual(b.AsSpan());
        }

        private static void ClassifyBlock(List<ByteDiffChunk> chunks, byte[] srcBlock, byte[] tgtBlock, long basePos)
        {
            int len = Math.Min(srcBlock.Length, tgtBlock.Length);
            long runStart = 0;
            bool? prevEqual = null;

            for (int i = 0; i <= len; i++)
            {
                bool equal = i < len && srcBlock[i] == tgtBlock[i];

                if (prevEqual.HasValue && equal != prevEqual.Value)
                {
                    // Flush current run
                    FlushRun(chunks, srcBlock, tgtBlock, basePos, runStart, i, prevEqual.Value);
                    runStart = i;
                }

                prevEqual = equal;
            }

            if (prevEqual.HasValue)
                FlushRun(chunks, srcBlock, tgtBlock, basePos, runStart, len, prevEqual.Value);
        }

        private static void FlushRun(
            List<ByteDiffChunk> chunks, byte[] src, byte[] tgt,
            long basePos, long start, long end, bool isEqual)
        {
            if (end <= start) return;
            long length = end - start;
            long pos = basePos + start;

            if (isEqual)
            {
                AppendOrMerge(chunks, new ByteDiffChunk(pos, length, DiffKind.Equal, null, null));
            }
            else
            {
                byte[] srcBytes = src.AsSpan((int)start, (int)length).ToArray();
                byte[] tgtBytes = tgt.AsSpan((int)start, (int)length).ToArray();
                chunks.Add(new ByteDiffChunk(pos, length, DiffKind.Modified, srcBytes, tgtBytes));
            }
        }

        private static void AppendOrMerge(List<ByteDiffChunk> chunks, ByteDiffChunk chunk)
        {
            if (chunks.Count > 0)
            {
                var last = chunks[^1];
                if (last.Kind == DiffKind.Equal && last.Position + last.Length == chunk.Position)
                {
                    chunks[^1] = last with { Length = last.Length + chunk.Length };
                    return;
                }
            }
            chunks.Add(chunk);
        }
    }
}
