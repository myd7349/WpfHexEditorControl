#!/usr/bin/env pwsh
# Add-ArchiveTokens.ps1
# Inserts AR_* theme tokens for the ArchiveExplorer plugin into all 18 Shell Colors.xaml files.
# Usage: pwsh Add-ArchiveTokens.ps1

param([switch]$DryRun)

$themesRoot = "$PSScriptRoot\Themes"

# Token values per theme — keyed by folder name
# AR_FolderBrush            : folder icon colour (warm amber family)
# AR_FileBrush              : file icon colour (use text foreground)
# AR_RatioBadgeBackgroundBrush : compression-ratio badge bg (blue family)
# AR_RatioBadgeForegroundBrush : compression-ratio badge text
# AR_FormatBadgeBackgroundBrush: whfmt format badge bg (green family)
# AR_FormatBadgeForegroundBrush: whfmt format badge text
# AR_NestIndicatorBrush     : nested archive breadcrumb arrow

$tokens = @{
    "VS2022Dark"      = @{ Folder="#DCB67A"; File="#D4D4D4"; RatioBg="#2D5A8E"; RatioFg="#FFFFFF"; FmtBg="#3A5A3A"; FmtFg="#C8E6C9"; Nest="#858585" }
    "VisualStudio"    = @{ Folder="#795E26"; File="#1E1E1E"; RatioBg="#0E639C"; RatioFg="#FFFFFF"; FmtBg="#107C10"; FmtFg="#FFFFFF"; Nest="#717171" }
    "Dracula"         = @{ Folder="#FFB86C"; File="#F8F8F2"; RatioBg="#6272A4"; RatioFg="#F8F8F2"; FmtBg="#50FA7B"; FmtFg="#282A36"; Nest="#6272A4" }
    "Nord"            = @{ Folder="#EBCB8B"; File="#D8DEE9"; RatioBg="#5E81AC"; RatioFg="#ECEFF4"; FmtBg="#A3BE8C"; FmtFg="#2E3440"; Nest="#4C566A" }
    "CatppuccinMocha" = @{ Folder="#FAB387"; File="#CDD6F4"; RatioBg="#89B4FA"; RatioFg="#1E1E2E"; FmtBg="#A6E3A1"; FmtFg="#1E1E2E"; Nest="#6C7086" }
    "CatppuccinLatte" = @{ Folder="#FE640B"; File="#4C4F69"; RatioBg="#1E66F5"; RatioFg="#EFF1F5"; FmtBg="#40A02B"; FmtFg="#EFF1F5"; Nest="#9CA0B0" }
    "TokyoNight"      = @{ Folder="#FF9E64"; File="#A9B1D6"; RatioBg="#7AA2F7"; RatioFg="#1A1B26"; FmtBg="#9ECE6A"; FmtFg="#1A1B26"; Nest="#444B6A" }
    "Cyberpunk"       = @{ Folder="#FF8C00"; File="#E0E0E0"; RatioBg="#00BFFF"; RatioFg="#0D0D0D"; FmtBg="#00FF7F"; FmtFg="#0D0D0D"; Nest="#555555" }
    "Synthwave84"     = @{ Folder="#FEA250"; File="#FFFFFF"; RatioBg="#F92AED"; RatioFg="#FFFFFF"; FmtBg="#72F1B8"; FmtFg="#1A1A2E"; Nest="#6272A4" }
    "GruvboxDark"     = @{ Folder="#FABD2F"; File="#EBDBB2"; RatioBg="#458588"; RatioFg="#FBF1C7"; FmtBg="#98971A"; FmtFg="#FBF1C7"; Nest="#928374" }
    "Matrix"          = @{ Folder="#00FF41"; File="#00CC33"; RatioBg="#004400"; RatioFg="#00FF41"; FmtBg="#003300"; FmtFg="#00FF41"; Nest="#005500" }
    "DarkGlass"       = @{ Folder="#FFC66D"; File="#D4D4D4"; RatioBg="#3A7EBF"; RatioFg="#FFFFFF"; FmtBg="#3A7A3A"; FmtFg="#C8E6C9"; Nest="#777777" }
    "Forest"          = @{ Folder="#D4A843"; File="#D4C9B0"; RatioBg="#3B6E4F"; RatioFg="#F0EDE0"; FmtBg="#2E5A3B"; FmtFg="#C8E6C9"; Nest="#6B7B5E" }
    "HighContrast"    = @{ Folder="#FFFF00"; File="#FFFFFF"; RatioBg="#1AEBFF"; RatioFg="#000000"; FmtBg="#00FF00"; FmtFg="#000000"; Nest="#FFFFFF" }
    "Minimal"         = @{ Folder="#B8860B"; File="#333333"; RatioBg="#4A90D9"; RatioFg="#FFFFFF"; FmtBg="#4A8A4A"; FmtFg="#FFFFFF"; Nest="#999999" }
    "Office"          = @{ Folder="#D97706"; File="#1F1F1F"; RatioBg="#0078D4"; RatioFg="#FFFFFF"; FmtBg="#107C10"; FmtFg="#FFFFFF"; Nest="#767676" }
}

$block = @"

    <!-- Archive Explorer tokens (AR_*) — inserted by Add-ArchiveTokens.ps1 -->
    <SolidColorBrush x:Key="AR_FolderBrush"                  Color="{0}" />
    <SolidColorBrush x:Key="AR_FileBrush"                    Color="{1}" />
    <SolidColorBrush x:Key="AR_RatioBadgeBackgroundBrush"    Color="{2}" />
    <SolidColorBrush x:Key="AR_RatioBadgeForegroundBrush"    Color="{3}" />
    <SolidColorBrush x:Key="AR_FormatBadgeBackgroundBrush"   Color="{4}" />
    <SolidColorBrush x:Key="AR_FormatBadgeForegroundBrush"   Color="{5}" />
    <SolidColorBrush x:Key="AR_NestIndicatorBrush"           Color="{6}" />
"@

$inserted = 0
Get-ChildItem -Path $themesRoot -Filter "Colors.xaml" -Recurse | ForEach-Object {
    $theme = $_.Directory.Name
    if (-not $tokens.ContainsKey($theme)) {
        Write-Warning "No token values for theme '$theme' — skipping."
        return
    }
    $t   = $tokens[$theme]
    $xml = Get-Content $_.FullName -Raw

    if ($xml -match 'AR_FolderBrush') {
        Write-Host "  [SKIP] $theme (already has AR_* tokens)"
        return
    }

    $snippet = $block -f $t.Folder, $t.File, $t.RatioBg, $t.RatioFg, $t.FmtBg, $t.FmtFg, $t.Nest
    $xml     = $xml -replace '</ResourceDictionary>', "$snippet`n</ResourceDictionary>"

    if (-not $DryRun) {
        Set-Content $_.FullName $xml -NoNewline
    }
    Write-Host "  [OK]   $theme"
    $inserted++
}

Write-Host "`nInserted AR_* tokens into $inserted file(s)."
