# LovSelectTable 组件

值集列表型选择弹窗（表格 + 搜索 + 分页 + 已选统计）。被 `LovSelect` 的 LIST 分支内嵌，也可独立使用。

> 本 README 遵循技能 `vue-component-visual-loop` §七 模板。**Part A 为开发前必填**（本组件于 2026-08-04 按模板反向补全）；Part B 为开发中 / 后沉淀。

---

## Part A：开发前必填

### 1. 功能需求（Functional Requirements）

- **FR1（弹窗表格渲染）**：作为使用者，我希望打开弹窗看到值集表格，单选高亮当前行，以便浏览与选择。验收：弹窗出现表格行，单选态当前行可高亮。
- **FR2（左侧勾选列）**：作为使用者，我希望多选用复选框且跨页保留已选、单选用 radio 一眼区分，以便明确交互模式。验收：多选出现复选框列（翻页保留）+ 单选出现 radio 列。
- **FR3（搜索栏）**：作为使用者，我希望按 `searchFields` 搜索（input/select/lov/datepicker），以便过滤。验收：搜索栏按配置渲染对应控件，提交后过滤列表。
- **FR4（分页）**：作为使用者，我希望大数据分页浏览（total/sizes/prev/pager/next/jumper），以便翻页。验收：`listConfig.pageable` 为真时渲染 `el-pagination`。
- **FR5（已选统计）**：作为使用者，我希望底部看到「已选 N 项」统计（跨页累计、不被当前页裁剪），以便知道选了多少。验收：多选已选数正确累加；单选显示 0/1。
- **FR6（确定/取消）**：作为使用者，我希望取消关闭弹窗、多选「确定」批量 emit，以便提交。验收：取消关闭；多选「确定」`emit('confirm', string[])`。
- **FR7（回显高亮）**：作为使用者，我希望通过 `modelValue` 传入已选时，弹窗打开即恢复勾选/高亮（含跨页），以便编辑态还原。验收：传入 `modelValue` 后对应行勾选/高亮，翻页后保持。
- **FR8（批量翻译）**：作为使用者，我希望 `refLovCode` 列经字典批量翻译显示中文，以便可读。验收：翻译列显示中文标签（经 `fetchBatchLabel` + `translateCache`）。

### 2. 设计（Design）

- **UI 薄壳**：模板直接消费传入的 `lovMeta` 渲染搜索栏/表格/分页，**自身不 fetch 元数据**（调用方须先 `fetchLovMeta` 拿到 `LovListMeta` 再传入 —— 独立使用时注意此契约）。
- **数据加载**：`dialogVisible` 变 `true` 时 `watch` 触发 `fetchListData()`（与真实交互一致），按 `listConfig` 决定直连或后端代理。
- **左侧勾选列**：多选 = 复选框（`reserve-selection` 跨页保留），单选 = radio（一眼区分交互模式）。
- **底部 footer**：左 `.lst-selected-count`（已选统计），右 取消/确定（多选才显示「确定」）。

### 3. 流程（Flow）

- **用户操作流程（多选）**：打开弹窗 → 加载数据 → 勾选（可跨页）→ 查看「已选 N 项」→（可选搜索/翻页）→ 确定 emit / 取消关闭。
- **用户操作流程（单选）**：打开弹窗 → 加载数据 → 点 radio 选中当前行 → 立即 emit 并关闭。
- **回显流程**：传入 `modelValue` → 打开弹窗 → `restoreSelection` 按 `getRowKey` 重放勾选/高亮 → 翻页时 `reserve-selection` 保留跨页选择。
- **关键数据流**：`lovMeta`(prop) + `modelValue`(prop) → `selectedValues`(权威集合) → 勾选 `onSelectionChange`（非重放时）→ `confirm` emit 副本；搜索/翻页 → `fetchListData` → 表格刷新。
- **状态机**：`closed → (dialogVisible true) → loading → data-ready → selecting → { multi: confirmed | single: auto-confirm } → closed`。标注：loading 态 UI=加载中；data-ready UI=表格+勾选列；selecting UI=可勾选/翻页/搜索。

