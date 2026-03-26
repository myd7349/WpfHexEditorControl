# ==========================================================
# Add-BDiffTokens.ps1
# Inserts 14 BDiff_* SolidColorBrush tokens into every Colors.xaml
# theme file (16 Shell themes + 2 Docking.Wpf themes).
#
# Tokens added:
#   BDiff_EqualByteBrush               - transparent (equal bytes: no highlight)
#   BDiff_ModifiedByteBrush            - per-byte background for modified bytes
#   BDiff_ModifiedByteStrongBrush      - stronger tint for the changed-value cell
#   BDiff_InsertedByteBrush            - per-byte background for inserted bytes
#   BDiff_DeletedByteBrush             - per-byte background for deleted bytes
#   BDiff_PaddingBrush                 - subtle tint for alignment padding slots
#   BDiff_OffsetForegroundBrush        - text colour for the offset column
#   BDiff_OffsetBackgroundBrush        - gutter background behind the offset column
#   BDiff_SeparatorBrush               - vertical divider between left and right halves
#   BDiff_AsciiForegroundBrush         - foreground for ASCII panel characters
#   BDiff_AsciiNonPrintableBrush       - foreground for '.' non-printable placeholder
#   BDiff_ContextRowBackgroundBrush    - very subtle tint for context (equal) rows
#   BDiff_CollapsedContextBrush        - background for "--- N identical rows ---" banner
#   BDiff_CollapsedContextFgBrush      - foreground for the fold banner text
#
# Run from: Sources/
# ==========================================================
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root  = $PSScriptRoot
$shell = Join-Path $root 'WpfHexEditor.Shell\Themes'
$dock  = Join-Path $root 'WpfHexEditor.Docking.Wpf\Themes'

# ---------------------------------------------------------------------------
# Per-theme color map
# ---------------------------------------------------------------------------
$dark = @{
    EqualByte         = '#00000000'
    ModifiedByte      = '#4D3A00'
    ModifiedByteStrong= '#6B5000'
    InsertedByte      = '#1A3320'
    DeletedByte       = '#3A1A1A'
    Padding           = '#1C1C1C'
    OffsetFg          = '#858585'
    OffsetBg          = '#1E1E1E'
    Separator         = '#3F3F46'
    AsciiFg           = '#C8C8C8'
    AsciiNonPrintable = '#555555'
    ContextRowBg      = '#1A1A1A'
    CollapsedBg       = '#252526'
    CollapsedFg       = '#858585'
}

$light = @{
    EqualByte         = '#00000000'
    ModifiedByte      = '#FFF3CD'
    ModifiedByteStrong= '#FFE082'
    InsertedByte      = '#E8F5E9'
    DeletedByte       = '#FFEBEE'
    Padding           = '#F5F5F5'
    OffsetFg          = '#AAAAAA'
    OffsetBg          = '#F3F3F3'
    Separator         = '#DDDDDD'
    AsciiFg           = '#333333'
    AsciiNonPrintable = '#BBBBBB'
    ContextRowBg      = '#FAFAFA'
    CollapsedBg       = '#F0F0F0'
    CollapsedFg       = '#AAAAAA'
}

