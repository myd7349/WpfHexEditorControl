//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Linq;
using System.Windows.Media;
using Xunit;
using WpfHexaEditor.Core;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Tests.Services
{
    /// <summary>
    /// Unit tests for CustomBackgroundService
    /// </summary>
    public class CustomBackgroundServiceTests
    {
        #region Add Operations

        [Fact]
        public void AddBlock_ValidBlock_ReturnsTrue()
        {
            // Arrange
            var service = new CustomBackgroundService();
            var block = new CustomBackgroundBlock(0, 100, Brushes.Blue, "Test");

            // Act
            var result = service.AddBlock(block);

            // Assert
            Assert.True(result);
            Assert.Equal(1, service.GetBlockCount());
        }

        [Fact]
        public void AddBlock_NullBlock_ReturnsFalse()
        {
            // Arrange
            var service = new CustomBackgroundService();

            // Act
            var result = service.AddBlock(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddBlock_WithParameters_ReturnsTrue()
        {
            // Arrange
            var service = new CustomBackgroundService();

            // Act
            var result = service.AddBlock(0, 100, Brushes.Red, "Description");

            // Assert
            Assert.True(result);
            Assert.Equal(1, service.GetBlockCount());
        }

        [Fact]
        public void AddBlock_NegativeStart_ReturnsFalse()
        {
            // Arrange
            var service = new CustomBackgroundService();

            // Act
            var result = service.AddBlock(-1, 100, Brushes.Blue, "Test");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddBlock_ZeroLength_ReturnsFalse()
        {
            // Arrange
            var service = new CustomBackgroundService();

            // Act
            var result = service.AddBlock(0, 0, Brushes.Blue, "Test");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddBlocks_MultipleBlocks_AddsAll()
        {
            // Arrange
            var service = new CustomBackgroundService();
            var blocks = new[]
            {
                new CustomBackgroundBlock(0, 10, Brushes.Blue, "Block1"),
                new CustomBackgroundBlock(20, 10, Brushes.Red, "Block2"),
                new CustomBackgroundBlock(40, 10, Brushes.Green, "Block3")
            };

            // Act
            var count = service.AddBlocks(blocks);

            // Assert
            Assert.Equal(3, count);
            Assert.Equal(3, service.GetBlockCount());
        }

        [Fact]
        public void AddBlocks_NullEnumerable_ReturnsZero()
        {
            // Arrange
            var service = new CustomBackgroundService();

            // Act
            var count = service.AddBlocks(null);

            // Assert
            Assert.Equal(0, count);
        }

        #endregion

        #region Remove Operations

        [Fact]
        public void RemoveBlock_ExistingBlock_ReturnsTrue()
        {
            // Arrange
            var service = new CustomBackgroundService();
            var block = new CustomBackgroundBlock(0, 100, Brushes.Blue, "Test");
            service.AddBlock(block);

            // Act
            var result = service.RemoveBlock(block);

            // Assert
            Assert.True(result);
            Assert.Equal(0, service.GetBlockCount());
        }

        [Fact]
        public void RemoveBlock_NullBlock_ReturnsFalse()
        {
            // Arrange
            var service = new CustomBackgroundService();

            // Act
            var result = service.RemoveBlock(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RemoveBlocksAt_Position_RemovesOverlappingBlocks()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(0, 100, Brushes.Blue, "Block1");
            service.AddBlock(50, 50, Brushes.Red, "Block2");

            // Act
            var count = service.RemoveBlocksAt(75); // Within both blocks

            // Assert
            Assert.Equal(2, count);
            Assert.Equal(0, service.GetBlockCount());
        }

        [Fact]
        public void RemoveBlocksAt_NegativePosition_ReturnsZero()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(0, 100, Brushes.Blue, "Test");

            // Act
            var count = service.RemoveBlocksAt(-1);

            // Assert
            Assert.Equal(0, count);
            Assert.Equal(1, service.GetBlockCount()); // Block still there
        }

        [Fact]
        public void RemoveBlocksInRange_ValidRange_RemovesBlocks()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(0, 10, Brushes.Blue, "Block1");
            service.AddBlock(20, 10, Brushes.Red, "Block2");
            service.AddBlock(40, 10, Brushes.Green, "Block3");

            // Act
            var count = service.RemoveBlocksInRange(15, 35); // Should remove Block2

            // Assert
            Assert.True(count >= 1);
        }

        [Fact]
        public void ClearAll_RemovesAllBlocks()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(0, 10, Brushes.Blue, "Block1");
            service.AddBlock(20, 10, Brushes.Red, "Block2");

            // Act
            var count = service.ClearAll();

            // Assert
            Assert.Equal(2, count);
            Assert.Equal(0, service.GetBlockCount());
        }

        #endregion

        #region Query Operations

        [Fact]
        public void GetAllBlocks_ReturnsAllAddedBlocks()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(0, 10, Brushes.Blue, "Block1");
            service.AddBlock(20, 10, Brushes.Red, "Block2");

            // Act
            var blocks = service.GetAllBlocks().ToList();

            // Assert
            Assert.Equal(2, blocks.Count);
        }

        [Fact]
        public void GetBlockAt_PositionWithinBlock_ReturnsBlock()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(10, 20, Brushes.Blue, "Test");

            // Act
            var block = service.GetBlockAt(15);

            // Assert
            Assert.NotNull(block);
            Assert.Equal("Test", block.Description);
        }

        [Fact]
        public void GetBlockAt_PositionOutsideBlocks_ReturnsNull()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(10, 20, Brushes.Blue, "Test");

            // Act
            var block = service.GetBlockAt(50);

            // Assert
            Assert.Null(block);
        }

        [Fact]
        public void GetBlocksAt_PositionWithMultipleBlocks_ReturnsAll()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(0, 100, Brushes.Blue, "Block1");
            service.AddBlock(50, 50, Brushes.Red, "Block2");

            // Act
            var blocks = service.GetBlocksAt(75).ToList();

            // Assert
            Assert.Equal(2, blocks.Count);
        }

        [Fact]
        public void GetBlocksInRange_ReturnsOverlappingBlocks()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(0, 10, Brushes.Blue, "Block1");
            service.AddBlock(20, 10, Brushes.Red, "Block2");
            service.AddBlock(40, 10, Brushes.Green, "Block3");

            // Act
            var blocks = service.GetBlocksInRange(15, 35).ToList();

            // Assert
            Assert.True(blocks.Count >= 1); // Should include at least Block2
        }

        [Fact]
        public void HasBlockAt_PositionWithBlock_ReturnsTrue()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(10, 20, Brushes.Blue, "Test");

            // Act
            var hasBlock = service.HasBlockAt(15);

            // Assert
            Assert.True(hasBlock);
        }

        [Fact]
        public void HasBlockAt_PositionWithoutBlock_ReturnsFalse()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(10, 20, Brushes.Blue, "Test");

            // Act
            var hasBlock = service.HasBlockAt(50);

            // Assert
            Assert.False(hasBlock);
        }

        [Fact]
        public void GetBlockCount_ReturnsCorrectCount()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(0, 10, Brushes.Blue, "Block1");
            service.AddBlock(20, 10, Brushes.Red, "Block2");

            // Act
            var count = service.GetBlockCount();

            // Assert
            Assert.Equal(2, count);
        }

        [Fact]
        public void HasBlocks_WithBlocks_ReturnsTrue()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(0, 10, Brushes.Blue, "Test");

            // Act
            var hasBlocks = service.HasBlocks();

            // Assert
            Assert.True(hasBlocks);
        }

        [Fact]
        public void HasBlocks_NoBlocks_ReturnsFalse()
        {
            // Arrange
            var service = new CustomBackgroundService();

            // Act
            var hasBlocks = service.HasBlocks();

            // Assert
            Assert.False(hasBlocks);
        }

        #endregion

        #region Validation Operations

        [Fact]
        public void WouldOverlap_OverlappingBlock_ReturnsTrue()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(10, 20, Brushes.Blue, "Existing");

            // Act
            var wouldOverlap = service.WouldOverlap(15, 10); // 15-25 overlaps 10-30

            // Assert
            Assert.True(wouldOverlap);
        }

        [Fact]
        public void WouldOverlap_NonOverlappingBlock_ReturnsFalse()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(10, 20, Brushes.Blue, "Existing");

            // Act
            var wouldOverlap = service.WouldOverlap(50, 10); // 50-60 doesn't overlap 10-30

            // Assert
            Assert.False(wouldOverlap);
        }

        [Fact]
        public void GetOverlappingBlocks_ReturnsOverlaps()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(10, 20, Brushes.Blue, "Block1");
            service.AddBlock(50, 20, Brushes.Red, "Block2");

            // Act
            var overlaps = service.GetOverlappingBlocks(15, 10).ToList();

            // Assert
            Assert.Equal(1, overlaps.Count);
            Assert.Equal("Block1", overlaps[0].Description);
        }

        #endregion

        #region Sort Operations

        [Fact]
        public void GetBlocksSorted_ReturnsSortedByStartOffset()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(40, 10, Brushes.Green, "Block3");
            service.AddBlock(0, 10, Brushes.Blue, "Block1");
            service.AddBlock(20, 10, Brushes.Red, "Block2");

            // Act
            var sorted = service.GetBlocksSorted().ToList();

            // Assert
            Assert.Equal("Block1", sorted[0].Description);
            Assert.Equal("Block2", sorted[1].Description);
            Assert.Equal("Block3", sorted[2].Description);
        }

        [Fact]
        public void GetBlocksSortedByLength_ReturnsSortedDescending()
        {
            // Arrange
            var service = new CustomBackgroundService();
            service.AddBlock(0, 10, Brushes.Blue, "Small");
            service.AddBlock(20, 50, Brushes.Red, "Large");
            service.AddBlock(80, 25, Brushes.Green, "Medium");

            // Act
            var sorted = service.GetBlocksSortedByLength().ToList();

            // Assert
            Assert.Equal("Large", sorted[0].Description); // Length 50
            Assert.Equal("Medium", sorted[1].Description); // Length 25
            Assert.Equal("Small", sorted[2].Description); // Length 10
        }

        #endregion
    }
}
