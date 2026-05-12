<#
.SYNOPSIS
  whfmt-guard — validates *.whfmt format-definition files.

.DESCRIPTION
  Rules:
    R1  whfmt-jsonc-parse      (ERR)  JSONC parse, /* */ header tolerated
    R2  whfmt-version-monotone (ERR)  version >= HEAD version
    R3  whfmt-schema-required  (ERR)  formatName/formatId/extensions/category/description
    R4  whfmt-id-uniqueness    (ERR)  formatId unique across catalog
    R5  whfmt-magic-collision  (WARN) sig+offset+ext overlap with another file
    R6  whfmt-strength-enum    (WARN) detection.strength in allowed set
    R7  whfmt-placeholder-drift(WARN) {{var}} not in variables{}
    R10 whfmt-expression-refs  (WARN) expression references undeclared identifier
                                       (lightweight identifier-scan; full AST
                                       validation lives in C# whfmt.Validate)

  Exit code = ERR count (0 if WARN-only).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string[]]$Files,

    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'
# Strict mode intentionally OFF: JSON shapes vary across 440+ catalog files and
# we read fields defensively. Per-rule try/catch protects the aggregate run.

# ----- locate repo root + catalog root -----
if (-not $RepoRoot) {
    $RepoRoot = (git rev-parse --show-toplevel 2>$null)
    if (-not $RepoRoot) { $RepoRoot = (Resolve-Path "$PSScriptRoot/../../../../..").Path }
}
$RepoRoot    = (Resolve-Path $RepoRoot).Path
$CatalogRoot = Join-Path $RepoRoot 'Sources/Core/WpfHexEditor.Core.Definitions/FormatDefinitions'
if (-not (Test-Path $CatalogRoot)) {
    Write-Error "whfmt-guard: catalog root not found at $CatalogRoot"
    exit 2
}

$AllowedStrengths = @('None','Weak','Medium','Strong','VeryStrong')
$RequiredFields   = @('formatName','formatId','extensions','category','description')

# R10 — built-in functions + reserved keywords known to the whfmt expression engine.
# Identifiers in expressions that are NOT in this set AND not declared in
# variables{} / functions{} are flagged.
$ExprBuiltins = @(
    # built-in functions registered in WhfmtBuiltinFunctions
    'min','max','abs','length','hex','toUpper','toLower',
    # string instance methods (called as receiver.method())
    'startsWith','endsWith','includes','contains','trim',
    # literal keywords
    'true','false','null',
    # member accessor (e.g. someString.length) — already covered by 'length' above
    'length'
)

$findings = New-Object System.Collections.Generic.List[object]
function Add-Finding($sev, $rule, $file, $detail) {
    $findings.Add([pscustomobject]@{ Sev=$sev; Rule=$rule; File=$file; Detail=$detail })
}

