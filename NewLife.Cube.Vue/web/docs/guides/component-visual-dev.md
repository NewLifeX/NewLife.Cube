# 组件可视化开发：开发期预览 vs 视觉回归基线

> 本指南讲清一件事：**「正在开发时看效果」和「开发完成后防回归」是两种不同需求，用不同手段，不要混为一谈。**
> 相关概念见 [standards/testing-standard.md](../standards/testing-standard.md)（测试策略）。

## 核心概念

### Story（故事）是什么

Story 是组件的**一个渲染状态变体**——即"给组件传一组特定 props，看它渲染成什么样"。每个 story 回答了同一个问题：**组件在某个特定 props 组合下应该长什么样？**

```ts
// core/components/LovSelectTable/LovSelectTable.story.ts
export const stories = [
  { id: 'LovSelectTable/SingleOpen', component: LovSelectTable, props: { ...base, multiple: false } },
  { id: 'LovSelectTable/MultiOpen',   component: LovSelectTable, props: { ...base, multiple: true } },
  { id: 'LovSelectTable/SingleEcho',  component: LovSelectTable, props: { ...base, multiple: false, modelValue: 1 } },
  { id: 'LovSelectTable/MultiEcho',   component: LovSelectTable, props: { ...base, multiple: true, modelValue: [1, 2] } },
];
```

Story 是**可编程、可复现、可对比**的组件状态快照。手动开浏览器每次都要点击操作才能到达某个状态，而 Story 可以直接"空降"到那个状态，省去所有交互步骤。

### 截图对比（Visual Regression Testing）的原理

Playwright 的 `toHaveScreenshot()` 做**像素级 Diff**：

```
  ┌──────────────┐     ┌──────────────┐
  │  基线截图      │     │  当前截图     │
  │ (baseline)    │     │  (actual)    │
  │ 上次确认的版本  │     │ 本次运行生成  │
  └──────┬───────┘     └──────┬───────┘
         │                    │
         ▼                    ▼
      ┌──────────────────────────┐
      │   像素级逐像素比较         │
      └──────────┬───────────────┘
                 │
        ┌────────┴────────┐
        ▼                 ▼
    ┌────────┐      ┌──────────┐
    │ 一致    │      │ 不一致    │
    │ 测试通过 │      │ 测试失败  │
    └────────┘      │ 生成 diff │
                    │ 图片高亮  │
                    │ 差异区域  │
                    └──────────┘
```

**判断标准不是"语义等价"，而是"像素精确"**。任何肉眼可见的变化（颜色、间距、位置、文字）都会被检出。

### Vitest 结构快照 vs Playwright 截图对比

|            | Vitest 结构快照 (`toMatchSnapshot`) | Playwright 截图对比 (`toHaveScreenshot`) |
| ---------- | ----------------------------------- | ---------------------------------------- |
| 运行环境   | Node.js (jsdom)                     | 真实浏览器 (Chromium)                    |
| 渲染引擎   | 无                                  | 有                                       |
| 验证维度   | HTML 字符串、数据结构               | 像素、布局、颜色、字体                   |
| 能发现什么 | "有没有这个 DOM 元素"               | "这个元素看起来对不对"                   |
| 速度       | 毫秒级                              | 秒级                                     |
| 能截图？   | ❌ 不能                              | ✅                                        |

**两者互补，各司其职：**
- Vitest 结构快照：验证逻辑（"按钮文本是否正确"），速度快
- Playwright 截图：验证视觉（"按钮在左上角、颜色正确、间距 12px"），真实渲染

### Gallery 模式是什么

本项目采用 **Gallery 模式**（自建 Storybook 轻量替代）来实现组件截图对比，而不是 `@playwright/experimental-ct-vue` 官方包。核心思路：

```
*.story.ts → ct/stories.ts (Vite glob 自动收集) → ct/gallery.html → Playwright 截图
```

这与 Storybook、Histoire、Ladle 等业界成熟方案思路一致，但去掉了庞大的依赖。Gallery 通过 `ct/vite.config.ts` 独立启动（端口 5190），mock 了后端 API，组件无需真实后端即可渲染。

### 文件命名规范

按后缀隔离，避免 Vitest 和 Playwright 抓取同一个文件导致冲突：

| 工具          | 文件名后缀                | 示例                        |
| ------------- | ------------------------- | --------------------------- |
| Vitest        | `*.spec.ts` / `*.test.ts` | `LovSelectTable.test.ts`    |
| Playwright CT | `*.ct.spec.ts`            | `LovSelectTable.ct.spec.ts` |
| Story 定义    | `*.story.ts`              | `LovSelectTable.story.ts`   |

## 一句话区分

- **开发中（actively building）**：你要的是「看一眼现在长啥样」，靠 hot reload 看浏览器即可，**不需要基线、不需要截图对比**。
- **开发完成（done）**：你要的是「保证以后没人改坏它」，这时才需要建立截图基线做回归对比。

---

## 阶段一：开发中 —— 直接看浏览器，不需要基线

你现在的场景是**正在写组件**，不是做回归保护。你需要的不是「对比差异」，而是「看一眼渲染结果」。

**1. 启动 gallery（端口 5190，Vite hot reload 自动生效）**

```bash
# 真实命令
vite --config ct/vite.config.ts --port 5190
# 便捷别名（见文末「待补充脚本」）：pnpm ct:server
```

**2. 浏览器打开**

```
http://127.0.0.1:5190/gallery.html
```

**3. 控制台挂载某个 story 变体**

