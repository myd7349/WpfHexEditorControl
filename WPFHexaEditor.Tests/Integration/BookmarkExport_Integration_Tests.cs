//////////////////////////////////////////////
// Apache 2.0  - 2026
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfHexaEditor.Models.Bookmarks;
using WpfHexaEditor.Services;

namespace WPFHexaEditor.Tests.Integration
{
    [TestClass]
    public class BookmarkExport_Integration_Tests
    {
        private BookmarkExportService _exportService;
        private List<EnhancedBookmark> _testBookmarks;
        private string _tempDirectory;

        [TestInitialize]
        public void Setup()
        {
            _exportService = new BookmarkExportService();

            // Create temporary directory for test files
            _tempDirectory = Path.Combine(Path.GetTempPath(), "BookmarkExportTests_" + System.Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            // Create test bookmarks
            _testBookmarks = new List<EnhancedBookmark>
            {
                new EnhancedBookmark(100, "File Header", "Important", "Contains magic bytes and version info")
                {
                    Tags = new List<string> { "header", "metadata", "version" },
                    Priority = 5,
                    CreatedBy = "TestUser",
                    CustomColor = System.Windows.Media.Colors.Red
                },
                new EnhancedBookmark(500, "Data Section", "Normal", "Main data area with compressed content")
                {
                    Tags = new List<string> { "data", "compressed" },
                    Priority = 2,
                    CreatedBy = "TestUser"
                },
                new EnhancedBookmark(1000, "Footer", "Important", "End marker with CRC32 checksum")
                {
                    Tags = new List<string> { "footer", "checksum" },
                    Priority = 4,
                    CreatedBy = "TestUser",
                    CustomColor = System.Windows.Media.Colors.Blue
                }
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Delete temporary directory
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }

        #region JSON Export/Import Tests

        [TestMethod]
        public void ExportToJson_CreatesValidJson()
        {
            var json = _exportService.ExportToJson(_testBookmarks);

            Assert.IsFalse(string.IsNullOrWhiteSpace(json));
            Assert.IsTrue(json.Contains("\"Position\": 100"));
            Assert.IsTrue(json.Contains("File Header"));
            Assert.IsTrue(json.Contains("Important"));
        }

        [TestMethod]
        public void ExportImportJson_RoundTrip_PreservesData()
        {
            // Export to JSON
            var json = _exportService.ExportToJson(_testBookmarks);

            // Import from JSON
            var result = _exportService.ImportFromJson(json);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(3, result.Bookmarks.Count);

            // Verify first bookmark
            var bookmark = result.Bookmarks.FirstOrDefault(b => b.BytePositionInStream == 100);
            Assert.IsNotNull(bookmark);
            Assert.AreEqual("File Header", bookmark.Description);
            Assert.AreEqual("Important", bookmark.Category);
            Assert.AreEqual("Contains magic bytes and version info", bookmark.Annotation);
            Assert.AreEqual(3, bookmark.Tags.Count);
            Assert.IsTrue(bookmark.HasTag("header"));
            Assert.AreEqual(5, bookmark.Priority);
        }

        [TestMethod]
        public void ExportToJsonFile_CreatesFile()
        {
            var filePath = Path.Combine(_tempDirectory, "bookmarks.json");

            _exportService.ExportToJsonFile(_testBookmarks, filePath);

            Assert.IsTrue(File.Exists(filePath));

            var content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains("File Header"));
        }

        [TestMethod]
        public void ImportFromJsonFile_ReadsFile()
        {
            var filePath = Path.Combine(_tempDirectory, "bookmarks.json");

            // Export first
            _exportService.ExportToJsonFile(_testBookmarks, filePath);

            // Import
            var result = _exportService.ImportFromJsonFile(filePath);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
        }

        #endregion

        #region XML Export/Import Tests

        [TestMethod]
        public void ExportToXml_CreatesValidXml()
        {
            var xml = _exportService.ExportToXml(_testBookmarks);

            Assert.IsFalse(string.IsNullOrWhiteSpace(xml));
            Assert.IsTrue(xml.Contains("<BookmarksExport"));
            Assert.IsTrue(xml.Contains("<Position>100</Position>"));
            Assert.IsTrue(xml.Contains("<Description>File Header</Description>"));
            Assert.IsTrue(xml.Contains("<Category>Important</Category>"));
        }

        [TestMethod]
        public void ExportImportXml_RoundTrip_PreservesData()
        {
            // Export to XML
            var xml = _exportService.ExportToXml(_testBookmarks);

            // Import from XML
            var result = _exportService.ImportFromXml(xml);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);

            // Verify bookmark details
            var bookmark = result.Bookmarks.FirstOrDefault(b => b.BytePositionInStream == 100);
            Assert.IsNotNull(bookmark);
            Assert.AreEqual("File Header", bookmark.Description);
            Assert.AreEqual("Important", bookmark.Category);
            Assert.AreEqual(3, bookmark.Tags.Count);
        }

