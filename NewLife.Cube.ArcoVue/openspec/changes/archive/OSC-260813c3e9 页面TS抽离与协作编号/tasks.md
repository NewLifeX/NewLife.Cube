# OSC-260813c3e9 Tasks

每个 T 完成后：把本条改为 `[x]`，并跑「命令」列。未列出的文件禁止改。

---

## A. 规范核对（创建期可能已落地 → 核对后勾选，禁止回退 max+1）

- [x] ### T01 `openspec-create.agent.md`

- 输入：`NewLife.Cube.ArcoVue/openspec/agents/openspec-create.agent.md`
- 输出：同一文件。必须含 `OSC-YYMMDDxxxx`、生成步骤（含 archive 查重）、**禁止** `最大号 +1`/`max+1`。全文 **不得** 出现 DeepSeek /「一次会话只执行 1 个未勾选 T」。
- 保留：状态机 Draft、五件套清单、前端框架官方链接。
- 命令：不跑 Vitest；回复贴出上述关键字所在行号。

- [x] ### T02 `openspec-approve.agent.md`

- 输入/输出：`openspec/agents/openspec-approve.agent.md`
- 必须：前缀定位；检查表含新号格式 `OSC-YYMMDDxxxx`；**无** DeepSeek/Flash 粒度项。
- 命令：同 T01（贴行号）。

- [x] ### T03 `openspec-apply.agent.md`

- 必须：前缀定位；SFC 构薄；**无** DeepSeek 执行粒度节。
- 命令：贴行号。

- [x] ### T04 `openspec-verify.agent.md`

- 只改 verify。必须：前缀定位；提示 `复盘 OSC-YYMMDDxxxx`。
- 命令：贴行号。

- [x] ### T05 `openspec-retro.agent.md`

- 必须：前缀定位；归档保持原文件夹名（新号与历史号）。
- 命令：贴行号。

- [x] ### T06 `openspec/README.md` + `changes/README.md` + `archive/README.md`

- 本 T **只改** `NewLife.Cube.ArcoVue/openspec/README.md`
- 必须：编号规则、SFC 职责分离、触发示例用 `OSC-260813c3e9`。
- 命令：贴行号。

- [x] ### T07 `changes/README.md`

- 只改 `openspec/changes/README.md`。新号示例 + 历史豁免。
- 命令：贴行号。

- [x] ### T08 `archive/README.md`

- 只改 `openspec/changes/archive/README.md`。
- 命令：贴行号。

- [x] ### T09 `harness/lessons.md`

- 只改格式小节为 `OSC-YYMMDDxxxx`。**禁止删除**任何历史 `## OSC-00xx` 条目。
- 命令：贴行号。

- [x] ### T09b 迁移方案编号段落

- 只改 `NewLife.Cube.ArcoVue/ArcoVue企业中后台迁移方案.md`
- 必须：§9.2 使用 `OSC-YYMMDDxxxx`、不再写「按落地顺序严格递增」或 DeepSeek 执行粒度；§10.1 禁止抢号且无「小模型/DeepSeek」条；§13 标明历史表；§14 新号格式。
- 命令：贴行号。

---

## B. 机械门禁

- [x] ### T10 新建 `sfcThin.spec.ts`

- 输入：无（新建）
- 输出：`NewLife.Cube.ArcoVue/web/src/core/utils/sfcThin.spec.ts`
- 符号：`ALLOWLIST` 初始 = design §4.1 全部 32 个 posix 路径；禁止 token 正则见 design §2.2 / §7。
- 保留：不改 `vitest.config.ts`、不改其它 spec。
- 命令：`pnpm --filter @cube/arco-vue exec vitest run --config vitest.config.ts src/core/utils/sfcThin.spec.ts`
- 结果：48 passed（1 ALLOWLIST 存在性 + 47 .vue 扫描）

---

## C. DefaultList

- [x] ### T11a `listContext.ts`

- 输入：`web/src/views/crud/DefaultList.vue`
- 输出：**新建** `web/src/views/crud/listContext.ts`
- 符号：design §5.2 全部绑定；`createListContext` `ListContext`。
- 本 T **不改** `.vue`（下一 T 才删）。允许 vue 暂时与新文件重复，直到 T11g。
- 保留：函数仍留在 vue。
- 命令：`pnpm --filter @cube/arco-vue test`
- 结果：355 全绿。注：chartSeq/tableResizeObserver 改 ref 以保跨闭包可变；measureTableHeight/renderCell 因依赖 ctx 状态且多域共用定义于本文件（design 表内部循环依赖时保行为优先）。

- [x] ### T11b `useListQuery.ts`

- 新建 `web/src/views/crud/useListQuery.ts`
- 符号：design §5.3 `useListQuery` 函数列表；参数 `ctx: ListContext`。
- 不改 `.vue`。
- 命令：`pnpm --filter @cube/arco-vue test`
- 结果：355 全绿。

- [x] ### T11c `useListCrud.ts`

