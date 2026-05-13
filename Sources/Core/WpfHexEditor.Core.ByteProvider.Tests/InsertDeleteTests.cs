using WpfHexEditor.Core.Bytes;
using Xunit;
using static WpfHexEditor.Core.ByteProvider.Tests.ByteProviderTestHelpers;

namespace WpfHexEditor.Core.ByteProvider.Tests;

public class InsertDeleteTests
{
    [Fact]
    public void InsertBytes_IncreasesVirtualLength()
    {
        using var p = FromBytes(1, 2, 3);
        p.InsertBytes(1, new byte[] { 0xAA, 0xBB });
        Assert.Equal(5, p.VirtualLength);
    }

    [Fact]
    public void InsertByte_ShiftsRemainingBytes()
    {
        using var p = FromBytes(1, 2, 3);
        p.InsertByte(1, 0xFF);
        Assert.Equal(4, p.VirtualLength);
        var (val, _) = p.GetByte(1);
        Assert.Equal(0xFF, val);
        var (next, _) = p.GetByte(2);
        Assert.Equal(2, next);
    }

    [Fact]
    public void InsertBytes_FiresEvent()
    {
        using var p = FromBytes(1, 2, 3);
        BytesInsertedEventArgs? args = null;
        p.BytesInserted += (_, e) => args = e;

        p.InsertBytes(0, new byte[] { 0xAA });

        Assert.NotNull(args);
        Assert.Equal(0, args!.VirtualPosition);
        Assert.Single(args.InsertedBytes);
    }

    [Fact]
    public void DeleteByte_DecreasesVirtualLength()
    {
        using var p = FromBytes(1, 2, 3);
        p.DeleteByte(1);
        Assert.Equal(2, p.VirtualLength);
    }

    [Fact]
    public void DeleteByte_FiresEvent()
    {
        using var p = FromBytes(1, 2, 3);
        BytesDeletedEventArgs? args = null;
        p.BytesDeleted += (_, e) => args = e;

        p.DeleteByte(0);

        Assert.NotNull(args);
        Assert.Equal(1, args!.Count);
    }

    [Fact]
    public void DeleteBytes_Range_CorrectCount()
    {
        using var p = FromBytes(1, 2, 3, 4, 5);
        p.DeleteBytes(1, 3);
        Assert.Equal(2, p.VirtualLength);
        var (first, _) = p.GetByte(0);
        var (second, _) = p.GetByte(1);
        Assert.Equal(1, first);
        Assert.Equal(5, second);
    }

    [Fact]
    public void DeleteByte_ReadOnly_Throws()
    {
        var p = new Bytes.ByteProvider();
        p.OpenMemory(new byte[] { 1, 2, 3 }, readOnly: true);
        Assert.Throws<InvalidOperationException>(() => p.DeleteByte(0));
        p.Dispose();
    }
}
