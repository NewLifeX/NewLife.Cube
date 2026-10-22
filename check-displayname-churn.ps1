# 检查提交区间内 [DisplayName] 是否出现 A→B→A 的往返（同一文件里的显示名被改回旧值）
# 往返意味着两次提交相互抵消，属于无谓改动：应改为一次都不改，或合并成一次有效变更。
#
# 用法：
#   pwsh -File check-displayname-churn.ps1                          # 默认检查最近 15 个提交
#   pwsh -File check-displayname-churn.ps1 -Range 71e8a4dd..HEAD    # 指定区间
#   pwsh -File check-displayname-churn.ps1 -WarnOnly                # 只告警不失败
param(
    [String]$Range = "HEAD~15..HEAD",
    [String]$Path = "*.cs",
    [Switch]$WarnOnly
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [Text.Encoding]::UTF8

$repo = $PSScriptRoot

$first = & git -C $repo log --format="%h" -1 $Range 2>$null
if (!$first) {
    Write-Host "无法解析提交区间：$Range" -ForegroundColor Red
    exit 1
}

# 01 开头标记提交边界；-U0 让每个 hunk 只包含实际改动的行
$lines = & git -C $repo log --reverse --format="%x01%h %s" -U0 $Range -- $Path
if ($LASTEXITCODE -ne 0) {
    Write-Host "git log 执行失败：$Range" -ForegroundColor Red
    exit 1
}

$minusRx = '^-\s*\[DisplayName\("(.*)"\)\]\s*$'
$plusRx = '^\+\s*\[DisplayName\("(.*)"\)\]\s*$'

$changes = [System.Collections.Generic.List[object]]::new()
$pending = [System.Collections.Generic.List[string]]::new()
$commit = ""
$subject = ""
$file = ""
$index = 0

foreach ($line in $lines) {
    if ($line.StartsWith([char]1)) {
        $text = $line.Substring(1)
        $sp = $text.IndexOf(' ')
        $commit = $text.Substring(0, $sp)
        $subject = $text.Substring($sp + 1)
        $index++
        $file = ""
        $pending.Clear()
        continue
    }
    if ($line -like "+++ b/*") { $file = $line.Substring(6); $pending.Clear(); continue }
    if ($line -like "@@*") { $pending.Clear(); continue }
    if ($line -match $minusRx) { $pending.Add($matches[1]); continue }
    if ($line -match $plusRx) {
        if ($pending.Count -eq 0) { continue }
        $old = $pending[0]
        $pending.RemoveAt(0)
        if ($old -ne $matches[1]) {
            $changes.Add([pscustomobject]@{
                Index   = $index
                Commit  = $commit
                Subject = $subject
                File    = $file
                Old     = $old
                New     = $matches[1]
            })
        }
    }
}

# 往返判定：同一文件里较早一次 X→Y，较晚一次 Y→X
$hits = @()
for ($i = 0; $i -lt $changes.Count; $i++) {
    $a = $changes[$i]
    for ($j = $i + 1; $j -lt $changes.Count; $j++) {
        $b = $changes[$j]
        if ($b.Index -le $a.Index) { continue }
        if ($b.File -eq $a.File -and $b.Old -eq $a.New -and $b.New -eq $a.Old) {
            $hits += [pscustomobject]@{
                File     = $a.File
                Value    = $a.Old
                Changed  = $a.Commit
                Reverted = $b.Commit
                RoundTrip = "$($a.Old) → $($a.New) → $($b.New)"
            }
        }
    }
}

if ($hits.Count -eq 0) {
    Write-Host "未发现 DisplayName 往返（区间 $Range，改动 $($changes.Count) 处）" -ForegroundColor Green
    exit 0
}

Write-Host "发现 $($hits.Count) 处 DisplayName 往返（区间 $Range）：" -ForegroundColor Yellow
$hits | Sort-Object File, Changed | Format-Table -AutoSize | Out-String -Width 200 | Write-Host

if ($WarnOnly) { exit 0 }

Write-Host "建议：把往返两边的改动都去掉，或合并成一次有效变更后再提交。" -ForegroundColor Yellow
exit 2
