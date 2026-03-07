# ==========================================================
# Add-FileHeaders.ps1
# Injects standardized file headers into all .cs and .xaml files
# of the WpfHexEditorControl solution.
#
# Usage:
#   .\Add-FileHeaders.ps1                   # Dry-run (preview only)
#   .\Add-FileHeaders.ps1 -Apply            # Apply changes
#   .\Add-FileHeaders.ps1 -Apply -Verbose   # Apply + detailed log
#
# Skip rule: files already starting with "// ==========" are left untouched.
# ==========================================================

param(
    [switch]$Apply,
    [switch]$Verbose
)

$solutionRoot = Split-Path $PSScriptRoot -Parent
$sourcesRoot  = Join-Path $solutionRoot "Sources"
$author       = "Derek Tremblay (derektremblay666@gmail.com)"
$contributor  = "Claude Sonnet 4.6"
$created      = "2026-03-06"

$stats = @{ Skipped = 0; Updated = 0; Errors = 0 }

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Get-ProjectName([string]$filePath) {
    # Walk up from file until we find a .csproj, return its name without ext
    $dir = Split-Path $filePath -Parent
    while ($dir -ne $null -and $dir -ne "") {
        $csproj = Get-ChildItem -Path $dir -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($csproj) { return [System.IO.Path]::GetFileNameWithoutExtension($csproj.Name) }
        $parent = Split-Path $dir -Parent
        if ($parent -eq $dir) { break }
        $dir = $parent
    }
    return "WpfHexEditorControl"
}

function Get-PrimaryTypeName([string]$content) {
    # Extract first class / interface / enum / struct / record name
    if ($content -match '(?m)^\s*(?:public|internal|protected|private)?\s*(?:static\s+|abstract\s+|sealed\s+|partial\s+)*(?:class|interface|enum|struct|record)\s+(\w+)') {
        return $Matches[1]
    }
    return $null
}

function Get-NamespaceName([string]$content) {
    if ($content -match '(?m)^namespace\s+([\w\.]+)') {
        return $Matches[1]
    }
    return $null
}

function Build-CsDescription([string]$typeName, [string]$ns, [string]$fileName) {
    if (-not $typeName) { $typeName = [System.IO.Path]::GetFileNameWithoutExtension($fileName) }

    # Infer a human-readable category from namespace segments
    $category = ""
    if ($ns -match "\.Bytes")          { $category = "byte-level data provider" }
    elseif ($ns -match "\.Cache")      { $category = "caching utility" }
    elseif ($ns -match "\.CharacterTable") { $category = "character table parser" }
    elseif ($ns -match "\.FormatDetection") { $category = "format detection component" }
    elseif ($ns -match "\.RomHacking") { $category = "ROM patching utility" }
    elseif ($ns -match "\.Search")     { $category = "search/find functionality" }
    elseif ($ns -match "\.ViewModel|ViewModels") { $category = "ViewModel (MVVM)" }
    elseif ($ns -match "\.Converter")  { $category = "WPF value converter" }
    elseif ($ns -match "\.Command")    { $category = "ICommand implementation" }
    elseif ($ns -match "\.Dialog")     { $category = "dialog window" }
    elseif ($ns -match "\.Service")    { $category = "application service" }
    elseif ($ns -match "\.Adapter")    { $category = "adapter (Adapter pattern)" }
    elseif ($ns -match "\.Extension")  { $category = "extension method class" }
    elseif ($ns -match "\.Interface|Interfaces") { $category = "interface contract" }
    elseif ($ns -match "\.Model|Models") { $category = "domain model" }
    elseif ($ns -match "\.Panel|Panels") { $category = "dockable IDE panel" }
    elseif ($typeName -match "^I[A-Z]") { $category = "interface contract" }
    elseif ($typeName -match "ViewModel$") { $category = "ViewModel (MVVM)" }
    elseif ($typeName -match "Converter$") { $category = "WPF value converter" }
    elseif ($typeName -match "Command$")   { $category = "ICommand implementation" }
    elseif ($typeName -match "Service$")   { $category = "application service" }
    elseif ($typeName -match "Adapter$")   { $category = "adapter (Adapter pattern)" }
    elseif ($typeName -match "Factory$")   { $category = "factory" }
    elseif ($typeName -match "Manager$")   { $category = "manager/coordinator" }
    elseif ($typeName -match "Window$")    { $category = "WPF window" }
    elseif ($typeName -match "Panel$")     { $category = "dockable IDE panel" }
    elseif ($typeName -match "Control$")   { $category = "WPF custom control" }
    else { $category = "component" }

    $desc  = "    Provides $typeName $category for the WpfHexEditorControl IDE."
    $desc += "`n//     Implement or extend this type to integrate with the surrounding"
    $desc += "`n//     module. See Architecture Notes for dependency context."

    return $desc
}

