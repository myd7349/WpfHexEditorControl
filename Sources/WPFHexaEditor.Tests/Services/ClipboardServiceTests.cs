//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.IO;
using Xunit;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.Bytes;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Tests.Services
{
    /// <summary>
    /// Unit tests for ClipboardService
    /// </summary>
    public class ClipboardServiceTests
    {
        private ByteProvider CreateTestProvider()
        {
            var provider = new ByteProvider();
            provider.Stream = new MemoryStream(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99 });
            return provider;
        }

        #region CanCopy Tests

        [Fact]
        public void CanCopy_ValidSelection_ReturnsTrue()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanCopy(5, provider);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanCopy_ZeroLength_ReturnsFalse()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanCopy(0, provider);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanCopy_NullProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ClipboardService();

            // Act
            var result = service.CanCopy(5, null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanCopy_ClosedProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = new ByteProvider(); // Not opened

            // Act
            var result = service.CanCopy(5, provider);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CanDelete Tests

        [Fact]
        public void CanDelete_ValidConditions_ReturnsTrue()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanDelete(5, provider, readOnlyMode: false, allowDeleteByte: true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanDelete_ReadOnlyMode_ReturnsFalse()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanDelete(5, provider, readOnlyMode: true, allowDeleteByte: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanDelete_DeleteNotAllowed_ReturnsFalse()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanDelete(5, provider, readOnlyMode: false, allowDeleteByte: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanDelete_ZeroLength_ReturnsFalse()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanDelete(0, provider, readOnlyMode: false, allowDeleteByte: true);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CopyToClipboard Tests

        [Fact]
        public void CopyToClipboard_DefaultMode_DoesNotThrow()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act & Assert (should not throw)
            service.CopyToClipboard(provider, 0, 5);
        }

        [Fact]
        public void CopyToClipboard_SpecifiedMode_DoesNotThrow()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act & Assert (should not throw)
            service.CopyToClipboard(provider, CopyPasteMode.HexaString, 0, 5);
        }

        [Fact]
        public void CopyToClipboard_NullProvider_DoesNotThrow()
        {
            // Arrange
            var service = new ClipboardService();

            // Act & Assert (should not throw)
            service.CopyToClipboard(null, CopyPasteMode.HexaString, 0, 5, true, null);
        }

        [Fact]
        public void CopyToClipboard_InvalidRange_DoesNotThrow()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act & Assert (should not throw)
            service.CopyToClipboard(provider, CopyPasteMode.HexaString, 10, 5, true, null); // Stop < Start
        }

        [Fact]
        public void DefaultCopyMode_CanBeSet()
        {
            // Arrange
            var service = new ClipboardService();

            // Act
            service.DefaultCopyMode = CopyPasteMode.AsciiString;

            // Assert
            Assert.Equal(CopyPasteMode.AsciiString, service.DefaultCopyMode);
        }

        #endregion

        #region CopyToStream Tests

        [Fact]
        public void CopyToStream_ValidParameters_CopiesData()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();
            var output = new MemoryStream();

            // Act
            service.CopyToStream(provider, output, 0, 5, copyChange: false);

            // Assert
            Assert.True(output.Length > 0);
        }

        [Fact]
        public void CopyToStream_NullOutput_DoesNotThrow()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act & Assert (should not throw)
            service.CopyToStream(provider, null, 0, 5, copyChange: false);
        }

        [Fact]
        public void CopyToStream_NullProvider_DoesNotThrow()
        {
            // Arrange
            var service = new ClipboardService();
            var output = new MemoryStream();

            // Act & Assert (should not throw)
            service.CopyToStream(null, output, 0, 5, copyChange: false);
        }

        [Fact]
        public void CopyToStream_InvalidRange_DoesNotThrow()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();
            var output = new MemoryStream();

            // Act & Assert (should not throw)
            service.CopyToStream(provider, output, -1, 5, copyChange: false);
        }

        #endregion

        #region GetCopyData Tests

        [Fact]
        public void GetCopyData_ValidRange_ReturnsBytes()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var data = service.GetCopyData(provider, 0, 5, copyChange: false);

            // Assert
            Assert.NotNull(data);
            Assert.True(data.Length > 0);
        }

        [Fact]
        public void GetCopyData_NullProvider_ReturnsNull()
        {
            // Arrange
            var service = new ClipboardService();

            // Act
            var data = service.GetCopyData(null, 0, 5, copyChange: false);

            // Assert
            Assert.Null(data);
        }

        [Fact]
        public void GetCopyData_InvalidRange_ReturnsNull()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var data = service.GetCopyData(provider, 10, 5, copyChange: false); // Stop < Start

            // Assert
            Assert.Null(data);
        }

        [Fact]
        public void GetCopyData_NegativeStart_ReturnsNull()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var data = service.GetCopyData(provider, -1, 5, copyChange: false);

            // Assert
            Assert.Null(data);
        }

        #endregion

        #region GetAllBytes Tests

        [Fact]
        public void GetAllBytes_ValidProvider_ReturnsAllData()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var data = service.GetAllBytes(provider, copyChange: false);

            // Assert
            Assert.NotNull(data);
            Assert.Equal(10, data.Length); // Test provider has 10 bytes
        }

        [Fact]
        public void GetAllBytes_NullProvider_ReturnsNull()
        {
            // Arrange
            var service = new ClipboardService();

            // Act
            var data = service.GetAllBytes(null, copyChange: false);

            // Assert
            Assert.Null(data);
        }

        [Fact]
        public void GetAllBytes_WithCopyChange_ReturnsData()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var data = service.GetAllBytes(provider, copyChange: true);

            // Assert
            Assert.NotNull(data);
        }

        #endregion

        #region FillWithByte Tests

        [Fact]
        public void FillWithByte_ValidParameters_DoesNotThrow()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act & Assert (should not throw)
            service.FillWithByte(provider, 0, 5, 0xFF, readOnlyMode: false);
        }

        [Fact]
        public void FillWithByte_ReadOnlyMode_DoesNothing()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();
            var originalByte = provider.GetByte(0).singleByte;

            // Act
            service.FillWithByte(provider, 0, 5, 0xFF, readOnlyMode: true);
            var afterByte = provider.GetByte(0).singleByte;

            // Assert
            Assert.Equal(originalByte, afterByte); // Should not change
        }

        [Fact]
        public void FillWithByte_NullProvider_DoesNotThrow()
        {
            // Arrange
            var service = new ClipboardService();

            // Act & Assert (should not throw)
            service.FillWithByte(null, 0, 5, 0xFF, readOnlyMode: false);
        }

        [Fact]
        public void FillWithByte_ZeroLength_DoesNotThrow()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act & Assert (should not throw)
            service.FillWithByte(provider, 0, 0, 0xFF, readOnlyMode: false);
        }

        [Fact]
        public void FillWithByte_NegativeStart_DoesNotThrow()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act & Assert (should not throw)
            service.FillWithByte(provider, -1, 5, 0xFF, readOnlyMode: false);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void ClipboardWorkflow_CopyGetData_WorksCorrectly()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var canCopy = service.CanCopy(5, provider);
            var data = service.GetCopyData(provider, 0, 5, copyChange: false);

            // Assert
            Assert.True(canCopy);
            Assert.NotNull(data);
            Assert.True(data.Length > 0);
        }

        [Fact]
        public void ClipboardWorkflow_CheckCanDeleteThenFill_WorksCorrectly()
        {
            // Arrange
            var service = new ClipboardService();
            var provider = CreateTestProvider();

            // Act
            var canDelete = service.CanDelete(5, provider, readOnlyMode: false, allowDeleteByte: true);
            service.FillWithByte(provider, 0, 3, 0x00, readOnlyMode: false);

            // Assert
            Assert.True(canDelete);
        }

        #endregion
    }
}
