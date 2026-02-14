# Bug: Hex Input in Insert Mode Produces F0 Bytes Instead of FF Pairs

## 🐛 Describe the bug

When typing consecutive hex characters (e.g., "FFFFFFFF...") in **Insert Mode**, the HexEditorV2 control produces incorrect byte sequences. Instead of pairing hex characters to form complete bytes (e.g., "FF FF FF FF"), it creates incomplete bytes with only the high nibble set (e.g., "F0 F0 F0 F0").

## 📋 To Reproduce

Steps to reproduce the behavior:

1. Open a file in HexEditorV2 sample application
2. Switch to **Insert Mode** (press Insert key or toggle in menu)
3. Position cursor at any location
4. Type a series of hex characters: `FFFFFFFF` (8 F's)
5. Observe the result

**Expected result:** `FF FF FF FF` (4 complete bytes)
**Actual result:** `F0 F0 F0 F0 F0 FF` (5 incomplete bytes + 1 complete)

## 🖼️ Screenshots

![Insert Mode Bug](Images/InsertModeBug.png)

The screenshot shows:
- Multiple `F0` bytes (incomplete - only high nibble set)
- One `FF` byte at the end (complete)
- All bytes are green (indicating they are inserted bytes)

## 🔍 Root Cause Analysis

### Current Behavior

In `HexEditorV2.xaml.cs`, the `HandleHexInput` method (around line 5900) handles hex character input:

```csharp
// IN INSERT MODE: Insert byte IMMEDIATELY after first nibble (don't wait for second nibble)
if (_viewModel.EditMode == EditMode.Insert)
{
    System.Diagnostics.Debug.WriteLine($"[HEXINPUT] HIGH NIBBLE - INSERT MODE: Inserting byte IMMEDIATELY with value 0x{_editingValue:X2}");
    _viewModel.InsertByte(_editingPosition, _editingValue);
    // Don't move to next byte yet - wait for low nibble to modify this inserted byte
    // CRITICAL: Keep cursor at editing position to allow low nibble input
    _viewModel.SetSelection(_editingPosition);
    System.Diagnostics.Debug.WriteLine($"[HEXINPUT] HIGH NIBBLE - Cursor locked at position {_editingPosition.Value} for low nibble");
}
```

### The Problem

The issue occurs when the user types continuously. The sequence should be:

1. Type 'F' → Insert 0xF0, wait for low nibble
2. Type 'F' → Update to 0xFF, commit, move to next position
3. Type 'F' → Start new byte at next position...

However, what's happening is:

1. Type 'F' → Insert 0xF0, cursor should stay at same position
2. Type 'F' → **Cursor position has drifted**, starts NEW byte edit instead of completing the previous one
3. Insert another 0xF0...

### Suspected Causes

1. **Timing Issue**: The `SetSelection` call after `InsertByte` might not be synchronous, allowing the cursor to drift before the next keystroke
2. **Position Mapping**: After inserting a byte, the virtual-to-physical position mapping changes, which might cause the cursor position to become invalid or shift
3. **ViewModel Refresh**: The `InsertByte` calls `RefreshVisibleLines()` which might trigger events that reset the cursor position
4. **KeyDown Event Ordering**: The `Content_KeyDown` handler captures `currentPos` at line 5275 before `HandleHexInput` is called, so if the position changes during `InsertByte`, the next keystroke will see the new position

## 🔧 Attempted Fix

A fix was attempted in commit `8053f98` by adding `SetSelection` after `InsertByte`:

```csharp
_viewModel.InsertByte(_editingPosition, _editingValue);
_viewModel.SetSelection(_editingPosition);
```

However, this fix is insufficient because:
- The selection might be updated asynchronously
- The position mapper might be changing the virtual position after insertion
- The editing state (`_isEditingByte`, `_editingPosition`) might be reset between keystrokes

## 💡 Potential Solutions

### Solution 1: Synchronize Position After Insertion
Ensure that after inserting a byte, the cursor position is forcibly synchronized before the method returns:

```csharp
_viewModel.InsertByte(_editingPosition, _editingValue);
// Force synchronous position update
Dispatcher.Invoke(() => {
    _viewModel.SetSelection(_editingPosition);
}, System.Windows.Threading.DispatcherPriority.Send);
```

### Solution 2: Check Editing State More Carefully
Modify the condition at line 5908 to be more resilient:

```csharp
// Start new byte edit if not currently editing, position changed, OR editing session timed out
if (!_isEditingByte || _editingPosition != currentPos)
{
    // BUT: If we just inserted a byte in Insert mode and position is +1, it's the same edit!
    if (_viewModel.EditMode == EditMode.Insert &&
        _isEditingByte &&
        currentPos.Value == _editingPosition.Value + 1)
    {
        // This is actually the continuation of the previous byte insertion
        // The position advanced by 1 due to the insertion
        // Continue editing at the new position
        _editingPosition = currentPos;
    }
    else
    {
        // Start fresh byte edit
        _isEditingByte = true;
        _editingPosition = currentPos;
        _editingHighNibble = true;
        // ... initialization
    }
}
```

### Solution 3: Don't Move Virtual Position After Insert
Modify the `PositionMapper` to ensure that after inserting at position N, the virtual position N still refers to the inserted byte, not the byte after it.

### Solution 4: Use a Different Insert Strategy
Instead of inserting on the high nibble and modifying on the low nibble, buffer the byte value and only insert when both nibbles are entered (similar to Overwrite mode), then shift the cursor.

## 🖥️ Environment

- **OS**: Windows 11 Home 10.0.26200
- **Framework**: .NET 8.0-windows / .NET Framework 4.8
- **Version**: WPFHexaEditor v2.2.0
- **Control**: HexEditorV2
- **Mode**: Insert Mode

## 📌 Related Code Locations

- `HexEditorV2.xaml.cs` line 5897: `HandleHexInput` method
- `HexEditorV2.xaml.cs` line 5947: Insert mode high nibble handling
- `HexEditorV2.xaml.cs` line 5908: Edit state check condition
- `HexEditorV2.xaml.cs` line 6047: `CommitByteEdit` method
- `ViewModels/HexEditorViewModel.cs` line 398: `InsertByte` method
- `Core/Bytes/ByteProvider.cs`: Position mapping after insertion

## 📝 Additional Context

This bug only occurs in **Insert Mode**. In **Overwrite Mode**, the hex input works correctly because:
1. The byte is not committed until both nibbles are entered
2. The position doesn't change during nibble entry
3. No insertion operation occurs until the byte is complete

The bug manifests most clearly when typing rapidly or when typing a long sequence of the same character.

## 🎯 Priority

**High** - This is a critical usability issue that makes Insert Mode nearly unusable for hex input. Users expect to be able to type hex pairs naturally, and the current behavior breaks this fundamental expectation.

## ✅ Acceptance Criteria

A fix is successful when:
1. Typing "FFFFFFFF" in Insert mode produces: `FF FF FF FF` (4 bytes)
2. Typing "12345678" in Insert mode produces: `12 34 56 78` (4 bytes)
3. Typing a single character 'F' then moving away produces: `F0` (1 incomplete byte)
4. The cursor remains at the correct position after each nibble entry
5. The fix works consistently regardless of typing speed

---

## 🔧 Attempted Fixes (Session 2026-02-14)

### Fix 1: Cursor Position Synchronization
**Location:** `HexEditorV2.xaml.cs` lines ~5987-6010 and ~5944-6000

**Changes:**
1. Added `Dispatcher.Invoke(DispatcherPriority.Send)` to force synchronous cursor position update after `InsertByte`
2. Added drift tolerance: if position drifts by ±1 in Insert mode while waiting for low nibble, continue same edit instead of starting new one

**Result:** ✅ Cursor position tracking works correctly - logs show `currentPos=102` matches `_editingPosition=102`

### Fix 2: VirtualOffset Calculation for LIFO Insertions
**Location:** `ByteProvider.cs` ModifyByteInternal (lines ~246-264)

**Problem Identified:**
- Insertions are stored in LIFO (stack) order with newest insertion at offset 0
- Original code calculated offset as `virtualPosition - virtualStart` (assumes FIFO)
- This caused wrong byte to be modified: offset 1 instead of 0

**Changes:**
```csharp
int totalInsertions = _editsManager.GetInsertionCountAt(physicalPos.Value);
int virtualOffset = totalInsertions - 1 - (int)(virtualPosition - virtualStart);
```

**Result:** ⚠️ PARTIALLY FIXED - logs show correct offset calculation, but bytes still show as F0 F0 F0 instead of FF FF FF

### Remaining Issue
Despite both fixes, typing "FFFFFFFF" still produces "F0 F0 F0 F0 F0 FF" pattern. The cursor tracking works, offset calculation is correct, but the ModifyInsertedByte operation appears to fail silently or modify the wrong byte.

**Next Steps:**
1. Debug `EditsManager.ModifyInsertedByte` to verify it finds and updates the correct byte
2. Verify inserted bytes list order and offset assignment in `InsertBytes`
3. Check if there's a cache invalidation issue preventing visual update

**Status:** 🔴 PAUSED - Critical save bug takes priority

---

**Labels**: bug, high-priority, V2, insert-mode, hex-input
**Milestone**: v2.2.1
**Assignee**: TBD
