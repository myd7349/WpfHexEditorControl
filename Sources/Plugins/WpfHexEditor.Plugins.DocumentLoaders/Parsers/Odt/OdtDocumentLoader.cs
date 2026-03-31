// ==========================================================
// Project: WpfHexEditor.Plugins.DocumentLoaders
// File: Parsers/Odt/OdtDocumentLoader.cs
// Description:
//     IDocumentLoader implementation for ODT/OTT files.
// ==========================================================

using System.Globalization;
using System.Xml.Linq;
using WpfHexEditor.Editor.Core;
using WpfHexEditor.Editor.DocumentEditor.Core;
using WpfHexEditor.Editor.DocumentEditor.Core.BinaryMap;
using WpfHexEditor.Editor.DocumentEditor.Core.Forensic;
using WpfHexEditor.Editor.DocumentEditor.Core.Model;
using WpfHexEditor.Editor.DocumentEditor.Core.Options;

namespace WpfHexEditor.Plugins.DocumentLoaders.Parsers.Odt;

public sealed class OdtDocumentLoader : IDocumentLoader
{
    public string LoaderName => "ODT Document Loader";

    public IReadOnlyList<string> SupportedExtensions { get; } = ["odt", "ott"];

    public bool CanLoad(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ext is not null &&
               (ext.Equals(".odt", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".ott", StringComparison.OrdinalIgnoreCase));
    }

    public async Task LoadAsync(
        string            filePath,
        Stream            stream,
        DocumentModel     target,
        CancellationToken ct = default)
    {
        byte[] rawBytes = await BufferStreamAsync(stream, ct);
        using var ms = new MemoryStream(rawBytes, writable: false);

        using var zipReader = new OdtZipReader(ms);

        string? mimeEntry = zipReader.ReadEntryText("mimetype");
        bool isOdt = mimeEntry is null ||
                     mimeEntry.Contains("opendocument.text", StringComparison.OrdinalIgnoreCase);
        if (!isOdt)
            throw new InvalidDataException("Archive mimetype does not identify an ODT/OTT document.");

        string? contentXml = zipReader.ReadEntryText("content.xml");
        if (contentXml is null)
            throw new InvalidDataException("ODT is missing content.xml entry.");

        long contentEntryOffset = zipReader.GetEntryDataOffset("content.xml");

        var mapBuilder = new BinaryMapBuilder();
        if (contentEntryOffset >= 0)
            mapBuilder.RegisterZipEntry("content.xml", contentEntryOffset);

        // Build style-name → formatting-properties map from styles.xml and content.xml automatic-styles
        var styleProps = new Dictionary<string, IReadOnlyDictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
        LoadOdtStyles(zipReader, "styles.xml",  styleProps);  // named styles (baseline)
        LoadOdtStyles(zipReader, "content.xml", styleProps);  // automatic styles override

        var mapper = new OdtXmlMapper(styleProps);
        var blocks = await Task.Run(
            () => mapper.Map(contentXml, contentEntryOffset, mapBuilder, ct), ct);

        var metadata = ReadMetaXml(zipReader);
        metadata = metadata with
        {
            MimeType = "application/vnd.oasis.opendocument.text",
            Title    = !string.IsNullOrEmpty(metadata.Title)
                           ? metadata.Title
                           : Path.GetFileNameWithoutExtension(filePath)
        };

        target.FilePath     = filePath;
        target.Metadata     = metadata;
        target.PageSettings = ReadOdtPageSettings(zipReader);

        foreach (var block in blocks)
            target.Blocks.Add(block);

        target.BinaryMap.MergeFrom(mapBuilder.Build());

        var alerts = new ForensicAnalyzer().Analyze(target, rawBytes);
        target.SetForensicAlerts(alerts);
    }

    private static async Task<byte[]> BufferStreamAsync(Stream stream, CancellationToken ct)
    {
        if (stream is MemoryStream ms && ms.TryGetBuffer(out _))
            return ms.ToArray();
        using var buf = new MemoryStream();
        await stream.CopyToAsync(buf, ct);
        return buf.ToArray();
    }

