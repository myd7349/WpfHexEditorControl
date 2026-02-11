using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using WpfHexaEditor.Core.Bytes;

namespace WpfHexEditor.Tests
{
    /// <summary>
    /// Unit tests for ByteProvider async extensions
    /// Tests progress reporting, cancellation, and UI responsiveness
    /// </summary>
    public class ByteProviderAsyncTests : IDisposable
    {
        private readonly string _testFile;
        private readonly ByteProvider _provider;

        public ByteProviderAsyncTests()
        {
            // Create test file with known content (10KB)
            _testFile = Path.GetTempFileName();

            byte[] testData = new byte[10000];
            for (int i = 0; i < testData.Length; i++)
            {
                testData[i] = (byte)(i % 256);
            }

            // Insert specific patterns for testing
            byte[] pattern1 = { 0xAA, 0xBB, 0xCC };
            Array.Copy(pattern1, 0, testData, 100, pattern1.Length);
            Array.Copy(pattern1, 0, testData, 500, pattern1.Length);
            Array.Copy(pattern1, 0, testData, 9000, pattern1.Length);

            File.WriteAllBytes(_testFile, testData);
            _provider = new ByteProvider(_testFile);
        }

        public void Dispose()
        {
            _provider?.Dispose();
            if (File.Exists(_testFile))
            {
                try { File.Delete(_testFile); } catch { }
            }
        }

        #region FindAllAsync Tests

        [Fact]
        public async Task FindAllAsync_FindsAllOccurrences()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };

            // Act
            var results = await _provider.FindAllAsync(pattern, 0, null, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(3, results.Count);
            Assert.Contains(100L, results);
            Assert.Contains(500L, results);
            Assert.Contains(9000L, results);
        }

