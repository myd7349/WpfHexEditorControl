using WpfHexEditor.Core.Bytes;
using Xunit;
using static WpfHexEditor.Core.ByteProvider.Tests.ByteProviderTestHelpers;

namespace WpfHexEditor.Core.ByteProvider.Tests;

public class UndoRedoTests
{
    [Fact]
    public void Undo_AfterModify_RestoresOriginal()
    {
        using var p = FromBytes(1, 2, 3);
        p.ModifyByte(0, 0xFF);
        Assert.True(p.CanUndo);
        p.Undo();
        var (val, _) = p.GetByte(0);
        Assert.Equal(1, val);
    }

    [Fact]
    public void Redo_AfterUndo_ReappliesChange()
    {
        using var p = FromBytes(1, 2, 3);
        p.ModifyByte(0, 0xFF);
        p.Undo();
        Assert.True(p.CanRedo);
        p.Redo();
        var (val, _) = p.GetByte(0);
        Assert.Equal(0xFF, val);
    }

    [Fact]
    public void CanUndo_FalseInitially()
    {
        using var p = FromBytes(1, 2, 3);
        Assert.False(p.CanUndo);
    }

    [Fact]
    public void ClearUndoRedoHistory_EmptiesStacks()
    {
        using var p = FromBytes(1, 2, 3);
        p.ModifyByte(0, 0xFF);
        p.ClearUndoRedoHistory();
        Assert.False(p.CanUndo);
        Assert.False(p.CanRedo);
    }

    [Fact]
    public void BeginCommitTransaction_UndoAsOne()
    {
        using var p = FromBytes(1, 2, 3);
        p.BeginUndoTransaction("batch");
        p.ModifyByte(0, 0xAA);
        p.ModifyByte(1, 0xBB);
        p.CommitUndoTransaction();

        p.Undo();

        var (a, _) = p.GetByte(0);
        var (b, _) = p.GetByte(1);
        Assert.Equal(1, a);
        Assert.Equal(2, b);
    }

    [Fact]
    public void RollbackUndoTransaction_DiscardsUndoEntry()
    {
        using var p = FromBytes(1, 2, 3);
        p.BeginUndoTransaction("batch");
        p.ModifyByte(0, 0xAA);
        p.RollbackUndoTransaction();

        // Physical change is kept; rollback only discards the pending undo batch entry
        var (val, _) = p.GetByte(0);
        Assert.Equal(0xAA, val);
        // No undo entry was committed, so Undo() should not revert this change
        Assert.False(p.CanUndo);
    }

    [Fact]
    public void PeekUndoDescription_ReturnsLastDescription()
    {
        using var p = FromBytes(1, 2, 3);
        p.BeginUndoTransaction("my-op");
        p.ModifyByte(0, 0xFF);
        p.CommitUndoTransaction();

        Assert.Equal("my-op", p.PeekUndoDescription());
    }
}
