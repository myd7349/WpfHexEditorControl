//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using BenchmarkDotNet.Attributes;
using System.IO;
using WpfHexaEditor.Core.Bytes;

namespace WPFHexaEditor.Benchmarks
{
    /// <summary>
    /// Benchmarks for ByteProvider operations (GetByte, SetByte, Stream operations)
    /// </summary>
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class ByteProviderBenchmarks
    {
        private ByteProvider _provider1KB;
        private ByteProvider _provider100KB;
        private ByteProvider _provider1MB;

        [GlobalSetup]
        public void Setup()
        {
            // Create test data of different sizes
            _provider1KB = CreateProvider(1024); // 1 KB
            _provider100KB = CreateProvider(100 * 1024); // 100 KB
            _provider1MB = CreateProvider(1024 * 1024); // 1 MB
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _provider1KB?.Dispose();
            _provider100KB?.Dispose();
            _provider1MB?.Dispose();
        }

        private ByteProvider CreateProvider(int size)
        {
            var data = new byte[size];
            for (int i = 0; i < size; i++)
                data[i] = (byte)(i % 256);

            var provider = new ByteProvider();
            provider.Stream = new MemoryStream(data);
            return provider;
        }

        #region GetByte Benchmarks

        [Benchmark(Description = "GetByte - Random access (1KB)")]
        public void GetByte_Random_1KB()
        {
            for (int i = 0; i < 1000; i++)
            {
                var pos = (i * 17) % _provider1KB.Length;
                _provider1KB.GetByte(pos);
            }
        }

        [Benchmark(Description = "GetByte - Sequential access (1KB)")]
        public void GetByte_Sequential_1KB()
        {
            for (long i = 0; i < _provider1KB.Length; i++)
            {
                _provider1KB.GetByte(i);
            }
        }

        [Benchmark(Description = "GetByte - Random access (100KB)")]
        public void GetByte_Random_100KB()
        {
            for (int i = 0; i < 10000; i++)
            {
                var pos = (i * 17) % _provider100KB.Length;
                _provider100KB.GetByte(pos);
            }
        }

        [Benchmark(Description = "GetByte - Sequential access (1MB)")]
        public void GetByte_Sequential_1MB()
        {
            // Sample every 1000th byte to keep benchmark reasonable
            for (long i = 0; i < _provider1MB.Length; i += 1000)
            {
                _provider1MB.GetByte(i);
            }
        }

        #endregion

        #region Stream Read Benchmarks

        [Benchmark(Description = "Read stream chunk (1KB)")]
        public void ReadStream_1KB()
        {
            var buffer = new byte[256];
            _provider1KB.Stream.Position = 0;
            _provider1KB.Stream.Read(buffer, 0, buffer.Length);
        }

        [Benchmark(Description = "Read stream chunk (100KB)")]
        public void ReadStream_100KB()
        {
            var buffer = new byte[4096];
            _provider100KB.Stream.Position = 0;
            _provider100KB.Stream.Read(buffer, 0, buffer.Length);
        }

        [Benchmark(Description = "Read entire stream (1MB)")]
        public void ReadStream_Full_1MB()
        {
            var buffer = new byte[_provider1MB.Length];
            _provider1MB.Stream.Position = 0;
            _provider1MB.Stream.Read(buffer, 0, buffer.Length);
        }

        #endregion

        #region Modification Benchmarks

        [Benchmark(Description = "AddByteModified - 1000 operations")]
        public void AddByteModified_1000()
        {
            for (int i = 0; i < 1000; i++)
            {
                _provider1KB.AddByteModified(0xFF, i % _provider1KB.Length, 1);
            }
        }

        [Benchmark(Description = "AddByteAdded - 100 operations")]
        public void AddByteAdded_100()
        {
            for (int i = 0; i < 100; i++)
            {
                _provider1KB.AddByteAdded(0xAA, 0);
            }
        }

        #endregion

        #region Length and Position Operations

        [Benchmark(Description = "Check Length property - 10000 times")]
        public void CheckLength_10000()
        {
            long total = 0;
            for (int i = 0; i < 10000; i++)
            {
                total += _provider1MB.Length;
            }
        }

        [Benchmark(Description = "Position validation - 10000 times")]
        public void ValidatePosition_10000()
        {
            int count = 0;
            for (int i = 0; i < 10000; i++)
            {
                var pos = i % (_provider1KB.Length + 100);
                if (pos >= 0 && pos < _provider1KB.Length)
                    count++;
            }
        }

        #endregion
    }
}
