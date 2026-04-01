# Add-CA-Tokens-V2.ps1 — Inject 5 additional CA_* tokens for overkill UI upgrade
# Usage: pwsh -File Add-CA-Tokens-V2.ps1

$Root = Split-Path $PSScriptRoot -Parent

$files = @(
    Get-ChildItem "$Root\Docking\WpfHexEditor.Docking.Wpf\Themes\*\Colors.xaml"
    Get-ChildItem "$Root\Shell\WpfHexEditor.Shell\Themes\*\Colors.xaml"
)

$darkPalette = @{
    CA_CodeBlockBackground = '#1A1D24'
    CA_ThinkingForeground  = '#A0A8B8'
    CA_AvatarUser          = '#4A6FA5'
    CA_AvatarAssistant     = '#CC785C'
    CA_Error               = '#E05252'
}

$lightPalette = @{
    CA_CodeBlockBackground = '#F5F5F5'
    CA_ThinkingForeground  = '#6B7280'
    CA_AvatarUser          = '#3B5998'
    CA_AvatarAssistant     = '#CC785C'
    CA_Error               = '#D32F2F'
}

$lightThemes = @('Light', 'CatppuccinLatte', 'Office', 'Minimal')

foreach ($file in $files) {
    $folderName = Split-Path (Split-Path $file -Parent) -Leaf
    $palette = if ($lightThemes -contains $folderName) { $lightPalette } else { $darkPalette }

    $content = Get-Content $file -Raw

    # Skip if already injected
    if ($content -match 'CA_CodeBlockBackground') {
        Write-Host "SKIP $folderName (already has V2 CA_* tokens)"
        continue
    }

    # Build the token block — insert before existing CA_* brush section ends
    $nl = [Environment]::NewLine
    $colorBlock = ''
    $brushBlock = ''
    foreach ($key in ($palette.Keys | Sort-Object)) {
        $val = $palette[$key]
        $colorBlock += '    <Color x:Key="' + $key + 'Color">' + $val + '</Color>' + $nl
        $brushBlock += '    <SolidColorBrush x:Key="' + $key + 'Brush" Color="{StaticResource ' + $key + 'Color}"/>' + $nl
    }

    # Insert colors after last existing CA_*Color line, brushes after last CA_*Brush line
    # Strategy: insert before </ResourceDictionary> but after existing CA_* block
    $block = $nl + '    <!-- Claude AI Assistant tokens V2 (CA_* overkill upgrade) -->' + $nl + $colorBlock + $nl + $brushBlock
    $content = $content -replace '</ResourceDictionary>', ($block + '</ResourceDictionary>')
    Set-Content $file -Value $content -NoNewline

    Write-Host ('OK   ' + $folderName + ' -- 5 CA_* V2 tokens added')
}

Write-Host ('Done. ' + $files.Count + ' files processed.')
