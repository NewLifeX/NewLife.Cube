# OSC-0015 Design — 筛选构建器与多级分组

## 1. 目标与契约边界

本号纯前端扩展。`NamedView`（存 `ViewsJson`）新增两个域：`filter`（筛选构建器方案）与 `group`（多级分组字段列表）。**筛选为纯前端过滤**：条件不并入后端请求，对已返回 `tableData` 按筛选条件本地过滤，翻页时对每页已加载数据继续过滤；分组对已返回 `tableData` 纯前端分组展示。**后端零改动**：不新增 API、不改 `Search(Pager)` 语义、不动 ViewProfile 线协议。

**筛选与搜索职责分离**：
- **搜索** = 关键字向后端获取数据的查询条件（搜索面板，`effectiveSearch` 并入请求，OSC-0012 不变）。
- **筛选** = 对后端返回的数据在前端按「筛选条件」本地过滤；翻页时依然使用当前筛选条件对本地数据过滤。

**操作符按字段类别开放**（纯前端匹配，不依赖后端算子）：

| 字段类别 | 可用操作符 | 说明 |
| --- | --- | --- |
| 状态 / 枚举 / 值集 | 等于 / 不等于 / 为空 / 不为空 | Boolean、Enum、dataSource 物化、LOV 值集 |
| 字符 | 等于 / 不等于 / 包含 / 不包含 / 为空 / 不为空 | String 等 |
| 人员（创建者/更新者/创建人员/更新人员） | 等于 / 不等于 | 值控件为**用户实体下拉**（数据源 `/Admin/User`） |
| 数字（整数/小数/浮点/双精度等） | 等于 / 不等于 / 大于 / 大于或等于 / 小于 / 小于或等于 / 为空 / 不为空 | **不含范围** |
| 日期 / 时间 / 日期时间 | 等于 / 晚于 / 早于 / 为空 / 不为空 | 晚于=after、早于=before |

字段类别判定（`resolveFieldFilterKind`）：人员名（Creator/Updater/创建者/更新者…）→ 枚举/值集（Boolean/Enum/dataSource/LOV）→ 数字（数值 typeName）→ 日期时间（DateTime 等）→ 字符。

## 2. 文件级改动地图

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/core/utils/viewProfile.ts` | `NamedView` 增加 `filter?`/`group?`；新增 `ViewFilter`/`ViewFilterCondition` 类型（12 个操作符）与 `normalizeFilter`（宽容解析、非法归一、未知字段保留）；`serializeNamedView`/`parseNamedViews` 透传两个域 | 既有 columns/sort/chrome/mapping/insight 结构与 `_raw` round-trip |
| `web/src/core/utils/filterBuilder.ts` | `resolveFieldFilterKind` 字段类别、`FILTER_OPS_BY_KIND` 操作符矩阵、`FILTER_OP_LABELS`、draft ↔ filter 转换 | — |
| `web/src/core/utils/searchFilters.ts` | `matchesViewFilter` 支持全部新操作符（eq/neq/contains/notContains/isNull/notNull/gt/gte/lt/lte/after/before） | 既有 `cleanSearchParams`/`collectSearchKeys`/`parseUrlSearch` |
| `web/src/core/utils/viewMapping.ts` | 新增 `groupRows(records, groupFields, fields, dataSource)` 与 `GroupNode` 类型 | 既有 bucketKanban/normalizeDataSource 等 |
| `web/src/views/crud/FilterBuilderPopover.vue`（新增） | 条件构建器弹层 UI：字段/操作符（按类别）/值控件（按类别与操作符）/删除、AND-OR 切换、应用/保存/清除；人员字段用户下拉；锚定工具栏「筛选」按钮 | — |
| `web/src/views/crud/GroupPopover.vue`（新增） | 多级分组字段选择弹层（有序增删）；锚定工具栏「分组」按钮 | — |
| `web/src/views/crud/DefaultList.vue` | 「筛选」按钮改为展开 FilterBuilderPopover；「分组」按钮改为展开 GroupPopover；`filterFields`=可见列∪人员字段；loadData 后对已加载数据 `matchesViewFilter` 本地过滤（分页/翻页继续过滤），全量加载时纠正 total；两弹层互斥 | GetPage/字段分区、CRUD、分页、RecordDrawer、既有搜索逻辑 |
| `web/src/stores/viewProfile.ts` | `patchActiveFilter`/`patchActiveGroup` 保存域；加载视图自动应用 | 400ms debounce、失败回滚、既有各域保存 |
| `web/src/features/search/QueryInsightPanel.vue` | 搜索字段容器默认一行、超行折叠「展开更多 N」 | 搜索/重置/保存/清除 emits 与 stat/chart 区 |
| `web/src/features/vtable/ListTable.vue` | 新增 `grouped` 模式：组头行渲染 + 组内行（VTable hierarchy 复用）+ 组头折叠 | 既有 records/columns/hierarchy 表视图语义 |
| `web/src/components/LovSelect.vue` | LOV LIST 模式：下拉支持远程搜索（输入过滤）+ 已选标签 | ENUM 模式与「更多」表格 |
| `web/src/**/*.spec.ts` | 补字段类别/操作符矩阵、matchesViewFilter 全操作符、groupRows、折叠逻辑、组件用例 | 既有测试断言 |
| `NewLife.Cube.ArcoVue/web/README.md`、`Doc/功能清单.md`、`Doc/Api/ArcoVue企业中后台迁移方案.md` | 事实性登记 OSC-0015 | — |

## 3. JSON schema 与兼容

### 3.1 NamedView 新增域

```ts
type ViewFilterOp =
  | 'eq' | 'neq' | 'contains' | 'notContains' | 'isNull' | 'notNull'
  | 'gt' | 'gte' | 'lt' | 'lte' | 'after' | 'before'