### 4. 功能验证列表（Verification Checklist）

> 源于 §1 功能需求，是「开发完成」验收清单，也是 §10 类人验证的输入。

| # | 验证项（源于 FR） | 前置状态 | 操作步骤（`?story=` 深链打开后） | 期望结果 | 状态 |
|---|---|---|---|---|---|
| V1 | 弹窗表格(单)（FR1） | `LovSelectTable/SingleOpen` | 直接查看 | 弹窗出现表格行 | ☐ |
| V2 | 弹窗表格(多)（FR2/F6） | `LovSelectTable/MultiOpen` | 直接查看 | 复选框列 + 右下「确定」按钮 | ☐ |
| V3 | 单选回显高亮（FR7） | `LovSelectTable/SingleEcho` | 直接查看 | 「已选 1 项」+ 当前行高亮 | ☐ |
| V4 | 多选回显（FR5/F7） | `LovSelectTable/MultiEcho` | 直接查看 | 「已选 2 项」+ 两行勾选 | ☐ |
| V5 | 分页+跨页统计（FR4/F5） | `LovSelectTable/PagedMultiSelection` | 第1页勾1→翻第2页勾2 | 「已选 3 项」（跨页累计） | ☐ |
| V6 | 跨页回显统计(BUG C2/C3)（FR5/F7） | `LovSelectTable/PagedMultiEcho`(modelValue=[1,2,21,22]) | 打开→翻第2页→翻回 | 「已选 4 项」始终不变、每页命中行高亮（修复前被裁成 2 项） | ☐ |

### 5. 数据契约（Data Contract）

- **不 fetch 元数据**：调用方先 `fetchLovMeta` 拿到 `LovListMeta` 传入 `lovMeta`（独立使用注意此契约）。
- **列表数据**：`fetchListData()` 在 `dialogVisible=true` 时触发，按 `listConfig` 直连或后端代理。
- **批量翻译（FR8）**：`refLovCode` 列经 `fetchBatchLabel` 翻译 + `translateCache` 缓存。
- **Mock 约定**：
  - CT：`ct/vite.config.ts` 别名桩 `@newlifex/cube-vue/core/utils/request` → `ct/mocks/request.ts`（桩列表数据；`code` 含 `Paged` 返回 23 条、可分 2 页）；`@newlifex/cube-vue/core/utils/lov-api` → `ct/mocks/lov-api.ts`（独立使用 `lovMeta` 同源于此桩）；`virtual:@newlifex/cube-vue-*` 桩为 `{}`（mockCubeVueVirtual 插件）。
  - Vitest：`vi.mock('@newlifex/cube-vue/core/utils/request')` 注入分页数据。
  - **铁律**：组件内凡被 mock 的 `request`/`lov-api` 必须用项目别名导入，禁用相对路径（相对路径绕过别名桩 → 打到真实后端）。

### 6. 边界与异常（Edge cases）

- 空数据 / 加载中 / 错误态展示。
- 分页边界：仅一页、末页单行、超长 total。
- 超长文本截断（单元格 ellipsis）。
- 超大列表：当前未做虚拟滚动，超大数据需评估。
- 连续快速翻页 / 搜索：以最后一次请求为准。
- 禁用态：整体 disabled。
- **回显项跨多页并重开弹窗**（核心 bug 场景）：`modelValue` 含未加载页的行，重开弹窗须恢复全部勾选且不丢跨页选择。
- `modelValue` 含未加载项：`getRowKey` 统一 `String(...)` 比对，保证回显命中。

### 7. 待确认项（Open Questions）

- [x] 无未决事项（本组件补录 Part A 时，需求与契约均已明确；BUG 修复 C2/C3 已闭环）。

---

## Part B：开发中 / 后沉淀

### 8. 功能清单与 功能 ↔ Story ↔ 测试 对应

**功能清单**

