//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.IO;
using WpfHexEditor.Core.Definitions;
using WpfHexEditor.Core.Definitions.Matching;

namespace WpfHexEditor.App.BinaryAnalysis.Services;

/// <summary>A candidate embedded file found by the carver.</summary>
public sealed record CarvedEntry(
    long   Offset,
    string FormatName,
    double Confidence,
    string Source        // "catalog" or user signature name
);

/// <summary>
/// Sliding-window file carver: scans a stream for embedded file signatures
/// using <see cref="FormatMatcher"/> (catalog) and <see cref="UserSignatureDbStore"/>.
/// </summary>
public static class FileCarverService
{
    private const int ScanStep       = 512;
    private const int HeaderReadSize = 64;

    /// <summary>
    /// Scans <paramref name="stream"/> for embedded files.
    /// Reports progress as bytes scanned.
    /// </summary>
    public static async Task<List<CarvedEntry>> ScanAsync(
        Stream stream,
        IReadOnlyList<UserSignature> userSignatures,
        IProgress<long>? progress = null,
        CancellationToken ct = default)
    {
        var catalog = EmbeddedFormatCatalog.Instance;
        var results = new List<CarvedEntry>();
        var header  = new byte[HeaderReadSize];
        long length = stream.Length;
        long offset = 0;

        while (offset < length)
        {
            ct.ThrowIfCancellationRequested();

            stream.Position = offset;
            int read = await stream.ReadAsync(header.AsMemory(0, (int)Math.Min(HeaderReadSize, length - offset)), ct);
            if (read == 0) break;

            var span = header.AsSpan(0, read);

            // Catalog scan
            var matches = FormatMatcher.GetTopMatches(catalog, span, maxResults: 3);
            foreach (var m in matches)
                if (m.Confidence >= 0.7)
                    results.Add(new CarvedEntry(offset, m.Entry.Name, m.Confidence, "catalog"));

            // User signatures scan
            foreach (var sig in userSignatures)
            {
                var pattern = sig.PatternBytes();
                if (pattern is null || pattern.Length == 0) continue;
                int sigOffset = sig.Offset;
                if (sigOffset + pattern.Length > read) continue;
                if (span.Slice(sigOffset, pattern.Length).SequenceEqual(pattern))
                    results.Add(new CarvedEntry(offset, sig.Name, 1.0, sig.Name));
            }

            progress?.Report(offset);
            offset += ScanStep;
        }

        // Deduplicate: keep highest-confidence hit per (offset, format)
        return results
            .GroupBy(e => (e.Offset, e.FormatName))
            .Select(g => g.MaxBy(e => e.Confidence)!)
            .OrderBy(e => e.Offset)
            .ToList();
    }
}
