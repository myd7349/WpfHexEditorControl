// ==========================================================
// Project: WpfHexEditor.Plugins.DocumentLoader.Odt
// File: Parsers/OdtZipReader.cs
// Description:
//     Opens an ODT/OTT (ZIP/ODF) archive and exposes the core XML
//     entries together with the absolute byte offsets of each entry's
//     data block within the ZIP container.
// ==========================================================

using System.IO.Compression;

namespace WpfHexEditor.Plugins.DocumentLoader.Odt.Parsers;

/// <summary>
/// Thin wrapper around <see cref="ZipArchive"/> that resolves
/// absolute byte offsets for ODT/ODF ZIP entry data segments.
/// </summary>
internal sealed class OdtZipReader : IDisposable
{
    private readonly ZipArchive _zip;
    private readonly Dictionary<string, long> _entryDataOffsets =
        new(StringComparer.OrdinalIgnoreCase);

    public OdtZipReader(Stream zipStream)
    {
        _zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        BuildOffsetTable(zipStream);
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public string? ReadEntryText(string entryName)
    {
        var entry = _zip.GetEntry(entryName);
        if (entry is null) return null;
        using var sr = new StreamReader(entry.Open());
        return sr.ReadToEnd();
    }

    public long GetEntryDataOffset(string entryName) =>
        _entryDataOffsets.TryGetValue(entryName, out long off) ? off : -1L;

    public void Dispose() => _zip.Dispose();

    // ── ZIP offset resolution (same algorithm as DocxZipReader) ─────────────

    private const uint LocalFileHeaderSig = 0x04034b50;

    private void BuildOffsetTable(Stream zipStream)
    {
        try
        {
            long savedPos = zipStream.Position;
            zipStream.Position = 0;

            using var br = new BinaryReader(zipStream, System.Text.Encoding.UTF8, leaveOpen: true);

            while (zipStream.Position + 4 <= zipStream.Length)
            {
                uint sig = br.ReadUInt32();
                if (sig != LocalFileHeaderSig) break;

                br.ReadUInt16(); br.ReadUInt16(); br.ReadUInt16();
                br.ReadUInt16(); br.ReadUInt16(); br.ReadUInt32();
                br.ReadUInt32(); br.ReadUInt32();
                ushort nameLen  = br.ReadUInt16();
                ushort extraLen = br.ReadUInt16();

                byte[] nameBytes = br.ReadBytes(nameLen);
                br.ReadBytes(extraLen);

                string entryName = System.Text.Encoding.UTF8.GetString(nameBytes);
                _entryDataOffsets[entryName] = zipStream.Position;

                var entry = _zip.GetEntry(entryName);
                if (entry is null) break;
                zipStream.Position += entry.CompressedLength;
            }

            zipStream.Position = savedPos;
        }
        catch { /* best-effort */ }
    }
}
