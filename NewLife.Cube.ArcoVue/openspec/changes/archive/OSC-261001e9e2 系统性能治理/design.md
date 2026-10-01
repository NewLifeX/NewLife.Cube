# OSC-261001e9e2 Design — 系统性能治理

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）。洞察区只增加一行文本，不引入新组件。列表绘制不改 VTable 配置。`.vue` 保持薄：`InsightPanel.vue` 只增加一个条件文本节点，判断留在 `useListQuery.ts` 与纯函数。

持久契约：`NewLife.Cube.ArcoVue/web/docs/系统性能治理.md`。本文是落地地图；行为表与该文冲突时，以先改规范文档、再改代码为准。

缓存：不改 XCode。`Entity.*` 继续 `AppendEntityLabels`。不新写 `ConcurrentDictionary`。

## 0. 状态唯一来源

| 状态 | 来源 | 禁止 |
| --- | --- | --- |
| 是否整表刷新 | `shouldReloadAfterWrite` 的返回值，加上视图种类（树强制刷新） | 在保存回调里再写一套字段名比较 |
| 图表数据 | 部件配置与现有 `GetChartData` / `chartOption` | 用当前列表页的行代替；洞察区写「当前页」 |
| 导出行数上限 | `ResolveExportCap` | 在分页循环里另写一个魔法数 |
| `List.*` 翻页或按键 | `PlanListLabelQuery` | 在 `BatchLabel` 里手写第二套页数 |
| 抽屉标签 | 字段 `dataSource` | 再为抽屉做一份与 `labelCache` 平行的字典 |
| 页面元数据 | 现有 `getPageCached` | 本号改 TTL 或加服务端缓存 |

## 1. 文件改动地图

| 文件 | 改动 | 不动 |
| --- | --- | --- |
| `web/docs/系统性能治理.md` | 已在创建时写入。执行时只在行为表需要改字时同步 | 不把暂缓项改成已落地 |
| `web/README.md` | 增加指向该规范的入口 | 字段组件规范一节 |
| `web/src/core/utils/patchReload.ts` | 新增 `shouldReloadAfterWrite`；`shouldReloadAfterPatch` 改为调用它 | 布尔单字段的真值表 |
| `web/src/core/utils/listRowPatch.ts` | 新建。`assignRowFields`、`removeListRow`、`deleteFollowUp` | — |
| `web/src/core/utils/lovHydrate.ts` | 新建。`groupMissingLov` | `missingLovValues` 的过滤语义 |
| `web/src/views/crud/useListCrud.ts` | 编辑保存、流程可写保存、单行删除按矩阵决定 `loadData` 或本地行 | 新增、批量删除、批量启用、导入仍 `loadData`；布尔与看板移动的调用点 |
| `web/src/views/crud/useListQuery.ts` | 不改 | `loadChart`、`loadData` 的筛选、分页、地区与列表 LOV |
| `web/src/views/crud/useRecordDrawer.ts` | `hydrateRowLabels` 的 LOV 循环改为 `groupMissingLov` + `Promise.allSettled` | 地区 `areaNames` 分片 |
| `web/src/features/search/InsightPanel.vue` | 不改。不增加「当前页」 | `useInsightPanel` 的数据 |
| `NewLife.Cube/Areas/Admin/Controllers/LovController.cs` | `List.*` 分支改用 `PlanListLabelQuery` | `Enum.*`、`AppendEntityLabels` |
| `NewLife.Cube/Services/LovLabelQuery.cs` | 新建。`PlanListLabelQuery`、`ListLabelScanPages = 10`、`ListLabelValueCap = 500` | — |
| `NewLife.Cube/Common/ReadOnlyEntityController2.cs` | `ExportData` 用 `ResolveExportCap` | 时间分片与按页导出的内部页大小 20000（它已受 `max` 截断） |
| `NewLife.Cube/Common/ExportCap.cs` | 新建。`ResolveExportCap`、`ExportHardCap = 1_000_000` | — |
| `NewLife.Cube/Setting.cs` 与 `NewLife.CubeNC/Setting.cs` | `MaxExport` 默认值改为 `1_000_000`，注释写明硬顶仍由 `ResolveExportCap` 执行 | `MaxBackup` 与其它设置项 |
| `NewLife.Cube.Vue/web/apps/cube-admin/src/views/admin/cube/index.vue` | 设置表单里 `maxExport` 的初始值改为 `1000000` | `maxBackup` 初始值 |

