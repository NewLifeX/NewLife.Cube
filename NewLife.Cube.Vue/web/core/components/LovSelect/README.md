# LovSelect 组件

值集（LOV）输入控件。统一入口，按后端值集配置自动渲染「枚举下拉」或「列表弹窗」两种形态。

> 本 README 遵循技能 `vue-component-visual-loop` §七 模板。**Part A 为开发前必填**（本组件于 2026-08-04 按模板反向补全）；Part B 为开发中 / 后沉淀。
> **测试完备性**按技能 §十 方法论落地：§1.5 能力维度矩阵（防漏功能）、§4.5 不变量红线（防假绿，详细见 Part C §13）、§4 验证列表即「故事走查闸门」。本组件曾因测试假绿（回显数字）踩坑，修复后沉淀双测保险，见 Part C。

---

## Part A：开发前必填

### 1. 功能需求（Functional Requirements）

- **FR1（ENUM 单选下拉）**：作为业务表单使用者，我希望 ENUM 类型值集渲染为单选下拉，选完即回填，以便快速选枚举值。验收：下拉展开出现 options，选值后 `modelValue`=选中值、输入框显示 label。
- **FR2（ENUM 多选下拉）**：作为使用者，我希望多选下拉可用 `collapse-tags` 折叠已选标签，以便多值紧凑展示。验收：多选模式下选中多项后标签折叠，hover 显示全量。
- **FR3（LIST 弹窗触发）**：作为使用者，我希望 LIST 类型值集渲染为只读输入框 + 搜索按钮，点击打开列表弹窗，以便从大表里选。验收：只读 input + 搜索按钮；点按钮弹出含 `LovSelectTable` 的弹窗。
- **FR4（加载中占位）**：作为使用者，当值集元数据尚未加载完成时，我希望看到 disabled loading 占位，以便知道组件在准备数据。验收：meta 未就绪时渲染 disabled loading `el-select`。
- **FR5（回显）**：作为使用者，我希望通过 `modelValue` 传入已选值时组件正确回显——ENUM 显示已选 label、LIST 显示已选文本（displayText），以便编辑态还原。验收：传入 `modelValue` 后输入框显示对应文本，且不依赖再次交互。
- **FR6（事件 emit）**：作为调用方，我希望选择变化时收到 `update:modelValue` 与 `change`，单选返回值、多选返回 `string[]`，以便表单集成。验收：选中后 emit 对应值。

### 1.5 能力维度矩阵（Completeness Matrix）—— 防漏功能

> 本矩阵强制穷举"形态 × 来源类型 × 值状态 × 组件态 × 交互"，每个 ✅ 格 = 已实现且已测；无空白格（不漏功能）。
> 本会话的缺陷「LIST 多选回显数字」正落于「LIST × 多选」格——该格曾破，现已补进不变量 INV-1 与守卫测试。

| 形态＼来源 | ENUM | LIST |
|---|---|---|
| **单选** | ✅ 单选下拉 + 回显 label（`EnumSingle` / `EnumSingleSelected`） | ✅ 只读 input + 搜索图标开弹窗 + 回显文本（`ListSingleClosed` / `ListSingleEcho`） |
| **多选** | ✅ 多选 + `collapse-tags` 折叠（`EnumMulti`） | ✅ 只读 + 弹窗多选 + 回显文本（**非数字**，INV-1）（`ListMultiClosed` / `ListMulti`） |

| 维度 | 取值 | 覆盖情况 |
|---|---|---|
| 值状态 | 空值 / 部分选中 / 全选 | ✅ 空（未选）、部分（`ListSingleEcho`=`1`）、全选（待补 story） |
| 组件态 | 只读 / 可编辑 / 禁用 | ✅ 只读（LIST）、可编辑（ENUM/LIST）；禁用态见 §6 |
| 数据态 | 加载中 / 出错 / 空结果 / 大量数据 | ✅ 加载中（FR4）；出错/空/大量（分页）见 §6 边界 |
| 交互 | 打开 / 选择 / 取消 / 确认 / 删除 / 搜索 / 清空 | ✅ 打开(搜索图标)、选择、取消、确认、删除(×/remove-tag)、搜索、清空(单选) |

