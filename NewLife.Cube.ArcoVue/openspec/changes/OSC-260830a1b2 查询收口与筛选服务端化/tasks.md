# OSC-260830a1b2 Tasks

## 后端（NewLife.Cube）

- [ ] **T1 字段白名单**：`AutomationFilter.TryBuildWhere` / `TryBuildCondition` 增加 `allowedField` 谓词；白名单外字段返回 null（整段放弃）。调用方 `ReadOnlyEntityController2.SearchData`、`EntityTreeController.Search`、`WidgetQueryService` 从 GetPage `search ∪ list` 构造白名单传入。
- [ ] **T2 时序时间窗**：命中分表/Log 实体且条件无时间字段时注入 `updateTime/createTime >= now-30d`；`CubeSetting` 新增 `FilterWindowDays`（默认 30，0 关闭）；响应头 `X-Cube-Filter-Narrowed`。
- [ ] **T3 startsWith 操作符**：后端 `TryBuildCondition` 增加 `startswith`/`notstartswith`（`fi.StartsWith`）；单测断言 `LIKE 'v%'`。
- [ ] **T4 条件复杂度上限**：条件数 >10 或 any-OR>5 放弃下推；长度 4KB 限制保留。
- [ ] **T5 回归单测（透明下推）**：`UserController` / `DepartmentController` 伪造 `viewFilter`，断言服务端过滤 + total 正确。

## 前端（NewLife.Cube.ArcoVue/web）

- [ ] **T6 退役 SearchDrawer / QueriesJson**：删除 `features/search/SearchDrawer.vue`、composable、`QueriesJson` store/工具；移除 `DefaultList.vue` 搜索抽屉入口；`InsightPanel` 保持 WidgetHost。
- [ ] **T7 保留 Q 搜索框**：工具栏保留 Q 输入（下推 `Q` 走 `SearchWhereByKeys`）。
- [ ] **T8 操作符 UI**：`filterBuilder.ts` / `searchFilters.ts` 增 `startsWith`/`notStartsWith`；`matchesViewFilter` 同构；时间窗收窄提示展示。
- [ ] **T9 GetPage Search 分区定位**：`Search` 分区改为可筛字段元数据（含值集候选），不再作查询参数模板；`listContext.ts` `filterFields` 与后端白名单对齐。

## 测试与构建

- [ ] **T10 跑测**：`pnpm test`（前端）、`dotnet test XUnitTest`（后端）相关用例全过。
- [ ] **T11 构建**：`dotnet build NewLife.Cube` 无错误；`pnpm build` 无错误（仅 chunk 体积 warning）。
- [ ] **T12 grep 无残留**：全仓 `SearchDrawer`/`QueriesJson` 为 0。

## 文档

- [ ] **T13** 迁移方案 §8.5.4 标记本号实施；§10.4 #14 标注已解决。
- [ ] **T14** 竞品报告 §6.2 #5、§8.2(1)、§9 路线更新。
- [ ] **T15** web README「通用查询与预定义查询」段更新为筛选单轨描述。

## 测试记录（执行期）

- （待执行）
