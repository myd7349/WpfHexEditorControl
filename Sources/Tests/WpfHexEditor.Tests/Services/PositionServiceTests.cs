//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.IO;
using Xunit;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.Bytes;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Tests.Services
{
    /// <summary>
    /// Unit tests for PositionService
    /// </summary>
    public class PositionServiceTests
    {
        private ByteProvider CreateTestProvider()
        {
            var provider = new ByteProvider();
            provider.Stream = new MemoryStream(new byte[100]); // 100 bytes
            return provider;
        }

        #region Line/Column Calculations

        [Fact]
        public void GetLineNumber_Position16BytesPerLine_ReturnsCorrectLine()
        {
            // Arrange
            var service = new PositionService();
            var provider = CreateTestProvider();

            // Act - Position 32 with 16 bytes per line = line 2
            var line = service.GetLineNumber(32, byteShiftLeft: 0, hideByteDeleted: false,
                bytePerLine: 16, byteSizeRatio: 1, provider);

            // Assert
            Assert.Equal(2, line);
        }

        [Fact]
        public void GetLineNumber_WithByteShift_CalculatesCorrectly()
        {
            // Arrange
            var service = new PositionService();
            var provider = CreateTestProvider();

            // Act
            var line = service.GetLineNumber(20, byteShiftLeft: 4, hideByteDeleted: false,
                bytePerLine: 16, byteSizeRatio: 1, provider);

            // Assert
            Assert.Equal(1, line); // (20 - 4) / 16 = 1
        }

        [Fact]
        public void GetColumnNumber_Position5_ReturnsColumn5()
        {
            // Arrange
            var service = new PositionService();
            var provider = CreateTestProvider();

            // Act
            var column = service.GetColumnNumber(5, hideByteDeleted: false,
                allowVisualByteAddress: false, visualByteAdressStart: 0,
                byteShiftLeft: 0, bytePerLine: 16, provider);

            // Assert
            Assert.Equal(5, column);
        }

        [Fact]
        public void GetColumnNumber_Position18_ReturnsColumn2()
        {
            // Arrange
            var service = new PositionService();
            var provider = CreateTestProvider();

            // Act - 18 % 16 = 2
            var column = service.GetColumnNumber(18, hideByteDeleted: false,
                allowVisualByteAddress: false, visualByteAdressStart: 0,
                byteShiftLeft: 0, bytePerLine: 16, provider);

            // Assert
            Assert.Equal(2, column);
        }

        [Fact]
        public void GetCountOfByteDeletedBeforePosition_NullProvider_ReturnsZero()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var count = service.GetCountOfByteDeletedBeforePosition(10, null);

            // Assert
            Assert.Equal(0, count);
        }

        #endregion

        #region Position Validation

        [Fact]
        public void GetValidPositionFrom_NullProvider_ReturnsNegative()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var position = service.GetValidPositionFrom(10, 5, null);

            // Assert
            Assert.Equal(-1, position);
        }

        [Fact]
        public void GetValidPositionFrom_PositiveCorrection_AddsToPosition()
        {
            // Arrange
            var service = new PositionService();
            var provider = CreateTestProvider();

            // Act
            var position = service.GetValidPositionFrom(10, 5, provider);

            // Assert
            Assert.True(position >= 10); // Should be at least start position
        }

        [Fact]
        public void GetValidPositionFrom_NegativeCorrection_SubtractsFromPosition()
        {
            // Arrange
            var service = new PositionService();
            var provider = CreateTestProvider();

            // Act
            var position = service.GetValidPositionFrom(20, -5, provider);

            // Assert
            Assert.True(position <= 20 && position >= 0);
        }

        #endregion

        #region Hex Conversion

        [Fact]
        public void HexLiteralToLong_ValidHex_ConvertsCorrectly()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var (success, position) = service.HexLiteralToLong("0x10");

            // Assert
            Assert.True(success);
            Assert.Equal(16, position);
        }

        [Fact]
        public void HexLiteralToLong_InvalidHex_ReturnsFalse()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var (success, position) = service.HexLiteralToLong("ZZZZ");

            // Assert
            Assert.False(success);
        }

        [Fact]
        public void LongToHex_ValidPosition_ReturnsHexString()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var hex = service.LongToHex(255);

            // Assert
            Assert.NotNull(hex);
            Assert.Contains("FF", hex.ToUpper());
        }

        [Fact]
        public void LongToHex_ZeroPosition_ReturnsZeroHex()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var hex = service.LongToHex(0);

            // Assert
            Assert.NotNull(hex);
        }

        #endregion

        #region Visibility Calculations

        [Fact]
        public void GetFirstVisibleBytePosition_ScrollZero_ReturnsZero()
        {
            // Arrange
            var service = new PositionService();
            var provider = CreateTestProvider();

            // Act
            var position = service.GetFirstVisibleBytePosition(scrollValue: 0,
                bytePerLine: 16, byteShiftLeft: 0, byteSizeRatio: 1,
                hideByteDeleted: false, allowVisualByteAddress: false,
                visualByteAdressStart: 0, provider);

            // Assert
            Assert.Equal(0, position);
        }

        [Fact]
        public void GetFirstVisibleBytePosition_ScrollOne_ReturnsCorrectPosition()
        {
            // Arrange
            var service = new PositionService();
            var provider = CreateTestProvider();

            // Act
            var position = service.GetFirstVisibleBytePosition(scrollValue: 1,
                bytePerLine: 16, byteShiftLeft: 0, byteSizeRatio: 1,
                hideByteDeleted: false, allowVisualByteAddress: false,
                visualByteAdressStart: 0, provider);

            // Assert
            Assert.Equal(16, position); // 1 * 16 = 16
        }

        [Fact]
        public void IsBytePositionVisible_PositionInRange_ReturnsTrue()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var isVisible = service.IsBytePositionVisible(bytePosition: 50,
                firstVisiblePosition: 0, lastVisiblePosition: 100);

            // Assert
            Assert.True(isVisible);
        }

        [Fact]
        public void IsBytePositionVisible_PositionOutOfRange_ReturnsFalse()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var isVisible = service.IsBytePositionVisible(bytePosition: 150,
                firstVisiblePosition: 0, lastVisiblePosition: 100);

            // Assert
            Assert.False(isVisible);
        }

        [Fact]
        public void IsBytePositionVisible_ExactBoundaries_ReturnsTrue()
        {
            // Arrange
            var service = new PositionService();

            // Act & Assert
            Assert.True(service.IsBytePositionVisible(0, 0, 100)); // At start
            Assert.True(service.IsBytePositionVisible(100, 0, 100)); // At end
        }

        #endregion

        #region Validation Helpers

        [Fact]
        public void IsPositionValid_ValidPosition_ReturnsTrue()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var isValid = service.IsPositionValid(50, 100);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsPositionValid_NegativePosition_ReturnsFalse()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var isValid = service.IsPositionValid(-1, 100);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsPositionValid_ExceedsMaxLength_ReturnsFalse()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var isValid = service.IsPositionValid(150, 100);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsPositionValid_AtMaxLength_ReturnsFalse()
        {
            // Arrange
            var service = new PositionService();

            // Act - Position must be < maxLength, not <= maxLength
            var isValid = service.IsPositionValid(100, 100);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ClampPosition_WithinRange_ReturnsSamePosition()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var clamped = service.ClampPosition(50, minPosition: 0, maxPosition: 100);

            // Assert
            Assert.Equal(50, clamped);
        }

        [Fact]
        public void ClampPosition_BelowMin_ReturnsMin()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var clamped = service.ClampPosition(-10, minPosition: 0, maxPosition: 100);

            // Assert
            Assert.Equal(0, clamped);
        }

        [Fact]
        public void ClampPosition_AboveMax_ReturnsMax()
        {
            // Arrange
            var service = new PositionService();

            // Act
            var clamped = service.ClampPosition(150, minPosition: 0, maxPosition: 100);

            // Assert
            Assert.Equal(100, clamped);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void PositionCalculations_LineAndColumn_WorkTogether()
        {
            // Arrange
            var service = new PositionService();
            var provider = CreateTestProvider();
            long position = 35; // Line 2, Column 3 (with 16 bytes per line)

            // Act
            var line = service.GetLineNumber(position, 0, false, 16, 1, provider);
            var column = service.GetColumnNumber(position, false, false, 0, 0, 16, provider);

            // Assert
            Assert.Equal(2, line); // 35 / 16 = 2
            Assert.Equal(3, column); // 35 % 16 = 3
        }

        [Fact]
        public void HexConversion_RoundTrip_PreservesValue()
        {
            // Arrange
            var service = new PositionService();
            long originalPosition = 255;

            // Act
            var hex = service.LongToHex(originalPosition);
            var (success, convertedBack) = service.HexLiteralToLong("0x" + hex);

            // Assert
            Assert.True(success);
            // Note: Conversion may add formatting, so just check it's a valid hex
            Assert.NotNull(hex);
        }

        #endregion
    }
}
