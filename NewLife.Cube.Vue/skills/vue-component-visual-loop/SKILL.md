---
name: vue-component-visual-loop
description: Vue 组件开发的「视觉快速迭代闭环」：Gallery 手动预览（?story= 深链 / 无参数侧栏，带 HMR）+ Vitest 逻辑回归 + Playwright CT 像素级截图。提供手动浏览器预览与 CT 测试编写规范，让"改代码→截图→调整→再截图"在分钟级闭环中完成。
---

# 组件视觉快速迭代闭环（vue-component-visual-loop）

> **适用场景**：开发 Vue 组件时，遵循「改代码 → 逻辑回归 → 视觉预览 → 截图锁定 → 循环」的闭环。
>
> **核心原则**：**组件只负责 UI 和事件**，逻辑抽到纯函数 / Hook。Vitest 守住逻辑不退化，Playwright CT 守住视觉不跑偏——两者不是独立路径，而是同一循环的两个阶段。
>
> **闭环为何快**：不必启动完整 SPA / 不必手搓演示页——Gallery 通过 Vite 直接渲染真实 `.vue` 组件，改完即 HMR 热更新，浏览器里秒看效果；逻辑改动用 Vitest 秒级回归，视觉改动用 CT 像素级兜底。
>
> **前置约束**：关注点分离（`*.logic.ts` 纯函数、`<script setup>` 精简）与测试策略，详见项目 `AGENTS.md` / 测试规范。

## 一、闭环总览（渐进式开发循环）

```
┌───────────────────────────────────────────────────────────────┐
│  1. 写代码                                                      │
│     • 组件只放 template + <script setup>（精简）                 │
│     • 业务逻辑抽到同目录 *.logic.ts 纯函数 / use*.ts Hook        │
│     • 组件通过 emit 上报事件，父级或 logic 层处理                │
└───────────────────────┬───────────────────────────────────────┘
                        ▼
┌───────────────────────────────────────────────────────────────┐
│  2. test:unit  ← Vitest 逻辑回归（~5s，快速反馈）               │
│     ❌ 失败 → 分析断言 → 回第 1 步   ✅ 通过 → 进视觉阶段        │
└───────────────────────┬───────────────────────────────────────┘
                        ▼
┌───────────────────────────────────────────────────────────────┐
│  3. ct:server  ← 手动浏览器预览（HMR，分钟级视觉迭代）          │
│     浏览器开根目录 /（无参数=侧栏 / ?story=单故事，index.html 即 gallery 宿主） │
│     ❌ 不满意 → 改 .vue（HMR 自动刷新）→ 再看   ✅ 满意 → 锁基线 │
└───────────────────────┬───────────────────────────────────────┘
                        ▼
┌───────────────────────────────────────────────────────────────┐
│  4. test:ct:dev  ← Playwright CT 截图（有头浏览器）             │
│     ❌ 不满意 → 回第 1 步   ✅ 满意 → 确立基线                  │
└───────────────────────┬───────────────────────────────────────┘
                        ▼
┌───────────────────────────────────────────────────────────────┐
│  5. test:ct:update  ← 确立基线，锁定当前效果                    │
│     以后 test:ct 做回归保护，防止被改坏                          │
└───────────────────────────────────────────────────────────────┘
```

---

## 二、文件清单与职责

为组件 `<ComponentName>` 新增验证闭环，需要以下文件：

```
core/components/<ComponentName>/
├── <ComponentName>.vue        # 组件实现（只负责 UI + emit）
├── <ComponentName>.logic.ts   # [可选] 业务逻辑纯函数（或 use<Name>.ts 组合式 Hook）
├── <ComponentName>.spec.ts    # Vitest 回归测试（逻辑 + emit + 边界）
├── <ComponentName>.story.ts   # Story 定义（声明 props 变体供 gallery 渲染）
└── <ComponentName>.ct.spec.ts # Playwright CT 截图测试（视觉回归）
```

| 文件           | 工具                 | 职责                                     | 运行命令                      |
| -------------- | -------------------- | ---------------------------------------- | ----------------------------- |
| `*.spec.ts`    | Vitest + jsdom       | 逻辑回归：纯函数、emit、props 渲染、边界  | `pnpm test:unit`              |
| `*.story.ts`   | Gallery              | 声明组件 props 变体                      | 被 `*.ct.spec.ts` 引用        |
| `*.ct.spec.ts` | Playwright + Gallery | 视觉回归：像素级截图对比                 | `pnpm test:ct[:dev\|:update]` |

> 文件名后缀做了隔离：`*.spec.ts` 归 Vitest，`*.ct.spec.ts` 归 Playwright，`*.story.ts` 归 Gallery，互不冲突。

---

