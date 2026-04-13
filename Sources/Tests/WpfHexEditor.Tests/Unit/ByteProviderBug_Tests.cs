// ==========================================================
// Project: WpfHexEditor.Tests
// File: ByteProviderBug_Tests.cs
// Description:
//     Regression tests for P0 bugs in ByteProvider / PositionMapper / EditsManager.
//     Covers: LIFO offset calculation for insertions, deletion of inserted bytes,
//     batch deletion position shifts, VirtualToPhysical skipping deleted bytes,
//     insertion integrity after removal, and ByteReader round-trip reads.
// ==========================================================

using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfHexEditor.Core.Bytes;

namespace WpfHexEditor.Tests.Unit
{
    [TestClass]
    public sealed class ByteProviderBug_Tests
    {
        // ── Helpers ────────────────────────────────────────────────────────────

        private static ByteProvider CreateProvider(byte[] data)
        {
            var provider = new ByteProvider();
            provider.OpenMemory(data);
            return provider;
        }

        // ── Insertion LIFO offset ──────────────────────────────────────────────

        /// <summary>
        /// Insert 3 bytes before physical pos 0, then modify the middle one.
        /// virtualOffset formula: N-1-relativePos must stay in [0, N).
        /// </summary>
        [TestMethod]
        public void ModifyInsertedByte_Valid_NoException()
        {
            var provider = CreateProvider(new byte[] { 0x00, 0x11, 0x22 });

            // Insert 3 bytes at virtual position 0 (before physical 0x00)
            provider.InsertByte(0, 0xAA);
            provider.InsertByte(0, 0xBB);
            provider.InsertByte(0, 0xCC);
            // Virtual layout: [0xAA(oldest), 0xBB, 0xCC(newest), 0x00, 0x11, 0x22]
            //                   virt 0         1     2             3     4     5

            // Modify the middle inserted byte (virtual pos 1 = 0xBB)
            provider.ModifyByte(1, 0xDD);

            var result = provider.GetByte(1);
            Assert.IsTrue(result.success, "GetByte on modified inserted byte should succeed");
            Assert.AreEqual((byte)0xDD, result.value, "Modified inserted byte should return new value");
        }

        /// <summary>
        /// Verifies LIFO ordering: oldest insertion is at the lowest virtual position,
        /// newest at the highest virtual position before the physical byte.
        /// </summary>
        [TestMethod]
        public void InsertedBytes_LIFO_Order_IsCorrect()
        {
            var provider = CreateProvider(new byte[] { 0xFF });

            // Insert oldest first, newest last — all at virtual pos 0
            provider.InsertByte(0, 0x01); // oldest
            provider.InsertByte(0, 0x02);
            provider.InsertByte(0, 0x03); // newest

            // Virtual layout: [0x01(oldest/virt0), 0x02(virt1), 0x03(newest/virt2), 0xFF(virt3)]
            Assert.AreEqual((byte)0x01, provider.GetByte(0).value, "Oldest insert at virtual 0");
            Assert.AreEqual((byte)0x02, provider.GetByte(1).value, "Middle insert at virtual 1");
            Assert.AreEqual((byte)0x03, provider.GetByte(2).value, "Newest insert at virtual 2");
            Assert.AreEqual((byte)0xFF, provider.GetByte(3).value, "Physical byte at virtual 3");
        }

        // ── Deletion of inserted bytes ─────────────────────────────────────────

        /// <summary>
        /// Insert 2 bytes then delete the first — EditsManager integrity must hold.
        /// </summary>
        [TestMethod]
        public void DeleteInsertedByte_Valid_IntegrityOk()
        {
            var provider = CreateProvider(new byte[] { 0xAA, 0xBB });

            provider.InsertByte(0, 0x01); // oldest, virt 0
            provider.InsertByte(0, 0x02); // newest, virt 1
            // Virtual: [0x01, 0x02, 0xAA, 0xBB]

            // Delete virt 0 (oldest inserted byte 0x01) — must not throw
            provider.DeleteByte(0);

            // Virtual after deletion: [0x02, 0xAA, 0xBB]
            Assert.AreEqual((byte)0x02, provider.GetByte(0).value, "Remaining insert at virt 0");
            Assert.AreEqual((byte)0xAA, provider.GetByte(1).value, "Physical byte at virt 1");
        }

