# Core/Converters

WPF value converters for data type transformations in XAML bindings.

## 📁 Contents

### 🔄 Hex/Decimal Converters

- **[ByteToHexStringConverter.cs](ByteToHexStringConverter.cs)** - Byte to hex string
  - Converts: `255` → `"0xFF"`
  - Optional: Show/hide "0x" prefix
  - Used in UI for hex display

- **[LongToHexStringConverter.cs](LongToHexStringConverter.cs)** - Long position to hex
  - Converts: `1024` → `"0x400"`
  - Used for file position display

- **[HexToLongStringConverter.cs](HexToLongStringConverter.cs)** - Hex string to long
  - Converts: `"0xFF"` → `255`
  - Validates hex input

### 👁️ Visibility Converters

- **[BooleanToVisibilityConverter.cs](BooleanToVisibilityConverter.cs)** - Bool to Visibility
  - Converts: `true` → `Visibility.Visible`
  - Converts: `false` → `Visibility.Collapsed`
  - Used to show/hide UI elements

- **[VisibilityToBooleanConverter.cs](VisibilityToBooleanConverter.cs)** - Visibility to Bool
  - Reverse of above
  - Used for two-way bindings

### 🔀 Logic Converters

- **[BoolInverterConverter.cs](BoolInverterConverter.cs)** - Boolean inverter
  - Converts: `true` → `false`
  - Used for enabling/disabling opposite controls

### 📂 File Converters

- **[PathToFilenameConverter.cs](PathToFilenameConverter.cs)** - Full path to filename
  - Converts: `"C:\folder\file.bin"` → `"file.bin"`
  - Used in window titles and status bars

## 🎯 Purpose

WPF value converters enable data type transformations in XAML bindings without code-behind. These converters are used throughout the hex editor UI to display byte values, positions, and control visibility.

## 🎓 Usage Example

### In XAML:

```xml
<Window.Resources>
    <converters:ByteToHexStringConverter x:Key="ByteToHex" Show0xTag="True"/>
    <converters:BooleanToVisibilityConverter x:Key="BoolToVis"/>
    <converters:PathToFilenameConverter x:Key="PathToFilename"/>
</Window.Resources>

<!-- Display byte as hex -->
<TextBlock Text="{Binding ByteValue, Converter={StaticResource ByteToHex}}"/>
<!-- Result: "0xFF" -->

<!-- Show/hide based on boolean -->
<Button Visibility="{Binding IsEnabled, Converter={StaticResource BoolToVis}}"/>

<!-- Show filename only -->
<TextBlock Text="{Binding FilePath, Converter={StaticResource PathToFilename}}"/>
<!-- Input: "C:\data\file.bin" → Output: "file.bin" -->
```

### In Code:

```csharp
var converter = new ByteToHexStringConverter { Show0xTag = true };
string result = converter.Convert(255, null, null, null);
// Returns: "0xFF"
```

## 🔗 Architecture

All converters implement `IValueConverter` interface:
```csharp
public interface IValueConverter
{
    object Convert(object value, Type targetType,
                   object parameter, CultureInfo culture);

    object ConvertBack(object value, Type targetType,
                       object parameter, CultureInfo culture);
}
```

## ✨ Features

- **Type Safety**: Null-safe implementations
- **Culture Support**: Respect regional settings
- **Two-Way Binding**: ConvertBack for editable controls
- **Parameter Support**: Optional converter parameters
- **Reusable**: Used across all HexEditor UI

## 📍 Usage Locations

These converters are used in:
- **[HexEditor.xaml](../../HexEditor.xaml)** - Main control XAML
- **[StatusBar.xaml](../../UserControls/StatusBar.xaml)** - Status display
- **[FindWindow.xaml](../../Dialog/FindWindow.xaml)** - Dialog windows
- **[FastTextLine.xaml](../../Core/FastTextLine.xaml)** - Text line rendering

## 🎨 Converter Patterns

### Simple Conversion:
```csharp
public object Convert(object value, ...) =>
    value is byte b ? $"0x{b:X2}" : "N/A";
```

### With Options:
```csharp
public bool Show0xTag { get; set; } = true;

public object Convert(object value, ...) =>
    Show0xTag ? $"0x{val:X2}" : $"{val:X2}";
```

## 📚 Related Components

- **[HexEditor.xaml](../../HexEditor.xaml)** - Main UI using these converters
- **[ByteProvider.cs](../Bytes/ByteProvider.cs)** - Provides data to convert
- **[ConstantReadOnly.cs](../../ConstantReadOnly.cs)** - Format constants

---

✨ WPF value converters for seamless data type transformations
