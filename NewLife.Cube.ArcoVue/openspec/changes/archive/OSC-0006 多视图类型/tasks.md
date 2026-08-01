# OSC-0006 Tasks

## P0 — 模型与门禁

- [x] 扩展 `ViewKind`：`table | tree | card | kanban | calendar | gantt`
- [x] `NamedView.mapping` + normalize（与 `view` 对齐；非法字段回落）
- [x] `canCreateViewKind(fields, typePath, kind)`：树无 Parent/children 元数据 → false
- [x] `resolveViewPageSize(kind, chrome)`：kanban/calendar/gantt → 200～500
- [x] 单元测试：normalize / canCreate / pageSize
- [x] **不写** `ganttJson`/`cardJson`；`viewsJson` 为唯一映射源

## P1 — Tab 工具条

- [x] `ViewTabsToolbar.vue`：Tab 切换、`···`（重命名/配置/复制/删除）、`+` 新建类型菜单
- [x] 替换 `NamedViewsToolbar` 下拉为 Tab IA（或内嵌改造）
- [x] 活跃视图切换写 `activeViewId` + 顶层 `view`/`columns`
- [x] 删除最后一视图时保底生成默认 table

## P2 — 配置抽屉「列表区」按 kind

- [x] table/tree：保留分页器/详情/删除/展开
- [x] card：标题字段、图片字段
- [x] kanban：分组依据、标题、图片
- [x] calendar：开始(必)、结束(可空)、标题、颜色
- [x] gantt：开始、结束、标题、颜色
- [x] 顶栏/背景色各类型共用

## P3 — 卡片 + 看板

- [x] `CardList.vue`：标题/图/两列字段/左下 详情+编辑+删除（按权限）
- [x] `KanbanBoard.vue`：按 groupField 分列；列内同卡片单元；只读无拖拽写回
- [x] `DefaultList` 舞台挂载 card/kanban；大 pageSize

## P4 — 日历 + 甘特

- [x] `CalendarMonth.vue`：月视图；`[start, end||start]`；色条；点击详情
- [x] `GanttView.vue`：`@visactor/vtable-gantt`（或等价）+ async chunk
- [x] 大 pageSize；不拖拽写回

## P5 — 树视图

- [x] table 模式 + hierarchy（有 children）
- [x] 无树数据提示；创建门禁已在 P0
- [x] ViewConfig 树相关 chrome（展开）

## P6 — 文档

- [x] `Doc/Api/ArcoVue企业中后台迁移方案.md` M3b / DTO
- [x] `Doc/Api/前端对接指南.md` ViewKind + mapping + pageSize
- [x] `NewLife.Cube.ArcoVue/web/README.md` 多视图说明

## P7 — 验收门禁

- [x] `pnpm test` / `pnpm build` 通过
- [x] `verify.md` AC 勾选；状态 → Validating → Done
- [x] 归档至 `openspec/changes/archive/`
