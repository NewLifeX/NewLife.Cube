# OSC-260813397e — 飞书风登录 SSO 与多租户隔离

## 1. 为何做

Cube WebAPI 已具备密码 / 短信 / 邮箱登录、图片验证码、RSA Challenge、TOTP MFA、OAuth 跳转与 `UserConnect` 绑定，以及共享库 + `TenantId` 行隔离。ArcoVue 登录仍读 v1 `LoginConfig` 字段，OAuth 缺少 `source=front-end` 与 `#token=`，无 MFA 步进与账号绑定；请求不带 `X-Tenant`；WebAPI 租户管理薄于 MVC（创建租户不建管理员关系、用户列表不按租户过滤）。业务实体若只加 `TenantId` 列而不实现 `ITenantScope`，列表会串租户。

对标飞书 / 字节：左品牌右表单、能力开关驱动的登录方式、底部 SSO 图标、登录后租户切换、账号安全里绑定第三方。本号在 **不新造登录协议** 的前提下把 SPA 与 WebAPI 接到同一套契约上，并补齐租户管理与新实体隔离规范。

## 2. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **一个实现 OSC**：登录/SSO/MFA + 第三方绑定 + 租户切换/管理齐平 + 新实体隔离（文档 + 基类藏 TenantId）。 |
| 2 | **飞书风登录页**：左品牌（logo/背景/版权）+ 右表单；可选租户 Code；Tab 由 `login.password/sms/mail` 决定；底部 `oauth[]` 图标（**不**绑 `EnableOAuthServer`；该开关仅表示 Cube 作 OAuth 服务端）。 |
| 3 | SSO 首次登录保持后端 **AutoRegister / 已有匹配绑定**；**不**移植 CubeNC `OAuthPending` → SPA 注册（ForceBind* 另案）。 |
| 4 | MFA **仅 TOTP**（`/Mfa/*`）。短信/邮箱只作登录验证码，不作第二因子。 |
| 5 | 多租户维持 **共享库 + TenantId**。不实现 `DatabaseName` 分库、域名路由、配额强制。 |
| 6 | JWT **不**携带 TenantId；每请求 `X-Tenant`（Code）优先，兼容 `X-Tenant-Id` / cookie。 |
| 7 | Challenge：仅当 `security.challengeRequired===true` 加密密码；否则明文（兼容 `AllowPlainPassword`）。 |
| 8 | 不改 Cube.Vue 登录页；不实现微信小程序/APP 登录 UI（API 已有，本号不做页）。 |

## 3. 做什么

1. 重做 ArcoVue 登录/注册/忘记密码为飞书风，消费嵌套 `LoginConfig`；验证码、Challenge、MFA 二步、OAuth `source=front-end` + hash token。
2. 持久化 refreshToken；401 尝试 `/Auth/Refresh`。
3. 个人设置：MFA 开通/关闭；第三方绑定/解绑（`/Sso/Bind`、`/Sso/UnBind` + 当前用户 UserConnect 列表）。
4. WebAPI 租户齐平 MVC：创建租户写 TenantUser、TenantUser 默认当前租户、用户列表 `SearchWithTenant`、部门盖章、`Admin|Tenant` 菜单可见性。
5. `GET/POST /Auth/Tenants` 与 `SwitchTenant`；ArcoVue 顶栏租户切换并注入 `X-Tenant`。
6. 基类：租户模式下表单隐藏 `TenantId`；文档规定新实体必须 `ITenantScope` + EntityController + 菜单 Mode。
7. 单测 + 登录/租户/绑定冒烟（本号以 Vitest 为主；关键路径可复用 Playwright 若环境可用）。

## 4. 不做什么

- 不补 WebAPI OAuthPending / 首次 SSO 强制跳转注册页。
- 不分库分表、不按 Domain 解析租户、不强制 Expired/MaxUsers。
- 不做 SMS/Email 作为 MFA。
- 不把飞书扫码登录做成独立协议（无对应后端）。
- 不重写 IdP 模式（`/Sso/Authorize` 等给其它应用用）。

## 5. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0004 | 壳 / UserProfile / token |
| OSC-0003 | 动态实体页（租户实体走 DefaultList） |
| `@cube/api-core` / `@cube/auth-logic` | LoginConfig v2、MFA、Challenge 已封装，登录页应对齐而非另造协议 |
| `DataScopeMiddleware` / `ITenantScope` | 隔离事实源 |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| Vitest | 是 | LoginConfig 能力映射、mfa_required 解析、OAuth URL、page/tenant header、表单藏 TenantId |
| api-core 单测 | 是 | Tenants/SwitchTenant/Bind URL |
| XUnit / build | 是 | Tenant OnInsert 建 TenantUser；User Search 带租户；dotnet build |
| 手工/E2E | 是 | 登录三通道 + MFA 步进（EnableMfa 时）+ OAuth 回跳；租户切换后列表不串数 |

## 7. 成功标准

- [ ] 登录页按 `LoginConfig` 显示/隐藏密码、短信、邮箱、验证码、注册入口与 SSO 图标；布局为左品牌右表单。
- [ ] OAuth 回跳 SPA 能写入 JWT（`source=front-end` + `#token=`）。
- [ ] `mfa_required:{token}` 进入二步验证而非直接失败；设置页可开通/关闭 TOTP。
- [ ] 已登录用户可绑定/解绑第三方；解绑后列表 Enable=false。
- [ ] 开启 EnableTenant 后：创建租户产生管理员 TenantUser；切换租户后 `X-Tenant` 生效；`ITenantScope` 实体列表只见本租户（平台管理员 TenantId=0 可见全部）。
- [ ] 新实体文档 + 基类隐藏 TenantId；本号新增单测全过，构建无错误。