$themes = @{
    'VS2022Dark'      = $dark
    'VisualStudio'    = $light
    'CatppuccinMocha' = @{ EqualByte='#00000000'; ModifiedByte='#3D3000'; ModifiedByteStrong='#5A4500'; InsertedByte='#1A2E1A'; DeletedByte='#2E1A1A'; Padding='#1E1E2E'; OffsetFg='#6C7086'; OffsetBg='#181825'; Separator='#313244'; AsciiFg='#CDD6F4'; AsciiNonPrintable='#45475A'; ContextRowBg='#1E1E2E'; CollapsedBg='#181825'; CollapsedFg='#6C7086' }
    'CatppuccinLatte' = @{ EqualByte='#00000000'; ModifiedByte='#FEF3E2'; ModifiedByteStrong='#FDE8B0'; InsertedByte='#E8F5E9'; DeletedByte='#FDECEA'; Padding='#EFF1F5'; OffsetFg='#7C7F93'; OffsetBg='#E6E9EF'; Separator='#CCD0DA'; AsciiFg='#4C4F69'; AsciiNonPrintable='#9CA0B0'; ContextRowBg='#EFF1F5'; CollapsedBg='#E6E9EF'; CollapsedFg='#7C7F93' }
    'Cyberpunk'       = @{ EqualByte='#00000000'; ModifiedByte='#1A1400'; ModifiedByteStrong='#2A2000'; InsertedByte='#001A0D'; DeletedByte='#1A000D'; Padding='#0D0D1A'; OffsetFg='#00FF9F'; OffsetBg='#0A0A14'; Separator='#1A1A33'; AsciiFg='#00FFFF'; AsciiNonPrintable='#005533'; ContextRowBg='#0D0D1A'; CollapsedBg='#0A0A14'; CollapsedFg='#00FF9F' }
    'DarkGlass'       = @{ EqualByte='#00000000'; ModifiedByte='#2A2200'; ModifiedByteStrong='#3D3200'; InsertedByte='#0D2010'; DeletedByte='#200D0D'; Padding='#121212'; OffsetFg='#808080'; OffsetBg='#181820'; Separator='#2A2A2A'; AsciiFg='#CCCCCC'; AsciiNonPrintable='#444444'; ContextRowBg='#121212'; CollapsedBg='#181820'; CollapsedFg='#808080' }
    'Dracula'         = @{ EqualByte='#00000000'; ModifiedByte='#38310E'; ModifiedByteStrong='#504500'; InsertedByte='#0E2E14'; DeletedByte='#2E0E0E'; Padding='#21222C'; OffsetFg='#6272A4'; OffsetBg='#191A21'; Separator='#2E303E'; AsciiFg='#F8F8F2'; AsciiNonPrintable='#44475A'; ContextRowBg='#21222C'; CollapsedBg='#191A21'; CollapsedFg='#6272A4' }
    'Forest'          = @{ EqualByte='#00000000'; ModifiedByte='#252200'; ModifiedByteStrong='#363200'; InsertedByte='#0D2010'; DeletedByte='#201010'; Padding='#16201A'; OffsetFg='#6A8060'; OffsetBg='#121A14'; Separator='#1E2A1E'; AsciiFg='#C8D8B8'; AsciiNonPrintable='#3A4A38'; ContextRowBg='#16201A'; CollapsedBg='#121A14'; CollapsedFg='#6A8060' }
    'GruvboxDark'     = @{ EqualByte='#00000000'; ModifiedByte='#352D00'; ModifiedByteStrong='#4A3E00'; InsertedByte='#1A2A10'; DeletedByte='#2A1010'; Padding='#1D2021'; OffsetFg='#928374'; OffsetBg='#1D2021'; Separator='#3C3836'; AsciiFg='#EBDBB2'; AsciiNonPrintable='#504945'; ContextRowBg='#1D2021'; CollapsedBg='#282828'; CollapsedFg='#928374' }
    'HighContrast'    = @{ EqualByte='#00000000'; ModifiedByte='#333300'; ModifiedByteStrong='#555500'; InsertedByte='#003300'; DeletedByte='#330000'; Padding='#000000'; OffsetFg='#FFFFFF'; OffsetBg='#000000'; Separator='#555555'; AsciiFg='#FFFFFF'; AsciiNonPrintable='#777777'; ContextRowBg='#000000'; CollapsedBg='#111111'; CollapsedFg='#FFFFFF' }
    'Matrix'          = @{ EqualByte='#00000000'; ModifiedByte='#0A1400'; ModifiedByteStrong='#142000'; InsertedByte='#002A00'; DeletedByte='#140000'; Padding='#000A00'; OffsetFg='#00AA41'; OffsetBg='#000800'; Separator='#001400'; AsciiFg='#00FF41'; AsciiNonPrintable='#004A14'; ContextRowBg='#000A00'; CollapsedBg='#000800'; CollapsedFg='#00AA41' }
    'Minimal'         = $light
    'Nord'            = @{ EqualByte='#00000000'; ModifiedByte='#35300A'; ModifiedByteStrong='#4A4200'; InsertedByte='#0E2620'; DeletedByte='#2A1010'; Padding='#2E3440'; OffsetFg='#4C566A'; OffsetBg='#2E3440'; Separator='#3B4252'; AsciiFg='#ECEFF4'; AsciiNonPrintable='#4C566A'; ContextRowBg='#2E3440'; CollapsedBg='#3B4252'; CollapsedFg='#4C566A' }
    'Office'          = $light
    'Synthwave84'     = @{ EqualByte='#00000000'; ModifiedByte='#2A2000'; ModifiedByteStrong='#3D3000'; InsertedByte='#00200E'; DeletedByte='#2A001A'; Padding='#1A1A2E'; OffsetFg='#848CA8'; OffsetBg='#16162A'; Separator='#2A2044'; AsciiFg='#FF8B39'; AsciiNonPrintable='#4A4060'; ContextRowBg='#1A1A2E'; CollapsedBg='#16162A'; CollapsedFg='#848CA8' }
    'TokyoNight'      = @{ EqualByte='#00000000'; ModifiedByte='#25240A'; ModifiedByteStrong='#363500'; InsertedByte='#0E2418'; DeletedByte='#24100E'; Padding='#16161E'; OffsetFg='#565F89'; OffsetBg='#13131D'; Separator='#1F2335'; AsciiFg='#C0CAF5'; AsciiNonPrintable='#3B4261'; ContextRowBg='#16161E'; CollapsedBg='#13131D'; CollapsedFg='#565F89' }
    'DockDark'        = $dark
    'DockLight'       = $light
}

