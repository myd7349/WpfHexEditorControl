# Add-CA-Tokens.ps1 — Inject 17 CA_* Color + Brush tokens into all 18 Colors.xaml theme files
# Usage: pwsh -File Add-CA-Tokens.ps1

$Root = Split-Path $PSScriptRoot -Parent

# All Colors.xaml files (2 Docking + 16 Shell)
$files = @(
    Get-ChildItem "$Root\Docking\WpfHexEditor.Docking.Wpf\Themes\*\Colors.xaml"
    Get-ChildItem "$Root\Shell\WpfHexEditor.Shell\Themes\*\Colors.xaml"
)

# Theme-specific palettes: [themeFolderName] → color values
# Dark-family defaults
$darkPalette = @{
    CA_PanelBackground         = '#1E1E2E'
    CA_MessageUserBackground   = '#2D2D3F'
    CA_MessageAssistantBackground = '#252535'
    CA_MessageBorder           = '#3F3F5F'
    CA_MessageForeground       = '#ECECF1'
    CA_InputBackground         = '#2D2D30'
    CA_InputForeground         = '#D4D4D4'
    CA_InputBorder             = '#5F5F7F'
    CA_ToolCallBackground      = '#1A1A2E'
    CA_ToolCallBorder          = '#3F3F5F'
    CA_ToolCallForeground      = '#A0A0C0'
    CA_AccentBranding          = '#CC785C'
    CA_TitleBarButtonBackground = '#2D2D30'
    CA_TitleBarButtonHover     = '#3E3E42'
    CA_TitleBarBadgeIdle       = '#4EC94E'
    CA_TitleBarBadgeStreaming   = '#E8A832'
    CA_TitleBarBadgeError      = '#E05252'
}

$lightPalette = @{
    CA_PanelBackground         = '#F5F5F5'
    CA_MessageUserBackground   = '#E8EBF0'
    CA_MessageAssistantBackground = '#FFFFFF'
    CA_MessageBorder           = '#D0D0D8'
    CA_MessageForeground       = '#1E1E1E'
    CA_InputBackground         = '#FFFFFF'
    CA_InputForeground         = '#1E1E1E'
    CA_InputBorder             = '#C0C0C8'
    CA_ToolCallBackground      = '#F0F0F5'
    CA_ToolCallBorder          = '#D0D0D8'
    CA_ToolCallForeground      = '#5A5A80'
    CA_AccentBranding          = '#CC785C'
    CA_TitleBarButtonBackground = '#EAEAEA'
    CA_TitleBarButtonHover     = '#D8D8DC'
    CA_TitleBarBadgeIdle       = '#2EA82E'
    CA_TitleBarBadgeStreaming   = '#D49520'
    CA_TitleBarBadgeError      = '#D03030'
}

# Map folder names to dark or light palette
$lightThemes = @('Light', 'CatppuccinLatte', 'Office', 'Minimal')

foreach ($file in $files) {
    $folderName = Split-Path (Split-Path $file -Parent) -Leaf
    $palette = if ($lightThemes -contains $folderName) { $lightPalette } else { $darkPalette }

    $content = Get-Content $file -Raw

    # Skip if already injected
    if ($content -match 'CA_PanelBackground') {
        Write-Host "SKIP $folderName (already has CA_* tokens)"
        continue
    }

    # Build the token block
    $nl = [Environment]::NewLine
    $block = $nl + '    <!-- Claude AI Assistant tokens (CA_*) -->' + $nl
    foreach ($key in ($palette.Keys | Sort-Object)) {
        $val = $palette[$key]
        $block += '    <Color x:Key="' + $key + 'Color">' + $val + '</Color>' + $nl
    }
    $block += $nl
    foreach ($key in ($palette.Keys | Sort-Object)) {
        $block += '    <SolidColorBrush x:Key="' + $key + 'Brush" Color="{StaticResource ' + $key + 'Color}"/>' + $nl
    }

    # Insert before closing </ResourceDictionary>
    $content = $content -replace '</ResourceDictionary>', ($block + '</ResourceDictionary>')
    Set-Content $file -Value $content -NoNewline

    Write-Host ('OK   ' + $folderName + ' -- 17 CA_* Color + 17 CA_* Brush tokens added')
}

Write-Host ('Done. ' + $files.Count + ' files processed.')
