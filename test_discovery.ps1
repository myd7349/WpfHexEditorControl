# Test script to verify PropertyDiscoveryService finds Tooltip properties
# Run this in PowerShell to check if properties are discovered

$sourceFile = "Sources\WPFHexaEditor\Core\Settings\PropertyDiscoveryService.cs"
$hexEditorFile = "Sources\WPFHexaEditor\PartialClasses\Compatibility\HexEditor.CompatibilityLayer.Properties.cs"

Write-Host "=== Checking Tooltip Properties ===" -ForegroundColor Cyan

# Check if DP declarations exist
Write-Host "`nDependencyProperty declarations:" -ForegroundColor Yellow
Get-Content $hexEditorFile | Select-String "ByteToolTip.*Property\s*=" | ForEach-Object {
    Write-Host "  Line $($_.LineNumber): $($_.Line.Trim())"
}

# Check if Category attributes exist
Write-Host "`nCategory attributes:" -ForegroundColor Yellow
Get-Content $hexEditorFile | Select-String -Pattern '\[.*Category\("Tooltip"\)\]' -Context 0,3 | ForEach-Object {
    Write-Host "  Line $($_.LineNumber): $($_.Line.Trim())"
    $_.Context.PostContext | ForEach-Object { Write-Host "    $_" }
}

# Check PropertyDiscoveryService logic
Write-Host "`nPropertyDiscoveryService checks:" -ForegroundColor Yellow
Write-Host "  1. Has [Category] attribute? Check lines 42-44 in PropertyDiscoveryService.cs"
Write-Host "  2. Has corresponding DependencyProperty field? Check lines 53-58"
Write-Host "  3. Is Browsable? Check lines 47-49"
Write-Host "  4. Is Brush type? Check lines 67-68"

Write-Host "`nExpected properties to be discovered:" -ForegroundColor Green
Write-Host "  - ByteToolTipDisplayMode (enum, Category='Tooltip')"
Write-Host "  - ByteToolTipDetailLevel (enum, Category='Tooltip')"
Write-Host "  - ShowByteToolTip (bool, Category='Display', BUT Browsable=false => EXCLUDED)"

Write-Host "`n=== Recommendation ===" -ForegroundColor Cyan
Write-Host "Run the Sample.Main application and check if these settings persist after app restart."
Write-Host "If not, the issue might be in SettingsStateService enum serialization."
