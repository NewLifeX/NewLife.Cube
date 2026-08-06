# OSC-0015 — 筛选构建器与多级分组

## 1. 为何做

当前 ArcoVue 实体列表的「筛选」按钮仅切换搜索面板显隐，「分组」按钮是占位提示（"分组能力将在后续版本提供"），没有飞书多维表格式的条件构建与分组体验。搜索表单平铺全部搜索字段，字段多时占满多行；状态/枚举/Lov/LOV LIST 等字段虽已具备下拉展示基础，但缺少系统性验证与 LOV LIST 远程搜索增强。本号在固定 CRUD 容器内补齐：**可视化筛选构建器（多条件 AND/OR，并入后端请求保证翻页完整）** 与 **多字段多级分组（表格内组头行，组头可折叠）**，方案随命名视图保存到 ViewProfile；同时优化搜索面板为「默认一行、超行折叠展开」，并完善搜索字段下拉化与后端搜索操作对齐。

## 2. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | 筛选构建器采用**非拖拽**的条件行构建（字段→操作符→值），条件组支持 `且(AND)/或(OR)` 逻辑切换。 |
| 2 | 筛选条件**并入后端请求**：序列化为与搜索表单一致的扁平参数（`字段=值`、`字段_min/_max`），并入 `effectiveSearch` 触发 `GetList`，翻页/导出完整。 |
| 3 | 筛选操作符**仅限后端 `Search(Pager)` 可表达的能力**：`等于(eq)`（含多选逗号分隔）、`范围(between)`（数值/日期 _min/_max）；不等于/包含/为空等后端不支持的操作符不纳入第一版。 |
| 4 | 分组为**多字段多级**（最多 3 个字段，有序），纯前端对已返回 `tableData` 分组；表格内渲染组头行 + 组内行，组头可折叠/展开；分组不并入请求。 |
| 5 | 筛选/分组方案以 `NamedView.filter` / `NamedView.group` 存入 `ViewsJson`，随视图自动应用；与既有「保存到此视图」（FiltersJson 默认搜索）并存、互不影响。 |
| 6 | Insight 搜索面板默认只展示一行搜索字段，超出折叠为「展开更多 N」，点击展开全部。 |
| 7 | 状态/枚举/单值 Lov 搜索字段继续走 dataSource/`LovSelect` 下拉；**LOV LIST（外部实体值集）增强远程搜索**（输入过滤）+ 已选标签。 |
| 8 | 搜索提交严格按后端逻辑：扁平参数等值 + 范围 + 关键字（与现有 `handleSearch` 对齐），不复制后端搜索逻辑。 |

## 3. 做什么

- 在 `NamedView` 增加 `filter`（筛选构建器方案）与 `group`（分组字段列表）两个域，序列化/反序列化透传并 round-trip 保留未知字段。
- 实现 `filterToSearchParams`：把构建器条件序列化为扁平搜索参数，与现有 `cleanSearchParams`/`collectSearchKeys` 协作。
- 新增 `FilterBuilderPopover`（弹层条件构建：条件行增删、AND/OR 切换、值控件复用 `SearchFieldInput`、应用/保存到视图/清除）。
- 新增 `GroupPopover`（弹层多级分组字段选择）与 `groupRows` 分组工具（组头 label+count，复用 dataSource 翻译）。
- `ListTable` 支持分组数据渲染（组头行 + 组内行，VTable hierarchy 复用，组头折叠）。
- `QueryInsightPanel` 搜索字段默认一行、超行折叠展开。
- `LovSelect` LOV LIST 模式增强远程搜索与已选标签。
- 补齐工具函数、store、DefaultList、组件测试；构建无错误；文档最小同步。

## 4. 不做什么

- 不改后端：不新增 API、不改 `Search(Pager)` 默认语义、不动 ViewProfile 线协议。
- 筛选操作符不含 不等于/包含/开头是/为空 等后端无法表达的算子（避免本地过滤与翻页完整性矛盾）。
- 不引入拖拽排序、任意查询表达式、跨实体数据源、多级 AND/OR 嵌套组（仅单层条件组 + 组级逻辑）。
- 分组不做后端聚合统计（组计数为前端本地对已加载数据的计数）；分组不跨页聚合。
- 分组折叠状态不持久化（仅会话内存），组字段配置才随视图保存。
- 不改变既有 CRUD 权限、数据权限、分页、六类视图与模板/表单域。

## 5. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0005 / OSC-0006 | Done：命名视图、ViewProfile store、六类视图与 ViewConfigDrawer 基线 |
| OSC-0009 | Done：搜索字段元数据、字段值格式化、GetPage 权威字段与 DefaultList 基线 |
| OSC-0012 | Done：FiltersJson/insight 域、effectiveSearch 唯一搜索状态、查询洞察面板 |
| OSC-0014 | Done：NamedView round-trip 保留未知字段（filter/group 可安全透传） |
| SPA-7 / DATA-4 / DATA-5 / SPA-15 | 本号消费的动态 CRUD、搜索、值集能力 |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| XUnit | 否 | 本号纯前端；后端无改动，不新增后端测试（既有回归由执行期构建验证） |
| ArcoVue Vitest | 是 | ViewFilter 解析/round-trip/非法归一、filterToSearchParams 序列化（eq/between/多选/AND/OR/清空）、groupRows 多级分组/计数/未知字段/折叠 key、QueryInsightPanel 折叠逻辑 |
| 组件测试 | 是 | FilterBuilderPopover 条件增删/逻辑切换/值控件复用/保存清除/互斥、GroupPopover 有序字段、ListTable 分组渲染、LovSelect 远程搜索 |
| 构建 | 是 | api-core 与 ArcoVue web 无错误构建 |
| 手工冒烟 | 是 | 筛选构建器保存→刷新→自动应用；AND/OR 生效与翻页完整；多级分组折叠；搜索面板展开；LOV LIST 远程搜索；与「保存到此视图」并存 |

## 7. 成功标准

- [ ] 用户可在构建器中构建多条件（等于/范围）筛选，应用后条件并入请求、翻页/导出完整，且随命名视图保存、重开自动应用。
- [ ] 用户可为命名视图配置最多 3 个字段的多级分组，表格内组头行展示字段值与计数，可折叠/展开；分组方案随视图保存。
- [ ] 筛选/分组与既有「保存到此视图」默认筛选并存，互不覆盖。
- [ ] Insight 搜索面板默认一行，超行显示「展开更多 N」并点击展开。
- [ ] 状态/枚举/单值 Lov 搜索字段下拉展示正确；LOV LIST 支持远程搜索与已选标签。
- [ ] 本 OSC 新增单测全部通过，相关构建无错误，事实性文档完成最小同步。
