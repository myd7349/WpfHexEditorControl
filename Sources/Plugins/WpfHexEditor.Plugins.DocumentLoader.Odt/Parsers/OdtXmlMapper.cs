// ==========================================================
// Project: WpfHexEditor.Plugins.DocumentLoader.Odt
// File: Parsers/OdtXmlMapper.cs
// Description:
//     Parses content.xml (ODF) into DocumentBlock trees.
//     Maps <text:p> → paragraph, <text:span> → run,
//     <text:h> → heading, <draw:frame>/<draw:image> → image blocks.
// ==========================================================

using System.Xml.Linq;
using WpfHexEditor.Editor.DocumentEditor.Core.BinaryMap;
using WpfHexEditor.Editor.DocumentEditor.Core.Model;

namespace WpfHexEditor.Plugins.DocumentLoader.Odt.Parsers;

/// <summary>
/// Maps ODF content.xml elements to <see cref="DocumentBlock"/> instances.
/// </summary>
internal sealed class OdtXmlMapper
{
    private static readonly XNamespace TextNs  = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
    private static readonly XNamespace DrawNs  = "urn:oasis:names:tc:opendocument:xmlns:drawing:1.0";
    private static readonly XNamespace StyleNs = "urn:oasis:names:tc:opendocument:xmlns:style:1.0";
    private static readonly XNamespace TableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";

    // ── Public entry point ──────────────────────────────────────────────────

    public List<DocumentBlock> Map(
        string           contentXml,
        long             entryBaseOffset,
        BinaryMapBuilder mapBuilder,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var root   = XDocument.Parse(contentXml, LoadOptions.SetLineInfo);
        var body   = root.Descendants(TextNs + "text").FirstOrDefault();
        var blocks = new List<DocumentBlock>();

        if (body is null) return blocks;

        foreach (var child in body.Elements())
        {
            ct.ThrowIfCancellationRequested();
            var block = MapElement(child, entryBaseOffset, mapBuilder);
            if (block is not null) blocks.Add(block);
        }

        return blocks;
    }

    // ── Element mapping ─────────────────────────────────────────────────────

    private static DocumentBlock? MapElement(
        XElement         elem,
        long             baseOffset,
        BinaryMapBuilder mapBuilder)
    {
        string local = elem.Name.LocalName;
        XNamespace ns = elem.Name.Namespace;

        if (ns == TextNs && local == "p")
            return MapParagraph(elem, baseOffset, mapBuilder, "paragraph");

        if (ns == TextNs && local == "h")
            return MapParagraph(elem, baseOffset, mapBuilder, "heading");

        if (ns == TextNs && local is "list" or "list-item")
            return MapList(elem, baseOffset, mapBuilder);

        if (ns == TableNs && local == "table")
            return MapTable(elem, baseOffset, mapBuilder);

        if (ns == DrawNs && local is "frame" or "image")
            return MapImage(elem, baseOffset, mapBuilder);

        return null;
    }

    private static DocumentBlock MapParagraph(
        XElement         pElem,
        long             baseOffset,
        BinaryMapBuilder mapBuilder,
        string           kind)
    {
        long off = ResolveOffset(pElem, baseOffset);
        int  len = EstimateLength(pElem);

        var para = new DocumentBlock
        {
            Kind      = kind,
            RawOffset = off,
            RawLength = len,
            Text      = string.Empty
        };

        var styleAttr = pElem.Attribute(TextNs + "style-name") ??
                        pElem.Attribute(StyleNs + "style-name");
        if (styleAttr is not null)
            para.Attributes["style"] = styleAttr.Value;

        var outlineLvl = pElem.Attribute(TextNs + "outline-level");
        if (outlineLvl is not null)
            para.Attributes["level"] = outlineLvl.Value;

        var sb = new System.Text.StringBuilder();

        foreach (var child in pElem.Elements())
        {
            if (child.Name.Namespace == TextNs && child.Name.LocalName == "span")
            {
                var run = MapRun(child, baseOffset, mapBuilder);
                para.Children.Add(run);
                sb.Append(run.Text);
            }
            else if (child.Name.Namespace == TextNs && child.Name.LocalName == "s")
            {
                int count = int.TryParse(
                    child.Attribute(TextNs + "c")?.Value, out int c) ? c : 1;
                sb.Append(' ', count);
            }
            else if (child.Name.Namespace == TextNs && child.Name.LocalName == "tab")
            {
                sb.Append('\t');
            }
            else if (child.Name.Namespace == DrawNs)
            {
                var imgBlock = MapImage(child, baseOffset, mapBuilder);
                para.Children.Add(imgBlock);
            }
        }

        // Plain text nodes directly inside <text:p>
        foreach (var tn in pElem.Nodes().OfType<XText>())
            sb.Append(tn.Value);

        para.Text = sb.ToString();
        mapBuilder.AddZipRelative("content.xml", para, off, len);
        return para;
    }

