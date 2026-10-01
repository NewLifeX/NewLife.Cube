# Status
- id: OSC-2610012e35
- state: Implementing
- updated: 2026-10-01T17:43:33+08:00
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
