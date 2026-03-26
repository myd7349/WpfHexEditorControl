# Add-WhfmtV2Tokens.ps1
# Injects WF_* theme tokens for whfmt v2.0 into all Colors.xaml files.
# Run from the Sources/ directory.

$insertMarker = '</ResourceDictionary>'

$tokenBlocks = @{

    # ── VS2022Dark (dark blue IDE) ──────────────────────────────────────────────
    'WpfHexEditor.Shell\Themes\VS2022Dark\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#252526" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#E51400" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#FF8C00" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#FFDD00" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#4EC9B0" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#2D2D30" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#3C3C3C" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#007ACC" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#388A34" />
"@

    'WpfHexEditor.Shell\Themes\Dracula\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#282A36" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#FF5555" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#FFB86C" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#F1FA8C" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#50FA7B" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#44475A" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#383A59" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#8BE9FD" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#50FA7B" />
"@

    'WpfHexEditor.Shell\Themes\Nord\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#2E3440" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#BF616A" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#D08770" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#EBCB8B" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#88C0D0" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#3B4252" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#434C5E" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#81A1C1" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#A3BE8C" />
"@

    'WpfHexEditor.Shell\Themes\TokyoNight\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#1A1B26" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#F7768E" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#FF9E64" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#E0AF68" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#73DACA" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#24283B" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#2F3549" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#7AA2F7" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#9ECE6A" />
"@

    'WpfHexEditor.Shell\Themes\GruvboxDark\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#282828" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#CC241D" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#D79921" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#FABD2F" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#8EC07C" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#3C3836" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#504945" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#458588" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#98971A" />
"@

    'WpfHexEditor.Shell\Themes\Matrix\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#001400" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#FF2222" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#FFAA00" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#DDFF00" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#00FF41" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#001E00" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#003300" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#00CC33" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#22BB22" />
"@

    'WpfHexEditor.Shell\Themes\Cyberpunk\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#0D0D0D" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#FF0055" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#FF9900" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#FFFF00" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#00FFFF" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#1A001A" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#1A1A33" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#FF00FF" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#00FF99" />
"@

    'WpfHexEditor.Shell\Themes\Synthwave84\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#1A1033" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#FE4450" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#FC9867" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#FFE261" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#72F1B8" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#2B1B52" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#3B2B62" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#FF7EDB" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#36F9F6" />
"@

    'WpfHexEditor.Shell\Themes\Forest\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#1A2515" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#CC4444" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#CC8844" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#CCBB44" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#88BB66" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#1E2D18" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#263320" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#66AA77" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#559944" />
"@

    'WpfHexEditor.Shell\Themes\DarkGlass\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#1C1C1C" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#E05252" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#E09050" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#E0D050" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#60C0B0" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#242424" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#2C2C2C" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#5090C0" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#508050" />
"@

    'WpfHexEditor.Shell\Themes\HighContrast\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#000000" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#FF0000" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#FFFF00" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#FFFFFF" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#00FFFF" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#111111" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#222222" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#00FF00" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#00FF00" />
"@

    'WpfHexEditor.Shell\Themes\Minimal\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#F5F5F5" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#CC2222" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#BB6600" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#AA9900" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#0066AA" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#EEEEEE" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#E8E8E8" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#0055BB" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#226622" />
"@

    'WpfHexEditor.Shell\Themes\CatppuccinLatte\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#EFF1F5" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#D20F39" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#E64553" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#DF8E1D" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#179299" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#E6E9EF" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#DCE0E8" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#1E66F5" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#40A02B" />
"@

    'WpfHexEditor.Shell\Themes\CatppuccinMocha\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#1E1E2E" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#F38BA8" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#FAB387" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#F9E2AF" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#94E2D5" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#313244" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#45475A" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#89B4FA" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#A6E3A1" />
"@

    'WpfHexEditor.Shell\Themes\VisualStudio\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#F0F0F0" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#C00000" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#FF7700" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#CCAA00" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#007070" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#E8E8E8" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#DDDDDD" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#1F5299" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#1E7C1E" />
"@

    'WpfHexEditor.Shell\Themes\Office\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#F3F3F3" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#C0392B" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#E67E22" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#F1C40F" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#16A085" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#ECECEC" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#E0E0E0" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#2980B9" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#27AE60" />
"@

    # Docking themes
    'WpfHexEditor.Docking.Wpf\Themes\Dark\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#252526" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#E51400" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#FF8C00" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#FFDD00" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#4EC9B0" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#2D2D30" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#3C3C3C" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#007ACC" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#388A34" />
"@

    'WpfHexEditor.Docking.Wpf\Themes\Light\Colors.xaml' = @"
    <!-- whfmt v2.0 tokens (WF_*) - inserted by Add-WhfmtV2Tokens.ps1 -->
    <SolidColorBrush x:Key="WF_RepeatingGroupBackground"  Color="#F0F0F0" />
    <SolidColorBrush x:Key="WF_AssertionErrorBrush"       Color="#C00000" />
    <SolidColorBrush x:Key="WF_AssertionWarningBrush"     Color="#FF7700" />
    <SolidColorBrush x:Key="WF_ForensicAlertBrush"        Color="#CCAA00" />
    <SolidColorBrush x:Key="WF_PointerArrowBrush"         Color="#007070" />
    <SolidColorBrush x:Key="WF_BitfieldSubrowBrush"       Color="#E8E8E8" />
    <SolidColorBrush x:Key="WF_InspectorGroupHeaderBrush" Color="#DDDDDD" />
    <SolidColorBrush x:Key="WF_NavigatorBookmarkBrush"    Color="#1F5299" />
    <SolidColorBrush x:Key="WF_ConfidenceBadgeBrush"      Color="#1E7C1E" />
"@
}

$baseDir = $PSScriptRoot
$updated = 0
$skipped = 0

foreach ($relPath in $tokenBlocks.Keys) {
    $fullPath = Join-Path $baseDir $relPath
    if (-not (Test-Path $fullPath)) {
        Write-Warning "Not found: $fullPath"
        $skipped++
        continue
    }

    $content = Get-Content $fullPath -Raw -Encoding UTF8

    # Skip if already patched
    if ($content -match 'WF_RepeatingGroupBackground') {
        Write-Host "Already patched: $relPath" -ForegroundColor DarkGray
        $skipped++
        continue
    }

    $inject = $tokenBlocks[$relPath]
    $newContent = $content -replace '</ResourceDictionary>', "$inject`n</ResourceDictionary>"
    Set-Content $fullPath $newContent -Encoding UTF8 -NoNewline
    Write-Host "Patched: $relPath" -ForegroundColor Green
    $updated++
}

Write-Host ""
Write-Host "Done. Updated: $updated  Skipped/already done: $skipped" -ForegroundColor Cyan
