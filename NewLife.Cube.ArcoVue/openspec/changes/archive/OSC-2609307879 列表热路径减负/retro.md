# Retro

## 概述

列表热路径按三条线落地：地区和值集不再按行串行打接口，布尔与看板在排序和筛选不依赖该字段时不再整表重查，菜单和元数据走已有实体缓存。验收门禁通过后归档。浏览器里的网络与图表加载没有实点，记在 verify 的 P2。

## 目标达成（对照 proposal §1）

| 目标 | 结论 | 证据 |
| --- | --- | --- |
| 1 地区一次取名、LOV 缺值仍翻译 | 达成 | `AreaController.Names` / `ResolveNames`；`hydrateAreaLabels` 按 200 分片；`missingLovValues` |
| 2 布尔与看板按条件跳过整表刷新 | 达成 | `patchReload.ts`；`useListCrud` 的启停与 `onKanbanMove` |
| 3 菜单与流程定义走实体缓存，字典不重复跑委托 | 达成 | `CubeController` 菜单树；`FindPublished`；`DataField.ToDictionary` |
| 4 fit 高度封顶，离页停表 | 达成 | `resolveFitHeight`；监控图、`useAutoStep`、甘特宽度轮询的 `onDeactivated` |

## 测试与构建

- XUnit 过滤 7 通过（验收中修过地区主键重复插入）。
- Vitest 29 通过。
- `dotnet build NewLife.Cube` 0 错误。`vue-tsc --noEmit` 通过。
- 未做浏览器冒烟。

## 过程中的坑

- `DAL.AddConnStr("Membership", Mode=Memory)` 在连接名已被占用时不会换库。固定主键的地区测试第二次运行会撞 `UNIQUE constraint failed: Area.ID`。插入前删除，断言后清理。
- `@newlifex/api-core` 的类型来自 `dist`。只改 `src/api.ts` 时 `vue-tsc` 看不到 `areaNames`，要先在该包执行 `pnpm build`。
- 表头排序图标在 `useListTable` 模块顶层注册，读不到函数里的主题快照。单元格 `style` 用快照；图标仍走 `themeColor`。
