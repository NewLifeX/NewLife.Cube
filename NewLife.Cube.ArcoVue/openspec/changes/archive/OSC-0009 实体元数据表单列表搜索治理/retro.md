# OSC-0009 Retro

> 进入 `Done` 或 `Rejected` 后填写。记录实际结果、偏差、测试证据与可复用教训。

## 结果摘要

- 状态：Done（验收通过，2026-08-05 归档）。
- 计划范围：ArcoVue 通用实体表单、详情、搜索栏与六类多维列表的字段元数据统一；后端补强 LIST `BatchLabel` 权威反查。
- 明确不在范围：Cube.Vue 前端改造、Int64 JSON 契约变更、低代码表单/跨实体查询、未净化 HTML 渲染。

## 实际完成范围

- **后端**：`LovController` 提取 `FetchRemoteList`；`BatchLabel` 改 async，LIST 按 `ValueField/LabelField` 分页权威反查（去重、大小写不敏感、页数上限、取完即止），ENUM 行为保持。
- **后端（补充迭代）**：`EntityController` 暴露 `EnableSelect/DisableSelect`（复用既有 `EnableOrDisableSelect` 的 OnSetField/日志/批量，与 NC 版对齐，`[HttpGet]` + `[EntityAuthorize(Update)]`）；`UserController` 为 `AreaId` 补 `ItemType=area4`。
- **api-core**：`enableSelect/disableSelect`（GET {type}/EnableSelect?keys=）封装，与后端契约对齐。
- **ArcoVue**：
  - `fieldParts.ts`：`resolveFieldsForKind` 分区回退（detail→edit→list 等），`DefaultList` 展示/回填/保存同源 + 分区 GetFields 兜底。
  - `fieldControl.ts`：静态 `dataSource` 优先于自动 Enum `lovCode`；多选 itemType 大小写不敏感；Int64/UInt64 安全整数转 number、超安全保留字符串；cascader/date/datetime/time 控件推断。
  - `LovSelect.vue` / `LovSelectTable.vue`：LIST 值集按 Meta 动态列/搜索/rowKey/value·label 映射；搜索框 LIST 单选直显首页数据 +「更多」高级表格；历史值 `BatchLabel` 权威回显与缓存；多选临时勾选 + 确认/取消；移除 `allow-search` 编辑光标。
  - `detailFormat.ts` + `RecordDrawer`：字典/多选/Boolean/JSON 摘要/URL/图片/文件安全富渲染；`fieldFormat.ts` 统一六视图展示格式化。
  - `validation.ts` + `FormContent`：手机/电话/邮件/邮箱/网址格式校验；后端 `FieldErrors` 映射到 Arco 表单字段。
  - `CascaderField.vue`：地区级联懒加载 `/Cube/Area` 子级、叶子值提交、回溯路径回显。
  - `datetime.ts`：壁钟时间解析（parseWallClock 等），按 itemType 推断 date/datetime/time 组件，避免 UTC `Z` 时区漂移。
  - 徽标交互：`fieldBadge.ts` + `ListTable`/`cardHelpers`/`RecordCard`——Enable 徽标可点击启停（列表/树/卡片/看板），非 Enable 徽标悬停光标 default；卡片/看板状态字段渲染徽标且宽度自适应；卡片等高（min-height 下发）与操作区固定左下；横向排版徽标垂直居中；卡片内部间距收紧。
- **文档**：迁移方案（OSC-0009 行、编号顺延 0009→0011、差距表#11）、web README 回写；验收时修正 3 处 `SetEnable` 残留（附录B_API参考 / Api·实体控制器 / DATA-实体控制器）。

## 与设计的偏差

- 无范围性偏差；T6~T9 为验收前通过会话窗口追加的补充迭代（搜索/校验/级联/日期、徽标交互与回退、卡片等高与样式），均并入本 OSC 任务与 status 注释，未改变 GetPage 契约、未新增值集 API 路径（EnableSelect/DisableSelect 为既有 `EnableOrDisableSelect` 的暴露）、未改六类视图布局语义。
- 初版自定义 `SetEnable` 接口按用户要求整体撤销，回退复用既有 `EnableOrDisableSelect`（见 T8.1/note4）。
- 后端 XUnit 因测试宿主引用差异受限（`XUnitTest` 引用 CubeNC），以编译 + 前端消费链路验证替代（详见 verify）。

## 验证证据

| 项 | 实际结果 | 证据/日期 |
| --- | --- | --- |
| ArcoVue 逻辑/组件测试 | 24 files / 173 tests passed | `npm run test -- --run`（2026-08-05） |
| api-core 测试 | 1 file / 6 tests passed | `npm run test`（2026-08-05） |
| 后端 XUnit | 受限（宿主引用差异），以编译验证替代 | `dotnet build NewLife.Cube.csproj`（2026-08-05） |
| web 构建 | vue-tsc + vite 成功（仅 chunk 体积警告） | `npm run build`（2026-08-05） |
| .NET 构建 | 0 错误 / 0 警告（增量） | `dotnet build NewLife.Cube.csproj`（2026-08-05） |
| 手工/E2E 冒烟 | 待真实 MVC 环境（新增/编辑/详情/搜索/六视图冒烟矩阵） | 已记录，不阻塞归档 |
| 验收编排 | 实现审计无缺口；代码审查无高危（8 项中/低记残留）；文档同步修正 3 处 SetEnable 残留 | verify.md（2026-08-05） |

## 经验沉淀候选

- `GetPage` 的字段分区必须是表单、详情、搜索和多视图的唯一字段表达事实源。
- LIST 标签回显必须由服务端按实际 `ValueField/LabelField` 反查，不能用前端分页数据猜测。
- `Int64AsString` 是跨端精度保护，任何前端类型归一化都必须区分安全整数与标识值。
- 字段显示/搜索/列表视图应共享 resolver，避免同一状态字段在不同界面出现不同标签。
- Enable 启停复用既有批量接口（`EnableOrDisableSelect`）优于新造 `SetEnable`：少一条 API 路径、与 NC 双栈对齐。
- 徽标在 flex column 交叉轴会被 `stretch` 拉伸，需 `align-self:flex-start`；横向布局需 `align-self:center` 防基线下沉。
- 卡片等高用「测量最大高度 → min-height 下发」而非 flex stretch，可避免视觉拉伸；操作区以 grid 末行 + `margin-top:auto` 固定左下。
- 验收前会话窗口小任务应在验收时并入 tasks.md 相似任务或新增任务项，保持单 OSC 追踪闭环。
