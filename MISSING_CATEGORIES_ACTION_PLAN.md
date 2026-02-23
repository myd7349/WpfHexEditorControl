# ACTION PLAN: Add Missing [Category] Attributes

**Priority:** HIGH
**Impact:** 24 properties currently hidden from JSON export
**Effort:** LOW (single attribute per property)

---

## IMMEDIATE ACTION REQUIRED (24 Properties)

These properties have CLR wrappers and valid types but are missing `[Category]` attributes:

### File: HexEditor.xaml.cs (9 properties)

```csharp
// Add [Category("Display")] before property
public bool ShowInlineBarChart

// Add [Category("Data")] before property
public bool IsModified

// Add [Category("Visual.BarChart")] before properties
public Visibility BarChartPanelVisibility
public int BarChartPanelHeight
public bool BarChartShowAxisLabels
public bool BarChartShowGridLines
public bool BarChartShowStatistics

// Add [Category("Colors.BarChart")] before properties
public Color BarChartBackgroundColor
public Color BarChartTextColor
```

### File: HexEditor.Zoom.cs (1 property)

```csharp
// Add [Category("Display")] before property
public double ZoomScale
```

### File: HexEditor.CompatibilityLayer.Properties.cs (3 properties)

```csharp
// Add [Category("Display.ToolTip")] before properties
public bool ShowByteToolTip
public ByteToolTipDisplayMode ByteToolTipDisplayMode
public ByteToolTipDetailLevel ByteToolTipDetailLevel
```

### File: HexEditor.DataInspectorIntegration.cs (2 properties)

```csharp
// Add [Category("DataInspector")] before properties
public Visibility DataInspectorVisibility
public int DataInspectorByteCount
```

### File: HexEditor.StructureOverlayIntegration.cs (1 property)

```csharp
// Add [Category("StructureOverlay")] before property
public Visibility StructureOverlayVisibility
```

### File: HexEditor.ParsedFieldsIntegration.cs (2 properties)

```csharp
// Add [Category("ParsedFields")] before properties
public Visibility ParsedFieldsPanelVisibility
public bool AutoRefreshParsedFields
```

### File: HexEditor.FormatDetection.cs (6 properties)

```csharp
// Add [Category("FormatDetection")] before properties
public bool EnableAutoFormatDetection
public string FormatDefinitionsPath
public bool AutoApplyDetectedBlocks
public bool ShowFormatDetectionStatus
public int MaxFormatDetectionSize
public int LoadedFormatCount  // Consider [Browsable(false)] if this is calculated/read-only
```

---

## IMPLEMENTATION CHECKLIST

For each property:
- [ ] Locate the CLR property wrapper
- [ ] Add `[Category("CategoryName")]` attribute on the line before the property
- [ ] Ensure the appropriate using statement exists: `using System.ComponentModel;`
- [ ] Verify property is included in JSON export after rebuild
- [ ] Test property persistence in Settings Editor

---

## PROPERTY TYPES CONFIRMED

All 24 properties have valid serializable types:

| Type | Count | Properties |
|------|-------|------------|
| bool | 12 | ShowInlineBarChart, IsModified, BarChartShowAxisLabels, BarChartShowGridLines, BarChartShowStatistics, ShowByteToolTip, AutoRefreshParsedFields, EnableAutoFormatDetection, AutoApplyDetectedBlocks, ShowFormatDetectionStatus |
| Color | 2 | BarChartBackgroundColor, BarChartTextColor |
| Visibility | 4 | BarChartPanelVisibility, DataInspectorVisibility, StructureOverlayVisibility, ParsedFieldsPanelVisibility |
| int | 3 | BarChartPanelHeight, DataInspectorByteCount, MaxFormatDetectionSize, LoadedFormatCount |
| double | 1 | ZoomScale |
| string | 1 | FormatDefinitionsPath |
| Enum | 2 | ByteToolTipDisplayMode, ByteToolTipDetailLevel |

---

## SUGGESTED CATEGORY NAMES

Use these consistent category names:

| Category | Purpose | Properties |
|----------|---------|------------|
| Display | General display settings | ShowInlineBarChart, ZoomScale |
| Display.ToolTip | Tooltip-related display | ShowByteToolTip, ByteToolTipDisplayMode, ByteToolTipDetailLevel |
| Data | Data-related properties | IsModified |
| Visual.BarChart | Bar chart visual settings | BarChartPanelVisibility, BarChartPanelHeight, BarChartShowAxisLabels, BarChartShowGridLines, BarChartShowStatistics |
| Colors.BarChart | Bar chart colors | BarChartBackgroundColor, BarChartTextColor |
| DataInspector | Data inspector panel | DataInspectorVisibility, DataInspectorByteCount |
| StructureOverlay | Structure overlay panel | StructureOverlayVisibility |
| ParsedFields | Parsed fields panel | ParsedFieldsPanelVisibility, AutoRefreshParsedFields |
| FormatDetection | Format detection system | EnableAutoFormatDetection, FormatDefinitionsPath, AutoApplyDetectedBlocks, ShowFormatDetectionStatus, MaxFormatDetectionSize, LoadedFormatCount |

---

## EXAMPLE IMPLEMENTATION

### Before:
```csharp
/// <summary>
/// Show or hide inline bar chart
/// </summary>
public bool ShowInlineBarChart
{
    get => (bool)GetValue(ShowInlineBarChartProperty);
    set => SetValue(ShowInlineBarChartProperty, value);
}
```

### After:
```csharp
/// <summary>
/// Show or hide inline bar chart
/// </summary>
[Category("Display")]
public bool ShowInlineBarChart
{
    get => (bool)GetValue(ShowInlineBarChartProperty);
    set => SetValue(ShowInlineBarChartProperty, value);
}
```

---

## VERIFICATION STEPS

After adding categories:

1. **Build the solution** to ensure no compilation errors
2. **Run the PropertyDiscoveryService** to verify properties are discovered:
   ```csharp
   var service = new PropertyDiscoveryService();
   var properties = service.DiscoverProperties(hexEditorInstance);
   ```
3. **Check JSON export** contains the new properties
4. **Test in Settings Editor** that properties appear in appropriate categories
5. **Verify persistence** by:
   - Changing property values
   - Exporting to JSON
   - Creating new HexEditor instance
   - Importing from JSON
   - Confirming values are restored

---

## ADDITIONAL CONSIDERATIONS

### Properties That May Need [Browsable(false)]

Consider hiding these from JSON export if they are:
- Calculated/read-only values (e.g., `LoadedFormatCount`)
- Internal state that shouldn't be persisted

### Properties That Need CLR Wrappers (Future Work)

28 properties currently have NO CLR wrappers and cannot be exposed via JSON. Consider adding wrappers for:
- AllowAutoHighLightSelectionByte
- AllowFileDrop / AllowTextDrop
- FileDroppingConfirmation
- TBL color properties (TblDteColor, TblMteColor, etc.)

---

## ESTIMATED TIME

- **Per property:** 1-2 minutes (locate, add attribute, verify)
- **Total for 24 properties:** ~30-45 minutes
- **Testing and verification:** 15-30 minutes
- **Total estimated time:** 45-75 minutes

---

## SUCCESS METRICS

After completion:
- JSON export should include 81 properties (up from 57)
- All major feature areas should have configurable settings exposed
- Settings Editor should show new properties in appropriate categories
- Property persistence should work correctly for all new properties