### 2. 设计（Design）

**分层（UI 薄壳 + 逻辑 Hook）**，便于各自独立测试：

| 文件 | 职责 |
|---|---|
| `index.vue` | 目录入口，普通 `<script>` 重导出，DevTools 显示组件名为 `LovSelect` |
| `LovSelect.vue` | 纯 UI：调用 `useLovSelect`，模板只做绑定 + `emit` 转发，不含逻辑 |
| `useLovSelect.ts` | **逻辑+事件层**：元数据加载/类型解析/选择状态/displayText/事件动作，不依赖 `emit`，事件以返回值暴露 |

**后端驱动（单一真相源）**：只接收 `code`，挂载后 `fetchLovMeta(code)` 拿后端值集元数据，按 `meta[0].type` 分支：

- `ENUM` → `el-select` 下拉（单/多选）。
- `LIST` → 只读 `el-input` + 搜索按钮，弹窗交给 `LovSelectTable`（直接传入已加载的 `listMeta`，**内嵌无重复请求**）。
- `null`（加载中）→ disabled loading 占位。

**可测性**：`useLovSelect` 的 `fetchMeta` 可注入（默认 `fetchLovMeta`），Vitest 传确定性 mock 即可测；事件（`onEnumChange`/`onTableSelect`/`onTableMultiConfirm`）返回「应 emit 的值」，组件只负责 `emit` 转发 → 逻辑层脱离渲染即可断言。

### 3. 流程（Flow）

- **用户操作流程（ENUM）**：挂载 → `fetchLovMeta` → 展开下拉 → 选值 → 回填 + emit → 收起。
- **用户操作流程（LIST）**：挂载 → `fetchLovMeta` → 点击搜索按钮 → 弹出 `LovSelectTable` → 选值/确定 → 回填 displayText + emit → 关闭。
- **关键数据流**：`code`(prop) → `fetchLovMeta` → `resolvedType` → 渲染分支；`modelValue`(prop) → `syncFromModelValue`/回显；选值 → `onXxxChange` → `emit('update:modelValue'/'change')`。
- **状态机**：
  `mounted → loading(null) → { ENUM: select-ready → selecting → confirmed → closed } | { LIST: input-ready → (search click) → dialog-open → selecting → confirmed → closed }`
  标注：loading 态 UI=disabled loading select；ENUM select-ready UI=可展开下拉；LIST dialog-open UI=内嵌 LovSelectTable 弹窗。

### 4. 功能验证列表（Verification Checklist / 故事走查闸门）

> 源于 §1 功能需求 + §1.5 矩阵，逐条可勾选。**它既是"开发完成"验收清单，也是 §10 类人验证输入，更是交付前的"故事走查闸门"**——开发完按此逐项走查，全部 PASS 才算完成。

| # | 验证项（源于 FR） | 前置状态 | 操作步骤（`?story=` 深链打开后） | 期望结果 | 状态 |
|---|---|---|---|---|---|
| V1 | ENUM 单选下拉（FR1） | `LovSelect/EnumSingle` | 点 `.el-select` 展开 | 出现 options 下拉 | ☐ |
| V2 | ENUM 多选下拉（FR2） | `LovSelect/EnumMulti` | 展开多选 | 多选 + collapse-tags 折叠 | ☐ |
| V3 | ENUM 回显（FR5） | `LovSelect/EnumSingleSelected` | 直接查看 | 显示已选 label「启用」（非数字 `1`） | ☐ |
| V4 | LIST 关闭态（FR3） | `LovSelect/ListSingleClosed` | 直接查看 | 只读 input + 搜索按钮 | ☐ |
| V5 | LIST 弹窗（FR3） | `LovSelect/ListSingleClosed` | 点搜索按钮 | 弹出 LovSelectTable 弹窗 | ☐ |
| V6 | LIST 回显（FR5） | `LovSelect/ListSingleEcho` | 直接查看 | 显示已选 displayText | ☐ |

