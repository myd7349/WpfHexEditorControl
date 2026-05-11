// ==========================================================
// Project: WpfHexEditor.Editor.DocumentEditor
// File: Services/EmbeddedObjectsScanner.cs
// Description:
//     Walks a DocumentModel's block tree to enumerate embedded
//     artefacts (images, OLE objects, macros) for forensic review.
//     Independent of format: relies on block Kind + Attributes
//     populated by loaders (DocxXmlMapper, OdtXmlMapper,
//     RtfStructureBuilder).
// ==========================================================

using System.IO;
using WpfHexEditor.Editor.DocumentEditor.Core.Model;

namespace WpfHexEditor.Editor.DocumentEditor.Services;

/// <summary>
/// Discovers embedded objects (images, OLE blobs, VBA macros) inside a
/// <see cref="DocumentModel"/> for the Embedded Objects review panel.
/// </summary>
public static class EmbeddedObjectsScanner
{
    /// <summary>
    /// Returns one entry per embedded artefact: images (with zipEntryName
    /// or in-memory binaryData), OLE objects, and a synthetic entry for
    /// VBA macros when <see cref="DocumentMetadata.HasMacros"/> is set.
    /// </summary>
    public static IReadOnlyList<EmbeddedObjectEntry> Scan(DocumentModel? model)
    {
        var list = new List<EmbeddedObjectEntry>();
        if (model is null) return list;

        Walk(model.Blocks, list);

        // Synthetic entry for VBA macros — only the loader can confirm
        // their presence (it reads word/vbaProject.bin). Drill-down opens
        // that ZIP entry from the source file when available.
        if (model.Metadata?.HasMacros == true)
        {
            list.Add(new EmbeddedObjectEntry
            {
                Kind         = "macro",
                Name         = "vbaProject.bin",
                SizeBytes    = -1,
                Source       = "word/vbaProject.bin",
                ZipEntryName = "word/vbaProject.bin",
                Block        = null
            });
        }
        return list;
    }

    private static void Walk(IEnumerable<DocumentBlock> blocks, List<EmbeddedObjectEntry> sink)
    {
        foreach (var b in blocks)
        {
            if (b.Kind == "image" || b.Kind == "object")
            {
                var entry = new EmbeddedObjectEntry
                {
                    Kind  = b.Kind == "object" ? "OLE" : "image",
                    Name  = ExtractName(b),
                    Block = b
                };
                if (b.Attributes.TryGetValue("zipEntryName", out var ze) && ze is string zes)
                {
                    entry.Source       = zes;
                    entry.ZipEntryName = zes;
                }
                if (b.Attributes.TryGetValue("binaryData", out var bd) && bd is byte[] bytes)
                {
                    entry.InlineData = bytes;
                    entry.SizeBytes  = bytes.Length;
                }
                else if (b.Attributes.TryGetValue("binarySize", out var bs))
                {
                    entry.SizeBytes = bs is int isz ? isz : -1;
                }
                else
                {
                    entry.SizeBytes = b.RawLength;
                }
                if (string.IsNullOrEmpty(entry.Source))
                    entry.Source = $"@0x{b.RawOffset:X}";

                sink.Add(entry);
            }

            if (b.Children.Count > 0) Walk(b.Children, sink);
        }
    }

    private static string ExtractName(DocumentBlock b)
    {
        if (b.Attributes.TryGetValue("zipEntryName", out var ze) && ze is string s && !string.IsNullOrEmpty(s))
            return Path.GetFileName(s);
        if (!string.IsNullOrEmpty(b.Text)) return b.Text;
        return b.Kind;
    }
}

/// <summary>A single embedded artefact discovered by <see cref="EmbeddedObjectsScanner"/>.</summary>
public sealed class EmbeddedObjectEntry
{
    public string  Kind         { get; set; } = string.Empty;
    public string  Name         { get; set; } = string.Empty;
    public string  Source       { get; set; } = string.Empty;
    public int     SizeBytes    { get; set; }
    public string? ZipEntryName { get; set; }
    public byte[]? InlineData   { get; set; }
    public DocumentBlock? Block { get; set; }

    public string SizeText => SizeBytes < 0
        ? "—"
        : SizeBytes < 1024
            ? $"{SizeBytes} B"
            : SizeBytes < 1024 * 1024
                ? $"{SizeBytes / 1024.0:F1} KB"
                : $"{SizeBytes / 1024.0 / 1024.0:F2} MB";
}
