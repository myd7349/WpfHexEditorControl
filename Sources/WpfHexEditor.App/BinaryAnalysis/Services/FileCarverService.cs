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
                    results.Add(new CarvedEntry(offset, m.Entry.Name, m.Confidence, "catalog",
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

    // Reads known size fields from the first HeaderReadSize bytes of each supported format.
    // Returns 0 when the format is unsupported or the header is too short.
    private static long EstimateSize(ReadOnlySpan<byte> header, string formatName)
    {
        return formatName switch
        {
            _ when IsZip(header)  => EstimateZip(header),
            _ when IsPng(header)  => EstimatePng(header),
            _ when IsPe(header)   => EstimatePe(header),
            _                     => 0,
        };
    }

    private static bool IsZip(ReadOnlySpan<byte> h)
        => h.Length >= 4 && h[0] == 0x50 && h[1] == 0x4B && h[2] == 0x03 && h[3] == 0x04;

    private static bool IsPng(ReadOnlySpan<byte> h)
        => h.Length >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47;

    private static bool IsPe(ReadOnlySpan<byte> h)
        => h.Length >= 2 && h[0] == 0x4D && h[1] == 0x5A; // MZ

    // ZIP local file header: compressed size is a LE int32 at offset 18
    private static long EstimateZip(ReadOnlySpan<byte> h)
    {
        if (h.Length < 26) return 0;
        long compressedSize = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(h[18..]);
        int  fileNameLen    = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(h[26..]);
        int  extraLen       = h.Length >= 30 ? System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(h[28..]) : 0;
        return compressedSize > 0 ? 30 + fileNameLen + extraLen + compressedSize : 0;
    }

    // PNG IHDR: width at offset 16 (BE int32), height at 20 (BE int32) — rough estimate only
    private static long EstimatePng(ReadOnlySpan<byte> h)
    {
        if (h.Length < 24) return 0;
        long width  = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(h[16..]);
        long height = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(h[20..]);
        // Rough estimate: 4 bytes/px (RGBA) + IDAT overhead; far better than 0
        return width > 0 && height > 0 ? width * height * 4 : 0;
    }

    // PE: SizeOfImage is a LE uint32 in the Optional Header at e_lfanew+24+40 (PE32) or PE32+
    // We only have 64 bytes; e_lfanew is at offset 60 — often points past our window, so guard.
    private static long EstimatePe(ReadOnlySpan<byte> h)
    {
        if (h.Length < 64) return 0;
        int lfanew = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(h[60..]);
        int sizeOfImageOffset = lfanew + 24 + 40; // optional header starts at lfanew+24; SizeOfImage at +40
        if (sizeOfImageOffset + 4 > h.Length) return 0;
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(h[sizeOfImageOffset..]);
    }
}
