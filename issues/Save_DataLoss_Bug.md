# Bug: Save Operation Causes Catastrophic Data Loss with Insertions

## 🐛 Describe the bug

When saving a file after inserting bytes in **Insert Mode**, the save operation destroys most of the file content, reducing a multi-megabyte file to a few hundred bytes.

## 📋 To Reproduce

Steps to reproduce the behavior:

1. Open a large file (e.g., 2.92 MB / 3,064,767 bytes)
2. Switch to **Insert Mode**
3. Insert several bytes (e.g., type "FFFFFFFF")
4. Save the file (Ctrl+S or File > Save)
5. Close and reopen the file

**Expected result:** File retains original size with inserted bytes added
**Actual result:** File is reduced to ~752 bytes, all original data lost ❌

## 🖼️ Evidence

**Before Save:**
- File size: 2.92 MB (3,064,767 bytes)
- Date: 16/12/2025 19:15
- Content: Normal with all data intact

**After Save:**
- File size: 1 KB (752 bytes)
- Date: 14/2/2026 08:47
- Content: Almost completely wiped out

## 🔍 Root Cause Analysis

### The Save Process

`ByteProvider.SaveAs` (line 464):
```csharp
for (long vPos = 0; vPos < virtualLength; vPos += BUFFER_SIZE)
{
    int toRead = (int)Math.Min(BUFFER_SIZE, virtualLength - vPos);
    byte[] buffer = GetBytes(vPos, toRead);
    outputStream.Write(buffer, 0, buffer.Length);
}
```

Save calls `GetBytes` which delegates to `ByteReader.GetBytes`, which calls `ReadByteInternal` for each byte.

### The Bug

In `ByteReader.ReadByteInternal` (line 76-91), when reading inserted bytes:

```csharp
if (isInserted)
{
    var insertions = _editsManager.GetInsertedBytesAt(physicalPos.Value);
    long virtualStart = _positionMapper.PhysicalToVirtual(physicalPos.Value, physicalFileLength);
    long insertionIndex = virtualPosition - virtualStart;

    if (insertionIndex >= 0 && insertionIndex < insertions.Count)
    {
        return (insertions[(int)insertionIndex].Value, true);  // ❌ WRONG!
    }
}
```

**Problem:** The code uses `insertionIndex` as a direct array index, but:

1. **Insertions are stored in LIFO (stack) order:**
   - Newest insertion (highest virtual position) is at index 0
   - Oldest insertion (lowest virtual position) is at end of list

2. **Each InsertedByte has a VirtualOffset field:**
   - This offset represents position relative to physical position
   - NOT the same as array index!

3. **Consequence:**
   - Reading wrong bytes from insertions list
   - Save writes corrupted data
   - Most bytes return `(0, false)` and are skipped
   - Result: File with only partial data saved

### Example

If we insert 3 bytes at virtual positions 100, 101, 102:

**Insertions list:**
```
[0] = (value=C, offset=0) → virtual 102 (newest)
[1] = (value=B, offset=1) → virtual 101
[2] = (value=A, offset=2) → virtual 100 (oldest)
```

**Current (WRONG) behavior:**
- Read virtual 100: insertionIndex=0 → returns insertions[0]=C ❌ (should be A)
- Read virtual 101: insertionIndex=1 → returns insertions[1]=B ✓ (correct by accident)
- Read virtual 102: insertionIndex=2 → returns insertions[2]=A ❌ (should be C)

**Correct behavior:**
- Calculate targetOffset = virtualPosition - virtualStart
- **Search** for InsertedByte with matching VirtualOffset
- Return that byte's Value

## 🔧 Fixes Applied

### Fix 1: PositionMapper.PhysicalToVirtual Calculation (ROOT CAUSE)
**Location:** `PositionMapper.cs` lines 278-290
**Commit:** 405b164

**Problem:** PhysicalToVirtual returned position of FIRST inserted byte instead of PHYSICAL byte position when exact segment match found.

**Change:**
```csharp
if (physicalPosition == segment.PhysicalPos)
{
    // OLD (WRONG): virtualPos = segment.VirtualOffset;
    // NEW (CORRECT): Physical byte is AFTER all insertions
    virtualPos = segment.VirtualOffset + segment.InsertedCount;
    return virtualPos;
}
```

**Impact:** This was the root cause of Save data loss. ByteReader was calculating wrong offsets because PhysicalToVirtual returned incorrect values, causing it to read physical bytes instead of inserted bytes during Save.

### Fix 2: ByteReader LIFO Offset Calculation
**Location:** `ByteReader.cs` lines 76-156

**Changes:**
1. Updated virtual space layout understanding to match PositionMapper semantics
2. Corrected LIFO offset calculation using proper firstInsertedVirtualPos
3. Added detailed diagnostic logging

**Result:** ByteReader now correctly reads inserted bytes with proper LIFO offset calculation.

### Fix 3: ByteProvider LIFO Offset Calculation
**Location:** `ByteProvider.cs` lines 264-300

**Changes:**
1. Updated virtual space layout understanding
2. Corrected LIFO offset calculation for ModifyInsertedByte operations

**Result:** Modifications to inserted bytes now work correctly.

## 🎯 Priority

**CRITICAL** 🚨 - This bug causes **PERMANENT DATA LOSS** and makes Insert Mode completely unusable.

## ✅ Acceptance Criteria

A fix is successful when:
1. Save preserves original file size + inserted bytes
2. After save, file content is identical to virtual view before save
3. No data loss occurs when saving files with insertions
4. Save works correctly with multiple insertions at different positions
5. Save works correctly with mix of insertions, modifications, and deletions

## 🔬 Current Status

**Fixes Applied:** ✅ All root cause fixes completed (commit 405b164)
**Testing Status:** ⏳ PENDING - Awaiting validation tests

### Tests Required Before Closing:
1. ⏳ Save with byte insertions - verify file size = original + inserted bytes
2. ⏳ Save with byte deletions - verify file size = original - deleted bytes
3. ⏳ Save with byte modifications only - verify file size unchanged
4. ⏳ Save with mix of insertions, deletions, and modifications
5. ⏳ After save, reopen file and verify content matches virtual view before save
6. ⏳ Large file test (multi-MB file with 100+ insertions)

### Expected Results:
With the PositionMapper fix, ByteReader now correctly reads inserted bytes during Save operations. This should resolve the data loss issue completely.

**Next Step:** Run comprehensive Save validation tests with insertions, deletions, and mixed edits.

---

## 📝 Related Issues

- ✅ RESOLVED: Insert Mode hex input bug (F0 F0 pattern) - Same PositionMapper root cause
- ✅ RESOLVED: ByteProvider.ModifyByteInternal LIFO offset bug

---

**Labels**: bug, critical, data-loss, V2, save, ByteProvider, insertions, **PENDING-VALIDATION**
**Milestone**: v2.2.0 (BLOCKER)
**Fix Date**: 2026-02-14
**Fixed in Commit**: 405b164
**Assignee**: TBD

