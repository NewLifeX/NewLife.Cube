# OSC-260813c3e9 Design — 页面 TS 抽离与协作编号

实施时按 tasks 勾选推进；不得发明 `design.md` 未写出的文件、符号、交互。

## 0. 前端框架（本号不新增依赖）

| 场景 | 框架 | 官方资料 |
| --- | --- | --- |
| 设计系统 / 壳 / 表单 | Arco Design Vue | https://arco.design/vue/docs/start |
| 图标 | IconPark `@icon-park/vue-next` | https://iconpark.oceanengine.com/official |
| 多维表格 | VisActor VTable | https://visactor.com/vtable/option/ListTable · https://visactor.com/vtable/api/Methods |
| 甘特 | `@visactor/vtable-gantt` | 沿用现有 `GanttView.vue` 已用 option；本号只搬代码不改 option 语义 |

本号 **零 UI 变更**：不改 props 默认值、文案、抽屉宽度、`placement`、工具栏顺序。不建 `ui/`。

## 1. 协作编号（A）

### 1.1 格式

```
OSC-YYMMDDxxxx
```

| 段 | 合法值 | 例 |
| --- | --- | --- |
| 前缀 | 固定 `OSC-` | `OSC-` |
| 日期 | `\d{6}`，创建日 Asia/Shanghai | `260813` |
| 随机 | `[0-9a-f]{4}`，紧接日期、**无**中间 `-` | `c3e9` |

完整正则：

- 新号：`^OSC-\d{6}[0-9a-f]{4}$`
- 历史：`^OSC-\d{4}$`（仅已存在的 0001–0019）

目录名：`{ID}{一个空格}{中文简述}`，例：`OSC-260813c3e9 页面TS抽离与协作编号`。

### 1.2 生成算法（create 必须按此，禁止 max+1）

1. `YYMMDD` ← 今天上海日期。
2. `xxxx` ← 4 位随机小写 hex（PowerShell: `-join ((1..4) | ForEach-Object { '{0:x}' -f (Get-Random -Max 16) })`；或 node `crypto.randomBytes(2).toString('hex')`）。
3. 若 `openspec/changes/` 或 `openspec/changes/archive/` 已有目录名前缀等于该 ID → 回到步骤 2，最多 8 次。
4. 用户给了 `OSC-0020` / `OSC-00xx` → **拒绝**，改按本算法生成，并在回复写新旧对照。

### 1.3 定位算法（approve/apply/verify/retro）

用户给出 ID（新或旧）→ 在 `changes/`（验收/复盘时含 `archive/`）找「目录名以此 ID 为前缀」的文件夹。匹配数 ≠ 1 → 停止询问。

### 1.4 须核对的规范文件（创建期可能已写入）

| 文件 | 必须出现的字面 |
| --- | --- |
| `openspec/agents/openspec-create.agent.md` | `OSC-YYMMDDxxxx`；`禁止` 与 `最大号 +1` 或 `max+1` 同段；**不得**出现 DeepSeek / Flash 一次 1 个 T |
| `openspec/agents/openspec-approve.agent.md` | 新号格式检查项；**无**厂商模型粒度项 |
| `openspec/agents/openspec-apply.agent.md` | 前缀定位；SFC 构薄；**无** DeepSeek 执行粒度节 |
| `openspec/agents/openspec-verify.agent.md` | 前缀定位；`复盘 OSC-YYMMDDxxxx` |
| `openspec/agents/openspec-retro.agent.md` | 前缀定位；归档保持原文件夹名 |
| `openspec/README.md` | 编号规则 + SFC 职责分离 |
| `openspec/changes/README.md` | 新号示例 |
| `openspec/changes/archive/README.md` | 新号 + 历史豁免 |
| `openspec/harness/lessons.md` | 格式改为 `OSC-YYMMDDxxxx`（**勿删**历史 `OSC-00xx` 条目） |
| `ArcoVue企业中后台迁移方案.md` | §9.2 / §10.1 不再写「按落地顺序严格递增」 |

