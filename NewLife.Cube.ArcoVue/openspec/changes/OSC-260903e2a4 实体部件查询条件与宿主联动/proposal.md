# OSC-260903e2a4 — 实体部件查询条件与宿主联动（页面仪表盘 / 首页工作台）

> 本号在已归档 **OSC-2608280e9e（Widget 协议）** 与 **OSC-26082815a1（首页工作台）** 基础上补齐「实体部件自定义查询」：配置部件时可用查询条件编辑器为部件添加常驻过滤条件（可引用当前宿主页字段值），后端按条件返回数据。底层 `WidgetInstance.query.extraFilter` 与 `WidgetQueryService` 已支持，缺的是**配置 UI、保存端校验与宿主值参数化**三段。

## 1. 目标愿景

「页面仪表盘（洞察槽）／首页工作台」的实体部件（`metricCard` / `miniChart` / `miniKanban` / `dataList` / `dataCard`）在添加/编辑时，允许用户用与列表页同款的**查询条件组件**为部件配置过滤条件；保存后每次取数后端按「部件查询条件 ∩ 宿主筛选（如适用）∩ 数据权限」返回，实现「同一个源实体、不同部件各看各的子集」。

- 目标 1：实体部件配置抽屉新增「查询条件」区，**整体复用** `FilterBuilderPopover`（OSC-0015/260830a1b2 同款：条件组 且/或、字段类别操作符、人员/枚举/LOV/数值/日期值控件），保存进 `query.extraFilter`（协议与线缆已存在，零破坏）。
- 目标 2：字段候选 = 源实体 `search ∪ list` 白名单（与后端 `AutomationFilter` 白名单对齐，OSC-260830a1b2），保证保存/查询不因越权字段 400。
- 目标 3：条件值支持「宿主字段引用」（`$host`）：在实体页洞察槽的跨实体部件上，条件值可引用当前宿主页筛选上下文某字段值（如「用户.角色 = 本页当前筛选的 RoleId」），由后端在取数时解析；宿主上下文缺失时按既有「未联动」语义处理，不误伤无宿主的工作台。
- 目标 4：工作台（`/home`、角色模板、命名工作台）无宿主字段概念，仅允许静态条件；后端在保存端强制（含 `$host` → 400），前端隐藏宿主引用入口。
- 目标 5：保存端 `DashboardJson.TryNormalize` 补 `extraFilter` 结构化校验（复杂度上限/字段白名单/操作符/`$host` 合法性），错误在保存即反馈而非查询期 400 后知后觉。

## 2. 为何做

代码现状（2026-09-03 核对）：

| 层 | 现状 | 缺口 |
| --- | --- | --- |
| 协议 | `WidgetInstance.query.extraFilter?: WidgetFilter`（api-core `widget.ts`）已定义 | 无「宿主引用」值形态 |
| 线缆 | `parseDashboardJson/serializeDashboardJson` 原样透传 `query` | 无 |
| 取数 | `useWidgetQuery.buildQueryBody` 已把 `q.extraFilter` 放进 `/Cube/Widget/Query` body | 无 |
| 后端 | `WidgetQueryService.Execute` 已把 `ExtraFilter` 经 `AutomationFilter.TryBuildWhere`（白名单=search∪list）AND 进 Where（租户/DataPermission 之后） | `$host` 对象值会被 `Unwrap` 当作字面 JSON 文本，静默错配 |
| 配置 UI | `WidgetConfigDrawer` 只编辑度量/分组/字段/联动，`save()` 重建 query 时**丢弃** `extraFilter` | 无查询条件编辑入口 |
| 保存校验 | `DashboardJson.TryNormalize` 不校验 `extraFilter` | 无字段白名单/复杂度/`$host` 校验 |