不改：`useCascaderField.ts`、`EntityController.Patch.cs`、`EntityController.DeleteSelect`、`ReadOnlyEntityController.GetChartData` 的 `PageSize`、`pageMetaCache.ts`、`main.ts` 的 Arco 全量注册、`Index()`。

## 2. 写回矩阵

`watchFields(kind, mapping)`：

| `kind` | `watchFields` |
| --- | --- |
| `kanban` | 非空的 `groupField` |
| `calendar` | 非空的 `startField` |
| `gantt` | 非空的 `plannedStartField`、`plannedEndField`、`actualStartField`、`actualEndField`、`groupField` |
| `tree` | 不使用该函数，调用方直接刷新 |
| `table`、`card` 及其余 | 空数组 |

`shouldReloadAfterPatch(name, sorts, filter)` 等于 `shouldReloadAfterWrite([name], sorts, filter, [])`。空名、空白名仍返回 true。

编辑保存与流程可写字段保存（提交字段集 = 表单里本次提交的字段名，流程路径只含白名单里出现的键）：

| 视图 | `shouldReloadAfterWrite` | 动作 |
| --- | --- | --- |
| `tree` | 不计算 | `loadData` |
| 其它 | true | `loadData` |
| 其它 | false | `assignRowFields` 到 `tableData` 里主键相同的行。找不到行则 `loadData` |

单行删除：

| 视图 | 删后当前页剩余行 | `pageIndex` | 动作 |
| --- | --- | --- | --- |
| `tree` | 任意 | 任意 | `loadData` |
| 其它 | > 0 | 任意 | `removeListRow`；`total > 0` 时 `total - 1` |
| 其它 | 0 | 0 | `removeListRow`；`total > 0` 时 `total - 1` |
| 其它 | 0 | > 0 | `pageIndex - 1`，然后 `loadData` |

`deleteFollowUp(pageIndex, remaining)` 返回 `'local'` 或 `'prevPage'`，供上表后三行使用。树不调用它。

新增、批量删除、批量启用/禁用、导入：一律 `loadData`。失败路径不改行、不减 `total`。

`assignRowFields(row, patch)`：对 `patch` 的每个键，若 `row` 已有该键或 `toPascalAndCamel` 后的键，写入已有键；否则写入原键。键与主键忽略大小写相等时跳过。

## 3. 抽屉缺值

`groupMissingLov(fields, readValue)` 的字段入选条件与列表 `hydrateLovLabels` 相同：有 `lovCode`，且 `resolveListControl(field) === 'lov'`。级联字段由调用方在传入前排除（抽屉现有 `isCascaderField` 分支先跑完）。

每个字段的值用 `missingLovValues([value], undefined, field.dataSource)`。空则该字段不进入分组。同一 `lovCode` 的缺失值去重后合并。返回 `{ code, values, fields }[]`。没有分组时不发请求。

`Promise.allSettled` 后，成功的 `map` 写进该组每个字段的 `dataSource`（保留原键）。失败的组保持原 `dataSource`。

## 4. `List.*` 查询计划

`PlanListLabelQuery(valueField, searchFieldNames, pendingCount, pageable)`：

| 条件 | `mode` | `maxPages` | `pageSize` | `extraValue` |
| --- | --- | --- | --- | --- |
| `pendingCount <= 0` | 调用方不请求 | — | — | — |
| 存在搜索字段名与 `valueField` 忽略大小写相等 | `keyed` | 1 | `min(500, pendingCount)` | 待翻译键逗号拼接（已截断到 500） |
| 其它且 `pageable` | `scan` | 10 | 500 | null |
| 其它且非 `pageable` | `scan` | 1 | `min(500, max(200, pendingCount))` | null |

