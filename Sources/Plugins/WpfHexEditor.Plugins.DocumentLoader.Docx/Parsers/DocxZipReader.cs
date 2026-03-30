// ==========================================================
// Project: WpfHexEditor.Plugins.DocumentLoader.Docx
// File: Parsers/DocxZipReader.cs
// Description:
//     Opens a DOCX (ZIP) archive and exposes the core XML entries
//     together with the absolute byte-offset of each entry's data
//     within the ZIP container so the BinaryMap can map
//     document-xml element positions to file offsets.
// ==========================================================

using System.IO.Compression;

namespace WpfHexEditor.Plugins.DocumentLoader.Docx.Parsers;

/// <summary>
/// Thin wrapper around <see cref="ZipArchive"/> that resolves
/// absolute byte offsets for ZIP entry data segments.
/// </summary>
internal sealed class DocxZipReader : IDisposable
{
    private readonly ZipArchive _zip;
    // Maps entry full name → absolute data start offset in the ZIP stream
    private readonly Dictionary<string, long> _entryDataOffsets = new(StringComparer.OrdinalIgnoreCase);

    public DocxZipReader(Stream zipStream)
    {
        _zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        BuildOffsetTable(zipStream);
    }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Opens an entry and returns its XML content as a string.
    /// Returns <see langword="null"/> if the entry does not exist.
    /// </summary>
    public string? ReadEntryText(string entryName)
    {
        var entry = _zip.GetEntry(entryName);
        if (entry is null) return null;
        using var sr = new StreamReader(entry.Open());
        return sr.ReadToEnd();
    }

    /// <summary>
    /// Returns the absolute offset of <paramref name="entryName"/>'s data
    /// within the ZIP stream, or -1 if unknown.
    /// </summary>
    public long GetEntryDataOffset(string entryName) =>
        _entryDataOffsets.TryGetValue(entryName, out long off) ? off : -1L;

    public void Dispose() => _zip.Dispose();

    // ── ZIP local-file-header offset resolution ─────────────────────────────
    //
    //  Each ZIP local file header starts at a known absolute offset.
    //  Layout:
    //    [4]  signature 0x04034b50
    //    [2]  version needed
    //    [2]  flags
    //    [2]  compression
    //    [2]  mod time
    //    [2]  mod date
    //    [4]  crc-32
    //    [4]  compressed size
    //    [4]  uncompressed size
    //    [2]  file name length  (n)
    //    [2]  extra field length (m)
    //    [n]  file name
    //    [m]  extra field
    //    <-- data starts here -->

    private const uint LocalFileHeaderSig = 0x04034b50;
    private const int  LocalHeaderFixedSize = 30;

    private void BuildOffsetTable(Stream zipStream)
    {
        try
        {
            long savedPos = zipStream.Position;
            zipStream.Position = 0;

            using var br = new BinaryReader(zipStream, System.Text.Encoding.UTF8, leaveOpen: true);

            while (zipStream.Position + 4 <= zipStream.Length)
            {
                long headerStart = zipStream.Position;
                uint sig = br.ReadUInt32();
                if (sig != LocalFileHeaderSig) break;

                // Skip fixed fields up to file name length
                br.ReadUInt16(); // version needed
                br.ReadUInt16(); // flags
                br.ReadUInt16(); // compression
                br.ReadUInt16(); // mod time
                br.ReadUInt16(); // mod date
                br.ReadUInt32(); // crc-32
                br.ReadUInt32(); // compressed size
                br.ReadUInt32(); // uncompressed size
                ushort nameLen  = br.ReadUInt16();
                ushort extraLen = br.ReadUInt16();

                byte[] nameBytes = br.ReadBytes(nameLen);
                br.ReadBytes(extraLen); // skip extra

                string entryName = System.Text.Encoding.UTF8.GetString(nameBytes);
                long   dataStart  = zipStream.Position;   // absolute data offset

                _entryDataOffsets[entryName] = dataStart;

                // Skip over compressed data to find next header
                var entry = _zip.GetEntry(entryName);
                if (entry is null) break;
                zipStream.Position = dataStart + entry.CompressedLength;
            }

            zipStream.Position = savedPos;
        }
        catch
        {
            // Offset table is best-effort; forensic analysis will report gaps
        }
    }
}
