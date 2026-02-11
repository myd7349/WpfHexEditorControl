using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using WpfHexaEditor.Core.MethodExtention;

namespace WpfHexEditor.Tests
{
    /// <summary>
    /// Unit tests for SpanSearchExtensions high-performance search methods
    /// </summary>
    public class SpanSearchExtensionsTests
    {
        [Fact]
        public void FindIndexOf_FindsSingleOccurrence()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] pattern = { 4, 5, 6 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            var results = span.FindIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Single(results);
            Assert.Equal(3, results[0]);
        }

        [Fact]
        public void FindIndexOf_FindsMultipleOccurrences()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 1, 2, 3, 1, 2, 3 };
            byte[] pattern = { 1, 2, 3 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            var results = span.FindIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Equal(0, results[0]);
            Assert.Equal(3, results[1]);
            Assert.Equal(6, results[2]);
        }

        [Fact]
        public void FindIndexOf_ReturnsEmptyForNoMatch()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 4, 5 };
            byte[] pattern = { 9, 9, 9 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            var results = span.FindIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void FindIndexOf_HandlesEmptyPattern()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 4, 5 };
            byte[] pattern = Array.Empty<byte>();
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            var results = span.FindIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void FindIndexOf_HandlesEmptyData()
        {
            // Arrange
            byte[] data = Array.Empty<byte>();
            byte[] pattern = { 1, 2, 3 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            var results = span.FindIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void FindIndexOf_HandlesPatternLongerThanData()
        {
            // Arrange
            byte[] data = { 1, 2 };
            byte[] pattern = { 1, 2, 3, 4, 5 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            var results = span.FindIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void FindIndexOf_HandlesOverlappingMatches()
        {
            // Arrange
            byte[] data = { 1, 1, 1, 1, 1 };
            byte[] pattern = { 1, 1 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            var results = span.FindIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Equal(4, results.Count); // Matches at 0, 1, 2, 3
            Assert.Equal(0, results[0]);
            Assert.Equal(1, results[1]);
            Assert.Equal(2, results[2]);
            Assert.Equal(3, results[3]);
        }

        [Fact]
        public void FindIndexOf_AppliesBaseOffset()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 4, 5 };
            byte[] pattern = { 3, 4 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);
            long baseOffset = 1000;

            // Act
            var results = span.FindIndexOf(pattern, baseOffset);

            // Assert
            Assert.Single(results);
            Assert.Equal(1002, results[0]); // 2 + 1000
        }

        [Fact]
        public void FindFirstIndexOf_FindsFirstMatch()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 1, 2, 3, 1, 2, 3 };
            byte[] pattern = { 1, 2, 3 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            long result = span.FindFirstIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void FindFirstIndexOf_ReturnsNegativeForNoMatch()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 4, 5 };
            byte[] pattern = { 9, 9, 9 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            long result = span.FindFirstIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void FindFirstIndexOf_AppliesBaseOffset()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 4, 5 };
            byte[] pattern = { 3, 4 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);
            long baseOffset = 5000;

            // Act
            long result = span.FindFirstIndexOf(pattern, baseOffset);

            // Assert
            Assert.Equal(5002, result); // 2 + 5000
        }

        [Fact]
        public void CountOccurrences_CountsCorrectly()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 1, 2, 3, 1, 2, 3 };
            byte[] pattern = { 1, 2, 3 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            int count = span.CountOccurrences(pattern);

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void CountOccurrences_ReturnsZeroForNoMatch()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 4, 5 };
            byte[] pattern = { 9, 9, 9 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            int count = span.CountOccurrences(pattern);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void CountOccurrences_HandlesEmptyPattern()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 4, 5 };
            byte[] pattern = Array.Empty<byte>();
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            int count = span.CountOccurrences(pattern);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void FindIndexOf_SingleBytePattern()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 2, 5, 2, 7 };
            byte[] pattern = { 2 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            var results = span.FindIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Equal(1, results[0]);
            Assert.Equal(3, results[1]);
            Assert.Equal(5, results[2]);
        }

        [Fact]
        public void FindIndexOf_PatternAtEndOfData()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] pattern = { 8, 9 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            var results = span.FindIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Single(results);
            Assert.Equal(7, results[0]);
        }

        [Fact]
        public void FindIndexOf_PatternAtBeginning()
        {
            // Arrange
            byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] pattern = { 1, 2 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            var results = span.FindIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Single(results);
            Assert.Equal(0, results[0]);
        }

        [Fact]
        public void FindIndexOf_LargePattern()
        {
            // Arrange
            byte[] data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
            byte[] pattern = { 100, 101, 102, 103, 104, 105 };
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

            // Act
            var results = span.FindIndexOf(pattern, baseOffset: 0);

            // Assert
            Assert.Single(results);
            Assert.Equal(100, results[0]);
        }
    }
}
