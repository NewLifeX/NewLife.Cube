# OSC-0015 Design — 筛选构建器与多级分组

## 1. 目标与契约边界

本号纯前端扩展。`NamedView`（存 `ViewsJson`）新增两个域：`filter`（筛选构建器方案）与 `group`（多级分组字段列表）。筛选条件应用时序列化为与搜索表单一致的扁平参数并入 `effectiveSearch` 触发 `GetList`；分组对已返回 `tableData` 纯前端分组展示。**后端零改动**：不新增 API、不改 `Search(Pager)` 语义、不动 ViewProfile 线协议。

后端 `ReadOnlyEntityController.Search(Pager)` 的搜索能力是操作符集合的硬约束：

- `p[字段]` 有值 → `field.Equal(val)`：**等值**（含多选字段逗号分隔字符串）。
- `p["Q"]` → `Entity.SearchWhereByKeys`：**全局关键字模糊**（非按字段，语义与字段级包含不同）。
- `p[字段_min]` / `p[字段_max]`：仅重写 `Search` 的控制器支持**范围**（与现有搜索表单 `_min/_max` 提交完全一致）。

因此本号筛选操作符 = `等于` + `范围`，与现有搜索表单能力严格一致；筛选构建器的价值在于**多条件可视化组合（AND/OR）**，而非新增后端不支持的算子。

## 2. 文件级改动地图

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/core/utils/viewProfile.ts` | `NamedView` 增加 `filter?`/`group?`；新增 `ViewFilter`/`ViewFilterCondition` 类型与 `normalizeFilter`（宽容解析、非法归一、未知字段保留）；`serializeNamedView`/`parseNamedViews` 透传两个域 | 既有 columns/sort/chrome/mapping/insight 结构与 `_raw` round-trip |
| `web/src/core/utils/searchFilters.ts` | 新增 `filterToSearchParams(filter, fields, keys)`：条件 → 扁平搜索参数 | 既有 `cleanSearchParams`/`collectSearchKeys`/`parseUrlSearch` |
| `web/src/core/utils/viewMapping.ts` | 新增 `groupRows(records, groupFields, fields, dataSource)` 与 `GroupNode` 类型 | 既有 bucketKanban/normalizeDataSource 等 |
| `web/src/views/crud/FilterBuilderPopover.vue`（新增） | 条件构建器弹层 UI：字段/操作符/值/删除、AND-OR 切换、应用/保存/清除；锚定工具栏「筛选」按钮 | — |
| `web/src/views/crud/GroupPopover.vue`（新增） | 多级分组字段选择弹层（有序增删）；锚定工具栏「分组」按钮 | — |
| `web/src/views/crud/DefaultList.vue` | 「筛选」按钮改为展开 FilterBuilderPopover；「分组」按钮改为展开 GroupPopover；`effectiveSearch` 合并 `viewFilterParams`；分组数据渲染分支；已筛选/已分组标签；两弹层互斥 | GetPage/字段分区、CRUD、分页、RecordDrawer、既有搜索逻辑 |
| `web/src/stores/viewProfile.ts` | `patchActiveFilter`/`patchActiveGroup` 保存域；加载视图自动应用 | 400ms debounce、失败回滚、既有各域保存 |
| `web/src/features/search/QueryInsightPanel.vue` | 搜索字段容器默认一行、超行折叠「展开更多 N」 | 搜索/重置/保存/清除 emits 与 stat/chart 区 |
| `web/src/features/vtable/ListTable.vue` | 新增 `grouped` 模式：组头行渲染 + 组内行（VTable hierarchy 复用）+ 组头折叠 | 既有 records/columns/hierarchy 表视图语义 |
| `web/src/components/LovSelect.vue` | LOV LIST 模式：下拉支持远程搜索（输入过滤）+ 已选标签 | ENUM 模式与「更多」表格 |
| `web/src/**/*.spec.ts` | 补 filter 解析/序列化、groupRows、折叠逻辑、组件用例 | 既有测试断言 |
| `NewLife.Cube.ArcoVue/web/README.md`、`Doc/功能清单.md`、`Doc/Api/ArcoVue企业中后台迁移方案.md` | 事实性登记 OSC-0015 | — |

## 3. JSON schema 与兼容

### 3.1 NamedView 新增域

```ts
interface ViewFilter {
  /** 条件组逻辑：all=且(AND)，any=或(OR) */
  logic: 'all' | 'any'
  /** 条件列表；空数组表示无筛选 */
  conditions: ViewFilterCondition[]
}

