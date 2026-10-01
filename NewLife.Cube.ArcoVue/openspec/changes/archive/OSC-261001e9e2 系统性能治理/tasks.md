# Tasks

- [x] T1 核对 `web/docs/系统性能治理.md` 与 design §2–§5 的表一致；`web/README.md` 增加入口链接
- [x] T2 `patchReload.ts`：`shouldReloadAfterWrite`；`shouldReloadAfterPatch` 改为单字段调用。补 `patchReload.spec.ts`
- [x] T3 新建 `listRowPatch.ts`：`assignRowFields`、`removeListRow`、`deleteFollowUp`。补 `listRowPatch.spec.ts`
- [x] T4 `useListCrud.ts`：编辑保存、流程可写保存、单行删除按 design §2。新增、批量、导入仍 `loadData`。树视图两处都 `loadData`。单行本地删除时从 `selectedKeys` 去掉该主键（`loadData` 会清空选择，本地路径需单独去掉）
- [x] T5 新建 `lovHydrate.ts` 的 `groupMissingLov`。`useRecordDrawer.hydrateRowLabels` 在地区分支之后用它并行 `BatchLabel`。补 `lovHydrate.spec.ts`
- [x] T6 新建 `LovLabelQuery.PlanListLabelQuery`。`LovController.BatchLabel` 的 `List.*` 分支改用计划。补 `LovLabelQueryTests`。`Enum.*` 与 `AppendEntityLabels` 的现有测试仍过
- [x] T7 确认 `loadChart` 与 `InsightPanel.vue` 无本号差异：不渲染「当前页」，不用当前列表页的行替换图表配置
- [x] T8 新建 `ExportCap.ResolveExportCap`，硬顶 1000000。`ExportData` 使用它。`NewLife.Cube` 与 `NewLife.CubeNC` 的 `MaxExport` 默认值改为 1000000。Cube.Vue 设置页 `maxExport` 初始值改为 1000000。补 `ExportCapTests`
- [x] T9 跑本号 Vitest 与 `ExportCapTests`、`LovLabelQueryTests`；`dotnet build NewLife.Cube`；web `vue-tsc --noEmit`
- [x] T10 手工冒烟：洞察区无「当前页」。部门卡片只改备注仍因整份提交键含排序字段 `Name` 而 `loadData`，按复盘指示仅记录、不补齐。未点删除，避免改演示数据。删除路径由 `listRowPatch.spec.ts` 覆盖