**禁止回退**：不得把 create 改回「未给编号则取 changes 最大号 +1」。

## 2. SFC 构薄规则（B）

### 2.1 `.vue` 允许留下的 script

1. `import` 子组件、composable、类型。
2. `defineProps` / `defineEmits` / `defineExpose` / `defineOptions` / `defineAsyncComponent`（仅用于模板里的异步子组件，如 DefaultList 对 ListTable 的现有写法可留在 `.vue` 或随组装器搬走；**搬与不搬以各 T 清单为准**）。
3. 调用 `useXxx(props, emit)`（或 `useXxx()`）。
4. 把返回值解包给模板（`const { a, b } = useXxx(...)`）。

除 import 与宏外建议 ≤20 行。

### 2.2 `.vue` 禁止（机械扫描）

在 `<script>` 与 `</script>` 之间匹配即违规（allowlist 内的文件除外）：

```
\b(watch|onMounted|onBeforeUnmount|onUnmounted)\s*\(|cubeApi\.
```

`computed(` / `ref(` **不**列入机械扫描（审计文件如 `home/index.vue` 可保留占位 `ref`）。抽离目标文件仍须把业务 `ref`/`computed` 搬进 composable。

### 2.3 构薄模板（复制后只改名字）

```vue
<script setup lang="ts">
import Child from './Child.vue';
import { useFoo } from './useFoo';

const props = defineProps<{ /* 与抽离前完全相同，禁止增删改名 */ }>();
const emit = defineEmits<{ /* 与抽离前完全相同 */ }>();

const {
  /* 仅模板用到的绑定名，名称与抽离前 script 顶层绑定一致 */
} = useFoo(props, emit);
</script>
```

无 `emits` 的组件：`useFoo(props)`。无 props 的：`useFoo()`。

### 2.4 样板（已存在，对照用，本号不改其对外行为）

- `web/src/layouts/useShellAuth.ts` + `RootLayout.vue`（RootLayout 属审计，保持现状）。
- `web/src/features/views/cardHelpers.ts`（纯函数，CardList 抽离后继续 import）。

### 2.5 分类条件矩阵（创建时已量过，执行时禁止重分类）

判定（对 `web/src/**/*.vue`，2026-08-13 快照）：

| 条件 | 动作 |
| --- | --- |
| script 含 `watch(` / `onMounted(` / `cubeApi.` **或** script 行数 > 40 | **抽离** → 同目录 `useXxx.ts`（DefaultList 除外，走 §5） |
| 否则 | **审计**：不新建文件 |

结果见 §4。禁止把审计文件抽成空 composable。禁止把抽离文件改判审计。

## 3. 冻结（禁止改语义）

| 冻结项 | 说明 |
| --- | --- |
| Pinia | `useUserStore` / `useUserProfileStore` / `useViewProfileStore` / `useAppStore` 的 **public 方法与字段名** |
| HTTP | `cubeApi` 方法名、`/api` 与控制器路径拼接方式 |
| DynamicPage | props `type` / `authId`；禁止读 layout/theme store（OSC-0003） |
| RecordDrawer | `placement="right"`；三 mode `add/edit/detail` |
| SectionKey | `useSections.ts` 内已有 key 字符串 |
| View JSON | `ViewsJson` / `FiltersJson` / `FormJson` / `QueriesJson` 字段名与归一化 |
| 图标 | `iconRegistry.ts` 名称 |
| OSC-0018 | 不实现、不改其五件套 |

搬移时：**函数名保持不变**（除非 §5 表另给目标名）。只改所在文件。

## 4. 47 文件地图

路径均相对 `NewLife.Cube.ArcoVue/web/src/`。

### 4.1 抽离（32）

