# 检查运行时消息（异常、日志、控制台、界面提示、埋点标签）是否全部为英文
# 规则：调用点字符串中不得出现 CJK 字符；注释中的中文不受限制。
# 仅检查 API 版（NewLife.Cube）；MVC 版不纳入运行时多语言范围。
#
# 用法：
#   pwsh -File check-english-messages.ps1                         # 默认目录，失败退出码 1
#   pwsh -File check-english-messages.ps1 -Dir NewLife.Cube -List # 盘点中文消息，不失败
param(
    [String]$Root = $PSScriptRoot,
    [String[]]$Dir,
    [String[]]$Exclude = @(),
    [String]$MessageCall = 'throw new|: base\(|Console\.Write|MessageBox\.Show|AppendTag\(|NewError\(|NewSpan\(|Debug\.Fail\(|WriteLog\(|OnWrite\(|XTrace\.|Log\.(Write|Error|Info|Debug|Warn|Fatal)|(Info|Error|Warn|Debug|Fatal)Prefixed\(|WritePrefixed\(|L\.T\(|Localization\.Translate|ApiException\(|HttpException\(',
    [Switch]$List,
    [String]$Csv
)

$ErrorActionPreference = "Stop"

if (!$Dir -or $Dir.Count -eq 0) { $Dir = @("NewLife.Cube") }
$dirs = @()
foreach ($item in $Dir) {
    $path = if ([IO.Path]::IsPathRooted($item)) { $item } else { Join-Path $Root $item }
    if (Test-Path -LiteralPath $path) { $dirs += $path }
}
if ($dirs.Count -eq 0) { throw "未找到待检查目录：$($Dir -join ', ')" }

$hits = @()

# NewLife.Cube 通过 <Compile Include="..\NewLife.CubeNC\..."> 链接共享源码，
# 这些文件编译进 API 版程序集，运行时消息同样必须使用英文源文（2026-10-20）
$scanFiles = @()
foreach ($dir in $dirs) { $scanFiles += Get-ChildItem $dir -Recurse -Filter *.cs }
$linkProject = Join-Path $Root "NewLife.Cube\NewLife.Cube.csproj"
if (Test-Path $linkProject) {
    $csproj = [IO.File]::ReadAllText($linkProject)
    $linkRoot = Join-Path $Root "NewLife.CubeNC"
    foreach ($m in [regex]::Matches($csproj, 'Include="\.\.\\NewLife\.CubeNC\\([^"]+)"')) {
        $linked = Join-Path $linkRoot $m.Groups[1].Value.Replace('\', '/')
        if (Test-Path $linked) { $scanFiles += Get-Item $linked }
    }
}
$scanFiles = $scanFiles | Sort-Object FullName -Unique

foreach ($file in $scanFiles) {
    $rel = if ($file.FullName.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase)) {
            $file.FullName.Substring($Root.Length).TrimStart("\", "/")
        }
        else { $file.FullName }

        if ($rel -match '\\(obj|bin)\\') { continue }
        $excluded = $false
        foreach ($pattern in $Exclude) {
            if ($rel -like $pattern -or $file.FullName -like $pattern) { $excluded = $true; break }
        }
        if ($excluded) { continue }

        $lines = Get-Content $file.FullName -Encoding UTF8
        $inBlock = $false
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i].Trim()

            if ($line -match "/\*") { $inBlock = $true }
            if ($inBlock) {
                if ($line -match "\*/") { $inBlock = $false }
                continue
            }
            if ($line.StartsWith("//")) { continue }

            # 去掉行尾注释，避免把注释里的中文算作运行时消息
            $code = $line -replace "//.*$", ""
            # 特性参数（Description/DisplayName/Obsolete/RequiresXxx/Justification 等）属元数据，不在本检查范围
            if ($code -match "^\s*\[") { continue }
            # 共享源码（CubeNC）的服务端日志暂保留中文模板，属待迁移存量（见 Doc/功能清单.md）；
            # 本检查对共享文件只约束用户可见消息（异常/接口响应），避免存量日志噪声淹没新问题
            if ($rel -like "NewLife.CubeNC\*" -and $code -match "XTrace\.|WriteLog\(|AppendTag\(|Log\.(Write|Error|Info|Debug|Warn|Fatal)") { continue }
            if ($code -match $MessageCall -and $code -match "[\u4e00-\u9fa5]") {
                $hits += [pscustomobject]@{ File = $rel; Line = $i + 1; Text = $code }
            }
        }
}

if ($Csv) {
    $hits | Export-Csv -LiteralPath $Csv -NoTypeInformation -Encoding UTF8
    Write-Host "已导出 $($hits.Count) 条中文运行时消息到 $Csv"
    exit 0
}

if ($List) {
    Write-Host "盘点模式：发现 $($hits.Count) 条中文运行时消息" -ForegroundColor Yellow
    foreach ($hit in $hits) {
        Write-Host ("  {0}:{1}: {2}" -f $hit.File, $hit.Line, $hit.Text)
    }
    exit 0
}

if ($hits.Count -gt 0) {
    Write-Host "发现 $($hits.Count) 处中文运行时消息，请改为英文：" -ForegroundColor Red
    foreach ($hit in $hits) {
        Write-Host ("  {0}:{1}: {2}" -f $hit.File, $hit.Line, $hit.Text)
    }
    exit 1
}

Write-Host "OK: 未发现中文运行时消息。"
exit 0
