# Services Architecture

This folder contains services that encapsulate the business logic of the HexEditor.
The goal is to reduce the complexity of the `HexEditor` class (currently 6115 lines) by extracting responsibilities into dedicated services.

## 📋 Available Services

### ✅ ClipboardService
**Responsibility:** Manage copy/paste/cut operations

**Main methods:**
- `CopyToClipboard()` - Copy to clipboard
- `CopyToStream()` - Copy to stream
- `GetCopyData()` - Retrieve copied data
- `FillWithByte()` - Fill selection with a byte
- `CanCopy()` - Check if copy is possible
- `CanDelete()` - Check if deletion is possible

**Usage:**
```csharp
var clipboardService = new ClipboardService
{
    DefaultCopyMode = CopyPasteMode.HexaString
};

// Copy selection
clipboardService.CopyToClipboard(_provider, SelectionStart, SelectionStop, _tblCharacterTable);

// Check if can copy
if (clipboardService.CanCopy(SelectionLength, _provider))
{
    // ...
}
```

---

### 🔄 FindReplaceService
**Responsibility:** Search and replace data with optimized cache

**Main methods:**
- `FindFirst()` - Find first occurrence
- `FindNext()` - Find next occurrence
- `FindLast()` - Find last occurrence
- `FindAll()` - Find all occurrences
- `ReplaceFirst()` - Replace first occurrence
- `ReplaceAll()` - Replace all occurrences
- `ClearCache()` - Clear search cache

**Usage:**
```csharp
var findReplaceService = new FindReplaceService();

// Search
byte[] searchData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
long position = findReplaceService.FindFirst(_provider, searchData);

// Replace
byte[] replaceData = new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 };
var replacedPositions = findReplaceService.ReplaceAll(_provider, searchData, replaceData, false, false);
```

**Note:** The service includes a search cache with 5-second timeout to optimize performance.

---

### ↩️ UndoRedoService
**Responsibility:** Manage undo/redo history

**Main methods:**
- `Undo()` - Undo last action (returns byte position)
- `Redo()` - Redo an undone action (returns byte position)
- `ClearAll()` - Clear all history
- `CanUndo()` - Check if undo is possible
- `CanRedo()` - Check if redo is possible
- `GetUndoCount()` - Get number of actions in history
- `GetUndoStack()` - Get undo stack

**Usage:**
```csharp
var undoRedoService = new UndoRedoService();

// Undo
if (undoRedoService.CanUndo(_provider))
{
    long position = undoRedoService.Undo(_provider);
    // Update position in UI
}

// Redo
if (undoRedoService.CanRedo(_provider))
{
    long position = undoRedoService.Redo(_provider, repeat: 3);
    // Redo the last 3 undone actions
}
```

---

### 🎯 SelectionService
**Responsibility:** Manage selection and validation

**Main methods:**
- `IsValidSelection()` - Check if selection is valid
- `GetSelectionLength()` - Calculate selection length
- `FixSelectionRange()` - Fix start/stop order
- `ValidateSelection()` - Validate and adjust selection to bounds
- `GetSelectionBytes()` - Retrieve selected bytes
- `GetAllBytes()` - Retrieve all bytes from provider
- `GetSelectAllStart()` / `GetSelectAllStop()` - Calculate positions for "Select All"
- `IsAllSelected()` - Check if everything is selected
- `HasSelection()` - Check if a selection exists
- `ExtendSelection()` - Extend selection with an offset
- `GetSelectionByte()` - Retrieve byte at position

**Usage:**
```csharp
var selectionService = new SelectionService();

// Check selection
if (selectionService.IsValidSelection(SelectionStart, SelectionStop))
{
    long length = selectionService.GetSelectionLength(SelectionStart, SelectionStop);
    byte[] data = selectionService.GetSelectionBytes(_provider, SelectionStart, SelectionStop);
}

// Fix selection if inverted
var (start, stop) = selectionService.FixSelectionRange(SelectionStart, SelectionStop);

// Validate and adjust to bounds
var (validStart, validStop) = selectionService.ValidateSelection(_provider, SelectionStart, SelectionStop);
```

---

## 🏗️ Architecture

```
HexEditor (Main Controller)
    ├── ClipboardService
    ├── FindReplaceService
    ├── UndoRedoService
    └── SelectionService
```

## 📦 Benefits of this Architecture

1. **Separation of Concerns** - Each service has a single responsibility
2. **Testability** - Services can be unit tested in isolation
3. **Reusability** - Services can be used in other contexts
4. **Maintainability** - Code is easier to understand and modify
5. **Extensibility** - Easy to add new services

## 🔧 Progressive Migration

Service integration is done progressively:

**Phase 1 (Completed ✅):** Create services without modifying HexEditor
- ✅ ClipboardService created and tested
- ✅ FindReplaceService created and tested
- ✅ UndoRedoService created and tested
- ✅ SelectionService created and tested

**Phase 2 (Completed ✅):** Integration of services into HexEditor
- ✅ All 4 services integrated into HexEditor.xaml.cs
- ✅ Critical bug fix: Cache properly cleared after modifications
- ✅ API preserved - no breaking changes

**Phase 3:** Complete refactoring of HexEditor to use services

This approach allows:
- ✅ Keep existing code functional
- ✅ Test each service individually
- ✅ Migrate progressively without regression

## 📝 Development Notes

- All services are in the `WpfHexaEditor.Services` namespace
- Services are stateless (when possible)
- Dependencies are passed as parameters rather than injected
- Services do NOT depend on HexEditor (strong decoupling)

## 🐛 Bug Fixes

**Critical Bug Fixed in Phase 2:**
- **Issue:** Search cache was never invalidated after data modifications
- **Impact:** Users received stale/incorrect search results after editing
- **Solution:** Added `ClearCache()` calls at all 11 data modification points in HexEditor
- **Locations:** ModifyByte, Paste, FillWithByte, ReplaceByte, ReplaceFirst, ReplaceAll, Undo handler, InsertByte, InsertBytes, DeleteSelection

## 🔮 Future Enhancements

Potential services for Phase 3:
- **BookmarkService** - Manage bookmarks
- **HighlightService** - Manage byte highlighting
- **ScrollService** - Manage scrolling and markers
- **ValidationService** - Validate data integrity