| # | `.vue` | 新建 composable | script 行（约） |
| --- | --- | --- | --- |
| 1 | `views/crud/DefaultList.vue` | §5 共 6 个 ts | 1431 |
| 2 | `features/vtable/ListTable.vue` | `features/vtable/useListTable.ts` | 943 |
| 3 | `views/crud/ViewConfigDrawer.vue` | `views/crud/useViewConfigDrawer.ts` | 560 |
| 4 | `features/views/GanttView.vue` | `features/views/useGanttView.ts` | 533 |
| 5 | `views/crud/RecordDrawer.vue` | `views/crud/useRecordDrawer.ts` | 401 |
| 6 | `features/views/CardList.vue` | `features/views/useCardList.ts` | 230 |
| 7 | `components/LovSelect.vue` | `components/useLovSelect.ts` | 213 |
| 8 | `components/LovSelectTable.vue` | `components/useLovSelectTable.ts` | 187 |
| 9 | `views/crud/FormLayoutDrawer.vue` | `views/crud/useFormLayoutDrawer.ts` | 181 |
| 10 | `features/search/InsightPanel.vue` | `features/search/useInsightPanel.ts` | 176 |
| 11 | `components/CascaderField.vue` | `components/useCascaderField.ts` | 175 |
| 12 | `features/views/CalendarMonth.vue` | `features/views/useCalendarMonth.ts` | 158 |
| 13 | `views/crud/ViewTabsToolbar.vue` | `views/crud/useViewTabsToolbar.ts` | 158 |
| 14 | `views/crud/FilterBuilderPopover.vue` | `views/crud/useFilterBuilderPopover.ts` | 155 |
| 15 | `components/FieldInput.vue` | `components/useFieldInput.ts` | 143 |
| 16 | `views/login/index.vue` | `views/login/useLoginPage.ts` | 123 |
| 17 | `views/login/register.vue` | `views/login/useRegisterPage.ts` | 113 |
| 18 | `components/SearchFieldInput.vue` | `components/useSearchFieldInput.ts` | 101 |
| 19 | `features/search/QueryComboButton.vue` | `features/search/useQueryComboButton.ts` | 96 |
| 20 | `features/views/KanbanBoard.vue` | `features/views/useKanbanBoard.ts` | 95 |
| 21 | `views/login/forgot-password.vue` | `views/login/useForgotPassword.ts` | 94 |
| 22 | `layouts/mix.vue` | `layouts/useMixLayout.ts` | 86 |
| 23 | `views/crud/FormContent.vue` | `views/crud/useFormContent.ts` | 84 |
| 24 | `views/crud/GroupPopover.vue` | `views/crud/useGroupPopover.ts` | 82 |
| 25 | `views/settings/AppearanceDrawer.vue` | `views/settings/useAppearanceDrawer.ts` | 70 |
| 26 | `features/search/SearchDrawer.vue` | `features/search/useSearchDrawer.ts` | 70 |
| 27 | `features/views/RecordCard.vue` | `features/views/useRecordCard.ts` | 68 |
| 28 | `views/settings/appearance.vue` | `views/settings/useAppearancePage.ts` | 68 |
| 29 | `views/crud/NamedViewsToolbar.vue` | `views/crud/useNamedViewsToolbar.ts` | 48 |
| 30 | `views/crud/ListChartModal.vue` | `views/crud/useListChartModal.ts` | 42 |
| 31 | `views/dynamic/DynamicPage.vue` | `views/dynamic/useDynamicPage.ts` | 42 |
| 32 | `components/TagsView.vue` | `components/useTagsView.ts` | 35（含 `watch(`） |

每行（DefaultList 除外）标准搬法：

**T-a**：新建 `useXxx.ts`，把该 `.vue` `<script setup>` 里除「§2.1 允许留下」以外的 **全部** 绑定与函数 **原样剪切** 进 `export function useXxx(props, emit)`（无 emit 则不加第二参）。`return { ...模板用到的顶层绑定 }`。props/emits 类型从原 `defineProps`/`defineEmits` 复制。

**T-b**：编辑 `.vue`：按 §2.3 只留构薄 script；从 `sfcThin.spec.ts` 的 `ALLOWLIST` **删除该相对路径**。

### 4.2 审计（15）— 禁止新建 composable

