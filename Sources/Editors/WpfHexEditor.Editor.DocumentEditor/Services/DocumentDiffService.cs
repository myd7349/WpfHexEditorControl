// ==========================================================
// Project: WpfHexEditor.Editor.DocumentEditor
// File: Services/DocumentDiffService.cs
// Description:
//     Block-level structural diff between two DocumentModels.
//     Flattens each model's block tree to a (Kind, Text) signature
//     stream and runs an LCS pass to label each row as Equal /
//     Added / Removed / Modified.
// Architecture notes:
//     LCS-based (Myers-equivalent for our short-row case) — keeps
//     dependencies internal; we don't bind to Core.Diff's binary-
//     oriented APIs because semantics differ (no byte alignment
//     concept on the document side).
//     Modified rows are emitted when adjacent Added+Removed pairs
//     share the same Kind — surfaces "this paragraph was edited"
//     vs "this paragraph was deleted and a different one inserted".
// ==========================================================

using WpfHexEditor.Editor.DocumentEditor.Core.Model;

namespace WpfHexEditor.Editor.DocumentEditor.Services;

/// <summary>Computes a structural diff between two <see cref="DocumentModel"/>s.</summary>
public static class DocumentDiffService
{
    /// <summary>Returns one row per block-level change, in display order.</summary>
    public static IReadOnlyList<DocumentDiffRow> Diff(DocumentModel left, DocumentModel right)
    {
        if (left  is null) throw new ArgumentNullException(nameof(left));
        if (right is null) throw new ArgumentNullException(nameof(right));

        var leftRows  = Flatten(left.Blocks);
        var rightRows = Flatten(right.Blocks);
        return BuildRows(leftRows, rightRows);
    }

    // ── Flattening ─────────────────────────────────────────────────────────

    private static List<DocumentDiffSignature> Flatten(IEnumerable<DocumentBlock> blocks)
    {
        var sink = new List<DocumentDiffSignature>();
        Walk(blocks, sink);
        return sink;
    }

    private static void Walk(IEnumerable<DocumentBlock> blocks, List<DocumentDiffSignature> sink)
    {
        foreach (var b in blocks)
        {
            // Skip purely structural containers; surface only "addressable" rows.
            if (b.Kind is DocumentBlockKinds.Section) { Walk(b.Children, sink); continue; }

            string text = b.Children.Count == 0
                ? (b.Text ?? string.Empty)
                : Flatten(b);
            sink.Add(new DocumentDiffSignature(b.Kind, text.Trim(), b));

            // Walk children only for container kinds where children represent
            // sub-blocks the user thinks of separately (tables, lists).
            if (b.Kind is DocumentBlockKinds.Table or DocumentBlockKinds.List)
                Walk(b.Children, sink);
        }
    }

    private static string Flatten(DocumentBlock b)
    {
        var sb = new System.Text.StringBuilder();
        Append(sb, b);
        return sb.ToString();

        static void Append(System.Text.StringBuilder sb, DocumentBlock b)
        {
            if (b.Children.Count == 0) { sb.Append(b.Text); return; }
            foreach (var c in b.Children) Append(sb, c);
        }
    }

    // ── LCS table + walk ───────────────────────────────────────────────────

    private static IReadOnlyList<DocumentDiffRow> BuildRows(
        IReadOnlyList<DocumentDiffSignature> a,
        IReadOnlyList<DocumentDiffSignature> b)
    {
        int m = a.Count, n = b.Count;
        var lcs = new int[m + 1, n + 1];
        for (int i = m - 1; i >= 0; i--)
            for (int j = n - 1; j >= 0; j--)
                lcs[i, j] = a[i].Equals(b[j])
                    ? lcs[i + 1, j + 1] + 1
                    : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);

        var rows = new List<DocumentDiffRow>(Math.Max(m, n));
        int x = 0, y = 0;
        while (x < m && y < n)
        {
            if (a[x].Equals(b[y]))
            {
                rows.Add(new DocumentDiffRow(DocumentDiffKind.Equal, a[x].Block, b[y].Block, a[x].Text));
                x++; y++;
            }
            else if (lcs[x + 1, y] >= lcs[x, y + 1])
            {
                rows.Add(new DocumentDiffRow(DocumentDiffKind.Removed, a[x].Block, null, a[x].Text));
                x++;
            }
            else
            {
                rows.Add(new DocumentDiffRow(DocumentDiffKind.Added, null, b[y].Block, b[y].Text));
                y++;
            }
        }
        while (x < m) { rows.Add(new DocumentDiffRow(DocumentDiffKind.Removed, a[x].Block, null, a[x].Text)); x++; }
        while (y < n) { rows.Add(new DocumentDiffRow(DocumentDiffKind.Added,   null, b[y].Block, b[y].Text)); y++; }

        return MergeAdjacentModifications(rows);
    }

    /// <summary>
    /// Replaces adjacent Removed+Added pairs of the same Kind with a single
    /// Modified row — much friendlier display than "deleted P1 / added P1'".
    /// </summary>
    private static IReadOnlyList<DocumentDiffRow> MergeAdjacentModifications(List<DocumentDiffRow> input)
    {
        var output = new List<DocumentDiffRow>(input.Count);
        for (int i = 0; i < input.Count; i++)
        {
            var cur = input[i];
            if (i + 1 < input.Count &&
                cur.Kind == DocumentDiffKind.Removed &&
                input[i + 1].Kind == DocumentDiffKind.Added &&
                cur.Left is not null && input[i + 1].Right is not null &&
                cur.Left.Kind == input[i + 1].Right!.Kind)
            {
                output.Add(new DocumentDiffRow(
                    DocumentDiffKind.Modified, cur.Left, input[i + 1].Right,
                    $"{cur.Text}  →  {input[i + 1].Text}"));
                i++;
                continue;
            }
            output.Add(cur);
        }
        return output;
    }
}

/// <summary>Internal flattening signature: kind + normalized text + original block ref.</summary>
internal readonly record struct DocumentDiffSignature(string Kind, string Text, DocumentBlock Block)
{
    public bool Equals(DocumentDiffSignature other) =>
        Kind == other.Kind && Text == other.Text;

    public override int GetHashCode() => HashCode.Combine(Kind, Text);
}

public enum DocumentDiffKind { Equal, Added, Removed, Modified }

/// <summary>One row in a structural diff between two documents.</summary>
public sealed record DocumentDiffRow(
    DocumentDiffKind Kind,
    DocumentBlock?   Left,
    DocumentBlock?   Right,
    string           Text)
{
    /// <summary>Effective Kind label for UI (uses Left when present, otherwise Right).</summary>
    public string BlockKind => Left?.Kind ?? Right?.Kind ?? string.Empty;

    public string KindGlyph => Kind switch
    {
        DocumentDiffKind.Added    => "+",
        DocumentDiffKind.Removed  => "−",
        DocumentDiffKind.Modified => "~",
        _                         => " "
    };
}
