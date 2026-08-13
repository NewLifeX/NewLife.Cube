# OSC-260813c3e9 Verify

验收时填写命令输出摘要。触及前端代码：新增单测全过 + 构建无错误。

## AC

- [x] **AC-01 新号格式**：`openspec-create.agent.md` 规定 `OSC-YYMMDDxxxx`；现行规则为禁止 `changes/` 最大号 +1，无「未给编号则取最大号 +1」作为现行步骤。
- [x] **AC-02 历史豁免**：`openspec/changes/OSC-0018 实体界面自定义设计方案/` 仍在且 `id: OSC-0018`；approve 仍接受 `OSC-00xx`。
- [x] **AC-03 无厂商模型绑定**：五壳 Agent、`openspec/README.md`、迁移方案 §9.2/§10.1 无 DeepSeek / Flash 一次 1 个 T / Pro 同组连做。
- [x] **AC-04 目录**：`openspec/changes/OSC-260813c3e9 页面TS抽离与协作编号/`，ID `OSC-260813c3e9`，非 `OSC-0020`。
- [x] **AC-05 47 文件**：§4.1 32 个已抽离（对应 `useXxx.ts` / DefaultList 六文件）；§4.2 15 个 audit ok，无空 composable（无 `useRootLayout`/`useShellToolbar` 等）。
- [x] **AC-06 sfcThin**：`ALLOWLIST === []`；48/48 通过。
- [x] **AC-07 回归**：`pnpm --filter @cube/arco-vue test` → 29 files / **355/355**（既有 spec 未删；抽离前约 307 + sfcThin 48）。
- [x] **AC-08 构建**：`pnpm --filter @cube/arco-vue build` → `vue-tsc -b` exit 0；vite build exit 0（仅 chunk>500kB 既有警告）。
- [x] **AC-09 冻结**：`RecordDrawer.vue` 模板 `placement="right"`；`useDynamicPage.ts` 不读 layout/theme store；未改 ViewProfile JSON 字段名。
- [x] **AC-10 未做 OSC-0018**：无实体自定义技能实现；OSC-0018 仍为 Draft 设计号。
- [x] **AC-11 冒烟**：Vite `http://localhost:5183/` 与 `/login` HTTP 200（SPA `#app`）；CubeDemo `http://localhost:5000/` HTTP 200。未做已登录态下 `Admin/User` 抽屉点击（无浏览器会话），不阻塞 AC-06~08。
- [x] **AC-12 非法输入**：create 写明用户给 `OSC-0020` / `OSC-00xx` 时拒绝沿用顺序号并改随机号。

## 命令与结果

```
pnpm --filter @cube/arco-vue exec vitest run --config vitest.config.ts src/core/utils/sfcThin.spec.ts
→ Test Files 1 passed; Tests 48 passed (48)

pnpm --filter @cube/arco-vue test
→ Test Files 29 passed (29); Tests 355 passed (355)

pnpm --filter @cube/arco-vue build
→ vue-tsc -b && vite build; built in ~13s; exit 0
```

## 三步编排摘要

- implementation-audit：对照 proposal/design，A 编号+规范、B sfcThin、C DefaultList 六文件、D 31 组 composable、E 15 审计、F 收口均已落地。`useListCrud`/`useListViews` 经 deps 传入 `loadData`，交叉调用未漏。15 个审计文件无对应空 composable。缺口：无（功能范围无用户可见行为变更）。
- code-review：0🔴。🟡 DefaultList.vue 构薄 script 因模板解包仍远超「约 20 行」建议值，机械门禁只扫禁止 token，可接受。🟡 `measureTableHeight` 在 `listContext` 与 `useListViews`/`useDefaultList` 均有调用点，属搬移后共用而非双份状态。未发明 design 外文件。
- doc-sync：openspec README / 五壳 / 迁移方案 §9.2§10.1 / web README「SFC 职责分离」已同步。验收期将 SFC 节从 OSC-0008/0009 能力列表中间移到「目录结构」之前，恢复抽屉能力条目连续。不改 `Doc/功能清单.md`（无用户可见功能）。

## 会话补录

- 验收 doc-sync：挪动 `web/README.md` SFC 小节位置（并入 T45，不新建任务项）。

## 风险

- DefaultList 交叉 `loadData` 已按 design §5.3 经 deps 注入，抽查 `useListCrud` 保存/删除后调用 `loadData()`。
- sfcThin 未扫 `ref(`；审计文件可残留占位 ref（如 `home/index.vue`），不视为失败。
- 已登录列表/抽屉交互未在本验收做浏览器点击。