`App.vue`、`components/RichEditor.vue`、`layouts/default.vue`、`apps/_demo/src/views/demo/echo/index.vue`、`apps/_demo/src/views/demo/echo/ListPageHeader.vue`、`layouts/SidebarMenuNodes.vue`、`components/UserAvatar.vue`、`layouts/LayoutContent.vue`、`layouts/top.vue`、`components/JsonEditor.vue`、`layouts/RootLayout.vue`、`layouts/ShellToolbar.vue`、`components/CommentReplyEditor.vue`、`layouts/side.vue`、`views/home/index.vue`。

审计 T：打开文件，确认无 §2.2 禁止 token（`home/index.vue` 的 `ref(` 允许）。在 status note 写一行 `audit ok: <path> scriptLines=<n>`。不改代码（除非误判含禁止 token——若发现与 §4.2 矛盾，**停止并询问**，禁止自行抽离）。

## 5. DefaultList 拆分（唯一多文件例外）

目录：`web/src/views/crud/`。共享状态 **只创建一次**，禁止四个领域 composable 各自 `ref` 一份。

### 5.1 目标文件

| 新文件 | 职责 |
| --- | --- |
| `listContext.ts` | 全部现有 `ref` / `reactive` / `computed` / 常量（如 `GANTT_PAGE_SIZE_OPTIONS`、`ganttZoomLabels`、`tableHeight`、`exportFormats`） |
| `useListQuery.ts` | 查询 / 分页 / LOV 水合 / 预定义查询 |
| `useListCrud.ts` | 增删改 / 启停 / 导入导出 / 图表打开 |
| `useListViews.ts` | 命名视图 / 筛选分组 / 列排序 chrome mapping / 全屏测高 / 甘特缩放 |
| `useRecordNav.ts` | 抽屉打开与上一条下一条 |
| `useDefaultList.ts` | 组装：`createListContext` + 四个 `use*` + 现有 `watch`/`onMounted`/`onBeforeUnmount`/`bootstrap` |

`DefaultList.vue` 最终只：`defineProps` + `defineAsyncComponent` 子视图 + `const x = useDefaultList(props)`。

### 5.2 `listContext.ts` 必须搬入的绑定（名称一字不改）

从 `DefaultList.vue` script 剪切（约 512–980 行一带的 store/ref/computed，以及 `drawerCanPrev` / `drawerCanNext`）：

`userStore` `profileStore` `evpStore` `typePath` `listFields` `searchFields` `addFields` `editFields` `detailFields` `pageSetting` `pkField` `tableData` `tableDataRaw` `loading` `enableBusy` `selectedKeys` `statData` `labelCache` `configDrawerVisible` `viewState` `pagination` `preferredDefaultView` `preferredPageSize` `pageProfileSize` `effectivePageSizePref` `searchForm` `masterTimeName` `masterTimeDisplayName` `enableKey` `searchTouched` `route` `formModel` `drawerVisible` `drawerMode` `drawerRowIndex` `saving` `chartVisible` `chartList` `tableHeight` `fieldErrors` `exportFormats` `headerSection` `flags` `isAdmin` `showHistoryTabs` `fieldParts` `drawerFields` `drawerFormLayout` `formLayoutDrawerVisible` `metaKeys` `columnTitles` `statLabels` `activeColumns` `activeSort` `activeViewKind` `ganttZoomLevel` `GANTT_PAGE_SIZE_OPTIONS` `ganttZoomLabels` `ganttZoomLabel` `activeViewId` `searchKeys` `urlSearch` `savedSearch` `baseSearch` `effectiveSearch` `insight` `chartData` `chartLoading` `chartError` `activeMapping` `activeCardMapping` `cardListKey` `activeKanbanMapping` `activeCalendarMapping` `activeGanttMapping` `isLargePageView` `effectivePageSize` `showPagerBar` `treeDataDetected` `treeRows` `chrome` `batchDeleteState` `advancedVisible` `searchPanelOpen` `activePopover` `filterPopoverVisible` `groupPopoverVisible` `localFilter` `viewFilter` `localGroup` `viewGroup` `filterFields` `isGrouped` `tableVisibleCount` `displayRows` `listShellStyle` `hasChromeBg` `listSurfaceStyle` `savedQueries` `appliedQueryId` `queryHasParams` `queryParamsDirty` `tablePanelRef` `fullscreen` `measuredTableHeight` `resolvedTableHeight` `tableColumns` `drawerCanPrev` `drawerCanNext`