        /// <summary>
        /// Deleting all inserted bytes at a position must leave only physical bytes.
        /// </summary>
        [TestMethod]
        public void DeleteAllInsertedBytesAtPosition_LeavesPhysicalByte()
        {
            var provider = CreateProvider(new byte[] { 0x55 });

            provider.InsertByte(0, 0xAA);
            provider.InsertByte(0, 0xBB);
            // Virtual: [0xAA, 0xBB, 0x55]

            provider.DeleteByte(0); // delete 0xAA
            provider.DeleteByte(0); // delete 0xBB

            // Only physical byte remains
            Assert.AreEqual(1L, provider.Length, "Only physical byte remains");
            Assert.AreEqual((byte)0x55, provider.GetByte(0).value, "Physical byte accessible");
        }

        // ── Batch deletion position shift ──────────────────────────────────────

        /// <summary>
        /// DeleteBytes(start, 3) must always delete from startVirtualPosition each iteration
        /// because each deletion shifts remaining virtual positions up.
        /// </summary>
        [TestMethod]
        public void BatchDelete_ShiftsVirtualPosition_Correctly()
        {
            var provider = CreateProvider(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44 });

            // Delete 3 bytes starting at virtual position 1
            provider.DeleteBytes(1, 3);

            // Virtual before: [0x00, 0x11, 0x22, 0x33, 0x44]
            // After deleting 3 from virt 1: [0x00, 0x44]
            Assert.AreEqual(2L, provider.Length, "Length should be 2 after deleting 3 of 5");
            Assert.AreEqual((byte)0x00, provider.GetByte(0).value, "First byte unchanged");
            Assert.AreEqual((byte)0x44, provider.GetByte(1).value, "Last byte at virt 1");
        }

        // ── VirtualToPhysical skips deleted bytes ──────────────────────────────

        /// <summary>
        /// After deleting physical-mapped virtual pos 2, GetByte(2) must return the
        /// byte that was previously at virtual pos 3, not a deleted byte.
        /// </summary>
        [TestMethod]
        public void VirtualToPhysical_SkipsDeletedBytes()
        {
            var provider = CreateProvider(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44 });

            // Delete virtual pos 2 (value 0x22)
            provider.DeleteByte(2);

