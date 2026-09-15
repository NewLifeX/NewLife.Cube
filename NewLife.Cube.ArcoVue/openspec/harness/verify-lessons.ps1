<#
.SYNOPSIS
    OpenSpec Harness 校验：lessons.md 条目索引 ↔ 正文双向锁定，并核对归档 OSC 教训覆盖。

.DESCRIPTION
    只读脚本（不修改任何文件），对应 openspec/README.md「Harness 教训库」约定：
      1. ERROR：正文条目（`## ` 标题，排除「格式」「条目索引」与代码围栏内容）必须出现在索引表；
      2. ERROR：索引表每行「条目」必须对应正文条目（防幽灵行）；
      3. ERROR：同一标题在正文出现多次（重复条目）；
      4. ERROR：changes/archive/ 下每个 OSC 至少有一条以该 ID 开头的教训条目；
      5. WARN ：标题格式偏离约定（如新号应为 `OSC-YYMMDDxxxx — <日期>`）。
    退出码：0 = 无 ERROR；1 = 存在 ERROR（门禁/CI 友好）。

.PARAMETER Root
    openspec 根目录；默认为脚本上级目录（harness 的父目录）。

.EXAMPLE
    powershell -File NewLife.Cube.ArcoVue\openspec\harness\verify-lessons.ps1

.EXAMPLE
    # 校验自带结构的临时副本（负例演练）
    powershell -File NewLife.Cube.ArcoVue\openspec\harness\verify-lessons.ps1 -Root "$env:TEMP\openspec-fixture"
#>
param(
    [String]$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function ReadText([String]$Path) {
    return [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
}

$lessonsPath = Join-Path $Root "harness/lessons.md"
if (-not (Test-Path $lessonsPath)) {
    Write-Host "lessons.md not found: $lessonsPath"
    exit 1
}

# ── 解析标题与索引 ────────────────────────────────────────────────────────────
$headings = [System.Collections.Generic.List[String]]::new()
$index    = [System.Collections.Generic.List[String]]::new()
$inFence  = $false
$inIndex  = $false

foreach ($line in ((ReadText $lessonsPath) -split "\r?\n")) {
    $t = $line.Trim()
    if ($t.StartsWith('```')) { $inFence = -not $inFence; continue }
    if ($inFence) { continue }

    if ($t -match '^##\s+(.+?)\s*$') {
        $title = $Matches[1]
        if ($title -match '^\u683C\u5F0F$') { $inIndex = $false; continue }                 # 格式
        if ($title -match '^\u6761\u76EE\u7D22\u5F15$') { $inIndex = $true; continue }      # 条目索引
        $inIndex = $false
        $headings.Add($title)
        continue
    }

    if ($inIndex -and $t.StartsWith('|')) {
        $cells = $t.Trim('|') -split '\|'
        if ($cells.Count -lt 1) { continue }
        $key = $cells[0].Trim()
        if ($key -and ($key -notmatch '^\u6761\u76EE$') -and ($key -notmatch '^-+$')) { $index.Add($key) }
    }
}

# ── 双向比对 ──────────────────────────────────────────────────────────────────
$missingInIndex = @($headings | Where-Object { $index -notcontains $_ })
$missingInBody  = @($index    | Where-Object { $headings -notcontains $_ })
$duplicates     = @($headings | Group-Object | Where-Object { $_.Count -gt 1 } | ForEach-Object { "$($_.Name) (x$($_.Count))" })

# ── 归档覆盖（Done OSC 须有教训条目） ────────────────────────────────────────
$archiveDir = Join-Path $Root "changes/archive"
$uncovered  = [System.Collections.Generic.List[String]]::new()
if (Test-Path $archiveDir) {
    foreach ($d in (Get-ChildItem -Path $archiveDir -Directory)) {
        $id = ($d.Name -split ' ')[0]
        $found = $false
        foreach ($h in $headings) {
            if ($h -match ('^' + [regex]::Escape($id) + '(\s|$)')) { $found = $true; break }
        }
        if (-not $found) { $uncovered.Add($d.Name) }
    }
}

# ── 标题格式（WARN） ─────────────────────────────────────────────────────────
$formatWarnings = [System.Collections.Generic.List[String]]::new()
foreach ($h in $headings) {
    $ok = $false
    if ($h -match '^OSC-\d{6}[0-9a-f]{4} \u2014 \d{4}-\d{2}-\d{2}$') { $ok = $true }
    elseif ($h -match '^OSC-\d{4}( .+)? \u2014 \d{4}-\d{2}-\d{2}$') { $ok = $true }
    elseif ($h -match '^\u6D41\u7A0B( .+)? \u2014 \d{4}-\d{2}-\d{2}$') { $ok = $true }    # 流程
    elseif ($h -match '^\u5F85\u529E \u2014 .+$') { $ok = $true }                          # 待办
    if (-not $ok) { $formatWarnings.Add($h) }
}

# ── 报告 ──────────────────────────────────────────────────────────────────────
# （消息用英文：PS 5.1 对无 BOM 的 UTF-8 脚本按 ANSI 解码，中文字面量会失真）
Write-Host "OpenSpec Harness - lessons.md check"
Write-Host ("  headings: {0} / index rows: {1} / archived OSCs: {2}" -f $headings.Count, $index.Count, @(Get-ChildItem -Path $archiveDir -Directory -ErrorAction SilentlyContinue).Count)
Write-Host ""

$hasError = $false

if ($missingInIndex.Count -gt 0) {
    $hasError = $true
    Write-Host "[ERROR] Headings missing from index (add one row per heading; key = exact heading text):" -ForegroundColor Red
    foreach ($h in $missingInIndex) { Write-Host ("  | {0} | (summary TBD) |" -f $h) -ForegroundColor Red }
}

if ($missingInBody.Count -gt 0) {
    $hasError = $true
    Write-Host "[ERROR] Index rows without a matching heading (remove ghost rows or restore the heading):" -ForegroundColor Red
    foreach ($h in $missingInBody) { Write-Host ("  {0}" -f $h) -ForegroundColor Red }
}

if ($duplicates.Count -gt 0) {
    $hasError = $true
    Write-Host "[ERROR] Duplicate headings (a heading may appear only once):" -ForegroundColor Red
    foreach ($h in $duplicates) { Write-Host ("  {0}" -f $h) -ForegroundColor Red }
}

if ($uncovered.Count -gt 0) {
    $hasError = $true
    Write-Host "[ERROR] Archived OSCs without any lesson entry (add one during retro):" -ForegroundColor Red
    foreach ($h in $uncovered) { Write-Host ("  {0}" -f $h) -ForegroundColor Red }
}

if ($formatWarnings.Count -gt 0) {
    Write-Host "[WARN] Headings not matching the naming convention (non-blocking):" -ForegroundColor Yellow
    foreach ($h in $formatWarnings) { Write-Host ("  {0}" -f $h) -ForegroundColor Yellow }
}

Write-Host ""
if ($hasError) {
    Write-Host "Result: ERROR(s) found - fix and re-run." -ForegroundColor Red
    exit 1
}

Write-Host "Result: index and headings are in sync; archive coverage complete." -ForegroundColor Green
exit 0