导出：

```ts
export function createListContext(props: { type: string; authId?: number }) { /* 上表全部 */ }
export type ListContext = ReturnType<typeof createListContext>;
```

`headerSection` 现用的 `defineAsyncComponent` + `getSectionLoader` **一并**进入 `listContext.ts`（需要 `import { defineAsyncComponent, type Component } from 'vue'`）。

### 5.3 函数 → 文件（名称一字不改）

**`useListQuery.ts`** — `export function useListQuery(ctx: ListContext)` 内含：

`hydrateLovLabels` `loadFields` `loadData` `loadChart` `applySearchToForm` `handleSearch` `handleReset` `handleApplyQuery` `handleSaveQuery` `handleRenameQuery` `handleDeleteQuery` `onPageChange` `onPageSizeChange` `onTableScrollBottom`

**`useListCrud.ts`**：

`onTableAction` `onToggleEnable` `updateSingleBooleanField` `handleSave` `handleDelete` `confirmBatchDelete` `handleBatchDelete` `handleExport` `handleImport` `onCardDelete` `onSelectionChange` `openChart`

**`useListViews.ts`**：

`onGanttZoom` `groupLabelOf` `hexToRgba` `measureTableHeight` `onToggleFullscreen` `onKeydown` `observeTableHeight` `renderCell` `loadProfile` `applyWorkspacePrefs` `syncLocalState` `onColumnsChange` `onSortChange` `onConfigSort` `onChromeChange` `onMappingChange` `onGanttMappingChange` `onInsightChange` `onToggleCollapse` `onConfigRename` `onSwitchView` `onCreateView` `onRenameView` `onRemoveView` `onDuplicateView` `onResetViews` `onSaveAsDefault` `onFilterPopoverVisible` `onGroupPopoverVisible` `onFilterApply` `onFilterSave` `onClearFilter` `onGroupApply` `onGroupSave` `onClearGroup`

**`useRecordNav.ts`**：

`clearModel` `findVisibleRowIndex` `loadRecordIntoDrawer` `openAdd` `openEdit` `openDetail` `navigateRecord`

**`useDefaultList.ts`**：

```ts
export function useDefaultList(props: { type: string; authId?: number }) {
  const ctx = createListContext(props);
  const query = useListQuery(ctx);
  const crud = useListCrud(ctx);
  const views = useListViews(ctx);
  const nav = useRecordNav(ctx);
  // 把 DefaultList.vue 里剩余的 watch / onMounted / onBeforeUnmount / bootstrap 原样放这里
  function bootstrap() { /* 原 bootstrap 函数体，内部改调 query/crud/views 已导出函数 */ }
  return { ...ctx, ...query, ...crud, ...views, ...nav };
}
```

领域 composable 若调用另一领域的函数：通过 `ctx` 不放函数；若必须互调（例如 `handleSave` 成功后 `loadData`），在 `useListCrud` 的参数改为 `useListCrud(ctx, { loadData: query.loadData })`。**仅当原代码确有交叉调用时**才加第二参，禁止预先发明事件总线。

交叉调用对照（执行时按 DefaultList 源码核实，下列为 2026-08-13 已知边）：

| 调用方 | 被调 | 处理 |
| --- | --- | --- |
| `handleSave` / 删除 / 启停 / 导入 | `loadData` | `useListCrud(ctx, { loadData })` |
| `openEdit` / `openDetail` | `loadRecordIntoDrawer` | 同文件 `useRecordNav` 内，无需跨文件 |
| `bootstrap` | `loadFields` `loadProfile` `loadData` 等 | 放在 `useDefaultList`，直接用 query/views 返回值 |
| `onSwitchView` | `loadData` 或本地重绘 | 若源码调用 `loadData`，则 `useListViews(ctx, { loadData })` |

