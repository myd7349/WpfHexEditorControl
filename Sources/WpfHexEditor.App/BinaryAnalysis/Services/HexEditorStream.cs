//////////////////////////////////////////////
// GNU Affero General Public License v3.0 - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.6
//////////////////////////////////////////////

using System.IO;
using WpfHexEditor.SDK.Contracts.Services;

namespace WpfHexEditor.App.BinaryAnalysis.Services;

/// <summary>
/// Read-only <see cref="Stream"/> adapter over <see cref="IHexEditorService"/>.
/// Allows bulk-read services (hash, frequency, carver) to treat the active hex file
/// as a plain stream without bypassing the ByteProvider virtual-position layer.
/// </summary>
public sealed class HexEditorStream : Stream
{
    private readonly IHexEditorService _hex;
    private long _position;

    // ReadBytes is capped internally; we pick a chunk size well under any limit.
    private const int ChunkSize = 65_536;

    public HexEditorStream(IHexEditorService hex)
    {
        ArgumentNullException.ThrowIfNull(hex);
        _hex = hex;
    }

    public override bool CanRead  => true;
    public override bool CanSeek  => true;
    public override bool CanWrite => false;
    public override long Length   => _hex.FileSize;

    public override long Position
    {
        get => _position;
        set => _position = Math.Clamp(value, 0, _hex.FileSize);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        long remaining = _hex.FileSize - _position;
        if (remaining <= 0) return 0;

        int toRead = (int)Math.Min(count, Math.Min(remaining, ChunkSize));
        var chunk  = _hex.ReadBytes(_position, toRead);
        chunk.CopyTo(buffer, offset);
        _position += chunk.Length;
        return chunk.Length;
    }

    public override long Seek(long offset, SeekOrigin origin) => Position = origin switch
    {
        SeekOrigin.Begin   => offset,
        SeekOrigin.Current => _position + offset,
        SeekOrigin.End     => _hex.FileSize + offset,
        _                  => throw new ArgumentOutOfRangeException(nameof(origin)),
    };

    public override void Flush() { }
    public override void SetLength(long value)  => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
