# OSC-0008 — 表单提交归一化与抽屉历史评论

## 1. 为何做

**表单提交**：ArcoVue 在 NewLife.Cube（MVC 版 WebAPI，前后端分离）下「新增/编辑」可能失败。对比 NewLife.Cube.Vue 皮肤后确认根因：ArcoVue 把枚举/Lov 选项值强制字符串化后提交，而后端 `EntityController.Insert/Update` 走 System.Text.Json 默认模型绑定（MVC 版未注册 CubeNC 的 `EntityModelBinderProvider`），JSON 字符串无法绑定到 `Int32`/`Enum` 实体属性，返回「请求数据格式不正确」。

**历史（M4a）**：`RecordDrawer` 历史 Tab 为朴素 timeline，无分页、无筛选，长日志不可读。

**评论（M4b）**：后端 `EntityComment` 已就绪（OSC-0002），前端评论 Tab 仍是 stub。

本号合并三块，统一「记录抽屉」体验。

## 2. 已锁定范围

| # | 决策 |
|---|------|
| 1 | 表单修复采用**方案 A（前端提交类型归一化）**，不改后端模型绑定。 |
| 2 | 根因：枚举/选项值字符串化（`lov-api.ts` 的 `String(o.value)` + `dataSource: Record<string,string>`）→ 提交 `"1"` 无法绑 `Int32`/`Enum`。 |
| 3 | 字段名问题**不成立**：GetPage 响应走 FastJson `CamelCase=true`，`DataField.name` 为 camelCase，ArcoVue 的 `FieldMeta.name` 即 camelCase，与 Cube.Vue 一致；本号不实施字段名转换，仅以 AC 冒烟验证。 |
| 4 | 空值策略对齐 Cube.Vue：`Nullable` String 字段空值提交 `""`（避免 DB NOT NULL 报错）；数值/布尔/日期空值仍过滤。 |
| 5 | 多选字段保持 `join(",")`（XCode 多选约定逗号分隔）；AC 冒烟验证后端接受。 |
| 6 | M4a 历史增强：**分页 + Action 筛选 + 展示增强**；字段 diff 不做（Log 无结构化变更数据，需后端另起）。 |
| 7 | M4b 评论 Tab：api-core 新增 `createCommentApi`（GET/POST/DELETE `/Cube/EntityComment`）+ RecordDrawer 评论 Tab 真实实现（列表/发表/回复/删除）。 |
| 8 | 纯前端 + api-core 包；后端无改动（EntityComment 后端 OSC-0002 已就绪）。 |

## 3. 做什么

- `fieldControl.ts`：`serializeSubmitModel` 增加**类型归一化**——按字段元数据把字符串数字转 `number`、字符串布尔转 `boolean`，覆盖枚举/数值/Lov 字段。
- `submitPayload.ts`：`prepareSubmitPayload` 空值策略调整——`String` 字段空值提交 `""`；数值/布尔/日期空值过滤（保持）。
- 补 `submitPayload` 单测（类型归一化 + 空值矩阵）。
- `api-core`：新增 `createCommentApi`（`getList/post/remove`）+ `EntityComment`/`CommentModel` 类型 + 导出；修改后必须 `pnpm build`。
- `RecordDrawer.vue`：历史 Tab 增强（分页/筛选/展示）；评论 Tab 真实实现（顶层 + 同表回复 + 删除）。
- 回写迁移方案 M4a/M4b 状态、对接指南、web README。

## 4. 不做什么

- 不改后端模型绑定（不注册 `EntityModelBinderProvider`、不加 JSON 转换器）。
- 不做字段名大小写转换（调查结论不成立）。
- 不做历史字段 diff、不做恢复到旧版本。
- 不改评论后端 API（复用 OSC-0002 契约）。
- 不实现评论 @ 提及、附件、审核流。

## 5. 依赖

| 依赖 | 关系 |
|---|---|
| OSC-0002 | Done：EntityComment 后端三 API |
| OSC-0003 | Done：RecordDrawer 右侧抽屉/历史 Tab 骨架 |
| OSC-0007 | 进行中：视图工具栏与卡片布局（不冲突，仅同仓库并发） |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
|---|---|---|
| Vitest（submitPayload 归一化/空值） | **是** | 类型矩阵 + 空值矩阵 |
| Vitest（api-core comment 类型/URL） | **是** | api.spec 增 comment 用例 |
| `pnpm build` | **是** | ArcoVue web + api-core 均需构建 |
| XUnit | 否（N/A，无后端改动） | |
| 手工冒烟 | 是（新增含枚举实体、历史分页筛选、评论发布/回复/删除） | |

## 7. 成功标准

- [ ] 新增含枚举/Lov 数值字段的实体在 MVC 版后端可成功保存；提交 body 中数值字段为 `number`。
- [ ] 非必填 String 字段留空可保存（提交 `""` 而非缺字段）。
- [ ] 历史 Tab 支持分页与 Action 筛选，时间/操作人/成功状态清晰展示。
- [ ] 评论 Tab 可加载、发表、回复、删除（本人/管理员）；api-core comment API 类型正确。
- [ ] 本 OSC 新增单测全过；`pnpm test` 与 `pnpm build`（含 api-core）无错误。
- [ ] 迁移方案 M4a/M4b、对接指南、web README 已事实性回写。
