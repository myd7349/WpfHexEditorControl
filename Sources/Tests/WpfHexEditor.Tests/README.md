# WPF HexEditor - Unit Tests

Comprehensive unit tests for WPF HexEditor performance optimizations using xUnit.

## 📊 Test Coverage

### SpanSearchExtensionsTests (18 tests)
Tests for high-performance Span<byte> search extensions.

**Coverage:**
- `FindIndexOf()` - Pattern matching in Span<byte>
  - Single occurrence
  - Multiple occurrences
  - Overlapping matches
  - Edge cases (empty data, empty pattern, pattern longer than data)
  - Base offset application
- `FindFirstIndexOf()` - First-match optimization
  - Early termination
  - Base offset
  - No-match scenarios
- `CountOccurrences()` - Zero-allocation counting
  - Correct counting
  - Empty patterns
  - No-match scenarios

**Status:** ✅ All 18 tests passing

### ByteProviderOptimizedSearchTests (17 tests)
Tests for ByteProvider optimized search methods using Span<byte> and ArrayPool.

**Coverage:**
- `FindIndexOfOptimized()` - Chunked search with ArrayPool
  - Multiple occurrences across file
  - Start position support
  - No-match handling
  - Small/large chunk sizes
  - Patterns at chunk boundaries
  - Edge cases (null, empty, beyond end)
- `FindFirstOptimized()` - Early-exit search
  - First occurrence finding
  - Start position support
  - No-match scenarios
- `CountOccurrencesOptimized()` - Optimized counting
  - Accurate counting
  - Start position support
  - Frequent patterns

**Status:** ✅ All 17 tests passing

## 🚀 Running Tests

### All Tests
```bash
cd Sources/Tests/WpfHexEditor.Tests
dotnet test
```

### Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~SpanSearchExtensionsTests"
dotnet test --filter "FullyQualifiedName~ByteProviderOptimizedSearchTests"
```

### Single Test Method
```bash
dotnet test --filter "FullyQualifiedName~FindIndexOfOptimized_FindsAllOccurrences"
```

### With Verbose Output
```bash
dotnet test --verbosity normal
```

### Generate Code Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## 🛠️ Requirements

- **.NET 8.0 SDK** for Windows
- **Windows 10/11** (WPF dependency for ByteProvider tests)
- **xUnit 2.6.6**
- **Microsoft.NET.Test.Sdk 17.8.0**

**Note:** Tests target `net8.0-windows` with `UseWPF=true` because ByteProvider depends on PresentationFramework.

## 📝 Test Structure

```
Tests/
└── WpfHexEditor.Tests/
    ├── WpfHexEditor.Tests.csproj
    ├── SpanSearchExtensionsTests.cs       (Pure Span<byte> tests, no file I/O)
    ├── ByteProviderOptimizedSearchTests.cs (ByteProvider integration tests)
    └── README.md
```

## 🎯 Test Philosophy

### Unit Tests (Current)
- **Fast execution** (< 2 seconds total)
- **No external dependencies** (temp files created/cleaned automatically)
- **Deterministic** (same input = same output)
- **Isolated** (each test independent)

### Integration Tests (Future)
- Large file handling (GB-sized files)
- Performance regression detection
- Multi-threaded scenarios
- Memory profiling

## 📈 Test Results Example

```
Test Run Successful.
Total tests: 35
     Passed: 35
 Total time: 1.8423 Seconds
```

## 🧪 Adding New Tests

### For Span Extensions
```csharp
[Fact]
public void MyNewTest()
{
    // Arrange
    byte[] data = { /* test data */ };
    byte[] pattern = { /* search pattern */ };
    ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

    // Act
    var results = span.FindIndexOf(pattern, baseOffset: 0);

    // Assert
    Assert.Equal(expectedCount, results.Count);
    Assert.Equal(expectedPosition, results[0]);
}
```

### For ByteProvider Tests
```csharp
public class MyNewTests : IDisposable
{
    private readonly string _testFile;
    private readonly ByteProvider _provider;

    public MyNewTests()
    {
        _testFile = Path.GetTempFileName();
        // Create test data
        File.WriteAllBytes(_testFile, testData);
        _provider = new ByteProvider(_testFile);
    }

    public void Dispose()
    {
        _provider?.Dispose();
        if (File.Exists(_testFile))
            try { File.Delete(_testFile); } catch { }
    }

    [Fact]
    public void MyTest()
    {
        // Test using _provider
    }
}
```

## 🐛 Troubleshooting

### Tests Fail with "Could not load PresentationFramework"
**Solution:** Ensure project targets `net8.0-windows` with `<UseWPF>true</UseWPF>`

### Tests Fail with File Access Errors
**Solution:** Check that test cleanup (Dispose) is working correctly

### Slow Test Execution
**Solution:**
- Reduce test file sizes if possible
- Use smaller chunk sizes for ByteProvider tests
- Run specific test classes instead of all tests

## 🔗 Related

- [Benchmarks](../../Benchmarks/WpfHexEditor.Benchmarks/README.md) - Performance benchmarking with BenchmarkDotNet
- [Performance Sample](../../Samples/WpfHexEditor.Sample.Performance/README.md) - Interactive performance demo
- [Performance Extensions](../../WPFHexaEditor/Core/Bytes/PERFORMANCE_README.md) - API documentation

## 📜 License

Apache 2.0 - Same as WPF HexEditor parent project

---

**✅ All tests green!**
