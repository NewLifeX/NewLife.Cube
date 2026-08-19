# OSC-260819e483 Retro

状态：骨架。Done 后填写。

## 做得好的

- （待填）

## 可改进

- （待填）

## 过程备注

- 用户要求将竞品分析 §8.6 第 1/2/3/4/6 项放进**一个** OSC，任务分阶段；不含第 5 项字段级权限。
- 创建当日曾误拆 `OSC-2608198ccf` / `11c1` / `45c2`，合并前已删除且无代码落地。
- Draft 修订 2：对照 NewLife.XCode `LogProvider`/`SqlBuilder.BuildOrder` 与 Cube `AutomationFilter`/`EnableOrDisableSelect`/`NotificationRecord` 审查。取消 LogProvider 装饰器、Remark JSON、EntityListFilter、sorts、projections、MentionsJson。P4 改为解析现有 `Field=old -> new`。
- Draft 补全 3：对照双栈（CubeNC Link `*Controller2`、自有 `ReadOnlyEntityController`/`CubeController`/`EntityTreeController`）、`Pager` 已注释绑定 `OrderBy`、`ViewSort` 单列、`TryBuildWhere` 无 `notcontains`、`EnableSelect` 为 GET、Insight 直接 `setOption`。把这些写成肯定约定，避免实施再猜。
- Draft 补全 4：用户指出 InsightPanel 应由用户配置 ECharts option 并持久化到 ViewProfile。取消后端 `autoChart`；option 进 `NamedView.insight.chartOption`（ViewsJson），保存剔除数据快照。
