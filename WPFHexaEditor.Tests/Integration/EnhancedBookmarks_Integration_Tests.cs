//////////////////////////////////////////////
// Apache 2.0  - 2026
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfHexaEditor.Core;
using WpfHexaEditor.Models.Bookmarks;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Tests.Integration
{
    [TestClass]
    public class EnhancedBookmarks_Integration_Tests
    {
        private BookmarkService _bookmarkService;

        [TestInitialize]
        public void Setup()
        {
            _bookmarkService = new BookmarkService();
        }

        [TestMethod]
        public void AddBookmarkWithMetadata_CreatesEnhancedBookmark()
        {
            // Add enhanced bookmark
            var success = _bookmarkService.AddBookmarkWithMetadata(
                position: 100,
                description: "Test Header",
                category: "Important",
                annotation: "This is a test annotation",
                tags: new List<string> { "test", "header" }
            );

            Assert.IsTrue(success);

            // Verify bookmark was added
            var bookmark = _bookmarkService.GetBookmarkAt(100);
            Assert.IsNotNull(bookmark);
            Assert.IsInstanceOfType(bookmark, typeof(EnhancedBookmark));

            var enhanced = bookmark as EnhancedBookmark;
            Assert.AreEqual("Important", enhanced.Category);
            Assert.AreEqual("This is a test annotation", enhanced.Annotation);
            Assert.AreEqual(2, enhanced.Tags.Count);
            Assert.IsTrue(enhanced.HasTag("test"));
            Assert.IsTrue(enhanced.HasTag("header"));
        }

        [TestMethod]
        public void GetBookmarksByGroup_ReturnsCorrectBookmarks()
        {
            // Add bookmarks to different categories
            _bookmarkService.AddBookmarkWithMetadata(100, "Header", "Important");
            _bookmarkService.AddBookmarkWithMetadata(200, "Data", "Normal");
            _bookmarkService.AddBookmarkWithMetadata(300, "Footer", "Important");

            // Get Important bookmarks
            var important = _bookmarkService.GetBookmarksByGroup("Important").ToList();

            Assert.AreEqual(2, important.Count);
            Assert.IsTrue(important.Any(b => b.BytePositionInStream == 100));
            Assert.IsTrue(important.Any(b => b.BytePositionInStream == 300));
        }

        [TestMethod]
        public void UpdateBookmarkCategory_ChangesCategory()
        {
            // Add bookmark
            _bookmarkService.AddBookmarkWithMetadata(100, "Test", "Normal");

            // Update category
            var success = _bookmarkService.UpdateBookmarkCategory(100, "Important");
            Assert.IsTrue(success);

            // Verify category changed
            var bookmark = _bookmarkService.GetBookmarkAt(100) as EnhancedBookmark;
            Assert.IsNotNull(bookmark);
            Assert.AreEqual("Important", bookmark.Category);
        }

        [TestMethod]
        public void UpdateBookmarkAnnotation_ChangesAnnotation()
        {
            // Add bookmark
            _bookmarkService.AddBookmarkWithMetadata(100, "Test", "Normal", "Old annotation");

            // Update annotation
            var success = _bookmarkService.UpdateBookmarkAnnotation(100, "New annotation");
            Assert.IsTrue(success);

            // Verify annotation changed
            var bookmark = _bookmarkService.GetBookmarkAt(100) as EnhancedBookmark;
            Assert.IsNotNull(bookmark);
            Assert.AreEqual("New annotation", bookmark.Annotation);
        }

        [TestMethod]
        public void AddTagToBookmark_AddsTag()
        {
            // Add bookmark
            _bookmarkService.AddBookmarkWithMetadata(100, "Test", "Normal");

            // Add tags
            var success1 = _bookmarkService.AddTagToBookmark(100, "tag1");
            var success2 = _bookmarkService.AddTagToBookmark(100, "tag2");

            Assert.IsTrue(success1);
            Assert.IsTrue(success2);

            // Verify tags were added
            var bookmark = _bookmarkService.GetBookmarkAt(100) as EnhancedBookmark;
            Assert.IsNotNull(bookmark);
            Assert.AreEqual(2, bookmark.Tags.Count);
            Assert.IsTrue(bookmark.HasTag("tag1"));
            Assert.IsTrue(bookmark.HasTag("tag2"));
        }

        [TestMethod]
        public void RemoveTagFromBookmark_RemovesTag()
        {
            // Add bookmark with tags
            _bookmarkService.AddBookmarkWithMetadata(100, "Test", "Normal", "", new List<string> { "tag1", "tag2", "tag3" });

            // Remove tag
            var success = _bookmarkService.RemoveTagFromBookmark(100, "tag2");
            Assert.IsTrue(success);

            // Verify tag was removed
            var bookmark = _bookmarkService.GetBookmarkAt(100) as EnhancedBookmark;
            Assert.IsNotNull(bookmark);
            Assert.AreEqual(2, bookmark.Tags.Count);
            Assert.IsTrue(bookmark.HasTag("tag1"));
            Assert.IsFalse(bookmark.HasTag("tag2"));
            Assert.IsTrue(bookmark.HasTag("tag3"));
        }

        [TestMethod]
        public void GetBookmarksByTag_FindsBookmarksWithTag()
        {
            // Add bookmarks with different tags
            _bookmarkService.AddBookmarkWithMetadata(100, "B1", "Normal", "", new List<string> { "important", "header" });
            _bookmarkService.AddBookmarkWithMetadata(200, "B2", "Normal", "", new List<string> { "data" });
            _bookmarkService.AddBookmarkWithMetadata(300, "B3", "Normal", "", new List<string> { "important", "footer" });

            // Search by tag
            var results = _bookmarkService.GetBookmarksByTag("important").ToList();

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Any(b => b.BytePositionInStream == 100));
            Assert.IsTrue(results.Any(b => b.BytePositionInStream == 300));
        }

        [TestMethod]
        public void NavigateBookmarksByCategory_WorksCorrectly()
        {
            // Add bookmarks in different categories
            _bookmarkService.AddBookmarkWithMetadata(100, "I1", "Important");
            _bookmarkService.AddBookmarkWithMetadata(200, "N1", "Normal");
            _bookmarkService.AddBookmarkWithMetadata(300, "I2", "Important");
            _bookmarkService.AddBookmarkWithMetadata(400, "N2", "Normal");
            _bookmarkService.AddBookmarkWithMetadata(500, "I3", "Important");

            // Get next Important bookmark from position 150
            var next = _bookmarkService.GetNextBookmarkInCategory(150, "Important");
            Assert.IsNotNull(next);
            Assert.AreEqual(300, next.BytePositionInStream);

            // Get previous Important bookmark from position 450
            var previous = _bookmarkService.GetPreviousBookmarkInCategory(450, "Important");
            Assert.IsNotNull(previous);
            Assert.AreEqual(300, previous.BytePositionInStream);
        }

        [TestMethod]
        public void GroupManagement_RegisterAndUnregister()
        {
            // Register group
            var group = new BookmarkGroup("TestGroup", System.Windows.Media.Colors.Purple)
            {
                Description = "Test group description"
            };

            var success = _bookmarkService.RegisterGroup(group);
            Assert.IsTrue(success);

            // Verify group was registered
            var retrieved = _bookmarkService.GetGroup("TestGroup");
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("TestGroup", retrieved.Name);
            Assert.AreEqual(System.Windows.Media.Colors.Purple, retrieved.Color);

            // Unregister group
            var removed = _bookmarkService.UnregisterGroup("TestGroup");
            Assert.IsTrue(removed);

            // Verify group was removed
            var retrievedAfter = _bookmarkService.GetGroup("TestGroup");
            Assert.IsNull(retrievedAfter);
        }

        [TestMethod]
        public void GetCategoryColors_ReturnsCategoryColorsDictionary()
        {
            // Register groups with colors
            _bookmarkService.RegisterGroup(new BookmarkGroup("Cat1", System.Windows.Media.Colors.Red));
            _bookmarkService.RegisterGroup(new BookmarkGroup("Cat2", System.Windows.Media.Colors.Green));
            _bookmarkService.RegisterGroup(new BookmarkGroup("Cat3", System.Windows.Media.Colors.Blue));

            // Get category colors
            var colors = _bookmarkService.GetCategoryColors();

            Assert.AreEqual(3, colors.Count);
            Assert.AreEqual(System.Windows.Media.Colors.Red, colors["Cat1"]);
            Assert.AreEqual(System.Windows.Media.Colors.Green, colors["Cat2"]);
            Assert.AreEqual(System.Windows.Media.Colors.Blue, colors["Cat3"]);
        }
    }
}
