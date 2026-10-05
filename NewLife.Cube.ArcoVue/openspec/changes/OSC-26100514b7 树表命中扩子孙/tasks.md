# OSC-26100514b7 Tasks — 树表命中扩子孙

## T1 后端扩子孙

- [x] T1-1 新增 `TreeDescendantExpand`：缓存索引 / 分层 BFS、去重、环检测、`canView`、默认封顶 **100000**。
- [x] T1-2 `ReadOnlyEntityController.Index`：`viewKind=tree` 且树实体时对 `SearchData` 命中扩子孙；`OnFillListValues` 之前执行。
- [x] T1-3 `ShouldExpandTreeDescendants`：`EntityTreeController` 派生短路；复用 `GetTreeParentFieldName` / `IEntityTree`。
- [x] T1-4 `GetPage.setting` 增加 `isTreeEntity`。

## T2 前端门控

- [x] T2-1 `useListQuery.loadData`：树表传 `viewKind: 'tree'`；纳入请求签名。
- [x] T2-2 树表跳过会裁掉扩入子孙的 `matchesViewFilter`（或仅过滤种子）。
- [x] T2-3（可选）`canCreateViewKind('tree')` 优先 `pageSetting.isTreeEntity`。

## T3 测试与文档

- [x] T3-1 XUnit：命中扩子孙图用例、行权、环、100000 截断、`viewKind` 门控。
- [x] T3-2 前端 spec：签名含 `viewKind`；非树表不带参。
- [x] T3-3 迁移方案 / 功能清单：树表命中扩子孙；TotalCount=命中数；Data 可大于 pageSize；封顶 100000。
- [x] T3-4 `pnpm exec vue-tsc -b`；相关 Vitest；`dotnet build` / 新增 XUnit 全过。
