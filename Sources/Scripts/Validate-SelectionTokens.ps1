# ==========================================================
# Project: WpfHexEditor.Shell
# File: Scripts/Validate-SelectionTokens.ps1
# Description:
#     Validates that all Colors.xaml files contain the 4 unified
#     Panel_Item* tokens and that aliased panel-specific tokens
#     exist alongside them. Also reports any XAML files still
#     referencing old tokens instead of Panel_* tokens.
# ==========================================================

param(
    [string]$Root = (Resolve-Path "$PSScriptRoot\..")
)

$ErrorCount = 0

# ── 1. Find all Colors.xaml files ────────────────────────────────────────────
$colorsFiles = Get-ChildItem -Path $Root -Recurse -Filter "Colors.xaml" |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' }

Write-Host "`n=== Unified Panel Token Validation ===" -ForegroundColor Cyan
Write-Host "Found $($colorsFiles.Count) Colors.xaml files`n"

$requiredTokens = @(
    'Panel_ItemSelectedBrush',
    'Panel_ItemInactiveSelectedBrush',
    'Panel_ItemHoverBrush',
    'Panel_ItemSelectedForegroundBrush'
)

foreach ($file in $colorsFiles) {
    $content = Get-Content $file.FullName -Raw
    $relPath = $file.FullName.Replace($Root, '').TrimStart('\')
    $missing = @()

    foreach ($token in $requiredTokens) {
        if ($content -notmatch "x:Key=`"$token`"") {
            $missing += $token
        }
    }

    if ($missing.Count -gt 0) {
        Write-Host "  FAIL: $relPath" -ForegroundColor Red
        foreach ($m in $missing) {
            Write-Host "        Missing: $m" -ForegroundColor Yellow
        }
        $ErrorCount += $missing.Count
    } else {
        Write-Host "  OK:   $relPath" -ForegroundColor Green
    }
}

# ── 2. Check for old tokens still used in XAML views ─────────────────────────
Write-Host "`n=== Old Token References in XAML Views ===" -ForegroundColor Cyan

$oldTokens = @(
    'SE_SelectedBrush', 'SE_HoverBrush',
    'PFP_ItemSelectedBrush', 'PFP_ItemHoverBrush', 'PFP_ItemSelectedHoverBrush',
    'PFP_ItemSelectedForegroundBrush',
    'PM_ListItemSelectedBrush', 'PM_ListItemHoverBrush',
    'UT_SelectionBrush', 'UT_HoverBrush',
    'ERR_SelectedRowBrush', 'ERR_HoverRowBrush',
    'PP_SelectedEntryBrush',
    'CD_OutlineSelectedBrush', 'CD_OutlineHoverBrush',
    'KSP_RowHoverBackground',
    'RES_SelectedRowBrush', 'RES_HoverRowBrush'
)

$xamlFiles = Get-ChildItem -Path $Root -Recurse -Include "*.xaml" |
    Where-Object {
        $_.FullName -notmatch '\\bin\\|\\obj\\' -and
        $_.Name -ne 'Colors.xaml' -and
        $_.FullName -notmatch '\\Themes\\'
    }

$oldRefs = @()
foreach ($file in $xamlFiles) {
    $content = Get-Content $file.FullName -Raw
    $relPath = $file.FullName.Replace($Root, '').TrimStart('\')
    foreach ($token in $oldTokens) {
        if ($content -match "DynamicResource\s+$token") {
            $oldRefs += [PSCustomObject]@{
                File  = $relPath
                Token = $token
            }
        }
    }
}

if ($oldRefs.Count -eq 0) {
    Write-Host "  No old token references found in XAML views." -ForegroundColor Green
} else {
    Write-Host "  Found $($oldRefs.Count) old token reference(s):" -ForegroundColor Yellow
    $oldRefs | Group-Object File | ForEach-Object {
        Write-Host "  $($_.Name):" -ForegroundColor Yellow
        $_.Group | ForEach-Object {
            Write-Host "    - $($_.Token)" -ForegroundColor DarkYellow
        }
    }
}

# ── 3. Summary ───────────────────────────────────────────────────────────────
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
if ($ErrorCount -eq 0 -and $oldRefs.Count -eq 0) {
    Write-Host "  All checks passed." -ForegroundColor Green
} else {
    if ($ErrorCount -gt 0) {
        Write-Host "  $ErrorCount missing unified token(s) in Colors.xaml files" -ForegroundColor Red
    }
    if ($oldRefs.Count -gt 0) {
        Write-Host "  $($oldRefs.Count) old token reference(s) in XAML views (cosmetic)" -ForegroundColor Yellow
    }
}
Write-Host ""
