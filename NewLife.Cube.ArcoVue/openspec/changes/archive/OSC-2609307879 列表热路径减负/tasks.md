# OSC-2609307879 Tasks

## A. 请求

- [x] T1 `AreaController.ResolveNames` + `POST Names`：空、去重、非数字忽略、超过 200 返回 400、缓存命中与 `ID.In` 回落。`AreaNamesTests` 覆盖 design §1.2。
  - 验收补录：固定主键插入前先删同 ID，断言后清理，避免 Membership 连接未换成内存库时第二次跑测试撞唯一键。
- [x] T2 `createPageApi.areaNames`；`areaLabels` 分片纯函数；`hydrateAreaLabels` 与抽屉地区分支改为该接口，失败忽略。
- [x] T3 `hydrateLovLabels` 只提交缺失值，字段间 `Promise.allSettled`。补 vitest：已有 `dataSource` 时新值仍请求。
- [x] T4 `patchReload.ts` + spec。`onToggleEnable` 的非 Enable 布尔改 `patchFields`；Enable 仍启停接口；成功后按纯函数决定 `loadData`。删除 `updateSingleBooleanField`。
- [x] T5 `onKanbanMove` 成功后调用同一纯函数。
- [x] T6 `calendarWindow` / `withCalendarWindow` + spec。`loadData` 使用副本；`logic=any` 且已有条件、非日期字段、字段不在 search/list 时不附加。导航三个处理函数在日历视图下调用 `loadData`。
- [x] T7 `loadFields` 中空分区的 `getFields` 改为 `Promise.all`。

## B. 绘制与保活

- [x] T8 抽出 `resolveFitHeight` 并用于 `resolvedTableHeight` 的 `fit` 分支。spec 覆盖短表与长表。
- [x] T9 `readThemeSnapshot`；`useListTable` 的 `style` 不再调用 `getComputedStyle`。格式规则按行预计算。`withChecks` 在记录引用与勾选集合都未变时复用上一份。
- [x] T10 `tagsView`：`cached` 上限 8，已存在的名字移到末尾。spec。`visited` 不删。
- [x] T11 `useMonitorChartWidget`、`useAutoStep`、`useGanttView` 在 `onDeactivated` 停表、`onActivated` 恢复，`onBeforeUnmount` 仍清理。
- [x] T12 `echartsTheme.ts` 改为函数内 `import('echarts')`。四个图表部件 `defineAsyncComponent`。`useShellAuth` 去掉登录后的 `ensureEchartsTheme`。

## C. 缓存

- [x] T13 `MenuTree` 的 `permissionedIds` 改为 `Role.FindAllWithCache()`。`RoleCacheMenuTests` 断言与 `FindAll` 的 Resources 并集一致。
- [x] T14 `FindPublished` 小表走 `Meta.Cache.FindAll`，否则保持 SQL。`WorkflowDefinitionFindPublishedTests` 覆盖发布过滤与停用后消失。
- [x] T15 `ToDictionary` 在非布尔且 `DataSourceMap` 非空时不调用委托。`DataFieldDictionaryTests`。map 为空时委托仍被调用。
- [x] T16 `GetPage` 对 List 只 `OnGetFields` 一次，再 `Clone` 出列设置用的全集与要 `PrepareFieldsForApi` 的那份。`ApplyColumnConfig` 只作用于后者。

## D. 文档与门禁

- [x] T17 迁移方案里日历「固定加载 1000 条」补上区间附加规则（含 `any` 例外）。功能清单无对应条目则不新增。
- [x] T18 跑 verify.md 中的新增单测与 `dotnet build NewLife.Cube`、`npx vue-tsc --noEmit`。失败则修到通过。手工冒烟留验收阶段时可在本项注明未跑。
  - 单测与 `dotnet build`、`vue-tsc --noEmit` 已通过。浏览器与接口冒烟未跑，留验收。
