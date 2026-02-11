# WPFHexaEditor Benchmarks

Performance benchmarking suite using [BenchmarkDotNet](https://benchmarkdotnet.org/) to measure and track the performance of critical operations in WPFHexaEditor.

## 🎯 What's Benchmarked

### 1. ByteProvider Operations (`ByteProviderBenchmarks`)
- **GetByte**: Random and sequential access patterns (1KB - 1MB)
- **Stream Reading**: Chunk and full stream reads
- **Modifications**: AddByteModified, AddByteAdded operations
- **Properties**: Length checks and position validation

### 2. Search Operations (`SearchBenchmarks`)
- **FindFirst**: Various pattern sizes (2, 4, 8 bytes) across different file sizes (10KB - 1MB)
- **FindAll**: Complete search with result enumeration
- **FindNext**: Iterating through all matches
- **Cache Performance**: With/without cache comparisons
- **Not Found Scenarios**: Worst-case search performance

### 3. Virtualization Service (`VirtualizationBenchmarks`)
- **CalculateVisibleRange**: Viewport calculations for files from 1KB to 1GB
- **GetVisibleLines**: Line generation for various file sizes
- **Position Conversions**: Line ↔ Byte position conversions
- **ShouldUpdateView**: Scroll threshold detection
- **Memory Calculations**: Savings estimations
- **Scroll Calculations**: Position and line offset calculations
- **Different Configurations**: 8, 16, 32 bytes per line

## 🚀 Running Benchmarks

### Run All Benchmarks

```bash
cd Sources/WPFHexaEditor.Benchmarks
dotnet run -c Release
```

### Run Specific Benchmark Class

```bash
# Run only ByteProvider benchmarks
dotnet run -c Release --filter "*ByteProviderBenchmarks*"

# Run only Search benchmarks
dotnet run -c Release --filter "*SearchBenchmarks*"

# Run only Virtualization benchmarks
dotnet run -c Release --filter "*VirtualizationBenchmarks*"
```

### Run Specific Benchmark Method

```bash
# Run specific method
dotnet run -c Release --filter "*ByteProviderBenchmarks.GetByte_Sequential_1KB*"
```

### Interactive Mode

```bash
# Let BenchmarkDotNet prompt you to select benchmarks
dotnet run -c Release
```

## 📊 Output Formats

Benchmarks automatically generate results in multiple formats:

- **Console Output**: Real-time results in terminal
- **Markdown**: `BenchmarkDotNet.Artifacts/results/*.md` (GitHub-compatible)
- **HTML**: `BenchmarkDotNet.Artifacts/results/*.html` (detailed charts)

## 📈 Understanding Results

### Key Metrics

- **Mean**: Average execution time
- **StdDev**: Standard deviation (consistency measure)
- **Median**: Middle value (less affected by outliers)
- **Allocated**: Memory allocated per operation

### Example Output

```
|                       Method |      Mean |    StdDev |    Median | Allocated |
|----------------------------- |----------:|----------:|----------:|----------:|
| GetByte_Sequential_1KB       |  12.34 μs |  0.45 μs  |  12.20 μs |     256 B |
| FindFirst_2Bytes_10KB        | 145.67 μs |  5.23 μs  | 144.80 μs |    1024 B |
```

### Performance Goals

- **ByteProvider GetByte**: < 50ns per operation
- **Search (FindFirst)**: < 500μs for 10KB
- **Virtualization CalculateVisibleRange**: < 1μs regardless of file size

## 🔧 Configuration

The benchmarks use the following configuration (see `Program.cs`):

- **Toolchain**: InProcessEmit (faster, no separate process)
- **Exporters**: HTML + GitHub Markdown
- **Columns**: Mean, StdDev, Median, Baseline Ratio
- **Memory Diagnostics**: Enabled

## 📝 Adding New Benchmarks

1. Create a new class in `WPFHexaEditor.Benchmarks` namespace
2. Add `[MemoryDiagnoser]` attribute to the class
3. Create `[GlobalSetup]` method for initialization
4. Add benchmark methods with `[Benchmark]` attribute
5. Add `[GlobalCleanup]` method for cleanup

Example:

```csharp
[MemoryDiagnoser]
public class MyBenchmarks
{
    private MyService _service;

    [GlobalSetup]
    public void Setup()
    {
        _service = new MyService();
    }

    [Benchmark(Description = "My operation")]
    public void MyOperation()
    {
        _service.DoSomething();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _service?.Dispose();
    }
}
```

## 🎯 Baseline Benchmarks

Some benchmarks are marked with `[Baseline = true]` to establish a reference point. Other benchmarks will show:
- **Ratio**: How much faster/slower compared to baseline
- Example: "Ratio = 2.5x" means 2.5 times slower than baseline

## ⚙️ Best Practices

1. **Always run in Release mode**: Debug mode adds overhead
2. **Close other applications**: Minimize interference
3. **Multiple runs**: BenchmarkDotNet automatically does warmup and multiple iterations
4. **Consistent environment**: Same machine, same load for comparisons
5. **Watch for outliers**: Check StdDev - high values indicate inconsistency

## 📦 Dependencies

- **BenchmarkDotNet** 0.14.0: Main benchmarking framework
- **WPFHexaEditor**: The library being benchmarked

## 🐛 Troubleshooting

### "Benchmark not found"

Make sure the class is `public` and methods have `[Benchmark]` attribute.

### "OutOfMemoryException"

Reduce the data size in benchmarks or increase available memory.

### Results seem inconsistent

- Close background applications
- Disable CPU throttling/turbo boost for consistency
- Run multiple times and compare

## 📚 Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Performance Best Practices](https://benchmarkdotnet.org/articles/guides/choosing-run-strategy.html)
- [Interpreting Results](https://benchmarkdotnet.org/articles/guides/statistics.html)

---

**Co-Authored-By:** Claude Sonnet 4.5 <noreply@anthropic.com>