```js
window.mountStory('LovSelectTable/SingleOpen')
window.setStoryProps({ dialogVisible: true })   // 打开弹窗、触发数据加载（与真实交互一致）
```

这时你修改 `LovSelectTable.vue`，Vite hot reload 自动刷新，你肉眼就能看到：

- 改 CSS → 立即看到布局变化
- 改 template → 立即看到 DOM 变化
- 加「已选 N 项」→ 立即看到左下角有没有出现

**不需要截图，不需要基线，直接看浏览器就行。** CT 配置（`playwright-ct.config.ts`）已把 `lo-vapi/request` 和 `virtual:@newlifex/cube-vue-*` 桩掉，gallery 不依赖后端即可渲染。

> **前置依赖**：阶段二 / 三的 `test:ct` / `test:ct:dev` 依赖本机已安装 **Google Chrome**（`playwright-ct.config.ts` 用 `channel: 'chrome'`）。本机未装 Chrome 时这些命令会失败；CI 中改用 `playwright install chromium` 并将配置 `channel` 改为 `chromium`。

---

## 阶段二：开发中想确认细节 / 留证据 —— 截图存档但不对比

场景：你不想一直盯着浏览器，或者想把当前效果存成图片仔细端详。

**有头浏览器 + 截图，但「强行覆盖基线」所以永不失败：**

```bash
# 真实命令（headed：肉眼可见渲染窗口；--update-snapshots：只拍照存档，不对比）
playwright test --config playwright-ct.config.ts --project=chromium-ct --headed --update-snapshots
# 便捷别名（见文末）：pnpm test:ct:dev
```

它做了三件事：

1. 自动启动 gallery（端口 5190，由 CT 配置的 `webServer` 托管）
2. 打开 Chromium 窗口，你**肉眼可见**组件渲染
3. 截图保存到磁盘（默认落在 `core/components/**/__screenshots__/` 或测试旁），你可以打开 PNG 端详

因为 `--update-snapshots`，它**永远不失败**，只是「拍照存档」。你根本不需要在意「基线」这个概念——它只是把截图存下来给你看。

```bash
# 开发循环
改代码 → pnpm test:ct:dev → 看浏览器窗口 → 不满意 → 继续改 → 再来一次
```

---

## 阶段三：开发完成，要保护 —— 这时才需要基线

组件做完了、效果确认满意，你要确保以后有人改坏它时能被发现。

```bash
# 1. 把当前截图确立为「标准答案」（首次运行也会自动生成基线并通过）
playwright test --config playwright-ct.config.ts --update-snapshots
# 便捷别名：pnpm test:ct:update

# 2. 以后任何人改了组件，跑这个做像素级 diff，有差异就失败
playwright test --config playwright-ct.config.ts
# 便捷别名：pnpm test:ct

# 3. 如果改动是有意的（确认新效果更对），重新确立基线
pnpm test:ct:update   # 覆盖旧基线
```

基线由 Playwright 的 `toHaveScreenshot()` 生成；首次运行生成基线并通过，之后每次做像素 diff，回归即失败。

---

## 总结：你该用哪个？

| 阶段                 | 你需要什么             | 用什么命令                                                                                           | 基线？         |
| -------------------- | ---------------------- | ---------------------------------------------------------------------------------------------------- | -------------- |
| **正在开发**         | 看渲染效果，hot reload | `vite --config ct/vite.config.ts --port 5190` + 浏览器                                               | 不需要         |
| **开发中想确认细节** | 截个图看看             | `playwright test --config playwright-ct.config.ts --project=chromium-ct --headed --update-snapshots` | 临时存，不在乎 |
| **开发完成，想保护** | 防止以后被改坏         | `test:ct:update` 确立基线 → 以后 `test:ct` 对比                                                      | 需要           |

**你当前在开发阶段，只需要 `vite --config ct/vite.config.ts --port 5190` 打开浏览器看效果就够了。** 截图对比是「确保不会再变坏」的工具，不是「看当前长什么样」的工具。

---

## 已注册脚本（package.json）

以下 4 个别名已在 `web/package.json` 注册，可直接 `pnpm <别名>` 使用（底层命令均已验证存在）：

```json
{
  "ct:server": "vite --config ct/vite.config.ts --port 5190",
  "test:ct": "playwright test --config playwright-ct.config.ts",
  "test:ct:update": "playwright test --config playwright-ct.config.ts --update-snapshots",
  "test:ct:dev": "playwright test --config playwright-ct.config.ts --project=chromium-ct --headed --update-snapshots"
}
```

> `test:ct` / `test:ct:dev` / `test:ct:update` 通过 CT 配置的 `webServer` 自动拉起 gallery（端口 5190，`reuseExistingServer: !process.env.CI`）；`ct:server` 是供你手动开浏览器预览的独立入口。

---

## 与测试规范的关系

- **gallery 预览（阶段一/二）是本地开发辅助，不进 CI**；`test:ct` 视觉回归（阶段三）可选接入 CI 作为组件级防回归卡点。
- **本指南的 `toHaveScreenshot()` 视觉回归 ≠ `testing-standard.md` 里「禁止滥用 `toMatchSnapshot()`」**。后者指 Vitest 组件测试里对动态类名/样式的脆弱结构快照；这里是用 Playwright 像素基线做**有意、可控**的视觉回归，二者不冲突。
- 视觉回归同样遵守 [standards/ui-spec.md](../standards/ui-spec.md)：截图对比的是真实渲染，若发现颜色非 `--el-*` 来源，先回 UI 规范修正，再更新基线。
