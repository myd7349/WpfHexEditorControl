# DIAGNOSTIC: Tooltip Settings Not Persisting

## Problem Description
The Tooltip settings (`ByteToolTipDisplayMode` and `ByteToolTipDetailLevel`) appear in the UI but do not persist when the application is closed and reopened.

## Investigation Steps

### 1. Verify Properties Have [Category] Attribute ✓
**File:** `PartialClasses\Compatibility\HexEditor.CompatibilityLayer.Properties.cs`

- Line 73: `[Category("Tooltip")]` on `ByteToolTipDisplayMode`
- Line 112: `[Category("Tooltip")]` on `ByteToolTipDetailLevel`

**Status:** CONFIRMED - Both properties have [Category] attributes.

### 2. Verify DependencyProperty Declarations ✓
- Line 66: `ByteToolTipDisplayModeProperty` declared
- Line 105: `ByteToolTipDetailLevelProperty` declared

**Status:** CONFIRMED - Both DependencyProperties exist with correct naming convention.

### 3. Verify Properties are NOT Browsable(false)
- `ByteToolTipDisplayMode`: NO [Browsable(false)] attribute ✓
- `ByteToolTipDetailLevel`: NO [Browsable(false)] attribute ✓

**Status:** CONFIRMED - Not hidden from discovery.

### 4. Check PropertyDiscoveryService Logic
**File:** `Core\Settings\PropertyDiscoveryService.cs`

Discovery requirements:
1. Line 42-44: Has [Category] attribute ✓
2. Line 53-58: Has corresponding DependencyProperty field ✓
3. Line 47-49: NOT [Browsable(false)] ✓
4. Line 67-68: NOT Brush type ✓

**Status:** SHOULD BE DISCOVERED - All conditions met.

### 5. Check SettingsStateService Enum Handling
**File:** `Core\Settings\SettingsStateService.cs`

**Serialization (Line 57-60):**
```csharp
else if (prop.PropertyType.IsEnum)
{
    state[prop.PropertyName] = value.ToString();
}
```

**Deserialization (Line 136-139):**
```csharp
else if (prop.PropertyType.IsEnum)
{
    value = Enum.Parse(prop.PropertyType, jsonValue.GetString());
}
```

**Status:** Enum handling implemented.

## Potential Issues

### Issue 1: Namespace Conflict
The enum type `ByteToolTipDisplayMode` might conflict with the property name during reflection.

**Evidence:**
```csharp
public static readonly DependencyProperty ByteToolTipDisplayModeProperty =
    DependencyProperty.Register(nameof(ByteToolTipDisplayMode),
        typeof(ByteToolTipDisplayMode),  // <-- Type and property have same name
        typeof(HexEditor),
        new PropertyMetadata(ByteToolTipDisplayMode.None, ...));
                              // ^^^^^^ Enum type again
```

### Issue 2: Partial Class Property Not Found
PropertyDiscoveryService uses reflection on `typeof(HexEditor)` which should include all partial classes, but there might be a timing or assembly loading issue.

### Issue 3: Enum Parsing Exception
If the enum value in JSON doesn't match the enum definition exactly (case-sensitive), `Enum.Parse()` will throw an exception that's silently caught.

## Recommended Debug Actions

### Action 1: Add Logging to SettingsStateService
Add debug output to see which properties are being saved/loaded:

```csharp
// In SaveState() method around line 40
foreach (var prop in properties)
{
    System.Diagnostics.Debug.WriteLine($"[SaveState] Processing: {prop.PropertyName}");
    // ... existing code
}

// In LoadState() method around line 102
foreach (var prop in properties)
{
    System.Diagnostics.Debug.WriteLine($"[LoadState] Processing: {prop.PropertyName}");
    // ... existing code
}
```

### Action 2: Inspect Saved JSON
Add a breakpoint or logging in `MainWindow.MainWindow_Closing()` at line 110:

```csharp
var json = HexEditorSettingsPanel.GetSettingsJson();
System.Diagnostics.Debug.WriteLine($"[SAVE] JSON Length: {json.Length}");
System.Diagnostics.Debug.WriteLine($"[SAVE] JSON Content:\n{json}");
```

Check if `ByteToolTipDisplayMode` and `ByteToolTipDetailLevel` appear in the JSON.

### Action 3: Test Enum Serialization
Run this test to verify enum serialization works:

```csharp
using System.Text.Json;
using WpfHexaEditor.Models;

var testEnum = ByteToolTipDisplayMode.Everywhere;
var json = JsonSerializer.Serialize(testEnum);
Console.WriteLine($"Serialized: {json}"); // Should be "1" or "Everywhere"

var parsed = JsonSerializer.Deserialize<ByteToolTipDisplayMode>(json);
Console.WriteLine($"Deserialized: {parsed}"); // Should be Everywhere
```

### Action 4: Check Actual Property Values at Runtime
Add logging in `MainWindow_Loaded()` after calling `LoadSettingsJson()`:

```csharp
HexEditorSettingsPanel.LoadSettingsJson(json);

// Check if values were restored
var displayMode = HexEditorControl.ByteToolTipDisplayMode;
var detailLevel = HexEditorControl.ByteToolTipDetailLevel;
System.Diagnostics.Debug.WriteLine($"[LOAD] DisplayMode = {displayMode}");
System.Diagnostics.Debug.WriteLine($"[LOAD] DetailLevel = {detailLevel}");
```

## Expected Fix

If the issue is confirmed to be in PropertyDiscoveryService or SettingsStateService, the fix might involve:

1. **Better enum handling:** Use `JsonSerializer` options to handle enum serialization as strings
2. **Explicit property registration:** Add these properties to a whitelist if automatic discovery fails
3. **Type resolution:** Ensure the enum types are correctly resolved during reflection

## Test Procedure

1. Launch Sample.Main application
2. Change Tooltip settings:
   - Set "Byte Tool Tip Display Mode" to "Everywhere"
   - Set "Byte Tool Tip Detail Level" to "Detailed"
3. Close the application
4. Check the saved JSON in `%LocalAppData%\<AppName>\user.config` or debug output
5. Reopen the application
6. Verify if Tooltip settings were restored

## Next Steps

Please run the diagnostic actions above and report:
1. Does `ByteToolTipDisplayMode` appear in the saved JSON?
2. Are there any exceptions in the debug output?
3. What are the actual values after loading?
