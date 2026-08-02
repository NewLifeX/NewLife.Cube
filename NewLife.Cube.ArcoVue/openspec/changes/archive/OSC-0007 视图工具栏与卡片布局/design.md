# OSC-0007 Design — 视图工具栏与卡片布局

## 1. 设计目标与边界

本号只优化已有多视图列表的前端呈现和前端状态编排。所有数据仍从既有 `GetPage`/`GetList`、导入/导出/删除 API 获取或写入；ViewProfile 的权威数据仍是 `ViewsJson`。

### 1.1 必须保留的图表实现

`DefaultList.vue` 中图表的唯一改动是删除两个模板按钮。以下符号属于冻结区，实施时不得删除、改名、移动到新模块或改变请求参数：

| 文件 | 必须保留的符号/行为 |
|---|---|
| `views/crud/DefaultList.vue` | `ListChartModal` import 与模板挂载 |
| 同上 | `chartVisible`、`chartList` ref |
| 同上 | `openChart()` 函数，仍通过 `cubeApi.page.getChartData(typePath)` 加载后打开 Modal |
| `views/crud/ListChartModal.vue` | 整个组件和既有图表渲染逻辑 |

删除点只有两处：搜索表单内 `@click="openChart"` 的按钮，以及 `.list-topbar` 内 `@click="openChart"` 的文本按钮。

### 1.2 非目标

- 不新增或修改后端路由、DTO、权限枚举、数据库列。
- 不改变详情、编辑、单条删除、导入、导出 API 的 URL 或参数。
- 不为看板、卡片、日历、甘特补充选择列或批量删除。
- 不把历史 `ViewChrome` 字段物理迁移或清洗出 `ViewsJson`。

## 2. 物理改动地图

| 文件 | 改动 | 不应改动 |
|---|---|---|
| `web/src/views/crud/DefaultList.vue` | 高级菜单、表格选择、图表按钮移除、工具栏可见性、旧 chrome 忽略 | 图表状态/API、记录抽屉、数据加载协议 |
| `web/src/views/crud/ViewConfigDrawer.vue` | 「工具栏」名称、视图条件开关、移除 add/custom、卡片布局选择 | 字段排序/冻结、背景/宽高配置 |
| `web/src/core/utils/viewMapping.ts` | `CardLayout`、CardMapping normalize、工具栏/批量删除纯判断 | 看板 dataSource 归一化与分桶实现 |
| `web/src/core/utils/viewMapping.spec.ts` | layout、门禁、长字段辅助函数的新增测试 | 已覆盖的看板别名归并断言 |
| `web/src/features/views/cardHelpers.ts` | `CardBodyField.fullRow` 和文本长度/控件判定 | 图片 URL 解析兼容 |
| `web/src/features/views/CardList.vue` | layout prop 和三类网格 class | 记录循环、主键回退 |
| `web/src/features/views/RecordCard.vue` | layout prop、图片位置、正文跨列 class、语义字体变量 | 双击详情和底部操作事件 |
| `web/src/theme/tokens.ts` | 语义字体 CSS 变量 | 外观、密度、圆角及 Arco theme 计算 |

## 3. 高级菜单与表格选择

### 3.1 状态来源

`DefaultList.vue` 已有如下状态，必须复用而不是新建重复状态：

| 状态 | 来源 | 用途 |
|---|---|---|
| `flags` | `resolveCrudFlags(userStore.getMenuPermission(typePath), pageSetting)` | 任何操作的最终权限来源 |
| `chrome` | 活跃 `NamedView` 经 `resolveChrome()` 归一化 | 视图层允许删除与工具栏开关 |
| `activeViewKind` | 当前 `NamedView.view` | 决定是否是表格视图 |
| `selectedKeys` | `ListTable @selection-change` | 当前已加载表格行的主键集合 |
| `typePath` | 页面 props | 当前实体 API 路径 |

禁止直接检查原始菜单位掩码绕开 `resolveCrudFlags()`，也禁止把选择状态写入 `ViewsJson`。

