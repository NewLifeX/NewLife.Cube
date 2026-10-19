# 将仓库内 NewLife.* PackageReference 升级到 NuGet 最新预发布版本
#
# 用法：
#   pwsh -File scripts/update-newlife-packages.ps1            # 查询并写入最新版本
#   pwsh -File scripts/update-newlife-packages.ps1 -Check     # 只检查，过期时退出码 1（CI 用）
#   pwsh -File scripts/update-newlife-packages.ps1 -Package NewLife.Core
param(
    [Switch]$Check,
    [String]$Package,
    [String]$Root = (Split-Path $PSScriptRoot -Parent),
    [String]$Feed = "https://api.nuget.org/v3-flatcontainer"
)

$ErrorActionPreference = "Stop"

# NewLife 版本形如 12.0.2026.1018-beta1517：先比数字段，再按 NuGet 规则比预发布标签
function Compare-Version([String]$Left, [String]$Right) {
    $lp = $Left.Split('-', 2)
    $rp = $Right.Split('-', 2)
    $ln = $lp[0].Split('.')
    $rn = $rp[0].Split('.')
    for ($i = 0; $i -lt [Math]::Max($ln.Count, $rn.Count); $i++) {
        $a = if ($i -lt $ln.Count) { [Int64]$ln[$i] } else { 0 }
        $b = if ($i -lt $rn.Count) { [Int64]$rn[$i] } else { 0 }
        if ($a -ne $b) { return $a.CompareTo($b) }
    }

    $lpre = if ($lp.Count -gt 1) { $lp[1] } else { "" }
    $rpre = if ($rp.Count -gt 1) { $rp[1] } else { "" }
    if ($lpre -eq $rpre) { return 0 }
    if ($lpre -eq "") { return 1 }
    if ($rpre -eq "") { return -1 }
    return [StringComparer]::OrdinalIgnoreCase.Compare($lpre, $rpre)
}

$projectFiles = Get-ChildItem -Path $Root -Recurse -Filter *.csproj -File |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
if ($projectFiles.Count -eq 0) { throw "未找到 csproj：$Root" }

$refs = @()
foreach ($file in $projectFiles) {
    $text = [IO.File]::ReadAllText($file.FullName)
    foreach ($m in [regex]::Matches($text, '<PackageReference\s+Include="(NewLife\.[^"]+)"\s+Version="([^"]+)"')) {
        $refs += [pscustomobject]@{
            File = $file.FullName
            Name = $m.Groups[1].Value
            Version = $m.Groups[2].Value
            Match = $m.Value
        }
    }
}
if ($Package) { $refs = @($refs | Where-Object { $_.Name -eq $Package }) }
if ($refs.Count -eq 0) { Write-Host "没有需要处理的 NewLife.* 引用"; exit 0 }

$latest = @{}
$errors = 0
foreach ($name in ($refs.Name | Sort-Object -Unique)) {
    try {
        $index = Invoke-RestMethod -Uri "$Feed/$($name.ToLowerInvariant())/index.json" -TimeoutSec 30
        $best = $null
        foreach ($v in $index.versions) {
            if ($best -eq $null -or (Compare-Version $v $best) -gt 0) { $best = $v }
        }
        $latest[$name] = $best
    }
    catch {
        Write-Host "✗ 查询 $name 失败：$($_.Exception.Message)"
        $errors++
    }
}

$changed = 0
foreach ($ref in $refs) {
    $new = $latest[$ref.Name]
    if (!$new) { continue }
    $cmp = Compare-Version $new $ref.Version
    if ($cmp -le 0) {
        if ($Check) { Write-Host "✓ $($ref.Name) $($ref.Version)" }
        continue
    }

    if ($Check) {
        Write-Host "✗ $($ref.Name) $($ref.Version) -> $new ($($ref.File))"
        $changed++
        continue
    }

    $text = [IO.File]::ReadAllText($ref.File)
    $text = $text.Replace($ref.Match, $ref.Match.Replace($ref.Version, $new))
    [IO.File]::WriteAllText($ref.File, $text, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "✓ $($ref.Name) $($ref.Version) -> $new"
    $changed++
}

if ($Check -and $changed -gt 0) {
    Write-Host "`n有 $changed 处引用落后于 NuGet 最新版，请运行 pwsh -File scripts/update-newlife-packages.ps1"
    exit 1
}
if (!$Check -and $changed -eq 0) { Write-Host "全部 NewLife.* 引用已是最新" }
if ($errors -gt 0) { exit 1 }
