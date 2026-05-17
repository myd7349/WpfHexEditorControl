// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: ByteProvider.Async.cs
// Description:
//     Async surface for ByteProvider — Phase 1 of the v2 public API expansion.
//     All blocking I/O is offloaded to the thread pool via Task.Run so callers
//     can stay on the UI thread without freezing it.
// ==========================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WpfHexEditor.Core.Bytes
{
    public sealed partial class ByteProvider
    {
        // ── File operations ───────────────────────────────────────────────────

        /// <summary>Open a file asynchronously.</summary>
        public Task OpenFileAsync(string filePath, bool readOnly = false, CancellationToken ct = default) =>
            Task.Run(() => { ct.ThrowIfCancellationRequested(); OpenFile(filePath, readOnly); }, ct);

        /// <summary>Open a stream asynchronously.</summary>
        public Task OpenStreamAsync(Stream stream, bool readOnly = false, CancellationToken ct = default) =>
            Task.Run(() => { ct.ThrowIfCancellationRequested(); OpenStream(stream, readOnly); }, ct);

        /// <summary>Open an in-memory byte array asynchronously.</summary>
        public Task OpenMemoryAsync(byte[] data, bool readOnly = false, CancellationToken ct = default) =>
            Task.Run(() => { ct.ThrowIfCancellationRequested(); OpenMemory(data, readOnly); }, ct);

        // ── Read ──────────────────────────────────────────────────────────────

        /// <summary>Read a single byte at a virtual position asynchronously.</summary>
        public ValueTask<(byte value, bool success)> GetByteAsync(long virtualPosition, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return new ValueTask<(byte, bool)>(GetByte(virtualPosition));
        }

        /// <summary>Read multiple bytes starting at a virtual position asynchronously.</summary>
        public ValueTask<byte[]> GetBytesAsync(long virtualPosition, int count, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (count <= 4096)
                return new ValueTask<byte[]>(GetBytes(virtualPosition, count));

            return new ValueTask<byte[]>(
                Task.Run(() => { ct.ThrowIfCancellationRequested(); return GetBytes(virtualPosition, count); }, ct));
        }

        // ── Save ──────────────────────────────────────────────────────────────

        /// <summary>Save all changes asynchronously.</summary>
        public Task SaveAsync(CancellationToken ct = default) =>
            Task.Run(() => { ct.ThrowIfCancellationRequested(); Save(); }, ct);

        /// <summary>Save to a new file path asynchronously.</summary>
        public Task SaveAsAsync(string newFilePath, bool overwrite = false, CancellationToken ct = default) =>
            Task.Run(() => { ct.ThrowIfCancellationRequested(); SaveAs(newFilePath, overwrite); }, ct);

        // ── Search ────────────────────────────────────────────────────────────

        /// <summary>
        /// Find all occurrences of <paramref name="pattern"/> as a streaming async sequence.
        /// Runs the search loop in a single background task and yields batches of positions,
        /// avoiding the per-match Task.Run overhead for high-match-count files.
        /// </summary>
        public async IAsyncEnumerable<long> FindAllAsync(
            byte[] pattern,
            long startPosition = 0,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (pattern == null || pattern.Length == 0) yield break;

            // Channel bridges the search thread to the async consumer without buffering all results.
            var channel = Channel.CreateBounded<long>(new BoundedChannelOptions(256)
            {
                SingleWriter = true,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait
            });

            var producer = Task.Run(async () =>
            {
                try
                {
                    long pos = startPosition;
                    while (!ct.IsCancellationRequested)
                    {
                        long found = FindFirst(pattern, pos);
                        if (found < 0) break;
                        await channel.Writer.WriteAsync(found, ct).ConfigureAwait(false);
                        pos = found + 1;
                    }
                }
                finally
                {
                    channel.Writer.TryComplete();
                }
            }, ct);

            await foreach (long match in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                yield return match;

            await producer.ConfigureAwait(false);
        }
    }
}
