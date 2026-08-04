# OSC-0008 Verify

> 进入 `Validating` 后逐项勾选；自动化门禁 + 关键手工冒烟通过后进入 `Done`。

## 验收标准（AC）

### 表单提交修复（方案 A）

- [ ] AC-01：提交含枚举/Lov 数值字段的实体时，请求 body 中该字段值为 `number`（如 `roleId: 1` 而非 `roleId: "1"`）。
- [ ] AC-02：`normalizeSubmitValue` 对数值字符串 `"123"`/`"12.5"` 转 number；非数值字符串原样保留；number 输入幂等。
- [ ] AC-03：`"true"/"1"`→`true`、`"false"/"0"`→`false`（Boolean 字段）。
- [ ] AC-04：Enum 字段字符串数字 `"1"` 转 `1`。
- [ ] AC-05：空值矩阵生效——数值空过滤、String 空提交 `""`、Boolean `false` 保留。
- [ ] AC-06：MVC 版后端新增含枚举字段实体成功（不再返回「请求数据格式不正确」）。
- [ ] AC-07：非必填 String 字段留空可保存（不触发 DB NOT NULL 错误）。

### api-core 评论 API

- [ ] AC-08：`createCommentApi` 三方法 URL/方法正确：`GET /Cube/EntityComment`、`POST /Cube/EntityComment`、`DELETE /Cube/EntityComment?id=`。
- [ ] AC-09：`CubeApi.comment` 暴露且类型正确；`pnpm build`（api-core）后 ArcoVue `vue-tsc` 可见。

### 历史 Tab（M4a）

- [ ] AC-10：历史 Tab 显示 Action 筛选下拉，切换后重置第 1 页并重新加载。
- [ ] AC-11：历史 Tab 分页器生效（pageSize 20，总数为 `/Admin/Log` 分页结果）。
- [ ] AC-12：时间以 `YYYY-MM-DD HH:mm:ss` 展示；操作人、成功/失败徽章、Remark 换行可见。
- [ ] AC-13：新建记录不显示历史（mode=add 保持无历史 Tab）。

### 评论 Tab（M4b）

- [ ] AC-14：评论 Tab 加载当前记录评论（顶层 + 回复缩进）；无评论显示空态。
- [ ] AC-15：可发表评论，成功后清空输入框并刷新列表。
- [ ] AC-16：可回复（`@replyUser` 前缀 + 内联输入），提交带 `parentId`。
- [ ] AC-17：本人评论显示删除并可删除；他人评论不显示删除（后端仍兜底）。
- [ ] AC-18：只读实体/新建记录不显示评论 Tab（保持 `showSideTabs` 语义）。

### 质量

- [ ] AC-19：本 OSC 新增单测全过；`pnpm test` 通过。
- [ ] AC-20：`pnpm build`（api-core + ArcoVue web）无错误。
- [ ] AC-21：迁移方案 M4a/M4b 状态、§10.4 差距表#1、对接指南、web README 已事实性回写。- [x] AC-22（增补）：点击「编辑」打开 RecordDrawer 时表单字段有值——GetPage 字段名 PascalCase 与 GetDetail 数据 camelCase 不一致，已通过 `normalizeKeysByFields` 按字段元数据归一化 formModel key（url.ts + DefaultList.loadRecordIntoDrawer），浏览器验证 admin 用户编号/名称/昵称/性别/密码均有值。
- [x] AC-23（增补）：新增/编辑表单不显示审计字段（创建/更新用户、IP、时间）——`fieldControl.ts` 的 `isAuditField` 按字段名匹配（CreateUser/ID、CreateIP、CreateTime、UpdateUser/ID、UpdateIP、UpdateTime），`FormContent` 新增/编辑均过滤；浏览器验证 Admin/User 编辑表单「更新者」分组消失，`isAuditField` 单测覆盖 8 字段名 + 非审计字段。
## 自动化门禁

```bash
cd NewLife.Cube.ArcoVue/packages/api-core && pnpm build
cd NewLife.Cube.ArcoVue/web && pnpm test && pnpm build
```

## 手工冒烟步骤

1. 用含枚举/Lov 数值字段的实体新增一条：确认提交 body 数值为 number、保存成功。
2. 非必填 String 字段留空保存：确认不报 DB NOT NULL。
3. 打开记录详情 → 历史 Tab：筛选「更新」、翻页，确认时间/操作人/徽章/换行正确。
4. 打开记录详情 → 评论 Tab：发表 → 回复 → 删除本人评论；用他人账号确认无删除按钮。
5. 只读实体与新建记录：确认无历史/评论 Tab。

## 执行记录

| 项 | 结果 |
|---|---|
| `pnpm test`（web） | 19 files / 126 tests passed（含 submitPayload 7、datetime 3、url 6、fieldControl 7） |
| `pnpm test`（api-core） | 4 tests passed（含 comment 3） |
| `pnpm build`（api-core） | tsup 成功，dist 含 `comment` API 类型 |
| `pnpm build`（web） | vue-tsc + vite ok |
| 手工冒烟（ArcoVue dev 5184） | 编辑 admin 用户：RecordDrawer 表单编号/名称/昵称/性别/密码均有值；历史/评论 Tab 正常 |

> 说明：编辑表单空值根因 = GetPage 字段名 PascalCase ↔ GetList/GetDetail 数据 camelCase 大小写不一致；已用 `normalizeKeysByFields` 修复并经浏览器验证。