function Build-ArchNotes([string]$typeName, [string]$ns, [string]$fileName) {
    $notes = "    Part of the $((($ns -split '\.')[-2..-1]) -join '.') layer."
    if ($typeName -match "^I[A-Z]") {
        $notes += "`n//     Interface — implementations must be registered via the appropriate"
        $notes += "`n//     registry or DI container."
    } elseif ($typeName -match "ViewModel$") {
        $notes += "`n//     MVVM pattern — no direct WPF UI references; data-bound from XAML."
    } elseif ($typeName -match "Converter$") {
        $notes += "`n//     Stateless WPF converter; safe for shared StaticResource usage."
    } elseif ($typeName -match "Adapter$") {
        $notes += "`n//     Adapter pattern — wraps internal App services behind SDK interfaces"
        $notes += "`n//     so that PluginHost never references WpfHexEditor.App directly."
    } elseif ($typeName -match "Service$") {
        $notes += "`n//     Service layer — injected via IDEHostContext; no direct UI coupling."
    } else {
        $notes += "`n//     No external WPF theme dependencies unless explicitly noted."
    }
    return $notes
}

function New-CsHeader([string]$projectName, [string]$fileName, [string]$desc, [string]$archNotes) {
    return @"
// ==========================================================
// Project: $projectName
// File: $fileName
// Author: $author
// Contributors: $contributor
// Created: $created
// Description:
//$desc
//
// Architecture Notes:
//$archNotes
//
// ==========================================================
"@
}

function New-XamlHeader([string]$projectName, [string]$fileName, [string]$desc, [string]$archNotes) {
    # Convert // lines to plain text for XML comment
    $d = $desc  -replace '^//\s*','' -replace '`n//',"`n    "
    $a = $archNotes -replace '^//\s*','' -replace '`n//',"`n    "
    return @"
<!-- ==========================================================
     Project: $projectName
     File: $fileName
     Author: $author
     Contributors: $contributor
     Created: $created
     Description:
         $d

     Architecture Notes:
         $a
     ========================================================== -->
"@
}

# ---------------------------------------------------------------------------
# Apache header detection — returns the block to replace (or $null)
# ---------------------------------------------------------------------------
function Get-ApacheBlock([string]$content) {
    # Match the opening slashline, optional lines, closing slashline
    if ($content -match '(?s)^(/{20,}.*?/{20,}\r?\n)') {
        return $Matches[1]
    }
    return $null
}

