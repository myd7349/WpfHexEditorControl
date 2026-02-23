# Analyze all Dependency Properties in HexEditor - Improved Version
$files = @(
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\HexEditor.xaml.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.Bookmarks.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\UI\HexEditor.Highlights.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.StatePersistence.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Search\HexEditor.FindReplace.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\UI\HexEditor.Clipboard.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.BatchOperations.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.Diagnostics.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.EditOperations.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Compatibility\HexEditor.CompatibilityLayer.Methods.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.ByteOperations.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Search\HexEditor.RelativeSearch.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Search\HexEditor.Search.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.AsyncOperations.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.StreamOperations.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.FileComparison.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Core\HexEditor.FileOperations.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.CustomBackgroundBlocks.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.DataInspectorIntegration.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.StructureOverlayIntegration.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\UI\HexEditor.UIHelpers.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\UI\HexEditor.Zoom.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.IPSPatcher.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.ParsedFieldsIntegration.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.TBL.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\UI\HexEditor.Events.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Compatibility\HexEditor.CompatibilityLayer.Properties.cs",
    "C:\Users\khens\source\repos\WpfHexEditorControl\Sources\WPFHexaEditor\PartialClasses\Features\HexEditor.FormatDetection.cs"
)

$results = @()
$totalDP = 0
$exposedCount = 0
$notExposedCount = 0

foreach ($file in $files) {
    if (-not (Test-Path $file)) {
        continue
    }

    $content = Get-Content $file -Raw

    # Find all DependencyProperty declarations
    $dpMatches = [regex]::Matches($content, 'public static readonly DependencyProperty (\w+)Property')

    foreach ($match in $dpMatches) {
        $totalDP++
        $dpName = $match.Groups[1].Value
        $propertyName = $dpName

        # Search for CLR property in entire file (could be before OR after DP)
        # Pattern 1: Look for [Category] attribute followed by property
        $categoryPattern = "\[Category\(`"([^`"]+)`"\)\][^\[]*?public\s+(\w+(?:<[^>]+>)?)\s+$propertyName\s*\{"
        # Pattern 2: Look for [Browsable(false)]
        $browsablePattern = "\[Browsable\(false\)\][^\[]*?public\s+(\w+(?:<[^>]+>)?)\s+$propertyName\s*\{"
        # Pattern 3: Look for property without attributes
        $propertyPatternNoAttrs = "(?<!\[)public\s+(\w+(?:<[^>]+>)?)\s+$propertyName\s*\{"

        $hasCategory = $false
        $category = ""
        $propertyType = "Unknown"
        $hasBrowsable = $false
        $foundProperty = $false

        # Check for [Category] attribute
        if ($content -match $categoryPattern) {
            $hasCategory = $true
            $category = $Matches[1]
            $propertyType = $Matches[2]
            $foundProperty = $true
        }

        # Check for [Browsable(false)]
        if ($content -match $browsablePattern) {
            $hasBrowsable = $true
            if ($propertyType -eq "Unknown") {
                $propertyType = $Matches[1]
            }
            $foundProperty = $true
        }

        # If no attributes found, look for plain property
        if (-not $foundProperty -and $content -match $propertyPatternNoAttrs) {
            $propertyType = $Matches[1]
            $foundProperty = $true
        }

        # Clean up property type
        $propertyType = $propertyType -replace '\s+', ''

        $isBrushType = $propertyType -eq 'Brush' -or $propertyType -like '*Brush' -or $propertyType -like 'System.Windows.Media.Brush'

        $exposed = $hasCategory -and -not $hasBrowsable -and -not $isBrushType

        if ($exposed) {
            $exposedCount++
        } else {
            $notExposedCount++
        }

        $results += [PSCustomObject]@{
            PropertyName = $propertyName
            Type = $propertyType
            HasCategory = $hasCategory
            Category = $category
            HasBrowsableFalse = $hasBrowsable
            IsBrushType = $isBrushType
            WillBeExposed = $exposed
            File = Split-Path $file -Leaf
            Reason = if (-not $exposed) {
                $reasons = @()
                if (-not $hasCategory) { $reasons += "Missing [Category]" }
                if ($hasBrowsable) { $reasons += "Has [Browsable(false)]" }
                if ($isBrushType) { $reasons += "Brush type (excluded by PropertyDiscoveryService)" }
                $reasons -join "; "
            } else { "" }
        }
    }
}

# Generate report
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DEPENDENCY PROPERTY VALIDATION REPORT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "SUMMARY:" -ForegroundColor Yellow
Write-Host "--------"
Write-Host "Total Dependency Properties: $totalDP"
Write-Host "Properties EXPOSED in JSON:  $exposedCount" -ForegroundColor Green
Write-Host "Properties NOT EXPOSED:      $notExposedCount" -ForegroundColor Red
Write-Host ""

Write-Host "PROPERTIES NOT EXPOSED TO JSON:" -ForegroundColor Red
Write-Host "-------------------------------"
$notExposed = $results | Where-Object { -not $_.WillBeExposed } | Sort-Object PropertyName

foreach ($prop in $notExposed) {
    Write-Host ""
    Write-Host "Property: $($prop.PropertyName)" -ForegroundColor Yellow
    Write-Host "  Type:     $($prop.Type)"
    Write-Host "  File:     $($prop.File)"
    Write-Host "  Category: $($prop.Category)"
    Write-Host "  Reason:   $($prop.Reason)" -ForegroundColor Red
}

Write-Host ""
Write-Host ""
Write-Host "CRITICAL ISSUES - Properties that SHOULD be exposed but are NOT:" -ForegroundColor Magenta
Write-Host "----------------------------------------------------------------"

# Properties that look like they should be exposed
$shouldBeExposed = $notExposed | Where-Object {
    $_.Type -ne "Unknown" -and
    -not $_.HasBrowsableFalse -and
    -not $_.IsBrushType
}

if ($shouldBeExposed.Count -eq 0) {
    Write-Host "None found - all valid properties are properly categorized!" -ForegroundColor Green
} else {
    foreach ($prop in $shouldBeExposed) {
        Write-Host ""
        Write-Host "Property: $($prop.PropertyName)" -ForegroundColor Yellow
        Write-Host "  Type:     $($prop.Type)"
        Write-Host "  File:     $($prop.File)"
        Write-Host "  Issue:    Missing [Category] attribute - should be added!" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host ""
Write-Host "PROPERTIES EXPOSED TO JSON:" -ForegroundColor Green
Write-Host "--------------------------"
$exposed = $results | Where-Object { $_.WillBeExposed } | Sort-Object Category, PropertyName

$groupedByCategory = $exposed | Group-Object Category

foreach ($group in $groupedByCategory) {
    Write-Host ""
    Write-Host "Category: $($group.Name)" -ForegroundColor Cyan
    foreach ($prop in $group.Group) {
        Write-Host "  - $($prop.PropertyName) ($($prop.Type))"
    }
}

# Export to CSV
$csvPath = "C:\Users\khens\source\repos\WpfHexEditorControl\dp_analysis_report.csv"
$results | Export-Csv -Path $csvPath -NoTypeInformation
Write-Host ""
Write-Host "Full report exported to: $csvPath" -ForegroundColor Green
