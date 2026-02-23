//////////////////////////////////////////////
// Apache 2.0  - 2026
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfHexaEditor.Models.Bookmarks;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Tests.Unit
{
    [TestClass]
    public class BookmarkSearchService_Tests
    {
        private BookmarkSearchService _searchService;
        private List<EnhancedBookmark> _testBookmarks;

        [TestInitialize]
        public void Setup()
        {
            _searchService = new BookmarkSearchService();

            // Create test bookmarks
            _testBookmarks = new List<EnhancedBookmark>
            {
                new EnhancedBookmark(100, "File Header", "Important", "Contains magic bytes")
                {
                    Tags = new List<string> { "header", "metadata" },
                    Priority = 5,
                    CreatedBy = "TestUser1"
                },
                new EnhancedBookmark(500, "Data Section", "Normal", "Main data area")
                {
                    Tags = new List<string> { "data" },
                    Priority = 2,
                    CreatedBy = "TestUser1"
                },
                new EnhancedBookmark(1000, "Footer", "Important", "End of file marker")
                {
                    Tags = new List<string> { "footer", "metadata" },
                    Priority = 4,
                    CreatedBy = "TestUser2"
                },
                new EnhancedBookmark(2000, "Checksum", "Validation", "CRC32 checksum location")
                {
                    Tags = new List<string> { "validation", "checksum" },
                    Priority = 3,
                    CreatedBy = "TestUser2"
                }
            };
        }

        #region SearchByAnnotation Tests

        [TestMethod]
        public void SearchByAnnotation_FindsMatchingBookmarks()
        {
            // Search for "magic"
            var results = _searchService.SearchByAnnotation(_testBookmarks, "magic");

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(100, results[0].BytePositionInStream);
        }

        [TestMethod]
        public void SearchByAnnotation_CaseSensitive_RespectsCase()
        {
            // Search for "MAGIC" (case sensitive)
            var results = _searchService.SearchByAnnotation(_testBookmarks, "MAGIC", caseSensitive: true);

            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void SearchByAnnotation_CaseInsensitive_IgnoresCase()
        {
            // Search for "MAGIC" (case insensitive)
            var results = _searchService.SearchByAnnotation(_testBookmarks, "MAGIC", caseSensitive: false);

            Assert.AreEqual(1, results.Count);
        }

        #endregion

        #region SearchByTag Tests

        [TestMethod]
        public void SearchByTag_FindsBookmarksWithTag()
        {
            var results = _searchService.SearchByTag(_testBookmarks, "metadata");

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Any(b => b.BytePositionInStream == 100));
            Assert.IsTrue(results.Any(b => b.BytePositionInStream == 1000));
        }

        [TestMethod]
        public void SearchByTag_EmptyQuery_ReturnsEmpty()
        {
            var results = _searchService.SearchByTag(_testBookmarks, "");

            Assert.AreEqual(0, results.Count);
        }

        #endregion

        #region SearchByCategory Tests

        [TestMethod]
        public void SearchByCategory_FindsBookmarksInCategory()
        {
            var results = _searchService.SearchByCategory(_testBookmarks, "Important");

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.All(b => b.Category == "Important"));
        }

        [TestMethod]
        public void SearchByCategory_CaseInsensitive_FindsMatches()
        {
            var results = _searchService.SearchByCategory(_testBookmarks, "important", caseSensitive: false);

            Assert.AreEqual(2, results.Count);
        }

        #endregion

        #region SearchByDateRange Tests

        [TestMethod]
        public void SearchByDateRange_FindsBookmarksInRange()
        {
            var startDate = DateTime.Now.AddDays(-1);
            var endDate = DateTime.Now.AddDays(1);

            var results = _searchService.SearchByDateRange(_testBookmarks, startDate, endDate);

            Assert.AreEqual(4, results.Count); // All bookmarks created today
        }

        #endregion

        #region SearchByCreator Tests

        [TestMethod]
        public void SearchByCreator_FindsBookmarksByUser()
        {
            var results = _searchService.SearchByCreator(_testBookmarks, "TestUser1");

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.All(b => b.CreatedBy == "TestUser1"));
        }

        #endregion

        #region SearchByPriority Tests

        [TestMethod]
        public void SearchByPriority_FindsHighPriorityBookmarks()
        {
            var results = _searchService.SearchByPriority(_testBookmarks, minPriority: 4, maxPriority: 5);

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.All(b => b.Priority >= 4));
        }

        #endregion

        #region SearchByQuery Tests

        [TestMethod]
        public void SearchByQuery_SearchesMultipleFields()
        {
            // "header" appears in description and tags
            var results = _searchService.SearchByQuery(_testBookmarks, "header");

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(100, results[0].BytePositionInStream);
        }

        #endregion

        #region Advanced Search Tests

        [TestMethod]
        public void Search_ComplexCriteria_CombinesFilters()
        {
            var criteria = new BookmarkSearchCriteria
            {
                Categories = new List<string> { "Important" },
                MinPriority = 4,
                RequiredTags = new List<string> { "metadata" }
            };

            var results = _searchService.Search(_testBookmarks, criteria);

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.All(b => b.Category == "Important" && b.Priority >= 4));
        }

        [TestMethod]
        public void Search_NoCriteria_ReturnsAll()
        {
            var results = _searchService.Search(_testBookmarks, new BookmarkSearchCriteria());

            Assert.AreEqual(4, results.Count);
        }

        #endregion

        #region Sort Tests

        [TestMethod]
        public void SortBookmarks_ByPosition_SortsCorrectly()
        {
            var sorted = _searchService.SortBookmarks(_testBookmarks, BookmarkSortCriteria.Position, descending: false);

            Assert.AreEqual(100, sorted[0].BytePositionInStream);
            Assert.AreEqual(500, sorted[1].BytePositionInStream);
            Assert.AreEqual(1000, sorted[2].BytePositionInStream);
            Assert.AreEqual(2000, sorted[3].BytePositionInStream);
        }

        [TestMethod]
        public void SortBookmarks_ByPriority_SortsCorrectly()
        {
            var sorted = _searchService.SortBookmarks(_testBookmarks, BookmarkSortCriteria.Priority, descending: true);

            Assert.AreEqual(5, sorted[0].Priority);
            Assert.AreEqual(4, sorted[1].Priority);
            Assert.AreEqual(3, sorted[2].Priority);
            Assert.AreEqual(2, sorted[3].Priority);
        }

        [TestMethod]
        public void SortBookmarks_ByCategory_SortsAlphabetically()
        {
            var sorted = _searchService.SortBookmarks(_testBookmarks, BookmarkSortCriteria.Category, descending: false);

            Assert.AreEqual("Important", sorted[0].Category);
            Assert.AreEqual("Important", sorted[1].Category);
            Assert.AreEqual("Normal", sorted[2].Category);
            Assert.AreEqual("Validation", sorted[3].Category);
        }

        #endregion
    }
}