        [TestMethod]
        public void ExportToXmlFile_CreatesFile()
        {
            var filePath = Path.Combine(_tempDirectory, "bookmarks.xml");

            _exportService.ExportToXmlFile(_testBookmarks, filePath);

            Assert.IsTrue(File.Exists(filePath));

            var content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains("<BookmarksExport"));
        }

        #endregion

        #region CSV Export Tests

        [TestMethod]
        public void ExportToCsv_CreatesValidCsv()
        {
            var csv = _exportService.ExportToCsv(_testBookmarks);

            Assert.IsFalse(string.IsNullOrWhiteSpace(csv));
            Assert.IsTrue(csv.Contains("Position,Description,Category"));
            Assert.IsTrue(csv.Contains("100,File Header,Important"));
            Assert.IsTrue(csv.Contains("500,Data Section,Normal"));
        }

        [TestMethod]
        public void ExportToCsv_WithOptions_IncludesSelectedColumns()
        {
            var options = new CsvExportOptions
            {
                IncludeAnnotation = true,
                IncludeTags = true,
                IncludePriority = true
            };

            var csv = _exportService.ExportToCsv(_testBookmarks, options);

            Assert.IsTrue(csv.Contains("Annotation"));
            Assert.IsTrue(csv.Contains("Tags"));
            Assert.IsTrue(csv.Contains("Priority"));
            Assert.IsTrue(csv.Contains("Contains magic bytes and version info"));
        }

        [TestMethod]
        public void ExportToCsvFile_CreatesFile()
        {
            var filePath = Path.Combine(_tempDirectory, "bookmarks.csv");

            _exportService.ExportToCsvFile(_testBookmarks, filePath);

            Assert.IsTrue(File.Exists(filePath));

            var content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains("Position,Description"));
        }

        #endregion

        #region Multi-Format Tests

        [TestMethod]
        public void ExportToFile_Json_CreatesJsonFile()
        {
            var filePath = Path.Combine(_tempDirectory, "test.json");

            _exportService.ExportToFile(_testBookmarks, filePath, BookmarkExportFormat.Json);

            Assert.IsTrue(File.Exists(filePath));
        }

        [TestMethod]
        public void ExportToFile_Xml_CreatesXmlFile()
        {
            var filePath = Path.Combine(_tempDirectory, "test.xml");

            _exportService.ExportToFile(_testBookmarks, filePath, BookmarkExportFormat.Xml);

            Assert.IsTrue(File.Exists(filePath));
        }

        [TestMethod]
        public void ExportToFile_Csv_CreatesCsvFile()
        {
            var filePath = Path.Combine(_tempDirectory, "test.csv");

            _exportService.ExportToFile(_testBookmarks, filePath, BookmarkExportFormat.Csv);

            Assert.IsTrue(File.Exists(filePath));
        }

        [TestMethod]
        public void ImportFromFile_Json_AutoDetectsFormat()
        {
            var filePath = Path.Combine(_tempDirectory, "bookmarks.json");

            // Export first
            _exportService.ExportToJsonFile(_testBookmarks, filePath);

            // Import with auto-detection
            var result = _exportService.ImportFromFile(filePath);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
        }

        [TestMethod]
        public void ImportFromFile_Xml_AutoDetectsFormat()
        {
            var filePath = Path.Combine(_tempDirectory, "bookmarks.xml");

            // Export first
            _exportService.ExportToXmlFile(_testBookmarks, filePath);

            // Import with auto-detection
            var result = _exportService.ImportFromFile(filePath);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Count);
        }

        #endregion
    }
}
