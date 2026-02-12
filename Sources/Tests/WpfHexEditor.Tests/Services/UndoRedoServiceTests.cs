//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using Xunit;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.Bytes;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Tests.Services
{
    /// <summary>
    /// Unit tests for UndoRedoService
    /// </summary>
    public class UndoRedoServiceTests
    {
        private ByteProvider CreateTestProvider()
        {
            var provider = new ByteProvider();
            provider.Stream = new System.IO.MemoryStream(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 });
            return provider;
        }

        #region Undo Tests

        [Fact]
        public void Undo_WithModifications_ReturnsPosition()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();
            provider.AddByteModified(0xFF, 0); // byte value, position

            // Act
            var result = service.Undo(provider);

            // Assert
            Assert.Equal(0, result); // Returns position of undone byte
        }

        [Fact]
        public void Undo_WithNoModifications_ReturnsNegative()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();

            // Act
            var result = service.Undo(provider);

            // Assert
            Assert.True(result < 0); // Returns -1 when no undo available
        }

        [Fact]
        public void Undo_WithNullProvider_ReturnsNegative()
        {
            // Arrange
            var service = new UndoRedoService();

            // Act
            var result = service.Undo(null);

            // Assert
            Assert.True(result < 0); // Returns -1
        }

        [Fact]
        public void Undo_RestoresByteValue()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();
            var originalByte = provider.GetByte(0).singleByte;
            provider.AddByteModified(0xFF, 0); // Modify to 0xFF

            // Act
            service.Undo(provider);
            var (restoredByte, success) = provider.GetByte(0);

            // Assert
            Assert.True(success);
            Assert.Equal(originalByte, restoredByte);
        }

        #endregion

        #region Redo Tests

        [Fact]
        public void Redo_AfterUndo_ReturnsPosition()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();
            provider.AddByteModified(0xFF, 0);
            service.Undo(provider);

            // Act
            var result = service.Redo(provider);

            // Assert
            Assert.Equal(0, result); // Returns position of redone byte
        }

        [Fact]
        public void Redo_WithNoUndo_ReturnsNegative()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();

            // Act
            var result = service.Redo(provider);

            // Assert
            Assert.True(result < 0); // Returns -1
        }

        [Fact]
        public void Redo_WithNullProvider_ReturnsNegative()
        {
            // Arrange
            var service = new UndoRedoService();

            // Act
            var result = service.Redo(null);

            // Assert
            Assert.True(result < 0); // Returns -1
        }

        [Fact]
        public void Redo_RestoresModifiedValue()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();
            provider.AddByteModified(0xFF, 0);
            service.Undo(provider);

            // Act
            service.Redo(provider);
            var (byteValue, success) = provider.GetByte(0);

            // Assert
            Assert.True(success);
            Assert.Equal(0xFF, byteValue.Value);
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void ClearAll_ClearsUndoAndRedo()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();
            provider.AddByteModified(0xFF, 0);
            service.Undo(provider);

            // Act
            service.ClearAll(provider);

            // Assert
            Assert.False(service.CanUndo(provider));
            Assert.False(service.CanRedo(provider));
        }

        [Fact]
        public void ClearAll_WithNullProvider_DoesNotThrow()
        {
            // Arrange
            var service = new UndoRedoService();

            // Act & Assert (should not throw)
            service.ClearAll(null);
        }

        #endregion

        #region Query Tests

        [Fact]
        public void CanUndo_WithModifications_ReturnsTrue()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();
            provider.AddByteModified(0xFF, 0);

            // Act
            var result = service.CanUndo(provider);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanUndo_WithNoModifications_ReturnsFalse()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();

            // Act
            var result = service.CanUndo(provider);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanRedo_AfterUndo_ReturnsTrue()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();
            provider.AddByteModified(0xFF, 0);
            service.Undo(provider);

            // Act
            var result = service.CanRedo(provider);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetUndoCount_ReturnsCorrectCount()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();
            provider.AddByteModified(0xFF, 0);
            provider.AddByteModified(0xAA, 1);

            // Act
            var count = service.GetUndoCount(provider);

            // Assert
            Assert.Equal(2, count);
        }

        [Fact]
        public void GetRedoCount_AfterUndo_ReturnsCorrectCount()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();
            provider.AddByteModified(0xFF, 0);
            provider.AddByteModified(0xAA, 1);
            service.Undo(provider);

            // Act
            var count = service.GetRedoCount(provider);

            // Assert
            Assert.Equal(1, count);
        }

        #endregion

        #region Complex Scenarios

        [Fact]
        public void MultipleUndoRedo_WorksCorrectly()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();
            var byte0 = provider.GetByte(0).singleByte.Value;
            var byte1 = provider.GetByte(1).singleByte.Value;
            var byte2 = provider.GetByte(2).singleByte.Value;

            provider.AddByteModified(0xFF, 0);
            provider.AddByteModified(0xAA, 1);
            provider.AddByteModified(0xBB, 2);

            // Act - Undo all
            service.Undo(provider, repeat: 3);

            // Assert - All restored
            Assert.Equal(byte0, provider.GetByte(0).singleByte.Value);
            Assert.Equal(byte1, provider.GetByte(1).singleByte.Value);
            Assert.Equal(byte2, provider.GetByte(2).singleByte.Value);

            // Act - Redo all
            service.Redo(provider, repeat: 3);

            // Assert - All modified again
            Assert.Equal(0xFF, provider.GetByte(0).singleByte.Value);
            Assert.Equal(0xAA, provider.GetByte(1).singleByte.Value);
            Assert.Equal(0xBB, provider.GetByte(2).singleByte.Value);
        }

        [Fact]
        public void UndoRedo_AfterClear_StartsEmpty()
        {
            // Arrange
            var provider = CreateTestProvider();
            var service = new UndoRedoService();
            provider.AddByteModified(0xFF, 0);
            service.Undo(provider);

            // Act
            service.ClearAll(provider);

            // Assert
            Assert.Equal(0, service.GetUndoCount(provider));
            Assert.Equal(0, service.GetRedoCount(provider));
            Assert.False(service.CanUndo(provider));
            Assert.False(service.CanRedo(provider));
        }

        #endregion
    }
}
