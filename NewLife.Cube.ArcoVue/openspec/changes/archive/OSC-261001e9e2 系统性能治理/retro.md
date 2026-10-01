# Retro

## 概述

系统性能治理把四条边界写成持久规范，并落地写回分流、抽屉值集合并、`List.*` 按键或最多 10 页、导出 100 万行封顶。图表取数未改。验收在部门页发现：只改备注仍因整份提交键含排序字段而整表刷新。用户随后指示复盘，该 P1 按仅记录归档，业务代码未再改。

## 目标达成（对照 proposal §1）

| 目标 | 结论 | 证据 |
| --- | --- | --- |
| 1 四条边界写入规范，后续按请求段、扫描行数、是否整表刷新验收 | 达成 | `web/docs/系统性能治理.md`；`web/README.md` 入口 |
| 2 抽屉缺值合并并行；`List.*` 可搜索时一次按键，否则最多 10 页且第 4 至 10 页不提前丢 | 达成 | `lovHydrate.ts`；`LovLabelQuery.PlanListLabelQuery`；`LovController` 的 `List.*` 分支 |
| 3 洞察区不渲染「当前页」；导出默认与硬顶 100 万行 | 达成 | `InsightPanel.vue` 与 `loadChart` 无本号差异；部门页正文无「当前页」；`ExportCap`；两处 `MaxExport` 与 Cube.Vue 初始值 |
| 4 排序、筛选和视图窗口都不依赖所改字段时，编辑和删除只改本地行 | 删除达成，编辑未达成 | 删除走 `removeListRow` / `deleteFollowUp`。编辑把整份提交键交给 `shouldReloadAfterWrite`，部门按 `Name` 排序时只改备注仍 `loadData`。仅记录，未补齐 |

## 测试与构建

- Vitest 17 通过：`patchReload.spec.ts`、`listRowPatch.spec.ts`、`lovHydrate.spec.ts`。
- XUnit 16 通过：`ExportCapTests`、`LovLabelQueryTests`、`LovControllerGuardTests`。
- `dotnet build NewLife.Cube` 0 错误。`vue-tsc --noEmit` 通过。
- 浏览器冒烟：部门卡片只改备注仍出现列表请求；备注已改回空。未点删除，避免改演示数据。洞察区没有「当前页」。

## 实际完成范围

计划内 T1–T9。T10 冒烟已做，编辑不刷新未达到，按仅记录关闭。会话内无计划外功能。单行删除时从 `selectedKeys` 去掉该主键，已写在 T4。

## 过程中的坑

- 刷新判断用了整份提交键。名称总在表单里，列表又常按名称排序，只改备注也会整表刷新。
- 草案把翻页改成 10、取消「当前页」之后，proposal 成功标准仍写「不超过 3」。验收时才改齐。
- 纯函数单测绿，不能代替带着真实排序的保存请求。
