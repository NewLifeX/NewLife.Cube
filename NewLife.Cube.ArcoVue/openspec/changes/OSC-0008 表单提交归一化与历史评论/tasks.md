# OSC-0008 Tasks

> 进入 `Implementing` 后逐项勾选；每项完成后先跑对应测试再推进。

## T1 表单类型归一化（方案 A）

- [x] 1.1 `fieldControl.ts` 新增并导出 `normalizeSubmitValue`（数值/布尔/枚举字符串→原生类型）。
- [x] 1.2 `serializeSubmitModel` 改造：多选 join + 按字段元数据调用 `normalizeSubmitValue`。
- [x] 1.3 `submitPayload.ts` 空值矩阵：`String` 空值提交 `""`；数值/布尔/日期/其它空值过滤（保持）。
- [x] 1.4 `submitPayload.spec.ts` 补单测（数值/布尔/枚举归一化、多选 join、空值矩阵、主键过滤）——7 用例全过。

## T2 api-core 评论 API

- [x] 2.1 `types.ts` 新增 `EntityCommentModel`。
- [x] 2.2 `api.ts` 新增 `createCommentApi`（getList/post/remove，URL `/Cube/EntityComment`）。
- [x] 2.3 `index.ts` 导出 `createCommentApi` + 类型；`cube.ts` 的 `CubeApi` 与 `createCubeApi` 接入 `comment`。
- [x] 2.4 `api.spec.ts` 补 comment 用例（3 条）。
- [x] 2.5 `pnpm build`（api-core）成功，dist 已更新。

## T3 历史 Tab 增强（M4a）

- [x] 3.1 新增 `web/src/core/utils/datetime.ts` 的 `formatDateTime` + 单测（3 用例）。
- [x] 3.2 `RecordDrawer.vue` 历史 Tab：Action 筛选下拉（全部/新增/更新/删除）+ 分页（pageSize 20）。
- [x] 3.3 展示增强：时间格式化、操作人、成功/失败徽章、Remark `pre-wrap`。

## T4 评论 Tab 接线（M4b）

- [x] 4.1 `RecordDrawer.vue` 评论 Tab：加载顶层 + 回复（前端按 parentId 组装）、发表框、空态。
- [x] 4.2 回复交互（`@replyUser` 前缀 + 内联输入框 + 取消/发送）。
- [x] 4.3 删除（本人显示，后端兜底）+ 成功/失败反馈 + 刷新。

## T5 验证与文档

- [x] 5.1 `pnpm test`（web）：19 files / 123 tests passed。
- [x] 5.2 `pnpm build`（api-core 先，ArcoVue web 后）无错误。
- [x] 5.3 自动化门禁通过；浏览器手工冒烟见 verify.md（待真实环境）。
- [x] 5.4 回写迁移方案（M4a/M4b 状态、§10.4 差距表#1、§13 OSC-0008 行、总验收清单）、对接指南、web README。
