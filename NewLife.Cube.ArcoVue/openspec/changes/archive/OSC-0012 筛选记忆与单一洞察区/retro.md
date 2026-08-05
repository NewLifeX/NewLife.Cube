# OSC-0012 Retro

## 结果摘要

- 状态：已完成（验收通过）。
- 计划：个人命名视图的显式筛选记忆；单一查询洞察面板内可并列统计标签与一张固定图表；按 typePath 保存 PageSize。
- 不做：自由仪表盘、多张图表、任意 ECharts option、跨实体数据源与模板继承。

## 实际结果

- **代码范围**：
  - 后端：`NewLife.Cube/Entity/视图配置.Biz.cs`（FiltersJson/PageSize 个人存取与容错解析）、`CubeController.cs`（`ViewProfile` GET/PUT 合并 FiltersJson、PageSize 校验枚举值）、`CubeService.cs`（camelCase 绑定修复，见 OSC-0013 迭代）。
  - api-core：`ViewProfileModel` 增加 `pageSize`、`filtersJson`；`createProfileApi` 扩展读写。
  - 前端：`web/src/stores/viewProfile.ts`（域解析 `viewsSource/filtersSource`、`personalViewsJson`）、`utils/viewProfile.ts`（`hasViewsDomain`/`hasFiltersDomain` 容错）、`DefaultList.vue`（统一 `effectiveSearch`、`chartSeq` 竞态保护、typePath 级 PageSize、单 `QueryInsightPanel`）、`QueryInsightPanel.vue`、`ViewConfigDrawer.vue`（`showStat`/`showChart` 独立开关）。
- **偏差**：无重大偏差。后续 OSC-0014 在其域解析上扩展「模板域（个人>模板>系统）」，属演进而非偏差。
- **测试证据**：后端 `ProfileCommentEntityTests` 7→13 passed（含后续回归）；api-core 8→11；web 198→219；api-core/web 构建成功；`NewLife.CubeNC -f net10.0` 构建 0 错误。
- **文档**：`Doc/附录C_实体参考.md`（FiltersJson/PageSize/UserId=0 模板语义）、`Doc/Api/核心接口架构.md`（ViewProfile body 契约）已登记。

## 经验沉淀候选

- 搜索状态应有单一 effectiveSearch，避免 list/stat/chart 使用不同条件。
- 配置 JSON 的未知字段必须在 round-trip 中保留。
- 页面 PageSize 应优先归属实体 ViewProfile；全局工作台值仅适合作为旧配置种子。
- 域解析（个人/模板/系统）采用**整体选取**而非字段级 patch 合并，契约简单、可预测；跨 OSC 演进时保持同一解析函数扩展而非各自实现。
- URL 查询参数只作为「进入页面的一次性来源」，绝不自动持久化，避免污染用户保存的筛选。
