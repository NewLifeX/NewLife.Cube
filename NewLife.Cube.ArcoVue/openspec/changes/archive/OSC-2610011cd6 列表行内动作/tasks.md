# OSC-2610011cd6 Tasks — 列表行内动作

## T1 请求语义

- [x] T1-1 新建 `opsRequest.ts` 与 spec：方法选择、`/api/` 识别、确认文案。
- [x] T1-2 `useListOpsLinks.requestDataAction` 接受方法。dataAction 走 POST；单元格 `/api/` 走 GET。
- [x] T1-3 `useDefaultList` 在这两类点击前 `Modal.confirm`，取消则不请求。

## T2 用户抽屉

- [x] T2-1 `useRecordDrawer` 计算 `showUserSecurityActions`，实现两个 POST 与确认文案。
- [x] T2-2 `RecordDrawer.vue` 底部按该标志渲染按钮。新增模式不渲染。

## T3 强制下线列

- [x] T3-1 `UserOnlineController` 静态构造增加合成列（design §4）。
- [x] T3-2 `NewLife.Cube.Tests/Osc2610011cd6OpsTests.cs`：反射 ListFields 与 `Kick` 的 HttpPost。
- [x] T3-3 `visibleOpsLinks`：无 Delete 权限时从操作列去掉 `/UserOnline/Kick`，其它 dataAction 保留。

## T4 验证与文档

- [x] T4-1 web：`pnpm exec vitest run src/core/utils/opsRequest.spec.ts`（4/4）与 `listLinkFields.spec.ts`（8/8）。
- [x] T4-2 `dotnet test NewLife.Cube.Tests --filter Osc2610011cd6`：通过 2，失败 0。
- [x] T4-3 `pnpm exec vue-tsc -b` 与 `dotnet build NewLife.Cube -f net10.0` 无错误。
- [x] T4-4 迁移方案补 dataAction POST 与 `/api/` 单元格一句。

## 会话小任务

- [x] T5 `useFieldInput` 上传响应断言改为经 `unknown`（vue-tsc TS2352）。不改变上传行为。
