# Core/CharacterTable

Custom character table (.tbl) support for game hacking and ROM editing.

## 📁 Contents

- **[TBLStream.cs](TBLStream.cs)** - TBL file parser and character mapper
  - Loads .tbl files (game character tables)
  - Maps byte sequences to custom characters
  - Bidirectional lookup (byte ↔ character)
  - Multi-byte character support (DTE encoding)
  - Default table fallback

- **[DTE.cs](DTE.cs)** - Dual Tile Encoding entry class
  - Represents multi-byte character mappings
  - Format: byte sequence → display character
  - Used in game localization and ROM hacking
  - Example: `88=Å` or `8899=™`

- **[Enum.cs](Enum.cs)** - Character table enumerations
  - Default table types (ASCII, EBCDIC)
  - TBL entry types and formats
  - Character encoding options

## 🎯 Purpose

TBL (Table) files are used in ROM hacking and retro game editing to define custom character encodings. Many old games use proprietary encodings where byte values don't correspond to standard ASCII/Unicode.

## 📖 TBL File Format

Example `.tbl` file:
```
00=<NULL>
0A=<LF>
20=<SPACE>
41=A
42=B
88=Å
8899=™
```

Format: `HEX_VALUE=CHARACTER`

## 🔗 Integration

```
HexEditor
    └── ByteProvider
        └── TBLStream (optional)
            ├── Loads: .tbl files
            └── Provides: Custom character display
```

## 🎓 Usage Example

```csharp
// Load a TBL file
var tblStream = new TBLStream("pokemon_gen1.tbl");

// Map byte to character
string character = tblStream.FindTBLMatch(0x88);
// Returns: "Å"

// Map multi-byte sequence
byte[] sequence = new byte[] { 0x88, 0x99 };
string multiChar = tblStream.FindTBLMatch(sequence);
// Returns: "™"

// Reverse lookup (character to bytes)
byte[] bytes = tblStream.FindTBLMatch("Å");
// Returns: [0x88]

// Use in HexEditor
hexEditor.LoadTblFile("game.tbl");
// Now hex view shows custom characters instead of ASCII
```

## 🎮 Use Cases

- **Game Localization**: View/edit game text with proper characters
- **ROM Hacking**: Modify retro game dialogue and text
- **Data Analysis**: Understand proprietary text formats
- **Reverse Engineering**: Map unknown encodings
- **Save File Editing**: Edit game saves with special characters

## ✨ Features

- **Multi-Byte Support**: DTE (Dual Tile Encoding) for compressed text
- **Bidirectional Mapping**: Byte → Char and Char → Byte
- **Default Fallback**: Uses ASCII when no TBL loaded
- **Format Validation**: Checks .tbl file syntax
- **Encoding Preservation**: Maintains original byte values

## 📚 TBL File Resources

Popular TBL files for classic games:
- Pokémon Red/Blue/Yellow
- Final Fantasy series
- Dragon Quest/Warrior
- Chrono Trigger
- Super Mario RPG

TBL files available at: https://www.romhacking.net

## 🔗 Related Components

- **[ByteProvider.cs](../Bytes/ByteProvider.cs)** - Uses TBLStream for character display
- **[TblService](../../Services/TblService.cs)** - Higher-level TBL management service
- **[StringDataLine.xaml.cs](../../Core/StringDataLine.xaml.cs)** - Displays TBL characters in UI

---

✨ Custom character table support for game hacking and ROM editing
