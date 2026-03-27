param()

$files = @(
    @{ Path = "$PSScriptRoot\WpfHexEditor.Docking.Wpf\Themes\Dark\Colors.xaml"; Color = '#50FFCC00' },
    @{ Path = "$PSScriptRoot\WpfHexEditor.Docking.Wpf\Themes\Light\Colors.xaml"; Color = '#40CC8800' }
)

foreach ($entry in $files) {
    $f = $entry.Path
    $color = $entry.Color
    if (!(Test-Path $f)) { Write-Host "SKIP $f (not found)"; continue }

    $content = [System.IO.File]::ReadAllText($f)
    if ($content.Contains('TDiff_HoverSegmentBrush')) {
        Write-Host "SKIP $f (already has token)"
        continue
    }

    $anchor = 'BDiff_HoverBrush'
    $pos = $content.IndexOf($anchor)
    if ($pos -lt 0) { Write-Host "WARN $f (no anchor)"; continue }

    $eol = $content.IndexOf("`n", $pos)
    if ($eol -lt 0) { $eol = $content.Length }

    $insertLine = "`r`n" + '    <SolidColorBrush x:Key="TDiff_HoverSegmentBrush"        Color="' + $color + '" />'
    $result = $content.Substring(0, $eol) + $insertLine + $content.Substring($eol)
    [System.IO.File]::WriteAllText($f, $result)
    Write-Host "OK $f"
}

Write-Host "Done."
