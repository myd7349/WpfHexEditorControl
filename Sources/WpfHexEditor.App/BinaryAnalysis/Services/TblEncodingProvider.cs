//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using WpfHexEditor.Core.Bytes;
using WpfHexEditor.Core.CharacterTable;

namespace WpfHexEditor.App.BinaryAnalysis.Services;

/// <summary>
/// Adapts a <see cref="TblStream"/> to <see cref="ITblDecodeTable"/> for string extraction.
/// Uses the same greedy multi-byte matching strategy as <c>TblStream.ToTblString()</c>:
/// longest entry wins, DTE/MTE are honoured, unmapped bytes break the current run.
/// </summary>
public sealed class TblDecodeTableAdapter : ITblDecodeTable
{
    // Pre-built lookup: hex-key (uppercase) → mapped text.
    // Only entries whose Value is non-empty and non-control are included.
    private readonly Dictionary<string, string> _map;

    // Maximum key length in bytes (drives greedy scan depth).
    private readonly int _maxKeyBytes;

    public TblDecodeTableAdapter(TblStream tbl)
    {
        _map = new Dictionary<string, string>(tbl.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in tbl.GetAllEntries())
        {
            // Skip control / end-of-line / end-of-block entries — they terminate a string run.
            if (entry.Type is DteType.EndBlock or DteType.EndLine or DteType.Invalid) continue;
            if (string.IsNullOrEmpty(entry.Value)) continue;

            // Key length must be even (valid hex pairs) and > 0.
            int keyLen = entry.Entry.Length;
            if (keyLen == 0 || keyLen % 2 != 0) continue;

            _map[entry.Entry.ToUpperInvariant()] = entry.Value;

            int byteLen = keyLen / 2;
            if (byteLen > _maxKeyBytes) _maxKeyBytes = byteLen;
        }

        if (_maxKeyBytes == 0) _maxKeyBytes = 1;
    }

    /// <inheritdoc/>
    public bool TryMatch(ReadOnlySpan<byte> data, int offset, out int bytesConsumed, out string text)
    {
        // Greedy: try longest possible key first, shrink until match or fail.
        int maxLen = Math.Min(_maxKeyBytes, data.Length - offset);

        for (int len = maxLen; len >= 1; len--)
        {
            // Build uppercase hex key for these `len` bytes.
            var key = BuildHexKey(data, offset, len);
            if (_map.TryGetValue(key, out string? mapped))
            {
                bytesConsumed = len;
                text          = mapped;
                return true;
            }
        }

        bytesConsumed = 0;
        text          = string.Empty;
        return false;
    }

    private static string BuildHexKey(ReadOnlySpan<byte> data, int offset, int len)
    {
        // Pre-allocate exact size (2 hex chars per byte).
        var chars = new char[len * 2];
        for (int i = 0; i < len; i++)
        {
            byte b  = data[offset + i];
            chars[i * 2]     = ByteConverters.ByteToHex(b)[0];
            chars[i * 2 + 1] = ByteConverters.ByteToHex(b)[1];
        }
        return new string(chars);
    }

    /// <summary>Number of mapped entries in the adapter.</summary>
    public int MappedCount => _map.Count;
}
