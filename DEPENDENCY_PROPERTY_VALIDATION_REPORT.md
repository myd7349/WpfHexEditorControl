# DEPENDENCY PROPERTY VALIDATION REPORT

**Generated:** 2026-02-23
**Analysis Scope:** All HexEditor DependencyProperty fields

---

## EXECUTIVE SUMMARY

| Metric | Count | Percentage |
|--------|-------|------------|
| **Total Dependency Properties** | 112 | 100% |
| **Exposed in JSON Export** | 57 | 50.9% |
| **NOT Exposed in JSON Export** | 55 | 49.1% |

---

## METHODOLOGY

The analysis examined all `public static readonly DependencyProperty` fields across:
- `HexEditor.xaml.cs`
- All partial class files in `PartialClasses/**/*.cs`

For each DependencyProperty, the corresponding CLR property wrapper was checked for:
1. **[Category]** attribute - REQUIRED for JSON export
2. **[Browsable(false)]** attribute - EXCLUDES from JSON export
3. **Brush type** - Explicitly excluded by PropertyDiscoveryService

---

## CRITICAL FINDINGS

### 24 Properties SHOULD BE EXPOSED but are NOT

These properties have valid types and CLR wrappers but are missing the `[Category]` attribute:

| Property Name | Type | File | Recommended Category |
|---------------|------|------|---------------------|
| **AutoApplyDetectedBlocks** | bool | HexEditor.FormatDetection.cs | FormatDetection |
| **AutoRefreshParsedFields** | bool | HexEditor.ParsedFieldsIntegration.cs | ParsedFields |
| **BarChartBackgroundColor** | Color | HexEditor.xaml.cs | Colors.BarChart |
| **BarChartPanelHeight** | int | HexEditor.xaml.cs | Visual.BarChart |
| **BarChartPanelVisibility** | Visibility | HexEditor.xaml.cs | Visual.BarChart |
| **BarChartShowAxisLabels** | bool | HexEditor.xaml.cs | Visual.BarChart |
| **BarChartShowGridLines** | bool | HexEditor.xaml.cs | Visual.BarChart |
| **BarChartShowStatistics** | bool | HexEditor.xaml.cs | Visual.BarChart |
| **BarChartTextColor** | Color | HexEditor.xaml.cs | Colors.BarChart |
| **ByteToolTipDetailLevel** | ByteToolTipDetailLevel | HexEditor.CompatibilityLayer.Properties.cs | Display.ToolTip |
| **ByteToolTipDisplayMode** | ByteToolTipDisplayMode | HexEditor.CompatibilityLayer.Properties.cs | Display.ToolTip |
| **DataInspectorByteCount** | int | HexEditor.DataInspectorIntegration.cs | DataInspector |
| **DataInspectorVisibility** | Visibility | HexEditor.DataInspectorIntegration.cs | DataInspector |
| **EnableAutoFormatDetection** | bool | HexEditor.FormatDetection.cs | FormatDetection |
| **FormatDefinitionsPath** | string | HexEditor.FormatDetection.cs | FormatDetection |
| **IsModified** | bool | HexEditor.xaml.cs | Data |
| **LoadedFormatCount** | int | HexEditor.FormatDetection.cs | FormatDetection |
| **MaxFormatDetectionSize** | int | HexEditor.FormatDetection.cs | FormatDetection |
| **ParsedFieldsPanelVisibility** | Visibility | HexEditor.ParsedFieldsIntegration.cs | ParsedFields |
| **ShowByteToolTip** | bool | HexEditor.CompatibilityLayer.Properties.cs | Display.ToolTip |
| **ShowFormatDetectionStatus** | bool | HexEditor.FormatDetection.cs | FormatDetection |
| **ShowInlineBarChart** | bool | HexEditor.xaml.cs | Display |
| **StructureOverlayVisibility** | Visibility | HexEditor.StructureOverlayIntegration.cs | StructureOverlay |
| **ZoomScale** | double | HexEditor.Zoom.cs | Display |

### Impact
These 24 properties represent valuable configuration options that users cannot currently discover or persist through the JSON export system.