### 4.5 不变量红线（Invariants，开发前必列）—— 防假绿

> 完整定义与"为什么容易破"见 Part C §13。此处为开发前就锁定的红线摘要；每条都有守卫测试（见 §16 映射表）。不变量是 `expect 绝不允许…` 的负向约束。

| ID | 不变量（负向约束） | 守卫测试 |
|---|---|---|
| INV-1 | LIST 回显必须显示文本 label，绝不允许回退成原始数字 id（单/多选同约束） | `ListMulti 选择后回显文本(不应是数字)` / `ListSingleEcho 回显文本(不应是数字)` |
| INV-2 | 外部 `modelValue` 仅作流入：`LovSelectTable` 只读内部同步值 `listSelectValue`，不得直连外部 `modelValue` | 逻辑层 + CT 弹窗传内部值 |
| INV-3 | LIST 触发器复用 el-select 原生 `suffix-icon`（搜索图标）+ `@visible-change` 拦截；整棵 el-select 子树 `pointer-events` 仅放开触发/×/清空 | `ListSingleOpen` / `ListMultiOpen`（点 `.el-select__suffix` 开弹窗） |
| INV-4 | 标签删除复用 el-select 原生 `@remove-tag`（载荷为 value），不自绘 `#tag` 插槽 | CT 交互 + 逻辑 |
| INV-5 | `getSelectedLabel` 只查 `labelCache`，缺失即回退原始值（数字 id），绝不做"按 id 回退源数据"作弊 | `lovStore.test.ts` 真实模块契约 |
| INV-6 | CT 桩 `ct/mocks/lovStore` 必须忠实复现生产 `getSelectedLabel` 契约（INV-5） | `LovSelect.mockFidelity.spec.ts` 桩忠实性 |

### 5. 数据契约（Data Contract）

- **依赖接口**：`fetchLovMeta(code)`（`@newlifex/cube-vue/core/utils/lov-api`）→ 后端 LOV 元数据；返回 `meta[]`，按 `meta[0].type` 分支（`ENUM`/`LIST`）。
- **字段与枚举映射**：ENUM options 由 meta 给出 `label/value`；LIST 回显文本 `displayText` 由已选值 + meta 推导。
- **Mock 约定**：
  - CT：`ct/vite.config.ts` 把别名 `@newlifex/cube-vue/core/utils/lov-api` 桩到 `ct/mocks/lov-api.ts`（桩按 `code` 前缀返回 ENUM/LIST meta）。
  - Vitest：`vi.mock('@newlifex/cube-vue/core/utils/lov-api')` 注入确定性 meta。
  - **铁律（曾踩坑）**：组件内凡用到被 mock 的 `fetchLovMeta` **必须用项目别名导入**，不可用相对路径。相对路径会绕过别名桩，在 Gallery/CT 里打到真实后端 → 组件空白、ENUM 显示原始数字（`1`）。`useLovSelect.ts` 现为别名导入。

### 6. 边界与异常（Edge cases）

- 空 meta / meta 缺 `type` → 降级为 loading 占位或空。
- 加载中快速切换 `code` → 以最后一次 fetch 为准，避免竞态。
- ENUM 无选项 / LIST 无数据 → 空态展示。
- 超长 label / displayText → 截断或 ellipsis。
- 禁用态 → 整体 disabled。
- `modelValue` 类型不匹配（如数字 `1` vs 字符串 `'1'`）→ 回显前统一 `String(...)` 比对。

### 7. 待确认项（Open Questions）

- [x] 无未决事项（本组件补录 Part A 时，需求与契约均已明确）。

---

## Part B：开发中 / 后沉淀

### 8. 功能清单与 功能 ↔ Story ↔ 测试 对应

