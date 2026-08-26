# OSC-26081903c0 Tasks

## A AI 浮窗

- [x] A.1 `createConfigApi.getAiConfig` + api-core 单测：GET `/Cube/GetAiConfig`（服务路径，无 `/api`）。
- [x] A.2 `aiMarkdown.ts` / `aiSse.ts` / `aiChatContext.ts` / `aiWelcome.ts` 纯函数 + spec（XSS、SSE 行、area/controller、query Base64 失败变空串、问候名、快捷行 Tab 归属）。
- [x] A.3 `web` 增加 `marked` 依赖；`AiAssistant.vue` 薄模板 + `useAiAssistant.ts`：GetAiConfig、FAB、**右侧停靠面板**（380px/100vh，最大化 inset 20px）、SSE fetch（绝对路径 `/Ai/AiChat`、`/Ai/OperationResult`）、token/租户头、session/最大化 localStorage。禁止 Element Plus。
  - marked 18 须 `new Renderer()` 再覆盖 `html`/`code`，不可传残缺 renderer 对象（否则 `paragraph is not a function`）。
- [x] A.4 `RootLayout.vue` 挂载；登录路由不挂。`appStore.aiContext`（含 queryB64、page 映射表）；DefaultList/RecordDrawer 与 DefaultObject 登记/注销 `applyFill`；`fill_form` 的 `kind/values` 与 Cube.Vue 一致。
- [x] A.5 空会话：问候 + 推荐/提问/分析 Tab + 建议列表（design §2.4/§2.5）；「更多」含深度+清空；底栏无附件、无深度勾选。流式中禁止第二路请求；run_js 截断 8192。不做历史/搭建。

## B 批量启停

- [x] B.1 `resolveBatchEnableState` + `viewMapping.spec.ts` 真值表（含 selected=0、>200、enableSelect=false、无 Enable、非 table）。
- [x] B.2 `listContext.advancedVisible` 纳入 `canEdit` 与 `batchEnableState.visible`。
- [x] B.3 `DefaultList` 高级菜单两项 + `useListCrud.confirmBatchEnable`：确认框、200 上限、成功清选择并 `loadData`、失败 `formatApiError`。
  - Arco `Modal.confirm` 须带 `content`（`ModalConfig` 必填），标题文案仍按 design。
- [x] B.4 不改 `onToggleEnable` 行内徽标；不改后端 EnableSelect。

## C 条件填色

- [x] C.1 Schema：`ViewFormatRule`（apply=`cell|side|row|column`，**无**嵌套 filter）；`normalizeFormat`/`serializeNamedView`/`MANAGED_VIEW_KEYS`；spec：非法色丢弃、未知 apply 丢弃、>50 截断、缺省 []、误传 `{filter:ViewFilter}` 丢弃。
- [x] C.2 `viewFormat.ts`：`newFormatRule`、`moveFormatRule`、`formatApplyOptions`、`ruleMatchesRow`、`resolveCellFormatColor`（跳过 side；row/cell；**column 无条件铺该字段列**）、`resolveRowSideColor`、`resolveCardTitleFormatColor`（仅 row）。spec：双通道可同时命中；空值 eq 不命中；整列不看操作符/值。
- [x] C.3 `evpStore.updateFormat`/`getFormat` + store spec（仿 updateFilter；空数组不写 viewsJson）。
- [x] C.4 `activePopover` 增加 `'format'`，与筛选/分组互斥。`DefaultList` 在分组与搜索之间插入「填色」+ 数量徽标（点徽标清除）。**table/tree/card** 显示按钮；kanban/calendar/gantt 不显示。
- [x] C.5 `FormatPopover.vue` + `useFormatPopover.ts`：标题「设置填色条件」；打开无规则时用第一字段种一条；弹层 `width:max-content`；规则行=柄+3×10 预置色板（含「文字加粗」）+范围+字段[+操作符+值]+删；**`apply=column` 只选字段**；范围顺序 **单元格 / 行侧边 / 整行 / 整列**；**card 仅行侧边+整行，新建 `apply=side`**。满 50 禁用；改即 `updateFormat`。无智能配色、无弹层 AI 输入、无应用/保存页脚。
- [x] C.6 拖拽换序走 `moveFormatRule`；工具栏图标经 IconPark 确认后注册；柄用已有 `drag`。
- [x] C.7 `useListTable` 数据列背景；实体行最左 3px 竖条（`resolveRowSideColor`）；**整行同时涂勾选/操作列**，分组头不铺。`CardList`→`RecordCard`：`side` 涂卡片左缘，`row` 涂 `.record-card-title`。看板可同源下发但不显示按钮。日历/甘特不绘制。
- [x] C.8 **不**改 `ViewConfigDrawer`（无条件格式 Tab）。

## 测试 / 构建 / 文档

- [x] T.1 `pnpm --filter @cube/api-core test` 与 `@cube/arco-vue test` 新增用例全过。
- [x] T.2 `pnpm --filter @cube/arco-vue build` 0 error。
- [x] T.3 手工冒烟见 verify AC-01…（实现侧已按 AC 对照自检；浏览器环境冒烟归验收）。
- [x] D.1 同步 `web/README.md`、`Doc/功能清单.md` SPA-7、`Doc/Api/核心接口架构.md`（若缺 Ai 行）、迁移方案 §3.1/§10.4、竞品分析 §6.1 #1#3#4 与文首版本注记。
