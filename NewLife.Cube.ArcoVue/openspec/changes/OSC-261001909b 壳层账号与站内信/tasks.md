# OSC-261001909b Tasks — 壳层账号与站内信

## T1 菜单搜索

- [ ] T1-1 `menuSearch.ts` + spec（隐藏项、叶子、空关键字、截断 8 条）。
- [ ] T1-2 `MenuSearch.vue` / `useMenuSearch.ts` 接入 `side.vue`、`top.vue`、`mix.vue` 菜单上方。

## T2 激活与注销

- [ ] T2-1 公开路由 `/activate`，`activate.vue` + `useActivatePage.ts`（design §3）。
- [ ] T2-2 安全页签注销：`useCloseAccount.ts`，用户名一致才 POST，成功清会话。

## T3 站内信跳转

- [ ] T3-1 `inboxTarget.ts` + spec。
- [ ] T3-2 Inbox JSON 增加 `target`；`InboxMessageItem.target`。
- [ ] T3-3 `WorkflowEngine.Notify` 写 Target；抽出 `BuildNotifyTarget` 并单测。
- [ ] T3-4 抽屉点击：先标已读，可解析则 push。
- [ ] T3-5 `useDefaultList` 对查询 `id` 自动打开详情一次。

## T4 租户成员

- [ ] T4-1 `TenantController` 合成列「成员」，Url 含 `tenantId={Id}`，无 DataAction。

## T5 验证与文档

- [ ] T5-1 web vitest：`menuSearch.spec.ts`、`inboxTarget.spec.ts`。
- [ ] T5-2 `dotnet test` 过滤 `Osc261001909b`；build net10.0；`vue-tsc -b`。
- [ ] T5-3 迁移方案与核心接口架构补 Inbox.target、激活页、注销。