`BatchLabel` 的 `List.*` 分支：待翻译键去重后 `Take(500)`，按计划调用 `FetchRemoteList`。`keyed` 时 `extraParams` 只增加值字段这一项，命中的行不论原来落在第几页都写入结果。匹配规则与今天相同：行的值字段落在待翻译集合内才写入结果。`scan` 从第 1 页走到待翻译集合清空、或本页行数小于 `pageSize`、或页数达到 10。不得在第 3 页结束导致第 4 页到第 10 页的名称丢失。第 11 页不再请求。

`Enum.*` 与 `ResolveEntityFactory` 命中时的提前返回保持原顺序，不进入计划。

## 5. 图表与导出

不改 `loadChart`、`InsightPanel.vue`、`GetChartData`。洞察区不新增文本。图表继续按部件配置返回：开发者图优先；没有开发者图时使用部件上的 `chartOption`。数据范围来自该配置（全部或自定义条件），不改成当前 `tableData`。

`ResolveExportCap(requested, settingMax)`，硬顶 `ExportHardCap = 1_000_000`：

| `requested` | `settingMax` | 返回 |
| --- | --- | --- |
| > 0 且 ≤ 1000000 | 任意 | `requested` |
| > 1000000 | 任意 | 1000000 |
| ≤ 0 | > 0 且 ≤ 1000000 | `settingMax` |
| ≤ 0 | ≤ 0 或 > 1000000 | 1000000 |

`ExportData` 在读取 `CubeSetting.MaxExport` 之后调用它，用返回值作为 `max`。`NewLife.Cube/Setting.cs` 与 `NewLife.CubeNC/Setting.cs` 的 `MaxExport` 默认值从 `10_000_000` 改为 `1_000_000`，说明文字同步。Cube.Vue 设置页 `maxExport` 初始值从 `10000000` 改为 `1000000`。`MaxBackup` 仍为 `10_000_000`。

## 6. 核心文档影响

| 文档路径 | 影响类型 | 说明 |
| --- | --- | --- |
| `NewLife.Cube.ArcoVue/web/README.md` | 修改 | 增加系统性能治理入口，链到 `docs/系统性能治理.md` |
| `NewLife.Cube.ArcoVue/web/docs/系统性能治理.md` | 新增 | 四条边界、四件事、暂缓、准入。创建草案时已写入 |
| `Doc/功能清单.md` | 无 | 无新菜单、无新接口编码 |
| `Doc/Api/核心接口架构.md` | 无 | 不新增 API。`BatchLabel` 与 `ExportFile` 的路径不变 |
| `ArcoVue企业中后台迁移方案.md` | 无 | 不新开专章。规范以 `web/docs/系统性能治理.md` 为单一事实源 |

## 7. 测试设计

| 测试 | 断言 |
| --- | --- |
| `patchReload.spec.ts` | 空字段名刷新；命中排序、筛选、`watchFields` 刷新；都不命中不刷新。原 `shouldReloadAfterPatch` 用例仍过 |
| `listRowPatch.spec.ts` | 写到已有驼峰键；跳过主键；删行后剩余行与 `'prevPage'` / `'local'` |
| `lovHydrate.spec.ts` | 两个字段同一 `lovCode` 合并；`dataSource` 已有值被丢掉；非 lov 控件不进组 |
| `ExportCapTests` | 上表四行；默认值断言 1000000 |
| `LovLabelQueryTests` | 值字段在搜索字段中 → `keyed` 且 `maxPages=1`；否则 `scan` 且 `maxPages=10`；`pendingCount=0` 时 `maxPages=0` |

`PlanListLabelQuery` 在 `pendingCount<=0` 时返回 `maxPages=0`，`BatchLabel` 看到 0 则保持今天的空结果返回，不调 `FetchRemoteList`。

手工：部门列表编辑一个不在筛选里的名称，网络面板没有新的列表请求，卡片或表格上的名称已变。洞察区没有「当前页」，图表仍按原配置出数。导出一个大表，文件行数不超过 1000000。
