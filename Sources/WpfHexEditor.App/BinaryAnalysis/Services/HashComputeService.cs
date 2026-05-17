//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.IO;
using System.Security.Cryptography;

namespace WpfHexEditor.App.BinaryAnalysis.Services;

public sealed record HashResult(string Algorithm, string HexDigest, long BytesHashed, TimeSpan Elapsed);

/// <summary>Computes multiple hash algorithms over a stream in a single pass per algorithm.</summary>
public static class HashComputeService
{
    private const int BufferSize = 81_920; // 80 KB

    /// <summary>
    /// Computes MD5, SHA1, SHA256, SHA512 over <paramref name="length"/> bytes of
    /// <paramref name="stream"/> starting at <paramref name="offset"/>.
    /// </summary>
    public static async Task<IReadOnlyList<HashResult>> ComputeAsync(
        Stream stream,
        long offset,
        long length,
        IProgress<long>? progress = null,
        CancellationToken ct = default)
    {
        using var md5    = MD5.Create();
        using var sha1   = SHA1.Create();
        using var sha256 = SHA256.Create();
        using var sha512 = SHA512.Create();

        var algos = new HashAlgorithm[] { md5, sha1, sha256, sha512 };
        var names = new[] { "MD5", "SHA1", "SHA256", "SHA512" };

        var buffer = new byte[BufferSize];
        stream.Position = offset;
        long remaining  = length;
        long done       = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (remaining > 0)
        {
            ct.ThrowIfCancellationRequested();
            int toRead = (int)Math.Min(remaining, buffer.Length);
            int read   = await stream.ReadAsync(buffer.AsMemory(0, toRead), ct);
            if (read == 0) break;

            foreach (var algo in algos)
                algo.TransformBlock(buffer, 0, read, null, 0);

            remaining -= read;
            done      += read;
            progress?.Report(done);
        }

        foreach (var algo in algos)
            algo.TransformFinalBlock([], 0, 0);

        sw.Stop();
        return names.Zip(algos, (name, algo) =>
            new HashResult(name, Convert.ToHexString(algo.Hash!).ToLowerInvariant(), length - remaining, sw.Elapsed))
            .ToList();
    }
}
