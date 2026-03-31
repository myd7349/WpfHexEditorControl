// ==========================================================
// Project: WpfHexEditor.Plugins.DocumentLoaders
// File: Parsers/Docx/DocxDocumentSaver.cs
// Description:
//     IDocumentSaver for DOCX files.
//     Strategy: copy-modify — open the original ZIP, copy all entries
//     except "word/document.xml", then rebuild that entry using
//     OoXmlSchemaEngine.SerializeBlocks() driven by DOCX.whfmt documentSchema.
//     No hardcoded OOXML element names in C#.
// ==========================================================

using System.IO.Compression;
using System.Xml.Linq;
using WpfHexEditor.Editor.DocumentEditor.Core;
using WpfHexEditor.Editor.DocumentEditor.Core.Model;
using WpfHexEditor.Editor.DocumentEditor.Core.Schema;

namespace WpfHexEditor.Plugins.DocumentLoaders.Parsers.Docx;

public sealed class DocxDocumentSaver : IDocumentSaver
{
    public string SaverName => "DOCX Document Saver";

    public IReadOnlyList<string> SupportedExtensions { get; } = [".docx", ".dotx"];

    public bool CanSave(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ext.Equals(".docx", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".dotx", StringComparison.OrdinalIgnoreCase);
    }

    public async Task SaveAsync(DocumentModel model, Stream output, CancellationToken ct = default)
    {
        // Load schema from DOCX.whfmt
        var schema = DocumentSchemaReader.ReadFromWhfmt("DOCX.whfmt");

        // Read original file as ZIP
        if (!File.Exists(model.FilePath))
            throw new FileNotFoundException("Original DOCX file not found.", model.FilePath);

        byte[] originalBytes = await File.ReadAllBytesAsync(model.FilePath, ct);
        using var originalMs = new MemoryStream(originalBytes);
        using var outputMs   = new MemoryStream();

        using (var originalZip = new ZipArchive(originalMs, ZipArchiveMode.Read, leaveOpen: true))
        using (var outputZip   = new ZipArchive(outputMs,   ZipArchiveMode.Create, leaveOpen: true))
        {
            const string documentEntry = "word/document.xml";

            foreach (var entry in originalZip.Entries)
            {
                if (entry.FullName.Equals(documentEntry, StringComparison.OrdinalIgnoreCase))
                    continue; // replaced below

                var newEntry = outputZip.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                newEntry.LastWriteTime = entry.LastWriteTime;
                await using var src = entry.Open();
                await using var dst = newEntry.Open();
                await src.CopyToAsync(dst, ct);
            }

            // Rebuild document.xml from model blocks
            string newXml = schema is not null
                ? OoXmlSchemaEngine.SerializeBlocks(model.Blocks, schema).ToString(SaveOptions.DisableFormatting)
                : FallbackSerialize(model);

            var docEntry = outputZip.CreateEntry(documentEntry, CompressionLevel.Optimal);
            await using var docStream = docEntry.Open();
            await using var writer   = new StreamWriter(docStream);
            await writer.WriteAsync(newXml);
        }

        outputMs.Position = 0;
        await outputMs.CopyToAsync(output, ct);
    }

    private static string FallbackSerialize(DocumentModel model)
    {
        var body = new XElement(
            XName.Get("document", "http://schemas.openxmlformats.org/wordprocessingml/2006/main"),
            new XElement(XName.Get("body", "http://schemas.openxmlformats.org/wordprocessingml/2006/main"),
                model.Blocks.Select(b =>
                    new XElement(XName.Get("p", "http://schemas.openxmlformats.org/wordprocessingml/2006/main"),
                        new XElement(XName.Get("r", "http://schemas.openxmlformats.org/wordprocessingml/2006/main"),
                            new XElement(XName.Get("t", "http://schemas.openxmlformats.org/wordprocessingml/2006/main"),
                                b.Text))))));
        return body.ToString(SaveOptions.DisableFormatting);
    }
}
