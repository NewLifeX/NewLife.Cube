# 多语言词表一致性校验：键集合、占位符、格式说明符
# 以基准语言文件（默认 zh-Hans.yml）为准，所有语言必须提供完全相同的键集合
#
# 用法：
#   pwsh -File check-locales.ps1                                      # 魔方默认目录（NewLife.Cube + NewLife.CubeNC）
#   pwsh -File check-locales.ps1 -LocaleDir NewLife.Cube\Locales -SourceDir NewLife.Cube,NewLife.CubeNC
#   pwsh -File check-locales.ps1 -LocaleDir NewLife.Cube\Locales -SkipOrphan   # 下游只想校验词表内部一致性
#   pwsh -File check-locales.ps1 -MetadataKeys NewLife.Cube\Locales\metadata-keys.json -MetadataSourceDir NewLife.Cube\Entity  # 元数据键允许部分语言缺失
#   pwsh -File check-locales.ps1 -MetadataKeys ... -RequireFullCoverage   # 元数据键也必须每种语言都有译文（魔方 CI 口径）
#   pwsh -File check-locales.ps1 -MetadataKeys ... -FullCoverageCultures "zh-Hant,de,fr" -CoverageBaseline NewLife.Cube\Locales\metadata-coverage-baseline.json
#       译文较全的语言强制 100% 覆盖；其余语言按基线棘轮，只拦新增缺口
#   pwsh -File check-locales.ps1 -MetadataKeys ... -UpdateCoverageBaseline ... -CoverageBaseline NewLife.Cube\Locales\metadata-coverage-baseline.json
#       重新生成覆盖基线（人工确认后执行）
param(
    [String]$LocaleDir = "NewLife.Cube\Locales",
    [String]$Baseline = "zh-Hans.yml",
    [String[]]$SourceDir = @("NewLife.Cube", "NewLife.CubeNC"),
    # 元数据键清单，允许部分语言缺失（缺失回退源文），并免于孤儿键检查
    [String]$MetadataKeys,
    # 从生成实体/控制器的 [DisplayName]/[Description] 特性中收集元数据键
    [String[]]$MetadataSourceDir = @(),
    [Switch]$SkipOrphan,
    [Switch]$Strict,
    # 元数据键不再允许部分语言缺失：每种语言都必须有译文，缺失即报错
    [Switch]$RequireFullCoverage,
    # 必须 100% 覆盖元数据键的语言（逗号或分号分隔），其余语言走覆盖基线棘轮
    [String]$FullCoverageCultures,
    # 覆盖基线：记录各语言当前允许缺失的元数据键，只允许减少不允许新增
    [String]$CoverageBaseline,
    # 按当前词表重新生成覆盖基线后退出
    [Switch]$UpdateCoverageBaseline
)

$ErrorActionPreference = "Stop"

# pwsh -File 会把 "NewLife.Cube,NewLife.CubeNC" 当成单个参数，这里统一拆成多个目录
$SourceDir = @($SourceDir | ForEach-Object { $_ -split '[,;]' } | Where-Object { $_ })

$root = $PSScriptRoot
$localeDir = if ([IO.Path]::IsPathRooted($LocaleDir)) { $LocaleDir } else { Join-Path $root $LocaleDir }
if (!(Test-Path $localeDir)) {
    Write-Host "未找到语言目录：$localeDir" -ForegroundColor Red
    exit 1
}

$pattern = '^\s*"((?:[^"\\]|\\.)*)"\s*:\s*"((?:[^"\\]|\\.)*)"\s*$'
$placeholder = '\{(\d+)(?:,-?\d+)?(:[^}]*)?\}'

function Unescape-Yaml([string]$text) {
    # 与源生成器保持一致：还原 \\ \" \n \r \t \uXXXX
    return [regex]::Replace($text, '\\(u[0-9a-fA-F]{4}|.)', {
        param($m)
        $v = $m.Groups[1].Value
        if ($v.Length -eq 5) { return [string][char][Convert]::ToInt32($v.Substring(1), 16) }
        switch -CaseSensitive ($v) {
            'n' { return "`n" }
            'r' { return "`r" }
            't' { return "`t" }
            # 反斜杠转义：单引号字符串里写两个反斜杠表示「反斜杠」字符本身
            '\' { return '\' }
            '"' { return '"' }
            default { return $m.Value }
        }
    })
}