- 新建 `useListCrud.ts`；符号见 §5.3。交叉 `loadData` 用第二参（design §5.3 表）。
- 命令：`pnpm --filter @cube/arco-vue test`
- 结果：355 全绿。第二参实际为 { loadData, openEdit, openDetail }（onTableAction 需 openEdit/openDetail，按源码核实）。

- [x] ### T11d `useListViews.ts`

- 新建 `useListViews.ts`；符号见 §5.3。
- 命令：`pnpm --filter @cube/arco-vue test`
- 结果：355 全绿。第二参 { loadData, applySearchToForm }（onSwitchView 需 applySearchToForm）。

- [x] ### T11e `useRecordNav.ts`

- 新建 `useRecordNav.ts`；符号见 §5.3。
- 命令：`pnpm --filter @cube/arco-vue test`
- 结果：355 全绿。

- [x] ### T11f `useDefaultList.ts`

- 新建 `useDefaultList.ts`；组装 + 原 `watch`/`onMounted`/`onBeforeUnmount`/`bootstrap`。
- 不改 `.vue`。
- 命令：`pnpm --filter @cube/arco-vue test`
- 结果：355 全绿。

- [x] ### T11g 构薄 `DefaultList.vue`

- 只改 `web/src/views/crud/DefaultList.vue`
- 按 design §5.4 删除已搬走的 script；调用 `useDefaultList(props)`。
- 从 `sfcThin.spec.ts` 的 `ALLOWLIST` 删除 `views/crud/DefaultList.vue`。
- 保留：template、style、props `type` `authId`、子组件 import。
- 命令：`pnpm --filter @cube/arco-vue test`
- 结果：355 全绿；vue-tsc -b 无错误；文件 1837→694 行。

---

## D. 其余抽离（每文件两 T：先建 composable，再构薄 vue）

标准命令（D 组每条）：`pnpm --filter @cube/arco-vue exec vitest run --config vitest.config.ts src/core/utils/sfcThin.spec.ts`

T-a 不改 ALLOWLIST；T-b 删除对应路径。T-a 之后若尚未构薄，vue 与 ts 可暂时重复。

- [x] ### T12a 新建 `features/vtable/useListTable.ts`（函数清单 design §6）
- [x] ### T12b 构薄 `features/vtable/ListTable.vue` + 移出 allowlist
- [x] ### T13a 新建 `views/crud/useViewConfigDrawer.ts`
- [x] ### T13b 构薄 `views/crud/ViewConfigDrawer.vue` + 移出 allowlist
- [x] ### T14a 新建 `features/views/useGanttView.ts`
- [x] ### T14b 构薄 `features/views/GanttView.vue` + 移出 allowlist
- [x] ### T15a 新建 `views/crud/useRecordDrawer.ts`
- [x] ### T15b 构薄 `views/crud/RecordDrawer.vue` + 移出 allowlist（保留模板 `placement="right"`）
- [x] ### T16a 新建 `features/views/useCardList.ts`（继续 import 现有 `cardHelpers.ts`）
- [x] ### T16b 构薄 `features/views/CardList.vue` + 移出 allowlist
- [x] ### T17a 新建 `components/useLovSelect.ts`
- [x] ### T17b 构薄 `components/LovSelect.vue` + 移出 allowlist
- [x] ### T18a 新建 `components/useLovSelectTable.ts`
- [x] ### T18b 构薄 `components/LovSelectTable.vue` + 移出 allowlist
- [x] ### T19a 新建 `views/crud/useFormLayoutDrawer.ts`
- [x] ### T19b 构薄 `views/crud/FormLayoutDrawer.vue` + 移出 allowlist
- [x] ### T20a 新建 `features/search/useInsightPanel.ts`
- [x] ### T20b 构薄 `features/search/InsightPanel.vue` + 移出 allowlist
- [x] ### T21a 新建 `components/useCascaderField.ts`
- [x] ### T21b 构薄 `components/CascaderField.vue` + 移出 allowlist
- [x] ### T22a 新建 `features/views/useCalendarMonth.ts`
- [x] ### T22b 构薄 `features/views/CalendarMonth.vue` + 移出 allowlist
- [x] ### T23a 新建 `views/crud/useViewTabsToolbar.ts`
- [x] ### T23b 构薄 `views/crud/ViewTabsToolbar.vue` + 移出 allowlist
- [x] ### T24a 新建 `views/crud/useFilterBuilderPopover.ts`
- [x] ### T24b 构薄 `views/crud/FilterBuilderPopover.vue` + 移出 allowlist
- [x] ### T25a 新建 `components/useFieldInput.ts`
- [x] ### T25b 构薄 `components/FieldInput.vue` + 移出 allowlist
- [x] ### T26a 新建 `views/login/useLoginPage.ts`
- [x] ### T26b 构薄 `views/login/index.vue` + 移出 allowlist
- [x] ### T27a 新建 `views/login/useRegisterPage.ts`
- [x] ### T27b 构薄 `views/login/register.vue` + 移出 allowlist
- [x] ### T28a 新建 `components/useSearchFieldInput.ts`
- [x] ### T28b 构薄 `components/SearchFieldInput.vue` + 移出 allowlist
- [x] ### T29a 新建 `features/search/useQueryComboButton.ts`
- [x] ### T29b 构薄 `features/search/QueryComboButton.vue` + 移出 allowlist
- [x] ### T30a 新建 `features/views/useKanbanBoard.ts`
- [x] ### T30b 构薄 `features/views/KanbanBoard.vue` + 移出 allowlist
- [x] ### T31a 新建 `views/login/useForgotPassword.ts`
- [x] ### T31b 构薄 `views/login/forgot-password.vue` + 移出 allowlist
- [x] ### T32a 新建 `layouts/useMixLayout.ts`
- [x] ### T32b 构薄 `layouts/mix.vue` + 移出 allowlist
- [x] ### T33a 新建 `views/crud/useFormContent.ts`
- [x] ### T33b 构薄 `views/crud/FormContent.vue` + 移出 allowlist
- [x] ### T34a 新建 `views/crud/useGroupPopover.ts`
- [x] ### T34b 构薄 `views/crud/GroupPopover.vue` + 移出 allowlist
- [x] ### T35a 新建 `views/settings/useAppearanceDrawer.ts`
- [x] ### T35b 构薄 `views/settings/AppearanceDrawer.vue` + 移出 allowlist
- [x] ### T36a 新建 `features/search/useSearchDrawer.ts`
- [x] ### T36b 构薄 `features/search/SearchDrawer.vue` + 移出 allowlist
- [x] ### T37a 新建 `features/views/useRecordCard.ts`
- [x] ### T37b 构薄 `features/views/RecordCard.vue` + 移出 allowlist
- [x] ### T38a 新建 `views/settings/useAppearancePage.ts`
- [x] ### T38b 构薄 `views/settings/appearance.vue` + 移出 allowlist
- [x] ### T39a 新建 `views/crud/useNamedViewsToolbar.ts`
- [x] ### T39b 构薄 `views/crud/NamedViewsToolbar.vue` + 移出 allowlist
- [x] ### T40a 新建 `views/crud/useListChartModal.ts`
- [x] ### T40b 构薄 `views/crud/ListChartModal.vue` + 移出 allowlist
- [x] ### T41a 新建 `views/dynamic/useDynamicPage.ts`（保持不读 layout/theme store）
- [x] ### T41b 构薄 `views/dynamic/DynamicPage.vue` + 移出 allowlist
- [x] ### T42a 新建 `components/useTagsView.ts`
- [x] ### T42b 构薄 `components/TagsView.vue` + 移出 allowlist