## 二·五、新增组件端到端清单（通用步骤）

为任意 `<ComponentName>` 跑通整条闭环，按序执行（细节仍在各阶段 references，此处只给时序导航）：

1. **填 Part A（开发前）**：按 `references/component-readme-template.md` 产出 §1~§7，**重点补齐「能力维度矩阵」（防漏功能）与「不变量红线」（防假绿）**，评审通过再写代码（§七铁律）。详见 §十 测试完备性方法论。
2. **写组件**：`<ComponentName>.vue`（薄壳 + emit）+ 必要的 `<ComponentName>.logic.ts`（纯函数 / `use*.ts` Hook）。关注点分离见 §五。
3. **写 Vitest**：`<ComponentName>.spec.ts`，Mock 用别名导入（§三 / `references/vitest-strategy.md`），`pnpm test:unit` 全绿。
4. **写 Story**：`<ComponentName>.story.ts` 声明 props 变体，`ct/stories.ts` 显式静态导入（§四 / `references/ct-gallery.md` §三）。
5. **手动视觉迭代**：`pnpm ct:server` 开画廊，HMR 反复调；`?story=<ComponentName>/<Variant>` 深链或侧栏浏览。
6. **写 CT 截图**：`<ComponentName>.ct.spec.ts` 用 `?story=` 一步挂载 + 等稳定后截图（§四 / `references/ct-gallery.md` §四~§五）。
7. **锁基线**：`pnpm test:ct:dev` 确认 → `pnpm test:ct:update` 确立基线；之后 `pnpm test:ct` 回归。
8. **类人验证**：按 README §4/§10 验证流程逐项点击（脚本或 MCP，§八 / `references/mcp-playwright.md`），输出 PASS/FAIL 并截图。
9. **沉淀 Part B（开发中/后）**：§8~§12 随闭环补全（§七）。

### 常见坑排查表

| 现象 | 根因 | 处置 |
| --- | --- | --- |
| Gallery / CT 里组件空白或显示原始值 | 组件内用相对路径导入被 mock 的模块，绕过别名桩打到真实后端 | 改为项目别名导入（§三 / `references/vitest-strategy.md` §二） |
| CT 基线频繁抖动失败 | 截图前未等 transition 收尾 | `waitForSelector` → `waitForFunction(visibility==visible)` → `waitForTimeout(400)`（§四 / §八） |
| 侧栏污染 CT 基线 | CT 未带 `?story=` | CT 永远带 `?story=` 深链（§四） |
| 改逻辑后 UI 测试要跟着改 | 逻辑写在 `<script setup>` 里 | 抽到 `*.logic.ts` 纯函数（§五） |
| 本机 CT 命令失败 | 缺对应浏览器 / `channel` 不符 | `playwright install <browser>` 并校验配置 `channel`（§六） |

---

## 三、阶段一：Vitest 逻辑回归

**要点**：纯函数输入输出、组件 emit 触发、props 渲染、边界条件（空数据 / 加载中 / 错误态）。

- **运行 / Mock 策略 / mount 步骤 / 别名优先纪律** → 详见 `references/vitest-strategy.md`
- 关键纪律：**组件内部凡涉及被 mock 的模块，必须用项目别名导入，不要用相对路径**（相对路径会绕过 Vite 别名桩，在 Gallery / CT 里打到真实后端，导致组件空白或返回原始值）。

---

## 四、阶段二：Playwright CT 截图验证

**架构**：自建 Gallery 模式（轻量 Storybook 替代），而非官方 CT 包。预览与测试共用同一套 story / 同一份 Vite 配置，所见即所测。

- **Gallery 架构、开发循环命令（ct:server / ?story= 深链 / 侧栏）、Story 编写、CT 测试编写（?story= 一步挂载 + 稳定后再截图纪律）、为何不用 Puppeteer 兜底** → 详见 `references/ct-gallery.md`

> `?story=<id>` 是 URL 查询参数，不是 server CLI 参数；CT 永远带 `?story=`，因此侧栏不会污染 CT 基线。

---

## 五、关注点分离（核心）

**不把逻辑放进组件，测试就会变得困难。** 如果 `<script setup>` 里直接写了 `fetch()`、`new Date()`、复杂 `filter/map`、拼装数据，Vitest 就需要 mock 更多东西，且逻辑与 UI 耦合，改逻辑时测试也得改。

```
❌ 反模式：组件内嵌逻辑
──────────────────────────────────────
const formatted = computed(() => props.data.map(it => ({
  ...it, statusText: it.status === 'active' ? '启用' : '禁用'
})))

✅ 正确模式：抽到 *.logic.ts
──────────────────────────────────────
// user.logic.ts ── 纯函数，极易测试
export function formatList(data: T[], dict: Record<number, string>) {
  return data.map(it => ({ ...it, statusText: dict[it.status] ?? '未知' }));
}
// index.vue ── 只负责渲染
const formatted = computed(() => formatList(props.data, statusDict));
```

