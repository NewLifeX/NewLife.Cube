# OSC-260903e2a4 Tasks

> 顺序：后端校验/解析 → api-core → ArcoVue 配置 UI → 联动判定 → 文档 → 测试构建。批准前不写业务代码。
> 硬门禁：新增 XUnit/Vitest 全过 + 构建无错误；`Osc260828*` / `Osc26082815a1*` Widget 回归全过。
> **2026-09-03 执行记录**：P1–P7 全部完成；XUnit `Osc260903WidgetQueryTests` 13 绿 + 既有 Osc260828/15a1/ef43/a1b2 回归 39 绿；api-core Vitest 51 绿；arco-vue Vitest 834 绿；NewLife.Cube 多 TFM 构建 0 error；vue-tsc/vite build 通过。宿主冒烟待 CubeDemo 宿主（手工清单见 verify.md）。

## P1 后端：保存端校验

- [x] `Widgets/DashboardJson.cs`：`TryNormalize` 增 6 参 `hostTypePath`（旧 4/5 参重载委托 null）；新增 `ValidateWidgetFilter`（logic/数量上限/源实体 search∪list 白名单/op/`$host` 表面与宿主字段/对象值拒绝）
- [x] `Controllers/CubeController.cs`：`ViewProfile` 与 `ViewProfileTemplate` PUT 传 `typePath` 给 `TryNormalize`
- [x] `Controllers/WorkbenchController.cs`：个人 / Role / Named 各 PUT 继续走 5 参重载（委托 `hostTypePath: null`，工作台禁 `$host`）
- [x] `WidgetQueryService.BuildAllowedField` 提取内部可复用 `GetAllowedNames(fact)` 供 DashboardJson 复用；`ResolveEntityType` → `FindEntityType`

## P2 后端：取数解析

- [x] `Widgets/WidgetQueryService.cs` `BuildWhere`：`ExtraFilter` 分支 `ResolveHostRefs`（从 `req.HostFilter` 等值条件解析 `$host`；克隆不写回原始 DTO，时间窗沿用原始 ExtraFilter）
- [x] 非 `$host` 对象值 → 400「筛选值无效」；`$host` 成功应用 → `hostApplied=true`；全部失败且无字面 → `hostFilterApplied=false`（未联动）
- [x] 时间窗判定沿用原始 ExtraFilter（未引入新行为）；net6 兼容（`JsonValue.TryGetValue<String>` 替代 `GetValueKind`）

## P3 api-core

- [x] `src/widget.ts`：类型注明 + `HOST_REF_KEY` / `isHostRefValue` / `hostRefField` / `hasHostRefFilter`
- [x] `src/widget.spec.ts`：纯函数边界 + extraFilter（字面/$host）round-trip 保留用例（51 全绿）

## P4 ArcoVue 配置 UI（复用 FilterBuilderPopover）

- [x] `views/crud/FilterBuilderPopover.vue` + `useFilterBuilderPopover.ts`：可选 `hostFields?`（行值来源 值/宿主 切换）、`showSaveView?`（false 隐藏「保存到此视图」）；DefaultList 调用零改动
- [x] `core/utils/viewProfile.ts` `normalizeFilter`：核对**无需改**（对象值本就原样透传，`$host` 天然兼容）
- [x] `features/widget/listFieldMeta.ts`：新增 `loadEntityFilterFields(typePath)`（GetPage search/list + GetFields + Automation/Meta 多源合并富化）
- [x] `features/widget/useWidgetConfigDrawer.ts` + `WidgetConfigDrawer.vue`：`draft.extraFilter` 还原/保存；「查询条件」区（条件数/编辑弹层/清除）；`save()` 合并保留 extraFilter（修复编辑丢条件）
- [x] `context.ts` / `InsightPanel.vue` / `useInsightPanel.ts` / `DefaultList.vue` / `WidgetHost.vue`：表面注入 `hostFilterFields`（宿主页 search∪list）

## P5 联动判定与运行时

- [x] `legacy.ts isUnlinkedWidget` 扩展（声明 `$host` 视为已声明联动）
- [x] `useWidgetQuery.ts` `unlinkedAfterQuery`（导出）：声明联动 → 以 `hostFilterApplied` 定角标；widget.spec 新用例 3 组

## P6 文档

- [x] `Doc/功能清单.md` DASH-1/DASH-2 增补 + 新增 DASH-4 行
- [x] `Doc/Api/核心接口架构.md`（Widget Query extraFilter `$host` + DashboardJson 保存校验）
- [x] `Doc/Api/前端对接指南.md`（Widget 配置：查询条件/宿主引用/白名单）
- [x] `web/README.md` 登记

## P7 测试与门禁

- [x] XUnit `Osc260903WidgetQueryTests`（13 用例全绿：DashboardJson 校验 7 + Query $host 6）
- [x] 回归：`Osc260828`（Widget/Workbench）/`Osc260902ef43`/`Osc260830` 全绿（39）
- [x] Vitest：api-core 51 + arco-vue 834 全绿
- [x] 构建：NewLife.Cube 多 TFM 0 error；api-core dist 重建；arco-vue vue-tsc -b + vite build 通过（wwwroot 已更新）
- [x] 手工冒烟清单入 verify.md（待 CubeDemo 宿主人工执行）；勾选本号 tasks 并提交

