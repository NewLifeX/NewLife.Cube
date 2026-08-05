# OSC-0009 Design — 实体元数据表单、列表与搜索治理

## 1. 目标与契约边界

本号把 `GetPage` / `GetFields` 的字段元数据统一解释为五种行为：**字段分区、控件选择、值显示、搜索值序列化、提交值序列化**。同一字段在添加、编辑、详情、搜索和 table/tree/card/kanban/calendar/gantt 中必须共享行为，不允许按组件分别猜测。

后端保持 `ControllerBaseX` 的 camelCase JSON 与 `Int64AsString=true`。`DataField.PrepareForApi()` 返回的 `dataSourceMap`、`lovCode`、`category`、`multiple`、`itemType`、`typeName`、`url`、`target` 是前端权威输入。LIST 的远端数据列与历史标签反查以 `/Admin/Lov/Meta` 和 `/Admin/Lov/BatchLabel` 为准。

涉及功能清单：DATA-1、DATA-4、DATA-5、DATA-6、DATA-11、SYS-16～20、SPA-7。

## 2. 文件级改动地图

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `NewLife.Cube.ArcoVue/web/src/views/crud/DefaultList.vue` | 增加唯一的字段分区 resolver；使 `drawerFields`、`loadRecordIntoDrawer`、保存和搜索 fallback 同源；映射后端 FieldErrors | 既有路由、分页、权限与 RecordDrawer 事件 API |
| `web/src/core/types/field.ts` | 补齐 LOV Meta、列、搜索和标签缓存所需类型 | 既有 `FieldMeta` 公共字段兼容性 |
| `web/src/core/utils/fieldNormalize.ts` | 规范化 GetPage/DataField/LOV Meta 的大小写与 dataSource | `pickDataSource` 的旧输入兼容 |
| `web/src/core/utils/fieldControl.ts` | 统一控件优先级、多选识别、Int64 安全提交与值显示入口 | 已有 audit 字段识别和公开函数兼容 |
| `web/src/core/utils/submitPayload.ts` | 仅消费统一字段/序列化策略 | add 模式移除主键、String 空值现有兼容规则 |
| `web/src/core/utils/lov-api.ts` | 扩展 Meta/ListData/BatchLabel 类型、缓存与错误语义 | 既有 ENUM/Lookup 请求路径 |
| `web/src/components/FieldInput.vue` | 使用安全数值/静态字典优先策略，向 LOV 传递多选与当前值 | 非 LOV 控件的 props/emits |
| `web/src/components/SearchFieldInput.vue` | 复用字段控件决策和搜索值序列化 | 搜索表单现有 emits |
| `web/src/components/LovSelect.vue` | Meta 驱动的 enum/list、显示标签缓存、LIST 单多选回显 | 对外 v-model 契约 |
| `web/src/components/LovSelectTable.vue` | 动态列/搜索/row key/value/label，多选暂存与确认 | 弹窗关闭事件名称 |
| `web/src/views/crud/RecordDrawer.vue` | 用统一 detail renderer 输出高频类型 | 历史、评论、导航行为 |
| `web/src/features/**`、`web/src/views/crud/**` 中的 table/tree/card/kanban/calendar/gantt adapter | 接入共享 display resolver，不改变布局能力 | ViewProfile、排序、分组、拖拽边界 |
| `NewLife.Cube/Areas/Admin/Controllers/LovController.cs` | 扩展 LIST `BatchLabel` 按权威配置反查 | ENUM BatchLabel 与 Meta/ListData 既有路由 |
| `NewLife.Cube/**/Lov*Tests.cs`（按实际现有测试位置） | 新增 LIST 反查与兼容 XUnit | 无关 Controller 测试 |
| `web/src/**/*.spec.ts` | 新增字段、LOV、搜索、视图 formatter 与组件用例 | OSC-0008 已验证行为 |

## 3. 字段分区与状态唯一来源

### 3.1 分区回退矩阵

| 请求上下文 | 首选 GetPage 分区 | 空时回退 | GetFields 兜底 | 禁止 |
| --- | --- | --- | --- | --- |
| list | `list` | 无 | `ViewKinds.List` | 以 detail/add 猜列表字段 |
| add | `addForm` | `editForm` | `ViewKinds.Add` | 使用 list 审计字段替代 |
| edit | `editForm` | `addForm` | `ViewKinds.Edit` | 使用 detail 覆盖可编辑字段 |
| detail | `detail` | `editForm → list` | `ViewKinds.Detail` | 详情 UI 与回填使用不同字段集 |
| search | `search` | 无 | `ViewKinds.Search` | 从 list 字段名推断查询条件 |

