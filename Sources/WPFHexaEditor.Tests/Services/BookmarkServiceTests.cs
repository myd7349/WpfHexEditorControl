//////////////////////////////////////////////
// Apache 2.0  - 2016-2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Linq;
using Xunit;
using WpfHexaEditor.Core;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Tests.Services
{
    /// <summary>
    /// Unit tests for BookmarkService
    /// </summary>
    public class BookmarkServiceTests
    {
        #region Add Tests

        [Fact]
        public void AddBookmark_ValidPosition_ReturnsTrue()
        {
            // Arrange
            var service = new BookmarkService();

            // Act
            var result = service.AddBookmark(100, "Test bookmark");

            // Assert
            Assert.True(result);
            Assert.True(service.HasBookmarkAt(100));
        }

        [Fact]
        public void AddBookmark_NegativePosition_ReturnsFalse()
        {
            // Arrange
            var service = new BookmarkService();

            // Act
            var result = service.AddBookmark(-1, "Invalid");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddBookmark_DuplicatePosition_ReturnsFalse()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "First");

            // Act
            var result = service.AddBookmark(100, "Duplicate");

            // Assert
            Assert.False(result);
            Assert.Equal(1, service.GetBookmarkCount());
        }

        [Fact]
        public void AddBookmark_DifferentMarkerTypes_ReturnsTrue()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Bookmark", ScrollMarker.Bookmark);

            // Act
            var result = service.AddBookmark(100, "SearchHighLight", ScrollMarker.SearchHighLight);

            // Assert
            Assert.True(result);
            Assert.Equal(2, service.GetBookmarkCount());
        }

        [Fact]
        public void AddBookmark_Object_ValidBookmark_ReturnsTrue()
        {
            // Arrange
            var service = new BookmarkService();
            var bookmark = new BookMark("Test", 100, ScrollMarker.Bookmark);

            // Act
            var result = service.AddBookmark(bookmark);

            // Assert
            Assert.True(result);
            Assert.True(service.HasBookmarkAt(100));
        }

        [Fact]
        public void AddBookmark_NullObject_ReturnsFalse()
        {
            // Arrange
            var service = new BookmarkService();

            // Act
            var result = service.AddBookmark(null);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Remove Tests

        [Fact]
        public void RemoveBookmark_ExistingPosition_ReturnsCount()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Test");

            // Act
            var count = service.RemoveBookmark(100);

            // Assert
            Assert.Equal(1, count);
            Assert.False(service.HasBookmarkAt(100));
        }

        [Fact]
        public void RemoveBookmark_NonExistentPosition_ReturnsZero()
        {
            // Arrange
            var service = new BookmarkService();

            // Act
            var count = service.RemoveBookmark(999);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void RemoveBookmark_WithMarkerFilter_RemovesOnlyMatchingMarker()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Bookmark", ScrollMarker.Bookmark);
            service.AddBookmark(100, "SearchHighLight", ScrollMarker.SearchHighLight);

            // Act
            var count = service.RemoveBookmark(100, ScrollMarker.Bookmark);

            // Assert
            Assert.Equal(1, count);
            Assert.True(service.HasBookmarkAt(100, ScrollMarker.SearchHighLight));
            Assert.False(service.HasBookmarkAt(100, ScrollMarker.Bookmark));
        }

        [Fact]
        public void RemoveAllBookmarks_WithMarker_RemovesOnlyThatType()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Bookmark1", ScrollMarker.Bookmark);
            service.AddBookmark(200, "Bookmark2", ScrollMarker.Bookmark);
            service.AddBookmark(300, "SearchHighLight", ScrollMarker.SearchHighLight);

            // Act
            var count = service.RemoveAllBookmarks(ScrollMarker.Bookmark);

            // Assert
            Assert.Equal(2, count);
            Assert.Equal(1, service.GetBookmarkCount());
            Assert.True(service.HasBookmarkAt(300));
        }

        [Fact]
        public void ClearAll_RemovesAllBookmarks()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Test1");
            service.AddBookmark(200, "Test2");
            service.AddBookmark(300, "Test3");

            // Act
            var count = service.ClearAll();

            // Assert
            Assert.Equal(3, count);
            Assert.Equal(0, service.GetBookmarkCount());
            Assert.False(service.HasBookmarks());
        }

        #endregion

        #region Query Tests

        [Fact]
        public void GetAllBookmarks_ReturnsAllBookmarks()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Test1");
            service.AddBookmark(200, "Test2");
            service.AddBookmark(300, "Test3");

            // Act
            var bookmarks = service.GetAllBookmarks().ToList();

            // Assert
            Assert.Equal(3, bookmarks.Count);
        }

        [Fact]
        public void GetBookmarksByMarker_FiltersCorrectly()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Bookmark1", ScrollMarker.Bookmark);
            service.AddBookmark(200, "Bookmark2", ScrollMarker.Bookmark);
            service.AddBookmark(300, "SearchHighLight", ScrollMarker.SearchHighLight);

            // Act
            var bookmarks = service.GetBookmarksByMarker(ScrollMarker.Bookmark).ToList();

            // Assert
            Assert.Equal(2, bookmarks.Count);
            Assert.All(bookmarks, b => Assert.Equal(ScrollMarker.Bookmark, b.Marker));
        }

        [Fact]
        public void GetBookmarkAt_ExistingPosition_ReturnsBookmark()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Test bookmark");

            // Act
            var bookmark = service.GetBookmarkAt(100);

            // Assert
            Assert.NotNull(bookmark);
            Assert.Equal(100, bookmark.BytePositionInStream);
            Assert.Equal("Test bookmark", bookmark.Description);
        }

        [Fact]
        public void GetBookmarkAt_NonExistentPosition_ReturnsNull()
        {
            // Arrange
            var service = new BookmarkService();

            // Act
            var bookmark = service.GetBookmarkAt(999);

            // Assert
            Assert.Null(bookmark);
        }

        [Fact]
        public void HasBookmarkAt_ExistingPosition_ReturnsTrue()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Test");

            // Act
            var result = service.HasBookmarkAt(100);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasBookmarks_WithBookmarks_ReturnsTrue()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Test");

            // Act
            var result = service.HasBookmarks();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasBookmarks_Empty_ReturnsFalse()
        {
            // Arrange
            var service = new BookmarkService();

            // Act
            var result = service.HasBookmarks();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetBookmarkCount_ReturnsCorrectCount()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Test1");
            service.AddBookmark(200, "Test2");

            // Act
            var count = service.GetBookmarkCount();

            // Assert
            Assert.Equal(2, count);
        }

        #endregion

        #region Navigation Tests

        [Fact]
        public void GetNextBookmark_ReturnsNextBookmark()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "First");
            service.AddBookmark(200, "Second");
            service.AddBookmark(300, "Third");

            // Act
            var next = service.GetNextBookmark(150);

            // Assert
            Assert.NotNull(next);
            Assert.Equal(200, next.BytePositionInStream);
        }

        [Fact]
        public void GetNextBookmark_NoNextBookmark_ReturnsNull()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "First");

            // Act
            var next = service.GetNextBookmark(200);

            // Assert
            Assert.Null(next);
        }

        [Fact]
        public void GetPreviousBookmark_ReturnsPreviousBookmark()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "First");
            service.AddBookmark(200, "Second");
            service.AddBookmark(300, "Third");

            // Act
            var prev = service.GetPreviousBookmark(250);

            // Assert
            Assert.NotNull(prev);
            Assert.Equal(200, prev.BytePositionInStream);
        }

        [Fact]
        public void GetPreviousBookmark_NoPreviousBookmark_ReturnsNull()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "First");

            // Act
            var prev = service.GetPreviousBookmark(50);

            // Assert
            Assert.Null(prev);
        }

        #endregion

        #region Update Tests

        [Fact]
        public void UpdateBookmarkDescription_ExistingBookmark_ReturnsTrue()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Original");

            // Act
            var result = service.UpdateBookmarkDescription(100, "Updated");

            // Assert
            Assert.True(result);
            var bookmark = service.GetBookmarkAt(100);
            Assert.Equal("Updated", bookmark.Description);
        }

        [Fact]
        public void UpdateBookmarkDescription_NonExistentBookmark_ReturnsFalse()
        {
            // Arrange
            var service = new BookmarkService();

            // Act
            var result = service.UpdateBookmarkDescription(999, "Test");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void UpdateBookmarkDescription_NullDescription_SetsEmpty()
        {
            // Arrange
            var service = new BookmarkService();
            service.AddBookmark(100, "Original");

            // Act
            service.UpdateBookmarkDescription(100, null);

            // Assert
            var bookmark = service.GetBookmarkAt(100);
            Assert.Equal(string.Empty, bookmark.Description);
        }

        #endregion
    }
}
