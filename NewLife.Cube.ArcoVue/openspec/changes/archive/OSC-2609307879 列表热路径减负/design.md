# OSC-2609307879 Design — 列表热路径减负

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）。表格绘制查阅 VisActor VTable [ListTable 配置](https://visactor.com/vtable/option/ListTable) 与 [实例接口](https://visactor.com/vtable/api/Methods)。图表懒加载只用动态 `import()`，不改图表配置协议。`.vue` 只保留薄 script；本号不新增页面，业务放在已有 `use*.ts` 与 `core/utils`。

缓存选型遵守 XCode `Doc/缓存架构总览.md`：小表用整表实体缓存，按主键查单对象，聚合用字段缓存。本号不引入 DbCache、不新写进程内字典。查询逻辑放在已有 Biz 或控制器动作里，不包 Service。

## 0. 状态唯一来源

| 状态 | 来源 | 禁止 |
| --- | --- | --- |
| 地区叶子显示名 | 列表上下文 `areaLabelCache`；抽屉用 `rowAreaLabels` | 再为每个 ID 发 `getDetail`；不把名称写入视图配置 |
| 用户筛选 | `viewFilter` ref，以及 lastQuery / 预定义查询里保存的那份 | 把日历区间写回这份状态 |
| 日历请求区间 | `loadData` 内由游标和模式算出的临时副本 | 新接口、新分页参数 |
| 页签是否保留 | `tagsView.visited` | 为减负删除页签 |
| 组件是否保活 | `tagsView.cached`，上限 8 | 用 sessionStorage 记保活名单 |
| 角色权限菜单 ID | `Role.FindAllWithCache()` 的 `Resources` | 再包一层静态 `HashSet` |
| 字段下拉字典 | 请求克隆上的 `DataSourceMap` | 写回控制器静态 `ListFields` |

## 1. 线 A — 请求

### 1.1 文件

| 文件 | 改动 | 不动 |
| --- | --- | --- |
| `NewLife.Cube/Areas/Cube/Controllers/AreaController.cs` | 新增 `Names` 与 `ResolveNames` | `Search`、`Map`、`InitAreaData` |
| `packages/api-core/src/api.ts` | `createPageApi` 增加 `areaNames` | `getList` / `getDetail` / `patchFields` 签名 |
| `web/src/core/utils/areaLabels.ts` | 分片与合并纯函数 | `mergeAreaLabel` / `collectCascaderIds` 语义 |
| `web/src/views/crud/useListQuery.ts` | `hydrateAreaLabels`、`hydrateLovLabels`、`loadData` 的请求筛选、GetFields 并行 | `dataSeq` 竞态丢弃 |
| `web/src/views/crud/useRecordDrawer.ts` | `hydrateRowLabels` 的地区分支改 `areaNames` | 非地区 LOV 的 `fetchBatchLabel` |
| `web/src/views/crud/useListCrud.ts` | 布尔走 `patchFields`；成功后按纯函数决定是否 `loadData` | 保存、删除、批量、导入仍 `loadData` |
| `web/src/views/crud/useListViews.ts` | 日历位移、回今天、切模式后调用 `loadData` | 甘特缩放、全屏测量 |
| `web/src/features/views/useCalendarMonth.ts` | 导出窗口起止纯函数 | `+N`、空白新建、周一起始 |
| `web/src/components/useCascaderField.ts` | 不改 | 路径回填仍 `getDetail` |

### 1.2 地区名称

`POST /api/Cube/Area/Names`，`[EntityAuthorize(PermissionFlags.Detail)]`，body：

| 字段 | 类型 | 规则 |
| --- | --- | --- |
| `ids` | `string[]` | 缺省、null 当空数组。去空白、去重（序数、忽略大小写）。空数组返回 `data: {}`，HTTP 200 |
| 元素 | 字符串 | 不能解析为 Int32 的元素忽略，不 400 |
| 数量 | 去重后 | `> 200` 返回 400，消息 `一次最多查询 200 个地区`。不截断 |

实现 `AreaController.ResolveNames(IList<String> ids)`，动作只做绑定与 400 判断：

1. 能解析的 ID 放进集合。
2. `Area.FindAllWithCache()` 里按 ID 取 `Name`。`Name` 为空则省略该键。
3. 缓存没有的 ID 再 `Area.FindAll(Area._.ID.In(missing), null, "ID,Name", 0, 0)`，同样只收录非空 `Name`。
4. 返回 `Dictionary<String,String>`，键为 ID 的十进制字符串。未知 ID 不出现。

不调用 `InitAreaData`。不改 XCode `地区.Biz.cs`。

前端 `areaNames(ids: string[])` 发上述 POST。`hydrateAreaLabels`：

1. `collectCascaderIds` 之后去掉 `areaLabelCache` 已有的键。
2. 剩余 ID 按 200 一片 `Promise.all` 调用 `areaNames`。
3. 每片成功则 `mergeAreaLabel`。任一片失败忽略，不阻断列表（与今天单 ID 失败相同）。
4. 没有剩余 ID 时不发请求。

抽屉 `hydrateRowLabels` 里 `isCascaderField` 的分支改为收集后走同一次（或同一纯函数）`areaNames`，不再 `getDetail('/Cube/Area', id)`。同一抽屉里多个地区字段合并成一次请求。

### 1.3 LOV 缺值

`hydrateLovLabels` 对仍满足 `lovCode` 且 `resolveListControl === 'lov'` 的字段：

- 收集本页值里，`labelCache[code]` 与 `f.dataSource` 都没有的字符串。
- 没有缺失值的字段不请求。
- 有缺失值的字段 `Promise.allSettled` 调 `fetchBatchLabel`。
- 成功后仍回写 `labelCache`、该字段及同 `lovCode` 的详情/编辑/添加分区 `dataSource`。
- 不再使用「`dataSource` 非空则跳过整列」。

枚举字段仍只走 `enrichFieldsWithEnumDataSource`，不进这个循环。

### 1.4 局部更新

纯函数 `shouldReloadAfterPatch(fieldName, sorts, filter)`（放 `web/src/core/utils/patchReload.ts`）：

| 输入 | 结果 |
| --- | --- |
| `fieldName` 为空 | `true`（保守刷新） |
| 任一排序字段名与 `fieldName` 忽略大小写相等 | `true` |
| `filter.conditions` 任一 `field` 与 `fieldName` 忽略大小写相等 | `true` |
| 其余 | `false` |

`onToggleEnable`：

| 字段 | 请求 | 成功后 |
| --- | --- | --- |
| 名为 Enable（忽略大小写） | 现有 `enableSelect` / `disableSelect` | `shouldReloadAfterPatch` 为真才 `loadData` |
| 其它布尔 | `patchFields`，body 只含该字段；`readFieldPatchResult` 的 `fail > 0` 当失败 | 同上 |
| 无 Update 权、无字段、无主键、`enableBusy` | 今天的提前返回 | 不请求 |

失败仍把本地值设回 `oldRaw` 并提示。删除仅被该切换使用的 `updateSingleBooleanField`。

`onKanbanMove` 成功后用分组字段名调用同一纯函数。为假则不 `loadData`。失败回滚与 `fail > 0` 保持今天的行为。

### 1.5 日历区间

`calendarWindow(mode, cursor)` 返回本地壁钟、无时区后缀的字符串。结束为开区间。

| 模式 | `start`（含） | `end`（不含，交给 `lt`） |
| --- | --- | --- |
| month | 当月 1 日 `T00:00:00` | 下月 1 日 `T00:00:00` |
| week | `startOfCalendarWeek` 当天 `T00:00:00` | 其后第 7 日 `T00:00:00` |
| day | 游标当天 `T00:00:00` | 次日 `T00:00:00` |

`withCalendarWindow(filter, startField, fieldType, window)` 返回新对象，不修改入参：

| 条件 | 请求用筛选 |
| --- | --- |
| 非日历视图，或无 `startField` | 原 `filter` |
| `fieldType` 不是 `DateTime` 且不是 `Date` | 原 `filter` |
| `filter.logic === 'any'` 且 `conditions.length > 0` | 原 `filter` |
| 其它 | `{ logic: 'all', conditions: [...原条件, {field, op:'gte', value:start}, {field, op:'lt', value:end}] }` |

`loadData` 只把这个副本传给 `buildViewFilterParam` 和本页 `matchesViewFilter`。`persistLastQuery`、预定义查询、筛选构建器绑定的仍是 `viewFilter` ref。

`onCalendarShift`、`onCalendarToday`、`onCalendarModeChange` 在改游标或模式后，若 `activeViewKind === 'calendar'` 则 `loadData()`。`pageSize` 仍由 `resolveViewPageSize('calendar')` 返回 1000，`pageIndex` 仍为 0。

开始字段不在 search∪list 时后端会 400。前端仅当 `listFields` 或 `searchFields` 含该字段名时才附加区间；否则用原筛选。

### 1.6 GetFields 兜底

`loadFields` 在 GetPage 分区为空时，对 List / Search / Add / Edit / Detail 中仍为空的那些 `getFields` 使用 `Promise.all`。全部回来后再 `enrichFieldsWithEnumDataSource` 与 `enrichFieldsWithLookup`。某一分区失败仍忽略，与今天 `catch` 相同。

## 2. 线 B — 绘制与保活

### 2.1 文件

| 文件 | 改动 | 不动 |
| --- | --- | --- |
| `web/src/views/crud/listContext.ts` | `resolvedTableHeight` 的 `fit` 分支 | `displayRows` 前 100 条、分组仍全量 |
| `web/src/core/utils/themeColor.ts` | 增加一次读取的快照函数 | 现有 `themeColor` 签名保留，供非表格调用 |
| `web/src/features/vtable/useListTable.ts` | `buildOption` 使用快照；格式索引；`withChecks` 复用 | 操作列 `customLayout`、主题 `MutationObserver` 仍触发 `refreshOption` |
| `web/src/core/utils/viewFormat.ts` | 导出按行预计算函数，若已有 `resolveCellFormat` 则复用它 | 规则匹配语义 |
| `web/src/stores/tagsView.ts` | `cached` 上限与触达顺序 | `visited`、`removeView`、`clearAll` |
| `web/src/features/widget/useMonitorChartWidget.ts` | `onDeactivated` 停 Interval，`onActivated` 再开 | 5 秒周期 |
| `web/src/features/widget/useAutoStep.ts` | 同上 | 1 秒步进 |
| `web/src/features/views/useGanttView.ts` | 宽度 `setInterval` 同样暂停 | 甘特数据请求 |
| `web/src/core/utils/echartsTheme.ts` | 去掉顶层 `import * as echarts`，在 `initEcharts` / `ensureEchartsTheme` 内 `import('echarts')` | 主题名解析 |
| `web/src/features/widget/index.ts` | 指标卡、迷你图、监控图、旧图表改为 `defineAsyncComponent` | 其余部件仍同步注册 |
| `web/src/layouts/useShellAuth.ts` | 删除登录后的 `ensureEchartsTheme` | `fetchLoginConfig` |

### 2.2 高度

`measuredTableHeight` 仍是视口内表格可用高度（现有 `measureTableHeight`）。`fit`：

```
content = max(240, 48 + tableData.length * 40)
resolved = min(measuredTableHeight, content)
```

`measuredTableHeight` 尚未测到时（仍是初始 `tableHeight`）用该初始值参与 `min`。`default` 与 `fill` 仍返回 `measuredTableHeight`。不改分页器位置，不改外壳 padding。

短表：宿主变矮，不留出整屏空白。长表：宿主停在视口高度，由 VTable 按容器高度虚拟化。

### 2.3 绘制

`readThemeSnapshot()` 在 `buildOption` 和主题观察器触发的 `refreshOption` 时调用一次，得到普通字符串。单元格 `style` 只读这份快照，不调用 `getComputedStyle`。

行格式：`records` 或 `formatRules` 变化时建 `Map<rowKey, { bg?: string; bold?: boolean }>`。单元格回调用行键查表。无规则时 Map 为空，外观与今天无规则相同。

`withChecks`：记录数组引用与勾选键集合都未变时返回上一次结果。否则仍复制并写入 `__checked`。时间分组的 `withTimeBucket` 排序保持现状。

### 2.4 保活

`CACHE_MAX = 8`。`addView`：

1. `visited` 逻辑不变。
2. 若 `name` 已在 `cached`，把它移到数组末尾（最近使用）。
3. 若不在，推入末尾。
4. 长度大于 8 时从数组头部删除，直到长度为 8。不从 `visited` 删除。

被挤出的页面再次进入时重新挂载，会重新请求列表。这是预期。

`useMonitorChartWidget`、`useAutoStep`、`useGanttView` 的计时器：`onDeactivated` 清除，`onActivated` 按原间隔重新建立。`onBeforeUnmount` 仍清除。组件不在保活树里时，这两个钩子不会误触发。

### 2.5 图表包

`echartsTheme.ts` 不得出现顶层 `echarts` 静态导入。`ensureEchartsTheme` 与 `initEcharts` 内部 `await import('echarts')`。

`registerPlatformWidgets` 对 `MetricCardWidget`、`MiniChartWidget`、`MonitorChartWidget`、`LegacyChartWidget` 使用 `defineAsyncComponent(() => import(...))`。数据列表、快捷链接、收件箱等不引用 echarts 的部件保持同步。

`useShellAuth` 不再在登录配置返回后调用 `ensureEchartsTheme`。对象页 `useDefaultObject` 里已有的调用保留，对象页打开时再加载。

## 3. 线 C — 缓存

### 3.1 文件

| 文件 | 改动 | 不动 |
| --- | --- | --- |
| `NewLife.Cube/Controllers/CubeController.cs` | `MenuTree` 中 `permissionedIds` 改为 `Role.FindAllWithCache()` | `allowedIds` 仍来自当前用户 `Roles` |
| `NewLife.Cube/Workflow/Entity/流程定义.Biz.cs` | `FindPublished` 增加小表分支 | `Publish`、`ToModel` |
| `NewLife.CubeNC/ViewModels/DataField.cs` | `ToDictionary` 在 `DataSourceMap` 非空时直接使用 | `PrepareForApi` 的填充顺序 |
| `NewLife.Cube/Common/ReadOnlyEntityController.cs` | `GetPage` 对 List 只调用一次 `OnGetFields` | `Index`、`SearchData`、匿名策略 |

不改 `NewLife.CubeNC/Common/ReadOnlyEntityController.cs`。SPA 不走 NC。`DataField.cs` 两栈链接同一文件，行为由「map 为空才走委托」保证 MVC。

### 3.2 MenuTree

将：

```csharp
var permissionedIds = Role.FindAll().SelectMany(r => r.Resources).ToArray();
```

改为 `Role.FindAllWithCache()`。`IsAccessible` 规则不变：当前用户资源里有该菜单，或没有任何角色声明过该菜单。多租户门控不变。

`FindAllWithCache` 与 `FindAll` 在测试库上的 `Resources` 并集必须一致，避免菜单变少或变多。

### 3.3 FindPublished

```csharp
if (Meta.Session.Count < 1000)
    return Meta.Cache.FindAll(e => e.TenantId == tenantId && e.TypePath == typePath && e.Enable && e.Published);
return FindAll(_.TenantId == tenantId & _.TypePath == typePath & _.Enable == true & _.Published == true);
```

`typePath` 为空仍返回空列表。不在 `WorkflowPageOverlay` 再缓存。实体 `Update`/`Insert`/`Delete` 会更新实体缓存，发布或停用后下一次列表读到新结果，不另做失效接口。

`Count >= 1000` 的分支保持 SQL，本号不造测试数据去覆盖该分支。

### 3.4 ToDictionary

在现有「委托 → 枚举反射 → DataSourceMap」之前增加：

- `DataSourceMap != null && DataSourceMap.Count > 0` 且字段不是布尔（布尔仍不把 map 当下列，与今天注释一致）：直接把该 map 写入 `dataSource` 后返回该段。
- map 为空或没有：保持今天的委托、枚举、再回退 map。

不在 `ToDictionary` 里给静态字段集合赋值。`PrepareForApi` 仍在请求克隆上填充 map。

### 3.5 GetPage 一次取 List

今天对 List 调用两次 `OnGetFields`（每次内部 `Clone`）。改为：

```csharp
var allList = OnGetFields(ViewKinds.List, null);
FixSearchMapCandidates(allList);
var listFields = allList.Clone();
var list = PrepareFieldsForApi(listFields);
```

其后 `ApplyColumnConfig(list)` 只作用于 `list`。`allList` 仍是未套用户列隐藏的全集，供列设置。`Clone` 是 `MemberwiseClone`，已填充的 `DataSourceMap` 引用共享；`PrepareForApi` 在 map 已有内容时直接返回，不会改共享字典的内容。不要把 `PrepareForApi` 用到静态 `ListFields` 上。

Add/Edit/Detail/Search 仍各调用一次 `PrepareMapViewFields`。

## 4. 条件矩阵（汇总）

| 场景 | 输出 |
| --- | --- |
| 列表无地区列 | 不调用 `Names` |
| 地区 ID 都已在 `areaLabelCache` | 不调用 `Names` |
| `Names` 401/403/400/网络失败 | 单元格保持原始 ID，列表仍展示 |
| LOV 本页值都已翻译 | 该字段不调用 `BatchLabel` |
| 布尔成功且字段不在排序/筛选中 | 不发 `GetList`，本地值保留 |
| 布尔成功且字段在排序或筛选中 | `loadData` |
| 布尔失败 | 本地回滚，不发 `GetList` |
| 日历 + 日期开始字段 + 筛选不是非空 `any` + 字段在 search 或 list | 请求附 `gte`/`lt` |
| 日历 + 用户筛选为非空 `any` | 不附加区间，仍第一页最多 1000 |
| 非日历 | 不附加区间 |
| `fit` 且内容高度小于视口 | 宿主等于内容高度 |
| `fit` 且内容高度大于视口 | 宿主等于视口测量值 |
| `cached` 已有 8 个再打开第 9 个 | `visited` 9 个，`cached` 8 个，最久未激活的名字被去掉 |
| `DataSourceMap` 为空 | `ToDictionary` 仍调用委托 |
| 流程定义表 ≥ 1000 行 | `FindPublished` 走 SQL |

## 5. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| `NewLife.Cube.ArcoVue/ArcoVue企业中后台迁移方案.md` | 日历「固定加载 1000 条」补一句：请求附带当前日/周/月区间；用户筛选为 `any` 且已有条件时不附带。不改导航位置、周起始、空白新建 |
| `Doc/功能清单.md` | 无列表性能条目则不新增章节 |
| XCode 文档 | 不改 |

## 6. 测试设计

| 测试 | 断言 |
| --- | --- |
| `NewLife.Cube.Tests` 中 `AreaNamesTests` | 空 ids → 空字典；未知 ID 省略；非法字符串忽略；201 个去重 ID → 400；缓存命中返回 `Name` |
| `WorkflowDefinitionFindPublishedTests` | 小表：已发布且启用且路径、租户匹配才返回；`Published=false` 保存后不再返回 |
| `DataFieldDictionaryTests` | map 已有一项时委托不被调用；map 为空时委托被调用 |
| `RoleCacheMenuTests` | 测试库上 `FindAllWithCache` 与 `FindAll` 的 Resources 并集相同 |
| `areaLabels.spec.ts` | 201 个 ID 分成 200+1 两片；已缓存 ID 被去掉 |
| `patchReload.spec.ts` | 矩阵：空字段名、排序命中、筛选命中、都不命中 |
| `calendarWindow.spec.ts` 或并入 `calendarDay.spec.ts` | 月/周/日起止；`any` 且有条件时原样返回；`all` 时追加 `gte`/`lt` 且不改入参 |
| `listContext` 或纯函数 `resolveFitHeight` | 内容 200、视口 800 → 200；内容 5000、视口 800 → 800；下限 240 |
| `tagsView` 单测（可抽 `touchCache`） | 第 9 个名字挤掉最久的；再次进入已有名字把它移到末尾 |

命令见 `verify.md`。