`resolveFieldsForKind(kind)` 是唯一入口。`DefaultList` 将其结果存为各分区状态；`drawerFields`、`loadRecordIntoDrawer`、`prepareSubmitPayload`、RecordDrawer detail renderer、SearchFieldInput 和视图 formatter 只接受该解析后的 `FieldMeta[]`。

### 3.2 键名与原始行

`normalizeKeysByFields` 继续负责 PascalCase 元数据名与 camelCase 数据键的兼容。它只为 `formModel` 生成字段别名，**不修改**列表原始 row；显示 formatter 用 `getValueByKey(row, field.name)` 读取，避免改写 VTable/视图数据源。

## 4. 控件与值类型矩阵

### 4.1 控件优先级

| 条件（按顺序判断） | add/edit 控件 | search 控件 | list/detail 显示 |
| --- | --- | --- | --- |
| `itemType` 是明确自定义控件 | 对应控件 | 对应可搜索版本/安全降级 | 对应安全 renderer |
| Boolean | switch | select/checkbox（以现有搜索交互为准） | 是/否或 dataSource 标签 |
| 有非空 `dataSource` | 本地 select/multi-select | 本地 select/multi-select | dataSource 标签 |
| LOV Meta 类别为 LIST | `LovSelect` / 多选 | `LovSelect` / 多选 | BatchLabel/缓存标签 |
| `lovCode=Enum.*` 且无 dataSource | ENUM LOV/Lookup | ENUM LOV/Lookup | Lookup 标签 |
| 日期/时间、数值、Guid、文本 | 现有类型控件 | 现有类型控件 | 通用安全 renderer |

`dataSource` 优先于自动 Enum `lovCode`。只有明确为 LIST 的值集才进入 LIST 弹窗；实现时可先加载 Meta 判别，不得通过 lovCode 文本猜测 LIST。

### 4.2 提交与搜索值矩阵

| 字段 | 输入值 | 提交/搜索值 | 说明 |
| --- | --- | --- | --- |
| Int16/Int32/Decimal/Double 等 | 合法数字字符串 | `Number` | 保持 OSC-0008 数值绑定修复 |
| Int64/UInt64 | 超安全整数或主/外键字符串 | 原字符串 | 保护精度，后端既有 JSON/模型绑定契约需以测试确认 |
| Int64/UInt64 | 安全整数且控件提供 number | number 或原值，按后端测试确定 | 不得先转再转回造成精度差 |
| Boolean | `true/false/1/0` | boolean | `false` 不是空值 |
| Enum/静态值集 | 选项 key | 保留字段底层类型 | 显示标签与原始 key 分离 |
| 多选 | 数组 | 现有 XCode 逗号字符串约定 | 识别 itemType 时忽略大小写 |
| LIST | 单值/数组 | `valueField` 的原始值/多选约定 | 禁止提交 label 或硬编码 id |

## 5. LIST LOV 与 BatchLabel

### 5.1 前端 Meta schema

`LovMeta` 至少包含：

```ts
interface LovMeta {
  kind: 'enum' | 'list'
  valueField?: string
  labelField?: string
  tableColumns?: Array<{ name: string; displayName?: string; typeName?: string; width?: number }>
  searchFields?: Array<{ name: string; displayName?: string; typeName?: string; dataSource?: Record<string, string> }>
  listConfig?: Record<string, unknown>
  options?: Array<{ value: unknown; label: string }>
}
```

非法/缺失 `valueField` 或 `labelField` 的 LIST Meta 视为不可选：保留当前安全文本、展示错误/重试，不把 `id`、`name` 当默认替代。现有后端明确提供该字段时方可工作。

### 5.2 前端选择与回显状态

`LovSelect` 的唯一状态为 `modelValue`；`displayLabels` 是按 `lovCode + rawValue` 缓存的派生状态，不反向修改模型。`LovSelectTable` 接收 `meta`、`multiple`、`modelValue`：

| 模式 | 点击行 | 关闭 | 外部 model 更新 |
| --- | --- | --- | --- |
| 单选 | 选中一项 | 立即关闭 | 立即以 `valueField` 更新 |
| 多选 | 切换临时勾选 | 不关闭 | 仅点击确认后一次更新 |
| 多选取消 | 丢弃临时勾选 | 关闭 | 不变 |
| 清除 | 清空 | 不适用 | `null` 或 `[]`，依字段多选语义 |

LIST 旧值标签优先缓存，缺失时请求 BatchLabel；不从 ListData 页内容推断。

### 5.3 后端 BatchLabel schema 与行为

实施时读取现有 `LovBatchLabelRequest/Response`，以兼容增量方式增加 LIST 所需上下文，最小目标为：请求携带 `lovCode` 和去重后的 `values`；服务端由 LOV 配置解析远端 ListData，按 `ValueField` 匹配并返回 `{ value, label }`。如远端 API 支持批量筛选，应使用配置支持的权威过滤参数；若不支持，应定义受控逐值/分页反查上限与失败响应，禁止静默只查第一页。

