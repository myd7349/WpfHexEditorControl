using WpfHexEditor.Core.Bytes;
using Xunit;
using static WpfHexEditor.Core.ByteProvider.Tests.ByteProviderTestHelpers;

namespace WpfHexEditor.Core.ByteProvider.Tests;

public class ModifyTests
{
    [Fact]
    public void ModifyByte_ChangesValue()
    {
        using var p = FromBytes(1, 2, 3);
        p.ModifyByte(1, 0xFF);
        var (val, _) = p.GetByte(1);
        Assert.Equal(0xFF, val);
    }

    [Fact]
    public void ModifyByte_ReadOnly_Throws()
    {
        var p = new Bytes.ByteProvider();
        p.OpenMemory(new byte[] { 1, 2, 3 }, readOnly: true);
        Assert.Throws<InvalidOperationException>(() => p.ModifyByte(0, 0xFF));
        p.Dispose();
    }

    [Fact]
    public void ModifyByte_WithByteModifiedSubscriber_FiresEvent()
    {
        using var p = FromBytes(1, 2, 3);
        ByteModifiedEventArgs? args = null;
        p.ByteModified += (_, e) => args = e;

        p.ModifyByte(0, 0xAA);

        Assert.NotNull(args);
        Assert.Equal(0, args!.VirtualPosition);
        Assert.Equal(0xAA, args.NewValue);
        Assert.True(args.IsSingleByte);
    }

    [Fact]
    public void ModifyBytes_ChangesMultipleValues()
    {
        using var p = FromBytes(1, 2, 3, 4, 5);
        p.ModifyBytes(1, new byte[] { 0xAA, 0xBB, 0xCC });
        Assert.Equal(new byte[] { 1, 0xAA, 0xBB, 0xCC, 5 }, p.GetBytes(0, 5));
    }

    [Fact]
    public void ModifyBytes_WithSubscriber_FiresEvent()
    {
        using var p = FromBytes(1, 2, 3);
        ByteModifiedEventArgs? args = null;
        p.ByteModified += (_, e) => args = e;

        p.ModifyBytes(0, new byte[] { 0xAA, 0xBB, 0xCC });

        Assert.NotNull(args);
        Assert.False(args!.IsSingleByte);
        Assert.Equal(3, args.NewValues!.Length);
    }

    [Fact]
    public void ModifyByte_NoUndoNoSubscriber_StillModifies()
    {
        // Even without undo/event, the byte must change.
        using var p = new Bytes.ByteProvider(ByteProviderOptions.Default.WithMaxUndoDepth(0));
        p.OpenMemory(new byte[] { 1, 2, 3 });
        p.ModifyByte(2, 0x99);
        var (val, _) = p.GetByte(2);
        Assert.Equal(0x99, val);
    }

    [Fact]
    public void ModifyBytes_ReadOnly_Throws()
    {
        var p = new Bytes.ByteProvider();
        p.OpenMemory(new byte[] { 1, 2, 3 }, readOnly: true);
        Assert.Throws<InvalidOperationException>(() => p.ModifyBytes(0, new byte[] { 0xFF }));
        p.Dispose();
    }
}
