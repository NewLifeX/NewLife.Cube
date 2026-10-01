# Retro

> 复盘 2026-10-01 | 状态 Done
> 验收决策：无 P0/P1 缺口；样例种子与 UI 抛光为执行期会话小任务（T5–T8）

## 概述

ArcoVue 为非实体控制器 `LovController` 补值集管理页：`pageKind=custom` 短路 + 静态 `Admin/Lov`，列表/定义/配置抽屉对齐 Cube.Vue LIST 字段。执行期修了 API 前导 `/` 404，补样例种子与行高/说明文/列宽。

## 目标达成（对照 proposal §1）

| 目标 | 结论 | 证据 |
| --- | --- | --- |
| 1 `/Admin/Lov` 可开可维护 | 达成 | `CUSTOM_TYPES` + `isLovPage` + 静态路由；`useLovPage` CRUD |
| 2 ENUM/LIST 配置走现有 SaveConfig | 达成 | `useLovConfig`；LIST 三 Tab + 编辑弹层 |
| 3 权限裁剪与失败不白屏 | 达成 | `lovPermFlags`；alert + empty |
| 4 Meta/ListData 行为不变 | 达成 | 未改调用点 |

## 测试与构建

- Vitest 18：lovAdmin 9、pageKind 9。
- `vue-tsc -b` 无错误。
- `LovSampleSeedsTests` 1 通过；`dotnet build NewLife.Cube -f net10.0` 0 错误。
- 浏览器冒烟在执行期做过（列表/配置/折行）；验收未再全点。

## 实际完成范围

计划内 T1–T4。会话小任务 T5–T8：LIST 1:1、类型文案、404 路径、空态/按钮文案、表头样式、样例种子、行高/说明文/列宽。顺带 `LovSampleSeeds` + Index/UseCube 幂等调用。

## 过程中的坑

- `cubeApi.page.getList` 的 type 缺前导 `/` 会拼成 `/apiAdmin/Lov` 404；自定义页统一用 `/Admin/Xxx`。
- 初稿写「不改后端」，执行期又要样例数据——须立刻改 proposal/design/tasks，避免验收当缺口。
- 配置说明放在控件右侧会挤窄输入框；用 Form `#label` 槽把 tip 放到标签下并左对齐。
- 抽屉内嵌表格列宽过窄会折行（如 `center`）；固定列宽 + `nowrap` + 横向滚动。
