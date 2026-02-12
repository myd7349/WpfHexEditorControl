//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Linq;
using Xunit;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Tests.Services
{
    /// <summary>
    /// Unit tests for TblService
    /// </summary>
    public class TblServiceTests
    {
        #region Properties Tests

        [Fact]
        public void HasTable_NoTableLoaded_ReturnsFalse()
        {
            // Arrange
            var service = new TblService();

            // Act
            var hasTable = service.HasTable;

            // Assert
            Assert.False(hasTable);
        }

        [Fact]
        public void HasTable_AfterLoadDefault_ReturnsTrue()
        {
            // Arrange
            var service = new TblService();

            // Act
            service.LoadDefault();
            var hasTable = service.HasTable;

            // Assert
            Assert.True(hasTable);
        }

        [Fact]
        public void CharacterTable_NoTableLoaded_ReturnsNull()
        {
            // Arrange
            var service = new TblService();

            // Act
            var table = service.CharacterTable;

            // Assert
            Assert.Null(table);
        }

        [Fact]
        public void CharacterTable_AfterLoadDefault_ReturnsTable()
        {
            // Arrange
            var service = new TblService();

            // Act
            service.LoadDefault();
            var table = service.CharacterTable;

            // Assert
            Assert.NotNull(table);
        }

        [Fact]
        public void CurrentFileName_NoTableLoaded_ReturnsNull()
        {
            // Arrange
            var service = new TblService();

            // Act
            var fileName = service.CurrentFileName;

            // Assert
            Assert.Null(fileName);
        }

        [Fact]
        public void CurrentDefaultType_NoTableLoaded_ReturnsNull()
        {
            // Arrange
            var service = new TblService();

            // Act
            var type = service.CurrentDefaultType;

            // Assert
            Assert.Null(type);
        }

        #endregion

        #region LoadFromFile Tests

        [Fact]
        public void LoadFromFile_NullFileName_ReturnsFalse()
        {
            // Arrange
            var service = new TblService();

            // Act
            var result = service.LoadFromFile(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void LoadFromFile_EmptyFileName_ReturnsFalse()
        {
            // Arrange
            var service = new TblService();

            // Act
            var result = service.LoadFromFile(string.Empty);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void LoadFromFile_WhitespaceFileName_ReturnsFalse()
        {
            // Arrange
            var service = new TblService();

            // Act
            var result = service.LoadFromFile("   ");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void LoadFromFile_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            var service = new TblService();

            // Act
            var result = service.LoadFromFile("C:\\NonExistent\\File.tbl");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region LoadDefault Tests

        [Fact]
        public void LoadDefault_AsciiType_ReturnsTrue()
        {
            // Arrange
            var service = new TblService();

            // Act
            var result = service.LoadDefault(DefaultCharacterTableType.Ascii);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void LoadDefault_DefaultParameter_ReturnsTrue()
        {
            // Arrange
            var service = new TblService();

            // Act
            var result = service.LoadDefault();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void LoadDefault_SetsCurrentDefaultType()
        {
            // Arrange
            var service = new TblService();

            // Act
            service.LoadDefault(DefaultCharacterTableType.Ascii);

            // Assert
            Assert.Equal(DefaultCharacterTableType.Ascii, service.CurrentDefaultType);
        }

        [Fact]
        public void LoadDefault_ClearsCurrentFileName()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act & Assert
            Assert.Null(service.CurrentFileName);
        }

        [Fact]
        public void LoadDefault_ReplacesExistingTable()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault(DefaultCharacterTableType.Ascii);

            // Act
            var result = service.LoadDefault(DefaultCharacterTableType.Ascii);

            // Assert
            Assert.True(result);
            Assert.True(service.HasTable);
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_RemovesTable()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            service.Clear();

            // Assert
            Assert.False(service.HasTable);
            Assert.Null(service.CharacterTable);
        }

        [Fact]
        public void Clear_ClearsFileName()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            service.Clear();

            // Assert
            Assert.Null(service.CurrentFileName);
        }

        [Fact]
        public void Clear_ClearsDefaultType()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            service.Clear();

            // Assert
            Assert.Null(service.CurrentDefaultType);
        }

        [Fact]
        public void Clear_NoTableLoaded_DoesNotThrow()
        {
            // Arrange
            var service = new TblService();

            // Act & Assert (should not throw)
            service.Clear();
        }

        #endregion

        #region Bookmark Operations Tests

        [Fact]
        public void GetTblBookmarks_NoTableLoaded_ReturnsEmpty()
        {
            // Arrange
            var service = new TblService();

            // Act
            var bookmarks = service.GetTblBookmarks();

            // Assert
            Assert.NotNull(bookmarks);
            Assert.Empty(bookmarks);
        }

        [Fact]
        public void GetTblBookmarks_WithDefaultTable_ReturnsBookmarks()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            var bookmarks = service.GetTblBookmarks();

            // Assert
            Assert.NotNull(bookmarks);
        }

        [Fact]
        public void HasBookmarks_NoTableLoaded_ReturnsFalse()
        {
            // Arrange
            var service = new TblService();

            // Act
            var hasBookmarks = service.HasBookmarks();

            // Assert
            Assert.False(hasBookmarks);
        }

        [Fact]
        public void GetBookmarkCount_NoTableLoaded_ReturnsZero()
        {
            // Arrange
            var service = new TblService();

            // Act
            var count = service.GetBookmarkCount();

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void GetBookmarkCount_WithDefaultTable_ReturnsNonNegative()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            var count = service.GetBookmarkCount();

            // Assert
            Assert.True(count >= 0);
        }

        #endregion

        #region FindMatch Tests

        [Fact]
        public void FindMatch_NoTableLoaded_ReturnsInvalid()
        {
            // Arrange
            var service = new TblService();

            // Act
            var (text, dteType) = service.FindMatch("41");

            // Assert
            Assert.Equal("#", text);
            Assert.Equal(DteType.Invalid, dteType);
        }

        [Fact]
        public void FindMatch_WithDefaultTable_ReturnsResult()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            var (text, dteType) = service.FindMatch("41", showSpecialValue: false);

            // Assert
            Assert.NotNull(text);
        }

        [Fact]
        public void FindMatch_WithSpecialValues_ReturnsResult()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            var (text, dteType) = service.FindMatch("00", showSpecialValue: true);

            // Assert
            Assert.NotNull(text);
        }

        #endregion

        #region BytesToString Tests

        [Fact]
        public void BytesToString_NoTableLoaded_ReturnsEmpty()
        {
            // Arrange
            var service = new TblService();
            var bytes = new byte[] { 0x41, 0x42, 0x43 };

            // Act
            var result = service.BytesToString(bytes);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void BytesToString_NullBytes_ReturnsEmpty()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            var result = service.BytesToString(null);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void BytesToString_EmptyBytes_ReturnsEmpty()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            var result = service.BytesToString(new byte[0]);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void BytesToString_WithDefaultTable_ReturnsString()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();
            var bytes = new byte[] { 0x41, 0x42, 0x43 }; // ABC in ASCII

            // Act
            var result = service.BytesToString(bytes);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(string.Empty, result);
        }

        #endregion

        #region ContainsSpecialValues Tests

        [Fact]
        public void ContainsSpecialValues_NoTableLoaded_ReturnsFalse()
        {
            // Arrange
            var service = new TblService();
            var bytes = new byte[] { 0x00, 0x01, 0x02 };

            // Act
            var result = service.ContainsSpecialValues(bytes, 0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ContainsSpecialValues_NullBytes_ReturnsFalse()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            var result = service.ContainsSpecialValues(null, 0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ContainsSpecialValues_EmptyBytes_ReturnsFalse()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            var result = service.ContainsSpecialValues(new byte[0], 0);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Query Operations Tests

        [Fact]
        public void IsDefaultTable_NoTableLoaded_ReturnsFalse()
        {
            // Arrange
            var service = new TblService();

            // Act
            var result = service.IsDefaultTable();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsDefaultTable_AfterLoadDefault_ReturnsTrue()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            var result = service.IsDefaultTable();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsFileTable_NoTableLoaded_ReturnsFalse()
        {
            // Arrange
            var service = new TblService();

            // Act
            var result = service.IsFileTable();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsFileTable_AfterLoadDefault_ReturnsFalse()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            var result = service.IsFileTable();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetTableInfo_NoTableLoaded_ReturnsEmpty()
        {
            // Arrange
            var service = new TblService();

            // Act
            var info = service.GetTableInfo();

            // Assert
            Assert.Equal(string.Empty, info);
        }

        [Fact]
        public void GetTableInfo_AfterLoadDefault_ReturnsDefaultInfo()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault(DefaultCharacterTableType.Ascii);

            // Act
            var info = service.GetTableInfo();

            // Assert
            Assert.Contains("Default", info);
            Assert.Contains("Ascii", info);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_RemovesTable()
        {
            // Arrange
            var service = new TblService();
            service.LoadDefault();

            // Act
            service.Dispose();

            // Assert
            Assert.False(service.HasTable);
        }

        [Fact]
        public void Dispose_NoTableLoaded_DoesNotThrow()
        {
            // Arrange
            var service = new TblService();

            // Act & Assert (should not throw)
            service.Dispose();
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Workflow_LoadDefaultConvertClear_WorksCorrectly()
        {
            // Arrange
            var service = new TblService();
            var bytes = new byte[] { 0x41, 0x42, 0x43 }; // ABC

            // Act
            var loadResult = service.LoadDefault();
            var convertResult = service.BytesToString(bytes);
            service.Clear();

            // Assert
            Assert.True(loadResult);
            Assert.NotEqual(string.Empty, convertResult);
            Assert.False(service.HasTable);
        }

        [Fact]
        public void Workflow_LoadDefaultFindMatchClear_WorksCorrectly()
        {
            // Arrange
            var service = new TblService();

            // Act
            var loadResult = service.LoadDefault();
            var (text, dteType) = service.FindMatch("41");
            service.Clear();

            // Assert
            Assert.True(loadResult);
            Assert.NotNull(text);
            Assert.False(service.HasTable);
        }

        [Fact]
        public void Workflow_MultipleLoadDefault_WorksCorrectly()
        {
            // Arrange
            var service = new TblService();

            // Act
            var firstLoad = service.LoadDefault(DefaultCharacterTableType.Ascii);
            var firstType = service.CurrentDefaultType;
            var secondLoad = service.LoadDefault(DefaultCharacterTableType.Ascii);
            var secondType = service.CurrentDefaultType;

            // Assert
            Assert.True(firstLoad);
            Assert.True(secondLoad);
            Assert.Equal(firstType, secondType);
            Assert.True(service.HasTable);
        }

        #endregion
    }
}
