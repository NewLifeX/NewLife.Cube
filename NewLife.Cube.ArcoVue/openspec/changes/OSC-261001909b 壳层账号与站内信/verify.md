# OSC-261001909b Verify — 壳层账号与站内信

## AC-1 菜单搜索

- [ ] AC1.1 关键字匹配可见叶子的 displayName，最多 8 条。
- [ ] AC1.2 `visible===false` 的项不出现。
- [ ] AC1.3 空关键字不弹出结果。回车进入第一条的 url。
- [ ] AC1.4 side、top、mix 三处都有搜索框。

## AC-2 激活

- [ ] AC2.1 `/activate?token=a&account=b@c.com` 发起 GET Activate。成功后到 `/login`。
- [ ] AC2.2 无 token 时显示渠道、账号、验证码。账号或验证码为空不能提交。
- [ ] AC2.3 未登录可打开该页。

## AC-3 注销

- [ ] AC3.1 安全页签有「注销账号」。输入与 `userInfo.name` 不一致时不能确定。
- [ ] AC3.2 成功后本地无 token，位于 `/login`。失败时仍停留在账号中心且 token 还在。
- [ ] AC3.3 `userInfo.name` 为空时按钮禁用。

## AC-4 站内信

- [ ] AC4.1 Inbox 项含 `target`。
- [ ] AC4.2 `Admin/User#12` 打开 `/Admin/User?id=12` 并弹出该记录详情。
- [ ] AC4.3 `Admin/User#0`、三段路径、带 `..` 的字符串只标已读，不跳转。
- [ ] AC4.4 流程通知在主体有 TypePath 与 EntityKey 时 Target 为二者用 `#` 连接；缺一则 Target 为空。

## AC-5 租户成员

- [ ] AC5.1 租户列表有「成员」，地址为 `/Admin/TenantUser?tenantId=` 加该行 Id，点击是导航不是 POST。
- [ ] AC5.2 依赖 OSC-2610011ff2：`/Admin/TenantUser` 已注册时页面是通用列表。本号单独验收时若路由 404，记为阻塞于 1ff2，不改成员 Url。

## 命令

```powershell
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/core/utils/menuSearch.spec.ts src/core/utils/inboxTarget.spec.ts
pnpm exec vue-tsc -b
dotnet test "NewLife.Cube.Tests/NewLife.Cube.Tests.csproj" --filter Osc261001909b
dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0
```

预期：两个 spec 与过滤测试通过；vue-tsc 与 build 无错误。
