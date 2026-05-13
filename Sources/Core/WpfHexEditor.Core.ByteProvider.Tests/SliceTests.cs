using WpfHexEditor.Core.Bytes;
using WpfHexEditor.Core.Slices;
using Xunit;
using static WpfHexEditor.Core.ByteProvider.Tests.ByteProviderTestHelpers;

namespace WpfHexEditor.Core.ByteProvider.Tests;

public class SliceTests
{

    [Fact]
    public void ByteSlice_Indexer_ReturnsCorrectByte()
    {
        using var p = FromBytes(10, 20, 30, 40, 50);
        var slice = new ByteSlice(p, 1, 3);
        Assert.Equal(20, slice[0]);
        Assert.Equal(30, slice[1]);
        Assert.Equal(40, slice[2]);
    }

    [Fact]
    public void ByteSlice_Indexer_OutOfRange_Throws()
    {
        using var p = FromBytes(1, 2, 3);
        var slice = new ByteSlice(p, 0, 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = slice[5]);
    }

    [Fact]
    public void ByteSlice_ToArray_CorrectContent()
    {
        using var p = FromBytes(1, 2, 3, 4, 5);
        var slice = new ByteSlice(p, 1, 3);
        Assert.Equal(new byte[] { 2, 3, 4 }, slice.ToArray());
    }

    [Fact]
    public void ByteSlice_Slice_SubRange()
    {
        using var p = FromBytes(1, 2, 3, 4, 5);
        var slice = new ByteSlice(p, 0, 5);
        var sub = slice.Slice(2, 2);
        Assert.Equal(2, sub.Length);
        Assert.Equal(3, sub[0]);
        Assert.Equal(4, sub[1]);
    }

    [Fact]
    public void ByteSlice_CopyTo_FillsSpan()
    {
        using var p = FromBytes(10, 20, 30);
        var slice = new ByteSlice(p, 0, 3);
        Span<byte> buf = stackalloc byte[3];
        slice.CopyTo(buf);
        Assert.Equal(10, buf[0]);
        Assert.Equal(20, buf[1]);
        Assert.Equal(30, buf[2]);
    }

    [Fact]
    public void ByteSlice_CopyTo_TooSmall_Throws()
    {
        using var p = FromBytes(1, 2, 3);
        var slice = new ByteSlice(p, 0, 3);
        byte[] small = new byte[2];
        Assert.Throws<ArgumentException>(() => slice.CopyTo(small.AsSpan()));
    }

    [Fact]
    public void ByteSlice_SequenceEqual_SameContent_True()
    {
        using var a = FromBytes(1, 2, 3);
        using var b = FromBytes(1, 2, 3);
        var sa = new ByteSlice(a, 0, 3);
        var sb = new ByteSlice(b, 0, 3);
        Assert.True(sa.SequenceEqual(sb));
    }

    [Fact]
    public void ByteSlice_SequenceEqual_DifferentContent_False()
    {
        using var a = FromBytes(1, 2, 3);
        using var b = FromBytes(1, 2, 4);
        var sa = new ByteSlice(a, 0, 3);
        var sb = new ByteSlice(b, 0, 3);
        Assert.False(sa.SequenceEqual(sb));
    }

    [Fact]
    public void ByteSlice_SequenceEqual_DifferentLength_False()
    {
        using var a = FromBytes(1, 2, 3);
        using var b = FromBytes(1, 2);
        var sa = new ByteSlice(a, 0, 3);
        var sb = new ByteSlice(b, 0, 2);
        Assert.False(sa.SequenceEqual(sb));
    }

    [Fact]
    public void ByteSlice_IsEmpty_TrueForZeroLength()
    {
        using var p = FromBytes(1, 2, 3);
        var slice = new ByteSlice(p, 0, 0);
        Assert.True(slice.IsEmpty);
        Assert.Empty(slice.ToArray());
    }

    [Fact]
    public void ByteSlice_Equality_SameSourceStartLength()
    {
        using var p = FromBytes(1, 2, 3);
        var a = new ByteSlice(p, 1, 2);
        var b = new ByteSlice(p, 1, 2);
        Assert.Equal(a, b);
        Assert.True(a == b);
    }
}
