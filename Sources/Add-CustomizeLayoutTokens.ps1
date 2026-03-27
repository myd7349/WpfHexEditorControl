# ==========================================================
# Add-CustomizeLayoutTokens.ps1
# Inserts 8 CL_* SolidColorBrush tokens into every Colors.xaml
# theme file (16 Shell themes + 2 Docking.Wpf themes).
# Run from: Sources/
# ==========================================================
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root  = $PSScriptRoot
$shell = Join-Path $root 'WpfHexEditor.Shell\Themes'
$dock  = Join-Path $root 'WpfHexEditor.Docking.Wpf\Themes'

# ---------------------------------------------------------------------------
# Per-theme color map
# Keys: Bg, SectionHdr, ToggleOn, ToggleOff, RadioSel, RadioUnsel, Hover, ModeBadge
# ---------------------------------------------------------------------------
$themes = @{
    'VS2022Dark'      = @{ Bg='#252526'; SectionHdr='#6A6A6A'; ToggleOn='#007ACC'; ToggleOff='#4D4D4D'; RadioSel='#007ACC'; RadioUnsel='#333333'; Hover='#1A2D3E'; ModeBadge='#388A34' }
    'VisualStudio'    = @{ Bg='#F3F3F3'; SectionHdr='#717171'; ToggleOn='#0078D4'; ToggleOff='#A0A0A0'; RadioSel='#0078D4'; RadioUnsel='#E0E0E0'; Hover='#E8E8E8'; ModeBadge='#107C10' }
    'CatppuccinMocha' = @{ Bg='#1E1E2E'; SectionHdr='#6C7086'; ToggleOn='#89B4FA'; ToggleOff='#585B70'; RadioSel='#89B4FA'; RadioUnsel='#313244'; Hover='#313244'; ModeBadge='#A6E3A1' }
    'CatppuccinLatte' = @{ Bg='#EFF1F5'; SectionHdr='#8C8FA1'; ToggleOn='#1E66F5'; ToggleOff='#ACB0BE'; RadioSel='#1E66F5'; RadioUnsel='#CCD0DA'; Hover='#DCE0E8'; ModeBadge='#40A02B' }
    'Cyberpunk'       = @{ Bg='#0D0D1A'; SectionHdr='#555577'; ToggleOn='#FF003C'; ToggleOff='#333355'; RadioSel='#FF003C'; RadioUnsel='#1A1A33'; Hover='#1A1A40'; ModeBadge='#00FF9F' }
    'DarkGlass'       = @{ Bg='#1E1E2E'; SectionHdr='#667788'; ToggleOn='#5ABFA0'; ToggleOff='#445566'; RadioSel='#5ABFA0'; RadioUnsel='#28283A'; Hover='#2A3040'; ModeBadge='#5ABFA0' }
    'Dracula'         = @{ Bg='#282A36'; SectionHdr='#6272A4'; ToggleOn='#BD93F9'; ToggleOff='#44475A'; RadioSel='#BD93F9'; RadioUnsel='#383A4A'; Hover='#3A3D50'; ModeBadge='#50FA7B' }
    'Forest'          = @{ Bg='#1A2818'; SectionHdr='#607060'; ToggleOn='#6AAA5A'; ToggleOff='#405040'; RadioSel='#6AAA5A'; RadioUnsel='#243020'; Hover='#2A3828'; ModeBadge='#6AAA5A' }
    'GruvboxDark'     = @{ Bg='#282828'; SectionHdr='#665C54'; ToggleOn='#FABD2F'; ToggleOff='#504945'; RadioSel='#FABD2F'; RadioUnsel='#3C3836'; Hover='#3C3836'; ModeBadge='#8EC07C' }
    'HighContrast'    = @{ Bg='#000000'; SectionHdr='#888888'; ToggleOn='#1AEBFF'; ToggleOff='#666666'; RadioSel='#1AEBFF'; RadioUnsel='#222222'; Hover='#1A1A1A'; ModeBadge='#00FF00' }
    'Matrix'          = @{ Bg='#000A00'; SectionHdr='#336633'; ToggleOn='#00FF41'; ToggleOff='#1A3A1A'; RadioSel='#00FF41'; RadioUnsel='#001400'; Hover='#001E00'; ModeBadge='#00FF41' }
    'Minimal'         = @{ Bg='#F5F5F5'; SectionHdr='#999999'; ToggleOn='#333333'; ToggleOff='#CCCCCC'; RadioSel='#333333'; RadioUnsel='#EBEBEB'; Hover='#E8E8E8'; ModeBadge='#4CAF50' }
    'Nord'            = @{ Bg='#2E3440'; SectionHdr='#4C566A'; ToggleOn='#88C0D0'; ToggleOff='#434C5E'; RadioSel='#88C0D0'; RadioUnsel='#3B4252'; Hover='#3B4252'; ModeBadge='#A3BE8C' }
    'Office'          = @{ Bg='#F0F0F0'; SectionHdr='#999999'; ToggleOn='#0078D4'; ToggleOff='#B0B0B0'; RadioSel='#0078D4'; RadioUnsel='#E0E0E0'; Hover='#E5E5E5'; ModeBadge='#107C41' }
    'Synthwave84'     = @{ Bg='#262335'; SectionHdr='#6644AA'; ToggleOn='#FF7EDB'; ToggleOff='#463465'; RadioSel='#FF7EDB'; RadioUnsel='#34294F'; Hover='#342850'; ModeBadge='#72F1B8' }
    'TokyoNight'      = @{ Bg='#1A1B26'; SectionHdr='#414868'; ToggleOn='#7AA2F7'; ToggleOff='#3B4261'; RadioSel='#7AA2F7'; RadioUnsel='#24283B'; Hover='#292E42'; ModeBadge='#9ECE6A' }
    'DockDark'        = @{ Bg='#252526'; SectionHdr='#6A6A6A'; ToggleOn='#007ACC'; ToggleOff='#4D4D4D'; RadioSel='#007ACC'; RadioUnsel='#333333'; Hover='#1A2D3E'; ModeBadge='#388A34' }
    'DockLight'       = @{ Bg='#F3F3F3'; SectionHdr='#717171'; ToggleOn='#0078D4'; ToggleOff='#A0A0A0'; RadioSel='#0078D4'; RadioUnsel='#E0E0E0'; Hover='#E8E8E8'; ModeBadge='#107C10' }
}

