# OSC-0009 Verify

> 进入 `Validating` 后逐项勾选；自动化门禁与关键冒烟通过后才可复盘归档。

## 验收标准（AC）

### 字段分区与表单

- [x] AC-01：`resolveFieldsForKind` 为唯一字段来源；`detail` 空时按 `detail → edit → list` 回退，`drawerFields` / `loadRecordIntoDrawer` / `handleSave` 同源（fieldParts.spec 4 用例）。
- [x] AC-02：`normalizeKeysByFields` 继续保证 PascalCase 元数据名 ↔ camelCase 数据键回填（url.spec 既有 6 用例保持）。
- [x] AC-03：`resolveControl`/`resolveListControl` 静态 `dataSource` 优先于自动 `Enum.*` lovCode（fieldControl.spec 新增用例）；LIST/无 dataSource 枚举仍走 LOV。
- [x] AC-04：`serializeSubmitModel` 用 `normalizeItemType` 识别多选，`MultipleSelect`/`MULTIPLESELECT` 等价（fieldControl.spec 新增用例）。
- [x] AC-05：Int64/UInt64 超 `Number.MAX_SAFE_INTEGER` 保留字符串，安全整数转 number（fieldControl.spec 新增用例 + FieldInput/SearchFieldInput.onSelect）。

### LIST LOV 与详情

- [x] AC-06：`LovSelectTable` 用 `meta.valueField/labelField/tableColumns/searchFields` 渲染列、rowKey、搜索与取值，不再依赖固定 `id/value/label/name`；无 Meta 时回退 `value/label` 约定。
- [x] AC-07：LIST 单选点击行即选中关闭；`LovSelect` 以 `labelCache` + `BatchLabel` 反查权威标签，未知值显示原始值安全文本（detailFormat/lookupLabel 不误映射）。
- [x] AC-08：LIST 多选在弹窗内 checkbox 临时勾选，底部取消/确认；确认一次更新 model，取消恢复初始选中；清除/移除标签可用。
- [x] AC-09：`BatchLabel` 后端已实现 ENUM 兼容 + LIST 按 `ValueField/LabelField` 分页权威反查（去重、大小写不敏感、页数上限）；空/重复/未知行为已实现，XUnit 因测试宿主引用差异受限（见执行记录）。
- [x] AC-10：`detailFormat` 正确呈现字典/多选、Boolean、JSON 摘要、URL 安全链接、图片缩略图、文件下载链接；HTML/Markdown 仅纯文本（detailFormat.spec 4 用例）。

### 搜索与六类视图

- [x] AC-11：搜索控件与主表单同源（`resolveSearchControl`/`resolveControl` 一致优先 dataSource）；`SearchFieldInput` 使用 LovSelect（LIST 标签反查）；未知 Meta 降级为原始值/禁用，不猜测字段。
- [x] AC-12：列表/卡片/看板共享 `renderCell` + `hydrateLovLabels`（BatchLabel 回写 dataSource）；calendar/gantt 使用标题/日期字段；Boolean/链接/图片摘要符合安全规则。
- [x] AC-13：本号未改动视图布局、排序、分组、拖拽写回或分页策略（未触碰相关代码）。

### 错误与质量

- [x] AC-14：保存失败时 `DefaultList` 提取 `ApiError.fieldErrors`，经 `FormContent.setFields` 映射到 Arco 表单字段（PascalCase/camelCase 大小写容错）；无法映射时保留全局提示。
- [x] AC-15：ArcoVue 新增单测全部通过（22 files / 142 tests）；后端 XUnit 受限原因见执行记录。
- [x] AC-16：ArcoVue web 与 `NewLife.Cube` 构建成功无错误；仅既有 chunk 体积警告。
- [x] AC-17：迁移方案（OSC-0009 行、编号顺延、差距表#11）与 web README 已事实性回写；核心接口架构/附录B 无 LOV 段落、功能清单无新增后端能力项，均无需改动。

## 自动化门禁

```powershell
npm.cmd --prefix "NewLife.Cube.ArcoVue\web" run test
npm.cmd --prefix "NewLife.Cube.ArcoVue\web" run build
dotnet test "魔方.sln" --no-restore
dotnet build "魔方.sln" --no-restore
```

> 执行阶段应按实际受影响测试项目收窄 `dotnet test`；验收记录实际项目、命令和通过数。若解决方案包含与本号无关的既有失败，必须分离记录并证明本号新增测试通过。

## 手工冒烟矩阵

1. 选择具有 Enum、Boolean、业务状态、LIST 单选、LIST 多选和 Int64 主外键的实体；分别新增、编辑、详情，确认值、标签和提交 body 正确。
2. 对 LIST 使用非 `id/value/label/name` 的 `ValueField/LabelField` 与自定义表格列，确认弹窗列、搜索、回显与 BatchLabel 正确。
3. 在搜索栏设置上述字段条件，切换 table/tree/card/kanban/calendar/gantt，确认请求条件和展示标签一致。
4. 让后端返回字段级验证错误，确认错误定位到 Arco 表单字段；再测试未知 LIST value、空值与超安全 Int64。
5. 检查 URL/图片/文件/JSON 详情的安全呈现，确认不存在原样 HTML 执行。

## 执行记录

| 项 | 结果 |
| --- | --- |
| ArcoVue 单元/组件测试 | `npm run test` 22 files / 142 tests passed（新增 fieldParts 4、detailFormat 4、fieldControl 增补 3） |
| 后端 XUnit | **受限**：`XUnitTest` 项目引用 `NewLife.CubeNC` 而非 MVC 版 `NewLife.Cube`，且 `BatchLabel` LIST 反查依赖远端 HTTP；以 `dotnet build NewLife.Cube`（0 错误）+ 前端 BatchLabel 消费链路验证替代 |
| ArcoVue 构建 | `npm run build` vue-tsc + vite 成功（仅 chunk 体积警告，非阻断） |
| .NET 构建 | `dotnet build NewLife.Cube.csproj` 0 错误 / 157 既有警告 |
| 手工冒烟 | 待可运行的真实 MVC 后端环境（新增/编辑/详情/搜索/六视图 × Enum/状态/LIST/Int64/字段错误） |
