//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using BenchmarkDotNet.Attributes;
using System.IO;
using WpfHexaEditor.Core.Bytes;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Benchmarks
{
    /// <summary>
    /// Benchmarks for search operations (FindFirst, FindNext, FindAll)
    /// </summary>
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class SearchBenchmarks
    {
        private FindReplaceService _service;
        private ByteProvider _provider10KB;
        private ByteProvider _provider100KB;
        private ByteProvider _provider1MB;
        private byte[] _searchPattern2Bytes;
        private byte[] _searchPattern4Bytes;
        private byte[] _searchPattern8Bytes;

        [GlobalSetup]
        public void Setup()
        {
            _service = new FindReplaceService();

            // Create test data with known patterns
            _provider10KB = CreateProviderWithPatterns(10 * 1024);
            _provider100KB = CreateProviderWithPatterns(100 * 1024);
            _provider1MB = CreateProviderWithPatterns(1024 * 1024);

            // Search patterns
            _searchPattern2Bytes = new byte[] { 0xAA, 0xBB };
            _searchPattern4Bytes = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
            _searchPattern8Bytes = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22 };
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _provider10KB?.Dispose();
            _provider100KB?.Dispose();
            _provider1MB?.Dispose();
        }

        private ByteProvider CreateProviderWithPatterns(int size)
        {
            var data = new byte[size];

            // Fill with semi-random data
            for (int i = 0; i < size; i++)
                data[i] = (byte)(i % 256);

            // Insert search patterns at known intervals
            for (int i = 0; i < size - 8; i += 1000)
            {
                if (i + 8 < size)
                {
                    data[i] = 0xAA;
                    data[i + 1] = 0xBB;
                    data[i + 2] = 0xCC;
                    data[i + 3] = 0xDD;
                    data[i + 4] = 0xEE;
                    data[i + 5] = 0xFF;
                    data[i + 6] = 0x11;
                    data[i + 7] = 0x22;
                }
            }

            var provider = new ByteProvider();
            provider.Stream = new MemoryStream(data);
            return provider;
        }

        #region FindFirst Benchmarks

        [Benchmark(Description = "FindFirst - 2 bytes pattern (10KB)")]
        public void FindFirst_2Bytes_10KB()
        {
            _service.FindFirst(_provider10KB, _searchPattern2Bytes);
        }

        [Benchmark(Description = "FindFirst - 4 bytes pattern (10KB)")]
        public void FindFirst_4Bytes_10KB()
        {
            _service.FindFirst(_provider10KB, _searchPattern4Bytes);
        }

        [Benchmark(Description = "FindFirst - 8 bytes pattern (10KB)")]
        public void FindFirst_8Bytes_10KB()
        {
            _service.FindFirst(_provider10KB, _searchPattern8Bytes);
        }

        [Benchmark(Description = "FindFirst - 2 bytes pattern (100KB)")]
        public void FindFirst_2Bytes_100KB()
        {
            _service.FindFirst(_provider100KB, _searchPattern2Bytes);
        }

        [Benchmark(Description = "FindFirst - 4 bytes pattern (1MB)")]
        public void FindFirst_4Bytes_1MB()
        {
            _service.FindFirst(_provider1MB, _searchPattern4Bytes);
        }

        #endregion

        #region FindAll Benchmarks

        [Benchmark(Description = "FindAll - 2 bytes pattern (10KB)")]
        public void FindAll_2Bytes_10KB()
        {
            var results = _service.FindAll(_provider10KB, _searchPattern2Bytes);
            // Force enumeration
            foreach (var _ in results) { }
        }

        [Benchmark(Description = "FindAll - 4 bytes pattern (10KB)")]
        public void FindAll_4Bytes_10KB()
        {
            var results = _service.FindAll(_provider10KB, _searchPattern4Bytes);
            foreach (var _ in results) { }
        }

        [Benchmark(Description = "FindAll - 2 bytes pattern (100KB)")]
        public void FindAll_2Bytes_100KB()
        {
            var results = _service.FindAll(_provider100KB, _searchPattern2Bytes);
            foreach (var _ in results) { }
        }

        #endregion

        #region Cache Performance Benchmarks

        [Benchmark(Description = "FindAll with cache - 10 repeated searches (10KB)")]
        public void FindAll_WithCache_Repeated_10KB()
        {
            // First search populates cache
            _service.FindAll(_provider10KB, _searchPattern4Bytes);

            // Subsequent searches use cache
            for (int i = 0; i < 10; i++)
            {
                var results = _service.FindAll(_provider10KB, _searchPattern4Bytes);
                foreach (var _ in results) { }
            }
        }

        [Benchmark(Description = "FindAll no cache - 10 searches (10KB)")]
        public void FindAll_NoCache_Repeated_10KB()
        {
            for (int i = 0; i < 10; i++)
            {
                // Clear cache before each search
                _service.ClearCache();
                var results = _service.FindAll(_provider10KB, _searchPattern4Bytes);
                foreach (var _ in results) { }
            }
        }

        #endregion

        #region FindNext Benchmarks

        [Benchmark(Description = "FindNext - iterate all results (10KB)")]
        public void FindNext_IterateAll_10KB()
        {
            var pos = _service.FindFirst(_provider10KB, _searchPattern2Bytes);

            while (pos != -1)
            {
                pos = _service.FindNext(_provider10KB, _searchPattern2Bytes, pos);
            }
        }

        [Benchmark(Description = "FindNext - iterate all results (100KB)")]
        public void FindNext_IterateAll_100KB()
        {
            var pos = _service.FindFirst(_provider100KB, _searchPattern4Bytes);

            while (pos != -1)
            {
                pos = _service.FindNext(_provider100KB, _searchPattern4Bytes, pos);
            }
        }

        #endregion

        #region Pattern Not Found Benchmarks

        [Benchmark(Description = "FindFirst - pattern not found (100KB)")]
        public void FindFirst_NotFound_100KB()
        {
            var notFoundPattern = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            _service.FindFirst(_provider100KB, notFoundPattern);
        }

        [Benchmark(Description = "FindAll - pattern not found (100KB)")]
        public void FindAll_NotFound_100KB()
        {
            var notFoundPattern = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            var results = _service.FindAll(_provider100KB, notFoundPattern);
            foreach (var _ in results) { }
        }

        #endregion
    }
}