# ----- filter input -----
$targets = @()
foreach ($f in $Files) {
    if (-not $f) { continue }
    $full = if ([System.IO.Path]::IsPathRooted($f)) { $f } else { Join-Path $RepoRoot $f }
    if (-not (Test-Path $full -PathType Leaf)) { continue }
    if ($full -notmatch '\.whfmt$') { continue }
    if ($full -match '[\\/](Tests|Samples)[\\/]') { continue }
    $fullResolved = (Resolve-Path $full).Path
    $catalogPrefix = $CatalogRoot.TrimEnd('\','/')
    if (-not ($fullResolved.StartsWith($catalogPrefix, [System.StringComparison]::OrdinalIgnoreCase))) {
        Write-Verbose "skip: $fullResolved not under $catalogPrefix"
        continue
    }
    $targets += $fullResolved
}
Write-Host "[whfmt-guard] scanned: $($targets.Count) file(s)"
if ($targets.Count -eq 0) { exit 0 }

# ----- helpers -----
function Test-Prop($obj, [string]$name) {
    if ($null -eq $obj) { return $false }
    if ($obj -is [System.Collections.IDictionary]) { return $obj.Contains($name) }
    if ($obj -is [psobject]) { return @($obj.PSObject.Properties.Name) -contains $name }
    return $false
}
function Get-PropNames($obj) {
    if ($null -eq $obj) { return @() }
    if ($obj -is [System.Collections.IDictionary]) { return @($obj.Keys) }
    if ($obj -is [psobject]) { return @($obj.PSObject.Properties.Name) }
    return @()
}

function Read-Whfmt([string]$path) {
    $raw = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    # strip UTF-8 BOM
    if ($raw.Length -gt 0 -and [int][char]$raw[0] -eq 0xFEFF) { $raw = $raw.Substring(1) }
    # strip leading /* ... */ header (only the first one, only if it precedes '{')
    $trim = $raw.TrimStart()
    if ($trim.StartsWith('/*')) {
        $end = $trim.IndexOf('*/')
        if ($end -ge 0) { $raw = $trim.Substring($end + 2) }
    }
    return $raw
}

function ConvertFrom-WhfmtJson([string]$text) {
    try {
        $clean = [regex]::Replace($text, '(?m)^\s*//.*$', '')
        # Two-pass: prefer pscustomobject (lets us use .Property dot access);
        # fall back to hashtable when JSON has same-name keys with different casing
        # (Newtonsoft, used at runtime by ImportFromJson, tolerates this; ConvertFrom-Json does not).
        try {
            $obj = ConvertFrom-Json -InputObject $clean -Depth 64
        } catch {
            if ($_.Exception.Message -match 'different casing') {
                $obj = ConvertFrom-Json -InputObject $clean -Depth 64 -AsHashtable
            } else { throw }
        }
        return [pscustomobject]@{ Ok=$true; Value=$obj; Error=$null }
    } catch {
        return [pscustomobject]@{ Ok=$false; Value=$null; Error=$_.Exception.Message }
    }
}

function Get-HeadContent([string]$relPath) {
    $head = git -C $RepoRoot show "HEAD:$relPath" 2>$null
    if ($LASTEXITCODE -ne 0) { return $null }
    return ($head -join "`n")
}

function Compare-Semver([string]$a, [string]$b) {
    # returns -1/0/1 ; treats missing/blank as 0.0
    function Parse($v) {
        $parts = @(($v -split '[.\-+]') | Where-Object { $_ -match '^\d+$' } | ForEach-Object { [int]$_ })
        while ($parts.Count -lt 3) { $parts += 0 }
        return $parts
    }
    $pa = Parse $a; $pb = Parse $b
    for ($i=0; $i -lt [Math]::Max($pa.Count,$pb.Count); $i++) {
        $va = if ($i -lt $pa.Count) { $pa[$i] } else { 0 }
        $vb = if ($i -lt $pb.Count) { $pb[$i] } else { 0 }
        if ($va -lt $vb) { return -1 }
        if ($va -gt $vb) { return  1 }
    }
    return 0
}

# ----- build catalog index once (for R4/R5) -----
$catalog = @{}  # path -> @{ id; sig; offset; exts; note }
$allWhfmt = Get-ChildItem -LiteralPath $CatalogRoot -Recurse -Filter '*.whfmt' -ErrorAction SilentlyContinue
foreach ($e in $allWhfmt) {
    try {
        $raw = Read-Whfmt $e.FullName
        $r = ConvertFrom-WhfmtJson $raw
        if (-not $r.Ok) { continue }
        $p = $r.Value
        $sig = $null; $off = 0; $note = $null
        if ((Test-Prop $p 'detection') -and $p.detection) {
            if (Test-Prop $p.detection 'signature') { $sig = [string]$p.detection.signature }
            if (Test-Prop $p.detection 'offset')    { try { $off = [int]$p.detection.offset } catch { $off = 0 } }
            if ((Test-Prop $p.detection 'validation') -and $p.detection.validation -and (Test-Prop $p.detection.validation 'note')) {
                $note = [string]$p.detection.validation.note
            }
        }
        $exts = @()
        if ((Test-Prop $p 'extensions') -and $p.extensions) {
            $exts = @($p.extensions | ForEach-Object { ([string]$_).ToLowerInvariant() })
        }
        $id = $null
        if (Test-Prop $p 'formatId') { $id = [string]$p.formatId }
        $catalog[$e.FullName] = @{ Id=$id; Sig=$sig; Offset=$off; Exts=$exts; Note=$note }
    } catch { }
}

# ----- per-target rules -----
foreach ($path in $targets) {
    $rel = [System.IO.Path]::GetRelativePath($RepoRoot, $path).Replace('\','/')
    try {
    $raw = Read-Whfmt $path
    $pr = ConvertFrom-WhfmtJson $raw

    # R1
    if (-not $pr.Ok) {
        Add-Finding 'ERR' 'whfmt-jsonc-parse' $rel $pr.Error
        continue   # later rules need a parsed object
    }
    $parsed = $pr.Value

    # R3 — required fields
    foreach ($k in $RequiredFields) {
        $present = Test-Prop $parsed $k
        if (-not $present) {
            Add-Finding 'ERR' 'whfmt-schema-required' $rel "missing field '$k'"
        } elseif ($k -eq 'extensions') {
            $val = $parsed.extensions
            if (-not $val -or @($val).Count -eq 0) {
                Add-Finding 'ERR' 'whfmt-schema-required' $rel "extensions must be a non-empty array"
            }
        } elseif (-not $parsed.$k) {
            Add-Finding 'ERR' 'whfmt-schema-required' $rel "field '$k' is empty"
        }
    }

    # R2 — version monotone
    if ((Test-Prop $parsed 'version') -and $parsed.version) {
        $headRaw = Get-HeadContent $rel
        if ($headRaw) {
            $stripped = $headRaw.TrimStart()
            if ($stripped.StartsWith('/*')) {
                $e = $stripped.IndexOf('*/'); if ($e -ge 0) { $headRaw = $stripped.Substring($e+2) }
            }
            $headPr = ConvertFrom-WhfmtJson $headRaw
            if ($headPr.Ok -and $headPr.Value.PSObject.Properties.Name -contains 'version' -and $headPr.Value.version) {
                $cmp = Compare-Semver ([string]$parsed.version) ([string]$headPr.Value.version)
                if ($cmp -lt 0) {
                    Add-Finding 'ERR' 'whfmt-version-monotone' $rel ("version {0} < HEAD {1}" -f $parsed.version, $headPr.Value.version)
                }
            }
        }
    }

    # R4 — id uniqueness
    if ((Test-Prop $parsed 'formatId') -and $parsed.formatId) {
        $thisId = [string]$parsed.formatId
        foreach ($kv in $catalog.GetEnumerator()) {
            if ($kv.Key -ieq $path) { continue }
            if ($kv.Value.Id -and ($kv.Value.Id -ieq $thisId)) {
                $otherRel = [System.IO.Path]::GetRelativePath($RepoRoot, $kv.Key).Replace('\','/')
                Add-Finding 'ERR' 'whfmt-id-uniqueness' $rel "formatId '$thisId' also used by $otherRel"
            }
        }
    }

    # R5 — magic collision (WARN, suppressed by detection.validation.note)
    $thisInfo = $catalog[$path]
    if ($thisInfo -and $thisInfo.Sig -and -not $thisInfo.Note) {
        foreach ($kv in $catalog.GetEnumerator()) {
            if ($kv.Key -ieq $path) { continue }
            $o = $kv.Value
            if (-not $o.Sig) { continue }
            if ($o.Sig -ne $thisInfo.Sig) { continue }
            if ($o.Offset -ne $thisInfo.Offset) { continue }
            $overlap = @($o.Exts | Where-Object { $thisInfo.Exts -contains $_ })
            if ($overlap.Count -gt 0) {
                $otherRel = [System.IO.Path]::GetRelativePath($RepoRoot, $kv.Key).Replace('\','/')
                Add-Finding 'WARN' 'whfmt-magic-collision' $rel ("sig {0} @ off {1} shared with {2} (ext {3})" -f $thisInfo.Sig, $thisInfo.Offset, $otherRel, ($overlap -join ','))
            }
        }
    }

    # R6 — strength enum
    if ((Test-Prop $parsed 'detection') -and (Test-Prop $parsed.detection 'strength') -and $parsed.detection.strength) {
        $s = [string]$parsed.detection.strength
        if ($AllowedStrengths -notcontains $s) {
            Add-Finding 'WARN' 'whfmt-strength-enum' $rel "detection.strength '$s' not in {$($AllowedStrengths -join ', ')}"
        }
    }

    # R7 — placeholder drift
    # Variables can come in two schemas:
    #   - dict   : { "magic": "" }                 → names are property names
    #   - typed[]: [ { "name": "magic", ... } ]    → names are .name of each item
    $declared = @()
    if ((Test-Prop $parsed 'variables') -and $parsed.variables) {
        $vars = $parsed.variables
        if ($vars -is [System.Collections.IEnumerable] -and -not ($vars -is [string]) -and -not ($vars -is [System.Collections.IDictionary]) -and -not ($vars -is [psobject] -and $vars.PSObject.Properties.Count -gt 0 -and -not ($vars -is [System.Array]))) {
            # Likely typed-array: iterate elements and pull .name
            foreach ($v in @($vars)) {
                if ((Test-Prop $v 'name') -and $v.name) { $declared += [string]$v.name }
            }
        }
        if ($declared.Count -eq 0) {
            # Fall back to dict-schema: property names
            $declared = Get-PropNames $vars
        }
    }
    $texts = @()
    if ((Test-Prop $parsed 'description') -and $parsed.description) {
        $texts += [string]$parsed.description
    }
    if ((Test-Prop $parsed 'blocks') -and $parsed.blocks) {
        foreach ($b in @($parsed.blocks)) {
            if ((Test-Prop $b 'description') -and $b.description) {
                $texts += [string]$b.description
            }
        }
    }
    $referenced = New-Object System.Collections.Generic.HashSet[string]
    foreach ($t in $texts) {
        foreach ($m in [regex]::Matches($t, '\{\{\s*([A-Za-z_][A-Za-z0-9_]*)\s*\}\}')) {
            [void]$referenced.Add($m.Groups[1].Value)
        }
    }
    foreach ($v in $referenced) {
        if ($declared -notcontains $v) {
            Add-Finding 'WARN' 'whfmt-placeholder-drift' $rel "{{$v}} referenced but not declared in variables{}"
        }
    }

    # R10 — expression identifier references (lightweight scan).
    # Walks every expression-bearing string in the .whfmt and flags identifiers
    # that are NOT in the declared variables{}, functions{} keys, or the built-in
    # set $ExprBuiltins. Full AST validation lives in C# WhfmtExpressionValidator
    # (whfmt.Validate / Models/Validation) — this PowerShell pass is the fast
    # pre-commit gate. Severity = WARN to avoid breaking the build on legitimate
    # member accesses we can't statically resolve (e.g. dynamic byte[] indexing).
    $declaredFns = @()
    if ((Test-Prop $parsed 'functions') -and $parsed.functions) {
        $declaredFns = Get-PropNames $parsed.functions
    }
    $known = @{}
    foreach ($n in $declared)     { $known[$n] = $true }
    foreach ($n in $declaredFns)  { $known[$n] = $true }
    foreach ($n in $ExprBuiltins) { $known[$n] = $true }

    # Heuristic: a string is "prose" (not an expression) when it contains words
    # separated only by whitespace, with no operator characters at all. We skip
    # prose to avoid false positives — many forensic suspiciousPatterns[].condition
    # fields in the catalog are English descriptions, not executable conditions.
    function Test-LooksLikeExpression([string]$s) {
        if (-not $s) { return $false }
        # Must contain at least one operator typical of expressions (== != < > && || + - * etc.).
        # Plain : alone is too weak (HTTP headers in prose use it).
        $hasOp = ($s -match '==|!=|<=|>=|<<|>>|&&|\|\||[<>+\-*/%^~?()\[\]]|(?<![A-Za-z])(?:&|\|)(?![A-Za-z])')
        if (-not $hasOp) { return $false }
        # Reject prose: 4+ ASCII words separated by spaces with no operator between them.
        # Heuristic: ratio of letters to operators. If letters >> ops by 20× and word count high, treat as prose.
        $letters = ([regex]::Matches($s, '[A-Za-z]')).Count
        $opChars = ([regex]::Matches($s, '[=<>!&|+\-*/%^~?()\[\]]')).Count
        if ($opChars -eq 0) { return $false }
        if ($letters -gt 80 -and ($letters / [Math]::Max($opChars,1)) -gt 25) { return $false }
        return $true
    }

    function Get-ExprStrings($p) {
        $out = @()
        if ((Test-Prop $p 'assertions') -and $p.assertions) {
            foreach ($a in @($p.assertions)) {
                if ((Test-Prop $a 'expression') -and $a.expression) {
                    $out += [pscustomobject]@{ Src=[string]$a.expression; Path='assertions[].expression' }
                }
            }
        }
        if ((Test-Prop $p 'blocks') -and $p.blocks) {
            foreach ($b in @($p.blocks)) {
                if ((Test-Prop $b 'expression') -and $b.expression) {
                    $out += [pscustomobject]@{ Src=[string]$b.expression; Path='blocks[].expression' }
                }
                if ((Test-Prop $b 'condition') -and $b.condition) {
                    $out += [pscustomobject]@{ Src=[string]$b.condition; Path='blocks[].condition' }
                }
            }
        }
        if ((Test-Prop $p 'forensic') -and $p.forensic -and (Test-Prop $p.forensic 'suspiciousPatterns')) {
            foreach ($s in @($p.forensic.suspiciousPatterns)) {
                if ($s -is [psobject] -and (Test-Prop $s 'condition') -and $s.condition) {
                    # Skip prose-style conditions to avoid drowning the report
                    # in false positives from documentary entries.
                    if (Test-LooksLikeExpression $s.condition) {
                        $out += [pscustomobject]@{ Src=[string]$s.condition; Path='forensic.suspiciousPatterns[].condition' }
                    }
                }
            }
        }
        return $out
    }

    foreach ($expr in (Get-ExprStrings $parsed)) {
        # Strip string literals so identifiers inside quotes don't false-positive.
        $src = $expr.Src
        $stripped = [regex]::Replace($src, "'(?:[^'\\]|\\.)*'", "''")
        $stripped = [regex]::Replace($stripped, '"(?:[^"\\]|\\.)*"', '""')
        # Skip member accesses (right side of a dot): identifier preceded by '.'
        # is a method/property on the receiver and resolved at eval time.
        $reported = @{}
        foreach ($m in [regex]::Matches($stripped, '(?<![.\w])([A-Za-z_][A-Za-z0-9_]*)')) {
            $id = $m.Groups[1].Value
            if ($known.ContainsKey($id)) { continue }
            if ($reported.ContainsKey($id)) { continue }
            $reported[$id] = $true
            Add-Finding 'WARN' 'whfmt-expression-refs' $rel ("{0}: '{1}' not declared in variables{{}} / functions{{}} / builtins" -f $expr.Path, $id)
        }
    }
    } catch {
        Add-Finding 'ERR' 'whfmt-guard-internal' $rel ("rule evaluation failed: " + $_.Exception.Message)
    }
}

# ----- report -----
$errCount  = @($findings | Where-Object { $_.Sev -eq 'ERR'  }).Count
$warnCount = @($findings | Where-Object { $_.Sev -eq 'WARN' }).Count

foreach ($f in $findings) {
    Write-Host ("  {0,-4} {1}: {2} — {3}" -f $f.Sev, $f.Rule, $f.File, $f.Detail)
}
Write-Host "summary: $errCount error(s), $warnCount warning(s)"

exit [Math]::Min($errCount, 100)