| # | 功能 | 说明 |
|---|---|---|
| F1 | 弹窗表格渲染 | 按 `tableColumns` 渲染，单选高亮当前行 |
| F2 | 左侧勾选列 | 多选=复选框（跨页 reserve）；单选=radio 直接选中并关闭 |
| F3 | 搜索栏 | `searchFields`：input / select / lov（嵌套 `LovSelect`）/ datepicker |
| F4 | 分页 | `listConfig.pageable` → `el-pagination`（total/sizes/prev/pager/next/jumper） |
| F5 | 已选统计 | 底部「已选 N 项」（多选=选中数；单选=0/1 项） |
| F6 | 确定/取消 | 取消关闭弹窗；多选「确定」emit `confirm(string[])` |
| F7 | 回显高亮 | 单选高亮已选行；多选勾选已选行（跨页 reserve-selection 保留） |
| F8 | 批量翻译 | `refLovCode` 列经 `fetchBatchLabel` 翻译 + `translateCache` |

**功能 ↔ Story ↔ 测试 一一对应**

Vitest 覆盖逻辑层（`LovSelectTable.spec.ts`）；CT 覆盖 UI 渲染与交互（`LovSelectTable.ct.spec.ts`）。每个 story 在 `LovSelectTable.story.ts` 内用「测试状态：」注释标明验证的运行时状态。

| 功能 | Story（LovSelectTable.story.ts） | Vitest（LovSelectTable.spec.ts） | CT 截图/断言（LovSelectTable.ct.spec.ts） |
|---|---|---|---|
| F1 弹窗表格(单) | `LovSelectTable/SingleOpen` | 覆盖（fetchListData、rowKey） | `单选-打开弹窗` |
| F2/F6 弹窗表格(多) | `LovSelectTable/MultiOpen` | 覆盖（selection-change、confirmMulti） | `多选-打开弹窗（含确定按钮）` |
| F7 单选回显高亮 | `LovSelectTable/SingleEcho` | 覆盖（currentValue 高亮） | `单选-回显已选高亮` |
| F5/F7 多选回显 | `LovSelectTable/MultiEcho` | 覆盖（restoreSelection、已选 2 项） | `多选-回显已选高亮` |
| F4/F5 分页+跨页统计 | `LovSelectTable/PagedMultiSelection` | — | `多选-翻页跨页选中统计正常`（第1页选1→翻页选2→断言「已选 3 项」→截图） |
| 回显/翻页/已选统计（BUG 修复·C2/C3） | `LovSelectTable/PagedMultiEcho` | 覆盖（跨页 modelValue 打开后「已选 N 项」与集合不被当前页勾选裁剪） | `多选-跨页回显已选统计正确`（打开→翻第2页→翻回，统计与勾选保持） |

### 9. 视觉基线说明（Visual Baselines）

- 需锁定像素基线的关键状态：`SingleOpen`/`MultiOpen`（含确定）、`SingleEcho`/`MultiEcho`（回显高亮）、`PagedMultiSelection`/`PagedMultiEcho`（分页跨页统计）。基线命名与 story id 对应。
- 基线更新需人工 review（详见技能 §六 CI 与基线管理），避免无意识覆盖。

### 10. 运行测试

```bash
# Vitest（逻辑层）
pnpm test:unit                                                       # 全部
pnpm exec vitest run --config vitest.config.unit.ts LovSelectTable    # 单文件（逻辑回归）
# 沙箱被 shim 拦截 / 无 pnpm 时，用 mjs 入口（.bin/vitest 是 shell 脚本，不能直接 node 执行）：
node node_modules/vitest/vitest.mjs run --config vitest.config.unit.ts LovSelectTable

# CT 视觉回归（需本机 Chrome，本地跑）
pnpm test:ct:update    # 首次/改样式后刷新基线（生成/覆盖 *-snapshots/*.png）
pnpm test:ct           # 无头回归对比（基线已确立后）
# 仅跑本次 BUG 修复的跨页回显验证（推荐先单独过这一条）：
pnpm test:ct --grep "跨页回显已选统计正确"
# 沙箱/CI 加 CT_NO_SANDBOX=1；陈旧 vite 占 5190 时加 CT_FRESH=1 切 5193 强制新 server
```

