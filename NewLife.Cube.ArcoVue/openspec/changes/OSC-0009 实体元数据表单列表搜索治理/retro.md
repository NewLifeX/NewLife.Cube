# OSC-0009 Retro

> 进入 `Done` 或 `Rejected` 后填写。记录实际结果、偏差、测试证据与可复用教训。

## 结果摘要

- 状态：Draft，待实施。
- 计划范围：ArcoVue 通用实体表单、详情、搜索栏与六类多维列表的字段元数据统一；后端补强 LIST `BatchLabel` 权威反查。
- 明确不在范围：Cube.Vue 前端改造、Int64 JSON 契约变更、低代码表单/跨实体查询、未净化 HTML 渲染。

## 实际完成范围

- **后端**：`LovController` 提取 `FetchRemoteList`；`BatchLabel` 改 async，LIST 按 `ValueField/LabelField` 分页权威反查（去重、大小写不敏感、页数上限、取完即止），ENUM 行为保持。
- **ArcoVue**：
  - `fieldParts.ts`：`resolveFieldsForKind` 分区回退（detail→edit→list 等），`DefaultList` 展示/回填/保存同源 + 分区 GetFields 兜底。
  - `fieldControl.ts`：静态 `dataSource` 优先于自动 Enum `lovCode`；多选 itemType 大小写不敏感；Int64/UInt64 安全整数转 number、超安全保留字符串。
  - `LovSelect.vue` / `LovSelectTable.vue`：LIST 值集按 Meta 动态列/搜索/rowKey/value·label 映射；历史值 `BatchLabel` 权威回显与缓存；多选临时勾选 + 确认/取消。
  - `detailFormat.ts` + `RecordDrawer`：字典/多选/Boolean/JSON 摘要/URL/图片/文件安全富渲染。
  - `FormContent` + `DefaultList`：后端 `FieldErrors` 映射到 Arco 表单字段。
  - `FieldInput`/`SearchFieldInput`：Int64 安全选择值。
- **文档**：迁移方案（OSC-0009 行、编号顺延 0009→0011、差距表#11）、web README 回写；tasks/verify 记录测试证据。

## 与设计的偏差

- 待实施；如扩大到 Cube.Vue、改变 GetPage 契约、增加新值集 API 或改变六类视图布局，必须先更新 proposal/design 并重新批准。

## 验证证据

| 项 | 实际结果 | 证据/日期 |
| --- | --- | --- |
| ArcoVue 逻辑/组件测试 | 待执行 | — |
| 后端 XUnit | 待执行 | — |
| web 构建 | 待执行 | — |
| .NET 构建 | 待执行 | — |
| 手工/E2E 冒烟 | 待执行 | — |

## 经验沉淀候选

- `GetPage` 的字段分区必须是表单、详情、搜索和多视图的唯一字段表达事实源。
- LIST 标签回显必须由服务端按实际 `ValueField/LabelField` 反查，不能用前端分页数据猜测。
- `Int64AsString` 是跨端精度保护，任何前端类型归一化都必须区分安全整数与标识值。
- 字段显示/搜索/列表视图应共享 resolver，避免同一状态字段在不同界面出现不同标签。
