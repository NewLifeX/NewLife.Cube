# OSC-261001909b Verify — 壳层账号与站内信

## AC-1 激活

- [ ] AC1.1 `/activate?token=a&account=b@c.com` 发起 GET Activate。成功后到 `/login`。
- [ ] AC1.2 无 token 时显示渠道、账号、验证码。账号或验证码为空不能提交。
- [ ] AC1.3 未登录可打开该页。

## AC-2 注销

- [ ] AC2.1 安全页签有「注销账号」。输入与 `userInfo.name` 不一致时不能确定。
- [ ] AC2.2 成功后本地无 token，位于 `/login`。失败时仍停留在账号中心且 token 还在。
- [ ] AC2.3 `userInfo.name` 为空时按钮禁用。

## AC-3 站内信

- [ ] AC3.1 Inbox 项含 `target`。
- [ ] AC3.2 `Admin/User#12` 打开 `/Admin/User?id=12` 并弹出该记录详情。
- [ ] AC3.3 `Admin/User#0`、三段路径、带 `..` 的字符串只标已读，不跳转。
- [ ] AC3.4 流程通知在主体有 TypePath 与 EntityKey 时 Target 为二者用 `#` 连接；缺一则 Target 为空。
- [ ] AC3.5 抽屉为双层时间轴：外层分组（今天/本周/本月/更长时间，从近到远、空桶不渲染、节点带图标）；组内每条消息为时间轴节点；条目内容无竖线。

## AC-4 租户成员

- [ ] AC4.1 租户列表有「成员」，地址为 `/Admin/TenantUser?tenantId=` 加该行 Id，点击是导航不是 POST。
- [ ] AC4.2 依赖 OSC-2610011ff2：`/Admin/TenantUser` 已注册时页面是通用列表。本号单独验收时若路由 404，记为阻塞于 1ff2，不改成员 Url。

## 命令

```powershell
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/core/utils/inboxTarget.spec.ts src/core/utils/closeAccount.spec.ts
pnpm exec vue-tsc -b
dotnet test "NewLife.Cube.Tests/NewLife.Cube.Tests.csproj" --filter Osc261001909b
dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0
```

预期：spec 与过滤测试通过；vue-tsc 与 build 无错误。

## 执行阶段记录（openspec-apply，2026-10-01）

- Vitest：`inboxTarget.spec.ts` 3、`closeAccount.spec.ts` 1，全过。
- XUnit：`Osc261001909b` 通过 2。
- `vue-tsc -b` 0 错误；`dotnet build NewLife.Cube -f net10.0` 0 错误。
- 代码审查：首轮 1 🔴（注销未清 Pinia）已修为 `userStore.logout()`；无剩余 🔴。
- 实现审计：对照 proposal/design/tasks 无实现缺口（菜单搜索按范围不做）。
