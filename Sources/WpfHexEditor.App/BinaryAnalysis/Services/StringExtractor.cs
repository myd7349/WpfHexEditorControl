//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.Text;

namespace WpfHexEditor.App.BinaryAnalysis.Services;

/// <summary>Scan result for one extracted string run.</summary>
public sealed record StringRun(long Offset, int Length, StringEncoding Encoding, string Value);

public enum StringEncoding { Ascii, Utf16Le }

/// <summary>Stateless service: extracts printable string runs from a byte buffer.</summary>
public static class StringExtractor
{
    private const byte MinPrintable = 0x20;
    private const byte MaxPrintable = 0x7E;

    /// <summary>
    /// Scans <paramref name="data"/> for consecutive printable ASCII and UTF-16LE runs
    /// of at least <paramref name="minLength"/> characters.
    /// </summary>
    public static List<StringRun> Extract(ReadOnlySpan<byte> data, int minLength = 4)
    {
        var results = new List<StringRun>();
        ExtractAscii(data, minLength, results);
        ExtractUtf16Le(data, minLength, results);
        results.Sort(static (a, b) => a.Offset.CompareTo(b.Offset));
        return results;
    }

    private static void ExtractAscii(ReadOnlySpan<byte> data, int minLength, List<StringRun> results)
    {
        int start = -1;
        for (int i = 0; i <= data.Length; i++)
        {
            bool printable = i < data.Length && data[i] >= MinPrintable && data[i] <= MaxPrintable;
            if (printable)
            {
                if (start < 0) start = i;
            }
            else
            {
                if (start >= 0)
                {
                    int len = i - start;
                    if (len >= minLength)
                        results.Add(new StringRun(start, len, StringEncoding.Ascii,
                            Encoding.ASCII.GetString(data.Slice(start, len))));
                    start = -1;
                }
            }
        }
    }

    private static void ExtractUtf16Le(ReadOnlySpan<byte> data, int minLength, List<StringRun> results)
    {
        int start = -1;
        int charCount = 0;
        for (int i = 0; i + 1 < data.Length; i += 2)
        {
            ushort ch = (ushort)(data[i] | (data[i + 1] << 8));
            bool printable = ch >= MinPrintable && ch <= MaxPrintable;
            if (printable)
            {
                if (start < 0) { start = i; charCount = 0; }
                charCount++;
            }
            else
            {
                if (start >= 0 && charCount >= minLength)
                {
                    int byteLen = charCount * 2;
                    results.Add(new StringRun(start, byteLen, StringEncoding.Utf16Le,
                        Encoding.Unicode.GetString(data.Slice(start, byteLen))));
                }
                start = -1;
            }
        }
        if (start >= 0 && charCount >= minLength)
        {
            int byteLen = charCount * 2;
            results.Add(new StringRun(start, byteLen, StringEncoding.Utf16Le,
                Encoding.Unicode.GetString(data.Slice(start, byteLen))));
        }
    }
}
