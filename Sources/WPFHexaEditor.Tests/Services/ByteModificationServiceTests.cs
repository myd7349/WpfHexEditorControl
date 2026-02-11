//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.IO;
using Xunit;
using WpfHexaEditor.Core.Bytes;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Tests.Services
{
    /// <summary>
    /// Unit tests for ByteModificationService
    /// </summary>
    public class ByteModificationServiceTests
    {
        private ByteProvider CreateTestProvider()
        {
            var provider = new ByteProvider();
            provider.Stream = new MemoryStream(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99 });
            return provider;
        }

        #region ModifyByte Tests

        [Fact]
        public void ModifyByte_ValidParameters_ReturnsTrue()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.ModifyByte(provider, 0xFF, 0, undoLength: 1, readOnlyMode: false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ModifyByte_NullProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();

            // Act
            var result = service.ModifyByte(null, 0xFF, 0, undoLength: 1, readOnlyMode: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ModifyByte_ClosedProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = new ByteProvider(); // Not opened

            // Act
            var result = service.ModifyByte(provider, 0xFF, 0, undoLength: 1, readOnlyMode: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ModifyByte_ReadOnlyMode_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.ModifyByte(provider, 0xFF, 0, undoLength: 1, readOnlyMode: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ModifyByte_ProviderReadOnlyMode_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();
            provider.ReadOnlyMode = true;

            // Act
            var result = service.ModifyByte(provider, 0xFF, 0, undoLength: 1, readOnlyMode: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ModifyByte_NegativePosition_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.ModifyByte(provider, 0xFF, -1, undoLength: 1, readOnlyMode: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ModifyByte_PositionBeyondLength_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.ModifyByte(provider, 0xFF, provider.Length, undoLength: 1, readOnlyMode: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ModifyByte_NullByte_ReturnsTrue()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.ModifyByte(provider, null, 0, undoLength: 1, readOnlyMode: false);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region InsertByte Tests (Single)

        [Fact]
        public void InsertByte_ValidParameters_ReturnsTrue()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.InsertByte(provider, 0xFF, 0, canInsertAnywhere: true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void InsertByte_NullProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();

            // Act
            var result = service.InsertByte(null, 0xFF, 0, canInsertAnywhere: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InsertByte_ClosedProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = new ByteProvider(); // Not opened

            // Act
            var result = service.InsertByte(provider, 0xFF, 0, canInsertAnywhere: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InsertByte_CannotInsertAnywhere_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.InsertByte(provider, 0xFF, 0, canInsertAnywhere: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InsertByte_NegativePosition_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.InsertByte(provider, 0xFF, -1, canInsertAnywhere: true);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region InsertByte Tests (Multiple)

        [Fact]
        public void InsertByte_MultipleValid_ReturnsTrue()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.InsertByte(provider, 0xFF, 0, length: 5, canInsertAnywhere: true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void InsertByte_MultipleNullProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();

            // Act
            var result = service.InsertByte(null, 0xFF, 0, length: 5, canInsertAnywhere: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InsertByte_MultipleCannotInsert_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.InsertByte(provider, 0xFF, 0, length: 5, canInsertAnywhere: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InsertByte_MultipleNegativePosition_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.InsertByte(provider, 0xFF, -1, length: 5, canInsertAnywhere: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InsertByte_MultipleZeroLength_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.InsertByte(provider, 0xFF, 0, length: 0, canInsertAnywhere: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InsertByte_MultipleNegativeLength_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.InsertByte(provider, 0xFF, 0, length: -1, canInsertAnywhere: true);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region InsertBytes Tests

        [Fact]
        public void InsertBytes_ValidParameters_ReturnsCount()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();
            var bytes = new byte[] { 0xAA, 0xBB, 0xCC };

            // Act
            var count = service.InsertBytes(provider, bytes, 0, canInsertAnywhere: true);

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void InsertBytes_NullProvider_ReturnsZero()
        {
            // Arrange
            var service = new ByteModificationService();
            var bytes = new byte[] { 0xAA, 0xBB, 0xCC };

            // Act
            var count = service.InsertBytes(null, bytes, 0, canInsertAnywhere: true);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void InsertBytes_ClosedProvider_ReturnsZero()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = new ByteProvider(); // Not opened
            var bytes = new byte[] { 0xAA, 0xBB, 0xCC };

            // Act
            var count = service.InsertBytes(provider, bytes, 0, canInsertAnywhere: true);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void InsertBytes_CannotInsert_ReturnsZero()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();
            var bytes = new byte[] { 0xAA, 0xBB, 0xCC };

            // Act
            var count = service.InsertBytes(provider, bytes, 0, canInsertAnywhere: false);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void InsertBytes_NullArray_ReturnsZero()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var count = service.InsertBytes(provider, null, 0, canInsertAnywhere: true);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void InsertBytes_EmptyArray_ReturnsZero()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();
            var bytes = new byte[0];

            // Act
            var count = service.InsertBytes(provider, bytes, 0, canInsertAnywhere: true);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void InsertBytes_NegativePosition_ReturnsZero()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();
            var bytes = new byte[] { 0xAA, 0xBB, 0xCC };

            // Act
            var count = service.InsertBytes(provider, bytes, -1, canInsertAnywhere: true);

            // Assert
            Assert.Equal(0, count);
        }

        #endregion

        #region DeleteBytes Tests

        [Fact]
        public void DeleteBytes_ValidParameters_ReturnsLastPosition()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var lastPosition = service.DeleteBytes(provider, 0, length: 3, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.True(lastPosition >= 0);
        }

        [Fact]
        public void DeleteBytes_NullProvider_ReturnsMinusOne()
        {
            // Arrange
            var service = new ByteModificationService();

            // Act
            var lastPosition = service.DeleteBytes(null, 0, length: 3, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.Equal(-1, lastPosition);
        }

        [Fact]
        public void DeleteBytes_ClosedProvider_ReturnsMinusOne()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = new ByteProvider(); // Not opened

            // Act
            var lastPosition = service.DeleteBytes(provider, 0, length: 3, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.Equal(-1, lastPosition);
        }

        [Fact]
        public void DeleteBytes_ReadOnlyMode_ReturnsMinusOne()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var lastPosition = service.DeleteBytes(provider, 0, length: 3, readOnlyMode: true, allowDelete: true);

            // Assert
            Assert.Equal(-1, lastPosition);
        }

        [Fact]
        public void DeleteBytes_ProviderReadOnlyMode_ReturnsMinusOne()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();
            provider.ReadOnlyMode = true;

            // Act
            var lastPosition = service.DeleteBytes(provider, 0, length: 3, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.Equal(-1, lastPosition);
        }

        [Fact]
        public void DeleteBytes_DeleteNotAllowed_ReturnsMinusOne()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var lastPosition = service.DeleteBytes(provider, 0, length: 3, readOnlyMode: false, allowDelete: false);

            // Assert
            Assert.Equal(-1, lastPosition);
        }

        [Fact]
        public void DeleteBytes_NegativePosition_ReturnsMinusOne()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var lastPosition = service.DeleteBytes(provider, -1, length: 3, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.Equal(-1, lastPosition);
        }

        [Fact]
        public void DeleteBytes_PositionBeyondLength_ReturnsMinusOne()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var lastPosition = service.DeleteBytes(provider, provider.Length, length: 3, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.Equal(-1, lastPosition);
        }

        [Fact]
        public void DeleteBytes_ZeroLength_ReturnsMinusOne()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var lastPosition = service.DeleteBytes(provider, 0, length: 0, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.Equal(-1, lastPosition);
        }

        [Fact]
        public void DeleteBytes_NegativeLength_ReturnsMinusOne()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var lastPosition = service.DeleteBytes(provider, 0, length: -1, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.Equal(-1, lastPosition);
        }

        #endregion

        #region DeleteRange Tests

        [Fact]
        public void DeleteRange_ValidParameters_ReturnsLastPosition()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var lastPosition = service.DeleteRange(provider, startPosition: 2, stopPosition: 5, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.True(lastPosition >= 0);
        }

        [Fact]
        public void DeleteRange_InvertedRange_AutoCorrects()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act (stop < start, should auto-correct)
            var lastPosition = service.DeleteRange(provider, startPosition: 5, stopPosition: 2, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.True(lastPosition >= 0);
        }

        [Fact]
        public void DeleteRange_NullProvider_ReturnsMinusOne()
        {
            // Arrange
            var service = new ByteModificationService();

            // Act
            var lastPosition = service.DeleteRange(null, startPosition: 2, stopPosition: 5, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.Equal(-1, lastPosition);
        }

        [Fact]
        public void DeleteRange_NegativeStart_ReturnsMinusOne()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var lastPosition = service.DeleteRange(provider, startPosition: -1, stopPosition: 5, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.Equal(-1, lastPosition);
        }

        [Fact]
        public void DeleteRange_NegativeStop_ReturnsMinusOne()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var lastPosition = service.DeleteRange(provider, startPosition: 2, stopPosition: -1, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.Equal(-1, lastPosition);
        }

        #endregion

        #region CanModify Tests

        [Fact]
        public void CanModify_ValidProvider_ReturnsTrue()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanModify(provider, readOnlyMode: false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanModify_NullProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();

            // Act
            var result = service.CanModify(null, readOnlyMode: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanModify_ClosedProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = new ByteProvider(); // Not opened

            // Act
            var result = service.CanModify(provider, readOnlyMode: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanModify_ReadOnlyMode_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanModify(provider, readOnlyMode: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanModify_ProviderReadOnlyMode_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();
            provider.ReadOnlyMode = true;

            // Act
            var result = service.CanModify(provider, readOnlyMode: false);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CanInsert Tests

        [Fact]
        public void CanInsert_ValidProvider_ReturnsTrue()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanInsert(provider, canInsertAnywhere: true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanInsert_NullProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();

            // Act
            var result = service.CanInsert(null, canInsertAnywhere: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanInsert_ClosedProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = new ByteProvider(); // Not opened

            // Act
            var result = service.CanInsert(provider, canInsertAnywhere: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanInsert_CannotInsertAnywhere_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanInsert(provider, canInsertAnywhere: false);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CanDelete Tests

        [Fact]
        public void CanDelete_ValidProvider_ReturnsTrue()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanDelete(provider, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanDelete_NullProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();

            // Act
            var result = service.CanDelete(null, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanDelete_ClosedProvider_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = new ByteProvider(); // Not opened

            // Act
            var result = service.CanDelete(provider, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanDelete_ReadOnlyMode_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanDelete(provider, readOnlyMode: true, allowDelete: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanDelete_ProviderReadOnlyMode_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();
            provider.ReadOnlyMode = true;

            // Act
            var result = service.CanDelete(provider, readOnlyMode: false, allowDelete: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanDelete_DeleteNotAllowed_ReturnsFalse()
        {
            // Arrange
            var service = new ByteModificationService();
            var provider = CreateTestProvider();

            // Act
            var result = service.CanDelete(provider, readOnlyMode: false, allowDelete: false);

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}
