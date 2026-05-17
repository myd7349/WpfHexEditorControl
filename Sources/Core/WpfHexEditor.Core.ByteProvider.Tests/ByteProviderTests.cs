using System.IO;
using WpfHexEditor.Core.Bytes;
using Xunit;
using static WpfHexEditor.Core.ByteProvider.Tests.ByteProviderTestHelpers;

namespace WpfHexEditor.Core.ByteProvider.Tests;

public class ByteProviderTests
{
    [Fact]
    public void OpenMemory_IsOpen()
    {
        using var p = FromBytes(1, 2, 3);
        Assert.True(p.IsOpen);
    }

    [Fact]
    public void OpenMemory_VirtualLength_Correct()
    {
        using var p = FromBytes(1, 2, 3, 4, 5);
        Assert.Equal(5, p.VirtualLength);
    }

    [Fact]
    public void Close_AfterOpen_IsOpenFalse()
    {
        var p = FromBytes(1, 2, 3);
        p.Close();
        Assert.False(p.IsOpen);
    }

    [Fact]
    public void GetByte_ValidPosition_ReturnsCorrectValue()
    {
        using var p = FromBytes(0xAA, 0xBB, 0xCC);
        var (val, ok) = p.GetByte(1);
        Assert.True(ok);
        Assert.Equal(0xBB, val);
    }

    [Fact]
    public void GetByte_InvalidPosition_ReturnsFalse()
    {
        using var p = FromBytes(1, 2, 3);
        var (_, ok) = p.GetByte(100);
        Assert.False(ok);
    }

    [Fact]
    public void GetBytes_Range_CorrectContent()
    {
        using var p = FromBytes(10, 20, 30, 40, 50);
        var result = p.GetBytes(1, 3);
        Assert.Equal(new byte[] { 20, 30, 40 }, result);
    }

    [Fact]
    public void OpenStream_ReadsContent()
    {
        var stream = new MemoryStream(new byte[] { 7, 8, 9 });
        using var p = new Bytes.ByteProvider();
        p.OpenStream(stream);
        Assert.True(p.IsOpen);
        Assert.Equal(3, p.VirtualLength);
    }

    [Fact]
    public void HasChanges_InitiallyFalse()
    {
        using var p = FromBytes(1, 2, 3);
        Assert.False(p.HasChanges);
    }

    [Fact]
    public void HasChanges_TrueAfterModify()
    {
        using var p = FromBytes(1, 2, 3);
        p.ModifyByte(0, 0xFF);
        Assert.True(p.HasChanges);
    }

    [Fact]
    public void Options_DefaultNotNull()
    {
        using var p = new Bytes.ByteProvider();
        Assert.NotNull(p.Options);
    }
}