### 3.2 纯计算契约

在 `viewMapping.ts`（优先）新增可单测的计算函数，名称可为 `resolveBatchDeleteState`。输入/输出必须与以下语义等价：

```ts
type BatchDeleteState = {
  visible: boolean
  disabled: boolean
}

resolveBatchDeleteState({
  viewKind,
  canDelete,
  allowDelete,
  selectedCount,
}): BatchDeleteState
```

| `viewKind === 'table'` | `canDelete` | `allowDelete` | `selectedCount` | `visible` | `disabled` |
|:---:|:---:|:---:|:---:|:---:|:---:|
| 否 | 任意 | 任意 | 任意 | false | true |
| 是 | false | 任意 | 任意 | false | true |
| 是 | true | false | 任意 | false | true |
| 是 | true | true | 0 | true | true |
| 是 | true | true | ≥1 | true | false |

`disabled` 在 `visible=false` 时固定返回 `true`，避免调用方遗漏二次保护。`handleBatchDelete()` 保留首行空选择保护；再增加相同可见/可用门禁的防御性检查，避免模板未来错误触发删除。

### 3.3 菜单结构

列表顶栏右侧只保留一个 `a-dropdown` 触发器「高级」。不再保留 `.page-tools`。菜单内容使用当前组件已导入的 `EXPORT_FORMATS` 和既有 `handleImport`/`handleExport`/`handleBatchDelete`，不复制 API 调用。

| 菜单项 | `visible` | `disabled` | 触发 |
|---|---|---|---|
| 导入 | `flags.canImport` | false | 复用 `a-upload :custom-request="handleImport"` |
| 导出 | `flags.canExport` | false | 二级格式项调用 `handleExport(format)` |
| 批量删除 | `resolveBatchDeleteState(...).visible` | `resolveBatchDeleteState(...).disabled` | 先确认，再 `handleBatchDelete()` |

「高级」触发器仅在上述三项至少一项可见时出现。导入与导出不依赖选择状态；批量删除是唯一依赖 `selectedKeys` 的操作。所有 URL 与 API 参数都使用当前 `typePath`，不得把其他实体的路径缓存进菜单状态。

### 3.4 行选择生命周期

- 当 `activeViewKind === 'table'`，传给 `ListTable` 的 `show-checkbox` 恒为 `true`，不再与删除权限或 `chrome.allowDelete` 绑定。
- `ListTable` 必须保留左侧行 checkbox 和列头全选 checkbox；无需在 `DefaultList` 手写一套选择 UI。
- `onSelectionChange(keys)` 仍是唯一写入 `selectedKeys` 的入口。
- 在 `onSwitchView`、`onCreateView`、`onRemoveView`、`onResetViews`、`watch(typePath)`、成功 `handleBatchDelete` 之前或之后，统一将 `selectedKeys.value = []`。数据 reload 不应把旧页主键误用于新数据。
- 不要求跨分页选择：翻页后选择集合以 `ListTable` 当前加载页的事件结果为准。

## 4. 工具栏与 chrome 兼容

### 4.1 可见性矩阵

`ViewConfigDrawer` 的折叠区标题由“顶部栏”改为“工具栏”。在 `DefaultList` 顶栏及 Drawer 中使用同一视图类型判断，避免“配置可开、页面不显示”或反向漂移。

| 项目 | table | tree | card | kanban | calendar | gantt |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| 筛选 | 显示 | 显示 | 显示 | 显示 | 显示 | 显示 |
| 搜索 | 显示 | 显示 | 显示 | 显示 | 显示 | 显示 |
| 分组 | 显示 | 显示 | 隐藏 | 隐藏 | 隐藏 | 隐藏 |
| 排序 | 显示 | 显示 | 隐藏 | 隐藏 | 隐藏 | 隐藏 |
| 添加记录配置 | 移除 | 移除 | 移除 | 移除 | 移除 | 移除 |
| 自定义按钮配置 | 移除 | 移除 | 移除 | 移除 | 移除 | 移除 |