        [Fact]
        public async Task FindAllAsync_WithStartPosition_FindsSubset()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };

            // Act
            var results = await _provider.FindAllAsync(pattern, 200, null, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count); // Should find 500 and 9000, but not 100
            Assert.DoesNotContain(100L, results);
            Assert.Contains(500L, results);
            Assert.Contains(9000L, results);
        }

        [Fact]
        public async Task FindAllAsync_NoMatch_ReturnsEmpty()
        {
            // Arrange
            byte[] pattern = { 0xFF, 0xFF, 0xFF, 0xFF };

            // Act
            var results = await _provider.FindAllAsync(pattern, 0, null, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task FindAllAsync_ReportsProgress()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };
            int lastProgress = -1;
            int progressCallCount = 0;
            var progressLock = new object();

            var progress = new Progress<int>(percent =>
            {
                lock (progressLock)
                {
                    progressCallCount++;
                    lastProgress = percent;
                }
            });

            // Act
            var results = await _provider.FindAllAsync(pattern, 0, progress, CancellationToken.None);

            // Give progress callbacks time to complete (Progress<T> posts to sync context)
            await Task.Delay(50);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(3, results.Count); // Should find all 3 occurrences
            lock (progressLock)
            {
                Assert.True(progressCallCount > 0, $"Progress should be reported at least once. Progress count: {progressCallCount}");
                Assert.Equal(100, lastProgress); // Final progress should be 100%
            }
        }

        [Fact]
        public async Task FindAllAsync_CanBeCancelled()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };
            var cts = new CancellationTokenSource();

            // Cancel immediately
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await _provider.FindAllAsync(pattern, 0, null, cts.Token);
            });
        }

        [Fact]
        public async Task FindAllAsync_CanBeCancelledDuringSearch()
        {
            // Arrange - Create larger file for meaningful cancellation test
            string largeFile = Path.GetTempFileName();
            try
            {
                byte[] largeData = new byte[10000000]; // 10MB for slower search
                File.WriteAllBytes(largeFile, largeData);

                using var largeProvider = new ByteProvider(largeFile);
                byte[] pattern = { 0xAA, 0xBB, 0xCC };

                var cts = new CancellationTokenSource();
                bool progressReported = false;

                // Cancel immediately when any progress is reported
                var progress = new Progress<int>(percent =>
                {
                    progressReported = true;
                    cts.Cancel();
                });

                // Act & Assert
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    await largeProvider.FindAllAsync(pattern, 0, progress, cts.Token);
                });

                // Verify progress was actually reported before cancellation
                Assert.True(progressReported, "Progress should have been reported before cancellation");
            }
            finally
            {
                if (File.Exists(largeFile))
                    try { File.Delete(largeFile); } catch { }
            }
        }

        #endregion

        #region GetByteAsync Tests

        [Fact]
        public async Task GetByteAsync_ReturnsCorrectByte()
        {
            // Act
            var result = await _provider.GetByteAsync(100, CancellationToken.None);

            // Assert
            Assert.True(result.success);
            Assert.NotNull(result.value);
            Assert.Equal(0xAA, result.value.Value);
        }

        [Fact]
        public async Task GetByteAsync_InvalidPosition_ReturnsFalse()
        {
            // Act
            var result = await _provider.GetByteAsync(-1, CancellationToken.None);

            // Assert
            Assert.False(result.success);
            Assert.Null(result.value);
        }

        [Fact]
        public async Task GetByteAsync_CanBeCancelled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await _provider.GetByteAsync(0, cts.Token);
            });
        }

        #endregion

        #region GetBytesAsync Tests

        [Fact]
        public async Task GetBytesAsync_ReturnsCorrectBytes()
        {
            // Arrange
            byte[] expected = { 0xAA, 0xBB, 0xCC };

            // Act
            var result = await _provider.GetBytesAsync(100, 3, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetBytesAsync_LargeBuffer_ReturnsCorrectly()
        {
            // Act
            var result = await _provider.GetBytesAsync(0, 1000, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1000, result.Length);
            // Verify pattern (skip positions 100-102, 500-502, 9000-9002 which have pattern bytes)
            for (int i = 0; i < 1000; i++)
            {
                // Skip positions where pattern was inserted
                if ((i >= 100 && i <= 102) || (i >= 500 && i <= 502))
                {
                    // These positions have the pattern { 0xAA, 0xBB, 0xCC }
                    if (i == 100 || i == 500) Assert.Equal(0xAA, result[i]);
                    else if (i == 101 || i == 501) Assert.Equal(0xBB, result[i]);
                    else if (i == 102 || i == 502) Assert.Equal(0xCC, result[i]);
                }
                else
                {
                    Assert.Equal((byte)(i % 256), result[i]);
                }
            }
        }

        [Fact]
        public async Task GetBytesAsync_CanBeCancelled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await _provider.GetBytesAsync(0, 100, cts.Token);
            });
        }

        #endregion

        #region Concurrent Operations Tests

        [Fact]
        public async Task FindAllAsync_ConcurrentSearches_WorkCorrectly()
        {
            // Arrange
            byte[] pattern1 = { 0xAA, 0xBB, 0xCC }; // Inserted at 100, 500, 9000
            byte[] pattern2 = { 0xBB, 0xCC, 0xAA }; // Doesn't exist in file

            // Act - Run two searches concurrently
            var task1 = _provider.FindAllAsync(pattern1, 0, null, CancellationToken.None);
            var task2 = _provider.FindAllAsync(pattern2, 0, null, CancellationToken.None);

            await Task.WhenAll(task1, task2);

            var results1 = await task1;
            var results2 = await task2;

            // Assert
            Assert.NotNull(results1);
            Assert.NotNull(results2);
            Assert.Equal(3, results1.Count); // Should find all 3 occurrences of pattern1
            Assert.Contains(100L, results1);
            Assert.Contains(500L, results1);
            Assert.Contains(9000L, results1);
            Assert.Empty(results2); // Pattern2 doesn't exist, should be empty
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task FindAllAsync_EmptyPattern_ReturnsEmpty()
        {
            // Arrange
            byte[] pattern = Array.Empty<byte>();

            // Act
            var results = await _provider.FindAllAsync(pattern, 0, null, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task FindAllAsync_NullPattern_ReturnsEmpty()
        {
            // Act
            var results = await _provider.FindAllAsync(null!, 0, null, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task FindAllAsync_StartPositionBeyondEnd_ReturnsEmpty()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };

            // Act
            var results = await _provider.FindAllAsync(pattern, _provider.Length + 1000, null, CancellationToken.None);

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        #endregion
    }
}
