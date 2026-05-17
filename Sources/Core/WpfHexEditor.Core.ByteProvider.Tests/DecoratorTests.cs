using WpfHexEditor.Core.Bytes;
using WpfHexEditor.Core.Decorators;
using WpfHexEditor.Core.Interfaces;
using Xunit;
using static WpfHexEditor.Core.ByteProvider.Tests.ByteProviderTestHelpers;

namespace WpfHexEditor.Core.ByteProvider.Tests;

internal sealed class PassThroughDecorator : ByteProviderDecorator
{
    public PassThroughDecorator(IByteProvider inner) : base(inner) { }
}

public class DecoratorTests
{
    [Fact]
    public void Decorator_ForwardsGetByte_ToInner()
    {
        using var inner = FromBytes(0xAA, 0xBB);
        using var decorator = new PassThroughDecorator(inner);

        var (val, ok) = decorator.GetByte(1);
        Assert.True(ok);
        Assert.Equal(0xBB, val);
    }

    [Fact]
    public void Decorator_ByteModifiedEvent_Forwarded()
    {
        using var inner = FromBytes(1, 2, 3);
        using var decorator = new PassThroughDecorator(inner);

        ByteModifiedEventArgs? received = null;
        decorator.ByteModified += (_, e) => received = e;

        inner.ModifyByte(0, 0xFF);

        Assert.NotNull(received);
        Assert.Equal(0xFF, received!.NewValue);
    }

    [Fact]
    public void Decorator_Dispose_UnsubscribesFromInner()
    {
        using var inner = FromBytes(1, 2, 3);
        int callCount = 0;

        var decorator = new PassThroughDecorator(inner);
        decorator.ByteModified += (_, _) => callCount++;
        decorator.Dispose();

        inner.ModifyByte(0, 0xFF);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Decorator_Dispose_DoesNotDisposeInner()
    {
        using var inner = FromBytes(1, 2, 3);
        var decorator = new PassThroughDecorator(inner);
        decorator.Dispose();
        Assert.True(inner.IsOpen);
    }

    [Fact]
    public void Decorator_ForwardsVirtualLength()
    {
        using var inner = FromBytes(1, 2, 3, 4, 5);
        using var decorator = new PassThroughDecorator(inner);
        Assert.Equal(5, decorator.VirtualLength);
    }

    [Fact]
    public void Decorator_ModifyByte_PropagatesToInner()
    {
        using var inner = FromBytes(1, 2, 3);
        using var decorator = new PassThroughDecorator(inner);

        decorator.ModifyByte(0, 0xAA);

        var (val, _) = inner.GetByte(0);
        Assert.Equal(0xAA, val);
    }
}
