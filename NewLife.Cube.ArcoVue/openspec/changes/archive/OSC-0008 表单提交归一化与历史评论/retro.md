# OSC-0008 Retro

> 进入 `Done` 或 `Rejected` 后填写。记录实际结果、偏差、测试证据与可复用教训。

## 结果摘要

- 状态：Done（验收通过）
- 实际完成范围：
  - **表单提交类型归一化（方案 A）**：`normalizeSubmitValue` + `serializeSubmitModel` + `submitPayload` 空值矩阵（数值/布尔/枚举字符串→原生类型；String 空值提交 `""`）。
  - **api-core 评论 API**：`createCommentApi`（getList/post/remove，`/Cube/EntityComment`）+ `EntityCommentModel` 类型。
  - **历史 Tab（M4a）**：分页（20/页）+ Action 筛选（新增/更新/删除）+ 时间/操作人/成功失败徽章/Remark 换行。
  - **评论 Tab（M4b）**：三层嵌套回复（顶层 + 两级回复，最深不再展开）、发表/回复/删除本人、头像徽章。
  - **抽屉导航（追加）**：上一条/下一条图标化 + 禁用态；切换记录时历史/讨论同步重载。
- 与 proposal/design 的偏差：
  1. **评论层级加深**：design §6 规划「仅一层回复 + 顶层」；实施扩展为**三层**（顶层 + 两级回复，最深不再展开），回复编辑框内嵌于被回复评论内部。
  2. **头像徽章（新增）**：评论/回复/内嵌编辑框前展示用户头像徽章，无头像回落用户名首字符（中文取首字、英文取首字母大写）——新增 `UserAvatar` 组件 + `avatarInitial` 工具 + 单测。
  3. **抽屉导航（新增）**：左上「上一条/下一条」由文字改为图标（IconUp/IconDown）+ 禁用态；切换记录时历史/评论同步重载（watch 主键值而非对象引用）。
  4. **编辑表单空值根因（AC-22/23 增补）**：design §1 结论「字段名问题不成立」被部分推翻——GetPage 字段名 **PascalCase** ↔ GetList/GetDetail 数据 **camelCase** 不一致，表单 `model[field.name]` 直取为空；以 `normalizeKeysByFields` 在 `loadRecordIntoDrawer` 统一归一化修复；另补审计字段隐藏（`isAuditField`）。

## 验证证据

| 项 | 实际结果 | 证据/日期 |
|---|---|---|
| submitPayload 单测 | 7 用例全过 | 2026-08-04 验收重跑 |
| datetime 单测 | 3 用例全过 | 同上 |
| api-core comment 用例 | 3 用例全过（api-core 共 4） | 同上 |
| `pnpm test`（web） | 20 files / 131 tests passed | 2026-08-04 |
| `pnpm build`（api-core + web） | tsup + vue-tsc/vite 均成功（仅 chunk 体积警告） | 2026-08-04 |
| 手工冒烟（编辑 admin 用户） | RecordDrawer 表单字段有值；历史/评论 Tab 正常 | 2026-08-02 |

## 经验沉淀候选

- MVC 版无 `EntityModelBinderProvider`，JSON 字符串→数值绑定失败：前端提交类型归一化（`normalizeSubmitValue`）可复用于其它皮肤。
- `String(value)` 字符串化 dataSource 的设计约束应写入 Harness：新控件避免把枚举/Lov 值强制字符串化后提交。
- **字段名 PascalCase ↔ 数据 camelCase 不一致**：凡 `model[field.name]` 直取前必须先 `normalizeKeysByFields` 或走容错取值（已写入 lessons）。
- Vue `watch` 对象引用不响应原地 `Object.assign` 修改：记录切换刷新须 watch 主键值（`getValueByKey`）。
- Arco `a-comment` 嵌套回复放 default slot（`.arco-comment-inner-comment`）；`Array.from` 取 Unicode 首字符安全。