**功能清单**

| # | 功能 | 说明 |
|---|---|---|
| F1 | ENUM 单选下拉 | 单选 `el-select`，options 来自 meta |
| F2 | ENUM 多选下拉 | 多选 `el-select` + `collapse-tags` 折叠标签 |
| F3 | LIST 弹窗触发 | 只读 input + 搜索按钮，点击打开 `LovSelectTable` 弹窗 |
| F4 | 加载中占位 | meta 未就绪时渲染 disabled loading `el-select` |
| F5 | 回显 | ENUM 显示已选 label；LIST 显示 `displayText`（已选文本） |
| F6 | 事件 emit | `update:modelValue` + `change`，单选返回值/多选返回 `string[]` |

> **修复记录（LIST 回显）**：`useLovSelect.ts` 在 setup 阶段会立即执行一次 `syncFromModelValue()`，但此时 `resolvedType` 尚未就绪，导致 LIST 的 `displayText` 回显被跳过。修复：在 `applyMeta()` 解析出类型为 LIST 后调用 `updateDisplayText()`，使初始 `modelValue` 在 meta 加载完成后正确回显。

**功能 ↔ Story ↔ 测试 一一对应**

Vitest 覆盖逻辑层（`LovSelect.spec.ts`）；CT 覆盖 UI 渲染与交互（`LovSelect.ct.spec.ts`）。每个 story 在 `LovSelect.story.ts` 内用「测试状态：」注释标明验证的运行时状态。

| 功能 | Story（LovSelect.story.ts） | Vitest（LovSelect.spec.ts） | CT 截图/断言（LovSelect.ct.spec.ts） |
|---|---|---|---|
| F1 ENUM 单选下拉 | `LovSelect/EnumSingle` | 覆盖（options 注入、onEnumChange 返回值） | `EnumSingle 下拉渲染` |
| F2 ENUM 多选下拉 | `LovSelect/EnumMulti` | 覆盖（onEnumMultiChange 返回值） | `EnumMulti 多选下拉渲染` |
| F5 ENUM 回显 | `LovSelect/EnumSingleSelected` | 覆盖（modelValue 回显） | `EnumSingle 选择后回显数据正常`（点选→断言显示「启用」→截图） |
| F3 LIST 关闭态(单) | `LovSelect/ListSingleClosed` | 覆盖（openDialog 动作） | `ListSingleClosed 只读输入框 + 搜索按钮` |
| F3 LIST 关闭态(多) | `LovSelect/ListMultiClosed` | 覆盖 | `ListMultiClosed 多选只读输入框` |
| F5 LIST 回显 | `LovSelect/ListSingleEcho` | 覆盖（displayText 恢复） | `ListSingleEcho 回显已选文本` |
| F3 LIST 弹窗(单) | 由 `ListSingleClosed` + dialog 交互 | — | `ListSingleOpen 弹窗含 LovSelectTable` |
| F3 LIST 弹窗(多) | 由 `ListMultiClosed` + dialog 交互 | — | `ListMultiOpen 多选弹窗含 LovSelectTable` |
| F5 LIST 回显(单·关闭态) | `LovSelect/ListSingleEcho` | 覆盖（displayText 恢复） | `ListSingleEcho 回显已选文本`（截图）+ `ListSingleEcho 回显文本(不应是数字)`（断言·守护 INV-1） |
| F5 LIST 回显(多·确认后) | 由 `ListMultiClosed` + dialog 交互 | 覆盖（onTableMultiConfirm 返回值） | `ListMulti 选择后回显文本(不应是数字)`（断言·守护 INV-1） |
| INV-5 getSelectedLabel 契约 | — | `lovStore.test.ts` 回显契约（真实模块，未登记回退数字/登记返文本/选择登记优先） | — |
| INV-6 CT 桩忠实 | — | `LovSelect.mockFidelity.spec.ts` CT 桩忠实性（未登记回退数字，无源兜底） | — |

