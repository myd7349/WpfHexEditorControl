//////////////////////////////////////////////
// Apache 2.0  - 2026
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributors: Claude Sonnet 4.5
//////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WpfHexaEditor.V2.ByteProvider
{
    /// <summary>
    /// Test suite for ByteProvider V2 - validates architecture and performance.
    /// Run this to verify V2 improvements over ByteProviderLegacy.
    /// </summary>
    public static class ByteProviderV2Test
    {
        /// <summary>
        /// Run all tests and output results to console.
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("ByteProvider V2 Test Suite");
            Console.WriteLine("========================================\n");

            try
            {
                Console.WriteLine("Starting Test 1...");
                TestBasicOperations();

                Console.WriteLine("Starting Test 2...");
                TestMultipleInsertions();

                Console.WriteLine("Starting Test 3...");
                TestVirtualPhysicalMapping();

                Console.WriteLine("Starting Test 4...");
                TestCachingPerformance();

                Console.WriteLine("Starting Test 5...");
                TestMemorySource();

                Console.WriteLine("\n========================================");
                Console.WriteLine("All tests completed!");
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n!!! TEST FAILED !!!");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Test 1: Basic read/write operations
        /// </summary>
        private static void TestBasicOperations()
        {
            Console.WriteLine("Test 1: Basic Operations");
            Console.WriteLine("------------------------");

            var provider = new ByteProvider();
            var testData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
            provider.OpenMemory(testData);

            // Test read
            var (value, success) = provider.GetByte(3);
            Console.WriteLine($"  Read byte at position 3: 0x{value:X2} (expected: 0x03) - {(value == 0x03 ? "✓ PASS" : "✗ FAIL")}");

            // Test modify
            provider.ModifyByte(3, 0xFF);
            (value, success) = provider.GetByte(3);
            Console.WriteLine($"  Modified byte at position 3: 0x{value:X2} (expected: 0xFF) - {(value == 0xFF ? "✓ PASS" : "✗ FAIL")}");

            // Test insert
            provider.InsertByte(4, 0xAA);
            var newLength = provider.VirtualLength;
            Console.WriteLine($"  After insert: VirtualLength = {newLength} (expected: 9) - {(newLength == 9 ? "✓ PASS" : "✗ FAIL")}");

            // Test delete
            provider.DeleteByte(0);
            newLength = provider.VirtualLength;
            Console.WriteLine($"  After delete: VirtualLength = {newLength} (expected: 8) - {(newLength == 8 ? "✓ PASS" : "✗ FAIL")}");

            provider.Dispose();
            Console.WriteLine();
        }

        /// <summary>
        /// Test 2: Multiple insertions at same physical position (V1 bug fix)
        /// </summary>
        private static void TestMultipleInsertions()
        {
            Console.WriteLine("Test 2: Multiple Insertions (V1 Bug Fix)");
            Console.WriteLine("-----------------------------------------");

            var provider = new ByteProvider();
            var testData = new byte[] { 0x00, 0x01, 0x02 };
            provider.OpenMemory(testData);

            // Insert multiple bytes at same position (this failed in V1)
            provider.InsertByte(1, 0xAA);
            provider.InsertByte(1, 0xBB);
            provider.InsertByte(1, 0xCC);

            // Virtual length should be 3 + 3 = 6
            var length = provider.VirtualLength;
            Console.WriteLine($"  VirtualLength after 3 insertions: {length} (expected: 6) - {(length == 6 ? "✓ PASS" : "✗ FAIL")}");

            // Verify byte order: 0x00, 0xCC, 0xBB, 0xAA, 0x01, 0x02
            var bytes = provider.GetBytes(0, (int)provider.VirtualLength);
            var expected = new byte[] { 0x00, 0xCC, 0xBB, 0xAA, 0x01, 0x02 };
            bool orderCorrect = bytes.SequenceEqual(expected);
            Console.WriteLine($"  Byte order: {string.Join(", ", bytes.Select(b => $"0x{b:X2}"))}");
            Console.WriteLine($"  Expected:   {string.Join(", ", expected.Select(b => $"0x{b:X2}"))}");
            Console.WriteLine($"  Order correct: {(orderCorrect ? "✓ PASS" : "✗ FAIL")}");

            provider.Dispose();
            Console.WriteLine();
        }

        /// <summary>
        /// Test 3: Virtual ↔ Physical position mapping
        /// </summary>
        private static void TestVirtualPhysicalMapping()
        {
            Console.WriteLine("Test 3: Virtual ↔ Physical Mapping");
            Console.WriteLine("-----------------------------------");

            var provider = new ByteProvider();
            var testData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };
            provider.OpenMemory(testData);

            // Insert at position 2
            provider.InsertByte(2, 0xAA);

            // Virtual position 2 should map to inserted byte (no physical position)
            // Virtual position 3 should map to physical position 2
            var (value2, _) = provider.GetByte(2);
            var (value3, _) = provider.GetByte(3);

            Console.WriteLine($"  Virtual[2] = 0x{value2:X2} (inserted, expected: 0xAA) - {(value2 == 0xAA ? "✓ PASS" : "✗ FAIL")}");
            Console.WriteLine($"  Virtual[3] = 0x{value3:X2} (physical[2], expected: 0x02) - {(value3 == 0x02 ? "✓ PASS" : "✗ FAIL")}");

            // Delete position 0
            provider.DeleteByte(0);

            // Virtual position 0 should now map to physical position 1
            var (value0, _) = provider.GetByte(0);
            Console.WriteLine($"  After delete, Virtual[0] = 0x{value0:X2} (physical[1], expected: 0x01) - {(value0 == 0x01 ? "✓ PASS" : "✗ FAIL")}");

            provider.Dispose();
            Console.WriteLine();
        }

        /// <summary>
        /// Test 4: Caching performance
        /// </summary>
        private static void TestCachingPerformance()
        {
            Console.WriteLine("Test 4: Caching Performance");
            Console.WriteLine("---------------------------");

            var provider = new ByteProvider();

            // Create 10KB test file (reduced from 1MB for faster testing)
            var testData = new byte[10 * 1024]; // 10KB
            for (int i = 0; i < testData.Length; i++)
                testData[i] = (byte)(i % 256);

            provider.OpenMemory(testData);

            // Add some modifications
            for (int i = 0; i < 1000; i += 100)
                provider.ModifyByte(i, 0xFF);

            // Benchmark: Read 1000 random bytes (reduced for faster testing)
            var random = new Random(42);
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 1000; i++)
            {
                long pos = random.Next(0, (int)provider.VirtualLength);
                provider.GetByte(pos);
            }

            stopwatch.Stop();
            Console.WriteLine($"  1000 random reads: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"  Average: {stopwatch.ElapsedMilliseconds / 1000.0:F3}ms per read");

            // Show cache statistics
            var stats = provider.GetCacheStatistics();
            Console.WriteLine("\n  Cache Statistics:");
            foreach (var line in stats.Split('\n'))
                Console.WriteLine($"    {line}");

            provider.Dispose();
            Console.WriteLine();
        }

        /// <summary>
        /// Test 5: Memory source operations
        /// </summary>
        private static void TestMemorySource()
        {
            Console.WriteLine("Test 5: Memory Source");
            Console.WriteLine("---------------------");

            var provider = new ByteProvider();
            var testData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"
            provider.OpenMemory(testData, readOnly: false);

            Console.WriteLine($"  Opened memory source: {provider.VirtualLength} bytes");
            Console.WriteLine($"  IsOpen: {provider.IsOpen} - {(provider.IsOpen ? "✓ PASS" : "✗ FAIL")}");
            Console.WriteLine($"  IsReadOnly: {provider.IsReadOnly} - {(!provider.IsReadOnly ? "✓ PASS" : "✗ FAIL")}");

            // Modify in memory
            provider.ModifyByte(0, 0x68); // 'H' -> 'h'
            var (firstByte, _) = provider.GetByte(0);
            Console.WriteLine($"  Modified first byte: 0x{firstByte:X2} (expected: 0x68) - {(firstByte == 0x68 ? "✓ PASS" : "✗ FAIL")}");

            provider.Dispose();
            Console.WriteLine();
        }

        /// <summary>
        /// Quick test method for debugging (callable from outside)
        /// </summary>
        public static void QuickTest()
        {
            Console.WriteLine("ByteProvider V2 Quick Test\n");

            var provider = new ByteProvider();
            provider.OpenMemory(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 });

            Console.WriteLine($"Initial: Length={provider.VirtualLength}");

            provider.InsertByte(2, 0xAA);
            Console.WriteLine($"After insert at 2: Length={provider.VirtualLength}");

            provider.DeleteByte(0);
            Console.WriteLine($"After delete at 0: Length={provider.VirtualLength}");

            provider.ModifyByte(1, 0xFF);
            var (value, _) = provider.GetByte(1);
            Console.WriteLine($"After modify at 1: Value=0x{value:X2}");

            Console.WriteLine($"\nHasChanges: {provider.HasChanges}");
            var (mod, ins, del) = provider.ModificationStats;
            Console.WriteLine($"Stats: {mod} modified, {ins} inserted, {del} deleted");

            provider.Dispose();
        }
    }
}
