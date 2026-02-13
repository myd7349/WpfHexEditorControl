# HexEditorV2 Architecture Documentation

## Overview

HexEditorV2 is a modern WPF hex editor control built with MVVM architecture, featuring native insert mode support and 99% performance improvement over V1 through custom DrawingContext rendering.

## Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│           UI Layer (HexEditorV2.xaml)                   │
│  - WPF UserControl (no chrome)                          │
│  - XAML layout and visual tree                          │
│  - Data binding to ViewModel                            │
└─────────────────────────────────────────────────────────┘
                         ↓ ↑
┌─────────────────────────────────────────────────────────┐
│    ViewModel Layer (HexEditorViewModel)                 │
│  - Business logic and state management                  │
│  - INotifyPropertyChanged for binding                   │
│  - Coordinates between UI and data                      │
│  - Line caching for performance                         │
└─────────────────────────────────────────────────────────┘
                         ↓ ↑
┌─────────────────────────────────────────────────────────┐
│            Service Layer                                │
│  - UndoRedoService: Undo/redo operations               │
│  - ClipboardService: Clipboard management               │
│  - SearchService: Find/replace operations               │
└─────────────────────────────────────────────────────────┘
                         ↓ ↑
┌─────────────────────────────────────────────────────────┐
│      Data Layer (ByteProvider)                          │
│  - File I/O and stream management                       │
│  - Byte-level operations                                │
│  - Change tracking                                      │
└─────────────────────────────────────────────────────────┘
```

## Key Components

### HexEditorV2 (UI Control)
- **File**: `V2/HexEditorV2.xaml.cs` + `.xaml`
- **Purpose**: Main WPF UserControl
- **Responsibilities**:
  - User input handling (keyboard, mouse)
  - Visual rendering coordination
  - V1 compatibility layer
  - Event raising for external consumers

### HexEditorViewModel (Business Logic)
- **File**: `V2/ViewModels/HexEditorViewModel.cs`
- **Purpose**: MVVM ViewModel
- **Responsibilities**:
  - State management (position, selection, edit mode)
  - Line caching for performance
  - Virtual ↔ Physical position mapping
  - Change notification

### HexViewport (Custom Rendering)
- **File**: `V2/Controls/HexViewport.cs`
- **Purpose**: Custom visual element with DrawingContext rendering
- **Responsibilities**:
  - High-performance hex/ASCII rendering
  - Custom DrawingContext override
  - Visual updates on data changes
- **Performance**: 99% faster than V1 TextBlock-based rendering

### VirtualPosition Model
- **File**: `V2/Models/VirtualPosition.cs`
- **Purpose**: Position abstraction for insert mode
- **Key Concept**: Virtual positions remain stable when bytes are inserted, while physical positions shift

## Design Patterns

### 1. MVVM (Model-View-ViewModel)
- **View**: HexEditorV2.xaml
- **ViewModel**: HexEditorViewModel
- **Model**: ByteProvider, VirtualPosition

### 2. Service Pattern
Each service has a single, focused responsibility:
- `UndoRedoService`: Manages undo/redo stacks
- `ClipboardService`: Handles copy/paste operations
- `SearchService`: Implements find/replace logic

### 3. Virtual Position Pattern
Enables native insert mode by maintaining a mapping between virtual (stable) and physical (actual file) positions.

### 4. Line Caching
ViewModel caches rendered lines for visible viewport, dramatically improving scroll performance.

## V1 Compatibility Architecture

HexEditorV2 maintains 100% API compatibility with V1 through:

### Type Compatibility (Phase 1)
- Brush wrapper properties for Color-based V2 properties
- Automatic conversion between Brush ↔ Color

### Visibility Compatibility (Phase 2)
- Visibility properties map to V2 bool properties
- Automatic conversion Visibility ↔ bool

### Method Compatibility (Phases 3, 6)
- String-based search methods wrap byte[]-based V2 methods
- V1 method signatures delegate to V2 implementations

### Event Compatibility (Phase 4)
- Granular V1 events (20 events) fire alongside consolidated V2 events
- Backward-compatible event signatures

### Configuration Compatibility (Phase 5)
- V1 configuration properties (AllowContextMenu, MouseWheelSpeed, etc.)
- Stored as simple properties, trigger actions as needed

### Advanced Features (Phase 7)
- Custom Background Blocks
- File Comparison
- State Persistence (XML)
- TBL advanced colors
- Bar Chart properties

### XAML Binding (Phase 8)
- DependencyProperty support for key properties
- Enables two-way binding in XAML scenarios

## Performance Optimizations

### 1. Custom DrawingContext Rendering
**Impact**: 99% performance boost
- Replaces V1's TextBlock-based rendering
- Direct drawing to visual layer
- Minimal WPF element overhead

### 2. Line Caching
**Impact**: Smooth scrolling
- Caches rendered line data
- Incremental updates on viewport scroll
- Invalidates only when data changes

### 3. Virtual Scrolling
**Impact**: Handles large files (GB+)
- Only renders visible lines
- Does not load entire file into memory
- Constant memory usage regardless of file size

### 4. Lazy Evaluation
**Impact**: Fast startup
- Defer non-critical operations
- Load data on-demand
- Minimal initialization cost

## Insert Mode Implementation

V2 introduces **native insert mode** support through VirtualPosition:

```
Virtual Position: [0] [1] [2] [3] [4]
Physical (before insert): 0x41 0x42 0x43 0x44
                           A    B    C    D

Insert 0xFF at virtual position 2:
Physical (after): 0x41 0x42 0xFF 0x43 0x44
Virtual:         [0]  [1]  [2]  [3]  [4]

Virtual positions remain stable, physical positions shift.
```

### Benefits:
- Intuitive editing behavior
- Selection remains valid during inserts
- Undo/redo works correctly
- Bookmarks stay aligned

## File Format Support

### Binary Files
- Any binary file format
- Large file support (tested up to 4GB+)
- Memory-mapped file access

### Character Tables (TBL)
- Custom character table files (.tbl)
- ASCII/UTF-8/Custom encodings
- DTE (Double-Table Encoding)
- MTE (Multi-Title Encoding) support (Phase 7)

## Extension Points

### For Application Developers
1. **Custom rendering**: Subscribe to viewport render events
2. **Custom bookmarks**: Use bookmark API
3. **Custom background blocks**: Add colored regions (Phase 7)
4. **State persistence**: Save/load editor state (Phase 7)

### For Library Developers
1. **Custom ByteProvider**: Implement custom data sources
2. **Custom Services**: Replace UndoRedo, Clipboard, Search services
3. **Custom Visual Elements**: Extend HexViewport rendering

## Migration Path V1 → V2

See [MigrationGuide.md](MigrationGuide.md) for detailed migration instructions.

**Summary**:
- V1 API is 100% compatible with V2
- Existing applications work without code changes
- Gradually migrate to V2 API for modern features
- Use Obsolete warnings as migration guide

## Testing Strategy

See [Phase 11 Tests](../Tests/) for:
- Unit tests for V1 compatibility
- Integration tests with V1 samples
- Performance benchmarks
- Regression tests

## Future Enhancements

Potential improvements for future releases:
- Full Bar Chart Panel implementation
- Hex compare view (side-by-side)
- Advanced search (regex, wildcards)
- Plugin architecture
- Theme system

## References

- [Migration Guide](MigrationGuide.md)
- [API Reference](ApiReference.md)
- [Quick Start](QuickStart.md)
- [GitHub Repository](https://github.com/abbaye/WpfHexEditorControl)