实现建议使用 `isTableLikeViewKind(kind)` 纯函数，`kind === 'table' || kind === 'tree'` 返回 true。配置抽屉和列表顶栏均调用它；为该函数补测试。

### 4.2 添加和旧字段策略

- 列表新增记录按钮只使用 `flags.canAdd`。移除 `chrome.allowAdd` 和 `chrome.addButtonText` 对运行时的影响；按钮文字固定使用现有“添加记录”。
- 移除 `chrome.customButton` 模板及 `onCustomButton()`；不能以空消息占位替代。
- `ViewChrome` 类型可暂保留 `allowAdd`、`addButtonText`、`customButton` 以兼容已保存的 JSON 和相邻代码。`resolveChrome()` 必须继续容忍它们。
- 更新其它 chrome 字段时，应通过对象扩展保留未知/历史键；不得写迁移脚本或过滤器主动删除历史键。

## 5. 卡片 mapping 与渲染

### 5.1 数据模型及归一化

只扩展卡片 mapping，不扩展后端 DTO、`ViewProfile` 列或独立 `cardJson`：

```ts
export type CardLayout = 'standard' | 'large' | 'row'
export type CardBodyColumns = 1 | 2 | 3
export type CardFieldOrientation = 'vertical' | 'horizontal'

export type CardMapping = {
  kind: 'card'
  titleField: string
  imageField?: string
  layout: CardLayout
  /** 实施期增强：正文字段栅格列数 */
  bodyColumns: CardBodyColumns
  /** 实施期增强：标签/值横排或竖排 */
  fieldOrientation: CardFieldOrientation
}
```

`seedMapping('card', fields)` 必须填 `layout: 'standard'`、`bodyColumns: 2`、`fieldOrientation: 'vertical'`。`normalizeMapping('card', raw, fields)` 的处理顺序如下：

1. 先按现有规则纠正/回退 `titleField` 和 `imageField`。
2. 仅接受 `raw.layout` 为精确字符串 `standard`、`large`、`row`。
3. 缺失、`null`、数组、对象、空字符串或其他字符串一律返回 `layout: 'standard'`。
4. `bodyColumns` 仅接受 1/2/3；非法回落 2；`layout !== 'row'` 时 3 回落 2。
5. `fieldOrientation` 仅接受 `horizontal`，其余回落 `vertical`。
6. 返回对象必须始终含 `layout` / `bodyColumns` / `fieldOrientation`。

`KanbanMapping` 不增加 layout；看板继续使用既有紧凑卡片，避免本号横向扩大范围。

### 5.2 配置抽屉

卡片区在“卡片图片”之后增加“卡片布局”单选/分段选择：

| 值 | 显示名 | 语义 |
|---|---|---|
| `standard` | 标准 | `minmax(260px, 1fr)` 的响应式网格 |
| `large` | 偏大 | `minmax(360px, 1fr)` 的响应式网格 |
| `row` | 整行 | 每行单卡，宽度为容器 100% |

`localMapping` 新增 `layout`，`syncMappingFromProps()` 从 `normalizeMapping()` 回填，`emitMapping()` 对 card 必须带上 layout。布局控件只在 `viewKind === 'card'` 显示。

### 5.3 CardList 与 RecordCard props

- `CardList` 新增 `layout: CardLayout` prop；`DefaultList` 将已归一化的活跃 CardMapping 的 `layout` 传入，mapping 缺失时用 `standard`。
- `CardList` 根元素追加 `card-list--standard` / `card-list--large` / `card-list--row` 之一，不通过行内 style 计算网格。
- `RecordCard` 同样接收 layout，并加 `record-card--row` class；不改变现有 `detail/edit/delete` emit 名称、双击详情或操作按钮权限。

### 5.4 图片位置和响应式

图片位置不保存额外字段，只由 layout 决定：