    private static DocumentBlock MapRun(
        XElement         rElem,
        long             baseOffset,
        BinaryMapBuilder mapBuilder)
    {
        long off = ResolveOffset(rElem, baseOffset);
        int  len = EstimateLength(rElem);

        var run = new DocumentBlock
        {
            Kind      = "run",
            RawOffset = off,
            RawLength = len,
            Text      = rElem.Value
        };

        var style = rElem.Attribute(TextNs + "style-name");
        if (style is not null) run.Attributes["style"] = style.Value;

        mapBuilder.AddZipRelative("content.xml", run, off, len);
        return run;
    }

    private static DocumentBlock MapList(
        XElement         listElem,
        long             baseOffset,
        BinaryMapBuilder mapBuilder)
    {
        long off = ResolveOffset(listElem, baseOffset);
        int  len = EstimateLength(listElem);

        var list = new DocumentBlock
        {
            Kind      = "list",
            RawOffset = off,
            RawLength = len,
            Text      = string.Empty
        };

        foreach (var item in listElem.Elements(TextNs + "list-item"))
        {
            foreach (var p in item.Elements())
            {
                var b = MapElement(p, baseOffset, mapBuilder);
                if (b is not null) list.Children.Add(b);
            }
        }

        mapBuilder.AddZipRelative("content.xml", list, off, len);
        return list;
    }

    private static DocumentBlock MapTable(
        XElement         tblElem,
        long             baseOffset,
        BinaryMapBuilder mapBuilder)
    {
        long off = ResolveOffset(tblElem, baseOffset);
        int  len = EstimateLength(tblElem);

        var table = new DocumentBlock
        {
            Kind      = "table",
            RawOffset = off,
            RawLength = len,
            Text      = "[table]"
        };

        foreach (var rowElem in tblElem.Descendants(TableNs + "table-row"))
        {
            var row = new DocumentBlock
            {
                Kind      = "table-row",
                RawOffset = ResolveOffset(rowElem, baseOffset),
                RawLength = EstimateLength(rowElem),
                Text      = string.Empty
            };
            foreach (var cellElem in rowElem.Elements(TableNs + "table-cell"))
            {
                foreach (var p in cellElem.Elements(TextNs + "p"))
                {
                    var b = MapParagraph(p, baseOffset, mapBuilder, "paragraph");
                    row.Children.Add(b);
                }
            }
            table.Children.Add(row);
        }

        mapBuilder.AddZipRelative("content.xml", table, off, len);
        return table;
    }

    private static DocumentBlock MapImage(
        XElement         elem,
        long             baseOffset,
        BinaryMapBuilder mapBuilder)
    {
        long off = ResolveOffset(elem, baseOffset);
        int  len = EstimateLength(elem);

        var img = new DocumentBlock
        {
            Kind      = "image",
            RawOffset = off,
            RawLength = len,
            Text      = "[image]"
        };

        mapBuilder.AddZipRelative("content.xml", img, off, len);
        return img;
    }

    // ── Offset / length helpers ──────────────────────────────────────────────

    private static long ResolveOffset(XElement elem, long baseOffset)
    {
        if (elem is System.Xml.IXmlLineInfo li && li.HasLineInfo())
            return (long)li.LineNumber * 1000 + li.LinePosition;
        return baseOffset;
    }

    private static int EstimateLength(XElement elem) =>
        System.Text.Encoding.UTF8.GetByteCount(elem.ToString());
}
