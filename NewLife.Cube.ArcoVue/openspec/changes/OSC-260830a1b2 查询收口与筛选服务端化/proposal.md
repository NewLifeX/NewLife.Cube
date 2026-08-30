# OSC-260830a1b2 — 查询收口与筛选服务端化

## 1. 为何做

ArcoVue 当前查询是**双轨**：`SearchDrawer`（Q / dtStart / GetPage Search 字段）+ `QueriesJson` 预定义查询，与筛选构建器（`NamedView.filter` → `viewFilter`）并存。迁移方案 §8.5.4 已拍板「只留筛选、全部走后端」，但尚未实施。

同时，筛选服务端化的**安全边界缺失**：`AutomationFilter.TryBuildCondition` 只校验字段存在于 `fact.Fields`，未校验该字段是否进入 `GetPage` 白名单（`search ∪ list`）——请求可伪造任意实体字段（如未下发的 `Salary`）做 eq/neq 推断，构成布尔侧信道泄露。

收口后筛选成为唯一查询入口，翻页/导出/统计/Insight/看板共用同一条件集，性能策略必须同步落地。

## 2. 范围（做）

> 关键前提（2026-08-29 实测）：XCode `Entity.FindAll(Expression, PageParameter)` 自动把 `page.State`（Expression/WhereBuilder）AND 进 where；而 `SearchData` 已把 `p.State = CreateWhere() & viewExp`。18 个重写 `Search` 的控制器（User/Log/Role/Department/Tenant 等）终点均落在 `FindAll(exp, page)`，**与 DataPermission 同一条透明通道，无需改造任何控制器**。仅 `id>0` 单条直达（语义合理）、`EntityTreeController` 走缓存内存 `Match`（已专门处理）、`LovController.ListData` 值集场景（非实体列表）三条例外无需处理。

### 后端（`NewLife.Cube`）

| # | 内容 | 文件 |
|---|------|------|
| 1 | **字段白名单**：`AutomationFilter` 增加白名单参数，`TryBuildCondition` 校验字段 ∈ `GetPage search ∪ list`；白名单外字段拒绝（删条件或抛 400）。调用方（`SearchData` / `EntityTreeController` / Widget 查询）传入白名单 | `Automation/AutomationFilter.cs`、调用点 |
| 2 | **性能保障：时序实体时间窗**：命中分表/日志类实体（`DataScale`/Log 实体）且条件无时间字段时，编译器自动注入近 30 天窗口（可配置）；响应头 `X-Cube-Filter-Narrowed` 告知前端已收窄 | `AutomationFilter.cs` |
| 3 | **操作符扩展**：新增 `startsWith`（编译为 `LIKE 'v%'` 可走索引）；`contains` 保留但限小表 | `AutomationFilter.cs` + 前端 |
| 4 | **条件复杂度上限**：条件数 ≤10；`logic=any` OR 项数 ≤5 | `AutomationFilter.cs` |
| 5 | **回归单测**：断言重写 Search 的 User/Department 控制器伪造 `viewFilter` 请求被服务端过滤且 total 正确（防绕过 `FindAll` 的自定义查询回潮） | `XUnitTest` |

### 前端（`NewLife.Cube.ArcoVue/web`）

| # | 内容 | 文件 |
|---|------|------|
| 6 | **退役 SearchDrawer / QueriesJson**：移除 `SearchDrawer.vue`、`QueriesJson` 相关 store/工具；筛选构建器为唯一条件入口 | `features/search/SearchDrawer.vue`、`stores/viewProfile.ts`、`core/utils/viewProfile.ts` |
| 7 | **保留 Q 轻量搜索框**：筛选之外保留工具栏 Q 输入（下推 `Q` 参数走既有 `SearchWhereByKeys`），不丢全文搜索 | `DefaultList.vue` 工具栏 |
| 8 | **操作符 UI 适配**：筛选构建器增加「开头是」操作符；时间窗收窄时展示提示 | `core/utils/filterBuilder.ts` |
| 9 | **GetPage Search 分区定位**：将 `Search` 分区改为可筛字段元数据（含值集候选），不再作为查询参数模板 | `listContext.ts` / 元数据消费 |

## 3. 不做什么

- **不改 18 个重写 `Search` 的控制器**（XCode State 透明通道已生效，见前提）。
- 不做字段级权限（视图级 ACL、按角色藏列）——另行大 OSC。
- 不新增 `sorts` 多列排序、不做公式字段、不做画布编排运行时（§8.2.6 非目标）。
- 不把 `NamedView.filter` 当权限；不放宽 `DataPermission` 与租户 Where（始终先于用户筛选）。
- 不做移动端 / i18n / 组件测试（各自独立项）。
- 不引入用户自定义 SQL / 跨实体查询引擎。

## 4. 依赖

| 依赖 | 关系 |
|------|------|
| OSC-260819e483 | Done：viewFilter 下推 + 筛选构建器已接线；本号在其上收口 |
| OSC-2608280e9e / OSC-26082815a1 | Done：InsightPanel=WidgetHost + `/home` 工作台；本号查询收口需保证其共用的 `getList` 条件集一致 |
| OSC-260815fa86 | Done：实体自动化；`automationGraph.ts` 的 Filter 已同构，需随操作符扩展同步 |
| 迁移方案 §8.5.4 | 本号即其实施；方案文已回写实测发现（v2026-08-29b） |

## 5. 验收 / 测试范围

| 类型 | 是否做 | 说明 |
|------|--------|------|
| XUnitTest | **是** | 白名单拒绝、时间窗注入、startsWith 编译、条件数上限、透明下推回归（User/Department） |
| Vitest | **是** | filterBuilder 操作符、时间窗提示、SearchDrawer 移除后无残留引用 |
| dotnet build | **是** | `NewLife.Cube` 无错误 |
| 前端构建 | **是** | `vue-tsc + vite build` 无错误 |
| 文档 | 是 | 迁移方案 §3.1 / §8.5.4、竞品报告 §6.2 / §8.2 / §9、web README |
| Playwright E2E | 是 | 筛选→翻页/导出条件一致冒烟（可选，环境受限则记录） |

硬门禁：触及前后端代码 → 执行期必须跑相关单测；验收期本 OSC 新增单测全过 + 构建无错误。

## 6. 成功标准

- [ ] `viewFilter` 条件字段仅限 `GetPage search ∪ list`，白名单外字段被拒绝
- [ ] 时序实体无时间条件筛选自动注入 30 天窗口，且前端可见收窄提示
- [ ] 筛选构建器支持「开头是」操作符；条件数超限被拒绝
- [ ] SearchDrawer / QueriesJson 已移除，无残留引用；Q 搜索框保留
- [ ] 18 个重写 Search 的控制器对 viewFilter 透明下推经单测钉死
- [ ] 本 OSC 新增单测全过；`NewLife.Cube` 与前端构建无错误
- [ ] 迁移方案 / 竞品报告 / web README 已回写（或 verify 说明无新编码号段）
