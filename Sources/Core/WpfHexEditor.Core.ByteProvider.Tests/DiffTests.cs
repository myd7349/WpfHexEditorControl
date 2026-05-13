using WpfHexEditor.Core.Bytes;
using WpfHexEditor.Core.Diff;
using Xunit;
using static WpfHexEditor.Core.ByteProvider.Tests.ByteProviderTestHelpers;

namespace WpfHexEditor.Core.ByteProvider.Tests;

public class DiffTests
{

    [Fact]
    public void Compare_IdenticalProviders_AllEqualChunks()
    {
        using var a = FromBytes(1, 2, 3, 4, 5);
        using var b = FromBytes(1, 2, 3, 4, 5);

        var diff = ByteProviderDiff.Compare(a, b);

        Assert.All(diff.Chunks, c => Assert.Equal(DiffKind.Equal, c.Kind));
    }

    [Fact]
    public void Compare_DifferentByte_DetectsModified()
    {
        using var a = FromBytes(1, 2, 3);
        using var b = FromBytes(1, 0xFF, 3);

        var diff = ByteProviderDiff.Compare(a, b);

        Assert.Contains(diff.Chunks, c => c.Kind == DiffKind.Modified);
    }

    [Fact]
    public void Compare_TargetLonger_DetectsInserted()
    {
        using var a = FromBytes(1, 2, 3);
        using var b = FromBytes(1, 2, 3, 4, 5);

        var diff = ByteProviderDiff.Compare(a, b);

        Assert.Contains(diff.Chunks, c => c.Kind == DiffKind.Inserted);
    }

    [Fact]
    public void Compare_SourceLonger_DetectsDeleted()
    {
        using var a = FromBytes(1, 2, 3, 4, 5);
        using var b = FromBytes(1, 2, 3);

        var diff = ByteProviderDiff.Compare(a, b);

        Assert.Contains(diff.Chunks, c => c.Kind == DiffKind.Deleted);
    }

    [Fact]
    public void ApplyDiff_ModifiedChunk_PatchesTarget()
    {
        using var source = FromBytes(1, 0xAA, 3);
        using var target = FromBytes(1, 2, 3);

        var diff = ByteProviderDiff.Compare(source, target);
        ByteProviderDiff.ApplyDiff(target, diff);

        var (val, _) = target.GetByte(1);
        Assert.Equal(0xAA, val);
    }

    [Fact]
    public void Compare_NullSource_Throws()
    {
        using var b = FromBytes(1, 2);
        Assert.Throws<ArgumentNullException>(() => ByteProviderDiff.Compare(null!, b));
    }
}
