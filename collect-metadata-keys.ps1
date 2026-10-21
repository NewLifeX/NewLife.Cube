# 收集实体元数据键：Model.xml 的表/字段描述，或生成代码中的 [DisplayName]/[Description] 特性。
# 输出 JSON 数组，供 check-locales.ps1 -MetadataKeys 使用；元数据键允许部分语言缺失并回退源文。
#
# 用法：
#   pwsh -File collect-metadata-keys.ps1 -Model XCode\Membership\Member.xml -Output XCode\Locales\metadata-keys.json
#   pwsh -File collect-metadata-keys.ps1 -SourceDir ..\Zero.Data -Output ..\Zero.Data\Locales\metadata-keys.json
param(
    [String[]]$Model = @(),
    [String[]]$SourceDir = @(),
    [String]$Output = "Locales\metadata-keys.json"
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$keys = [System.Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)

function Add-Key([String]$text) {
    if ([String]::IsNullOrWhiteSpace($text)) { return }

    $value = $text.Trim()
    [void]$keys.Add($value)

    # 与 ModelResolver.GetDisplayName 保持一致：取首个分隔符前的短语
    $index = $value.IndexOfAny([Char[]]@('.', '。', ',', '，', ';', '；', ':', '：', "`r", "`n"))
    if ($index -gt 0) {
        $display = $value.Substring(0, $index).Trim()
        if ($display) { [void]$keys.Add($display) }
    }
}

foreach ($item in @($Model | Where-Object { $_ })) {
    $path = if ([IO.Path]::IsPathRooted($item)) { $item } else { Join-Path $root $item }
    if (!(Test-Path -LiteralPath $path)) {
        Write-Host "未找到模型文件：$path" -ForegroundColor Red
        exit 1
    }

    $xml = [xml](Get-Content -LiteralPath $path -Raw -Encoding UTF8)
    foreach ($table in $xml.SelectNodes("//*[local-name()='Table']")) {
        Add-Key ([String]$table.Description)
        foreach ($column in $table.SelectNodes(".//*[local-name()='Column']")) {
            Add-Key ([String]$column.Description)
        }
    }
}

$pattern = '\[(?:DisplayName|Description)\("((?:[^"\\]|\\.)*)"\)\]'

# 去掉整行注释，避免把注释掉的旧文案收进清单
function Get-CodeLines([String]$text) {
    $lines = $text -split "`r?`n"
    return ($lines | Where-Object { !$_.TrimStart().StartsWith('//') }) -join "`n"
}

foreach ($dir in @($SourceDir | Where-Object { $_ })) {
    $dirPath = if ([IO.Path]::IsPathRooted($dir)) { $dir } else { Join-Path $root $dir }
    if (!(Test-Path -LiteralPath $dirPath)) {
        Write-Host "未找到源码目录：$dirPath" -ForegroundColor Red
        exit 1
    }

    foreach ($file in Get-ChildItem $dirPath -Recurse -Filter *.cs -File | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }) {
        $text = Get-CodeLines ([IO.File]::ReadAllText($file.FullName))
        foreach ($m in [regex]::Matches($text, $pattern)) {
            $key = $m.Groups[1].Value.Replace('\"', '"').Replace('\\', '\')
            Add-Key $key
        }
        # 控制器/服务里用属性赋值设置的列表列名、表头与表单字段名，同样属于元数据
        foreach ($m in [regex]::Matches($text, '(?:DisplayName|Header|HeaderTitle)\s*=\s*"((?:[^"\\]|\\.)*)"')) {
            $key = $m.Groups[1].Value.Replace('\"', '"').Replace('\\', '\')
            Add-Key $key
        }
    }
}

# 程序集元数据（AssemblyTitle/Description）按 Core 规范以英文源文入表，属单值元数据
foreach ($dir in @($SourceDir | Where-Object { $_ })) {
    $dirPath = if ([IO.Path]::IsPathRooted($dir)) { $dir } else { Join-Path $root $dir }
    if (!(Test-Path -LiteralPath $dirPath)) { continue }

    foreach ($file in Get-ChildItem $dirPath -Recurse -Filter *.csproj -File | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }) {
        $text = [IO.File]::ReadAllText($file.FullName)
        foreach ($m in [regex]::Matches($text, '<(?:AssemblyTitle|Description)>([^<]+)</(?:AssemblyTitle|Description)>')) {
            Add-Key ([System.Net.WebUtility]::HtmlDecode($m.Groups[1].Value))
        }
    }
}

$outputPath = if ([IO.Path]::IsPathRooted($Output)) { $Output } else { Join-Path $root $Output }
$outputDir = [IO.Path]::GetDirectoryName($outputPath)
if ($outputDir -and !(Test-Path -LiteralPath $outputDir)) { [void][IO.Directory]::CreateDirectory($outputDir) }

# 补充清单：代码里以 L.T 使用的界面标签（图表系列名、日志动作等）与实体元数据同属界面文案，
# 允许按语言增量翻译，缺失回退源文。清单文件与输出目录同级。
$extraPath = Join-Path $outputDir 'metadata-extra.json'
if (Test-Path -LiteralPath $extraPath) {
    $extra = Get-Content -LiteralPath $extraPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($item in @($extra)) { if ($item) { Add-Key ([String]$item) } }
}

$list = @($keys | Sort-Object)
ConvertTo-Json -InputObject $list -Depth 2 | Set-Content -LiteralPath $outputPath -Encoding UTF8

Write-Host "已收集 $($list.Count) 个元数据键：$outputPath" -ForegroundColor Green