### 5.4 DefaultList.vue 搬完后 script 只允许

- vue / 异步子组件 import（`ListTable` `CardList` `KanbanBoard` `CalendarMonth` `GanttView` 的 `defineAsyncComponent`）
- 同步子组件 import（`SearchDrawer` `InsightPanel` `RecordDrawer` 等，与现模板一致）
- `defineProps<{ type: string; authId?: number }>()`
- `const { /* 模板绑定 */ } = useDefaultList(props);`

**保留不动：** `<template>` 与 `<style>` 全文；props 名 `type` `authId`。

## 6. 其它抽离文件的函数清单（剪切，不改名）

执行 T-a 时打开对应 `.vue`，把下列函数（及同文件内未列出但属于 script 的其它 `function`/`const` 逻辑）全部搬进 `useXxx`。下表是 **最低必须搬走** 的具名函数，防止只搬一半。

**ListTable** → `useListTable.ts`：`ensureSeparatorLayer` `clearHeaderSeparators` `updateHeaderSeparators` `onHostMouseMove` `onHostMouseLeave` `rowId` `opsFlags` `opsColumnWidth` `renderOpsLayout` `leadingCount` `buildColumns` `frozenCount` `withChecks` `groupHeaderFormat` `groupHeaderStyle` `groupTitleFormat` `toDataField` `isUpperChar` `buildOption` `syncFromTable` `fieldKey` `bindEvents` `mountTable` `refreshOption` `onTableScroll` `applyRecords`，以及全部 `ref`/`watch`/`onMounted`/`onBeforeUnmount`。宿主：模板里已有的根节点保持；`mountTable` 仍对同一 `ref` 挂 VTable。

**ViewConfigDrawer** → `useViewConfigDrawer.ts`：`currentPrimaryColor` `fieldLabel` `isColorValue` `contrastText` `chipStyle` `togglePanel` `syncMappingFromProps` `syncFromProps` `commitColumns` `displayTitle` `onTitleEdit` `toggleVisible` `toggleFreeze` `onDragStart` `onDrop` `onSortField` `onSortDir` `emitChrome` `emitInsight` `emitMapping` `isBarPresetActive` `pickBarPresetColor` `openBarColorPicker` `onBarColorInput` `onBarColorChange` `clearBarColor` `onCardLayoutChange` `setBodyColumns` `setFieldOrientation` `emitName` `restoreBgDefault` `isBgSwatchSelected` `pickBgSwatch` `onBgColorPick` `setWidth` `setHeight` + 全部状态。

**GanttView** → `useGanttView.ts`：`toDateStr` `fieldLabel` `buildRecords` `startWidthWatch` `stopWidthWatch` `computeTimelineRange` `startResizeWatch` `stopResizeWatch` `buildZoomLevels` `applyZoomLevel` `doSetZoomLevel` `scrollToFirstTask` `mountGantt` `onWheelCapture` + 生命周期。不改 Gantt option 字段含义。

**RecordDrawer** → `useRecordDrawer.ts`：`rawOf` `formatDetail` `detailImageOf` `detailUrlOf` `detailFileOf` `detailTitle` `loadHistory` `onHistoryActionChange` `onHistoryPageChange` `historySuccess` `historyActionLabel` `loadComments` `startCommentReply` `cancelCommentReply` `submitComment` `submitReply` `isReplyTarget` `avatarOf` `canDeleteComment` `removeComment` `onSave` + 状态。`placement="right"` 留在模板。

其余 §4.1 第 6–32 行：该 `.vue` script 内 **全部** 非 §2.1 内容搬入对应 `useXxx`（函数名以源码为准，上表未逐列的不发明新名）。

## 7. sfcThin 门禁（复制实现）

新建 **仅此文件**：`NewLife.Cube.ArcoVue/web/src/core/utils/sfcThin.spec.ts`。

Vitest 已 `include: ['src/**/*.{spec,test}.ts']`，无需改 `vitest.config.ts`。

