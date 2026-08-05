# OSC-0012 Verify

> 进入 `Validating` 后逐项勾选；当前为 Draft 验收计划，尚无执行结果。

## 验收标准

- [x] AC-01：损坏/旧/空 FiltersJson 安全回退；false、0、合法数组可保存，未知 search 字段不会发送。
- [x] AC-02：有效筛选严格遵循 URL→当前视图保存条件→空条件；URL 不自动持久化。
- [x] AC-03：保存/清除只影响当前 NamedView 的筛选，切换/刷新后正确恢复。
- [x] AC-04：每个视图只有一个 insight 配置，`showStat`/`showChart` 可独立或同时启用；旧 `mode` 与非法值安全迁移，未知 JSON 不丢失。
- [x] AC-05：统计与图表都使用同一 effectiveSearch；GetList 与 GetChartData 参数一致，无参数 getChartData 行为兼容，过期响应不能覆盖新搜索结果。
- [x] AC-06：搜索字段/操作与统计/图表位于同一 QueryInsightPanel；无第二份搜索状态，窄屏保持一个连续面板。
- [x] AC-07：chart/stat 无数据、403、404、网络失败不影响 CRUD 列表或另一洞察区域。
- [x] AC-08：ViewProfile.PageSize 由 Cube.xml/xcode 生成并按 typePath 个人保存；仅接受 `20/50/100/200/500/1000`，未配置时才回落 workspace 种子或 20。
- [x] AC-09：切换实体页面的 PageSize 互不影响；kanban/calendar/gantt 自动大页策略不覆盖已保存的普通页面偏好，也不再写 workspace.pageSize。
- [x] AC-10：未改变 GetPage 权限、写入 API、视图拖拽/布局、多张图表或任意图表配置能力。
- [x] AC-11：本 OSC 新增 XUnit、api-core/web/组件测试全过，构建无错误，文档同步完成。

## 自动化门禁

```powershell
npm.cmd --prefix "packages/api-core" run test
npm.cmd --prefix "NewLife.Cube.ArcoVue\web" run test
npm.cmd --prefix "packages/api-core" run build
npm.cmd --prefix "NewLife.Cube.ArcoVue\web" run build
```

## 手工冒烟

1. 对有搜索字段实体分别以 URL、保存筛选和空条件进入，确认来源与请求参数。
2. 保存/清除两个命名视图的不同条件，刷新并切换六类视图。
3. 分别启用统计、图表、统计+图表，确认同一 QueryInsightPanel 内的洞察与列表同条件；模拟 chart/统计失败和快速切换筛选。
4. 为两个不同实体设置不同 PageSize，重新进入页面确认隔离；切换 kanban/calendar/gantt 确认自动大页不回写普通偏好。
5. 用手机宽度检查单面板顺序、换行和无 active view/无权限提示。

## 执行记录

- Draft：未执行。仅完成 OpenSpec 文档创建，测试 N/A。
- Implementing（2026-08-05）：T1–T4.2/T4.4 已完成。自动化门禁已跑通：后端 ProfileCommentEntityTests 7 passed；api-core Vitest 8 passed；web Vitest 198 passed；api-core 与 web 构建无错误。AC-01~AC-11 的逐项勾选与手工冒烟（T4.3）留待进入 Validating 后执行。
- Validating（2026-08-05）：验收执行。
  - 三步审计：① 实现审计——FiltersJson 安全回退/域解析（`hasViewsDomain`/`viewsSource`）、单一 insight（`showStat`/`showChart` 独立）、统一 `effectiveSearch` + `chartSeq` 竞态保护、单 `QueryInsightPanel`、typePath 级 PageSize（`getPageSize`/`resolveViewPageSize`）均已按 proposal/design 落地；② 代码审查——关键实现点无资源/并发/命名问题，`normalizePageSize` 收口枚举值、大页策略经 `resolveViewPageSize` 不写回 workspace；③ 文档同步——`Doc/附录C_实体参考.md`（FiltersJson/PageSize/UserId=0 模板语义）、`Doc/Api/核心接口架构.md`（ViewProfile body 契约）已登记。
  - 自动化门禁复跑：后端 ProfileCommentEntityTests 13 passed；api-core Vitest 11 passed；web Vitest 219 passed；api-core 与 web 构建成功。
  - 手工冒烟（T4.3）：URL/保存/空条件三种来源、双命名视图隔离、六类视图切换、统计+图表同条件、双实体 PageSize 隔离与 kanban 大页不回写、手机宽度单面板均符合预期。
  - AC-01~AC-11 全部勾选通过；状态 → Validating。
