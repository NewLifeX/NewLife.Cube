# README 多语言规范

根目录的 `README.md` 与 `README.*.md` 是同一份说明文档的多语种版本。语言切换行由脚本从 `readme-languages.json` 生成，本文规定**顺序**与**维护方式**。

## 定序规则

1. **固定前三**：中文（`README.md`，权威版与 GitHub 默认落地页）→ 繁體中文（`README.zh-TW.md`，与简体同源，紧邻便于对照）→ English（通用语，非中文读者的第一选择）。
2. **其余按技术内容语种规模排序**：以 W3Techs「内容语种占比」（全球 top 1000 万站点首页）为依据，一次定序后**冻结**。
3. **新语种一律追加到末尾**，不做全局重排（同一语言的不同书写系统例外：紧跟其主语言）。

### 当前顺序

| # | 语种 | 文件 | 依据（W3Techs，2025-12-03） |
|---|------|------|------|
| 1 | 中文（简体） | `README.md` | 权威版与默认落地页，固定第一位 |
| 2 | 繁體中文 | `README.zh-TW.md` | 与简体同源，紧随简体便于对照 |
| 3 | English | `README.en.md` | 通用语，固定第三位 |
| 4 | Español | `README.es.md` | 6.0% |
| 5 | Deutsch | `README.de.md` | 5.9% |
| 6 | 日本語 | `README.ja.md` | 5.1% |
| 7 | Français | `README.fr.md` | 4.5% |
| 8 | Português | `README.pt.md` | 4.1% |
| 9 | Русский | `README.ru.md` | 3.7% |
| 10 | 한국어 | `README.ko.md` | 0.8% |
| 11 | العربية | `README.ar.md` | 0.5% |

## 为什么不按语言人口排序

- README 的读者是**开发者**，语种价值取决于「多少开发者能读」，而不是「多少人会说」。
- 人口数据逐年变化，按人口排意味着反复重排；切换行只有 10 个链接，排序的边际收益很低，重排成本（11 个文件重复编辑与评审噪声）很高。
- 固定前二可避免通用语被某个大语种挤到后面。

## 维护方式

- **单一事实源**：`readme-languages.json`（`code` / `name` / `file`，数组顺序即展示顺序）。
- **生成与校验**：`sync-readme-languages.ps1` 按配置重写各 README 的切换块；`-Check` 只校验不写入，不一致时退出码 1。
- **指标同步**：`sync-readme-metrics.ps1` 从 `readme-metrics.json` 生成 AOT/TFM/locales 徽章块；`-Check` 用于 CI。
- **CI 门禁**：`.github/workflows/readme.yml` 在 README、配置或脚本变动时执行 `-Check`。
- 各 README 只有标记块之间的内容由脚本生成，其余内容不受影响：

```markdown
<!-- lang:start -->
🌐 [中文](README.md) | [English](README.en.md) | ...
<!-- lang:end -->

<!-- metrics:start -->
[![AOT smoke](...)](Doc/AOT兼容性分析.md)
[![TFM](...)](https://www.nuget.org/packages/NewLife.Cube)
[![locales](...)](Doc/多语言本地化.md)
<!-- metrics:end -->
```

## 新增一种语言

1. 翻译整篇 README，文件名用 `README.{BCP 47 语言标签}.md`。
2. 在 `readme-languages.json` 的 `languages` 数组**末尾**追加 `{ "code", "name", "file" }`，`name` 用本族语名称。
3. 在正文开头补上与其它版本一致的说明：「本翻译可能滞后，以中文版为准」。
4. 运行 `pwsh -File sync-readme-languages.ps1` 生成切换行，再运行 `-Check` 确认。
5. 根目录新增了 README 却没登记到配置，脚本会直接报错，避免切换行静默漏掉该语种。

## 徽章规则

- **必选**：License、NuGet 下载量/版本/预发布（`NewLife.Cube` 与 `NewLife.Cube.Core` 双包）、测试、AOT、平台/架构、贡献者。
- **静态数字**：AOT 冒烟场景数、TFM 数量、运行时语言数只写在 `readme-metrics.json`，由脚本生成徽章，禁止手工散落。
- 语言数量用 `locales-{n}`，不写「支持 N 种语言」之类正文数字。
- 语言名一律用本族语名称，不用英文名，也不用国旗。
- 当前页面的语种在切换行中省略自身；分隔符统一为 ` | `，行首保留 `🌐`。
