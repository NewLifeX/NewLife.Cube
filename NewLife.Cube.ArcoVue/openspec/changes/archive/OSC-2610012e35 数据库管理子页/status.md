# Status
- id: OSC-2610012e35
- state: Done
- updated: 2026-10-03T22:35:00+08:00
- approvedBy: openspec-approve
- trigger: "按照项目 OpenSpec 规范，批准并执行 OSC 2e35 。"
- checklist: passed
- note: 批准通过：范围单一，依赖 OSC-2608139feb 已完成；草案、设计、任务与 UI 信息架构满足检查表。
  执行开始：进入 Implementing，准备按设计实现 API、ArcoVue 页面与测试。
  实现与测试完成：后端 5/5、dbPage 2/2、api-core 40/40、net10 build 与 vue-tsc 均通过。
  收尾门禁：会话小任务已补录（无新增）；代码审阅与实现审计无 🔴 项、无需求缺口，等待验收。
  路由修复：表/实体/差异读取动作改为显式 API 路由，避免部署宿主未解析类级 `[action]` 模板导致 404。
  路由回归：OSC 过滤测试 9/9 通过，net10 构建 0 错误。
  抽屉修复：加载、错误、空态与表格统一为可见互斥状态，并隔离快速切换时的旧响应。
  卡片调整：四个数据库管理动作收纳到“更多”下拉菜单，并复用已注册 IconPark 图标。
  渲染链路修复：抽屉状态分支收敛在单一默认插槽容器，确保内容不被 Drawer Teleport 忽略。
  空白根因定位（真实根因）：`a-table-column` 未放入 `#columns` 插槽时，Arco 2.57 会将默认插槽当作裸 `<table>` 内容渲染（列组件渲染为注释节点），列不注册 → 表格 0 列空壳；因数据分支恒被选中，"空白"与加载/空态修复无关。已在 Playwright 真实环境复现（thead 缺失、tbody 0 行），修复为两处表格的列声明收敛进 `<template #columns>`。
  修复验证：`e2e/admin-db-drawer.spec.ts`（表/实体/差异 均渲染可见内容）2/2 通过；`vue-tsc -b` 0 错误；`useDbPage.spec.ts` 1/1 通过；修复后截图抽屉表格数据行完整。
  友好名修订：抽屉「名称」列展示 description（注释优先→实体 [Description]→技术名回落）；后端 10/10、vue-tsc 0、vitest 3/3、E2E 2/2（含 AccessRule 描述断言），真机截图确认。
  UI 修订：移除「表」菜单项；实体列表拆「名称/备注」并支持数据字典下钻（ShowEntityFields，列对齐 CubeNC 架构定义）；后端 14/14、vue-tsc 0、vitest 4/4、api-core 72/72、E2E 2/2。
  二次 UI 修订：移除「差异」出口；列名（显示名/字段名）与排列、数字列右对齐、全列单行省略+Tooltip 规范化；vue-tsc 0、vitest 3/3、E2E 2/2（含右对齐与省略渲染断言），截图含 Tooltip 验证。
  宽度修订：实体列表与数据字典抽屉统一为 720（`RECORD_DRAWER_WIDE`），与实体对象 详情/编辑/添加 宽抽屉一致；vue-tsc 0、E2E 2/2（含宽度断言）。
  验收（openspec-verify）：三步编排完成（实现审计/代码审查/文档同步，修正 SFC 纯净度 1 处）；门禁重跑全绿（build 0 错误、后端 14/14、vue-tsc 0、vitest 4/4、api-core 72/72、E2E 2/2）；缺口清单（P2）待用户决策。
  验收缺口决策：补齐 1/2/3（纯表可达 `ShowTableFields`+合并行、Tooltip E2E、清理 `dbDiff`）并复核全绿（后端 18/18、vitest 6/6、E2E 2/2，含纯表流程与 hover 断言）；第 4 条（字典显示表名）仅记录；验收通过（checklist passed），待复盘。