---

## PROPERTIES CORRECTLY EXCLUDED FROM JSON

### 2 Brush Properties (Intentionally Excluded)
| Property Name | Type | Category | Reason |
|---------------|------|----------|--------|
| SelectionActiveBrush | Brush | Colors.Selection | PropertyDiscoveryService excludes Brush types |
| SelectionInactiveBrush | Brush | Colors.Selection | PropertyDiscoveryService excludes Brush types |

**Note:** These properties have `[Category]` attributes but are excluded because PropertyDiscoveryService skips all Brush types (Color types are preferred).

### 1 Internal Property (Correctly Hidden)
| Property Name | Type | Reason |
|---------------|------|--------|
| ActualOffsetWidth | GridLength | Has `[Browsable(false)]` - internal calculated value |

---

## PROPERTIES WITHOUT CLR WRAPPERS (28 Total)

These properties have ONLY DependencyProperty declarations and are designed for XAML-only binding. They intentionally lack CLR wrappers and cannot be exposed via JSON:

### Behavior Properties (13)
- AllowAutoHighLightSelectionByte (bool)
- AllowAutoSelectSameByteAtDoubleClick (bool)
- AllowByteCount (bool)
- AllowCustomBackgroundBlock (bool)
- AllowDeleteByte (bool)
- AllowExtend (bool)
- AllowFileDrop (bool)
- AllowMarkerClickNavigation (bool)
- AllowTextDrop (bool)
- AppendNeedConfirmation (bool)
- FileDroppingConfirmation (bool)
- ProgressRefreshRate (type unknown)
- CustomEncoding (type unknown)

### Color Properties (9)
- AutoHighLiteSelectionByteBrush (Color)
- BarChartColor (type unknown)
- InlineBarChartColor (type unknown)
- Tbl3ByteColor (type unknown)
- Tbl4PlusByteColor (type unknown)
- TblAsciiColor (type unknown)
- TblDefaultColor (type unknown)
- TblDteColor (type unknown)
- TblEndBlockColor (type unknown)
- TblEndLineColor (type unknown)
- TblJaponaisColor (type unknown)
- TblMteColor (type unknown)

### Other Properties (6)
- ByteShiftLeft (type unknown)
- DefaultCopyToClipboardMode (type unknown)
- VisualCaretMode (type unknown)

**Rationale:** Properties without CLR wrappers are typically designed for internal use or XAML-only scenarios. Adding CLR wrappers would enable JSON export but requires additional implementation work.

---

## PROPERTIES SUCCESSFULLY EXPOSED IN JSON (57 Total)

### By Category

#### Behavior (2)
- AllowContextMenu (bool)
- AllowZoom (bool)

#### Colors.ByteStates (4)
- ByteAddedColor (Color)
- ByteModifiedColor (Color)
- HighLightColor (Color)
- MouseOverColor (Color)

#### Colors.Foreground (5)
- ForegroundContrast (Color)
- ForegroundFirstColor (Color)
- ForegroundHighLightOffSetHeaderColor (Color)
- ForegroundOffSetHeaderColor (Color)
- ForegroundSecondColor (Color)

#### Colors.Selection (2)
- SelectionFirstColor (Color)
- SelectionSecondColor (Color)

#### Data (9)
- EditMode (EditMode)
- FileName (string)
- IsFileOrStreamLoaded (bool)
- IsOperationActive (bool)
- Position (long)
- PreloadByteInEditorMode (PreloadByteInEditor)
- ReadOnlyMode (bool)
- SelectionStart (long)
- SelectionStop (long)

#### Display (4)
- DataStringVisual (DataVisualType)
- OffSetStringVisual (DataVisualType)
- ShowAscii (bool)
- ShowOffset (bool)

#### Keyboard (6)
- AllowBuildinCtrla (bool)
- AllowBuildinCtrlc (bool)
- AllowBuildinCtrlv (bool)
- AllowBuildinCtrly (bool)
- AllowBuildinCtrlz (bool)
- MouseWheelSpeed (MouseWheelSpeed)