| 代码位置            | 测试工具      | 测什么                |
| ------------------- | ------------- | --------------------- |
| `*.logic.ts` 纯函数 | Vitest        | 输入输出、边界、异常  |
| `*.vue` template    | Vitest        | props 渲染、emit 事件 |
| `*.vue` 样式/布局   | Playwright CT | 截图对比              |

---

## 六、命令速查

```bash
# 在 <web-root> 下执行

# === 开发循环（视觉快速迭代）===
pnpm ct:server          # 手动浏览器预览（HMR；无参数=侧栏，?story=单故事）
pnpm test:ct:dev        # 有头截图存档（不对比，开发中确认用）

# === Vitest 逻辑回归 ===
pnpm test:unit          # 全部 Vitest 测试
pnpm exec vitest run --config vitest.config.unit.ts path/to/xxx.spec.ts  # 单文件

# === CT 截图回归 ===
pnpm test:ct            # 无头模式，截图对比基线
pnpm test:ct:update     # 更新基线截图（确认效果后）
```

> 前置依赖：CT 命令依赖本机浏览器（通常 `channel: 'chrome'` 或项目配置的浏览器）。本机无对应浏览器时这些命令会失败；CI 中按需改为 `playwright install <browser>` 并调整配置 `channel`。

### CI 与基线管理要点

- **CI 跑 CT**：CT 依赖本机浏览器，CI 中需先 `playwright install <browser>` 并确认配置 `channel` 与安装的浏览器一致；基线对比失败应阻断合并。
- **基线管理**：基线截图是"视觉契约"，更新需人工 review（PR 里贴 before/after 或走预览 review），避免无意识覆盖；基线漂移时回滚到上一版快照即可。
- **产物存档**：`test-results/` 与 `ct/verify-shots/` 均为瞬态，CI 仅保留失败时的产物供排查，常态不提交。

---

## 七、组件 README 模板（开发前必填 + 闭环沉淀）

每个组件目录（`core/components/<Name>/`）都应有一份 `README.md`，作为「设计 ↔ 功能 ↔ Story ↔ 测试 ↔ 验证」的唯一事实源。**完整模板见 `references/component-readme-template.md`**。

> **铁律：开始编码前必须先填 Part A（开发前必填）并经评审，再动手写 `.vue`。** Part A 是验收标准与后续验证的输入，避免"边写边想、需求不清"导致返工。

**Part A — 开发前必填（动手前产出）**
1. 功能需求（FR）：角色 / 功能 / 价值，每条可验收
2. **能力维度矩阵（Completeness Matrix）**：枚举维度画矩阵，每个可达格 = 必实现且必测的能力（防漏功能，详见 §十）
3. 设计：定位、分层契约、交互模式、视觉要点
4. 流程：用户操作流程、关键数据流、状态机
5. 功能验证列表（Verification Checklist / 故事清单）：源于 FR，逐条可勾选，是完成定义也是类人验证输入（= 最终走查闸门）
6. **不变量红线（Invariants）**：开发前列出"容易破、破了用户立刻可见"的负向约束，每条必有守卫测试（防假绿，详见 §十）
7. 数据契约：依赖接口、字段与枚举映射、Mock 约定（确保不请求真实后端）
8. 边界与异常：空 / 加载 / 错误 / 分页边界 / 超长 / 超大列表
9. 待确认项（Open Questions）：开发前清零

**Part B — 开发中 / 后沉淀（闭环产出）**
8. 功能 ↔ Story ↔ 测试 对应表（迭代导航图）
9. 视觉基线说明（哪些状态需锁 CT 基线）
10. 验证流程（类人点击清单，复用 §4）
11. 问题原因分析（仅 BUG 修复）
12. 变更记录（Changelog）

> 该 README 把"人肉确认"固化为可复跑的验证清单（§4 + §10），并让"改代码→看 Vitest→按 Story 看视觉→CT 锁基线"的导航图始终成立。

---

## 八、类人点击验证流程（真人式点击，补 CT 之不足）

自动化 CT（`*.ct.spec.ts`）是**像素级比对**：布局没变就过，但它只证明"长这样"，不证明"点得动、数据对"。为此在闭环末尾加一道**类人点击验证**——启动 `ct:server` 后用 Playwright 按组件 README 的「验证流程」逐项点击、翻页、断言文字与勾选数，输出每功能 `PASS/FAIL` 报告并截图，等价于一个人在浏览器里逐一确认功能真的可用。