interface ViewFilter {
  /** 条件组逻辑：all=且(AND)，any=或(OR) */
  logic: 'all' | 'any'
  /** 条件列表；空数组表示无筛选 */
  conditions: ViewFilterCondition[]
}

interface ViewFilterCondition {
  /** 字段名（filterFields 中 canonical name） */
  field: string
  /** 操作符（按字段类别开放，见 filterBuilder.FILTER_OPS_BY_KIND） */
  op: ViewFilterOp
  /** 值；isNull/notNull 无值；eq/neq 可为标量或数组（多选字段） */
  value?: unknown
  /** 保留字段（历史 between 上界；新操作符不再使用） */
  value2?: unknown
}

/** 分组字段列表（有序，最多 3 个）；空数组表示无分组 */
type ViewGroup = string[]
```

**归一化规则（`normalizeFilter`）**：
- `filter` 缺失 / 非对象 / 非法：归一为 `{ logic: 'all', conditions: [] }`。
- `logic` 非 `'all'|'any'`：归一为 `'all'`。
- `conditions` 非数组：归一为 `[]`；逐条过滤非法项——`field` 非字符串丢弃；`op` 不在 12 个合法集合内丢弃；`isNull`/`notNull` 无值要求；其余操作符 `value` 为空（null/undefined/''/空数组）丢弃（`false`/`0` 合法保留）。
- 读取后按当前 `filterFields` 字段集清理未知 `field`。
- **round-trip**：`NamedView` 的未知顶层属性与 `filter` 内部未知扩展字段（如未来算子）在序列化时原样保留；本号只读写已知域，禁止删除未知键。

### 3.2 纯前端匹配（替代后端并入）

筛选条件**不序列化进请求**。`matchesViewFilter(row, filter, fields)` 对单行匹配：

| 操作符 | 匹配规则 |
| --- | --- |
| eq / neq | 等值宽松比较（数组多选任一命中；枚举字符串值 vs 数字行值按 String 比较） |
| contains / notContains | 字符串包含 |
| isNull / notNull | 行值为 null / '' / 缺失 |
| gt / gte / lt / lte | 数值优先比较，日期/字符串按字典序 |
| after / before | 日期/字符串字典序比较（晚于/早于） |

匹配对 GetList 返回的 camelCase 行做 `getValueByKey` 大小写容错。

**逻辑合并（AND/OR）**：`logic='all'` 要求全部条件命中（`every`）；`logic='any'` 任一命中（`some`）。纯前端匹配，无后端表达限制。

**客户端过滤兜底**：业务重写 `Search(Pager)` 的控制器（如 `Department.Search` 仅处理 `id/parentId/enable/visible`）与树控制器不应用通用等值过滤。因此**只要 `viewFilter` 有条件，前端即对已加载数据做 `matchesViewFilter` 过滤**（eq/neq/contains/notContains/isNull/notNull/gt/gte/lt/lte/after/before 全支持）：重写/树控制器场景使筛选真正生效，普通控制器后端无筛选参数时同样由前端过滤。本页已加载全部数据且发生删减时纠正分页 total 反映过滤结果。

## 4. 状态与优先级

`DefaultList` 搜索状态仍为 `effectiveSearch`（搜索面板条件，OSC-0012 不变）；**筛选为独立前端状态，不并入请求**：

| 状态 | 来源 | 说明 |
| --- | --- | --- |
| 搜索条件 | `effectiveSearch`（表单 / baseSearch） | 关键字向后端取数（OSC-0012 现有） |
| 筛选条件 | `viewFilter`（NamedView.filter，会话/持久化） | **本号新增**：对已加载数据前端过滤，翻页继续过滤 |

筛选与搜索叠加生效：请求按搜索条件取数，返回数据再按筛选条件前端过滤。`filter` 为空（`conditions: []`）时过滤零影响。

分组是独立于筛选/搜索的展示状态：`viewGroup = NamedView.group`（字段列表）；对 `tableData` 分组渲染；组折叠状态为**会话内存** `collapsedGroupKeys: Set<string>`（key = 逐级字段值路径），不持久化、不并入请求。切换视图后按新 `group` 重新分组，折叠集清空。

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
- 字段下拉：候选 = **当前视图可见字段 ∪ 人员字段**（`activeColumns` 可见列 ∩ `listFields`，另加隐藏的创建者/更新者人员字段；排除无操作符可用的纯展示字段）。
- 操作符下拉：按字段类别开放（`FILTER_OPS_BY_KIND`）——枚举/值集：等于/不等于/为空/不为空；字符：等于/不等于/包含/不包含/为空/不为空；人员：等于/不等于；数字：等于/不等于/大于/大于或等于/小于/小于或等于/为空/不为空；日期时间：等于/晚于/早于/为空/不为空。
- 值控件：按类别与操作符渲染——为空/不为空无值控件；人员为**用户实体下拉**（懒加载 `/Admin/User` 前 500 条）；枚举/值集为 dataSource 下拉（无物化 dataSource 的 LOV 用 LovSelect）；数字为输入框；日期时间为日期选择；字符为输入框。
- 空字段/空值条件行不参与应用；用户未填值的条件行以弱化样式显示。
- 条件行较多时弹层内部 `max-height: 320px; overflow-y: auto`。

**交互矩阵**：

| 操作 | 行为 |
| --- | --- |
| 应用 | `emit('apply', viewFilter)` → DefaultList 写 store `patchActiveFilter` **持久化**（刷新/下次打开保留）→ 触发 `loadData()`；关闭弹层 |
| 保存到此视图 | `emit('save', viewFilter)` → store `patchActiveFilter` 持久化；不立即刷新（下次打开/刷新自动应用）；toast 成功 |
| 重置 | 清空全部条件行（保留弹层打开） |
| 清除（工具栏标签） | 清空 `viewFilter`（写空方案持久化）并 reload，回到无筛选状态 |
| 无 active view | 保存按钮禁用并提示（与 OSC-0012 一致） |
| 关闭 | 点击按钮再次点击 / 点击弹层外空白关闭（`trigger=click` 默认）；关闭不丢弃未应用编辑（下次打开仍保留会话内编辑，除非点「重置」） |

### 5.3 GroupPopover（新增，popover 弹层）

**形态**：`a-popover` 锚定工具栏「分组」按钮（trigger=click），宽度 360px，无遮罩；与筛选弹层互斥（同一时刻只展示一个）。

**DOM/视觉顺序**（弹层内）：标题「分组」→ 已选分组字段有序列表 → 「添加分组字段」下拉 → 底部操作（清除 / 保存到此视图 / 应用 / 取消）。

- 候选字段 = `listFields` 中可分组字段（有 dataSource 的枚举/布尔/单值 Lov/状态字段，及 `groupFieldCandidates` 现有语义）。
- 已选分组字段有序列表，最多 3 个；每项显示字段名 + `上移/下移/删除` 按钮（按钮，非拖拽）。
- 操作：应用（写 store `patchActiveGroup` **持久化**并本地重分组）/ 保存到此视图 / 清除（写空方案持久化）/ 取消；关闭不丢弃未应用编辑。

### 5.4 表格分组渲染（ListTable groupBy 模式）

- 数据输入：直接传原始行（`treeRows`/`tableData`），由 VTable `groupConfig.groupBy` 原生分组（参考官方 [list-table-group-checkbox](https://visactor.com/vtable/demo/table-type/list-table-group-checkbox) demo）；不再手工 `groupRows` 组装组头节点。
- `groupConfig`：`groupBy`（分组字段数组，转 camelCase 与数据行字段匹配）、`titleCheckbox: true`（组标题行左侧显示 checkbox，与组内子行选中状态级联同步）、`titleFieldFormat`（组标题文本「📁 label (count)」，label 按分组字段 dataSource 翻译）。
- checkbox 置于 `rowSeriesNumber`（行号列，`cellType/headerType: 'checkbox'`，width 48，format 空串）——位于每行最前面；`enableCheckboxCascade: true` 使组标题 checkbox 与组内子行级联勾选/取消。
- 组标题行样式由 `theme.groupTitleStyle` 定制（浅灰底 #F7F8FA + 加粗），不再依赖组头行 `__groupHeader` 标记。
- **不使用 VTable `hierarchy`（tree）渲染分组**：tree 模式下 VTable 会把 checkbox 列自动置为 tree 列，导致 checkbox 图标不渲染、勾选态与选中集不同步（OSC-0015 分组后勾选框不可用根因）。
- 多级分组：`groupBy` 支持字段数组，VTable 逐级生成组标题。
- 勾选态读取：`checkbox_state_change` 延后到宏任务遍历展示行（`getCellOriginRecord` 取记录 + `getCellCheckboxState` 取状态）——VTable 内部级联监听在 setTimeout(0) 注册，同步读取会拿到级联前旧状态并触发父级清空重置。
- 空数据：组标题不渲染，保持既有 `a-empty`。

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

- 筛选为纯前端过滤：`getList` 仅携带 `effectiveSearch`（搜索条件），筛选条件不并入请求；已加载数据经 `matchesViewFilter` 本地过滤，翻页继续过滤。
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
| matchesViewFilter | eq/neq/contains/notContains/isNull/notNull/gt/gte/lt/lte/after/before 全操作符、all/any 逻辑、camelCase 行容错、空 filter 恒真 |
| groupRows | 单字段/多级分组、计数正确、dataSource 翻译、未分组项、未知分组字段回退、空数据 |
| 折叠 | 「展开更多」溢出判定、展开/收起切换、字段变化重置 |
| store | patchActiveFilter/patchActiveGroup 保存、加载视图自动应用、失败回滚、切换视图隔离 |
| 组件 | FilterBuilderPopover 增删条件/逻辑切换/值控件/保存清除/无 active view/互斥、GroupPopover 有序字段/上限 3/互斥、ListTable 组头渲染/折叠 |
| LovSelect | 远程搜索防抖/关键字参数、已选标签移除、ENUM 模式回归 |

## 10. 风险与回滚

| 风险 | 缓解 |
| --- | --- |
| 后端不支持跨字段 OR | `any` 多条件降级为前端二次过滤并明确提示；单条件等价 AND；文档声明 |
| 后端不支持新算子 | 筛选为纯前端匹配，不依赖后端；操作符按字段类别矩阵开放，不暴露不可用算子 |
| 分组对大数据量性能 | 仅对当前页已加载数据分组（`tableData`），不跨页；计数为本地计数 |
| filter 与搜索字段冲突 | filter 参数覆盖同名字段并在序列化后统一 `cleanSearchParams`；构建器内展示覆盖提示 |
| JSON round-trip 丢未来字段 | `normalizeFilter` 只读写已知域，未知键保留 |
| 折叠误判 | 用 `offsetHeight > clientHeight` 判定溢出；字段变化重置展开态 |
| 回归既有搜索 | `filter` 为空时请求与 OSC-0012 完全一致；既有搜索单测保持通过 |
