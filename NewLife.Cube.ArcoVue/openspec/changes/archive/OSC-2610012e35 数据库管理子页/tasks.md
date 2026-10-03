# OSC-2610012e35 Tasks — 数据库管理子页

## T1 API

- [x] T1-1 `NewLife.Cube` `DbController` 增加 ShowTables、ShowEntities、ModelDiff、Compact，返回 design §2 的 JSON。
  - [x] 调整（2026-10-03）：新增 `ShowEntityFields`（`name`+`type`），字段列对齐 CubeNC `Db/Entities.cshtml`（AI/PK/UQ、允许空 N、备注 TrimPrefix 去重）；未知实体 code 非 0。
- [x] T1-2 `Osc2610012e35DbTests`：非法连接名 code 非 0；测试库 ShowTables code 0 且 tables 为数组。
  - [x] 修复：表、实体、差异读取动作使用显式 `/api/Admin/Db/{Action}` 特性路由，并以反射测试锁定。
  - [x] 调整：`ShowEntityFields` 未知实体 + Membership/User 字段架构测试（共 14/14）。

## T2 前端

- [x] T2-1 api-core 四个方法与 `api.spec.ts` URL 断言。
- [x] T2-2 `dbPage.ts` 的权限与 `flattenDiff`，加 spec。
- [x] T2-3 `useDbPage` 与 `index.vue`：四按钮、右抽屉、压缩确认。空差异文案「无差异」。
  - [x] 修复：抽屉加载、错误、空态和表格状态互斥可见；快速切换动作不会被旧请求覆盖。
  - [x] 修复：将抽屉内容收敛至单一默认插槽容器，保证 Teleport 型 Drawer 中的状态分支正常渲染。
  - [x] 调整：表、实体、差异、压缩收纳到带图标的「更多」下拉菜单，保留权限与压缩进行中禁用逻辑。
  - [x] 修复（抽屉空白真实根因）：两处 `a-table-column` 收敛进 `<template #columns>` 插槽——默认插槽会被 Arco 当作裸 `<table>` 内容渲染，列不注册导致表格 0 列空壳、抽屉全空白。
  - [x] 回归：新增 `e2e/admin-db-drawer.spec.ts` 覆盖「更多 → 表/实体/差异」抽屉均渲染可见内容。
  - [x] 调整（2026-10-03）：抽屉「名称」列改显用户友好 `description`（数据库注释→实体 [Description]→技术名回落）；ShowTables/ShowEntities 补字段，契约测试 + AccessRule 描述 E2E 断言。
  - [x] 调整（2026-10-03）：移除「表」菜单项；名称列=描述首句、新增末列备注（`splitDbDescription`）；点击实体名称下钻数据字典（AI/PK/UQ 等 8 列）、可返回实体列表；两表启用横向滚动。
  - [x] 调整（2026-10-03 二次）：实体表列名改「显示名」；全列单行省略+Tooltip；数字列（行数/长度/精度）右对齐；字典标题用实体中文名、显示名置首列、「名称」改「字段名」；移除「差异」菜单与抽屉（删 `flattenDiff`/diffRows 与相关 spec）。
  - [x] 调整（2026-10-03 三次）：抽屉宽度统一为 `RECORD_DRAWER_WIDE`（720），与实体对象 详情/编辑/添加 宽抽屉一致。

## T3 验证与文档

- [x] T3-1 `dotnet test` 过滤 `Osc2610012e35`；`dotnet build NewLife.Cube -f net10.0`。
- [x] T3-2 web `vitest` `dbPage.spec.ts` 与 api-core `api.spec.ts`；`vue-tsc -b`。
- [x] T3-3 核心接口架构与迁移方案补四条路径。功能清单有数据库行则更新，否则写 DB-1。

## T4 验收缺口补齐（openspec-verify 决策，2026-10-03）

- [x] T4-1 纯表可达：`ShowTableFields`（数据库架构字段字典）+ 实体列表合并纯表行（`mergeDbEntityRows`）+ `openDictionary` 双数据源；后端 18/18、E2E 含纯表流程。
- [x] T4-2 Tooltip E2E：字典「类型」列截断单元格 hover 断言（`AccessActionKinds`）。
- [x] T4-3 清理死方法：api-core 删除 `dbDiff`（`dbTables` 因 T4-1 复用保留）。