### 11. 验证流程（类人点击验证）

除自动化 CT 截图外，可运行 `ct:server` 后用 Playwright 真人式逐项点击断言，确认每个功能真实可用（见技能 `vue-component-visual-loop` → 类人点击验证流程）：启动 gallery 后按 §4 清单逐项点击 / 翻页 / 断言文字与勾选数；截图统一输出到固定目录 `ct/verify-shots/`（与 `ct/` 同目录，已被 `.gitignore` 忽略）。

```bash
pnpm ct:server            # 起 gallery(dev server, 5190, HMR)，随后按「验证流程」表逐项点击/翻页/断言
```

逐项验证清单（对应 §4 + 功能 ↔ Story，覆盖回显/翻页/已选统计三大块）：

| 功能 | 操作（?story= 深链打开后） | 期望断言 |
|---|---|---|
| F1 弹窗表格(单) | `LovSelectTable/SingleOpen` | 弹窗出现表格行 |
| F2/F6 弹窗表格(多) | `LovSelectTable/MultiOpen` | 复选框列 + 右下「确定」按钮 |
| F7 单选回显高亮 | `LovSelectTable/SingleEcho` | 「已选 1 项」+ 当前行高亮 |
| F5/F7 多选回显 | `LovSelectTable/MultiEcho` | 「已选 2 项」+ 两行勾选 |
| F4/F5 分页+跨页统计 | `LovSelectTable/PagedMultiSelection`：第1页勾1→翻第2页勾2 | 「已选 3 项」（跨页累计） |
| **BUG 修复·C2/C3** | `LovSelectTable/PagedMultiEcho`（modelValue=[1,2,21,22]）：打开→翻第2页→翻回 | 「已选 4 项」始终不变、每页命中行高亮（修复前会被裁成 2 项） |

> 陈旧 vite 占 5190 时，先结束残留 node；或脚本内 baseUrl 切 `http://127.0.0.1:5193/?story=...` 并加 `CT_FRESH=1` 起新 server。

### 12. 问题可能原因分析（回显 / 翻页 / 已选统计）

> 结论先行：**用户反馈的 6 个现象（回显、翻页回显、已选、翻页后已选不正确、翻页后勾选才更新、点击回显时已选不正确）根因同源**——多选模式下 `selectedValues`（已选集合，也是底部「已选 N 项」的权威来源）在 `restoreSelection` 用 `toggleRowSelection` 重放勾选时，被 `el-table` 的 `@selection-change`（`onSelectionChange`）以"本次重放命中的当前页行"覆盖，导致跨页/回显的选中项被裁剪丢失。
>
> 判定依据：症状 4/5/6 同时出现且只在"翻页 / 重开回显"后发生，而 `onSelectionChange` 正是这两个路径都会触发的唯一写入口，且与"手动勾选后统计恢复"（症状 5）吻合——手动勾选走的是同一条 `onSelectionChange`、但此时 `selectedValues` 未被覆盖。

#### 现象 ↔ 可能根因对照

| 现象 | 候选根因（全部列出） | 实际判定 |
|---|---|---|
| **回显**（重开弹窗应恢复勾选与高亮） | A1 `dialogVisible` watch 未把 `modelValue` 灌入 `selectedValues`；A2 `restoreSelection` 因 `tableRef` 未挂载而跳过重放；A3 `getRowKey` 类型与 `selectedValues`(string) 不一致导致 `toggleRowSelection` 比对失败 | A1/A3 不成立（watch 已灌入、key 统一 String）；属"重放是否被正确保留"问题，见核心根因 |
| **翻页回显**（翻到某页应恢复该页已选勾选） | B1 `reserve-selection` 未生效（缺 `row-key`）；B2 翻页 `fetchListData` 时机早于重放；B3 核心根因导致重放集合本身已被裁掉，重放自然不全 | B1/B2 排除（已设 `row-key`、重放在 `nextTick` 后）；真正受限点是核心根因 |
| **已选**（底部「已选 N 项」） | C1 统计文本用错来源；C2 `selectedValues` 被覆盖（核心根因） | C1 排除（已用 `selectedValues.length`）；C2 成立 |
| **翻页后已选不正确** | C2 `onSelectionChange` 在重放期间以当前页子集覆盖 | 成立（核心根因） |
| **翻页后勾选才更新** | C2 同上：翻页重放被覆盖→统计掉到当前页，直到用户手动勾选触发一次"全量" `selection-change` 才恢复 | 成立（核心根因） |
| **点击回显时已选不正确** | C2 `restoreSelection` 对回显项调用 `toggleRowSelection`，`selection-change` 只上报"当前页命中的回显行"，把 `selectedValues`（含其它页/未加载项）裁成当前页子集 | 成立（核心根因） |

