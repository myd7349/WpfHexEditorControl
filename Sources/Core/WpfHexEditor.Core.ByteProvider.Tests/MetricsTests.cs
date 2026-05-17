using WpfHexEditor.Core.Bytes;
using WpfHexEditor.Core.Metrics;
using Xunit;

namespace WpfHexEditor.Core.ByteProvider.Tests;

/// <summary>Test-double metrics that records calls.</summary>
internal sealed class RecordingMetrics : IByteProviderMetrics
{
    public int FileOpenedCount;
    public int FileClosedCount;
    public int ByteModifiedCount;
    public int BytesInsertedCount;
    public int BytesDeletedCount;
    public int SaveCount;
    public int SearchCount;
    public long LastBytesWritten;
    public int LastMatchCount;

    public void OnFileOpened(string? filePath, long fileSize) => FileOpenedCount++;
    public void OnFileClosed(string? filePath)               => FileClosedCount++;
    public void OnByteRead(long virtualPosition)             { }
    public void OnByteModified(long virtualPosition)         => ByteModifiedCount++;
    public void OnBytesInserted(long virtualPosition, int count) => BytesInsertedCount++;
    public void OnBytesDeleted(long virtualPosition, long count) => BytesDeletedCount++;
    public void OnSave(string? filePath, long bytesWritten, long elapsedMs)
    {
        SaveCount++;
        LastBytesWritten = bytesWritten;
    }
    public void OnSearchCompleted(int matchCount, long elapsedMs)
    {
        SearchCount++;
        LastMatchCount = matchCount;
    }
}

public class MetricsTests
{
    [Fact]
    public void NullMetrics_IsSingleton()
    {
        Assert.Same(NullByteProviderMetrics.Instance, NullByteProviderMetrics.Instance);
    }

    [Fact]
    public void NullMetrics_AllMethodsNoOp()
    {
        var m = NullByteProviderMetrics.Instance;
        m.OnFileOpened(null, 0);
        m.OnFileClosed(null);
        m.OnByteRead(0);
        m.OnByteModified(0);
        m.OnBytesInserted(0, 1);
        m.OnBytesDeleted(0, 1);
        m.OnSave(null, 0, 0);
        m.OnSearchCompleted(0, 0);
    }

    [Fact]
    public void CustomMetrics_OnFileOpened_Called()
    {
        var m = new RecordingMetrics();
        var opts = ByteProviderOptions.Default.WithMetrics(m);
        using var p = new Bytes.ByteProvider(opts);
        p.OpenMemory(new byte[] { 1, 2, 3 });
        Assert.Equal(1, m.FileOpenedCount);
    }

    [Fact]
    public void CustomMetrics_OnFileClosed_Called()
    {
        var m = new RecordingMetrics();
        var opts = ByteProviderOptions.Default.WithMetrics(m);
        using var p = new Bytes.ByteProvider(opts);
        p.OpenMemory(new byte[] { 1, 2, 3 });
        p.Close();
        Assert.Equal(1, m.FileClosedCount);
    }

    [Fact]
    public void CustomMetrics_OnByteModified_Called()
    {
        var m = new RecordingMetrics();
        var opts = ByteProviderOptions.Default.WithMetrics(m);
        using var p = new Bytes.ByteProvider(opts);
        p.OpenMemory(new byte[] { 1, 2, 3 });
        p.ModifyByte(0, 0xFF);
        Assert.Equal(1, m.ByteModifiedCount);
    }

    [Fact]
    public void CustomMetrics_OnBytesInserted_Called()
    {
        var m = new RecordingMetrics();
        var opts = ByteProviderOptions.Default.WithMetrics(m);
        using var p = new Bytes.ByteProvider(opts);
        p.OpenMemory(new byte[] { 1, 2, 3 });
        p.InsertByte(0, 0xAA);
        Assert.Equal(1, m.BytesInsertedCount);
    }

    [Fact]
    public void CustomMetrics_OnBytesDeleted_Called()
    {
        var m = new RecordingMetrics();
        var opts = ByteProviderOptions.Default.WithMetrics(m);
        using var p = new Bytes.ByteProvider(opts);
        p.OpenMemory(new byte[] { 1, 2, 3 });
        p.DeleteByte(0);
        Assert.Equal(1, m.BytesDeletedCount);
    }

    [Fact]
    public void CustomMetrics_OnSearch_Called()
    {
        var m = new RecordingMetrics();
        var opts = ByteProviderOptions.Default.WithMetrics(m);
        using var p = new Bytes.ByteProvider(opts);
        p.OpenMemory(new byte[] { 1, 2, 1, 2 });

        var searchOpts = new Search.Models.SearchOptions
        {
            Pattern = new byte[] { 1, 2 }
        };
        p.Search(searchOpts);
        Assert.Equal(1, m.SearchCount);
    }
}
