// ==========================================================
// Project: WpfHexEditor.Plugins.DocumentLoader.Rtf
// File: Parsers/RtfStructureBuilder.cs
// Description:
//     Consumes an RtfTokenizer stream and builds a DocumentBlock tree.
//     Each block carries the absolute stream offsets of its source tokens
//     so the BinaryMap can be populated accurately.
// ==========================================================

using WpfHexEditor.Editor.DocumentEditor.Core.BinaryMap;
using WpfHexEditor.Editor.DocumentEditor.Core.Model;

namespace WpfHexEditor.Plugins.DocumentLoader.Rtf.Parsers;

/// <summary>
/// Transforms a token stream into a <see cref="DocumentBlock"/> tree
/// and fills a <see cref="BinaryMapBuilder"/> with per-block offsets.
/// </summary>
internal sealed class RtfStructureBuilder
{
    // ── Public entry point ──────────────────────────────────────────────────

    /// <summary>
    /// Walk the entire token stream, build the block tree, and record
    /// binary-map entries.
    /// </summary>
    public (List<DocumentBlock> Roots, DocumentMetadata Metadata) Build(
        RtfTokenizer tokenizer,
        BinaryMapBuilder mapBuilder,
        CancellationToken ct = default)
    {
        var roots    = new List<DocumentBlock>();
        var metadata = new DocumentMetadata { MimeType = "application/rtf" };

        // RTF is always one outer group: { \rtf1 … }
        // We push a synthetic "root" context and let the group walker fill it.
        var rootCtx = new GroupContext(null, 0);
        WalkGroup(tokenizer, rootCtx, mapBuilder, metadata, ct);

        foreach (var b in rootCtx.Children)
            roots.Add(b);

        return (roots, metadata);
    }

    // ── Group walking ───────────────────────────────────────────────────────

