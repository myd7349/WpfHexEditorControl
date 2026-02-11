# ByteProviderBench

Benchmarking tool for measuring ByteProvider performance.

## 📁 Contents

- **[Program.cs](Program.cs)** - Console benchmarking application
- **[ByteProviderBench.csproj](ByteProviderBench.csproj)** - Project file

## 🎯 Purpose

This is a performance benchmarking tool used during development to measure and optimize ByteProvider operations:
- File reading/writing performance
- Memory usage under various scenarios
- Insert/delete operation speed
- Search algorithm efficiency
- Large file handling benchmarks

## 🔧 Benchmarked Operations

The tool tests:
- **Sequential Read**: Reading bytes sequentially
- **Random Read**: Random access patterns
- **Sequential Write**: Writing bytes in order
- **Random Write**: Random write patterns
- **Insert Operations**: Dynamic file size changes
- **Delete Operations**: Byte removal performance
- **Search Performance**: Find/FindAll operations
- **Memory Footprint**: RAM usage with large files

## 🎓 Usage

```bash
# Build and run
cd Tools/ByteProviderBench
dotnet run

# Run with specific file size
dotnet run -- --size 1GB

# Run specific benchmark
dotnet run -- --benchmark read

# Output results to file
dotnet run -- --output results.txt
```

## 📊 Typical Benchmark Output

```
ByteProvider Performance Benchmarks
====================================

File Size: 1 GB
Test Duration: 30 seconds each

Sequential Read:  1,234 MB/s
Random Read:      456 MB/s
Sequential Write: 890 MB/s
Random Write:     234 MB/s

Insert (1000 bytes):  12 ms
Delete (1000 bytes):  8 ms

Search Pattern (10 bytes):
  First Match:  23 ms
  All Matches:  456 ms (1234 results)

Memory Usage:
  Base:       45 MB
  1000 mods:  67 MB
  10000 mods: 234 MB
```

## 🔗 Related Components

- **[ByteProvider.cs](../../WPFHexaEditor/Core/Bytes/ByteProvider.cs)** - Component being benchmarked
- **[FindReplaceService.cs](../../WPFHexaEditor/Services/FindReplaceService.cs)** - Search benchmarks
- **[ByteModificationService.cs](../../WPFHexaEditor/Services/ByteModificationService.cs)** - Insert/delete benchmarks

## ⚡ Performance Targets

Development targets (as of 2026):
- **Sequential Read**: > 1000 MB/s
- **Random Read**: > 400 MB/s
- **Insert 1KB**: < 20 ms
- **Search 10 bytes**: < 50 ms (1 GB file)
- **Memory overhead**: < 100 bytes per modified byte

## 🎨 Profiling Integration

Can be used with:
- **dotnet-trace**: CPU profiling
- **dotnet-counters**: Real-time metrics
- **Visual Studio Profiler**: Memory analysis
- **PerfView**: Advanced tracing

## 📈 Historical Results

Track performance improvements across versions to ensure no regressions.

## 🔧 Building

```bash
dotnet build ByteProviderBench.csproj
```

**Note**: This is a development/internal tool, not included in NuGet package.

---

✨ Performance benchmarking for ByteProvider optimization
