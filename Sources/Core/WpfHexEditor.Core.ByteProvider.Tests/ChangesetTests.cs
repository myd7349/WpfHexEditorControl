using WpfHexEditor.Core.Bytes;
using WpfHexEditor.Core.Changesets;
using Xunit;
using static WpfHexEditor.Core.ByteProvider.Tests.ByteProviderTestHelpers;

namespace WpfHexEditor.Core.ByteProvider.Tests;

public class ChangesetTests
{

    [Fact]
    public void GetChangesetSnapshot_Clean_ReturnsEmpty()
    {
        using var p = FromBytes(1, 2, 3);
        var snap = p.GetChangesetSnapshot();
        Assert.Same(ChangesetSnapshot.Empty, snap);
    }

    [Fact]
    public void GetChangesetSnapshot_WithModify_ContainsRange()
    {
        using var p = FromBytes(1, 2, 3);
        p.ModifyByte(1, 0xFF);
        var snap = p.GetChangesetSnapshot();
        Assert.NotEmpty(snap.Modified);
        Assert.Equal(1, snap.Modified[0].Offset);
        Assert.Equal(0xFF, snap.Modified[0].Values[0]);
    }

    [Fact]
    public void ExportImportJson_RoundTrip_IdempotentEdits()
    {
        using var src = FromBytes(1, 2, 3, 4, 5);
        src.ModifyByte(0, 0xAA);
        src.ModifyByte(1, 0xBB);

        var json = src.ExportChangesetJson();

        using var dst = FromBytes(1, 2, 3, 4, 5);
        dst.ImportChangesetJson(json);

        var (a, _) = dst.GetByte(0);
        var (b, _) = dst.GetByte(1);
        Assert.Equal(0xAA, a);
        Assert.Equal(0xBB, b);
    }

    [Fact]
    public void CreateCheckpoint_RestoreCheckpoint_ResetsEdits()
    {
        using var p = FromBytes(1, 2, 3);
        p.CreateCheckpoint("before");
        p.ModifyByte(0, 0xFF);
        p.RestoreCheckpoint("before");

        var (val, _) = p.GetByte(0);
        Assert.Equal(1, val);
    }

    [Fact]
    public void DeleteCheckpoint_RemovesEntry()
    {
        using var p = FromBytes(1, 2, 3);
        p.CreateCheckpoint("snap");
        Assert.Contains("snap", p.GetCheckpoints());
        p.DeleteCheckpoint("snap");
        Assert.DoesNotContain("snap", p.GetCheckpoints());
    }

    [Fact]
    public void ImportChangeset_ReadOnly_Throws()
    {
        var p = new Bytes.ByteProvider();
        p.OpenMemory(new byte[] { 1, 2, 3 }, readOnly: true);
        Assert.Throws<InvalidOperationException>(() =>
            p.ImportChangeset(new ChangesetSnapshot(
                new[] { new ModifiedRange(0, new byte[] { 0xFF }) },
                System.Array.Empty<InsertedBlock>(),
                System.Array.Empty<DeletedRange>())));
        p.Dispose();
    }

    [Fact]
    public void GetCheckpoints_Alphabetical()
    {
        using var p = FromBytes(1, 2, 3);
        p.CreateCheckpoint("z-snap");
        p.CreateCheckpoint("a-snap");
        p.CreateCheckpoint("m-snap");
        var keys = p.GetCheckpoints();
        Assert.Equal(new[] { "a-snap", "m-snap", "z-snap" }, keys);
    }
}
