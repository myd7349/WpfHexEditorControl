//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using BenchmarkDotNet.Attributes;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Benchmarks
{
    /// <summary>
    /// Benchmarks for UI virtualization service
    /// Tests the performance of viewport calculations and line generation
    /// </summary>
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class VirtualizationBenchmarks
    {
        private VirtualizationService _service;

        [GlobalSetup]
        public void Setup()
        {
            _service = new VirtualizationService();
            _service.BytesPerLine = 16;
            _service.LineHeight = 20;
            _service.BufferLines = 2;
        }

        #region CalculateVisibleRange Benchmarks

        [Benchmark(Baseline = true, Description = "CalculateVisibleRange - Small file (1KB)")]
        public void CalculateVisibleRange_1KB()
        {
            long fileSize = 1024;
            long totalLines = _service.CalculateTotalLines(fileSize);

            for (int i = 0; i < 1000; i++)
            {
                _service.CalculateVisibleRange(
                    scrollOffset: i * 5,
                    viewportHeight: 400,
                    totalLines: totalLines);
            }
        }

        [Benchmark(Description = "CalculateVisibleRange - Medium file (1MB)")]
        public void CalculateVisibleRange_1MB()
        {
            long fileSize = 1024 * 1024;
            long totalLines = _service.CalculateTotalLines(fileSize);

            for (int i = 0; i < 1000; i++)
            {
                _service.CalculateVisibleRange(
                    scrollOffset: i * 100,
                    viewportHeight: 400,
                    totalLines: totalLines);
            }
        }

        [Benchmark(Description = "CalculateVisibleRange - Large file (100MB)")]
        public void CalculateVisibleRange_100MB()
        {
            long fileSize = 100L * 1024 * 1024;
            long totalLines = _service.CalculateTotalLines(fileSize);

            for (int i = 0; i < 1000; i++)
            {
                _service.CalculateVisibleRange(
                    scrollOffset: i * 1000,
                    viewportHeight: 400,
                    totalLines: totalLines);
            }
        }

        [Benchmark(Description = "CalculateVisibleRange - Huge file (1GB)")]
        public void CalculateVisibleRange_1GB()
        {
            long fileSize = 1024L * 1024 * 1024;
            long totalLines = _service.CalculateTotalLines(fileSize);

            for (int i = 0; i < 1000; i++)
            {
                _service.CalculateVisibleRange(
                    scrollOffset: i * 10000,
                    viewportHeight: 400,
                    totalLines: totalLines);
            }
        }

        #endregion

        #region GetVisibleLines Benchmarks

        [Benchmark(Description = "GetVisibleLines - 1KB file")]
        public void GetVisibleLines_1KB()
        {
            long fileSize = 1024;

            for (int i = 0; i < 1000; i++)
            {
                var lines = _service.GetVisibleLines(
                    scrollOffset: 0,
                    viewportHeight: 400,
                    fileLength: fileSize);
            }
        }

        [Benchmark(Description = "GetVisibleLines - 1MB file")]
        public void GetVisibleLines_1MB()
        {
            long fileSize = 1024 * 1024;

            for (int i = 0; i < 1000; i++)
            {
                var lines = _service.GetVisibleLines(
                    scrollOffset: i * 100,
                    viewportHeight: 400,
                    fileLength: fileSize);
            }
        }

        [Benchmark(Description = "GetVisibleLines - 100MB file")]
        public void GetVisibleLines_100MB()
        {
            long fileSize = 100L * 1024 * 1024;

            for (int i = 0; i < 1000; i++)
            {
                var lines = _service.GetVisibleLines(
                    scrollOffset: i * 1000,
                    viewportHeight: 400,
                    fileLength: fileSize);
            }
        }

        #endregion

        #region Position Conversion Benchmarks

        [Benchmark(Description = "LineToBytePosition - 10000 conversions")]
        public void LineToBytePosition_10000()
        {
            long total = 0;
            for (long i = 0; i < 10000; i++)
            {
                total += _service.LineToBytePosition(i);
            }
        }

        [Benchmark(Description = "BytePositionToLine - 10000 conversions")]
        public void BytePositionToLine_10000()
        {
            long total = 0;
            for (long i = 0; i < 1000000; i += 100)
            {
                total += _service.BytePositionToLine(i);
            }
        }

        [Benchmark(Description = "Round-trip position conversion - 1000 times")]
        public void RoundTripConversion_1000()
        {
            for (long i = 0; i < 1000; i++)
            {
                long bytePos = i * 16;
                long line = _service.BytePositionToLine(bytePos);
                long backToBytes = _service.LineToBytePosition(line);
            }
        }

        #endregion

        #region ShouldUpdateView Benchmarks

        [Benchmark(Description = "ShouldUpdateView - 10000 checks")]
        public void ShouldUpdateView_10000()
        {
            int count = 0;
            for (int i = 0; i < 10000; i++)
            {
                if (_service.ShouldUpdateView(i * 5, i * 5 + 10))
                    count++;
            }
        }

        #endregion

        #region Memory Calculation Benchmarks

        [Benchmark(Description = "EstimateMemorySavings - Various file sizes")]
        public void EstimateMemorySavings_Various()
        {
            long total = 0;

            // Test various file sizes
            total += _service.EstimateMemorySavings(100, 30);      // 1.6 KB
            total += _service.EstimateMemorySavings(1000, 30);     // 16 KB
            total += _service.EstimateMemorySavings(10000, 30);    // 160 KB
            total += _service.EstimateMemorySavings(100000, 30);   // 1.6 MB
            total += _service.EstimateMemorySavings(1000000, 30);  // 16 MB
        }

        [Benchmark(Description = "GetMemorySavingsText - 1000 conversions")]
        public void GetMemorySavingsText_1000()
        {
            for (int i = 0; i < 1000; i++)
            {
                var text = _service.GetMemorySavingsText(10000, 30);
            }
        }

        #endregion

        #region Scroll Calculation Benchmarks

        [Benchmark(Description = "ScrollToPosition - 1000 calculations")]
        public void ScrollToPosition_1000()
        {
            double total = 0;

            for (long i = 0; i < 1000; i++)
            {
                total += _service.ScrollToPosition(
                    bytePosition: i * 160,
                    centerInView: false,
                    viewportHeight: 400);
            }
        }

        [Benchmark(Description = "ScrollToPosition with centering - 1000 calculations")]
        public void ScrollToPosition_Centered_1000()
        {
            double total = 0;

            for (long i = 0; i < 1000; i++)
            {
                total += _service.ScrollToPosition(
                    bytePosition: i * 160,
                    centerInView: true,
                    viewportHeight: 400);
            }
        }

        [Benchmark(Description = "GetScrollOffsetForLine - 10000 calculations")]
        public void GetScrollOffsetForLine_10000()
        {
            double total = 0;

            for (long i = 0; i < 10000; i++)
            {
                total += _service.GetScrollOffsetForLine(i);
            }
        }

        #endregion

        #region Different BytesPerLine Configurations

        [Benchmark(Description = "CalculateVisibleRange - 8 bytes per line")]
        public void CalculateVisibleRange_8BytesPerLine()
        {
            _service.BytesPerLine = 8;
            long fileSize = 1024 * 1024;
            long totalLines = _service.CalculateTotalLines(fileSize);

            for (int i = 0; i < 1000; i++)
            {
                _service.CalculateVisibleRange(100, 400, totalLines);
            }

            _service.BytesPerLine = 16; // Reset
        }

        [Benchmark(Description = "CalculateVisibleRange - 32 bytes per line")]
        public void CalculateVisibleRange_32BytesPerLine()
        {
            _service.BytesPerLine = 32;
            long fileSize = 1024 * 1024;
            long totalLines = _service.CalculateTotalLines(fileSize);

            for (int i = 0; i < 1000; i++)
            {
                _service.CalculateVisibleRange(100, 400, totalLines);
            }

            _service.BytesPerLine = 16; // Reset
        }

        #endregion
    }
}
