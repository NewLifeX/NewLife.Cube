# OSC-0009 — 实体元数据表单、列表与搜索治理

## 1. 为何做

ArcoVue 的通用实体页已经以 `GetPage`、`GetList`、`GetDetail` 和 CRUD API 驱动，但字段元数据在表单、详情、搜索栏和六类列表视图中的解释并不一致。结果是枚举、状态、静态值集、LIST 值集和关联实体字段会出现控件选择错误、编辑回填标签缺失、提交类型不安全、搜索条件错误或列表显示原始值等问题。

已确认的根因包括：

1. `detailFields` 为空时，`drawerFields` 回退到 `listFields`，而 `loadRecordIntoDrawer` 仍用空的 `detailFields` 归一化数据，详情可能全部为 `-`。
2. 后端 `DataField.PrepareForApi()` 可同时返回 `dataSourceMap` 与自动 `lovCode=Enum.*`；ArcoVue 编辑控件优先取 `lovCode`，错误绕开已完整的静态字典。
3. LIST LOV 元数据包含 `valueField`、`labelField`、`searchFields`、`tableColumns`，ArcoVue 却硬编码 `id/value/label/name`，无法通用处理业务关联表。
4. LIST 的旧值标签没有权威批量反查；前端不能通过拉取第一页猜测标签。
5. 搜索栏、VTable 与 card/tree/kanban/calendar/gantt 的值显示没有统一复用字段标签解析，状态和关联字段在不同视图中可能表现不同。
6. `Int64AsString=true` 是后端契约；前端将所有 Int64 转 `Number` 会损失超过安全整数范围的主键/外键。

## 2. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | 本号主目标是 ArcoVue 通用实体的添加、编辑、详情、搜索和多维列表的一致字段行为。 |
| 2 | 为保证 LIST 旧值正确回显，允许且必须修改 `NewLife.Cube` 的 `LovController.BatchLabel`；它是通用 API 契约补强，不实施 Cube.Vue 前端改造。 |
| 3 | 后端 `GetPage` / `DataField` 是字段表达唯一事实源。前端只规范化、选择控件和呈现，不根据字段名猜业务关系。 |
| 4 | 静态 `dataSourceMap`（枚举、状态、布尔）优先本地控件；仅 LIST 或无静态字典的 LOV/枚举进入 `LovSelect` / Lookup 链路。 |
| 5 | LIST 标签由后端按 `valueField` / `labelField` 权威反查；禁止以加载列表第一页作为回显方案。 |
| 6 | 详情优先支持字典/多选、Boolean、日期时间、URL、图片/文件、JSON 摘要；HTML/Markdown 不使用未经净化的 `v-html`。 |
| 7 | 搜索栏与 table/tree/card/kanban/calendar/gantt 必须共享同一字段值显示与过滤值序列化规则。 |

## 3. 做什么

- 在 ArcoVue 建立视图种类一致的字段分区解析、键名归一化和显示值格式化入口。
- 修复新增、编辑、详情字段 fallback 与回填来源不一致的问题；补齐 `GetFields` 分区兜底。
- 调整控件优先级和提交归一化：静态字典优先，LIST LOV 专用；保护 Int64/UInt64 精度；统一识别多选 `itemType`。
- 扩展 LOV 元数据、列表选择器、多选确认和标签缓存：动态列、动态搜索、`valueField` / `labelField`、单选/多选均按 Meta 工作。
- 扩展后端 `BatchLabel`，使 LIST 可按权威配置批量反查历史 value 的标签。
- 搜索栏、VTable 和其他五类视图统一展示字典/LIST 标签，并按字段元数据提交搜索条件。
- 补齐逻辑、组件/API、后端和必要端到端测试；同步事实性接口与能力文档。

## 4. 不做什么

- 不改 `NewLife.Cube.Vue/web/core` 或旧 Vue 壳；后续以独立 OSC 对齐。
- 不改后端 `Int64AsString=true` JSON 契约，不以全局 `Number` 规避类型问题。
- 不引入任意 HTML 渲染、富文本编辑器、跨实体查询或新的低代码表单引擎。
- 不实现 LIST 值集的“拉取首页猜标签”、不将 `LovEnumItem` 当作远端 LIST 的通用数据源。
- 不改变现有六类视图的布局、排序或写回能力；仅修复其字段显示与搜索数据解释。

## 5. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0003 | Done：动态 CRUD、FieldInput、SearchFieldInput、RecordDrawer 基线 |
| OSC-0005 / OSC-0006 | Done：VTable 与六类多维视图基线 |
| OSC-0008 | Done：提交归一化、key 归一化和 RecordDrawer 基线 |
| DATA-1 / DATA-4 / DATA-5 / DATA-6 / DATA-11 / SYS-16～20 | 本号消费/补强的 Cube 元数据和值集能力 |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| ArcoVue Vitest | 是 | 字段 fallback、控件优先级、显示/提交归一化、LOV Meta 映射、搜索与多视图值格式化 |
| ArcoVue 组件测试 | 是 | `LovSelect` / `LovSelectTable` 的动态列、单选、多选确认、旧值回显 |
| XUnit | 是 | LIST `BatchLabel` 的 Meta、批量反查、空值/未知值/权限与兼容 ENUM |
| 构建 | 是 | ArcoVue web、受影响 .NET 项目均构建无错误 |
| 手工/E2E 冒烟 | 是 | 覆盖新增、编辑、详情、搜索和六类视图的枚举/状态/LIST/Int64 场景 |

## 7. 成功标准

- [ ] 新增、编辑、详情对静态字典、Enum、Boolean、LIST 单选/多选和关联字段使用正确控件与标签。
- [ ] `detail` 元数据缺失时仍能按回退字段正确加载和显示详情。
- [ ] LIST 使用任意合法 `valueField` / `labelField` / `tableColumns` / `searchFields`，可选择、提交、回显和批量显示历史标签。
- [ ] 搜索栏和六类列表视图显示与详情一致的标签，不泄露内部原始值；搜索请求值符合字段类型。
- [ ] Int64/UInt64 主外键不发生精度丢失；安全数值与字符串的处理有测试证据。
- [ ] 本 OSC 新增单测全部通过，相关 web 与 .NET 构建无错误；文档按实际 API/行为增量同步。
