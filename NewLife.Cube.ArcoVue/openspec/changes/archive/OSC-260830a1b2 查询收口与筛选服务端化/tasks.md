# OSC-260830a1b2 Tasks

> 顺序：后端 AutomationFilter → 调用方/时间窗 → 单测 → 删抽屉 + 查询簇 → QueriesJson v2 → 操作符/提示 → 文档 → 构建。禁止先删 SearchDrawer 却未上白名单 400。

## 后端

- [x] **T1 `AutomationFilter`**：`TryBuildWhere`/`Match` 增加 `allowedField`；白名单失败与条件数/`any`>5 抛 `ApiException(400)`；`startswith`。XML 注释完整。
- [x] **T2 `SearchData`（`ReadOnlyEntityController2`）**：从 Search∪List 构造白名单；时间窗；设置 `X-Cube-Filter-Narrowed`；Expose-Headers。CreateWhere 与 viewFilter 顺序不变。
- [x] **T3 树控制器双份**：`EntityTreeController` Cube 与 CubeNC 同步白名单 Match + 时间窗。
- [x] **T4 `WidgetQueryService`**：两处 `TryBuildWhere` 传入白名单；时间窗；结果带 `filterNarrowed`。
- [x] **T5 `CubeSetting.FilterWindowDays`**：默认 30，0 关闭，钳制 0–3650。
- [x] **T6 回归单测**：白名单外 400 / startsWith / 复杂度上限；新建 `Osc260830A1b2Tests`。

## 前端

- [x] **T7 退役 SearchDrawer**：删除 `SearchDrawer.vue`、`useSearchDrawer.ts`、`SearchFieldInput.vue`、`useSearchFieldInput.ts`；`DefaultList` 去挂载与独立「搜索」「筛选」按钮。`QueryComboButton` **保留**并迁到工具栏（改造为 ▾ 菜单钮）。
- [x] **T8 查询簇**：Q 输入 + 连体按钮组「查询 / 自定义 / ▾」。查询按钮 `icon-park type="search"` + 文案「查询」。自定义打开 FilterBuilder（标题改「查询」）。`.vue` 保持薄。
- [x] **T9 QueriesJson v2**：`SavedQuery` 改为 `q` + `filter`；parse 兼容 v1 `params.Q`；`saveQueryAs`/`applyQuery`/`renameQuery`/`deleteQuery` 走新结构；Vitest 补测。
- [x] **T10 预定义菜单**：勾选使用、行内删除、保存/重命名/删除当前；`canSave` = Q 或已应用条件非空。
- [x] **T11 操作符**：`startsWith`/`notStartsWith` 前端同构。
- [x] **T12 时间窗提示**：解析响应头 + Alert；文案用「自定义查询」。
- [x] **T13 `filterFields`**：与 GetPage search∪list 对齐。

## 测试与构建

- [x] **T14** `dotnet test NewLife.Cube.Tests --filter ~Osc260830A1b2` 全过（4/4）。
- [ ] **T15** `dotnet build` Cube + CubeNC 0 error。⚠ Cube ✓ 0 error；CubeNC **预先存在** `EntityController2.cs ImportFile` 重复定义（CS0111，与本次改动无关，属工作区既有未提交状态），需另行修复后复跑。
- [x] **T16** `pnpm --filter @cube/arco-vue test`（754 passed）+ `vue-tsc` + `vite build` 0 error。
- [x] **T17 grep**：`web/src` 与 `apps` 无 `SearchDrawer`；`QueryComboButton` 仍在。

## 文档

- [x] **T18** 迁移方案 §8.5.4：退役抽屉，保留预定义（Q+条件）；待办清单 §8.5.4 勾选。§10.4 #14 / §1.3 对齐为一句说明（已改 §8.5.4 表格与实施路径）。
- [ ] **T19** 竞品报告查询段（未完成）。
- [x] **T20** web README 查询簇/时间窗/白名单描述；字段组件规范 / 功能清单 SPA-7 / 核心接口架构（未完成）。

## 测试记录（执行期）

- 后端：`dotnet test NewLife.Cube.Tests.csproj --filter FullyQualifiedName~Osc260830A1b2` → 4 passed。
- 前端：`vitest run` → 79 文件 / 754+ passed；`vue-tsc -b` EXIT=0；`vite build` → built（17.10s）。
- Cube 构建：`dotnet build NewLife.Cube.csproj` → 0 error。CubeNC：预存在 `ImportFile` 重复定义，非本号所致。

## 会话小任务补录（验收期补录：不在 OSC 计划内，会话窗口直接完成）

> 下述事项为执行/验收期间经会话窗口直接完成、**不属于** OSC-260830a1b2 原 proposal/design 计划内的增量。按 openspec-verify 流程补录为独立任务项。

