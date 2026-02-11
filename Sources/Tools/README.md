# Tools

This directory contains development and benchmarking tools for the WPF HexEditor project.

## 📊 ByteProviderBench

**Purpose:** Performance benchmarking tool for the `ByteProvider` class.

**Description:**
ByteProviderBench is a benchmarking application that measures the performance of various `ByteProvider` operations to identify bottlenecks and optimize critical code paths.

### Benchmarked Operations

- **Read Performance**
  - Single byte reads (`GetByte()`)
  - Bulk byte reads (`GetBytes()`)
  - Sequential vs random access patterns

- **Write Performance**
  - Single byte modifications
  - Bulk paste operations
  - Insert vs overwrite modes

- **Search Performance**
  - `FindFirst()` on various data sizes
  - `FindAll()` performance
  - Cache hit vs cache miss scenarios

- **Undo/Redo Performance**
  - Stack operations
  - Memory usage during large undo stacks
  - Redo operation speed

### Running Benchmarks

```bash
cd ByteProviderBench
dotnet run --configuration Release
```

**Note:** Always run benchmarks in Release mode for accurate results.

### Interpreting Results

The tool outputs:
- **Mean** - Average execution time
- **StdDev** - Standard deviation (lower is more consistent)
- **Median** - Middle value (50th percentile)
- **Min/Max** - Fastest and slowest runs
- **Ops/sec** - Operations per second (higher is better)

### Use Cases

- **Before optimization** - Establish baseline performance
- **After changes** - Verify improvements
- **Regression testing** - Ensure changes don't degrade performance
- **Platform comparison** - Compare .NET Framework vs .NET Core performance

### Performance Targets

Based on typical hardware (SSD, 16GB RAM, modern CPU):

| Operation | Target | Acceptable |
|-----------|--------|------------|
| GetByte() | < 1 μs | < 5 μs |
| FindFirst() 1MB | < 10 ms | < 50 ms |
| Paste 1KB | < 1 ms | < 10 ms |
| Undo/Redo | < 100 μs | < 1 ms |

### Adding New Benchmarks

To add a new benchmark:

1. Create a new benchmark class:
```csharp
[MemoryDiagnoser]
public class MyBenchmark
{
    private ByteProvider _provider;

    [GlobalSetup]
    public void Setup()
    {
        // Initialize
    }

    [Benchmark]
    public void MyOperation()
    {
        // Code to benchmark
    }
}
```

2. Add to program entry point
3. Run and analyze results

### Tips for Accurate Benchmarks

- Close all other applications
- Disable antivirus temporarily
- Use consistent test data
- Run multiple iterations (minimum 10)
- Warm up the JIT compiler
- Test on representative data sizes

### Integration with CI/CD

ByteProviderBench can be integrated into CI/CD pipelines to:
- Detect performance regressions
- Generate performance reports
- Compare branches
- Track performance trends over time

Example GitHub Actions workflow:
```yaml
- name: Run Benchmarks
  run: |
    cd Sources/Tools/ByteProviderBench
    dotnet run -c Release --exporters json

- name: Upload Results
  uses: actions/upload-artifact@v2
  with:
    name: benchmark-results
    path: BenchmarkDotNet.Artifacts/
```

---

## 🔍 Future Tools

Potential tools to add:

- **MemoryProfiler** - Track memory usage patterns
- **StressTest** - Test with extreme file sizes
- **FuzzTester** - Random input testing for robustness
- **UIBenchmark** - Measure UI rendering performance
- **ComparisonTool** - Compare before/after performance

---

## 📚 Related Documentation

- [Core Components](../WPFHexaEditor/Core/README.md) - ByteProvider implementation details
- [Services Architecture](../WPFHexaEditor/Services/README.md) - Service layer performance
- [Main README](../../README.md) - Project overview

---

✨ Tools by Derek Tremblay and contributors
