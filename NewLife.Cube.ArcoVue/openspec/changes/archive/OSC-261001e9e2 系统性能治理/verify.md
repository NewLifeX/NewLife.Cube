# Verify

## 命令

```powershell
dotnet test NewLife.Cube.Tests --filter "FullyQualifiedName~ExportCapTests|FullyQualifiedName~LovLabelQueryTests"
dotnet build NewLife.Cube\NewLife.Cube.csproj --no-restore
```

web 目录：

```powershell
pnpm exec vitest run src/core/utils/patchReload.spec.ts src/core/utils/listRowPatch.spec.ts src/views/crud/lovHydrate.spec.ts
pnpm exec vue-tsc --noEmit
```

## 执行记录（2026-10-01 验收）

- Vitest：`patchReload.spec.ts`、`listRowPatch.spec.ts`、`lovHydrate.spec.ts` 共 17 通过。
- `dotnet test` 过滤 `ExportCapTests|LovLabelQueryTests|LovControllerGuardTests`：16 通过，0 失败。
- `dotnet build NewLife.Cube`：0 错误（既有 XML 注释警告，无本号新增）。
- `pnpm exec vue-tsc --noEmit`：0 错误。
- `useListQuery.ts` 与 `features/search/InsightPanel.vue` 无本号差异。页面正文无「当前页」。
- 浏览器：已登录的部门卡片视图，排序为 `Name desc`。只改「备注」后保存，仍发出 `GET /api/Admin/Department?pageIndex=0&pageSize=1000&sort=Name&desc=true`。备注已改回空。未做删除冒烟，避免改演示数据。

路径以仓库内现有测试项目与 `web` 的 `package.json` 为准。新增文件不在上述 filter 中时，把对应测试类名补进命令，不改过滤以外的全库。

## AC

- [x] **AC1 规范** `web/docs/系统性能治理.md` 含请求预算、读路径三分、写路径分流、缓存与失效、三条准入。`web/README.md` 有指向它的链接。
- [ ] **AC2 编辑不刷新** 纯函数在所改字段不命中时为 false，单测通过。`useListCrud.finishEdit` 把整份提交键交给 `shouldReloadAfterWrite`。部门列表按名称排序时，只改备注仍会 `loadData`。见下方 P1。
- [x] **AC3 编辑要刷新** 所改字段等于某条排序、某条筛选，或等于看板分组 / 日历开始 / 甘特计划起止时，`shouldReloadAfterWrite` 为 true，`finishEdit` 调用 `loadData`。树视图编辑成功一律 `loadData`。
- [x] **AC4 新增与批量** 新增保存、批量删除、批量启用/禁用、导入成功仍调用 `loadData`。
- [x] **AC5 删除** 非树、删后当前页还有行：`removeListRow`，`total > 0` 时减 1，不调用 `loadData`。删后当前页为空且 `pageIndex > 0`：页码减 1 并 `loadData`。树删除一律 `loadData`。失败发生在改行之前。浏览器未点删除。
- [x] **AC6 布尔与看板** 原 `shouldReloadAfterPatch` 用例通过。看板跨列与布尔徽标仍走单字段判断。
- [x] **AC7 抽屉标签** 同一 `lovCode` 合并；已有值与枚举、级联不进组。`Promise.allSettled` 隔离失败。地区仍先走 `areaNames`。
- [x] **AC8 List 按键** `PlanListLabelQuery`：可搜索为 `keyed` / 1 页；否则 `scan` / 10 页；空集合不请求。控制器按计划翻页，循环上限是计划的 `MaxPages`。`LovControllerGuardTests` 通过。
- [x] **AC9 图表** `InsightPanel.vue` 与 `loadChart` 无本号改动。部门页正文没有「当前页」。
- [x] **AC10 导出** `ExportCapTests` 覆盖 0、更小正数、超过 100 万。两处 `MaxExport` 默认 1000000。Cube.Vue 初始 `maxExport` 为 1000000。`MaxBackup` 仍为 10000000。
- [x] **AC11 暂缓仍在** `GetChartData` 的 `PageSize = 1000`。`DeleteSelect` 仍逐键 `FindData`。`useCascaderField` 仍 `getDetail`。`main.ts` 仍 `app.use(ArcoVue)`。`EnableTotalCount` 仍传入 `RetrieveTotalCount`。
- [x] **AC12 权限与空** 无更新权时 `onTableAction` 在打开编辑前返回。删除在 `remove` 成功后才改行。`pendingCount <= 0` 不请求。`ResolveExportCap(0, 0)` 返回 1000000。
- [x] **AC13 构建** 本号新增单测通过。`dotnet build NewLife.Cube` 无错误。`vue-tsc --noEmit` 无错误。

## 三步摘要

- 实现审计：目标 1、2、3 与删除路径对齐 design。目标 4 的编辑路径在常见排序下仍整表刷新，见 P1。
- 代码审查：无命名、兼容性或安全方面的必须修复项。`finishEdit` 用整份提交键做刷新判断，是 P1 的代码位置。
- 文档同步：`proposal.md` 成功标准里残留的「翻页不超过 3」和已撤销的图表纯函数测试说明，已改成与修订稿一致的 10 页、且图表不单测。持久规范 `web/docs/系统性能治理.md` 与实现的四条边界一致。

## 缺口（仅记录）

用户在看到下列 P1 后指示按规范复盘，不补齐。

- P1：`useListCrud.ts` 的 `finishEdit` 把 `Object.keys(patch)` 整份提交键交给 `shouldReloadAfterWrite`。表单里名称非空就会带上 `Name`。部门卡片当前排序是 `Name desc`，只把备注改成 `e9e2-smoke` 后，保存仍请求 `GET /api/Admin/Department?pageIndex=0&pageSize=1000&sort=Name&desc=true`。目标 4 写的是「所改字段」。备注已改回空。后续写回变更应只拿本次改过的字段做判断。

## 暂缓区必须保留

| 符号 | 保留行为 |
| --- | --- |
| `GetChartData` | `PageSize = 1000`，`RetrieveTotalCount = false` |
| `AppendEntityLabels` | 按主键 `Unique.In`，不可见键省略 |
| `DeleteSelect` / `BatchUpdateFields` | 逐键 `FindData` |
| `useCascaderField` | 叶子 `getDetail` |
| `getPageCached` | TTL 30 秒 |
| `Index` | 整实体列表，`EnableTotalCount` 默认 true |