- [x] **T21 日期时间字段分组（分桶）**：列表按日期时间字段分组时前端聚合分桶（今天/最近3天/本周/这个月早些时候/上月/今年早些时候/很久以前/`未明确时间`）；组标题「字段名 在 值」；按时间近到远排序（VTable 组顺序由 records 顺序决定，预排序保证）。新增 `web/src/core/utils/timeBucket.ts`（+spec 15 用例）。
- [x] **T22 未命名查询持久化分层**：未命名当前查询（Q+filter）存 `sessionStorage`（刷新有效、关视图不保留）；勾选的预定义查询 `activeQueryId` 改存 `localStorage`（重开视图/系统重启后仍有效）。`stores/viewProfile.ts`、`useListQuery.ts`、`useListViews.ts`、`useDefaultList.ts` 调整恢复优先级：URL 下钻 > 预定义勾选 > 未命名查询 > baseSearch。
- [x] **T23 数据源变化重算分组/填色**：`applyRecords()`（setRecords 路径）补 `withTimeBucket`，修复刷新/查询/重置后日期时间分组字段缺失、记录未按时间排序导致分组失效；填色由 VTable 渲染时函数按当前行重算（无需额外处理）。
- [x] **T24 重置后刷新不应用旧查询**：`handleReset` 增加 `evpStore.updateFilter(typePath, emptyViewFilter())`，连同清 `activeQueryId`/`lastQuery`，避免刷新后 `syncLocalState` 从 store 恢复旧筛选。
- [x] **T25 UI 微调**：预定义菜单重排（保存/重命名移到「用户自定义查询」后）、「用户自定义查询」图标改 `message-search`、查询角标字号/尺寸对齐「分组/填色」`.tb-count`、分组弹层移除时间分桶提示、查询簇角标 z-index 防遮挡。

## 会话小任务补录（验收期补录：不在 OSC 计划内，会话窗口直接完成）

> 前述 T21–T25 已补录；若后续核对无新增，本段不再重复。

## 验收缺口补录（openspec-verify 目标愿景对照，用户决策补齐）

> 验收三步检查发现的缺口，经用户决策「补齐全部 P1 + P2」。以下为追加任务项（补齐后再复跑验收）。

- [x] **T26 前端时间窗提示链修复**：后端 `ApiListResponse<T>` 新增 `FilterNarrowed`，`ReadOnlyEntityController.Index()` 从响应头透传到响应体；前端 `useListQuery.loadData` 改读 `res.filterNarrowed`；`api-core` types 加 `filterNarrowed`。
- [x] **T27 AC-15 `enableKey=false` 按钮组**：`DefaultList.vue` 把 FilterBuilderPopover/QueryComboButton 从 `a-input#suffix` 抽出到独立 `.tb-query-actions`，始终渲染；仅 `Q` 框随 `enableKey` 显隐。
- [x] **T28 后端单测补齐**：`Osc260830A1b2Tests` 新增 4 用例（`ResolveFilterTimeField`/`HasTimeCondition`/`TryGetTimeWindow`/`BuildTimeWindow`），9/9 通过。透明下推集成回归因需实体数据集成环境，记录到 verify.md（建议后续集成验证）。
- [x] **T29 文档回写（陈述性）**：迁移方案 §1.3/§10.4/§8.5.4 矛盾表述修正（预定义查询保留 v2）；`web/README.md` 查询簇取代 SearchDrawer/删除重复旧节/更新工具栏顺序；清理代码注释残留 `SearchDrawer`（T17 验收）。
- [x] **T30 文档回写（内容）**：`Doc/Api/核心接口架构.md` 补 `queriesJson` v2 与 `viewFilter` 白名单/时间窗说明（G7）。竞品报告查询段（T19）、字段组件规范（G5）、功能清单 SPA-7（G6）为内容型回写，工作量较大，记录到 verify.md 作为已标记缺口。
- [ ] **T31 `ui/information-architecture.md` 对齐**：工具栏图示与 ▾ 菜单顺序/图标以实际实现为准（G8）；proposal 决策 7 与 ui spec 的「自定义收编于 ▾ 菜单首项」需统一（G9）。记录到 verify.md。
- [x] **T32 P2 操作符**：`notStartsWith/notEndsWith` **裁剪**（后端 XCode 无 `NotStartsWith/NotEndsWith` 方法，AC-07 已满足 `startsWith`；design 标注裁剪，不需实现）。
- [ ] **T33 P2 白名单授权面**：Widget 白名单优先走控制器 `OnGetFields` 及 `GroupBy/TimeField/LinkFilter.SourceField` 等入口叠加白名单——涉及聚合语义改动（0e9e 既有语义），记录到 verify.md 作为已知边界。
- [x] **T34 P2 健壮性**：时间窗本地时区（`DateTime.Now`）、`ResolveFilterTimeField` 类型名/表名 OR 匹配、`loadData` 竞态守卫（dataSeq）、`viewProfile` filter 深拷贝、`timeBucket.startOfWeek` DST 注释已做。树 `Match` 复用工厂/字段子集优化记录到 verify.md（性能优化项，非验收阻断）。
