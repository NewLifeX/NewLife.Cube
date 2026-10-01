# OSC-2610019c9d Tasks — 值集管理页

## T1 纯函数与路由

- [x] T1-1 新建 `web/src/core/utils/lovAdmin.ts`：`lovPermFlags`、`filterLovRows`、`buildEnumSaveBody`、`buildListSaveBody`、`canSaveConfig`。
- [x] T1-2 `lovAdmin.spec.ts` 覆盖 design §7。
- [x] T1-3 `pageKind.ts` 的 `CUSTOM_TYPES` 加入 `admin/lov`。`useDynamicPage.ts` 增加 `isLovPage`。`DynamicPage.vue` 在 custom 且 `isLovPage` 时挂值集页。
- [x] T1-4 `router/index.ts` 增加静态子路由 `Admin/Lov` → `DynamicPage`，`meta.typePath` 为 `Admin/Lov`。

## T2 列表与定义抽屉

- [x] T2-1 `useLovPage.ts`：`GET /api/Admin/Lov`，关键字 `Q`，新增 POST、编辑 PUT、删除 DELETE。
- [x] T2-2 `index.vue` 薄壳：表格列编码/名称/类型/启用/操作；右侧定义抽屉；删除确认。工具栏「+ 添加值集」居左（对齐实体列表「添加记录」）。
- [x] T2-3 按钮按 design §2 显隐。列表失败 alert。空列表文案「暂无自定义值集」。

## T3 配置抽屉

- [x] T3-1 `useLovConfig.ts` + `LovConfigDrawer.vue`：GetConfig；ENUM 与 LIST 分区；保存 SaveConfig；类型不可配时禁用保存。
- [x] T3-2 保存失败不关闭抽屉。

## T4 验证与文档

- [x] T4-1 `pnpm exec vitest run src/core/utils/lovAdmin.spec.ts`（工作目录 `NewLife.Cube.ArcoVue/web`）全部通过。
- [x] T4-2 `pnpm exec vue-tsc -b` 无错误。
- [x] T4-3 迁移方案补「值集管理页」一句。

## T5 会话小任务（LIST 1:1 + 类型文案）

- [x] T5-1 LIST 配置对齐 Cube.Vue：listConfig 全字段、搜索字段/表格列编辑弹层、同源代理禁用、头栏类型/编码/来源。
- [x] T5-2 类型下拉与列表展示改为「枚举 / 自定义列表」（`lovTypeLabel`）。
- [x] T5-3 单测与 vue-tsc 回归通过。

## T6 会话小任务（404 / 文案）

- [x] T6-1 列表 CRUD 路径改为 `/Admin/Lov`（带前导 `/`），修复 `resolveRequestUrl` 拼成 `/apiAdmin/Lov` 的 404。
- [x] T6-2 空态「暂无自定义值集」；工具栏「+ 添加值集」。

## T7 会话小任务（表头样式 + 样例种子）

- [x] T7-1 值集列表表面/表头对齐实体 DefaultList（`list-surface` / `list-panel` / VTable `headerStyle` 色与字重）。
- [x] T7-2 `LovSampleSeeds`：是否/性别/优先级 ENUM + 用户 LIST 样例；`UseCube` 与 `Index` 幂等写入；单测覆盖。

## T8 会话小任务（行高 / 说明文 / 列宽）

- [x] T8-1 列表工具栏与表头/行高压缩（small + 紧凑 padding）。
- [x] T8-2 列表配置与搜索字段/表格列编辑弹窗：说明文字放到标签下方并左对齐。
- [x] T8-3 搜索字段/表格列表格固定列宽 + nowrap，避免「显示标题」「对齐」等折行。

## 测试记录

```
pnpm exec vitest run src/core/utils/lovAdmin.spec.ts src/core/utils/pageKind.spec.ts
→ 通过

pnpm exec vue-tsc -b
→ 0 errors
```
