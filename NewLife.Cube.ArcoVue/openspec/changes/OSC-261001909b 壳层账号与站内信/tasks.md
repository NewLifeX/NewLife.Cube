# OSC-261001909b Tasks — 壳层账号与站内信

## T1 激活与注销

- [x] T1-1 公开路由 `/activate`，`activate.vue` + `useActivatePage.ts`（design §2）。
- [x] T1-2 安全页签注销：`useCloseAccount.ts`，用户名一致才 POST，成功清会话。

## T2 站内信跳转

- [x] T2-1 `inboxTarget.ts` + spec。
- [x] T2-2 Inbox JSON 增加 `target`；`InboxMessageItem.target`。
- [x] T2-3 `WorkflowEngine.Notify` 写 Target；抽出 `BuildNotifyTarget` 并单测。
- [x] T2-4 抽屉点击：先标已读，可解析则 push。
- [x] T2-5 `useDefaultList` 对查询 `id` 自动打开详情一次。
- [x] T2-6 抽屉改双层时间轴：日期分桶（今天/本周/本月/更长时间，从近到远、空桶不渲染）+ 分组图标（`inboxBucket.ts`+spec、`INBOX_BUCKET_ICONS`）；通知条目内竖线移除。

## T3 租户成员

- [x] T3-1 `TenantController` 合成列「成员」，Url 含 `tenantId={Id}`，无 DataAction。

## T4 验证与文档

- [x] T4-1 web vitest：`inboxTarget.spec.ts`、`closeAccount.spec.ts`。
- [x] T4-2 `dotnet test` 过滤 `Osc261001909b`；build net10.0；`vue-tsc -b`。
- [x] T4-3 迁移方案与核心接口架构补 Inbox.target、激活页、注销。