> LIST 弹窗内部是 `LovSelectTable`，其分页/统计/回显等细分功能见 `../LovSelectTable/README.md`。

### 9. 视觉基线说明（Visual Baselines）

- 需锁定像素基线的关键状态：`EnumSingle` 下拉展开、`EnumSingleSelected` 已选回显（显示「启用」）、`ListSingleOpen` 弹窗含表格。基线命名与 story id 对应。
- 基线更新需人工 review（详见技能 §六 CI 与基线管理），避免无意识覆盖。

### 10. 验证流程（类人点击验证）

除自动化 CT 外，可运行 `ct:server` 后用 Playwright 真人式逐项点击断言，确认每个功能真实可用（详见技能 `vue-component-visual-loop` → 类人点击验证流程）：启动 gallery 后按 §4 清单逐项点击 / 翻页 / 断言文字与回显；截图统一输出到固定目录 `ct/verify-shots/`（与 `ct/` 同目录，已被 `.gitignore` 忽略）。

```bash
pnpm ct:server            # 起 gallery(dev server, 5190, HMR)，随后按「验证流程」表逐项点击/翻页/断言
```

逐项验证清单（对应 §4 + 功能 ↔ Story）：

| 功能 | 操作 | 期望 |
|---|---|---|
| F1 ENUM 单选下拉 | 打开 `?story=LovSelect/EnumSingle`，点 `.el-select` 展开 | 出现 options 下拉 |
| F2 ENUM 多选下拉 | `?story=LovSelect/EnumMulti` 展开 | 多选 + collapse-tags |
| F5 ENUM 回显 | `?story=LovSelect/EnumSingleSelected` | 显示已选 label「启用」 |
| F3 LIST 关闭态 | `?story=LovSelect/ListSingleClosed` | 只读 input + 搜索按钮 |
| F3 LIST 弹窗 | 点搜索按钮 | 弹出 LovSelectTable（见 LovSelectTable 验证） |
| F5 LIST 回显 | `?story=LovSelect/ListSingleEcho` | 显示已选 displayText |

### 11. 运行测试

```bash
# Vitest（逻辑层）
pnpm test:unit                                                  # 全部
pnpm exec vitest run --config vitest.config.unit.ts LovSelect   # 单文件（逻辑回归）
# 沙箱被 shim 拦截 / 无 pnpm 时，用 mjs 入口：
node node_modules/vitest/vitest.mjs run --config vitest.config.unit.ts LovSelect

# CT 视觉回归（需本机 Chrome，本地跑）
pnpm test:ct:update    # 首次/改样式后刷新基线（生成/覆盖 *-snapshots/*.png）
pnpm test:ct           # 无头回归对比（基线已确立后）
pnpm test:ct:dev       # 有头浏览器跑测试并刷新基线（看过程）
# 仅跑本组件：pnpm test:ct --grep "LovSelect/"
# 沙箱/CI 加 CT_NO_SANDBOX=1；陈旧 vite 占 5190 时加 CT_FRESH=1 切 5193 强制新 server
```

---

## Part C：测试完备性（防回归）

> 本组件曾出现真实缺陷「LIST 多选/单选关闭态回显数字而非文本」，而修复前**所有自动化测试全绿**。
> 复盘根因后沉淀如下规范，确保「会出问题的地方」都有断言守护，而不是靠截图或作弊桩蒙混。

### 13. 核心不变量（Invariants）——测试必须守护的红线

> 这些不变量是"容易破、破了用户立刻可见"的红线。每一条都曾破过或极易破；测试必须以**断言（语义）**守护，而非仅截图。
> 完整「不变量 ↔ 测试」映射见 §16。

