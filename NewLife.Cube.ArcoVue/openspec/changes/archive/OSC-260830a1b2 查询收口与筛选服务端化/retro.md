# OSC-260830a1b2 Retro

> 复盘：2026-08-30。从 `Implementing` → `Validating` → `Done`。

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | AC-01~19 通过；AC-09（导出）标 ⚠️ 需人工验证（R4） |
| 自动化门禁 | 后端 `Osc260830A1b2Tests` **9/9** + Cube build **0 error**；前端 **770/770** + `vue-tsc` EXIT=0 + `vite build` 0 error |
| 缺口 | P1 全部补齐（时间窗链 / AC-15 / 单测 / 文档矛盾）；剩余 R1–R6（内容文档、聚合语义、集成回归、导出、P2 裁剪、ui 文档） |

## 范围回顾

| 维度 | 计划 | 实际 |
|------|------|------|
| 控制器 | 不改 18 个 Search | ✅ 未改；无 `ApplyRequestFilter`；签名未变 |
| 白名单 | 400 非静默放弃 | ✅ `TryBuildWhere`/`Match` 白名单 400 + 超限 400 |
| 产品面 | 退役抽屉；查询簇 + 预定义保存 Q+条件 | ✅ 查询簇 + `QueriesJson` v2（q+filter）；`enableKey=false` 按钮组独立 |
| 时间窗 | `FilterWindowDays` 默认 30 | ✅ 默认 30、0 关闭、钳制；响应体 `filterNarrowed` 透传 |

## 实际完成范围（含会话补录 T21–T34）

- **原计划 T1–T20**：后端白名单/时间窗/startsWith/复杂度、前端查询簇/QueriesJson v2/操作符/时间窗提示/白名单对齐等，全部完成（T15 CubeNC 预存 ImportFile 例外；T19/T20 内容型回写为已知缺口）。
- **会话补录 T21–T25**：日期时间分组分桶、未命名查询持久化分层、数据源变化重算分组/填色、重置后刷新不应用旧查询、UI 微调。
- **验收补齐 T26–T34**：时间窗响应体透传（T26）、AC-15 按钮组独立（T27）、后端时间窗单测（T28）、文档矛盾修正/注释清理（T29/T30）、核心接口 v2 说明（T30）、P2 健壮性（T34：本地时区/表名匹配/竞态守卫/深拷贝/DST 注释）。

## 教训

- **前端读响应头不可靠**：`api-core.createRequest` 只返回响应体、丢弃响应头；需要响应头值时应在响应体透传（如 `ApiListResponse.FilterNarrowed`）。本次时间窗提示因读 `res.headers` 恒空，验收才发现。
- **VTable groupBy 组顺序取决于 records 首现顺序**：`GroupConfig.sort` 不生效；时间分桶需按时间字段**预排序 records**（组间+组内近到远）。
- **功能按钮勿被条件渲染容器连带隐藏**：`enableKey=false` 时查询按钮组被 `a-input` 连带隐藏（AC-15），应独立渲染。
- **monorepo `@cube/api-core` 改 src 后必须 build**：types 入口是 `dist`，否则 ArcoVue `vue-tsc` 看不到新导出（本号再次验证）。
- **XCode 无 `NotStartsWith/NotEndsWith`**：design 列出的操作符若 XCode 不支持，应裁剪并记录，而非强行实现。
- **验收必补会话内增量**：日期时间分组/查询持久化分层/数据更新重算/重置刷新/UI 微调均在会话窗口完成、不在 OSC 计划，验收需补录（T21–T25）。
- **时间窗时区用本地 `DateTime.Now.Date`**，与既有 `dtStart` 本地惯例一致，避免 UTC 零点把本地当日 0:00–7:59 挤出默认窗口。

## 待后续

- T15 复跑（CubeNC `ImportFile` CS0111 为工作区预存，需另行修复）。
- R1 内容文档（竞品报告 / 字段组件规范 / 功能清单 SPA-7）。
- R3 透明下推集成回归（需实体数据集成环境）。
- R4 导出一致性人工验证。