#### ScrollMarkers (5)
- ShowBookmarkMarkers (bool)
- ShowDeletedMarkers (bool)
- ShowInsertedMarkers (bool)
- ShowModifiedMarkers (bool)
- ShowSearchResultMarkers (bool)

#### StatusBar (7)
- ShowBytesPerLineInStatusBar (bool)
- ShowEditModeInStatusBar (bool)
- ShowFileSizeInStatusBar (bool)
- ShowPositionInStatusBar (bool)
- ShowRefreshTimeInStatusBar (bool)
- ShowSelectionInStatusBar (bool)
- ShowStatusMessage (bool)

#### TBL (6)
- ShowTblAscii (bool)
- ShowTblDte (bool)
- ShowTblEndBlock (bool)
- ShowTblEndLine (bool)
- ShowTblJaponais (bool)
- ShowTblMte (bool)

#### Visual (7)
- ByteGrouping (ByteSpacerGroup)
- ByteOrder (ByteOrderType)
- BytePerLine (int)
- ByteSize (ByteSizeType)
- ByteSpacerPositioning (ByteSpacerPosition)
- ByteSpacerVisualStyle (ByteSpacerVisual)
- ByteSpacerWidthTickness (ByteSpacerWidth)

---

## RECOMMENDATIONS

### Priority 1: Add [Category] Attributes (24 Properties)
Add `[Category]` attributes to the 24 properties listed in "Critical Findings" section. These are the most valuable additions as they:
- Have valid types for JSON serialization
- Have existing CLR property wrappers
- Represent important configuration options
- Only require adding a single attribute line

**Example Fix:**
```csharp
// Before
public bool ShowInlineBarChart
{
    get => (bool)GetValue(ShowInlineBarChartProperty);
    set => SetValue(ShowInlineBarChartProperty, value);
}

// After
[Category("Display")]
public bool ShowInlineBarChart
{
    get => (bool)GetValue(ShowInlineBarChartProperty);
    set => SetValue(ShowInlineBarChartProperty, value);
}
```

### Priority 2: Consider Adding CLR Wrappers for Key Properties
Evaluate the 28 properties without CLR wrappers and determine which should be exposed via JSON. Candidates include:
- AllowAutoHighLightSelectionByte
- AllowFileDrop / AllowTextDrop
- FileDroppingConfirmation
- TBL color properties (9 properties)

### Priority 3: Validate Category Names
Ensure category names follow a consistent naming convention:
- Use dot notation for subcategories (e.g., "Colors.Selection")
- Group related properties together
- Consider hierarchical organization in UI

---

## VALIDATION RULES

Properties are included in JSON export if ALL of the following are true:
1. ✓ Has a CLR property wrapper
2. ✓ Has `[Category("...")]` attribute
3. ✓ Does NOT have `[Browsable(false)]` attribute
4. ✓ Is NOT of type `Brush` (Color types are preferred)

---

## FILES ANALYZED

- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\HexEditor.xaml.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.Bookmarks.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\UI\HexEditor.Highlights.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.StatePersistence.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Search\HexEditor.FindReplace.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\UI\HexEditor.Clipboard.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.BatchOperations.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.Diagnostics.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.EditOperations.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Compatibility\HexEditor.CompatibilityLayer.Methods.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.ByteOperations.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Search\HexEditor.RelativeSearch.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Search\HexEditor.Search.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.AsyncOperations.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.StreamOperations.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.FileComparison.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.FileOperations.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.CustomBackgroundBlocks.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.DataInspectorIntegration.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.StructureOverlayIntegration.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\UI\HexEditor.UIHelpers.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\UI\HexEditor.Zoom.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.IPSPatcher.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.ParsedFieldsIntegration.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.TBL.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\UI\HexEditor.Events.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Compatibility\HexEditor.CompatibilityLayer.Properties.cs
- C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.FormatDetection.cs

---

## APPENDIX: RAW DATA

Full detailed CSV report available at:
`C:\Users\khens\source\repos\WpfHexEditorControl\dp_analysis_report.csv`