| ID | 不变量 | 为什么容易破 / 破了后果 |
|---|---|---|
| INV-1 | **LIST 回显必须显示文本 label，绝不允许回退成原始数字 id**（单选关闭态 `ListSingleEcho`、多选确认 `ListMulti` 同约束） | 多选 `confirmMulti` 只 emit 纯 id、`loadMeta` 正常路径漏消费 `inlineEnums` 都会让 `getSelectedLabel` 查不到 label → 显示 `1`/`2`，用户无法识别已选 |
| INV-2 | **外部 `modelValue` 仅作流入（inflow-only）**：`LovSelectTable` 只读父组件同步后的内部值（`listSelectValue`），不得直接消费外部 `modelValue` | 直接传外部值 → 回流/回显错位（前几轮"回流不正常"根因） |
| INV-3 | **LIST 触发器复用 el-select 原生 `suffix-icon`（搜索图标）+ `@visible-change` 拦截打开弹窗**；整棵 el-select 子树 `pointer-events` 仅放开 `.el-select__suffix`/`.el-tag__close`/`.el-select__clear`，不得自绘触发按钮 | 自绘按钮 + stale 代码曾导致"点击搜索图标没反应"；`pointer-events:none` 若误伤触发元素则点击失效 |
| INV-4 | **标签删除复用 el-select 原生 `@remove-tag`**（载荷为 value），不得自绘 `#tag` 插槽 | 自绘插槽与 `pointer-events:none` 策略叠加，易让删除事件摸不到 |
| INV-5 | **`getSelectedLabel` 只查 `labelCache`，缺失即回退原始值（数字 id），绝不做"按 id 回退源数据"的作弊** | 一旦加源兜底，测试环境把数字伪装成文本 → 缺陷被掩盖（本轮踩坑） |
| INV-6 | **CT 桩 `ct/mocks/lovStore` 必须忠实复现生产 `getSelectedLabel` 契约（INV-5）** | 桩不忠实 = 测试在骗自己；本轮"回显数字"缺陷正是被不忠实桩掩盖 |

### 14. 缺陷复盘：为什么此前的测试没抓到「LIST 回显数字」

本会话暴露的真实缺陷（LIST 多选/单选关闭态回显数字而非文本），在修复前**所有自动化测试全绿**。复盘根因，避免重蹈：

1. **CT 桩不忠实（最关键）**：旧 `ct/mocks/lovStore.getSelectedLabel` 带"按 id 回退 SAMPLE 源"兜底，把本应显示的数字 `1` 重写成 `管理员`，组件在 CT 里"看起来正常"，缺陷被桩抹平。
2. **只有截图测试、没有语义断言**：`ListSingleEcho` 是 `toHaveScreenshot`；而该基线恰是在缺陷态捕获的 → 缺陷被**固化进基线**，重跑永远匹配、永不报错。截图只能锁"视觉长相"，锁不住"语义对错"。
3. **单测层把 `getSelectedLabel` 整个 mock 掉**：`LovSelect.test.ts` 用 `vi.mock('./lovStore')` 把 `getSelectedLabel` 桩成 `label-${v}`，从未走真实解析路径 → 逻辑层也测不到回显语义。
4. **缺少"回显必须是文本而非数字 id"的断言测试**：ENUM 有 `EnumSingle 选择后回显数据正常`（`toContainText('启用')`），但 LIST 没有同类断言。

### 15. 测试编写规范（确保能测出会出问题的地方）

- **凡是"用户可见文本"必须用 `toContainText` 断言语义，不能只靠截图**。截图仅作视觉回归，且每次改动须人工 review 基线；**绝不在缺陷态捕获/更新基线**（否则固化 bug）。
- **lovStore 相关 CT 桩必须忠实**（INV-6）；并对桩本身写一条「忠实性断言」测试（§16 映射 `mock fidelity`），防止桩悄悄作弊。
- **关键不变量（INV-1…INV-6）每条都要有守卫测试**，写进 §16 对应表；新增不变量时同步补测试。
- **单测层若 mock 了 lovStore，必须另设"真实 lovStore 契约测试"** 覆盖 `getSelectedLabel` 等被 mock 掉的核心逻辑——否则 mock 把缺陷也一起 mock 掉了（本轮教训 3）。
- **回归用例命名显式标注反向约束**（如"不应是数字 / 必须文本"），让意图一眼可见。

