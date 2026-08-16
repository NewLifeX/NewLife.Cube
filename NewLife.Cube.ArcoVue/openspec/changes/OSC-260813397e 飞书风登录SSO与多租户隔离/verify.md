# OSC-260813397e Verify

> 进入 Validating 后勾选。

## 执行阶段记录（openspec-apply）

- 2026-08-13：批准后 Implementing；完成飞书风登录、hash token、401 Refresh、账号安全、Auth Tenants/Binds、租户齐平与文档。
- Vitest：`@cube/api-core` 18+49 通过；`@cube/arco-vue` 365 通过；`pnpm --filter @cube/arco-vue build` 通过。
- `dotnet build NewLife.Cube` 0 错误。
- T6.3 手工冒烟与 AC 勾选留给 openspec-verify。

## 验收阶段记录（openspec-verify）

- （待填）

## 验收标准

### 登录

- [ ] **AC-01 布局**：≥992px 左品牌右表单；品牌来自 LoginConfig（logo/背景/版权），无硬编码四张统计卡。
- [ ] **AC-02 通道**：`login.password/sms/mail` 为 false 的 Tab 不出现；全 false 且无 oauth 显示 empty。
- [ ] **AC-03 配置字段**：页面不依赖 `providers/allowLogin/enableSms`；SSO 来自 `oauth[]`。
- [ ] **AC-04 Captcha**：`login.captcha` 为 true 时展示 SVG 且登录/发码携带 captcha 字段。
- [ ] **AC-05 Challenge**：`challengeRequired` 为 true 时先 Challenge 再登录，失败不回退明文。
- [ ] **AC-06 MFA 登录**：`mfa_required:{token}` 进入二步；错误码不进二步。
- [ ] **AC-07 OAuth**：跳转 URL 含 `source=front-end`；回站 `#token=` 后已登录且 hash 清除。
- [ ] **AC-08 注册/忘记密码**：视觉与登录右栏一致；`register.enabled=false` 无注册入口。

### 账号安全

- [ ] **AC-09 MFA 设置**：available 时能 Setup→Activate 看到 backupCodes；Disable 需验证码。
- [ ] **AC-10 绑定**：未绑可跳转 Bind；已绑可 UnBind，列表不再显示已启用绑定。
- [ ] **AC-11 权限**：无 Admin 菜单的用户仍能列出自己的绑定（/Auth/Binds 或等价）。

### 多租户

- [ ] **AC-12 切换**：`/Auth/Tenants` 列出可访问租户；Switch 后请求带 `X-Tenant`（平台项不带头或 TenantId=0）。
- [ ] **AC-13 创建租户**：POST Tenant 后存在该管理员 TenantUser。
- [ ] **AC-14 隔离**：租户用户看 `ITenantScope` 列表只有本租户行；平台管理员可见全部。
- [ ] **AC-15 用户列表**：租户模式下 User 搜索不出现外租户用户（SearchWithTenant）。
- [ ] **AC-16 TenantId 字段**：租户用户添加/编辑表单无 TenantId；插入行 TenantId=当前。
- [ ] **AC-17 文档**：`web/docs/多租户新实体接入.md` 含 6 条清单。

### 边界与暂缓

- [ ] **AC-18 无 OAuthPending**：本号未实现首次 SSO 强制 SPA 注册。
- [ ] **AC-19 无分库**：未使用 DatabaseName 切连接。
- [ ] **AC-20 保留**：`/Auth/Login`、`DataScopeMiddleware`、`ITenantScope` CreateWhere 算法未删。

### 门禁

- [ ] **AC-21** 本号新增单测全过。
- [ ] **AC-22** `pnpm --filter @cube/arco-vue test` 与 `build` 无错误。
- [ ] **AC-23** `dotnet build NewLife.Cube` 无错误。

## 自动化门禁

```powershell
cd "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue\web"
pnpm exec vitest run src/views/login/loginConfig.spec.ts
pnpm test
pnpm build
dotnet build "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube\NewLife.Cube.csproj"
```

## 预期

- Vitest / build / dotnet 退出码 0。
- 手工：密码登录成功；配置了 OAuth 的环境回跳有 token；EnableTenant 下两租户数据隔离。