    private static void WalkGroup(
        RtfTokenizer   tokenizer,
        GroupContext   ctx,
        BinaryMapBuilder mapBuilder,
        DocumentMetadata metadata,
        CancellationToken ct)
    {
        // Current paragraph / run accumulation
        DocumentBlock? currentParagraph = null;
        DocumentBlock? currentRun       = null;
        long           runStart         = -1;

        void FlushRun()
        {
            if (currentRun is null) return;
            int len = (int)(currentRun.RawOffset + currentRun.RawLength); // placeholder
            currentParagraph?.Children.Add(currentRun);
            mapBuilder.Add(currentRun, currentRun.RawOffset, currentRun.RawLength);
            currentRun = null;
        }

        void FlushParagraph()
        {
            FlushRun();
            if (currentParagraph is null) return;
            ctx.Children.Add(currentParagraph);
            mapBuilder.Add(currentParagraph, currentParagraph.RawOffset, currentParagraph.RawLength);
            currentParagraph = null;
        }

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var tok = tokenizer.NextToken(ct);

            switch (tok.Kind)
            {
                case RtfTokenKind.EndOfStream:
                    FlushParagraph();
                    return;

                case RtfTokenKind.GroupOpen:
                {
                    FlushRun();
                    var child = new GroupContext(ctx, tok.Offset);
                    WalkGroup(tokenizer, child, mapBuilder, metadata, ct);

                    // Promote recognised destination groups to proper blocks
                    var block = child.ToBlock();
                    if (block is not null)
                    {
                        currentParagraph ??= MakeParagraph(tok.Offset);
                        currentParagraph.Children.Add(block);
                        mapBuilder.Add(block, block.RawOffset, block.RawLength);
                    }
                    break;
                }

                case RtfTokenKind.GroupClose:
                    FlushParagraph();
                    ctx.CloseOffset = tok.Offset + tok.Length;
                    return;

                case RtfTokenKind.ControlWord:
                    HandleControlWord(tok, ctx, ref currentParagraph, ref currentRun,
                                      ref runStart, mapBuilder, metadata, FlushRun, FlushParagraph);
                    break;

                case RtfTokenKind.ControlSymbol:
                    AppendSymbolText(tok, ref currentParagraph, ref currentRun, ref runStart, tok.Offset);
                    break;

                case RtfTokenKind.Text:
                    if (!string.IsNullOrEmpty(tok.Text))
                    {
                        currentParagraph ??= MakeParagraph(tok.Offset);
                        currentRun       ??= MakeRun(tok.Offset);
                        runStart          = runStart < 0 ? tok.Offset : runStart;

                        // Accumulate text into run; update length lazily
                        currentRun.Text += tok.Text;
                        // Update raw length to cover this token
                        SetRawLength(ref currentRun, tok.Offset + tok.Length);
                    }
                    break;

                case RtfTokenKind.BinaryData:
                {
                    FlushRun();
                    currentParagraph ??= MakeParagraph(tok.Offset);
                    var imgBlock = new DocumentBlock
                    {
                        Kind      = "image",
                        RawOffset = tok.Offset,
                        RawLength = tok.Length,
                        Text      = $"[binary data {tok.Parameter} bytes]"
                    };
                    imgBlock.Attributes["binarySize"] = tok.Parameter;
                    if (tok.Binary is not null)
                        imgBlock.Attributes["binaryData"] = tok.Binary;

                    currentParagraph.Children.Add(imgBlock);
                    mapBuilder.Add(imgBlock, tok.Offset, tok.Length);
                    break;
                }
            }
        }
    }

    // ── Control word dispatch ───────────────────────────────────────────────

    private static void HandleControlWord(
        RtfToken         tok,
        GroupContext     ctx,
        ref DocumentBlock? currentParagraph,
        ref DocumentBlock? currentRun,
        ref long           runStart,
        BinaryMapBuilder   mapBuilder,
        DocumentMetadata   metadata,
        Action             flushRun,
        Action             flushParagraph)
    {
        switch (tok.Word)
        {
            case "par":
            case "pard":
                flushParagraph();
                break;

            case "line":
                if (currentRun is not null)
                    currentRun.Text += "\n";
                break;

            case "tab":
                if (currentRun is not null)
                    currentRun.Text += "\t";
                break;

            // Character formatting — stored as run attributes
            case "b":
                EnsureRun(ref currentParagraph, ref currentRun, tok.Offset);
                currentRun!.Attributes["bold"] = tok.Parameter != 0;
                break;

            case "i":
                EnsureRun(ref currentParagraph, ref currentRun, tok.Offset);
                currentRun!.Attributes["italic"] = tok.Parameter != 0;
                break;

            case "ul":
                EnsureRun(ref currentParagraph, ref currentRun, tok.Offset);
                currentRun!.Attributes["underline"] = tok.Parameter != 0;
                break;

            case "fs":
                EnsureRun(ref currentParagraph, ref currentRun, tok.Offset);
                currentRun!.Attributes["fontSize"] = tok.Parameter;
                break;

            case "cf":
                EnsureRun(ref currentParagraph, ref currentRun, tok.Offset);
                currentRun!.Attributes["colorIndex"] = tok.Parameter;
                break;

            // Destination markers — mark current group context
            case "fonttbl":
                ctx.Destination = RtfDestination.FontTable;
                break;

            case "colortbl":
                ctx.Destination = RtfDestination.ColorTable;
                break;

            case "stylesheet":
                ctx.Destination = RtfDestination.StyleSheet;
                break;

            case "info":
                ctx.Destination = RtfDestination.Info;
                break;

            case "author":
                if (ctx.Destination == RtfDestination.Info)
                    ctx.PendingMetaKey = "author";
                break;

            case "title":
                if (ctx.Destination == RtfDestination.Info)
                    ctx.PendingMetaKey = "title";
                break;

            case "pict":
                ctx.Destination = RtfDestination.Picture;
                break;

            case "object":
                ctx.Destination = RtfDestination.Object;
                break;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static DocumentBlock MakeParagraph(long offset) =>
        new() { Kind = "paragraph", RawOffset = offset, RawLength = 0 };

    private static DocumentBlock MakeRun(long offset) =>
        new() { Kind = "run", RawOffset = offset, RawLength = 0 };

    private static void EnsureRun(ref DocumentBlock? para, ref DocumentBlock? run, long offset)
    {
        para ??= MakeParagraph(offset);
        run  ??= MakeRun(offset);
    }

    /// <summary>Updates RawLength so the block spans up to <paramref name="endOffset"/>.</summary>
    private static void SetRawLength(ref DocumentBlock? block, long endOffset)
    {
        if (block is null) return;
        int newLen = (int)(endOffset - block.RawOffset);
        if (newLen > block.RawLength)
        {
            // DocumentBlock.RawLength is init-only; replace with a new instance
            block = new DocumentBlock
            {
                Kind      = block.Kind,
                Text      = block.Text,
                RawOffset = block.RawOffset,
                RawLength = newLen
            };
            // Copy attributes / children
            foreach (var kv in ((Dictionary<string, object>)block.Attributes))
                block.Attributes[kv.Key] = kv.Value;
            foreach (var c in block.Children)
                block.Children.Add(c);
        }
    }

    private static void AppendSymbolText(
        RtfToken         tok,
        ref DocumentBlock? para,
        ref DocumentBlock? run,
        ref long           runStart,
        long               offset)
    {
        string text = tok.Word switch
        {
            "\\" => "\\",
            "{"  => "{",
            "}"  => "}",
            "~"  => "\u00A0",   // non-breaking space
            "-"  => "\u00AD",   // soft hyphen
            _    => string.Empty
        };
        if (text.Length == 0) return;

        para     ??= MakeParagraph(offset);
        run      ??= MakeRun(offset);
        runStart   = runStart < 0 ? offset : runStart;
        run.Text  += text;
        SetRawLength(ref run, offset + tok.Length);
    }
}

// ── Internal group context ──────────────────────────────────────────────────

internal enum RtfDestination
{
    None, FontTable, ColorTable, StyleSheet, Info, Picture, Object, Header, Footer
}

internal sealed class GroupContext(GroupContext? parent, long openOffset)
{
    public GroupContext?     Parent       { get; } = parent;
    public long              OpenOffset   { get; } = openOffset;
    public long              CloseOffset  { get; set; }
    public RtfDestination    Destination  { get; set; }
    public string?           PendingMetaKey { get; set; }
    public List<DocumentBlock> Children  { get; } = [];

    /// <summary>
    /// Converts recognised destinations to a <see cref="DocumentBlock"/>.
    /// Returns <see langword="null"/> for invisible destinations.
    /// </summary>
    public DocumentBlock? ToBlock()
    {
        return Destination switch
        {
            RtfDestination.Picture =>
                new DocumentBlock
                {
                    Kind      = "image",
                    RawOffset = OpenOffset,
                    RawLength = (int)(CloseOffset - OpenOffset),
                    Text      = "[picture]"
                },
            RtfDestination.Object =>
                new DocumentBlock
                {
                    Kind      = "object",
                    RawOffset = OpenOffset,
                    RawLength = (int)(CloseOffset - OpenOffset),
                    Text      = "[OLE object — offset only]"
                },
            // FontTable, ColorTable, StyleSheet, Info are metadata — not content blocks
            _ => null
        };
    }
}
