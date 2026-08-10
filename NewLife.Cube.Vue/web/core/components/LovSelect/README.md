# LovSelect 组件

值集（LOV）输入控件。统一入口，按后端值集配置自动渲染「枚举下拉」或「列表弹窗」两种形态。

> 本 README 遵循技能 `vue-component-visual-loop` §七 模板。**Part A 为开发前必填**（本组件于 2026-08-04 按模板反向补全）；Part B 为开发中 / 后沉淀。

---

## Part A：开发前必填

### 1. 功能需求（Functional Requirements）

- **FR1（ENUM 单选下拉）**：作为业务表单使用者，我希望 ENUM 类型值集渲染为单选下拉，选完即回填，以便快速选枚举值。验收：下拉展开出现 options，选值后 `modelValue`=选中值、输入框显示 label。
- **FR2（ENUM 多选下拉）**：作为使用者，我希望多选下拉可用 `collapse-tags` 折叠已选标签，以便多值紧凑展示。验收：多选模式下选中多项后标签折叠，hover 显示全量。
- **FR3（LIST 弹窗触发）**：作为使用者，我希望 LIST 类型值集渲染为只读输入框 + 搜索按钮，点击打开列表弹窗，以便从大表里选。验收：只读 input + 搜索按钮；点按钮弹出含 `LovSelectTable` 的弹窗。
- **FR4（加载中占位）**：作为使用者，当值集元数据尚未加载完成时，我希望看到 disabled loading 占位，以便知道组件在准备数据。验收：meta 未就绪时渲染 disabled loading `el-select`。
- **FR5（回显）**：作为使用者，我希望通过 `modelValue` 传入已选值时组件正确回显——ENUM 显示已选 label、LIST 显示已选文本（displayText），以便编辑态还原。验收：传入 `modelValue` 后输入框显示对应文本，且不依赖再次交互。
- **FR6（事件 emit）**：作为调用方，我希望选择变化时收到 `update:modelValue` 与 `change`，单选返回值、多选返回 `string[]`，以便表单集成。验收：选中后 emit 对应值。

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

### 4. 功能验证列表（Verification Checklist）

> 源于 §1 功能需求，是「开发完成」验收清单，也是 §10 类人验证的输入。

| # | 验证项（源于 FR） | 前置状态 | 操作步骤（`?story=` 深链打开后） | 期望结果 | 状态 |
|---|---|---|---|---|---|
| V1 | ENUM 单选下拉（FR1） | `LovSelect/EnumSingle` | 点 `.el-select` 展开 | 出现 options 下拉 | ☐ |
| V2 | ENUM 多选下拉（FR2） | `LovSelect/EnumMulti` | 展开多选 | 多选 + collapse-tags 折叠 | ☐ |
| V3 | ENUM 回显（FR5） | `LovSelect/EnumSingleSelected` | 直接查看 | 显示已选 label「启用」（非数字 `1`） | ☐ |
| V4 | LIST 关闭态（FR3） | `LovSelect/ListSingleClosed` | 直接查看 | 只读 input + 搜索按钮 | ☐ |
| V5 | LIST 弹窗（FR3） | `LovSelect/ListSingleClosed` | 点搜索按钮 | 弹出 LovSelectTable 弹窗 | ☐ |
| V6 | LIST 回显（FR5） | `LovSelect/ListSingleEcho` | 直接查看 | 显示已选 displayText | ☐ |

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

### 12. 变更记录（Changelog）

| 日期 | 变更 | 关联 |
|---|---|---|
| 2026-08-04 | 按 `vue-component-visual-loop` 模板反向补全 Part A（功能需求/设计/流程/验证列表/数据契约/边界/待确认） | FR1–FR6 / V1–V6 |
| 2026-08-04 | 修复 LIST 回显：`applyMeta()` 解析出 LIST 后调用 `updateDisplayText()` | F5 |
| 2026-08-04 | 别名导入修复：`useLovSelect.ts` 由相对路径改为 `@newlifex/cube-vue/core/utils/lov-api`（避免绕过 CT mock 致空白/ENUM 原始值） | §5 数据契约 |