实现必须包含：

1. `import { readFileSync, readdirSync, statSync } from 'node:fs'` 与 `import { join, relative, sep } from 'node:path'`。
2. 从 `web/src` 递归收集 `.vue`（含 `apps/_demo`）。
3. 对每个文件取 `<script` … `</script>`（无 script 的如 `App.vue` 视为通过）。
4. 若相对路径（posix，`views/crud/DefaultList.vue`）在 `ALLOWLIST` 中 → 跳过断言。
5. 否则：`expect(script).not.toMatch(/\b(watch|onMounted|onBeforeUnmount|onUnmounted)\s*\(|cubeApi\./)`。
6. 另有一条：`ALLOWLIST` 中的路径必须仍存在于磁盘（防止写错路径）。

**初始 `ALLOWLIST`**（posix，相对 `src/`）= §4.1 全部 32 个 `.vue` 路径。

每完成一个 T-b，从数组删除对应路径。最终数组为 `[]`。

**禁止** 加 Vue plugin、禁止 `import` 业务模块（本 spec 只读文件）。

## 8. 测试命令（写死，禁止换包名）

在仓库根 `NewLife.Cube/`：

| 何时 | 命令 | 预期 |
| --- | --- | --- |
| 每个触及 `web/` 的 T 之后 | `pnpm --filter @cube/arco-vue exec vitest run --config vitest.config.ts src/core/utils/sfcThin.spec.ts` | 该 spec 通过（T10 之前尚未有文件则该 T 用下一行） |
| T10 之前的规范-only T | 不跑 Vitest；回复贴出目标文件命中行 | — |
| DefaultList / 大文件 T 之后另加 | `pnpm --filter @cube/arco-vue test` | 既有约 307 条全绿 |
| 全部 T 完成后 | `pnpm --filter @cube/arco-vue test` 与 `pnpm --filter @cube/arco-vue build` | 全绿；`vue-tsc -b && vite build` 无 error |

工作目录：以 `NewLife.Cube` 为根（pnpm workspace）。若 filter 找不到包，改用：

`pnpm --filter ./NewLife.Cube.ArcoVue/web test`

二者等价时优先 filter 包名 `@cube/arco-vue`。

既有 28 个 `*.spec.ts` **禁止删除、禁止改断言意图**（除非本号新 spec）。允许因 import 路径变化做 **最小** import 修正，但本号抽离不改 `core/utils/*.ts` 导出的话通常无需动旧 spec。

## 9. 核心文档影响

| 文件 | 改动 |
| --- | --- |
| `openspec/README.md` | 编号 + SFC 活规则（创建期可能已写） |
| `openspec/agents/openspec-create.agent.md` | 编号算法（创建期可能已写）；无 DeepSeek 专节 |
| 其余四壳 Agent、changes/archive README、lessons 格式、迁移方案 §9.2§10.1§13§14 | 同 A |
| `NewLife.Cube.ArcoVue/web/README.md` | 追加一小节「SFC 职责分离」，指向本号；不删既有 OSC-00xx 能力登记 |

不改 `Doc/功能清单.md`（无用户可见功能）。不改 OSC-0018 目录。

## 10. 测试设计

| 用例 | 输入 | 期望 |
| --- | --- | --- |
| sfcThin 未抽离 | allowlist 含 DefaultList | DefaultList 不触发禁止 token 失败 |
| sfcThin 已抽离 | allowlist 无 DefaultList 且 vue 仍有 `watch(` | 失败 |
| sfcThin 终态 | allowlist `[]` 且 32 文件已构薄 | 通过 |
| 回归 | 全量 vitest | 与抽离前同数通过（允许 +1 文件 +若干 sfcThin cases） |
| 构建 | vue-tsc | 无 error |
| 编号 | 新 Draft 不得为 `OSC-0020` | create 规范禁止 |

手工冒烟（验收）：`Admin/User` 列表加载、翻页、打开右侧抽屉、保存取消、切换 table/card、登录页可提交（不要求真登录成功若后端未起，记环境受限）。
