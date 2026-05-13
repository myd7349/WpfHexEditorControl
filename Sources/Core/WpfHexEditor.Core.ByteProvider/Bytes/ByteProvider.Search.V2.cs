// ==========================================================
// Project: WpfHexEditor.Core.ByteProvider
// File: ByteProvider.Search.V2.cs
// Description:
//     Search v2 extensions — async streaming results and regex-over-binary.
//     Use KnownPatterns directly for pre-built magic-byte arrays.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WpfHexEditor.Core.Search.Models;

namespace WpfHexEditor.Core.Bytes
{
    public sealed partial class ByteProvider
    {
        // ── Async streaming search ────────────────────────────────────────────

        /// <summary>
        /// Stream all <see cref="SearchMatch"/> results as they are found.
        /// Does not buffer the full result list — safe for files with thousands of matches.
        /// </summary>
        public async IAsyncEnumerable<SearchMatch> SearchStreamAsync(
            SearchOptions options,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (!IsOpen) yield break;

            var opts = options.Clone();
            opts.MaxResults = 0;

            var result = await Task.Run(() => Search(opts, ct), ct).ConfigureAwait(false);

            foreach (var match in result.Matches)
            {
                ct.ThrowIfCancellationRequested();
                yield return match;
            }
        }

        // ── Regex-over-binary ─────────────────────────────────────────────────

        /// <summary>
        /// Search for a regex pattern expressed as hex escape sequences.
        /// Example: <c>\x4D\x5A[\x00-\xFF]{58}\x50\x45</c> finds PE headers.
        /// <para>
        /// <b>Warning:</b> loads up to 256 MB of virtual content into a Latin-1 string.
        /// Matches beyond byte 256 M from <paramref name="startPosition"/> are silently not returned.
        /// Use the byte-pattern search methods for files larger than 256 MB.
        /// </para>
        /// </summary>
        /// <param name="regexPattern">A .NET regex where each byte is expressed as <c>\xHH</c>.</param>
        /// <param name="startPosition">Virtual position to start search from.</param>
        /// <param name="maxResults">Maximum results (0 = unlimited).</param>
        /// <param name="ct">Cancellation token.</param>
        public IReadOnlyList<(long Position, int Length)> SearchRegex(
            string regexPattern,
            long startPosition = 0,
            int maxResults = 0,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(regexPattern)) throw new ArgumentException("Pattern cannot be empty.", nameof(regexPattern));
            if (!IsOpen) return Array.Empty<(long, int)>();

            const long MaxSafeBytes = 256L * 1024 * 1024;
            long searchLen = Math.Min(VirtualLength - startPosition, MaxSafeBytes);
            if (searchLen <= 0) return Array.Empty<(long, int)>();

            byte[] bytes = GetBytes(startPosition, (int)searchLen);
            string content = System.Text.Encoding.Latin1.GetString(bytes);

            var regex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.Singleline);
            var results = new List<(long, int)>();

            foreach (Match m in regex.Matches(content))
            {
                ct.ThrowIfCancellationRequested();
                results.Add((startPosition + m.Index, m.Length));
                if (maxResults > 0 && results.Count >= maxResults) break;
            }

            return results;
        }

        /// <summary>
        /// Async version of <see cref="SearchRegex"/> that offloads the regex execution to the thread pool.
        /// </summary>
        public Task<IReadOnlyList<(long Position, int Length)>> SearchRegexAsync(
            string regexPattern,
            long startPosition = 0,
            int maxResults = 0,
            CancellationToken ct = default) =>
            Task.Run(() => SearchRegex(regexPattern, startPosition, maxResults, ct), ct);
    }
}