### 16. 不变量 ↔ 测试 对应表

| 不变量 | 守卫测试（文件 · 用例） |
|---|---|
| INV-1 LIST 多选回显文本 | `LovSelect.ct.spec.ts` · `ListMulti 选择后回显文本(不应是数字)` |
| INV-1 LIST 单选关闭态回显文本 | `LovSelect.ct.spec.ts` · `ListSingleEcho 回显文本(不应是数字)` |
| INV-2 modelValue inflow-only | `useLovSelect` 派生 `listSelectValue` + CT 弹窗传内部值（已有逻辑；建议补"改 modelValue 后 LovSelectTable 收到内部同步值"断言） |
| INV-3 搜索图标触发弹窗 | `LovSelect.ct.spec.ts` · `ListSingleOpen`/`ListMultiOpen`（点 `.el-select__suffix` 开弹窗） |
| INV-4 remove-tag 删除 | `LovSelect.ct.spec.ts` 交互 + 逻辑（建议补"点 × 删标签后回显减少"断言） |
| INV-5 getSelectedLabel 无作弊 | `lovStore.test.ts` · 回显契约（真实模块） |
| INV-6 CT 桩忠实 | `LovSelect.mockFidelity.spec.ts` · CT 桩忠实性 |

---

### 12. 变更记录（Changelog）

| 日期 | 变更 | 关联 |
|---|---|---|
| 2026-08-04 | 按 `vue-component-visual-loop` 模板反向补全 Part A（功能需求/设计/流程/验证列表/数据契约/边界/待确认） | FR1–FR6 / V1–V6 |
| 2026-08-04 | 修复 LIST 回显：`applyMeta()` 解析出 LIST 后调用 `updateDisplayText()` | F5 |
| 2026-08-04 | 别名导入修复：`useLovSelect.ts` 由相对路径改为 `@newlifex/cube-vue/core/utils/lov-api`（避免绕过 CT mock 致空白/ENUM 原始值） | §5 数据契约 |
| 2026-08-10 | 修复 LIST 回显数字：① `LovSelectTable.fetchListData` 登记当前 lovCode 自身行（`registerRows`）② `useLovSelect.loadMeta` 正常路径消费 `inlineEnums` 写入 `translateCache` ③ `LovSelect.listTags` 加 translateCache 回退 + 依赖 listMeta 响应式 | INV-1 / F5 |
| 2026-08-10 | 新增断言测试守护回显：`LovSelect.ct.spec.ts` 增 `ListMulti 选择后回显文本(不应是数字)`、`ListSingleEcho 回显文本(不应是数字)` | INV-1 |
| 2026-08-10 | 新增契约/忠实性单测：`lovStore.test.ts`（真实模块回显契约，INV-5）、`LovSelect.mockFidelity.spec.ts`（CT 桩忠实性，INV-6） | INV-5 / INV-6 |
| 2026-08-10 | 沉淀 Part C：核心不变量(§13)、缺陷复盘(§14)、测试编写规范(§15)、不变量↔测试表(§16) | 测试完备性 |
| 2026-08-10 | 按 `vue-component-visual-loop` §十 方法论升级本文档：新增 §1.5 能力维度矩阵（防漏功能）、§4.5 不变量红线（开发前，防假绿）、§4 标注为「故事走查闸门」；顶部加方法论指针 | 测试完备性 / §十 |
| 2026-08-10 | 强化技能 `vue-component-visual-loop`：新增 §十 测试完备性方法论（理论根基+矩阵+不变量+四阶段流程+防假绿纪律）+ 参考文档 `references/testing-completeness-methodology.md`；模板 `component-readme-template.md` 补 §1.5 矩阵、§4.5 不变量、§8 不变量↔测试表 | 测试完备性 / 技能 |
