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
    string Source,       // "catalog" or user signature name
    long   EstSize       // 0 = unknown; parsed from magic header where possible
);

/// <summary>
/// Sliding-window file carver: scans a stream for embedded file signatures
/// using <see cref="FormatMatcher"/> (catalog) and <see cref="UserSignatureDbStore"/>.
/// </summary>
public static class FileCarverService
{
    private const int    ScanStep       = 512;
    private const int    HeaderReadSize = 64;
    private const string SourceCatalog  = "catalog";

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
                    results.Add(new CarvedEntry(offset, m.Entry.Name, m.Confidence, SourceCatalog,
                        EstimateSize(span, m.Entry.Name)));

            // User signatures scan
            foreach (var sig in userSignatures)
            {
                var pattern = sig.PatternBytes();
                if (pattern is null || pattern.Length == 0) continue;
                int sigOffset = sig.Offset;
                if (sigOffset + pattern.Length > read) continue;
                if (span.Slice(sigOffset, pattern.Length).SequenceEqual(pattern))
                    results.Add(new CarvedEntry(offset, sig.Name, 1.0, sig.Name, 0));
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

    // Dispatch on the catalog format name (already resolved by FormatMatcher — no byte re-check).
    private static long EstimateSize(ReadOnlySpan<byte> header, string formatName) => formatName switch
    {
        "ZIP Archive"              => EstimateZip(header),
        "PNG Image"                => EstimatePng(header),
        "Windows Executable (PE)"  => EstimatePe(header),
        _                          => 0,
    };

    private static long EstimateZip(ReadOnlySpan<byte> h)
    {
        // ZIP local file header layout: compressed size at +18 (uint32 LE),
        // file name length at +26 (uint16 LE), extra field length at +28 (uint16 LE).
        if (h.Length < 30) return 0;
        long compressedSize = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(h[18..]);
        int  fileNameLen    = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(h[26..]);
        int  extraLen       = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(h[28..]);
        return compressedSize > 0 ? 30 + fileNameLen + extraLen + compressedSize : 0;
    }

    private static long EstimatePng(ReadOnlySpan<byte> h)
    {
        // IHDR chunk: width at +16 (uint32 BE), height at +20 (uint32 BE).
        // 4 bytes/px (RGBA) is an over-estimate; actual size depends on compression.
        if (h.Length < 24) return 0;
        long width  = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(h[16..]);
        long height = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(h[20..]);
        return width > 0 && height > 0 ? width * height * 4 : 0;
    }

    private static long EstimatePe(ReadOnlySpan<byte> h)
    {
        // e_lfanew at +60 points to the PE signature; SizeOfImage is at lfanew+24+40.
        // Our window is only 64 bytes, so lfanew usually points beyond it — guard strictly.
        if (h.Length < 64) return 0;
        int lfanew            = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(h[60..]);
        int sizeOfImageOffset = lfanew + 64;
        if (sizeOfImageOffset + 4 > h.Length) return 0;
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(h[sizeOfImageOffset..]);
    }
}