#### 全部候选根因（含已排除项，供复核）

- **C1（统计来源错）**：`selectedCountText` 多选用 `selectedValues.length`。✅ 正确，已排除。
- **C2（权威集合被覆盖，核心根因）**：`onSelectionChange` 在 `restoreSelection` 重放 `toggleRowSelection` 期间被触发，el-table 此时仅上报本次 toggle 的当前页行，覆盖掉 `selectedValues` 中来自其它页 / 回显的选中项。
- **C3（重放末尾二次裁剪）**：原 `restoreSelection` 末尾 `if (restored.length > 0) selectedValues.value = restored;` 把 `selectedValues` 重置为"仅当前页命中的行"，丢弃跨页已选。⚠️ 与 C2 同源，叠加放大。
- **C4（`row-key` 类型不一致）**：`getRowKey` 返回原始类型(number)，而 `selectedValues` 为 string，致 `toggleRowSelection` 与 el-table 内部 key 比对失败 → 回显勾选不生效。✅ 已统一用 `String(...)` 比对，排除。
- **C5（`reserve-selection` 未生效）**：缺 `row-key` 时跨页不保留。✅ 已设 `:row-key="getRowKey"`，排除。
- **C6（jsdom 假象）**：测试环境 `selection-change` 不触发，`selectedValues` 只靠重放赋值，掩盖真实环境覆盖 bug。⚠️ 已在 Vitest 用分页 mock 复现并断言修复。

#### 修复方案（结论先行 + 技术核对）

1. **`onSelectionChange` 加 `restoringSelection` 守卫**：重放（回显 / 翻页）期间 `restoring=true`，`onSelectionChange` 直接 `return`，绝不覆盖权威集合；重放结束后 `restoring=false`，用户手动勾选仍走"以 el-table 全量选择为准"的正常路径（`reserve-selection` 保证跨页全量）。
2. **`restoreSelection` 不再回写 `selectedValues`**：改为 `toggle(row, selected.has(key))`——命中则勾选、未命中则取消（顺带清掉 `reserve-selection` 可能残留的旧勾选），让"勾选视图"与"权威集合"对齐，而非用视图反写集合。
3. **`selectedValues` 成为唯一真相源**：弹窗打开由 `modelValue` 灌入、用户勾选由 `onSelectionChange`（非重放时）更新、`confirm` 直接 emit 副本。任何路径都不再用"当前页勾选子集"裁剪它。

> 该修复不改动对外契约（props/emits 不变），属最小改动；旧 `restoreSelection` 的"回显勾选"职责保留，只是去掉了危险的二次回写。

### 13. 变更记录（Changelog）

| 日期 | 变更 | 关联 |
|---|---|---|
| 2026-08-04 | 按 `vue-component-visual-loop` 模板反向补全 Part A（功能需求/设计/流程/验证列表/数据契约/边界/待确认） | FR1–FR8 / V1–V6 |
| 2026-08-04 | 修复跨页回显/已选统计（C2/C3）：`restoringSelection` 守卫 + `restoreSelection` 改 `toggle` 不回写；`selectedValues` 成唯一真相源 | F5/F7 / V6 |
