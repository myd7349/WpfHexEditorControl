# Double-click byte selection not working in HexEditorV2

## 🐛 Bug Description

Double-clicking on a byte in HexEditorV2 should select all bytes with the same value, but the selection is not being applied correctly.

## Expected Behavior

When double-clicking on a byte (e.g., value `0x41`):
1. All bytes in the visible region with the same value should be selected
2. `SelectionStart` should be set to the first match position
3. `SelectionStop` should be set to the last match position
4. The selected bytes should be visually highlighted

## Actual Behavior

- Double-click is detected (confirmed by debug logs)
- `SelectAllBytesWith()` method is called
- But the selection is not visually applied or doesn't work as expected

## Technical Details

**Implementation:**
- Feature implemented in commit `bbec374` - "Fix double-click select to use real selection"
- Auto-highlight feature works correctly (commit `dbd0522`)
- Uses real selection (SelectionStart/SelectionStop) instead of visual highlighting

**Key Files:**
- `V2/HexEditorV2.xaml.cs` - `HexViewport_ByteDoubleClicked()` handler
- `V2/HexEditorV2.xaml.cs` - `SelectAllBytesWith()` method
- `V2/ViewModels/HexEditorViewModel.cs` - Selection management

**Debug Logging:**
Debug logs have been added to:
- `HexViewport_ByteDoubleClicked()` - Shows when double-click is detected
- `SelectAllBytesWith()` - Shows search results and selection being set
- Output visible in StatusText and System.Diagnostics.Debug.WriteLine()

## Steps to Reproduce

1. Open a file in HexEditorV2
2. Double-click on any byte in the hex view
3. Observe that the selection doesn't work as expected

## Environment

- HexEditorV2 (V2 architecture with MVVM)
- Insert mode implementation active
- Custom DrawingContext rendering

## Related Features

- ✅ Auto-highlight feature works (highlights matching bytes in yellow)
- ❌ Double-click selection broken

## Priority

**Medium** - V1 compatibility feature, affects user experience but workaround exists (manual selection)

## Suggested Investigation Areas

1. Check if `SelectionStart` and `SelectionStop` are being set correctly in `SelectAllBytesWith()`
2. Verify that `OnSelectionChanged()` is triggered after setting selection
3. Confirm that `RefreshVisibleLines()` is called to update visual representation
4. Check if there's a conflict between auto-highlight and selection highlighting
5. Verify the search is finding the correct matching bytes

## Labels

- `bug`
- `V1-compatibility`
- `HexEditorV2`
- `selection`
