# 校验源码里 L.T / L.TLog / Localization.Translate 的字面量键都有中文译文
# 规则：含中文字符的键视为「源文元数据」（实体/枚举/菜单等以中文源文为 key），豁免；
#       英文源文键必须在基准词表（默认 zh-Hans.yml）中存在，否则中文界面会回退英文。
#
# 用法：
#   pwsh -File check-missing-keys.ps1                      # 默认目录，失败退出码 1
#   pwsh -File check-missing-keys.ps1 -List                # 只盘点缺失键，不失败
#   pwsh -File check-missing-keys.ps1 -LocaleDir NewLife.Cube/Locales -Baseline zh-Hans.yml
param(
    [String]$Root = $PSScriptRoot,
    [String[]]$Dir = @("NewLife.Cube", "NewLife.CubeNC"),
    [String]$LocaleDir = "NewLife.Cube\Locales",
    [String]$Baseline = "zh-Hans.yml",
    [Switch]$List
)

$ErrorActionPreference = "Stop"

function Resolve-Path2([String]$path) {
    if ([IO.Path]::IsPathRooted($path)) { return $path }
    return Join-Path $Root $path
}

$localePath = Join-Path (Resolve-Path2 $LocaleDir) $Baseline
if (!(Test-Path -LiteralPath $localePath)) {
    Write-Host "未找到基准词表：$localePath" -ForegroundColor Red
    exit 1
}

# 基准词表的键集合
$lineRx = '^\s*"((?:[^"\\]|\\.)*)"\s*:\s*"((?:[^"\\]|\\.)*)"\s*$'
$keys = [System.Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
foreach ($line in [IO.File]::ReadAllLines($localePath, [Text.Encoding]::UTF8)) {
    $m = [regex]::Match($line, $lineRx)
    if ($m.Success) { [void]$keys.Add($m.Groups[1].Value) }
}

# 放行清单：确认不需要面向用户翻译的键（如 AI 工具提示词）
$allow = [System.Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
$allowPath = Join-Path (Resolve-Path2 $LocaleDir) 'missing-keys.allow.json'
if (Test-Path -LiteralPath $allowPath) {
    $json = Get-Content -LiteralPath $allowPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($item in @($json.keys)) { if ($item) { [void]$allow.Add([String]$item) } }
}

# 待扫描文件：源码目录 + NewLife.Cube.csproj 链接的 CubeNC 共享源码
$files = @()
foreach ($d in $Dir) {
    $dirPath = Resolve-Path2 $d
    if (!(Test-Path -LiteralPath $dirPath)) { continue }
    $files += Get-ChildItem $dirPath -Recurse -File |
        Where-Object { $_.Extension -in '.cs', '.cshtml' } |
        Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
}
$linkProject = Join-Path $Root "NewLife.Cube\NewLife.Cube.csproj"
if (Test-Path -LiteralPath $linkProject) {
    $csproj = [IO.File]::ReadAllText($linkProject)
    $linkRoot = Join-Path $Root "NewLife.CubeNC"
    foreach ($m in [regex]::Matches($csproj, 'Include="\.\.\\NewLife\.CubeNC\\([^"]+)"')) {
        $linked = Join-Path $linkRoot $m.Groups[1].Value.Replace('\', '/')
        if ((Test-Path -LiteralPath $linked) -and $linked.EndsWith('.cs')) { $files += Get-Item $linked }
    }
}
$files = $files | Sort-Object FullName -Unique

# 字面量调用：L.T("...")、L.TLog("...")、Localization.Translate("...")，支持 @"..."
$callRx = '(?:^|[^\w.])(?:L\.T(?:Log)?|Localization\.Translate)\s*\(\s*(?:"((?:[^"\\]|\\.)*)"|@"((?:[^"]|"")*)")'
# 中文汉字与全角标点都视为「源文」键（实体/枚举/模板以中文源文为 key，回退中文本身即正确）
$cjk = [regex]'[\u3000-\u303F\u4E00-\u9FFF\uFF00-\uFFEF]'

$missing = @()
$dynamic = 0
$checked = 0
foreach ($file in $files) {
    $lines = [IO.File]::ReadAllLines($file.FullName, [Text.Encoding]::UTF8)
    $rel = $file.FullName
    if ($rel.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase)) { $rel = $rel.Substring($Root.Length).TrimStart('\', '/') }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $text = $lines[$i]
        if ($text.IndexOf('L.T') -lt 0 -and $text.IndexOf('Localization.Translate') -lt 0) { continue }

        foreach ($m in [regex]::Matches($text, $callRx)) {
            $key = if ($m.Groups[1].Success) { $m.Groups[1].Value } else { $m.Groups[2].Value.Replace('""', '"') }
            $key = $key.Replace('\"', '"').Replace('\\', '\').Replace('\n', "`n").Replace('\r', "`r").Replace('\t', "`t")
            if ($key -eq '') { $dynamic++; continue }
            if ($cjk.IsMatch($key)) { continue }
            $checked++
            if (!$keys.Contains($key) -and !$allow.Contains($key)) { $missing += [pscustomobject]@{ File = $rel; Line = $i + 1; Key = $key } }
        }

        # 变量/表达式作键：无法静态校验，单独计数
        if ($text -match '(?:L\.T(?:Log)?|Localization\.Translate)\s*\(\s*[^"\s\)]' -and $text -notmatch '(?:L\.T(?:Log)?|Localization\.Translate)\s*\(\s*[@"]') { $dynamic++ }
    }
}

foreach ($m in $missing) {
    Write-Host ("{0}({1}): 缺少中文译文 → {2}" -f $m.File, $m.Line, $m.Key) -ForegroundColor Red
}

if ($List) {
    Write-Host ("盘点完成：检查 {0} 处英文键，缺失 {1} 处，动态键 {2} 处（未静态校验）" -f $checked, $missing.Count, $dynamic) -ForegroundColor Yellow
    exit 0
}

if ($missing.Count -gt 0) {
    Write-Host ("英文源文键缺少中文译文：{0} 处。请把键加入 {1} 并补中文译文。" -f $missing.Count, $Baseline) -ForegroundColor Red
    exit 1
}

Write-Host ("OK: {0} 处英文源文键均有中文译文（动态键 {1} 处未校验）。" -f $checked, $dynamic) -ForegroundColor Green
