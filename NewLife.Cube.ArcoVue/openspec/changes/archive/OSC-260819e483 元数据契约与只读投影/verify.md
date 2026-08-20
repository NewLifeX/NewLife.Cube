# OSC-260819e483 Verify

> 状态：通过（openspec-verify）→ 用户决策「仅记录不补齐」→ openspec-retro 归档
> 触发：按照本项目 OpenSpec 规范，对 e483 进行验收和复盘。 / 仅记录不补齐。

## 签名与复用（全程）

- [x] `Fill` / `EnableFieldValidation` 默认 false / `Search` / `GetPage` / `Insert`/`Update`/`OnUpdate` / `AddComment` 六参数 / `WriteLog` 均未改签名
- [x] 未新增 `EntityListFilter`、`CubeFieldDiffLogProvider`、`EntityFieldChange`、`MentionsJson`、GetPage `projections`、`sorts`
- [x] 筛选走 `AutomationFilter`；排序走现有 `Sort`/`BuildOrder`；diff 解析现有 Remark
- [x] GetPage 仍 `[AllowAnonymous]`
- [x] PATCH Action 不在 CubeNC；`SearchData` 只在共享 `ReadOnlyEntityController2` 接了一次 viewFilter

## P1

- [x] GetPage（WebAPI 与 CubeNC）：非 PK、非只读、Nullable=false → `required:true`；Fill 后 Required 仍 false
- [x] 布尔 NOT NULL 可出现星号；提交 false 成功（单测覆盖 isFieldRequired 布尔矩阵）
- [x] 数据权限列表 WebAPI Index 不 500；`RetrieveState` 统计对象若不是 TEntity 则 Stat=null
- [x] 无校验头时写入与改前一致；有头则缺必填失败；读请求无该头

## P2

- [x] `viewFilter` logic=`all`/`any`；可下推时服务端过滤且 AND 数据权限
- [x] `logic=any` 不能放大 `CreateWhere` 范围
- [x] 非法 JSON 或长度 >4096 → 400；无法下推不 500；当前页前端复核仍工作
- [x] 未新增 `sorts`；前端仍单列 `sort`/`desc`；请求无法再绑 `OrderBy`
- [x] 两栈 EntityTree 内存 Match；空条件不传 viewFilter
- [x] `notContains` 可下推（本号补 TryBuildWhere）或不下推时不 500

## P3

- [x] `PatchFields` 为 PATCH + `{id,values}`；`BatchUpdateFields` 为 POST + `{keys,fields[]}`（兼容 `{keys,field,value}`）；均需 Update 权限
- [x] 只改白名单；逐行 Valid+OnUpdate；部分失败返回 `{ok,fail,errors}`
- [x] 空 keys / 超 500 / 未知字段 → 400；多字段 ≤50
- [x] PUT、EnableSelect（仍为 GET）与今日一致
- [x] CubeNC 无这两个 Action
- [x] 局部更新只校验提交字段（`ValidateEntityFields(..., onlyFields)`，T4）

## P4

- [x] XCode 与 Log 表未改；Remark 仍为 `Field=old -> new` 文法；`LogOnChange` 未全局打开
- [x] 历史 Tab：标量 Update 解析 diff（单测 `logRemarkDiff.spec`）；长名优先；逗号值/旧散文/自动化 JSON 回落原文
- [x] 评论可带最多 20 个 mentionUserIds 写 NotificationRecord（InApp/Mention）；非法 Id 跳过；不传则与今日一致；未改 AddComment 签名
- [x] 通知失败不导致评论 500

## P5

- [x] 无 GetPage `projections` 键；无 `autoChart` 查询参数；`GetChartData` 签名未改
- [x] `insight.chartOption` 可经 ViewProfile 保存/读回；保存后无 `series.data`/`dataset.source` 快照
- [x] 超 32KB 或非对象 → 不写入
- [x] showChart + 用户 option：Insight 出图且不依赖 GetChartData 空数组
- [x] 子类 `OnGetChartData` 非空 → 仍用后端 option
- [x] §8.2.2 / §8.2.3 已改为允许一张用户 option；§8.2.6 只读例外已写

## 会话小任务（T1–T5）

- [x] T1 批量修改值控件按 typeName 自适应
- [x] T2 InsightPanel 仅看板/图表 + `useMasterTimeRange`
- [x] T3 默认隐藏 InsightPanel（showStat/showChart）
- [x] T4 `onlyFields` 修复批量确定后不成功
- [x] T5 批量多字段 `Fields[]`（≤50）

## Validating 验收记录（2026-08-21）

### 测试与构建门禁

| 命令 | 结果 |
|------|------|
| `dotnet test XUnitTest --filter FullyQualifiedName~Osc260819` | 8 passed / 0 failed |
| `dotnet test NewLife.Cube.Tests --filter FullyQualifiedName~Osc260819` | 21 passed / 0 failed |
| `dotnet build NewLife.Cube/NewLife.Cube.csproj` | 0 error / 0 warning |
| `dotnet build NewLife.CubeNC/NewLife.CubeNC.csproj` | 0 error / 0 warning |
| `pnpm --filter @cube/api-core test` | vitest 36 + node:test 51，全过 |
| `pnpm --filter @cube/arco-vue test` | 65 files / 593 passed / 0 failed |
| `pnpm --filter @cube/arco-vue build` | vue-tsc + vite 0 error（仅 chunk size warning） |

### 三步检查摘要

1. **实现审计**：P1–P5 与 T1–T5 均已勾选；文件地图到位；CubeNC 无 PATCH；冻结签名未破坏。
2. **代码审查**：无 🔴；局部校验 `onlyFields` 正确。
3. **文档同步**：功能清单 DATA-10a / SPA-18、核心接口架构概览表、竞品分析 §8.6；迁移方案 §8.2 / web README 已对齐。

### 目标愿景对照

| 目标 | 结论 |
|------|------|
| 目标 1–5（P1–P5） | ✅ 代码 + 单测兑现 |
| 不做项（字段 ACL / 改签名 / 浸入 XCode） | ✅ 遵守 |

### 缺口与用户决策

| 级 | 缺口 | 决策 |
|----|------|------|
| 🟢 | S.1–S.7 浏览器冒烟未点验 | **仅记录不补齐**（2026-08-21） |
| 🟢 | 无法下推 viewFilter 翻页不完整 | design 已知限制，仅记录 |
| 🟢 | 图表数据=已加载列表行 | design 已知限制，仅记录 |

**无 P0 / P1。** 用户决策后按通过放行。

### checklist: passed

## 冒烟残余（人工，不阻断）

| # | 操作 | 预期 |
|---|------|------|
| S.1 | `LogOnChange=true` 实体改字段 → 历史 Tab | 字段 diff 表 |
| S.2 | 评论 `@` 提及 | 站内信 |
| S.3 | 筛选 → 翻页 | 可下推则仍过滤；`notContains` 不 500 |
| S.4 | 批量修改多字段 | 一次更新 |
| S.5 | 双击单元格 | PatchFields；布尔 EnableSelect |
| S.6 | 配置 chartOption → 刷新 | 仍在；>32KB 拒绝 |
| S.7 | 无校验头 POST/PUT | 与改前一致 |

## 风险记录

- chunk 体积警告为项目级既有问题。
- FormatPopover sfcThin 已在 OSC-26081903c0 修复；本轮 Vitest 593/593。