---

## E. 审计（只读确认，禁止新建 composable）

- [x] ### T43 审计 15 个已薄文件

- 输入：design §4.2 清单
- 输出：不改代码；在 `status.md` 的 note 追加 15 行 `audit ok: <path> scriptLines=<n>`
- 若任一文件出现 `watch(` / `onMounted(` / `onBeforeUnmount(` / `onUnmounted(` / `cubeApi.` → **停止询问**（与快照矛盾）
- 命令：不跑测；贴 15 行表
- 结果：15 文件全部 forbidden=False（App.vue 无 script）；15 行 audit ok 已写入 status.md。

---

## F. 收口

- [x] ### T44 清空 allowlist

- 只改 `sfcThin.spec.ts`：`ALLOWLIST` 必须为 `[]`
- 若仍有路径 → 回到对应未勾选 T-b，禁止在本 T 把未构薄文件留在空名单外硬过
- 命令：`pnpm --filter @cube/arco-vue exec vitest run --config vitest.config.ts src/core/utils/sfcThin.spec.ts`
- 结果：`ALLOWLIST = []`（T42b 收口时收敛）；sfcThin 48/48 通过（全量扫描 48 个 vue 无禁止 token）。

- [x] ### T45 `web/README.md`

- 只改 `NewLife.Cube.ArcoVue/web/README.md`
- 追加「SFC 职责分离」短节：composable 模式、禁止 vue 内业务 watch/onMounted/cubeApi、指向 OSC-260813c3e9
- 不删既有 OSC-00xx 能力条目
- 命令：无 Vitest
- 结果：已追加 5 条要点（含门禁 spec 与 47 文件收口说明），既有条目未删。

- [x] ### T46 全量测试 + 构建

- 不改代码
- 命令 1：`pnpm --filter @cube/arco-vue test`（全绿）
- 命令 2：`pnpm --filter @cube/arco-vue build`（无 error）
- 在 `status.md` note 记录两条命令的通过摘要
- 结果：vitest 355/355 全绿（29 文件，抽离前 307 + sfcThin 48）；vue-tsc -b exit 0；vite build exit 0（仅 chunk>500kB 既有警告，非 error）。
