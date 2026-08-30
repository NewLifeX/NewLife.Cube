# OSC-260830a1b2 — 查询收口与筛选服务端化

## 1. 目标愿景

实体列表只剩一条查询心智：工具栏关键字 Q + 自定义条件（原筛选构建器）编译为后端 Where，并可存成预定义方案；翻页、导出、统计、Insight、看板/日历大 pageSize 看到同一结果集。伪造未下发字段不再构成布尔侧信道。

- 目标 1：`viewFilter` 字段必须属于 GetPage `search ∪ list` 白名单，否则 HTTP 400；Widget Query 同源校验。
- 目标 2：分表/日志类实体在无时间条件时自动收窄近 N 天（默认 30，可关），列表响应头 `X-Cube-Filter-Narrowed` 可被工具栏提示消费。
- 目标 3：退役 `SearchDrawer` 与独立「搜索 / 筛选」工具栏按钮；Q 后接按钮组（现有 `search` 图标）：主按钮按关键字查询，邻钮打开自定义查询；下拉保留预定义查询（勾选使用 / 重命名 / 删除）。预定义同时保存 Q 与自定义条件。
- 目标 4：不改 18 个重写 `Search` 的控制器；用单测定死 XCode `page.State` 透明通道；迁移方案 §8.5.4 / 竞品 / web README 回写本号。

## 2. 为何做

ArcoVue 查询仍是双轨：`SearchDrawer`（含 Q 与 GetPage Search 字段）与工具栏「筛选」构建器并存，心智分裂。收口后 **查询** 是唯一产品面：关键字走 `Q`，字段条件走 `viewFilter`，预定义方案走 `QueriesJson`（升级为同时记住 Q 与条件）。

安全：`AutomationFilter.TryBuildCondition` 只校验字段存在于 `fact.Fields`，未校验是否进入 GetPage 白名单。请求可伪造未下发列（如 `Salary`）做 eq/neq 推断。

实测（2026-08-29）：不必为「全控制器下推」发明 `ApplyRequestFilter`。`SearchData` 已把 `p.State = CreateWhere() & viewExp`；XCode `FindAll(Expression, PageParameter)` 自动 AND `page.State`。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **一个 OSC**：后端白名单/时间窗/startsWith + 前端收口 UI。不拆后端号。 |
| 2 | **不改 18 个重写 Search 的控制器**。不引入 `ApplyRequestFilter`。例外：`id>0` 单条、`EntityTreeController` 缓存 `Match`（须白名单）、`LovController.ListData`（**本号不改**）。 |
| 3 | 白名单外字段 / 条件数>10 / `logic=any` 且 OR 项>5 → **`ApiException(400)`**。无法编译的合法白名单操作符仍 e483：`TryBuildWhere` 返回 null，调用方内存过滤当前页。 |
| 4 | **保留 Q**（`SearchWhereByKeys`）。Q 与 `viewFilter` **AND**。§1.3「连 Q 一起退役」按本号覆盖。 |
| 5 | **保留并升级 QueriesJson**（不删列）。v2 存 `{ id, name, q, filter }`。ArcoVue **继续读写**。v1 `params` 只迁出 `Q`，其余 SearchDrawer 键丢弃。 |
| 6 | 产品文案 **「筛选」改为「查询」**。工具栏不再出现独立「筛选」「搜索」按钮。图标用现有 IconPark **`search`**（已在 `QueryComboButton` / 原搜索按钮使用），**不用** `filter`。 |
| 7 | 工具栏查询簇：`[Q 输入][查询][自定义][▾]` 为连体按钮组（Q 与组间距 8px；组内无缝拼接，复用 `QueryComboButton` 边框重叠样式）。`enableKey===false` 时隐藏 Q，按钮组仍在。 |
| 8 | GetPage `search` 分区保留为可查询字段元数据（值集），不再当抽屉参数模板。 |
| 9 | NamedView.filter 仍作「此视图默认自定义条件」（不含 Q）。预定义是实体级个人方案，含 Q。应用预定义不自动 PUT 视图。 |
| 10 | 皮肤 ArcoVue。`AutomationFilter` CubeNC Link 同源。不改 Cube.Vue。时间窗 `FilterWindowDays` 默认 30，`0` 关闭。 |
| 11 | **不做**：字段矩阵 ACL、值集 `entity:`、DataScope、公式、多列 sorts、用户 SQL、画布、把预定义当权限。 |

## 4. 做什么

**后端：** 同前：白名单、时间窗、startsWith、复杂度 400、透明下推回归。无新查询参数名。

**前端：** 删除 SearchDrawer；Q 提到工具栏；独立「筛选/搜索」按钮去掉；`QueryComboButton` 迁到 Q 后并扩成三键组；自定义查询仍用原 FilterBuilder（标题改「查询」）；QueriesJson v2；startsWith；时间窗提示。

**文档：** §8.5.4 标实施号；预定义保留且与 Q 一并保存；web README。

## 5. 不做什么

- 不改 18 个重写 `Search` 的控制器；不改 `GetList`/`GetPage`/`CreateWhere`/`GetChartData` 签名。
- 不恢复 SearchDrawer / GetPage Search 字段作为查询表单。
- 不把预定义查询改存进 NamedView；不把 NamedView.filter 当权限。
- 不删 `QueriesJson` 列；不强制把旧 `params` 里的 Name/dtStart 转成 viewFilter 条件。
- 不改 Cube.Vue。不做 i18n / 组件测试套件。

## 6. 依赖

| 依赖 | 关系 |
|------|------|
| OSC-0016 | 预定义 QueriesJson + QueryComboButton；本号迁工具栏并改 payload |
| OSC-0015 / OSC-260819e483 | 条件构建器 + viewFilter 下推 |
| OSC-2608280e9e / OSC-26082815a1 | Insight/工作台共用 getList 条件 |
| OSC-2608273d95 | 不阻塞 |
| 迁移方案 §8.5.4 | 本号实施；预定义**不退役**，只退役抽屉双轨 |

## 7. 测试范围

| 类型 | 是否做 | 说明 |
|------|--------|------|
| XUnit | 是 | 白名单 400、时间窗、startsWith、复杂度、透明下推 |
| Vitest | 是 | QueriesJson v2 解析/兼容 v1；保存须含 q 或条件；勾选应用回填 Q+filter；startsWith；时间窗头解析 |
| 构建 | 是 | Cube + CubeNC + arco-vue |
| 手工 | 是 | 按钮组查询/自定义/预定义勾选重命名删除；Q+条件一起保存再应用 |

硬门禁：执行期跑相关单测；验收期本 OSC 新增单测全过 + 构建无错误。

## 8. 成功标准

- [ ] 白名单外字段 → 400。
- [ ] 时间窗 + 前端提示；`FilterWindowDays=0` 关闭。
- [ ] 自定义查询支持「开头是」；条件超限 400。
- [ ] 无 SearchDrawer；无独立「筛选」「搜索」按钮；查询簇用 `search` 图标。
- [ ] 预定义可保存 Q+自定义条件；勾选使用、重命名、删除可用。
- [ ] 透明下推单测钉死；构建无错误；文档回写。