**两种执行方式**：

1. **脚本方式**：按 README「验证流程」表编写一个 Playwright 脚本，逐项 `click` / 翻页 / `toHaveText` / `toHaveCount` 断言 + 截图。确定性高、可 CI，不需额外依赖；
2. **带 MCP Playwright 的智能体**（子代理 / 专家）：用 `@playwright/mcp` 工具直接驱动浏览器，并能**读取截图做视觉核对**，更灵活（注册与操作清单见 `references/mcp-playwright.md`）。

> 无论哪种方式，都复用同一套 `?story=` 深链 + `ct/mocks/` 数据，环境一致，不会"演示页和真实组件两套实现"。

**纪律**（与 CT 共用）：

- **等挂载稳定**：`waitForSelector` → `waitForFunction(visibility==visible)` → `waitForTimeout(400)`，避免动画抖动误判；
- **断言用文字而非像素**：类人验证的价值正是确认"数据对"，优先 `toHaveText` / `toHaveCount`；
- **选择器型组件不自动开弹窗**：需显式点击展开 / 点按钮开弹窗，以验证中间态。

---

## 十、测试完备性方法论（理论指导：防漏功能 + 防假绿）

> 深度展开见 `references/testing-completeness-methodology.md`。本技能之所以能"不漏功能、测试测得出问题、按故事逐一验收"，靠的是把成熟测试理论落到 README 三块载体 + 四阶段流程。

**为什么需要它**：组件开发两类典型失败——① 漏功能（只做单选忘多选、列表回显显示数字）；② 假绿（所有测试全绿但线上有 bug）。两者都靠下面三块消除。

**README 三大理论载体（开发前必填）**
1. **能力维度矩阵（Completeness Matrix）**——枚举"形态 × 来源 × 状态 × 数据态 × 交互"等维度画矩阵，每个可达格 = 一个必须实现且测试的能力；空白格 = 漏了或不支持。→ 防漏功能。
2. **不变量红线（Invariants）**——开发前列出"容易破、破了用户立刻可见"的负向约束（expect 绝不允许…），每条必有守卫断言测试。→ 防假绿。
3. **故事清单（§4 验证列表）**——每个故事 = `?story=` 深链 + 操作步骤 + 期望；开发完按此逐项走查全 PASS 才算完成。→ 最终交付闸门。

**四阶段流程**：Phase 0 写 README（矩阵+不变量+故事）→ Phase 1 派测试（故事→CT 语义断言；不变量→守卫测试；mock 掉的核心逻辑额外生成"真实契约测试+桩忠实性测试"双测）→ Phase 2 实现（红转绿、逻辑抽 `*.logic.ts`、别名导入）→ Phase 3 故事走查闸门（全 PASS 交付）。

**防假绿纪律（本会话真实教训）**：① 截图只锁视觉不锁语义，用户可见文本必用 `toContainText` 断言；② 绝不在缺陷态更新截图基线（否则固化 bug）；③ mock 掉核心逻辑必须"真实模块契约测试 + 桩忠实性测试"双测，缺一即掩盖缺陷；④ 测试须先确认"能因正确原因变红"。

> 真实案例：LovSelect 曾"LIST 回显显示数字"而所有测试全绿，正是四层原因全中（桩不忠实 / 只截图 / 单测 mock 掉核心逻辑 / 缺语义断言）。修复后补的双测即本方法论落地，见 `core/components/LovSelect/README.md` Part C。

---

## 十一、参考文档

| 文档                                                          | 内容                                             |
| ------------------------------------------------------------- | ------------------------------------------------ |
| `references/ct-environment-setup.md`                          | CT 环境从零搭建（目录/配置文件/脚本/gitignore/纪律，新项目复刻） |
| `references/vitest-strategy.md`                               | Vitest Mock 策略、mount 步骤、别名优先纪律        |
| `references/ct-gallery.md`                                    | Gallery 架构、开发循环命令、Story / CT 编写、Puppeteer 废弃 |
| `references/mcp-playwright.md`                                | MCP Playwright 注册（按环境配置）、操作清单、读截图核对 |
| `references/component-readme-template.md`                     | 组件 README 模板（Part A 开发前必填 + Part B 闭环沉淀） |
| `references/testing-completeness-methodology.md`             | 测试完备性方法论（理论根基 / 矩阵 / 不变量 / 四阶段流程 / 防假绿纪律 / 真实案例） |
| `docs/standards/testing-standard.md`                          | 测试分层、Mock 策略、覆盖率门槛、CT 架构         |
| `docs/standards/frontend-testable-development.md`             | 团队规范总纲                                     |
| `<web-root>/AGENTS.md`                                        | 硬约束（关注点分离、测试策略、命名约定）         |