            // New virtual: [0x00, 0x11, 0x33, 0x44]
            Assert.AreEqual(4L, provider.Length, "Length decreased by 1");
            Assert.AreEqual((byte)0x00, provider.GetByte(0).value);
            Assert.AreEqual((byte)0x11, provider.GetByte(1).value);
            Assert.AreEqual((byte)0x33, provider.GetByte(2).value, "Virtual 2 now maps to former physical 3");
            Assert.AreEqual((byte)0x44, provider.GetByte(3).value);
        }

        /// <summary>
        /// Delete the first byte — all remaining bytes shift down by one virtual position.
        /// </summary>
        [TestMethod]
        public void DeleteFirstByte_AllRemainingBytesMoveDown()
        {
            var provider = CreateProvider(new byte[] { 0xAA, 0xBB, 0xCC });

            provider.DeleteByte(0);

            Assert.AreEqual(2L, provider.Length);
            Assert.AreEqual((byte)0xBB, provider.GetByte(0).value, "Former virt 1 is now virt 0");
            Assert.AreEqual((byte)0xCC, provider.GetByte(1).value, "Former virt 2 is now virt 1");
        }

        // ── InsertionIntegrity after removal ───────────────────────────────────

        /// <summary>
        /// After RemoveSpecificInsertion (via DeleteByte on inserted byte),
        /// remaining VirtualOffsets must be contiguous [0,1,2,...].
        /// </summary>
        [TestMethod]
        public void InsertionIntegrity_AfterRemoval_IsContiguous()
        {
            var provider = CreateProvider(new byte[] { 0xFF });

            provider.InsertByte(0, 0x01); // oldest  → virt 0
            provider.InsertByte(0, 0x02); // middle  → virt 1
            provider.InsertByte(0, 0x03); // newest  → virt 2

            // Delete middle insert (virt 1)
            provider.DeleteByte(1);

            // Remaining virtual: [0x01, 0x03, 0xFF]
            Assert.AreEqual(3L, provider.Length, "2 inserts + 1 physical = 3");
            Assert.AreEqual((byte)0x01, provider.GetByte(0).value, "Oldest insert still accessible");
            Assert.AreEqual((byte)0x03, provider.GetByte(1).value, "Newest insert shifted down");
            Assert.AreEqual((byte)0xFF, provider.GetByte(2).value, "Physical byte last");
        }

        // ── ByteReader round-trip ──────────────────────────────────────────────

        /// <summary>
        /// Insert 0xAB at virtual pos 5 — GetByte(5) must return 0xAB.
        /// </summary>
        [TestMethod]
        public void ByteReader_InsertedByte_RoundTrip()
        {
            var provider = CreateProvider(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 });

            // Insert at virtual pos 5 (before physical 0x55)
            provider.InsertByte(5, 0xAB);

            // Virtual: [0x00, 0x11, 0x22, 0x33, 0x44, 0xAB, 0x55]
            var result = provider.GetByte(5);
            Assert.IsTrue(result.success, "GetByte on inserted byte must succeed");
            Assert.AreEqual((byte)0xAB, result.value, "Inserted byte value must be 0xAB");
            Assert.AreEqual((byte)0x55, provider.GetByte(6).value, "Physical byte shifted to virt 6");
        }

        /// <summary>
        /// After deleting virtual pos 3, GetByte(3) must not return the deleted byte.
        /// </summary>
        [TestMethod]
        public void ByteReader_NoDeletedByteReturned()
        {
            var provider = CreateProvider(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44 });

            provider.DeleteByte(3); // delete 0x33

            var result = provider.GetByte(3);
            Assert.IsTrue(result.success, "GetByte at virt 3 after deletion must succeed");
            Assert.AreEqual((byte)0x44, result.value, "Must return former virt 4 (0x44), not deleted 0x33");
        }

        // ── Modify physical byte after insertions ──────────────────────────────

        /// <summary>
        /// Modifying the physical byte at its new virtual position (after insertions) must work.
        /// </summary>
        [TestMethod]
        public void ModifyPhysicalByte_AfterInsertions_Works()
        {
            var provider = CreateProvider(new byte[] { 0xAA, 0xBB });

            provider.InsertByte(0, 0x01);
            provider.InsertByte(0, 0x02);
            // Virtual: [0x01, 0x02, 0xAA, 0xBB]
            // Physical 0xAA is now at virtual pos 2

            provider.ModifyByte(2, 0xCC);

            Assert.AreEqual((byte)0xCC, provider.GetByte(2).value, "Physical byte modified correctly");
            Assert.AreEqual((byte)0xBB, provider.GetByte(3).value, "Adjacent physical byte unchanged");
        }

        // ── Multiple deletions non-consecutive ─────────────────────────────────

        /// <summary>
        /// Delete two non-adjacent bytes — virtual positions must be coherent throughout.
        /// </summary>
        [TestMethod]
        public void Delete_TwoNonAdjacentBytes_CoherentVirtualPositions()
        {
            var provider = CreateProvider(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44 });

            provider.DeleteByte(4); // delete 0x44 first (no shift issue)
            provider.DeleteByte(1); // delete 0x11 (was virt 1, still virt 1 after first deletion)

            // Remaining: [0x00, 0x22, 0x33]
            Assert.AreEqual(3L, provider.Length);
            Assert.AreEqual((byte)0x00, provider.GetByte(0).value);
            Assert.AreEqual((byte)0x22, provider.GetByte(1).value);
            Assert.AreEqual((byte)0x33, provider.GetByte(2).value);
        }
    }
}