function Get-Placeholders([string]$text) {
    $set = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($m in [regex]::Matches($text, $placeholder)) {
        [void]$set.Add([int]$m.Groups[1].Value)
    }
    return ,$set
}

function Get-PlaceholderSpecs([string]$text) {
    $map = @{}
    foreach ($m in [regex]::Matches($text, $placeholder)) {
        $index = [int]$m.Groups[1].Value
        if (!$map.ContainsKey($index)) { $map[$index] = $m.Groups[2].Value }
    }
    return $map
}

function Read-Locale([string]$path) {
    $entries = [ordered]@{}
    $errors = @()
    $lineNo = 0
    foreach ($line in Get-Content -LiteralPath $path -Encoding UTF8) {
        $lineNo++
        if ($line.Trim().Length -eq 0 -or $line.TrimStart().StartsWith("#")) { continue }
        $m = [regex]::Match($line, $pattern)
        if (!$m.Success) {
            $errors += "第 $lineNo 行格式非法：$line"
            continue
        }
        $key = Unescape-Yaml $m.Groups[1].Value
        if ($entries.Contains($key)) {
            $errors += "第 $lineNo 行键重复：$key"
            continue
        }
        $entries[$key] = Unescape-Yaml $m.Groups[2].Value
    }
    return @{ Entries = $entries; Errors = $errors }
}

$referencePath = Join-Path $localeDir $Baseline
if (!(Test-Path -LiteralPath $referencePath)) {
    Write-Host "未找到基准语言文件：$referencePath" -ForegroundColor Red
    exit 1
}
$reference = Read-Locale $referencePath
$allErrors = @($reference.Errors | ForEach-Object { "$($Baseline): $_" })
$allWarnings = @()

