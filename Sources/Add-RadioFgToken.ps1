# ==========================================================
# Add-RadioFgToken.ps1
# Inserts CL_RadioSelectedForegroundBrush after CL_RadioSelectedBrush
# in every Colors.xaml (16 Shell + 2 Docking.Wpf).
# Run from: Sources/
# ==========================================================
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root  = $PSScriptRoot
$shell = Join-Path $root 'WpfHexEditor.Shell\Themes'
$dock  = Join-Path $root 'WpfHexEditor.Docking.Wpf\Themes'

# Per-theme foreground color for selected radio pills
# Black (#000000) for light/bright accents, White (#FFFFFF) for dark accents
$themes = @{
    'VS2022Dark'      = '#000000'   # accent #007ACC — medium blue → black
    'VisualStudio'    = '#000000'   # accent #0078D4 — blue → black
    'CatppuccinMocha' = '#000000'   # accent #89B4FA — light blue → black
    'CatppuccinLatte' = '#FFFFFF'   # accent #1E66F5 — dark blue → white
    'Cyberpunk'       = '#FFFFFF'   # accent #FF003C — bright red → white
    'DarkGlass'       = '#000000'   # accent #5ABFA0 — teal → black
    'Dracula'         = '#000000'   # accent #BD93F9 — purple → black
    'Forest'          = '#000000'   # accent #6AAA5A — green → black
    'GruvboxDark'     = '#000000'   # accent #FABD2F — yellow → black
    'HighContrast'    = '#000000'   # accent #1AEBFF — cyan → black
    'Matrix'          = '#000000'   # accent #00FF41 — green → black
    'Minimal'         = '#FFFFFF'   # accent #333333 — dark gray → white
    'Nord'            = '#000000'   # accent #88C0D0 — light blue → black
    'Office'          = '#FFFFFF'   # accent #0078D4 — blue → white
    'Synthwave84'     = '#000000'   # accent #FF7EDB — pink → black
    'TokyoNight'      = '#000000'   # accent #7AA2F7 — blue → black
    'DockDark'        = '#000000'
    'DockLight'       = '#000000'
}

function Inject-Token {
    param([string]$filePath, [string]$fgColor)

    $content = Get-Content $filePath -Raw -Encoding UTF8

    if ($content -match 'CL_RadioSelectedForegroundBrush') {
        Write-Host "  SKIP (already present): $filePath"
        return
    }

    $anchor = 'CL_RadioSelectedBrush"'
    $idx = $content.IndexOf($anchor)
    if ($idx -lt 0) {
        Write-Host "  SKIP (no anchor found): $filePath"
        return
    }

    # Find end of the line containing CL_RadioSelectedBrush
    $eol = $content.IndexOf("`n", $idx)
    if ($eol -lt 0) { $eol = $content.Length }

    $insertLine = "`n    <SolidColorBrush x:Key=`"CL_RadioSelectedForegroundBrush`" Color=`"$fgColor`" />"
    $newContent = $content.Insert($eol, $insertLine)

    Set-Content $filePath $newContent -Encoding UTF8 -NoNewline
    Write-Host "  OK: $filePath"
}

# Shell themes
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

Write-Host "`nInjecting CL_RadioSelectedForegroundBrush..."
foreach ($name in $shellMap.Keys) {
    Inject-Token -filePath $shellMap[$name] -fgColor $themes[$name]
}

# Docking themes
Inject-Token -filePath (Join-Path $dock 'Dark\Colors.xaml')  -fgColor $themes['DockDark']
Inject-Token -filePath (Join-Path $dock 'Light\Colors.xaml') -fgColor $themes['DockLight']

Write-Host "`nDone."
