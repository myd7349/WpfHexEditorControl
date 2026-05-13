using WpfHexEditor.Core.Metrics;
using Xunit;

namespace WpfHexEditor.Core.ByteProvider.Tests;

public class OptionsTests
{
    [Fact]
    public void Default_HasExpectedValues()
    {
        var opts = ByteProviderOptions.Default;
        Assert.Equal(512L * 1024 * 1024, opts.MemoryMapThresholdBytes);
        Assert.Equal(1_000, opts.MaxUndoDepth);
        Assert.Equal(64 * 1024, opts.FileCacheSizeBytes);
        Assert.False(opts.WatchExternalChanges);
        Assert.Same(NullByteProviderMetrics.Instance, opts.Metrics);
    }

    [Fact]
    public void WithMemoryMapThreshold_ReturnsNewInstance()
    {
        var original = ByteProviderOptions.Default;
        var modified = original.WithMemoryMapThreshold(1024);
        Assert.NotSame(original, modified);
        Assert.Equal(1024, modified.MemoryMapThresholdBytes);
        Assert.Equal(512L * 1024 * 1024, original.MemoryMapThresholdBytes);
    }

    [Fact]
    public void WithNoMemoryMapping_SetsMaxValue()
    {
        var opts = ByteProviderOptions.Default.WithNoMemoryMapping();
        Assert.Equal(long.MaxValue, opts.MemoryMapThresholdBytes);
    }

    [Fact]
    public void WithMaxUndoDepth_SetsDepth()
    {
        var opts = ByteProviderOptions.Default.WithMaxUndoDepth(42);
        Assert.Equal(42, opts.MaxUndoDepth);
    }

    [Fact]
    public void WithMetrics_InjectsSink()
    {
        var custom = new RecordingMetrics();
        var opts = ByteProviderOptions.Default.WithMetrics(custom);
        Assert.Same(custom, opts.Metrics);
    }

    [Fact]
    public void WithMetrics_Null_FallsBackToNullMetrics()
    {
        var opts = ByteProviderOptions.Default.WithMetrics(null!);
        Assert.Same(NullByteProviderMetrics.Instance, opts.Metrics);
    }

    [Fact]
    public void FluentChain_AllOptions_Immutable()
    {
        var opts = ByteProviderOptions.Default
            .WithMemoryMapThreshold(100)
            .WithMaxUndoDepth(10)
            .WithFileCacheSize(4096)
            .WithExternalChangeWatcher(true);

        Assert.Equal(100, opts.MemoryMapThresholdBytes);
        Assert.Equal(10, opts.MaxUndoDepth);
        Assert.Equal(4096, opts.FileCacheSizeBytes);
        Assert.True(opts.WatchExternalChanges);
        Assert.Equal(512L * 1024 * 1024, ByteProviderOptions.Default.MemoryMapThresholdBytes);
    }

}