    private static DocumentMetadata ReadMetaXml(OdtZipReader zip)
    {
        var xml = zip.ReadEntryText("meta.xml");
        if (xml is null) return new DocumentMetadata();
        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace dc   = "http://purl.org/dc/elements/1.1/";
            XNamespace meta = "urn:oasis:names:tc:opendocument:xmlns:meta:1.0";

            return new DocumentMetadata
            {
                Title      = doc.Descendants(dc   + "title").FirstOrDefault()?.Value            ?? string.Empty,
                Author     = doc.Descendants(meta + "initial-creator").FirstOrDefault()?.Value  ?? string.Empty,
                CreatedUtc = TryParseDate(doc.Descendants(meta + "creation-date").FirstOrDefault()?.Value)
            };
        }
        catch { return new DocumentMetadata(); }
    }

    private static DateTime? TryParseDate(string? value) =>
        DateTime.TryParse(value, out var dt) ? dt.ToUniversalTime() : null;

    /// <summary>
    /// Reads all <c>style:style</c> elements from the given ZIP entry (styles.xml or content.xml)
    /// and extracts their text-properties (bold, italic, underline, fontSize, fontFamily) into
    /// <paramref name="into"/>. Content.xml automatic-styles should be loaded last to override.
    /// </summary>
    private static void LoadOdtStyles(
        OdtZipReader zipReader,
        string entryName,
        Dictionary<string, IReadOnlyDictionary<string, object>> into)
    {
        var xml = zipReader.ReadEntryText(entryName);
        if (xml is null) return;

        XDocument doc;
        try { doc = XDocument.Parse(xml); }
        catch { return; }

        XNamespace styleNs = "urn:oasis:names:tc:opendocument:xmlns:style:1.0";
        XNamespace foNs    = "urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0";

        foreach (var style in doc.Descendants(styleNs + "style"))
        {
            var name = style.Attribute(styleNs + "name")?.Value;
            if (name is null) continue;

            var props = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var tp    = style.Element(styleNs + "text-properties");
            if (tp is null) { into[name] = props; continue; }

            if (tp.Attribute(foNs + "font-weight")?.Value     == "bold")      props["bold"]      = true;
            if (tp.Attribute(foNs + "font-style")?.Value      == "italic")    props["italic"]    = true;
            if (tp.Attribute(foNs + "text-decoration")?.Value == "underline") props["underline"] = true;

            var fs = tp.Attribute(foNs + "font-size")?.Value;
            if (fs is not null)
            {
                // Parse "12pt", "14pt" → store as double fontSize
                if (fs.EndsWith("pt", StringComparison.Ordinal) &&
                    double.TryParse(fs[..^2], NumberStyles.Any, CultureInfo.InvariantCulture, out double ptVal))
                    props["fontSize"] = ptVal;
            }

            var ff = tp.Attribute(foNs  + "font-family")?.Value
                  ?? tp.Attribute(styleNs + "font-name")?.Value;
            if (ff is not null) props["fontFamily"] = ff;

            into[name] = props;
        }
    }

    private static DocumentPageSettings? ReadOdtPageSettings(OdtZipReader zip)
    {
        try
        {
            var xml = zip.ReadEntryText("styles.xml");
            if (xml is null) return null;

            XNamespace StyleNs = "urn:oasis:names:tc:opendocument:xmlns:style:1.0";
            XNamespace FoNs    = "urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0";

            var doc   = XDocument.Parse(xml);
            var props = doc.Descendants(StyleNs + "page-layout-properties").FirstOrDefault();
            if (props is null) return null;

            string? pgW   = props.Attribute(FoNs + "page-width")?.Value;
            string? pgH   = props.Attribute(FoNs + "page-height")?.Value;
            if (pgW is null || pgH is null) return null;

            string? mTop  = props.Attribute(FoNs + "margin-top")?.Value;
            string? mBot  = props.Attribute(FoNs + "margin-bottom")?.Value;
            string? mLeft = props.Attribute(FoNs + "margin-left")?.Value;
            string? mRight= props.Attribute(FoNs + "margin-right")?.Value;

            return DocumentPageSettings.FromOdt(pgW, pgH, mTop, mBot, mLeft, mRight);
        }
        catch { return null; }
    }
}
