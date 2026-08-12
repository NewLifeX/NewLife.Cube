# 组件可视化开发：开发期预览 vs 视觉回归基线

> 本指南讲清一件事：**「正在开发时看效果」和「开发完成后防回归」是两种不同需求，用不同手段，不要混为一谈。**
> 具体命令、配置、Gallery 挂载契约、Story / CT 编写等**操作细节**，一律见技能 `vue-component-visual-loop`（`skills/vue-component-visual-loop/`，references：`ct-environment-setup.md` 搭建、`ct-gallery.md` 使用）。本文只规定**规范、流程与达成效果**。

## 一、核心规范（概念）

### Story 是什么
Story 是组件的**一个渲染状态变体**——"给组件传一组特定 props，看它渲染成什么样"。它是可编程、可复现、可对比的组件状态快照，免去手动点击到达某个状态。

### 截图对比（CT）的原理
Playwright 的 `toHaveScreenshot()` 做**像素级 Diff**：将「基线截图」与「本次截图」逐像素比较，**判断标准是"像素精确"而非"语义等价"**——任何肉眼可见的变化（颜色、间距、位置、文字）都会被检出。

| 维度 | Vitest 结构快照 (`toMatchSnapshot`) | Playwright 截图对比 (`toHaveScreenshot`) |
| --- | --- | --- |
| 运行环境 | Node.js (jsdom) | 真实浏览器 (Chromium) |
| 验证维度 | HTML 字符串、数据结构 | 像素、布局、颜色、字体 |
| 能发现什么 | "有没有这个 DOM 元素" | "这个元素看起来对不对" |
| 速度 | 毫秒级 | 秒级 |

**两者互补**：Vitest 验证逻辑（速度快），Playwright 验证视觉（真实渲染）。

### Gallery 模式（本项目采用的预览/测试架构）
本项目采用**自建 Gallery 模式**（轻量 Storybook 替代，零额外依赖），而不是 `@playwright/experimental-ct-vue` 官方包。预览与测试共用同一份 story 与同一份 Vite 配置，做到**所见即所测**。搭建与架构细节见技能 `references/ct-environment-setup.md`、`ct-gallery.md`。

### 文件命名规范（按后缀隔离，避免工具互抓）
| 工具 | 文件名后缀 | 示例 |
| --- | --- | --- |
| Vitest | `*.spec.ts` / `*.test.ts` | `LovSelectTable.spec.ts` |
| Playwright CT | `*.ct.spec.ts` | `LovSelectTable.ct.spec.ts` |
| Story 定义 | `*.story.ts` | `LovSelectTable.story.ts` |

## 二、组件 README 与故事状态标注约定（规范）

为保证「测试、验证截图与功能一一对应」，CT 套件约定三条：

1. **故事注明测试状态**：每个 story 上方用 `// 测试状态：…` 注释标明该变体要验证的运行时状态。CT 测试据此命名，二者不脱节。
2. **功能 ↔ Story ↔ 测试 对应表**：每个被 CT 覆盖的组件目录必须含 `README.md`，列出「功能清单」与三列表，使功能、故事、截图测试可追溯、不遗漏（模板见技能 `references/component-readme-template.md`）。
3. **CT 不只截图、还要断言**：关键状态必须先用 `expect` 断言（如回显文本、翻页后统计数）再 `toHaveScreenshot()`，避免把错误状态固化成基线。

## 三、使用流程（三阶段）

**一句话区分**：开发中你要「看一眼现在长啥样」（hot reload 即可，不需要基线）；开发完成你要「保证以后没人改坏它」（这时才需要基线做回归）。

| 阶段 | 你需要什么 | 用什么（命令名） | 基线？ | 达成效果 |
| --- | --- | --- | --- | --- |
| **正在开发** | 看渲染效果，hot reload | `ct:server` + 浏览器 | 不需要 | 改 `.vue` 即 HMR 刷新，秒看布局/CSS/DOM 变化 |
| **开发中想确认/留证据** | 截个图仔细端详 | `test:ct:dev` | 临时存，不对比 | 有头浏览器肉眼可见渲染，截图存档不失败 |
| **开发完成，要保护** | 防以后被改坏 | `test:ct:update` 锁基线 → 以后 `test:ct` 对比 | 需要 | 像素级回归，任何视觉漂移即失败、阻断合并 |

> 命令的**具体调用方式、Gallery 深链（`?story=`）、侧栏浏览、各脚本行为**见技能 `references/ct-gallery.md`（使用）与 `ct-environment-setup.md`（搭建/脚本清单）。

## 四、与测试规范的关系（规范）

- Gallery 预览（阶段一/二）是**本地开发辅助，不进 CI**；`test:ct` 视觉回归（阶段三）可选接入 CI 作组件级防回归卡点。
- 本指南的 `toHaveScreenshot()` 视觉回归 **≠** 测试规范里「禁止滥用 `toMatchSnapshot()`」：后者指 Vitest 对动态类名/样式的脆弱结构快照；这里是用 Playwright 像素基线做**有意、可控**的视觉回归，二者不冲突。
- 视觉回归同样遵守 `standards/ui-spec.md`：若发现颜色非 `--el-*` 来源，先回 UI 规范修正，再更新基线。
- 测试分层、Mock 策略（不用 MSW）、覆盖率门槛见 `standards/testing-standard.md` 与总纲 `standards/frontend-testable-development.md`。
