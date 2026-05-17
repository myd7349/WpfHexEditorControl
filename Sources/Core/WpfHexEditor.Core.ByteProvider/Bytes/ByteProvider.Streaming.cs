// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: ByteProvider.Streaming.cs
// Description:
//     Streaming / zero-copy read surface for ByteProvider — Phase 2.
//     Avoids materialising large byte arrays when the caller only needs
//     to iterate or copy data (e.g. export, hash, diff).
// ==========================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace WpfHexEditor.Core.Bytes
{
    public sealed partial class ByteProvider
    {
        private const int DefaultStreamChunk = 64 * 1024;

        // ── Lazy byte iterator ────────────────────────────────────────────────

        /// <summary>
        /// Lazily yield bytes from <paramref name="start"/> for <paramref name="count"/> bytes.
        /// Allocates one 64 KB buffer regardless of total length — safe for multi-GB files.
        /// </summary>
        public IEnumerable<byte> ReadBytesStream(long start, long count)
        {
            if (!IsOpen || count <= 0) yield break;

            long end = Math.Min(start + count, VirtualLength);
            for (long pos = start; pos < end; pos += DefaultStreamChunk)
            {
                int chunk = (int)Math.Min(DefaultStreamChunk, end - pos);
                byte[] buffer = GetBytes(pos, chunk);
                foreach (byte b in buffer)
                    yield return b;
            }
        }

        // ── Async line streaming ──────────────────────────────────────────────

        /// <summary>
        /// Asynchronously stream lines of <paramref name="bytesPerLine"/> bytes
        /// starting at <paramref name="startVirtualPosition"/>.
        /// Does not buffer the entire file — safe for multi-GB files.
        /// </summary>
        public async IAsyncEnumerable<byte[]> ReadLinesAsync(
            long startVirtualPosition,
            int bytesPerLine,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (!IsOpen || bytesPerLine <= 0) yield break;

            long pos = startVirtualPosition;
            long length = VirtualLength;

            // Batch ~64 KB worth of lines per Task.Run regardless of line width.
            int linesPerBatch = Math.Max(1, DefaultStreamChunk / bytesPerLine);

            while (pos < length)
            {
                ct.ThrowIfCancellationRequested();
                long remaining = (length - pos + bytesPerLine - 1) / bytesPerLine;
                int lineCount = (int)Math.Min(linesPerBatch, remaining);
                var lines = await Task.Run(() => GetLines(pos, lineCount, bytesPerLine), ct).ConfigureAwait(false);

                foreach (var line in lines)
                {
                    ct.ThrowIfCancellationRequested();
                    yield return line;
                }

                pos += (long)lineCount * bytesPerLine;
            }
        }

        // ── CopyTo ────────────────────────────────────────────────────────────

        /// <summary>
        /// Copy <paramref name="count"/> virtual bytes starting at <paramref name="start"/>
        /// into <paramref name="destination"/> asynchronously.
        /// Uses 64 KB read chunks — no full-file allocation.
        /// </summary>
        public async Task CopyToAsync(
            Stream destination,
            long start,
            long count,
            CancellationToken ct = default)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (count <= 0) return;

            long end = Math.Min(start + count, VirtualLength);

            for (long pos = start; pos < end; pos += DefaultStreamChunk)
            {
                ct.ThrowIfCancellationRequested();
                int chunk = (int)Math.Min(DefaultStreamChunk, end - pos);
                byte[] buffer = await Task.Run(() => GetBytes(pos, chunk), ct).ConfigureAwait(false);
                await destination.WriteAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Copy the entire virtual content into <paramref name="destination"/> asynchronously.
        /// </summary>
        public Task CopyToAsync(Stream destination, CancellationToken ct = default) =>
            CopyToAsync(destination, 0, VirtualLength, ct);
    }
}
