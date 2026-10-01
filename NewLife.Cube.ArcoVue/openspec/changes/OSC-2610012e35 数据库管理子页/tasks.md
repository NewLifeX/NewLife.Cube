# OSC-2610012e35 Tasks — 数据库管理子页

## T1 API

- [x] T1-1 `NewLife.Cube` `DbController` 增加 ShowTables、ShowEntities、ModelDiff、Compact，返回 design §2 的 JSON。
- [x] T1-2 `Osc2610012e35DbTests`：非法连接名 code 非 0；测试库 ShowTables code 0 且 tables 为数组。
  - [x] 修复：表、实体、差异读取动作使用显式 `/api/Admin/Db/{Action}` 特性路由，并以反射测试锁定。

## T2 前端

- [x] T2-1 api-core 四个方法与 `api.spec.ts` URL 断言。
- [x] T2-2 `dbPage.ts` 的权限与 `flattenDiff`，加 spec。
- [x] T2-3 `useDbPage` 与 `index.vue`：四按钮、右抽屉、压缩确认。空差异文案「无差异」。

## T3 验证与文档

- [x] T3-1 `dotnet test` 过滤 `Osc2610012e35`；`dotnet build NewLife.Cube -f net10.0`。
- [x] T3-2 web `vitest` `dbPage.spec.ts` 与 api-core `api.spec.ts`；`vue-tsc -b`。
- [x] T3-3 核心接口架构与迁移方案补四条路径。功能清单有数据库行则更新，否则写 DB-1。