interface ViewFilterCondition {
  /** 字段名（listFields 中可搜索字段的 canonical name） */
  field: string
  /** 操作符：仅 eq / between */
  op: 'eq' | 'between'
  /** 值；eq 时可为标量或数组（多选字段），between 时为下界 */
  value?: unknown
  /** between 上界 */
  value2?: unknown
}

/** 分组字段列表（有序，最多 3 个）；空数组表示无分组 */
type ViewGroup = string[]
```

**归一化规则（`normalizeFilter`）**：
- `filter` 缺失 / 非对象 / 非法：归一为 `{ logic: 'all', conditions: [] }`。
- `logic` 非 `'all'|'any'`：归一为 `'all'`。
- `conditions` 非数组：归一为 `[]`；逐条过滤非法项——`field` 非字符串丢弃；`op` 非 `'eq'|'between'` 丢弃；`op='between'` 且 `value`/`value2` 均为空丢弃；`op='eq'` 且 `value` 为空（null/undefined/''/空数组）丢弃（`false`/`0` 合法保留，与 `cleanSearchParams` 语义一致）。
- 读取后按当前 `listFields` 可搜索字段集清理未知 `field`（与 `cleanSearchParams` 的合法 key 集同理）。
- **round-trip**：`NamedView` 的未知顶层属性与 `filter` 内部未知扩展字段（如未来算子）在序列化时原样保留；本号只读写 `logic`/`conditions`/`field`/`op`/`value`/`value2` 已知域，禁止删除未知键。

### 3.2 序列化到搜索参数

`filterToSearchParams(filter, fields, keys): Record<string, unknown>`：

| 条件 op | 字段控件类型 | 输出参数 | 说明 |
| --- | --- | --- | --- |
| `eq` | 标量字段 | `{ [field]: value }` | 与搜索表单等值提交一致；`value` 标量化（数组→逗号分隔字符串） |
| `eq` | 多选字段（`SearchField.Multiple` / itemType `multipleselect`） | `{ [field]: 'a,b' }` | 逗号分隔，后端 Multiple 处理 |
| `between` | 数值/日期范围字段 | `{ [field+'_min']: value, [field+'_max']: value2 }` | 与 `_min/_max` 提交一致；仅单侧填值则只输出对应参数 |
| 任一 | — | 结果并入 `cleanSearchParams(…, keys)` | 未知字段/空值在最终请求前再次清理 |

**逻辑合并（AND/OR）**：后端 `Search` 对多个字段参数天然是 AND（`whereExpression &= field.Equal(...)`）。因此：
- `logic='all'`：全部条件参数直接并入 → 后端 AND，语义一致。
- `logic='any'`：后端无法表达跨字段 OR。**处理**：`any` 仅在第一版构建器中保留 UI 语义，应用时若为 `any` 且条件数 > 1，回退为**前端对已加载数据二次过滤**（在 AND 请求结果之上），并在构建器内提示「或(OR) 仅作用于当前页已加载数据」；若条件数 = 1，`any` 与 `all` 等价，直接并入请求。
  - 这是唯一引入前端二次过滤的点，且仅限 `any` 多条件场景，作为明确声明的降级。

## 4. 状态与优先级

`DefaultList` 唯一搜索状态仍是 `effectiveSearch`，新增一个派生来源：

| 条件 | effectiveSearch 来源 | 说明 |
| --- | --- | --- |
| 会话内已点「搜索/重置」 | `cleanSearchParams({...searchForm})` | 表单权威（OSC-0012 现有） |
| 未点搜索 | `baseSearch`（URL → saved → 空） | 现有 |
| 任意时刻 | `{ ...有效搜索, ...filterToSearchParams(viewFilter, fields, keys) }` | **本号新增**：构建器条件叠加，最后应用 |

优先级：`searchForm`（或 baseSearch）为搜索基准，`filter` 条件**覆盖/叠加**同名字段（filter 优先）；最终再经 `cleanSearchParams(…, keys)` 清理未知/空值。`filter` 为空（`conditions: []`）时对请求零影响。

分组是独立于 `effectiveSearch` 的展示状态：`viewGroup = NamedView.group`（字段列表）；对 `tableData` 分组渲染；组折叠状态为**会话内存** `collapsedGroupKeys: Set<string>`（key = 逐级字段值路径），不持久化、不并入请求。切换视图后按新 `group` 重新分组，折叠集清空。

## 5. UI 及交互矩阵

### 5.1 工具栏

| 按钮 | 现状 | 本号 | 禁用条件 |
| --- | --- | --- | --- |
| 筛选 | 切换搜索面板显隐 | 点击展开 `FilterBuilderPopover`（锚定本按钮）；若已有 filter 则显示「已筛选 N 条」标签（点击标签可直接清除） | 无可搜索字段时禁用 |
| 分组 | 占位提示 | 点击展开 `GroupPopover`（锚定本按钮）；若已有 group 则显示「已按 X 分组」标签 | 无分组候选字段时禁用 |
| 搜索 | 切换搜索面板 | 不变 | — |

**展开互斥**：筛选弹层与分组弹层各自独立展开区，同一时刻只展示一个；打开其中一个时自动关闭另一个（共享 `ref` 当前展开者）。点击按钮再次点击或点击外部空白关闭。

### 5.2 FilterBuilderPopover（新增，popover 弹层）

**形态**：`a-popover` 锚定工具栏「筛选」按钮（trigger=click，非受控 `popupVisible` 由父级管理以便与其他弹层互斥）。宽度 420px，无遮罩、不遮挡页面（区别于 Drawer 右滑）。

**DOM/视觉顺序**（弹层内）：标题「筛选」→ 条件组逻辑切换（`且(AND)` / `或(OR)` 单选按钮组）→ 条件行列表（**竖排**，每条件一行）→ 「+ 添加条件」→ 底部操作（重置 / 保存到此视图 / 应用 / 取消）。

**条件行**：`字段下拉` → `操作符下拉` → `值控件` → `删除按钮(×)`，单行横排，行间竖排堆叠。
- 字段下拉：候选 = `searchFields`（GetPage search 分区，排除无操作符可用的纯展示字段）。
- 操作符下拉：`等于`（所有候选字段）；`在范围之间`（仅数值/日期/时间/日期时间范围候选字段，即 `resolveSearchControl` ∈ `numberRange/dateRange/datetimeRange/timeRange` 的字段）。
- 值控件：复用 `SearchFieldInput`（按字段类型渲染 select/Lov/input/date-picker 等）；`between` 用范围控件（`_min/_max`）。
- 空字段/空值条件行不参与应用；用户未填值的条件行以弱化样式显示。
- 条件行较多时弹层内部 `max-height: 320px; overflow-y: auto`。

**交互矩阵**：

| 操作 | 行为 |
| --- | --- |
| 应用 | `emit('apply', viewFilter)` → DefaultList 更新 `viewFilter` → 触发 `loadData()`；关闭弹层 |
| 保存到此视图 | `emit('save', viewFilter)` → store `patchActiveFilter` 持久化；不立即刷新（下次打开/刷新自动应用）；toast 成功 |
| 重置 | 清空全部条件行（保留弹层打开） |
| 清除（工具栏标签） | 清空 `viewFilter` 并 reload，回到无筛选状态 |
| 无 active view | 保存按钮禁用并提示（与 OSC-0012 一致） |
| 关闭 | 点击按钮再次点击 / 点击弹层外空白关闭（`trigger=click` 默认）；关闭不丢弃未应用编辑（下次打开仍保留会话内编辑，除非点「重置」） |

### 5.3 GroupPopover（新增，popover 弹层）

**形态**：`a-popover` 锚定工具栏「分组」按钮（trigger=click），宽度 360px，无遮罩；与筛选弹层互斥（同一时刻只展示一个）。

**DOM/视觉顺序**（弹层内）：标题「分组」→ 已选分组字段有序列表 → 「添加分组字段」下拉 → 底部操作（清除 / 保存到此视图 / 应用 / 取消）。

- 候选字段 = `listFields` 中可分组字段（有 dataSource 的枚举/布尔/单值 Lov/状态字段，及 `groupFieldCandidates` 现有语义）。
- 已选分组字段有序列表，最多 3 个；每项显示字段名 + `上移/下移/删除` 按钮（按钮，非拖拽）。
- 操作：应用（更新 `viewGroup` 并本地重分组）/ 保存到此视图 / 清除 / 取消；关闭不丢弃未应用编辑。

### 5.4 表格分组渲染（ListTable grouped 模式）

- 数据输入：`groupRows(tableData, groupFields, fields, dataSource)` → `GroupNode[]`（组头节点 `{ __group: true, label, count, children, path }`，叶为原数据行）。
- VTable `hierarchy: true` 复用：组头为父节点行（渲染「📁 label (count)」），组内行为 children；`hierarchyExpandLevel: 2` 默认展开一级。
- 组头行点击 = VTable hierarchy 折叠/展开（复用树视图能力）；折叠 key 记录到 `collapsedGroupKeys`（会话内存）。
- 多级分组：一级组头下嵌套二级组头（children 中仍有 `__group` 节点）。
- 空数据：组头不渲染，保持既有 `a-empty`。

### 5.5 QueryInsightPanel 一行折叠

- 搜索字段容器（`a-form-item` 列表）默认固定高度一行（`max-height` + `overflow: hidden`），超出折叠。
- 折叠时底部显示「展开更多 N」（N = 溢出字段数，`offsetHeight > clientHeight` 判定），点击展开全部并变为「收起」。
- 展开状态 `ref` 会话内存；字段增删/视图切换后重置为折叠态。
- 仅当字段数造成溢出时才显示「展开更多」；未溢出不显示。

### 5.6 LovSelect LOV LIST 远程搜索

- LIST 单选/多选下拉：输入关键字 → 调 `fetchLovListData`（携带关键字参数）→ 更新下拉 options；防抖 300ms；空输入显示首页。
- 已选标签：多选模式已选值显示 `a-tag`（现有），增强为可搜索移除；单选用已有 label 显示。
- 保持「更多」表格入口不变；ENUM 模式不变。

## 6. API 约定

**无后端 API 变更**。筛选条件通过既有 `GetList` 查询参数提交：

- `getList(type, params)` 的 `params` 合并 `{ ...effectiveSearch, ...filterToSearchParams(...) }`，走既有 query serializer（数组逗号分隔沿用现有约定）。
- 服务端路径、权限、返回结构不变；不调用 `GetChartData` 相关变更。

## 7. 适用框架与官方资料

- 构建器、弹层、下拉、标签、折叠：[Arco Design Vue](https://arco.design/vue/docs/start)（`a-popover`/`a-select`/`a-tag`/`a-radio-group`）。
- 表格分组行渲染：[VisActor VTable](https://visactor.com/vtable/option/ListTable) hierarchy 配置与[实例接口](https://visactor.com/vtable/api/Methods)（复用既有树视图 hierarchyExpandLevel 经验）。
- 实施前若组件 API 不确定，先查阅上述官方文档，不凭印象补造配置。

## 8. 核心文档影响

| 文档路径 | 影响 | 说明 |
| --- | --- | --- |
| `Doc/Api/ArcoVue企业中后台迁移方案.md` | 修改 | 事实性登记 OSC-0015、筛选构建器/多级分组/搜索面板边界 |
| `Doc/功能清单.md` | 修改 | 更新 SPA 对应实现与测试状态 |
| `NewLife.Cube.ArcoVue/web/README.md` | 修改 | 补筛选构建器、多级分组、搜索面板折叠与 LOV 远程搜索说明 |
| `Doc/附录B_API参考.md` / `Doc/附录C_实体参考.md` | 评估 | 无新 API/实体字段；如无实质变化不补充 |

## 9. 测试设计

| 目标 | 自动化证据 |
| --- | --- |
| ViewFilter 解析 | 缺失/损坏/非法 logic/非法 op/空条件/false/0、多视图隔离、未知字段清理、round-trip 未知键保留 |
| filterToSearchParams | eq 标量/数组/多选逗号分隔、between 单双侧、AND 全并入、any 多条件降级标记、空 filter 零影响、未知 key 清理 |
| groupRows | 单字段/多级分组、计数正确、dataSource 翻译、未分组项、未知分组字段回退、空数据 |
| 折叠 | 「展开更多」溢出判定、展开/收起切换、字段变化重置 |
| store | patchActiveFilter/patchActiveGroup 保存、加载视图自动应用、失败回滚、切换视图隔离 |
| 组件 | FilterBuilderPopover 增删条件/逻辑切换/值控件/保存清除/无 active view/互斥、GroupPopover 有序字段/上限 3/互斥、ListTable 组头渲染/折叠 |
| LovSelect | 远程搜索防抖/关键字参数、已选标签移除、ENUM 模式回归 |

## 10. 风险与回滚

| 风险 | 缓解 |
| --- | --- |
| 后端不支持跨字段 OR | `any` 多条件降级为前端二次过滤并明确提示；单条件等价 AND；文档声明 |
| 后端不支持不等于/包含 | 操作符仅 eq/between；构建器字段/操作符候选由能力矩阵生成，不暴露不可用算子 |
| 分组对大数据量性能 | 仅对当前页已加载数据分组（`tableData`），不跨页；计数为本地计数 |
| filter 与搜索字段冲突 | filter 参数覆盖同名字段并在序列化后统一 `cleanSearchParams`；构建器内展示覆盖提示 |
| JSON round-trip 丢未来字段 | `normalizeFilter` 只读写已知域，未知键保留 |
| 折叠误判 | 用 `offsetHeight > clientHeight` 判定溢出；字段变化重置展开态 |
| 回归既有搜索 | `filter` 为空时请求与 OSC-0012 完全一致；既有搜索单测保持通过 |
