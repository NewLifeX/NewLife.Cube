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

## T6 补充迭代：搜索角色 / 字段校验 / 地区级联 / 日期时间

- [x] 6.1 搜索框角色 LIST 单选：`LovSelect` LIST 单选改为 `a-select` 直显首页数据（按 Meta `valueField/labelField` 映射），“更多”按钮打开高级表格；移除 enum/select 的 `allow-search`，单/多选不再显示编辑光标。
- [x] 6.2 通用字段校验：新增 `validation.ts`（手机/电话/邮件/邮箱/网址，按 itemType 与字段名识别，空值不触发格式校验），`FormContent.rulesFor` 接入；新增 `validation.spec.ts`（7 用例）。
- [x] 6.3 地区级联：新增 `CascaderField.vue`（懒加载 `/Cube/Area` 子级、叶子值提交、向上回溯路径回显）；`fieldControl` 增 `cascader` 控件与 `isCascaderField`；`FieldInput`/`SearchFieldInput` 渲染级联；后端 MVC `UserController` 为 `AreaId` 补 `ItemType=area4`（对齐 NC 版）。
- [x] 6.4 日期时间：`datetime.ts` 重写为壁钟时间解析（`parseWallClock`/`formatDate`/`formatTime`/`formatDateTime`/`inferDateKind`/`toPickerValue`/`fromPickerValue`），避免 UTC 'Z' 串被换算到本地时区；`FieldInput` 按 `inferDateKind` 选择 date/datetime/time 组件与 `value-format`；`resolveSearchControl` 按 itemType 返回 `dateRange`/`datetimeRange`/`timeRange`。
- [x] 6.5 列表多维视图同步：新增 `fieldFormat.ts` `formatFieldValue`（日期/时间/字典/布尔/LOV 缓存/地区叶子），`DefaultList.renderCell` 改用它，CardList/KanbanBoard/ListTable 经 `format-cell` 同步；`detailFormat.detailText` 增日期格式化。
- [x] 6.6 测试：`datetime.spec.ts`（18）、`fieldFormat.spec.ts`（7）、`validation.spec.ts`（7）、`fieldControl.spec.ts` 增补 cascader/date 推断（+1 文件级用例）；全量 24 files / 172 tests 通过；`npm run build` 与 `dotnet build NewLife.Cube` 均成功。

## T7 徽标交互：Enable 点击启停 / 悬停光标 / 卡片看板徽标与高度

- [x] 7.1 后端 `EntityController` 新增 `SetEnable(Int64 id, Boolean enable)` action（`[HttpGet]`、`[EntityAuthorize(Update)]`，按 Enable 字段 `OnSetField` + `OnUpdate`，返回 `ApiResponse<TEntity>`）；`api-core.page.setEnable` 封装 `GET {type}/SetEnable?id=&enable=`。
- [x] 7.2 列表/树视图：`ListTable` 列定义增 `enableToggle`，Enable 徽标 `cursor: pointer` 且点击 emit `toggleEnable`（不进 `rowClick`）；`DefaultList` 标记 `enableToggle = isEnableField && canEdit` 并 `onToggleEnable` 调 `setEnable` 后刷新。
- [x] 7.3 列表/树视图：非 Enable 的状态/枚举/值集徽标 `cursor: default`——悬停鼠标不变（VTable badge 列 style 增加光标控制）。
- [x] 7.4 卡片/看板：`cardHelpers.CardBodyField` 增 `badge`（`resolveCellBadge`）与 `enableToggle`（`isEnableField`）；`RecordCard` 渲染徽标（浅底+同色文字），Enable 徽标可点击；`CardList`/`KanbanBoard` 透传 `toggleEnable` 至 `DefaultList`。
- [x] 7.5 卡片高度：`.card-list` 改 `align-items: start`，卡片高度按所显示字段自动伸缩；操作区以 grid 末行 + `margin-top:auto` + `justify-content:flex-start` 固定于各卡片左下角。
- [x] 7.6 测试：`fieldBadge.spec.ts` 增 `isEnableField`（4 断言）；api-core `api.spec.ts` 增 `setEnable` URL（1 用例）；web 24 files / 173 tests、api-core 5 tests 全过；`npm run build` 与 `dotnet build NewLife.Cube`（0 错误）通过。

## T8 徽标交互回退与卡片等高

- [x] 8.1 撤销自定义 `SetEnable` 接口，改用既有 `EnableOrDisableSelect`：后端 `EntityController` 暴露 `EnableSelect(keys, reason)` / `DisableSelect(keys, reason)`（`[HttpGet]` + `[EntityAuthorize(Update)]`，内部复用 `EnableOrDisableSelect` 的 OnSetField/日志/批量逻辑，与 NC 版对齐）。
- [x] 8.2 api-core `setEnable` 移除，改为 `enableSelect(type, keys, reason?)` / `disableSelect(...)`（`GET {type}/EnableSelect?keys=1,2`）；`DefaultList.onToggleEnable` 单条主键切换调用对应接口后刷新。
- [x] 8.3 看板视图徽标与卡片视图保持一致（核验）：`KanbanBoard` 经 `RecordCard` 渲染 `CardBodyField.badge`（状态/枚举/值集），Enable 徽标可点击——上一轮 T7.4 已落地，本轮无回归。
- [x] 8.4 卡片等高：`CardList` 挂载/数据或布局变化后测量所有 `.record-card` 最大高度，以 `min-height` 统一下发（`RecordCard` 增 `minHeight` prop，并入 `cardCssVars`），使所有卡片高度=后端返回全量对象中最高者；操作区仍经 grid 末行 + `margin-top:auto` 固定左下。
- [x] 8.5 测试：api-core `api.spec.ts` 改/增 `enableSelect`、`disableSelect` 用例（2 条）；web 24 files / 173 tests、api-core 6 tests 全过；`npm run build` 与 `dotnet build NewLife.Cube`（0 错误）通过。
- [x] 8.6 卡片内部间距收紧：`.record-card` grid `gap` 8px→4px、`.record-card-ops` `padding-top` 4px→2px，减小字段区与顶部标题/底部操作按钮的间隙；纯样式，构建成功。

## T9 看板/卡片徽标样式（宽度自适应 + 横向居中）

- [x] 9.1 `RecordCard` 徽标样式：`.record-card-badge` 增 `align-self: flex-start` + `max-width: 100%` + `box-sizing: border-box`——修复 vertical 布局（flex `column`）下交叉轴 `stretch` 把徽标拉伸到整行宽的问题；结合 `inline-block` + `white-space: nowrap`，徽标宽度严格按文案自适应。
- [x] 9.2 看板视图与卡片视图共用 `RecordCard`，样式修复同时作用于两者；Enable 徽标点击切换行为不受影响。
- [x] 9.3 横向排版（`fieldOrientation=horizontal`）下，`.record-card--orient-horizontal .record-card-field .record-card-badge` 增 `align-self: center`——徽标与前方标签垂直居中对齐，不再随 `align-items: baseline` 文本基线下沉；value 文本仍保持基线对齐。
- [x] 9.4 纯样式调整，无逻辑变更；`npm run build`（vue-tsc + vite）成功。
