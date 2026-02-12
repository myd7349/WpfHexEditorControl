//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Linq;
using Xunit;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Tests.Services
{
    /// <summary>
    /// Unit tests for VirtualizationService
    /// </summary>
    public class VirtualizationServiceTests
    {
        #region Properties Tests

        [Fact]
        public void BytesPerLine_DefaultValue_Is16()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act & Assert
            Assert.Equal(16, service.BytesPerLine);
        }

        [Fact]
        public void LineHeight_DefaultValue_Is20()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act & Assert
            Assert.Equal(20, service.LineHeight);
        }

        [Fact]
        public void BufferLines_DefaultValue_Is2()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act & Assert
            Assert.Equal(2, service.BufferLines);
        }

        [Fact]
        public void BytesPerLine_CanBeSet()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            service.BytesPerLine = 32;

            // Assert
            Assert.Equal(32, service.BytesPerLine);
        }

        [Fact]
        public void LineHeight_CanBeSet()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            service.LineHeight = 30;

            // Assert
            Assert.Equal(30, service.LineHeight);
        }

        [Fact]
        public void BufferLines_CanBeSet()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            service.BufferLines = 5;

            // Assert
            Assert.Equal(5, service.BufferLines);
        }

        #endregion

        #region CalculateVisibleRange Tests

        [Fact]
        public void CalculateVisibleRange_ValidInput_ReturnsRange()
        {
            // Arrange
            var service = new VirtualizationService();
            double scrollOffset = 100;
            double viewportHeight = 400;
            long totalLines = 1000;

            // Act
            var (startLine, count) = service.CalculateVisibleRange(scrollOffset, viewportHeight, totalLines);

            // Assert
            Assert.True(startLine >= 0);
            Assert.True(count > 0);
        }

        [Fact]
        public void CalculateVisibleRange_ZeroScroll_StartsAtZero()
        {
            // Arrange
            var service = new VirtualizationService();
            double scrollOffset = 0;
            double viewportHeight = 400;
            long totalLines = 1000;

            // Act
            var (startLine, count) = service.CalculateVisibleRange(scrollOffset, viewportHeight, totalLines);

            // Assert
            Assert.Equal(0, startLine);
            Assert.True(count > 0);
        }

        [Fact]
        public void CalculateVisibleRange_NearEnd_ClampsCorrectly()
        {
            // Arrange
            var service = new VirtualizationService();
            double scrollOffset = 19800; // Near end (990 lines * 20 height)
            double viewportHeight = 400;
            long totalLines = 1000;

            // Act
            var (startLine, count) = service.CalculateVisibleRange(scrollOffset, viewportHeight, totalLines);

            // Assert
            Assert.True(startLine + count <= totalLines);
        }

        [Fact]
        public void CalculateVisibleRange_UpdatesFirstVisibleLine()
        {
            // Arrange
            var service = new VirtualizationService();
            double scrollOffset = 100;
            double viewportHeight = 400;
            long totalLines = 1000;

            // Act
            service.CalculateVisibleRange(scrollOffset, viewportHeight, totalLines);

            // Assert
            Assert.Equal(5, service.FirstVisibleLine); // 100 / 20 = 5
        }

        [Fact]
        public void CalculateVisibleRange_UpdatesVisibleLineCount()
        {
            // Arrange
            var service = new VirtualizationService();
            double scrollOffset = 0;
            double viewportHeight = 400;
            long totalLines = 1000;

            // Act
            service.CalculateVisibleRange(scrollOffset, viewportHeight, totalLines);

            // Assert
            Assert.True(service.VisibleLineCount > 0);
        }

        [Fact]
        public void CalculateVisibleRange_ZeroLineHeight_UsesSafeDefault()
        {
            // Arrange
            var service = new VirtualizationService();
            service.LineHeight = 0;
            double scrollOffset = 0;
            double viewportHeight = 400;
            long totalLines = 1000;

            // Act
            var (startLine, count) = service.CalculateVisibleRange(scrollOffset, viewportHeight, totalLines);

            // Assert
            Assert.True(count >= 0);
        }

        #endregion

        #region LineToBytePosition Tests

        [Fact]
        public void LineToBytePosition_LineZero_ReturnsZero()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var position = service.LineToBytePosition(0);

            // Assert
            Assert.Equal(0, position);
        }

        [Fact]
        public void LineToBytePosition_LineOne_Returns16()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var position = service.LineToBytePosition(1);

            // Assert
            Assert.Equal(16, position); // BytesPerLine = 16
        }

        [Fact]
        public void LineToBytePosition_CustomBytesPerLine_CalculatesCorrectly()
        {
            // Arrange
            var service = new VirtualizationService();
            service.BytesPerLine = 32;

            // Act
            var position = service.LineToBytePosition(10);

            // Assert
            Assert.Equal(320, position); // 10 * 32
        }

        #endregion

        #region BytePositionToLine Tests

        [Fact]
        public void BytePositionToLine_PositionZero_ReturnsZero()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var line = service.BytePositionToLine(0);

            // Assert
            Assert.Equal(0, line);
        }

        [Fact]
        public void BytePositionToLine_Position16_ReturnsOne()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var line = service.BytePositionToLine(16);

            // Assert
            Assert.Equal(1, line);
        }

        [Fact]
        public void BytePositionToLine_Position15_ReturnsZero()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var line = service.BytePositionToLine(15);

            // Assert
            Assert.Equal(0, line); // Still on first line
        }

        [Fact]
        public void BytePositionToLine_CustomBytesPerLine_CalculatesCorrectly()
        {
            // Arrange
            var service = new VirtualizationService();
            service.BytesPerLine = 32;

            // Act
            var line = service.BytePositionToLine(320);

            // Assert
            Assert.Equal(10, line); // 320 / 32
        }

        #endregion

        #region CalculateTotalLines Tests

        [Fact]
        public void CalculateTotalLines_ZeroLength_ReturnsZero()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var lines = service.CalculateTotalLines(0);

            // Assert
            Assert.Equal(0, lines);
        }

        [Fact]
        public void CalculateTotalLines_ExactMultiple_ReturnsCorrect()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var lines = service.CalculateTotalLines(160); // 10 * 16

            // Assert
            Assert.Equal(10, lines);
        }

        [Fact]
        public void CalculateTotalLines_NotExactMultiple_RoundsUp()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var lines = service.CalculateTotalLines(161); // 10 * 16 + 1

            // Assert
            Assert.Equal(11, lines); // Should round up
        }

        [Fact]
        public void CalculateTotalLines_OneByte_ReturnsOne()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var lines = service.CalculateTotalLines(1);

            // Assert
            Assert.Equal(1, lines);
        }

        #endregion

        #region CalculateTotalScrollHeight Tests

        [Fact]
        public void CalculateTotalScrollHeight_ZeroLines_ReturnsZero()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var height = service.CalculateTotalScrollHeight(0);

            // Assert
            Assert.Equal(0, height);
        }

        [Fact]
        public void CalculateTotalScrollHeight_100Lines_Returns2000()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var height = service.CalculateTotalScrollHeight(100);

            // Assert
            Assert.Equal(2000, height); // 100 * 20
        }

        [Fact]
        public void CalculateTotalScrollHeight_CustomLineHeight_CalculatesCorrectly()
        {
            // Arrange
            var service = new VirtualizationService();
            service.LineHeight = 30;

            // Act
            var height = service.CalculateTotalScrollHeight(100);

            // Assert
            Assert.Equal(3000, height); // 100 * 30
        }

        #endregion

        #region GetVisibleLines Tests

        [Fact]
        public void GetVisibleLines_ZeroLength_ReturnsEmpty()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var lines = service.GetVisibleLines(0, 400, 0);

            // Assert
            Assert.Empty(lines);
        }

        [Fact]
        public void GetVisibleLines_ValidInput_ReturnsLines()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var lines = service.GetVisibleLines(0, 400, 1024);

            // Assert
            Assert.NotEmpty(lines);
        }

        [Fact]
        public void GetVisibleLines_LinesHaveCorrectStructure()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var lines = service.GetVisibleLines(0, 400, 1024);
            var firstLine = lines.First();

            // Assert
            Assert.Equal(0, firstLine.LineNumber);
            Assert.Equal(0, firstLine.StartPosition);
            Assert.True(firstLine.ByteCount > 0);
            Assert.True(firstLine.ByteCount <= 16);
        }

        [Fact]
        public void GetVisibleLines_LastLinePartial_HasCorrectByteCount()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var lines = service.GetVisibleLines(0, 400, 25); // 1 full line + 9 bytes
            var lastLine = lines.Last();

            // Assert
            Assert.Equal(9, lastLine.ByteCount);
        }

        [Fact]
        public void GetVisibleLines_BufferLinesMarked_Correctly()
        {
            // Arrange
            var service = new VirtualizationService();
            service.BufferLines = 2;

            // Act
            var lines = service.GetVisibleLines(0, 400, 1024);

            // Assert
            Assert.Contains(lines, l => l.IsBuffer == false); // Some visible lines
        }

        #endregion

        #region ShouldUpdateView Tests

        [Fact]
        public void ShouldUpdateView_NoChange_ReturnsFalse()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var result = service.ShouldUpdateView(100, 100);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldUpdateView_SmallChange_ReturnsFalse()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var result = service.ShouldUpdateView(100, 105); // Less than half line (10)

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldUpdateView_LargeChange_ReturnsTrue()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var result = service.ShouldUpdateView(100, 120); // More than half line

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldUpdateView_NegativeChange_ReturnsTrue()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var result = service.ShouldUpdateView(120, 100); // Scrolling up

            // Assert
            Assert.True(result);
        }

        #endregion

        #region EstimateMemorySavings Tests

        [Fact]
        public void EstimateMemorySavings_ReturnsPositive()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var savings = service.EstimateMemorySavings(totalLines: 1000, visibleLines: 30);

            // Assert
            Assert.True(savings > 0);
        }

        [Fact]
        public void EstimateMemorySavings_MoreVisibleLines_LessSavings()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var savings1 = service.EstimateMemorySavings(totalLines: 1000, visibleLines: 20);
            var savings2 = service.EstimateMemorySavings(totalLines: 1000, visibleLines: 50);

            // Assert
            Assert.True(savings1 > savings2);
        }

        [Fact]
        public void EstimateMemorySavings_CustomBytesPerControl_CalculatesCorrectly()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var savings = service.EstimateMemorySavings(totalLines: 100, visibleLines: 10, bytesPerControl: 1000);

            // Assert
            Assert.True(savings > 0);
        }

        #endregion

        #region GetMemorySavingsText Tests

        [Fact]
        public void GetMemorySavingsText_SmallAmount_ReturnsBytes()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var text = service.GetMemorySavingsText(totalLines: 1, visibleLines: 1);

            // Assert
            Assert.Contains("bytes", text);
        }

        [Fact]
        public void GetMemorySavingsText_LargeAmount_ReturnsKBOrMB()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var text = service.GetMemorySavingsText(totalLines: 10000, visibleLines: 30);

            // Assert
            Assert.True(text.Contains("KB") || text.Contains("MB") || text.Contains("GB"));
        }

        [Fact]
        public void GetMemorySavingsText_AlwaysContainsSaved()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var text = service.GetMemorySavingsText(totalLines: 1000, visibleLines: 30);

            // Assert
            Assert.Contains("saved", text);
        }

        #endregion

        #region ScrollToPosition Tests

        [Fact]
        public void ScrollToPosition_PositionZero_ReturnsZero()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var offset = service.ScrollToPosition(bytePosition: 0, centerInView: false, viewportHeight: 400);

            // Assert
            Assert.Equal(0, offset);
        }

        [Fact]
        public void ScrollToPosition_WithoutCenter_ReturnsLineStart()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var offset = service.ScrollToPosition(bytePosition: 16, centerInView: false, viewportHeight: 400);

            // Assert
            Assert.Equal(20, offset); // Line 1 * 20 height
        }

        [Fact]
        public void ScrollToPosition_WithCenter_AdjustsForViewport()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var offset = service.ScrollToPosition(bytePosition: 160, centerInView: true, viewportHeight: 400);

            // Assert
            Assert.True(offset >= 0);
        }

        [Fact]
        public void ScrollToPosition_WithCenter_NeverNegative()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var offset = service.ScrollToPosition(bytePosition: 0, centerInView: true, viewportHeight: 400);

            // Assert
            Assert.True(offset >= 0);
        }

        #endregion

        #region GetScrollOffsetForLine Tests

        [Fact]
        public void GetScrollOffsetForLine_LineZero_ReturnsZero()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var offset = service.GetScrollOffsetForLine(0);

            // Assert
            Assert.Equal(0, offset);
        }

        [Fact]
        public void GetScrollOffsetForLine_LineOne_Returns20()
        {
            // Arrange
            var service = new VirtualizationService();

            // Act
            var offset = service.GetScrollOffsetForLine(1);

            // Assert
            Assert.Equal(20, offset);
        }

        [Fact]
        public void GetScrollOffsetForLine_CustomLineHeight_CalculatesCorrectly()
        {
            // Arrange
            var service = new VirtualizationService();
            service.LineHeight = 30;

            // Act
            var offset = service.GetScrollOffsetForLine(10);

            // Assert
            Assert.Equal(300, offset);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Workflow_LineConversion_IsReversible()
        {
            // Arrange
            var service = new VirtualizationService();
            long originalPosition = 320;

            // Act
            long line = service.BytePositionToLine(originalPosition);
            long convertedBack = service.LineToBytePosition(line);

            // Assert
            Assert.Equal(320, convertedBack); // Should round to line start
        }

        [Fact]
        public void Workflow_ScrollCalculation_WorksCorrectly()
        {
            // Arrange
            var service = new VirtualizationService();
            long fileLength = 10000;

            // Act
            long totalLines = service.CalculateTotalLines(fileLength);
            double scrollHeight = service.CalculateTotalScrollHeight(totalLines);
            var lines = service.GetVisibleLines(0, 400, fileLength);

            // Assert
            Assert.True(totalLines > 0);
            Assert.True(scrollHeight > 0);
            Assert.NotEmpty(lines);
        }

        [Fact]
        public void Workflow_VirtualizationEfficiency_ShowsSavings()
        {
            // Arrange
            var service = new VirtualizationService();
            long totalLines = 10000;
            int visibleLines = 30;

            // Act
            long savings = service.EstimateMemorySavings(totalLines, visibleLines);
            string savingsText = service.GetMemorySavingsText(totalLines, visibleLines);

            // Assert
            Assert.True(savings > 0);
            Assert.NotEmpty(savingsText);
        }

        #endregion
    }
}