function Get-ClTokenBlock {
    param([hashtable]$t)
    return @"

    <!-- Customize Layout tokens (CL_*) — inserted by Add-CustomizeLayoutTokens.ps1 -->
    <SolidColorBrush x:Key="CL_BackgroundBrush"      Color="$($t.Bg)" />
    <SolidColorBrush x:Key="CL_SectionHeaderBrush"    Color="$($t.SectionHdr)" />
    <SolidColorBrush x:Key="CL_ToggleActiveBrush"     Color="$($t.ToggleOn)" />
    <SolidColorBrush x:Key="CL_ToggleInactiveBrush"   Color="$($t.ToggleOff)" />
    <SolidColorBrush x:Key="CL_RadioSelectedBrush"    Color="$($t.RadioSel)" />
    <SolidColorBrush x:Key="CL_RadioUnselectedBrush"  Color="$($t.RadioUnsel)" />
    <SolidColorBrush x:Key="CL_HoverBrush"            Color="$($t.Hover)" />
    <SolidColorBrush x:Key="CL_ModeBadgeBrush"        Color="$($t.ModeBadge)" />
"@
}

function Inject-Tokens {
    param([string]$filePath, [hashtable]$themeColors)

    $content = Get-Content $filePath -Raw -Encoding UTF8

    if ($content -match 'CL_BackgroundBrush') {
        Write-Host "  SKIP (already present): $filePath"
        return
    }

    $block      = Get-ClTokenBlock -t $themeColors
    $marker     = '</ResourceDictionary>'
    $newContent = $content.TrimEnd() -replace [regex]::Escape($marker), "$block`n$marker"

    Set-Content $filePath $newContent -Encoding UTF8 -NoNewline
    Write-Host "  OK: $filePath"
}

# ---------------------------------------------------------------------------
# Shell themes
# ---------------------------------------------------------------------------
$shellMap = @{
    'VS2022Dark'      = (Join-Path $shell 'VS2022Dark\Colors.xaml')
    'VisualStudio'    = (Join-Path $shell 'VisualStudio\Colors.xaml')
    'CatppuccinMocha' = (Join-Path $shell 'CatppuccinMocha\Colors.xaml')
    'CatppuccinLatte' = (Join-Path $shell 'CatppuccinLatte\Colors.xaml')
    'Cyberpunk'       = (Join-Path $shell 'Cyberpunk\Colors.xaml')
    'DarkGlass'       = (Join-Path $shell 'DarkGlass\Colors.xaml')
    'Dracula'         = (Join-Path $shell 'Dracula\Colors.xaml')
    'Forest'          = (Join-Path $shell 'Forest\Colors.xaml')
    'GruvboxDark'     = (Join-Path $shell 'GruvboxDark\Colors.xaml')
    'HighContrast'    = (Join-Path $shell 'HighContrast\Colors.xaml')
    'Matrix'          = (Join-Path $shell 'Matrix\Colors.xaml')
    'Minimal'         = (Join-Path $shell 'Minimal\Colors.xaml')
    'Nord'            = (Join-Path $shell 'Nord\Colors.xaml')
    'Office'          = (Join-Path $shell 'Office\Colors.xaml')
    'Synthwave84'     = (Join-Path $shell 'Synthwave84\Colors.xaml')
    'TokyoNight'      = (Join-Path $shell 'TokyoNight\Colors.xaml')
}

Write-Host "`nInjecting CL_* tokens into Shell themes..."
foreach ($name in $shellMap.Keys) {
    Inject-Tokens -filePath $shellMap[$name] -themeColors $themes[$name]
}

# ---------------------------------------------------------------------------
# Docking.Wpf themes
# ---------------------------------------------------------------------------
Write-Host "`nInjecting CL_* tokens into Docking.Wpf themes..."
Inject-Tokens -filePath (Join-Path $dock 'Dark\Colors.xaml')  -themeColors $themes['DockDark']
Inject-Tokens -filePath (Join-Path $dock 'Light\Colors.xaml') -themeColors $themes['DockLight']

Write-Host "`nDone - 18 files processed."
