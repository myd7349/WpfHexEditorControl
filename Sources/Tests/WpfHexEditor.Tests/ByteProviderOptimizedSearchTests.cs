using System;
using System.IO;
using System.Linq;
using Xunit;
using WpfHexaEditor.Core.Bytes;

namespace WpfHexEditor.Tests
{
    /// <summary>
    /// Unit tests for ByteProvider optimized search methods using Span&lt;byte&gt; and ArrayPool
    /// </summary>
    public class ByteProviderOptimizedSearchTests : IDisposable
    {
        private readonly string _testFile;
        private readonly ByteProvider _provider;

        public ByteProviderOptimizedSearchTests()
        {
            // Create test file with known content
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

        [Fact]
        public void FindIndexOfOptimized_FindsAllOccurrences()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };

            // Act
            var results = _provider.FindIndexOfOptimized(pattern, 0).ToList();

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Contains(100L, results);
            Assert.Contains(500L, results);
            Assert.Contains(9000L, results);
        }

        [Fact]
        public void FindIndexOfOptimized_WithStartPosition()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };

            // Act
            var results = _provider.FindIndexOfOptimized(pattern, startPosition: 200).ToList();

            // Assert
            Assert.Equal(2, results.Count); // Should find 500 and 9000, but not 100
            Assert.DoesNotContain(100L, results);
            Assert.Contains(500L, results);
            Assert.Contains(9000L, results);
        }

        [Fact]
        public void FindIndexOfOptimized_NoMatch_ReturnsEmpty()
        {
            // Arrange
            byte[] pattern = { 0xFF, 0xFF, 0xFF, 0xFF };

            // Act
            var results = _provider.FindIndexOfOptimized(pattern, 0).ToList();

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void FindFirstOptimized_FindsFirstOccurrence()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };

            // Act
            long result = _provider.FindFirstOptimized(pattern, 0);

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void FindFirstOptimized_WithStartPosition()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };

            // Act
            long result = _provider.FindFirstOptimized(pattern, startPosition: 200);

            // Assert
            Assert.Equal(500, result); // Should skip first occurrence at 100
        }

        [Fact]
        public void FindFirstOptimized_NoMatch_ReturnsNegative()
        {
            // Arrange
            byte[] pattern = { 0xFF, 0xFF, 0xFF, 0xFF };

            // Act
            long result = _provider.FindFirstOptimized(pattern, 0);

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void CountOccurrencesOptimized_CountsCorrectly()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };

            // Act
            int count = _provider.CountOccurrencesOptimized(pattern, 0);

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void CountOccurrencesOptimized_WithStartPosition()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };

            // Act
            int count = _provider.CountOccurrencesOptimized(pattern, startPosition: 200);

            // Assert
            Assert.Equal(2, count); // Should count 500 and 9000, but not 100
        }

        [Fact]
        public void CountOccurrencesOptimized_NoMatch_ReturnsZero()
        {
            // Arrange
            byte[] pattern = { 0xFF, 0xFF, 0xFF, 0xFF };

            // Act
            int count = _provider.CountOccurrencesOptimized(pattern, 0);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void FindIndexOfOptimized_SmallChunkSize()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };

            // Act - Use very small chunks to test boundary handling
            var results = _provider.FindIndexOfOptimized(pattern, 0, chunkSize: 128).ToList();

            // Assert - Should still find all occurrences despite small chunks
            Assert.Equal(3, results.Count);
            Assert.Contains(100L, results);
            Assert.Contains(500L, results);
            Assert.Contains(9000L, results);
        }

        [Fact]
        public void FindIndexOfOptimized_LargeChunkSize()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };

            // Act - Use large chunks to process file in fewer reads
            var results = _provider.FindIndexOfOptimized(pattern, 0, chunkSize: 1024 * 1024).ToList();

            // Assert
            Assert.Equal(3, results.Count);
        }

        [Fact]
        public void FindIndexOfOptimized_PatternAtChunkBoundary()
        {
            // This test verifies that patterns spanning chunk boundaries are found correctly
            // The overlap mechanism should handle this

            // Arrange
            string tempFile = Path.GetTempFileName();
            try
            {
                // Create file where pattern spans typical chunk boundary (64KB)
                byte[] data = new byte[70000];
                byte[] pattern = { 0xDE, 0xAD, 0xBE, 0xEF };

                // Place pattern at chunk boundary (assuming 64KB chunks)
                int boundaryPos = 65534; // 2 bytes before 64KB boundary
                Array.Copy(pattern, 0, data, boundaryPos, pattern.Length);

                File.WriteAllBytes(tempFile, data);

                using var provider = new ByteProvider(tempFile);

                // Act
                var results = provider.FindIndexOfOptimized(pattern, 0, chunkSize: 65536).ToList();

                // Assert
                Assert.Single(results);
                Assert.Equal(boundaryPos, results[0]);
            }
            finally
            {
                if (File.Exists(tempFile))
                    try { File.Delete(tempFile); } catch { }
            }
        }

        [Fact]
        public void FindIndexOfOptimized_SingleBytePattern()
        {
            // Arrange
            byte[] pattern = { 0xAA };

            // Act
            var results = _provider.FindIndexOfOptimized(pattern, 0).ToList();

            // Assert
            Assert.NotEmpty(results); // Should find at least the occurrences we inserted
            Assert.Contains(100L, results); // First byte of our pattern
            Assert.Contains(500L, results);
            Assert.Contains(9000L, results);
        }

        [Fact]
        public void FindIndexOfOptimized_EmptyPattern_ReturnsEmpty()
        {
            // Arrange
            byte[] pattern = Array.Empty<byte>();

            // Act
            var results = _provider.FindIndexOfOptimized(pattern, 0).ToList();

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void FindIndexOfOptimized_NullPattern_ReturnsEmpty()
        {
            // Arrange
            byte[]? pattern = null;

            // Act
            var results = _provider.FindIndexOfOptimized(pattern!, 0).ToList();

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void FindIndexOfOptimized_StartPositionBeyondEnd_ReturnsEmpty()
        {
            // Arrange
            byte[] pattern = { 0xAA, 0xBB, 0xCC };

            // Act
            var results = _provider.FindIndexOfOptimized(pattern, startPosition: _provider.Length + 1000).ToList();

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void CountOccurrencesOptimized_FrequentPattern()
        {
            // Arrange
            byte[] pattern = { 0x00 }; // Byte 0 appears every 256 bytes in our test data

            // Act
            int count = _provider.CountOccurrencesOptimized(pattern, 0);

            // Assert
            // In 10000 bytes with pattern (i % 256), byte 0 appears 10000/256 = 39 times
            Assert.True(count >= 39, $"Expected at least 39 occurrences, got {count}");
        }
    }
}