| 输入 | 返回 |
| --- | --- |
| ENUM value | 保持现有 enum 标签行为 |
| LIST 已知 value | 对应 `LabelField` 文本 |
| 空 value | 不返回条目 |
| 重复 value | 去重查询，响应最多一项 |
| 未知 value | 不虚构标签；前端保留原值安全文本 |
| 配置/远端失败 | 明确 API 错误，前端保留旧缓存并给重试入口 |

## 6. 详情、搜索与多维视图

### 6.1 详情 renderer

| 字段特征 | 呈现 | 安全规则 |
| --- | --- | --- |
| dataSource / ENUM / LIST | 标签、多个标签 | 原始值无标签时显示安全文本 |
| Boolean | 是/否 | 不将字符串 truthy 误作 true |
| Date/DateTime | 本地既有日期 formatter | 无效值 `-` |
| URL | 新窗口安全链接 | 仅 http/https 等白名单协议 |
| 图片 | 缩略图 + 失败占位 | 不执行 SVG/HTML 内容 |
| 文件 | 文件名/下载链接 | 显示名转义 |
| JSON | 截断摘要，可展开 `<pre>` 文本 | 不 `v-html` |

### 6.2 搜索和视图 adapter

`SearchFieldInput` 使用 §4 同一 control resolver 与 §4.2 搜索序列化；无可用 LOV Meta 时不发送伪造 label，仅保留原条件或禁用控件。列表 display resolver 返回显示文本/安全展示描述，table/tree/card/kanban/calendar/gantt 只消费此派生结果，不修改 row。VTable 的 formatter、列刷新和生命周期用法不确定时，实施前必须读取官方 ListTable/Methods 文档。

## 7. 后端字段错误

保存响应包含 `FieldErrors` 时，`DefaultList` 将错误 key 先按当前 edit/add 字段的 PascalCase/camelCase 别名匹配，再调用 Arco Form 官方支持的字段错误 API；无匹配项才显示全局错误。执行前必须查 Arco Design Vue 官方 Form 文档确认实例 API。

## 8. 核心文档影响

| 文档路径 | 影响 | 说明 |
| --- | --- | --- |
| `Doc/Api/核心接口架构.md` | 修改 | 事实性补充 LIST BatchLabel 请求/响应与失败语义 |
| `Doc/附录B_API参考.md` | 修改 | 补 `/Admin/Lov/Meta`、`ListData`、`BatchLabel` 字段说明 |
| `Doc/功能清单.md` | 修改 | 更新 DATA/SYS/SPA 相关实现与测试事实 |
| `Doc/Api/ArcoVue企业中后台迁移方案.md` | 修改 | 在差距/后续里程碑中事实性登记 OSC-0009 |
| `NewLife.Cube.ArcoVue/web/README.md` | 修改 | 更新通用 CRUD、LOV、测试说明 |

不创建新的长期文档；既有 Markdown 仅做事实性最小增量。

## 9. 测试设计

| 目标 | 自动化证据 |
| --- | --- |
| 分区 fallback 与 key 归一 | Vitest：detail 空、edit/add 回退、Pascal/camel 行数据 |
| 控件优先级 | Vitest：dataSource + Enum lovCode、LIST Meta、Boolean、多选大小写 |
| Int64 | Vitest：MAX_SAFE_INTEGER 两侧、主外键字符串、序列化幂等 |
| LOV 前端 | 组件/Vitest：动态 value/label/列/搜索、单选、多选确认取消、缓存和未知值 |
| BatchLabel | XUnit：ENUM 兼容、LIST 自定义字段、重复/空/未知、远端失败 |
| 搜索/多视图 | Vitest：过滤值及六类 adapter 同一 resolver 输出 |
| 字段错误 | 组件测试或 API mock：camel/Pascal FieldErrors 映射与全局回退 |

## 10. 风险与回滚

| 风险 | 缓解 |
| --- | --- |
| BatchLabel 远端接口不支持值过滤 | 先明确 ListConfig 可表达的过滤契约；不能安全反查时返回明确错误，禁止首页猜测 |
| 控件优先级改变影响现有 ENUM | ENUM/dataSource/Lookup 回归用例；保留现有 Meta 缓存 API |
| Int64 字符串与后端绑定兼容不明 | 先添加端到端/XUnit 绑定证据，再固定提交策略 |
| 多视图各自格式化漂移 | 只暴露一个 display resolver，adapter 不复制映射逻辑 |
| Arco/VTable API 假设错误 | 执行阶段先查官方文档，关键组件补测试 |
