using System.IO;
using WpfHexEditor.Core.Factories;
using WpfHexEditor.Core.Interfaces;
using Xunit;

namespace WpfHexEditor.Core.ByteProvider.Tests;

public class FactoryTests
{
    private readonly IByteProviderFactory _factory = new ByteProviderFactory();

    [Fact]
    public void CreateFromMemory_ReturnsOpenProvider()
    {
        using var p = _factory.CreateFromMemory(new byte[] { 1, 2, 3 });
        Assert.True(p.IsOpen);
        Assert.Equal(3, p.VirtualLength);
    }

    [Fact]
    public void CreateFromMemory_ReadOnly_IsReadOnly()
    {
        using var p = _factory.CreateFromMemory(new byte[] { 1, 2, 3 }, readOnly: true);
        Assert.True(p.IsReadOnly);
    }

    [Fact]
    public void CreateFromStream_ReturnsOpenProvider()
    {
        var stream = new MemoryStream(new byte[] { 10, 20, 30 });
        using var p = _factory.CreateFromStream(stream);
        Assert.True(p.IsOpen);
        Assert.Equal(3, p.VirtualLength);
    }

    [Fact]
    public void CreateFromMemory_WithOptions_HonorsUndoDepth()
    {
        var opts = ByteProviderOptions.Default.WithMaxUndoDepth(5);
        using var p = _factory.CreateFromMemory(new byte[] { 1, 2, 3 }, options: opts);
        Assert.Equal(5, ((Bytes.ByteProvider)p).Options.MaxUndoDepth);
    }

    [Fact]
    public void CreateFromMemory_ReturnsIByteProvider()
    {
        using var p = _factory.CreateFromMemory(new byte[] { 1 });
        Assert.IsAssignableFrom<IByteProvider>(p);
    }
}
