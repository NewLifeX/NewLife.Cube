# OSC-260813397e Tasks

> 进入 Implementing 后勾选。顺序：T1 登录契约/页 → T2 token/OAuth hash → T3 MFA+绑定 → T4 租户 API/齐平 → T5 壳与文档 → T6 测试构建。

## T1 登录配置与飞书风页面

- [x] 1.1 `loginConfig.ts`：`resolveLoginTabs`、`buildSsoLoginUrl`（必须含 `source=front-end`）。`loginConfig.spec.ts` ≥4 例。
  - [x] 1.1a（会话）方案 A：`oauth[]` **不**绑 `EnableOAuthServer`；`Doc/OAUTH` 与 design §2.2 对齐。
  - [x] 1.1b（会话）`sendCodeCaptcha`（CaptchaScene 位 4）；`passwordStrength` 客户端校验；`startPage` + 过滤 `/Admin*`。
- [x] 1.2 `useLoginPage.ts` 只读 `login/oauth/security/register`，删除对 `providers/allowLogin/enableSms` 的依赖。
- [x] 1.3 `index.vue` 左右栏布局（design §2.1）；验证码图；`challengeRequired` 时加密。SFC 薄脚本。
- [x] 1.4 登录成功写入 `accessToken`+`refreshToken`；捕获 `mfa_required` 切 `mfa` 态，调 `mfaVerify`。
- [x] 1.5 `register.vue` / `forgot-password.vue` 对齐视觉与 `register.*` / captcha。

## T2 回跳 Token 与刷新

- [x] 2.1 `router/index.ts`：解析 `#token=`（及 refresh）后清 hash。
- [x] 2.2 axios/api 封装：401 用 refreshToken + userName 调 `/Auth/Refresh` 一次；失败清会话去登录。
- [x] 2.3 OAuth 按钮走 `buildSsoLoginUrl`；`r` 回 `/login`。

## T3 MFA 设置与第三方绑定

- [x] 3.1 后端：若普通用户无法 List UserConnect，新增 `GET /Auth/Binds`（当前用户 + 可见 OAuth）。
- [x] 3.2 api-core：`listBinds`、已有 mfa* 保持；单测 URL。
- [x] 3.3 `SecuritySettings.vue` + `useSecuritySettings.ts`：MFA Setup/Activate/Disable；绑定跳转 `/Sso/Bind/{name}`；解绑 `/Sso/UnBind/{name}` 确认。
- [x] 3.4 用户菜单入口「账号安全」。

## T4 租户 WebAPI 与切换

- [x] 4.1 `GET/POST /Auth/Tenants`、`/Auth/SwitchTenant`（design §4.2）。
- [x] 4.2 `TenantController` OnInsert/OnUpdate 保证 Manager 的 TenantUser。
- [x] 4.3 `TenantUserController` 强制当前租户 Search/盖章。
- [x] 4.4 `UserController.Search` → `SearchWithTenant`；OnInsert `EnsureTenantUser`。
- [x] 4.5 Department 盖章 + 相关 Menu `Admin|Tenant`。
- [x] 4.6 `OnGetFields` 租户模式隐藏 `TenantId`（ITenantScope 实体）。
- [x] 4.7 api-core `listTenants`/`switchTenant` + 单测。

## T5 壳、请求头、新实体文档

- [x] 5.1 `stores/tenant.ts` + 请求拦截注入 `X-Tenant`。
- [x] 5.2 用户菜单租户分组切换（替代初版顶栏下拉）；切换后刷新菜单与页。
- [x] 5.3 ~~登录页可选租户 Code~~ → **终验改为不展示**（登录后选租户，见 T8.1）。
- [x] 5.4 撰写 `web/docs/多租户新实体接入.md`（6 条清单 + CubeDemo 引用）。
- [x] 5.5 `web/README.md` 登记登录/租户/账号安全。

## T6 验证

- [x] 6.1 本号新增 Vitest 全过；`pnpm --filter @cube/arco-vue test` 与 `build` 无错误。
- [x] 6.2 `dotnet build NewLife.Cube` 无错误。
- [x] 6.3 冒烟证据：loginConfig/tenantHeader/mfaQr/api-core URL 单测 + `Osc260813397eTenantTests`（TenantUser / UserTenantSearch）；全浏览器手工未在本环境跑，残余见 verify。

## T7 验收缺口补齐（2026-08-16 openspec-verify）

- [x] 7.1 **P0** `onUnauthorized` 调用 `clearTenantSession`；`tenantHeader.spec` 覆盖。
- [x] 7.2 **P1** 移除注册页 `oauthToken` / `getOAuthPendingInfo` / `oauthMode`。
- [x] 7.3 **P1** `TenantManagerHelper` + `UserTenantSearch`；`Osc260813397eTenantTests` 2 例通过。
- [x] 7.4 **P2** MFA Setup `qrcode` 本地 DataURL 二维码 + 密钥复制；design Bind 统一为 OAuth **name**。
- [x] 7.5 **P1** T6.3 以自动化证据勾选；手工浏览器项记残余风险。

## T8 终验前会话打磨（2026-08-17）

- [x] 8.1 去掉登录页「租户 Code」；租户切换迁入用户弹出菜单分组（`a-dgroup`，账号安全之上）并配图标。
- [x] 8.2 `GetTenantId`：无租户 Cookie 时勿对 null `.ToString()`（修复 EnableTenant 下 `/Auth/LoginConfig` 500）。
- [x] 8.3 DrawingCaptcha `data:image…` / 裸 base64 → `normalizeCaptchaImageHtml` 再 `v-html`。
- [x] 8.4 登录验证码输入行与密码框同宽（`captcha-group`）。
