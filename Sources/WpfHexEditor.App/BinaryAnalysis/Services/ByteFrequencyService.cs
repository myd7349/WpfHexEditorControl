//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.IO;
using System.Text;

namespace WpfHexEditor.App.BinaryAnalysis.Services;

public sealed record FrequencyResult(int[] Counts, double Entropy, long TotalBytes);

/// <summary>Computes byte frequency histogram and Shannon entropy over a stream.</summary>
public static class ByteFrequencyService
{
    private const int BufferSize = 81_920;

    public static async Task<FrequencyResult> AnalyzeAsync(
        Stream stream,
        IProgress<long>? progress = null,
        CancellationToken ct = default)
    {
        var counts = new int[256];
        var buffer = new byte[BufferSize];
        stream.Position = 0;
        long total = 0;

        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(), ct)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            for (int i = 0; i < read; i++)
                counts[buffer[i]]++;
            total += read;
            progress?.Report(total);
        }

        double entropy = ComputeEntropy(counts, total);
        return new FrequencyResult(counts, entropy, total);
    }

    private static double ComputeEntropy(int[] counts, long total)
    {
        if (total == 0) return 0.0;
        double h = 0.0;
        foreach (int c in counts)
        {
            if (c == 0) continue;
            double p = (double)c / total;
            h -= p * Math.Log2(p);
        }
        return h;
    }

    /// <summary>Exports frequency data as CSV: Byte,Count,Percent</summary>
    public static string ToCsv(FrequencyResult result)
    {
        var sb = new StringBuilder("Byte,Count,Percent\r\n", 256 * 30);
        for (int i = 0; i < 256; i++)
        {
            double pct = result.TotalBytes > 0
                ? (double)result.Counts[i] / result.TotalBytes * 100.0
                : 0.0;
            sb.Append($"0x{i:X2},{result.Counts[i]},{pct:F4}\r\n");
        }
        return sb.ToString();
    }
}
