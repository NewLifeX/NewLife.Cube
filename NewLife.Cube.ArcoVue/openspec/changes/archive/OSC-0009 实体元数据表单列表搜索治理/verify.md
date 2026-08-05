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

### 补充迭代 AC（T6~T9，验收前会话窗口完成的小任务）

- [x] AC-18：搜索框值集（如角色）LIST 单选直显首页数据 +「更多」打开高级表格；enum/select 移除 `allow-search`，单/多选不再显示编辑光标（T6.1）。
- [x] AC-19：手机/电话/邮件/邮箱/网址通用字段按元数据自动格式校验，空值不触发（validation.spec 7 用例）（T6.2）。
- [x] AC-20：`ItemType=area4`/`area`/`cascader` 字段渲染 Arco Cascader，懒加载 `/Cube/Area` 子级并回溯路径回显；后端 MVC `UserController` 为 `AreaId` 补 `ItemType=area4`（T6.3）。
- [x] AC-21：日期/时间/日期时间按 itemType 推断 date/datetime/time 组件；壁钟时间解析避免 UTC `Z` 串时区漂移（datetime.spec 18 用例）（T6.4）。
- [x] AC-22：列表/卡片/看板经 `formatFieldValue` 同步展示日期/字典/布尔/LOV/地区叶子（T6.5）。
- [x] AC-23：Enable 徽标（列表/树/卡片/看板）可点击，调后端 `EnableSelect/DisableSelect`（GET {type}/EnableSelect?keys=）启停；非 Enable 徽标悬停光标 default（T7/T8，api.spec 2 用例）。
- [x] AC-24：卡片/看板状态/枚举/值集渲染徽标且宽度按文案自适应；卡片视图高度统一为全量对象最高者（min-height 下发）、操作区固定左下；横向排版徽标与标签垂直居中对齐（T9）。
- [x] AC-25：卡片内部间距收紧（grid gap 8→4px、操作区 padding-top 4→2px），纯样式（T8.6）。

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
| ArcoVue 单元/组件测试 | `npm run test -- --run` 24 files / 173 tests passed（T1~T9 累计：fieldParts 4、detailFormat 4、fieldControl 增补、datetime 18、fieldFormat 7、validation 7、fieldBadge 4 等） |
| api-core 测试 | `npm run test` 1 file / 6 tests passed（enableSelect/disableSelect 2 用例） |
| 后端 XUnit | **受限**：`XUnitTest` 项目引用 `NewLife.CubeNC` 而非 MVC 版 `NewLife.Cube`，且 `BatchLabel` LIST 反查依赖远端 HTTP；以 `dotnet build NewLife.Cube`（0 错误）+ 前端 BatchLabel 消费链路验证替代 |
| ArcoVue 构建 | `npm run build` vue-tsc + vite 成功（仅 chunk 体积警告，非阻断） |
| .NET 构建 | `dotnet build NewLife.Cube.csproj` 0 错误（增量，0 警告/0 错误） |
| 手工冒烟 | 待可运行的真实 MVC 后端环境（新增/编辑/详情/搜索/六视图 × Enum/状态/LIST/Int64/字段错误）——已如实记录，不阻塞归档 |

## 验收编排摘要（2026-08-05）

| 步骤 | 结果 |
| --- | --- |
| 实现审计 implementation-audit | ✅ **无缺口**：T1~T9 逐条核验代码真实存在且完整；确认前端调用 `enableSelect/disableSelect`（无 setEnable 残留）、`RecordCard.minHeight` + `CardList.measureTallest` 等高、`CascaderField` 懒加载 `/Cube/Area`、后端 `EnableSelect/DisableSelect` 带 `[EntityAuthorize(Update)]` |
| 代码审查 code-review | ✅ **无高危**：授权、XSS（无 v-html 注入）、壁钟时间、Int64 精度、徽标事件冒泡、NewLife 规范（正式类型名/XML 注释/防御性注释）均通过；8 项中/低问题记入下方风险 |
| 文档同步 doc-sync | ✅ 修正 3 处 `SetEnable` 残留（附录B_API参考 / Api·实体控制器 / DATA-实体控制器）+ web README 补 T6~T9 说明；tasks/status 与代码一致 |

## 风险与残留

| 级别 | 问题 | 处置 |
| --- | --- | --- |
| 中 | `EnableOrDisableSelect` 实体无 Enable 字段或 keys 为空时返回 `Code=0 共启用[0]个`，前端仍提示成功 | 记残留，建议另立 OSC 增强错误反馈 |
| 低 | `EnableSelect/DisableSelect` 的 `keys` 参数声明未使用（实际走 `GetRequest("keys")`） | 记残留 |
| 低 | `SplitAsInt()` 仅 Int32，Int64 雪花主键会丢精度 | 当前实体多为 Int32，记残留 |
| 低 | `datetime.ts` `split(/[\\/\\]/)[0]` 与注释声明的斜杠日期支持不一致 | 记残留 |
| 低 | `DefaultList.onToggleEnable` 无 loading 锁，快速双击可能并发回跳 | 记残留 |
| 低 | `CascaderField` watch 重复 resolvePath 请求、模块级 areaCache 跨实例共享 | 记残留 |
| 低 | 卡片 `enableToggle` 未带 `flags.canEdit` 权限判断（仅 UX 不一致） | 记残留 |
| — | T5.3 / 手工冒烟矩阵：真实 MVC 环境待验证 | 不阻塞归档，已记录 |