| layout | 宽度 | DOM/视觉顺序 | 图片尺寸 |
|---|---|---|---|
| standard | 任意 | 标题 → 图片 → 正文 → 操作 | 全宽，140px 高，`object-fit: cover` |
| large | 任意 | 标题 → 图片 → 正文 → 操作 | 全宽，180px 高，`object-fit: cover` |
| row | ≥640px | 左图片；右侧包含标题、正文、操作 | 固定 180px 宽、180px 高 |
| row | <640px | 回退为标题 → 图片 → 正文 → 操作 | 全宽，140px 高 |

当 `imageUrl` 为空时，所有布局都不渲染图片区或图片占位。整行布局可通过 CSS grid/flex 切换，不得复制一套卡片模板。

### 5.5 正文字段和 `fullRow`

在 `cardHelpers.ts` 导出类型，替换现有匿名字段类型：

```ts
export type CardBodyField = {
  key: string
  label: string
  value: string
  fullRow: boolean
}
```

`buildCardBodyFields()` 的字段筛选顺序保持不变：只取 visible 列，跳过 title/image/kanban group，最多 8 项。每个值格式化完成后再判定 `fullRow`：

- `field.itemType` 规范化为小写后是 `textarea`、`multiline`、`richtext`、`html` 时为 true。
- 否则以 `Array.from(value).length >= 33` 判定为 true，按 Unicode 码位计数；`-` 也按普通短值处理。
- 无 FieldMeta 时仅应用长度规则。

`RecordCard` 的字段容器保持两列 grid；`item.fullRow=true` 的元素加 `record-card-field--full` 并 `grid-column: 1 / -1`。标签和值样式保持现有语义，不截断数据。

## 6. 语义字体 Token

在 `buildThemeTokens()` 的 `cssVars` 新增以下变量。假设基础字号 14px，所有字号均乘 `fontScale`；字重不缩放：

| 变量 | 值 | 用途 |
|---|---|---|
| `--cube-font-size-body` | `${14 * scale}px` | 常规正文、按钮 |
| `--cube-font-size-meta` | `${12 * scale}px` | 字段标签、辅助提示 |
| `--cube-font-size-title` | `${16 * scale}px` | 抽屉/区块标题 |
| `--cube-font-weight-normal` | `400` | 常规文本 |
| `--cube-font-weight-medium` | `500` | 卡片标题、工具栏标题 |

本号触及的 CSS 应替换为 `var(--cube-font-size-*)` / `var(--cube-font-weight-*)`；不要求一次性修改未触及组件。不得改变现有 `zoom` 机制。

## 7. 测试设计

### 7.1 Vitest 单测

| 测试目标 | 输入 | 关键断言 |
|---|---|---|
| `normalizeMapping` 卡片布局 | 缺失/非法/合法 layout | 分别回退 standard 或保留合法值；标题/图片既有回退不回归 |
| `resolveBatchDeleteState` | 条件矩阵五行 | 完全匹配 `visible` / `disabled` 矩阵 |
| `isTableLikeViewKind` | 六种 ViewKind | 只有 table/tree 为 true |
| `isCardBodyFieldFullRow`（如抽取） | textarea、32/33 Unicode 码位、无 meta | textarea=true、32=false、33=true、emoji 按码位计 |
| `buildCardBodyFields` | 超过 8 列、排除列、长字段 | 仍最多 8 项，排除正确，`fullRow` 正确 |

### 7.2 构建和手工验证

执行命令、权限场景、旧 JSON 样例及逐步验收见 `verify.md`。实施者必须先通过新增 Vitest，再运行 `pnpm test` 与 `pnpm build`；不得以“仅样式”跳过测试。

## 8. 风险与回滚

| 风险 | 缓解 |
|---|---|
| 选择控件在无删除权限时显得多余 | 这是为高级菜单的后续批处理扩展保留的一致表格能力；菜单仍严格按权限隐藏删除。 |
| 旧 `ViewsJson` 无 layout | 归一化总是给 standard，不做后端迁移。 |
| 历史 chrome add/custom 值影响新 UI | 渲染主动忽略，保存时保留，避免存量 JSON 数据丢失。 |
| 图表被误删造成后续变更缺基座 | AC-02 和文件级冻结清单要求保留全部图表实现。 |
