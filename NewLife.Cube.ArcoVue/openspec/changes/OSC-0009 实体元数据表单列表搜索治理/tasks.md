# OSC-0009 Tasks

> 进入 `Implementing` 后逐项勾选；每项完成后先跑对应测试再推进。

## T1 契约与字段分区统一

- [x] 1.1 新增 `fieldParts.ts` 的 `resolveFieldsForKind`：add→edit、edit→add、detail→edit→list、search/list 无回退；作为展示/回填/保存唯一字段来源。
- [x] 1.2 改造 `DefaultList.vue`：`drawerFields`、`loadRecordIntoDrawer`、`handleSave` 统一消费 `resolveFieldsForKind`；保留 `normalizeKeysByFields` 兼容。
- [x] 1.3 `lov-api.ts` / `types/lov.ts` 已具备并消费 LIST Meta 的 `valueField`、`labelField`、`tableColumns`、`searchFields`、`listConfig`（LovSelect/LovSelectTable 使用）。
- [x] 1.4 新增 `fieldParts.spec.ts`（4 用例：空分区、add/edit 互回退、detail 回退、detail 优先）。

## T2 控件、提交与详情语义

- [x] 2.1 调整 `resolveControl` / `resolveListControl`：静态 `dataSource` 优先于自动 Enum `lovCode`；Boolean 仍固定开关；LIST/无 dataSource 枚举仍走 LOV。`resolveSearchControl` 保持一致。
- [x] 2.2 `serializeSubmitModel` 多选识别改用 `normalizeItemType`（大小写不敏感）。
- [x] 2.3 Int64/UInt64：安全整数转 number，超安全整数保留字符串（`normalizeSubmitValue` / `FieldInput.onSelect` / `SearchFieldInput.onSelect`）。
- [x] 2.4 新增 `detailFormat.ts`：dataSource/多选/Boolean/JSON 摘要/URL/图片/文件安全渲染；`RecordDrawer` 接入富渲染。
- [x] 2.5 新增 `detailFormat.spec.ts`（4 用例）；`fieldControl.spec.ts` 增补 Int64/多选大小写/控件优先级（新增 3 用例）。

## T3 LIST LOV 与 BatchLabel 契约

- [x] 3.1 `LovController` 提取 `FetchRemoteList`（含 GET/POST、分页、固定参数、DataPath/TotalPath 解析）；LIST 反查 schema/语义写入 design。
- [x] 3.2 `BatchLabel` 改 async：LIST 按 `ValueField`/`LabelField` 分页权威反查（去重、大小写不敏感、页数上限、取完即止）；ENUM 行为保持。
- [x] 3.3 `LovSelect.vue`：LIST 单选/多选标签展示、`labelCache` + `BatchLabel` 历史值回显、清除/移除标签；向弹窗传 meta/multiple/modelValue。
- [x] 3.4 `LovSelectTable.vue`：动态列（`tableColumns`）、`rowKey=valueField`、searchFields 渲染（枚举引用下拉）、多选临时勾选 + 确认/取消。
- [x] 3.5 前端 Vitest 覆盖控件/提交/详情链路（142 全过）；**后端 XUnit 受限**：`XUnitTest` 引用 CubeNC 而非 MVC 版 `NewLife.Cube`，且 BatchLabel 依赖远端 HTTP，以 `dotnet build` 编译验证 + 前端 BatchLabel 消费链路替代（见 verify）。

## T4 搜索栏与多维列表一致性

- [x] 4.1 共享 label 逻辑：`renderCell`（列表/卡片/看板）+ `hydrateLovLabels`（BatchLabel 回写 dataSource）+ `SearchFieldInput`/`RecordDrawer` 消费同一 `dataSource`/BatchLabel 语义。
- [x] 4.2 搜索控件与表单控件同源（`resolveSearchControl`/`resolveControl` 一致优先 dataSource）；搜索值序列化含 Int64 安全与多选。
- [x] 4.3 table/tree/card/kanban 经 `format-cell="renderCell"`；calendar/gantt 使用标题/日期字段，不涉及标签映射；未知 LIST value 显示原始值安全文本。
- [x] 4.4 formatter 与搜索序列化已补逻辑测试；VTable 列 formatter 用法保持既有模式（无新增 API 依赖）。

## T5 验证与文档

- [x] 5.1 `npm run test`：22 files / 142 tests 全过；`npm run build`：vue-tsc + vite 成功（仅 chunk 体积警告）。
- [x] 5.2 `dotnet build NewLife.Cube`：0 错误（157 既有警告）；BatchLabel 行为以编译 + 前端消费链路验证（XUnit 受限原因见 T3.5）。
- [ ] 5.3 真实 MVC 后端新增/编辑/详情/搜索/六视图冒烟（Enum、状态、LIST 单多选、Int64、字段错误）——待可运行环境。
- [x] 5.4 迁移方案（编号顺延 + OSC-0009 行 + 差距表#11）、web README 已回写；核心接口架构/附录B 无 LOV 段落（BatchLabel 为既有接口行为增强，无新路径），功能清单无新增后端能力项，均无需改动。
