# Status
- id: OSC-260903e2a4
- state: Implementing
- updated: 2026-09-03T21:30:00+08:00
- approvedBy: openspec-approve
- trigger: 批准并执行 e2a4 变更。
- checklist: passed
- note: |
    批准检查表通过后进入执行。范围单一（实体部件查询条件 + $host 宿主引用）；proposal 目标/锁定决策/不做什么/测试齐全；
    design 含复用盘点、协议值形态、后端解析矩阵、文件地图与测试设计；tasks P1–P7 可勾选；ID OSC-260903e2a4 未占用。
    执行顺序：P1 后端保存端校验 → P2 后端取数解析 → P3 api-core → P4 配置 UI → P5 联动判定 → P6 文档 → P7 测试门禁。
    关键约束：AutomationFilter.TryBuildWhere 不改；query.extraFilter 为唯一持久化载体；工作台域禁 $host。
    **执行进度（P1–P7 全部完成并提交）**：后端 DashboardJson 校验 + WidgetQueryService $host 解析 +
    CubeController 传宿主（多 TFM 0 error）；api-core HOST_REF_KEY/isHostRefValue/hasHostRefFilter + dist 重建；
    ArcoVue FilterBuilderPopover 可选 hostFields/showSaveView、配置抽屉「查询条件」区、hostFilterFields 注入、
    未联动判定扩展（vue-tsc 0 error、vite build 通过、wwwroot 已更新）；文档 4 处 + DASH-4 行。
    XUnit Osc260903WidgetQueryTests 13 绿 + Widget/工作台回归 39 绿；api-core Vitest 51 绿；arco-vue Vitest 834 绿。
    待宿主（CubeDemo）人工冒烟（清单见 verify.md）→ 验收。