即：端到端数据管道已通但无 UI、无校验、无宿主参数；本号补齐三段并**复用既有条件组件与筛选协议**，不为仪表盘另造一套查询语言。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **范围**：仅实体部件（`source.provider` 以 `entity.` 开头）——洞察槽 `metricCard/miniChart` 与工作台五种 kind。`named` 平台部件数据由服务端提供，不提供查询条件配置。 |
| 2 | **协议零扩展**：复用 `query.extraFilter`（`{logic, conditions[]}`，同 `ViewFilter` 形状）为持久化载体；不新增 DashboardJson 顶层键、不改 `WidgetQueryRequest` 结构、不改 `buildQueryBody` 载荷结构。 |
| 3 | **复用**：条件编辑器**整体复用 `FilterBuilderPopover`**（含 `useFilterBuilderPopover` 与底层纯函数 `filterBuilder.ts`），仅加两个可选 props（`hostFields` 宿主引用开关、`showSaveView` 隐藏「保存到此视图」）；既有调用方（DefaultList）零改动。 |
| 4 | **白名单**：条件字段候选 = 源实体 `search ∪ list`（前端 GetPage/getFields 多源加载，与 a1b2 页面 `filterFields` 同构）；后端维持 `WidgetQueryService.BuildAllowedField`（FieldCollection Search∪List）**不加字段**。越权/未知字段：保存 400 + 查询 400 双保险。 |
| 5 | **值形态**：条件值两类——字面量（现状）与宿主引用对象 `{ "$host": "<宿主字段>" }`。`$host` 仅在实体页洞察槽（有 `hostTypePath` 宿主上下文）开放；工作台三域（个人/角色/命名）保存含 `$host` → 400。 |
| 6 | **联动语义**：`linkFilter`（跨实体等值联动 + 未联动角标）**语义不变、继续独立配置**，向后兼容存量部件；`$host` 是更一般的按条件引用宿主上下文，两者可并存；「未联动」判定扩展为同时考虑 `$host`（见 design §5 矩阵）。 |
| 7 | **复杂度上限**：沿用 `AutomationFilter`——总条件 ≤10、`any` ≤5；不新增上限。对象值必须恰为 `$host` 标记，其它对象值 → 400（防把 JSON 文本当字面值静默错配）。 |
| 8 | **保存端校验**：`DashboardJson.TryNormalize` 增 `hostTypePath` 参数（insight 传当前 typePath、workbench 传 null）并校验 `extraFilter`（结构/白名单/`$host`）；查询端校验保持为最终强制（列日后漂移场景）。 |
| 9 | **前端宿主字段候选**：实体页 `hostTypePath` = 宿主实体的 `search∪list`（复用页面已加载元数据，不重复请求）；工作台宿主候选为空 → 隐藏宿主引用。 |
| 10 | **不做**：不改 `AutomationFilter.TryBuildWhere`（自动化/流程引擎共用，无宿主概念）；不做 SQL/脚本/join 值；不把查询条件当 ACL（行权仍 DataPermission + 租户）；不迁移 Cube.Vue/NaiveUI/CubeNC Razor；不新增后端执行模式。 |

## 4. 做什么（按阶段）

**P1 后端校验与解析**：
1. `Widgets/DashboardJson.cs` `TryNormalize(..., surface)` 增 `hostTypePath` 重载/参数 + `ValidateWidgetFilter`（白名单/复杂度/操作符/`$host` 合法性/对象值拒绝），insight 与模板 PUT 传 `typePath`、工作台各 PUT 传 null。
2. `Widgets/WidgetQueryService.cs` `BuildWhere` 增 `$host` 预解析（从 `req.HostFilter` 等值条件取值）；解析失败的条件跳过并驱动 `hostFilterApplied`；非 `$host` 对象值 400；改写后的 filter 再交 `AutomationFilter.TryBuildWhere`（该方法不改）。
3. `WidgetController` / `CubeController` / `WorkbenchController` 传 `hostTypePath` 给 `TryNormalize`。

**P2 api-core**：
4. `widget.ts` 类型标注 + `WidgetFilter` 宿主引用纯函数（`isHostRefValue` / `hostRefField` / `hasHostRefFilter`）+ Vitest；round-trip 无需改（query 原样透传）。

**P3 ArcoVue 配置 UI（复用）**：
5. `useWidgetConfigDrawer`/`WidgetConfigDrawer.vue`：`draft.extraFilter` 还原/保存；「查询条件」区（摘要 + 打开 FilterBuilderPopover + 清除）；`save()` 合并进 `inst.query.extraFilter`（修复编辑丢条件缺陷）。
6. `listFieldMeta.ts` 扩展 `loadEntityFilterFields(typePath)`（search∪list 多源加载 + 枚举/LOV 富化，复用既有 loader）；同源（insight 源==宿主）直接吃表面注入的宿主字段候选，不重复请求。
7. `FilterBuilderPopover.vue`/`useFilterBuilderPopover.ts` 增可选 `hostFields?: FieldMeta[]`（行值来源「固定值/宿主字段」切换）与 `showSaveView?: boolean`（默认 true）；DefaultList 调用点不动。
8. `InsightPanel`/`context.ts`/`useInsightPanel`：surface 注入宿主 `search∪list` 字段候选（复用页面已计算 `filterFields`）。
9. `legacy.ts isUnlinkedWidget` 与 `useWidgetQuery` 未联动判定扩展（声明 `$host` 视为已声明联动；以响应 `hostFilterApplied` 定角标）。

