# OSC-2610019c9d Tasks — 值集管理页

## T1 纯函数与路由

- [ ] T1-1 新建 `web/src/core/utils/lovAdmin.ts`：`lovPermFlags`、`filterLovRows`、`buildEnumSaveBody`、`buildListSaveBody`、`canSaveConfig`。
- [ ] T1-2 `lovAdmin.spec.ts` 覆盖 design §7。
- [ ] T1-3 `pageKind.ts` 的 `CUSTOM_TYPES` 加入 `admin/lov`。`useDynamicPage.ts` 增加 `isLovPage`。`DynamicPage.vue` 在 custom 且 `isLovPage` 时挂值集页。
- [ ] T1-4 `router/index.ts` 增加静态子路由 `Admin/Lov` → `DynamicPage`，`meta.typePath` 为 `Admin/Lov`。

## T2 列表与定义抽屉

- [ ] T2-1 `useLovPage.ts`：`GET /api/Admin/Lov`，关键字 `Q`，新增 POST、编辑 PUT、删除 DELETE。
- [ ] T2-2 `index.vue` 薄壳：表格列编码/名称/类型/启用/操作；右侧定义抽屉；删除确认。
- [ ] T2-3 按钮按 design §2 显隐。列表失败 alert。空列表文案「暂无手工值集」。

## T3 配置抽屉

- [ ] T3-1 `useLovConfig.ts` + `LovConfigDrawer.vue`：GetConfig；ENUM 与 LIST 分区；保存 SaveConfig；类型不可配时禁用保存。
- [ ] T3-2 保存失败不关闭抽屉。

## T4 验证与文档

- [ ] T4-1 `pnpm exec vitest run src/core/utils/lovAdmin.spec.ts`（工作目录 `NewLife.Cube.ArcoVue/web`）全部通过。
- [ ] T4-2 `pnpm exec vue-tsc -b` 无错误。
- [ ] T4-3 迁移方案补「值集管理页」一句。