# ---------------------------------------------------------------------------
# Process a single .cs file
# ---------------------------------------------------------------------------
function Process-CsFile([string]$path) {
    try {
        $content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

        # Already has proper new header?
        if ($content -match '^// ==========') {
            # Check if it has Contributors line; if not, update it
            if ($content -match 'Contributors:') {
                if ($Verbose) { Write-Host "  SKIP (compliant): $path" -ForegroundColor DarkGray }
                $stats.Skipped++
                return
            }
        }

        $fileName    = Split-Path $path -Leaf
        $projectName = Get-ProjectName $path
        $typeName    = Get-PrimaryTypeName $content
        $ns          = Get-NamespaceName $content
        $desc        = Build-CsDescription $typeName $ns $fileName
        $archNotes   = Build-ArchNotes $typeName $ns $fileName
        $newHeader   = New-CsHeader $projectName $fileName $desc $archNotes

        $apacheBlock = Get-ApacheBlock $content
        $newContent  = $null

        if ($content -match '^// ==========') {
            # Has new format but missing Contributors — rebuild header
            $newContent = $content -replace '(?s)^// ==========.*?// ==========\r?\n', "$newHeader`n"
        } elseif ($apacheBlock) {
            $newContent = $content.Replace($apacheBlock, "$newHeader`n")
        } else {
            $newContent = "$newHeader`n$content"
        }

        if ($Apply) {
            [System.IO.File]::WriteAllText($path, $newContent, [System.Text.Encoding]::UTF8WithoutBOM)
        }
        Write-Host "  UPDATE: $path" -ForegroundColor Green
        $stats.Updated++
    } catch {
        Write-Host "  ERROR: $path — $_" -ForegroundColor Red
        $stats.Errors++
    }
}

# ---------------------------------------------------------------------------
# Process a single .xaml file
# ---------------------------------------------------------------------------
function Process-XamlFile([string]$path) {
    try {
        $content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

        # Already has proper new header?
        if ($content -match '<!-- =========') {
            if ($Verbose) { Write-Host "  SKIP (compliant): $path" -ForegroundColor DarkGray }
            $stats.Skipped++
            return
        }

        $fileName    = Split-Path $path -Leaf
        $projectName = Get-ProjectName $path
        $typeName    = [System.IO.Path]::GetFileNameWithoutExtension($fileName)
        $desc        = "    $typeName — WPF view/resource file for the $projectName module."
        $archNotes   = "    Theme applied via merged ResourceDictionaries (PanelCommon + theme-specific)."
        $newHeader   = New-XamlHeader $projectName $fileName $desc $archNotes

        # Insert after <?xml ...?> declaration if present, else prepend
        if ($content -match '^<\?xml') {
            $newContent = $content -replace '(?m)^(<\?xml[^\n]*\n)', "`$1$newHeader`n"
        } else {
            $newContent = "$newHeader`n$content"
        }

        if ($Apply) {
            [System.IO.File]::WriteAllText($path, $newContent, [System.Text.Encoding]::UTF8WithoutBOM)
        }
        Write-Host "  UPDATE: $path" -ForegroundColor Green
        $stats.Updated++
    } catch {
        Write-Host "  ERROR: $path — $_" -ForegroundColor Red
        $stats.Errors++
    }
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
if (-not $Apply) {
    Write-Host "=== DRY-RUN mode (use -Apply to write changes) ===" -ForegroundColor Yellow
}

Write-Host "Scanning: $sourcesRoot" -ForegroundColor Cyan

$csFiles   = Get-ChildItem -Path $sourcesRoot -Recurse -Filter "*.cs"   | Where-Object { $_.FullName -notmatch '\\obj\\' }
$xamlFiles = Get-ChildItem -Path $sourcesRoot -Recurse -Filter "*.xaml" | Where-Object { $_.FullName -notmatch '\\obj\\' }

Write-Host "Found $($csFiles.Count) .cs files and $($xamlFiles.Count) .xaml files" -ForegroundColor Cyan

foreach ($f in $csFiles)   { Process-CsFile   $f.FullName }
foreach ($f in $xamlFiles) { Process-XamlFile $f.FullName }

Write-Host ""
Write-Host "=== DONE ===" -ForegroundColor Cyan
Write-Host "  Updated : $($stats.Updated)"  -ForegroundColor Green
Write-Host "  Skipped : $($stats.Skipped)"  -ForegroundColor DarkGray
Write-Host "  Errors  : $($stats.Errors)"   -ForegroundColor Red

if (-not $Apply -and $stats.Updated -gt 0) {
    Write-Host ""
    Write-Host "Re-run with -Apply to write $($stats.Updated) file(s)." -ForegroundColor Yellow
}