**P4 文档**：
10. `Doc/功能清单.md` DASH-1/DASH-2 增补、`Doc/Api/核心接口架构.md`（DashboardJson/Widget Query 契约）、`Doc/Api/前端对接指南.md`（Widget 配置消费约定）、`web/README.md` 登记。

## 5. 不做什么

- 不改 `AutomationFilter.TryBuildWhere` / `ViewFilterDto` / `ReadFilterValue`（通用筛选语义不变，自动化/工作流零影响）。
- 不新增后端聚合执行器、不放开 `sql/script/join`。
- 不改变 `linkFilter` 线缆与既有部件行为；存量无 `extraFilter` 配置原样兼容（归一前后不出现 `extraFilter` 键差异）。
- 不做工作台 `$host`（无宿主页）；不给 `named` 平台部件加查询。
- 不把 Widget Query 当字段级 ACL；行权仍是 `DataPermission` + 租户 Where + `Detail`。
- 不做条件「共享/模板/复制到其它部件」（后续另号）。
- 不迁移 Cube.Vue / NaiveUI / CubeNC Razor；不改 CubeNC。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-2608280e9e | `WidgetInstance.query.extraFilter` 协议、`WidgetQueryService`、`DashboardJson.TryNormalize`、`WidgetConfigDrawer` 骨架 |
| OSC-26082815a1 / OSC-260902ef43 | 工作台三域保存路径（个人/角色/命名）与 `surface=workbench` 归一 |
| OSC-0015 / OSC-260830a1b2 | `FilterBuilderPopover` / `filterBuilder.ts` / `ViewFilter` 与后端 search∪list 白名单语义 |
| OSC-0016 | `QueryComboButton` 等查询簇参照（不直接复用，仅语义对齐） |
| 迁移方案 §8.5.2 / §8.5.3 | 部件协议与工作台分层 |

## 7. 测试范围

| 类型 | 是否做 | 说明 |
|------|--------|------|
| XUnit（NewLife.Cube.Tests） | 是 | `DashboardJson.TryNormalize` extraFilter 校验矩阵（白名单/上限/`$host` 表面/对象值拒绝）；`WidgetQueryService` 静态条件与 `$host` 解析矩阵（同源/跨源/缺值/时间窗组合）；控制器传参 |
| Vitest（api-core） | 是 | `isHostRefValue/hostRefField/hasHostRefFilter`；extraFilter round-trip 保留 |
| Vitest（arco-vue） | 是 | `isUnlinkedWidget` 扩展、`useWidgetConfigDrawer` 还原/保存 extraFilter、buildQueryBody 回归（含 extraFilter 不变式） |
| 构建 | 是 | NewLife.Cube + NewLife.CubeNC + @newlifex/api-core + arco-vue（`vue-tsc`/`vite build`） |
| 手工 | 是 | 洞察槽实体部件配置静态/宿主条件、工作台部件静态条件、跨实体缺宿主上下文角标、编辑保留条件、越权字段保存 400 |
| Cube.Vue/NaiveUI/CubeNC 改代码 | 否 | — |

硬门禁：本 OSC 新增单测全过 + 构建无错误；`Osc260828*`/`Osc26082815a1*` 既有 Widget 用例回归全过。

## 8. 成功标准

- [ ] 洞察槽/工作台实体部件可配置查询条件，保存进 `query.extraFilter`，取数按条件过滤（count/group/list 均生效）。
- [ ] 条件编辑器与列表页「查询」组件同一套实现（整体复用），无第二套条件行 UI 漂移。
- [ ] 跨实体部件可配置 `$host` 宿主引用；宿主上下文存在时正确过滤且无「未联动」角标；缺失时回落既有未联动语义。
- [ ] 工作台三域保存含 `$host` 的配置被 400 拒绝；前端不显示宿主引用入口。
- [ ] 越权/未知字段在保存即 400；既有无 `extraFilter` 配置保存/读取与今日一致。
- [ ] 编辑存量实体部件不再丢弃其 `extraFilter`。