# 元数据键：来自显式清单或实体/控制器特性。允许部分语言缺失，并免于孤儿键检查
$metadataKeySet = [System.Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
if ($MetadataKeys) {
    $metaPath = if ([IO.Path]::IsPathRooted($MetadataKeys)) { $MetadataKeys } else { Join-Path $root $MetadataKeys }
    if (!(Test-Path -LiteralPath $metaPath)) {
        Write-Host "未找到元数据键清单：$metaPath" -ForegroundColor Red
        exit 1
    }
    $metaJson = Get-Content -LiteralPath $metaPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($metaJson -is [array]) { $metaItems = $metaJson }
    elseif ($null -ne $metaJson.keys) { $metaItems = $metaJson.keys }
    else { $metaItems = @() }
    foreach ($item in @($metaItems)) { if ($item) { [void]$metadataKeySet.Add([String]$item) } }
}
foreach ($dir in @($MetadataSourceDir | Where-Object { $_ })) {
    $dirPath = if ([IO.Path]::IsPathRooted($dir)) { $dir } else { Join-Path $root $dir }
    if (!(Test-Path $dirPath)) { continue }
    foreach ($file in Get-ChildItem $dirPath -Recurse -Filter *.cs -File) {
        $text = [IO.File]::ReadAllText($file.FullName)
        foreach ($m in [regex]::Matches($text, '\[(?:DisplayName|Description)\("((?:[^"\\]|\\.)*)"\)\]')) {
            $key = $m.Groups[1].Value.Replace('\"', '"').Replace('\\', '\')
            if ($key) { [void]$metadataKeySet.Add($key) }
        }
    }
}

# 运行期组合键：菜单规范键 menu:<类型全名>、字段键 field:<...> 由类型全名拼出，源码中没有字面量，
# 按元数据键口径处理——免于孤儿检查，非强制覆盖语言允许缺失（运行期回退源文/英文）
$runtimeKeySet = [System.Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
$runtimeKeyPrefixes = @("menu:", "entity:", "field:")
foreach ($key in @($reference.Entries.Keys)) {
    foreach ($prefix in $runtimeKeyPrefixes) {
        if ($key.StartsWith($prefix, [StringComparison]::Ordinal)) {
            [void]$runtimeKeySet.Add($key)
            [void]$metadataKeySet.Add($key)
            break
        }
    }
}

# 强制 100% 覆盖的语言（逗号或分号分隔）
$fullCoverageSet = [System.Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
foreach ($item in @($FullCoverageCultures | ForEach-Object { $_ -split '[,;]' } | Where-Object { $_ })) {
    [void]$fullCoverageSet.Add($item.Trim())
}

# 覆盖基线：其余语言的元数据缺口只允许减少，不允许新增
$baselineCommon = [System.Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
$baselinePerLang = @{}
$baselinePath = $null
if ($CoverageBaseline) {
    $baselinePath = if ([IO.Path]::IsPathRooted($CoverageBaseline)) { $CoverageBaseline } else { Join-Path $root $CoverageBaseline }
    if ($UpdateCoverageBaseline) {
        # 重新生成基线：统计各语言缺失的元数据键，公共缺口合并存放，避免文件膨胀
        $missingByLang = @{}
        $langs = @()
        foreach ($file in Get-ChildItem $localeDir -Filter *.yml | Sort-Object Name) {
            $lang = [IO.Path]::GetFileNameWithoutExtension($file.Name)
            if ($file.Name -eq $Baseline -or $lang -eq "en" -or $fullCoverageSet.Contains($lang)) { continue }

            $locale = Read-Locale $file.FullName
            $missing = @($metadataKeySet | Where-Object { !$locale.Entries.Contains($_) })
            $missingByLang[$lang] = $missing
            $langs += $lang
        }

        $common = $null
        foreach ($lang in $langs) {
            $set = [System.Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
            foreach ($k in $missingByLang[$lang]) { [void]$set.Add($k) }
            if ($null -eq $common) { $common = $set } else { $common.IntersectWith($set) }
        }
        if ($null -eq $common) { $common = [System.Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal) }

        $perLang = [ordered]@{}
        foreach ($lang in $langs) {
            $rest = @($missingByLang[$lang] | Where-Object { !$common.Contains($_) } | Sort-Object)
            $perLang[$lang] = $rest
        }
        $doc = [ordered]@{
            '_comment'    = '元数据词表覆盖基线：记录各语言当前允许缺失的键，只允许减少不允许新增。公共缺口放 common，个别语言额外缺口放 perLanguage。'
            'generatedAt' = (Get-Date -Format 'yyyy-MM-dd')
            'common'      = @($common | Sort-Object)
            'perLanguage' = $perLang
        }
        $dir2 = Split-Path -Parent $baselinePath
        if ($dir2 -and !(Test-Path -LiteralPath $dir2)) { New-Item -ItemType Directory -Path $dir2 | Out-Null }
        $doc | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $baselinePath -Encoding UTF8
        Write-Host "已生成覆盖基线：$baselinePath（公共缺口 $($common.Count) 条）" -ForegroundColor Green
        exit 0
    }
    if (!(Test-Path -LiteralPath $baselinePath)) {
        Write-Host "未找到覆盖基线：$baselinePath（可先用 -UpdateCoverageBaseline 生成）" -ForegroundColor Red
        exit 1
    }
    $baselineJson = Get-Content -LiteralPath $baselinePath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($item in @($baselineJson.common)) { if ($item) { [void]$baselineCommon.Add([String]$item) } }
    if ($null -ne $baselineJson.perLanguage) {
        foreach ($p in $baselineJson.perLanguage.PSObject.Properties) {
            $set = [System.Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
            foreach ($v in @($p.Value)) { if ($v) { [void]$set.Add([String]$v) } }
            $baselinePerLang[$p.Name] = $set
        }
    }
}

foreach ($file in Get-ChildItem $localeDir -Filter *.yml | Sort-Object Name) {
    if ($file.Name -eq $Baseline) { continue }
    $lang = [IO.Path]::GetFileNameWithoutExtension($file.Name)
    $locale = Read-Locale $file.FullName
    foreach ($e in $locale.Errors) { $allErrors += "$($file.Name): $e" }

    # 必查键 = 基准键 ∪ 元数据键。基准键任何语言都必须有；元数据键按覆盖策略判定
    $requiredKeys = [System.Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
    foreach ($key in $reference.Entries.Keys) { [void]$requiredKeys.Add($key) }
    foreach ($key in $metadataKeySet) { [void]$requiredKeys.Add($key) }

    $fullCoverage = $RequireFullCoverage -or $fullCoverageSet.Contains($lang)
    foreach ($key in $requiredKeys) {
        # en 是源文语言，允许只提供元数据译文，其余消息回退英文源文
        if ($lang -eq "en") { continue }
        if ($locale.Entries.Contains($key)) { continue }

        $isMeta = $metadataKeySet.Contains($key)
        if (!$isMeta) {
            $allErrors += "$($file.Name): 缺少键 $key"
            continue
        }

        # 元数据键：强制覆盖的语言缺失即报错；其余语言按基线棘轮，只拦新增缺口
        if ($fullCoverage) {
            $allErrors += "$($file.Name): 缺少元数据译文 $key"
            continue
        }
        # 运行期组合键：非强制覆盖语言允许缺失，不参与覆盖基线棘轮
        if ($runtimeKeySet.Contains($key)) { continue }
        if ($baselinePath) {
            $allowed = $baselineCommon.Contains($key) -or ($baselinePerLang.ContainsKey($lang) -and $baselinePerLang[$lang].Contains($key))
            if (!$allowed) { $allErrors += "$($file.Name): 新增元数据缺口 $key" }
        }
    }
    foreach ($key in $locale.Entries.Keys) {
        if (!$reference.Entries.Contains($key)) {
            # 元数据键允许只出现在部分语言；但仍需校验占位符
            if ($metadataKeySet.Contains($key)) {
                $value = $locale.Entries[$key]
                $expected = Get-Placeholders $key
                $actual = Get-Placeholders $value
                if (!$expected.SetEquals($actual)) { $allErrors += "$($file.Name): 占位符不一致 → $key" }
                continue
            }
            $allErrors += "$($file.Name): 多余键 $key"
            continue
        }

        $value = $locale.Entries[$key]
        $expected = Get-Placeholders $key
        $actual = Get-Placeholders $value
        if (!$expected.SetEquals($actual)) {
            $allErrors += "$($file.Name): 占位符不一致 → $key"
            continue
        }

        # 格式说明符只在译文保留该占位符时比对，缺失时仅告警
        $enSpecs = Get-PlaceholderSpecs $key
        $trSpecs = Get-PlaceholderSpecs $value
        foreach ($index in $enSpecs.Keys) {
            if ($trSpecs.ContainsKey($index) -and $trSpecs[$index] -ne $enSpecs[$index]) {
                $allWarnings += "$($file.Name): 格式说明符不一致 → $key"
                break
            }
        }
    }
}

# 英文词表：中文元数据 key 必须给出英文译文，否则英文界面会回退中文（2026-10-22）
# en 是源文语言，不参与上面的「缺少键」校验，但中文源文的元数据必须在本表补英文译文。
$enFile = Join-Path $localeDir "en.yml"
if (Test-Path -LiteralPath $enFile) {
    $enLocale = Read-Locale $enFile
    foreach ($e in $enLocale.Errors) { $allErrors += "en.yml: $e" }

    $cnRegex = '[\u4e00-\u9fa5]'
    foreach ($key in $enLocale.Entries.Keys) {
        if ($key -notmatch $cnRegex) { continue }

        $value = $enLocale.Entries[$key]
        if ($value -match $cnRegex) { $allErrors += "en.yml: 中文键缺少英文译文 → $key" }
        $expected = Get-Placeholders $key
        $actual = Get-Placeholders $value
        if (!$expected.SetEquals($actual)) { $allErrors += "en.yml: 占位符不一致 → $key" }
    }

    # 覆盖率提示：元数据清单中的中文键是否都补了英文
    if ($metadataKeySet.Count -gt 0) {
        $cnMeta = @($metadataKeySet | Where-Object { $_ -match $cnRegex })
        if ($cnMeta.Count -gt 0) {
            $missing = @($cnMeta | Where-Object { !$enLocale.Entries.Contains($_) })
            Write-Host "英文元数据覆盖率：$($cnMeta.Count - $missing.Count) / $($cnMeta.Count)" -ForegroundColor Cyan
            if ($missing.Count -gt 0) {
                $allWarnings += "en.yml: $($missing.Count) 个中文元数据键缺少英文译文（英文界面将回退中文）"
                foreach ($m in ($missing | Select-Object -First 10)) { $allWarnings += "  缺译文：$m" }
            }
        }
    }
}

# 严格模式：译文不得与英文源文完全相同（值等于键即视为未翻译），
# 同形词（如德语 Status、荷兰语 Systeem）在 Locales/identical.allow.json 中按语言登记后放行。
if ($Strict) {
    $allowPath = Join-Path $localeDir "identical.allow.json"
    $allow = @{}
    if (Test-Path -LiteralPath $allowPath) {
        $allowJson = Get-Content -LiteralPath $allowPath -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($p in $allowJson.PSObject.Properties) {
            $set = [System.Collections.Generic.HashSet[String]]::new([StringComparer]::Ordinal)
            foreach ($v in @($p.Value)) { [void]$set.Add([String]$v) }
            $allow[$p.Name] = $set
        }
    }

    foreach ($file in Get-ChildItem $localeDir -Filter *.yml | Sort-Object Name) {
        $lang = [IO.Path]::GetFileNameWithoutExtension($file.Name)
        if ($lang -eq [IO.Path]::GetFileNameWithoutExtension($Baseline) -or $lang -eq "en") { continue }

        $locale = Read-Locale $file.FullName
        $allowed = $null
        if ($allow.ContainsKey($lang)) { $allowed = $allow[$lang] }
        $count = 0
        foreach ($key in $locale.Entries.Keys) {
            if ($locale.Entries[$key] -cne $key) { continue }
            if ($null -ne $allowed -and $allowed.Contains($key)) { continue }
            $allErrors += "$($file.Name): 未翻译（值等于英文源文）→ $key"
            if ($count++ -ge 20) { $allErrors += "$($file.Name): 未翻译条目过多，仅列出前 20 条"; break }
        }
    }
}

# 孤儿键：基准表中的键在源码里完全找不到（改文案后遗留的死条目）
$orphans = @()
if (!$SkipOrphan) {
    $sourceDirs = @($SourceDir | ForEach-Object {
        if ([IO.Path]::IsPathRooted($_)) { $_ } else { Join-Path $root $_ }
    } | Where-Object { Test-Path $_ })

    # 视图里的 L.T("...") 同样会命中词表，孤儿键检测必须把 .cshtml 也算进来（2026-10-20）
    # 程序集元数据（AssemblyTitle/Description）与 PackageDescription 是单值，按 Core 规范以英文源文入表（2026-10-21）
    $sourceText = ($sourceDirs | ForEach-Object { Get-ChildItem $_ -Recurse -File | Where-Object { $_.Extension -in '.cs', '.cshtml', '.csproj' } } |
        Where-Object { $_.FullName -notmatch '\\(obj|bin|Locales)\\' } |
        ForEach-Object { [IO.File]::ReadAllText($_.FullName) }) -join "`n"

    # NewLife.Cube 通过 <Compile Include="..\NewLife.CubeNC\..."> 链接共享源码，
    # 其中的 L.T 调用同样会命中词表；孤儿键检测必须把这些文件算进来（2026-10-20）
    $linkProject = Join-Path $root "NewLife.Cube\NewLife.Cube.csproj"
    if (Test-Path $linkProject) {
        $csproj = [IO.File]::ReadAllText($linkProject)
        $linkRoot = Join-Path $root "NewLife.CubeNC"
        $linkedText = [regex]::Matches($csproj, 'Include="\.\.\\NewLife\.CubeNC\\([^"]+)"') |
            ForEach-Object { Join-Path $linkRoot ($_.Groups[1].Value.Replace('\', '/')) } |
            Where-Object { Test-Path $_ } |
            ForEach-Object { [IO.File]::ReadAllText($_) }
        if ($linkedText) { $sourceText += "`n" + ($linkedText -join "`n") }
    }

    # 统一换行，避免多行提示词因 CRLF 与 LF 差异被判为孤儿键
    $sourceText = $sourceText.Replace("`r`n", "`n")

    foreach ($key in $reference.Entries.Keys) {
        if ($metadataKeySet.Contains($key)) { continue }

        # 源码中的字面量形式：制表符写作 \t，引号写作 \"
        $escaped = $key.Replace('\', '\\').Replace('"', '\"').Replace("`t", '\t').Replace("`r", '\r').Replace("`n", '\n')
        if (!$sourceText.Contains($key) -and !$sourceText.Contains($escaped)) {
            $orphans += $key
        }
    }
    foreach ($o in $orphans) { $allErrors += "孤儿键（源码中找不到）：$o" }
}

foreach ($e in $allErrors) { Write-Host $e -ForegroundColor Red }
foreach ($w in $allWarnings) { Write-Host $w -ForegroundColor Yellow }

if ($allErrors.Count -gt 0) {
    foreach ($o in $orphans) { Write-Host "孤儿键（源码中找不到）：$o" -ForegroundColor Red }
}

$count = (Get-ChildItem $localeDir -Filter *.yml).Count
if ($allErrors.Count -gt 0) {
    Write-Host "多语言词表校验未通过：$count 个语言，$($allErrors.Count) 个错误" -ForegroundColor Red
    exit 1
}

Write-Host "多语言词表校验通过：$count 个语言，基准 $($reference.Entries.Keys.Count) 条" -ForegroundColor Green
