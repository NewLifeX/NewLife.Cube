# OSC-0012 — 筛选记忆与单一洞察区

## 1. 为何做

当前 `ViewProfile.FiltersJson` 已在后端模型和数据库中预留，但 ArcoVue 未消费；用户在命名视图中设置搜索条件后离开页面即丢失。`DefaultList` 也已拿到 `GetList.stat`，却没有按视图保存洞察展示偏好；分页条数则错误地保存为全局 `UserProfile.workspace.pageSize`，不同实体页面互相影响。飞书多维表格的有效体验是“同一数据源的受控视图”，本号在固定 CRUD 容器中补齐筛选记忆、单一查询洞察面板和页面级 PageSize，不引入自由画布。

## 2. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | 筛选条件以 `typePath + NamedView.id` 为粒度保存到个人 `ViewProfile.FiltersJson`。 |
| 2 | 用户必须显式点击“保存到此视图”；普通搜索、重置、URL 参数均不自动持久化。 |
| 3 | 有效筛选优先级为 `URL 参数 > 个人已保存筛选 > 空条件`；本号不读取全局模板。 |
| 4 | 每个命名视图只有一个 Insight，但可独立启用统计标签与一张固定图表；配置写入该视图的 `ViewsJson.insight`。 |
| 5 | 统计使用当前 `GetList` 的 `stat`；图表使用同一搜索参数请求既有 `GetChartData`，二者可同时展示，不允许用户输入 ECharts option。 |
| 6 | `SearchForm` 与 Insight 合并为单一“查询与洞察面板”：搜索字段/操作在上，统计标签与图表作为下方可选结果区，避免两个独立面板的状态和响应式组合。 |
| 7 | `PageSize` 保存为当前 `typePath` 的个人 `ViewProfile.PageSize`，不随命名视图切换；全局 `workspace.pageSize` 仅作为无页面配置时的兼容种子。 |
| 8 | 不新增、删除、拖拽或嵌套容器区块；固定顺序是查询与洞察面板→视图/工具栏→数据视图→分页→右侧 RecordDrawer。 |

## 3. 做什么

- 解析、规范化和保存 `FiltersJson`，在加载命名视图时将其应用到搜索表单。
- 在搜索区提供保存、清除已保存条件和来源提示；保留临时搜索与已保存默认筛选的区别。
- 在 `NamedView` 增加受限 `insight` 配置，配置抽屉可独立开关统计标签和固定图表。
- 将 SearchForm 与 Insight 作为单一查询面板，令 `GetList` 与 `GetChartData` 使用同一已归一化筛选参数；无图表、无权限、空结果均安全降级。
- 在 `Cube.xml` 为 ViewProfile 增加 `PageSize`，经 xcode 生成实体/Model 后由个人 profile API 保存当前页面条数；旧全局偏好只作首次页面默认值。
- 补齐后端、工具函数、store、api-core 与 `DefaultList` 的测试，并同步迁移方案与前端说明。

## 4. 不做什么

- 不新增除 `ViewProfile.PageSize` 外的后端数据结构、控制器路径或查询 API。
- 不做分组引擎、任意查询表达式、跨实体数据源、多张图表、仪表盘、图表拖拽与自定义图表 option。
- 不实现模板继承；全局模板由 OSC-0014 单独处理。
- 不改变既有 CRUD 权限、数据权限、分页和六类视图写回能力。

## 5. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0005 / OSC-0006 | Done：命名视图、ViewProfile store、六类视图与 ViewConfigDrawer 基线 |
| OSC-0009 | Done：搜索字段元数据、字段值格式化与 DefaultList 基线 |
| SPA-7 / DATA-4 / DATA-5 / SPA-15 | 本号消费的动态 CRUD、搜索、统计和图表能力 |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| XUnit | 是 | PageSize 的 Cube.xml 生成映射、个人 upsert、范围归一与旧记录兼容 |
| ArcoVue Vitest | 是 | FiltersJson 解析/迁移/优先级、视图切换、统计/图表组合、同筛选参数、PageSize 优先级 |
| api-core 测试 | 是 | `getChartData(type, params)` 查询参数序列化与既有无参数兼容 |
| 组件测试 | 是 | 查询洞察面板保存/清除、统计+图表组合、配置抽屉开关与权限/空态 |
| 构建 | 是 | api-core 与 ArcoVue web 无错误构建 |
| 手工冒烟 | 是 | 搜索、保存、刷新、切换视图、统计+图表、URL 覆盖与实体间 PageSize 隔离 |

## 7. 成功标准

- [ ] 用户可将合法搜索条件显式保存为当前命名视图默认值，并可独立清除。
- [ ] URL 参数、个人已保存筛选和空条件按已锁定优先级工作，未知/失效字段不会进入请求。
- [ ] 每个命名视图仅有一个洞察配置，统计标签和一张图表可独立或同时显示，且均与当前有效筛选同源。
- [ ] 每个 typePath 独立保存 PageSize；无页面配置时才回落历史全局偏好，切换实体页面不再互相覆盖。
- [ ] 没有已保存配置时，页面行为与 OSC-0009 当前行为一致。
- [ ] 本 OSC 新增单测全部通过，相关构建无错误，事实性文档完成最小同步。