function Get-BDiffTokenBlock {
    param([hashtable]$t)

    return @"

    <!-- BDiff tokens (BDiff_*) — inserted by Add-BDiffTokens.ps1 -->
    <SolidColorBrush x:Key="BDiff_EqualByteBrush"            Color="$($t.EqualByte)" />
    <SolidColorBrush x:Key="BDiff_ModifiedByteBrush"         Color="$($t.ModifiedByte)" />
    <SolidColorBrush x:Key="BDiff_ModifiedByteStrongBrush"   Color="$($t.ModifiedByteStrong)" />
    <SolidColorBrush x:Key="BDiff_InsertedByteBrush"         Color="$($t.InsertedByte)" />
    <SolidColorBrush x:Key="BDiff_DeletedByteBrush"          Color="$($t.DeletedByte)" />
    <SolidColorBrush x:Key="BDiff_PaddingBrush"              Color="$($t.Padding)" />
    <SolidColorBrush x:Key="BDiff_OffsetForegroundBrush"     Color="$($t.OffsetFg)" />
    <SolidColorBrush x:Key="BDiff_OffsetBackgroundBrush"     Color="$($t.OffsetBg)" />
    <SolidColorBrush x:Key="BDiff_SeparatorBrush"            Color="$($t.Separator)" />
    <SolidColorBrush x:Key="BDiff_AsciiForegroundBrush"      Color="$($t.AsciiFg)" />
    <SolidColorBrush x:Key="BDiff_AsciiNonPrintableBrush"    Color="$($t.AsciiNonPrintable)" />
    <SolidColorBrush x:Key="BDiff_ContextRowBackgroundBrush" Color="$($t.ContextRowBg)" />
    <SolidColorBrush x:Key="BDiff_CollapsedContextBrush"     Color="$($t.CollapsedBg)" />
    <SolidColorBrush x:Key="BDiff_CollapsedContextFgBrush"   Color="$($t.CollapsedFg)" />
"@
}

function Inject-Tokens {
    param([string]$filePath, [hashtable]$t)

    $content = Get-Content $filePath -Raw -Encoding UTF8

    if ($content -match 'BDiff_EqualByteBrush') {
        Write-Host "  SKIP (already present): $filePath"
        return
    }

    $block   = Get-BDiffTokenBlock -t $t
    $updated = $content -replace '</ResourceDictionary>', "$block`n</ResourceDictionary>"
    Set-Content $filePath $updated -Encoding UTF8 -NoNewline
    Write-Host "  OK: $filePath"
}

Write-Host "`n=== Injecting BDiff_* tokens into Shell themes ==="
$shellThemes = @{
    'VS2022Dark'='VS2022Dark'; 'VisualStudio'='VisualStudio'; 'CatppuccinMocha'='CatppuccinMocha'
    'CatppuccinLatte'='CatppuccinLatte'; 'Cyberpunk'='Cyberpunk'; 'DarkGlass'='DarkGlass'
    'Dracula'='Dracula'; 'Forest'='Forest'; 'GruvboxDark'='GruvboxDark'
    'HighContrast'='HighContrast'; 'Matrix'='Matrix'; 'Minimal'='Minimal'
    'Nord'='Nord'; 'Office'='Office'; 'Synthwave84'='Synthwave84'; 'TokyoNight'='TokyoNight'
}

foreach ($themeName in $shellThemes.Keys) {
    $path = Join-Path $shell "$themeName\Colors.xaml"
    if (Test-Path $path) {
        Inject-Tokens -filePath $path -t $themes[$themeName]
    } else {
        Write-Warning "NOT FOUND: $path"
    }
}

Write-Host "`n=== Injecting BDiff_* tokens into Docking.Wpf themes ==="
Inject-Tokens -filePath (Join-Path $dock 'Dark\Colors.xaml')  -t $themes['DockDark']
Inject-Tokens -filePath (Join-Path $dock 'Light\Colors.xaml') -t $themes['DockLight']

Write-Host "`nDone. 14 new BDiff_* tokens injected into all 18 Colors.xaml files."
