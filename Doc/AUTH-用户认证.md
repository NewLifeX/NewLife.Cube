# 用户登录验证（登录 / 注册 / 忘记密码）

> 本文档是魔方用户验证业务的**权威总览**，同时覆盖 **MVC（服务端渲染）** 与 **API（前后端分离）** 两种接入方式。
> 登录、注册、忘记密码三大业务，及其下所有登录/注册/找回方式，均在此完整定义。

---

## 1. 业务全景

```
用户验证业务
├── 登录
│   ├── 密码登录（RSA 加密传输 + 防爆破 + 外部验证）
│   ├── 手机验证码登录（支持自动注册）
│   ├── 邮箱验证码登录（支持自动注册）
│   ├── 三方登录（OAuth2.0：微信/QQ/钉钉/GitHub 等，全页跳转）
│   └── 微信登录（小程序 / App，API 专用）
├── 注册
│   ├── 用户名注册（用户名 + 密码）
│   ├── 手机注册（手机号 + 短信验证码 + 密码）
│   ├── 邮箱注册（邮箱 + 邮件验证码 + 密码）
│   └── 三方回跳注册/绑定（OAuth 登录后补全资料）
└── 忘记密码（重置密码）
    ├── 手机取回（手机号 + 短信验证码）
    ├── 邮箱取回（邮箱 + 邮件验证码）
    └── 验证问题取回（密保 Q&A）※ 未实现，见 §4.2
```

### 1.1 功能矩阵（实现状态）

| 业务 | 方式 | MVC | API | 后端核心 | 状态 |
|------|------|-----|-----|---------|------|
| 登录 | 密码登录 | ✅ | ✅ | `UserService.LoginByPassword` | 完整 |
| 登录 | 手机验证码 | ✅ | ✅ | `UserService.LoginBySms` | 完整 |
| 登录 | 邮箱验证码 | ✅ | ✅ | `UserService.LoginByMail` | 完整 |
| 登录 | 三方登录 | ✅ | ✅（全页跳转） | `SsoController` | 完整 |
| 登录 | 微信登录 | — | ✅ | `SsoController.WxMiniLogin/WxAppLogin` | 完整 |
| 注册 | 用户名注册 | ✅ | ✅ | `UserService.RegisterByPassword` | 完整 |
| 注册 | 手机注册 | ✅ | ✅ | `UserService.RegisterByPhoneCode` | 完整 |
| 注册 | 邮箱注册 | ✅ | ✅ | `UserService.RegisterByMailCode` | 完整 |
| 注册 | 三方回跳 | — | ✅ | `UserService.RegisterByOAuthBind` | 完整 |
| 忘记密码 | 手机取回 | ✅ | ✅ | `UserService.ResetBySmsCode` | 完整 |
| 忘记密码 | 邮箱取回 | ✅ | ✅ | `UserService.ResetByMailCode` | 完整 |
| 忘记密码 | 验证问题取回 | ❌ | ❌ | 实体无此字段，未实现 | 未实现 |

### 1.2 接入方式差异

| 维度 | MVC（服务端渲染） | API（前后端分离） |
|------|------------------|------------------|
| 登录页 | `~/Admin/User/Login`（Razor 视图 + Bootstrap Tab） | 皮肤自带 `/login` 路由（Vue/React 等） |
| 端点前缀 | `/Admin/User/*`、`/Sso/*` | `/Auth/*`、`/Sso/*` |
| 请求格式 | HTML 表单 POST（`application/x-www-form-urlencoded`） | JSON（`application/json`） |
| 登录状态 | Cookie 会话（`token-{系统名}`）+ 自动跳转 | `accessToken/refreshToken`（前端自行保存） |
| 密码加密 | `GET /Admin/User/GetLoginKey` + JSEncrypt（PKCS#1v1.5） | `GET /Auth/Challenge` + Web Crypto（RSA-OAEP/SHA-256） |
| 验证码发送 | `POST /Admin/User/SendVerifyCode` | `POST /Auth/SendCode` |
| 登录配置 | `LoginViewModel`（服务端填充） | `GET /Auth/LoginConfig`（JSON） |
| 业务逻辑 | 两者**共用同一套** `UserService` / `SsoController`，不重复实现 | 同左 |

> 核心原则：**业务逻辑全部收敛在 `UserService`**（登录/注册/重置/发码），MVC 控制器与 API 控制器都是薄封装。

---

## 2. 登录

登录入口统一为 `UserService.Login(LoginModel, HttpContext)`，按 `LoginModel.Category` 分发：

```
UserService.Login
├── Category=Password（默认） → LoginByPassword   （密码 + RSA 解密 + 防爆破 + 外部验证）
├── Category=Mobile           → LoginBySms        （手机号 + 短信验证码，未注册可自动建号）
└── Category=Mail             → LoginByMail       （邮箱 + 邮件验证码，未注册可自动建号）
```

`LoginModel` 关键字段：

| 字段 | 说明 |
|------|------|
| `Username` | 用户名 / 手机号 / 邮箱（按 Category 语义） |
| `Password` | 密码，或 RSA 加密后的 Base64 密文；验证码登录时即验证码 |
| `Category` | `AuthCategory` 枚举：`Password=0 / Mobile=1 / Mail=2 / OAuth=3` |
| `Remember` | 记住登录（MVC 写长 Cookie，API 影响令牌有效期） |
| `ChallengeId` | RSA 挑战标识，非空时后端解密 `Password` |
| `CaptchaId/CaptchaCode` | 图片验证码（`CaptchaScene & 1` 启用时必填） |

登录成功后统一走 `CompleteLogin`：记录登录统计 → 自动绑定租户 → 颁发 JWT → 写 Cookie / 返回 Token；用户开启 MFA 时中断返回挂起令牌。

### 2.1 密码登录

**流程**

```
① GET 登录页/登录配置
② （可选）GET Challenge/GetLoginKey 获取 RSA 公钥 → JSEncrypt/WebCrypto 加密密码
③ POST Login（category 缺省=Password），携带 username / 加密密码 / challengeId
④ 服务端：校验图片验证码（如启用）→ 防爆破计数检查 → 私钥解密密码 → 本地验证
    │        ├─ 失败 → 尝试外部验证服务（ExternalAuthUrl，可自动同步用户）
    │        └─ 成功 → CompleteLogin 颁发令牌 / 写 Cookie
⑤ 前端跳转 ReturnUrl 或首页
```

**MVC**

```http
GET  /Admin/User/Login?r={returnUrl}            # 登录页（含 challengeId + 公钥）
POST /Admin/User/Login                          # 表单：username/password/challengeId/remember
GET  /Admin/User/GetLoginKey                    # 获取新鲜 RSA 公钥（登录提交前调用）
```

**API**

```http
GET  /Auth/LoginConfig?tenant={tenant}          # 登录配置（含 login.password/captcha）
GET  /Auth/Challenge                            # 获取 challengeId + publicKey（RSA-OAEP/SHA-256）
POST /Auth/Login
Content-Type: application/json
{ "username": "admin", "password": "<明文或密文>", "challengeId": "", "captchaId": "", "captchaCode": "" }
```

**安全要点**

- `AllowPlainPassword=false` 时强制加密传输，未带 `challengeId` 直接拒绝；`challengeId` 无效（过期/伪造）明确报错，不静默降级明文
- Challenge 有效期 300 秒、一次性使用（防重放）；前端必须**提交时**获取新鲜公钥，禁止预取缓存复用
- 防爆破：用户名 / IP / 子网(24/16) 三级连续错误计数，超过 `MaxLoginError` 锁定 `LoginForbiddenTime` 秒

### 2.2 手机验证码登录

**流程**

```
① POST SendCode { channel: "Sms", username: 手机号, action: "Login" }  → 下发短信验证码
② POST Login { username: 手机号, password: 验证码, category: "mobile" }
③ 服务端：校验验证码（SmsLoginCodePrefix 缓存）→ 查 User.FindByMobile
    │        ├─ 不存在 → 若 AutoRegister=true 自动建号（Name=P{手机号}，MobileVerified=true）
    │        └─ 存在 → 检查 Enable → CompleteLogin
```

**MVC**（登录面板"手机验证码"子 Tab）

```http
POST /Admin/User/SendVerifyCode   # { channel:"Sms", username, action:"Login" }
POST /Admin/User/Login            # 表单：username=手机号, password=验证码, category=Mobile
```

**API**

```http
POST /Auth/SendCode   # { "channel": "Sms", "username": "13800138000", "action": "Login" }
POST /Auth/Login      # { "username": "13800138000", "password": "1234", "category": "mobile" }
```

> 手机/邮箱验证码登录的 `password` 字段是**验证码**，不做 RSA 加密。

### 2.3 邮箱验证码登录

与手机验证码登录同构：`channel: "Mail"`、`category: "mail"`、按 `User.FindByMail` 查用户，未注册自动建号（`Name=邮箱前缀`，`MailVerified=true`）。

### 2.4 三方登录（OAuth）

**流程（全页跳转，MVC 与 API 皮肤通用）**

```
① 登录页点击三方图标 → GET /Sso/Login?name={provider}&r={returnUrl}
② 记录 OAuthLog（state=log.Id）→ 重定向到提供商授权页
③ 用户授权 → 提供商回调 GET /Sso/LoginInfo/{provider}?code=&state=
④ 用 code 换 accessToken → 取用户信息（OpenID/UserName/Avatar...）
⑤ 按 OpenID 找 UserConnect：
    ├─ 已绑定 → 直接登录该本地用户
    ├─ 未绑定且用户已登录 → 绑定到当前用户
    ├─ 未绑定且未登录 → 自动注册（OAuthConfig.AutoRegister）或跳转补全资料页（OAuth 回跳注册）
    └─ 绑定后写 UserConnect + CompleteLogin
```

**入口**

| 场景 | 端点 |
|------|------|
| 发起三方登录 | `GET /Sso/Login?name={provider}&r={returnUrl}` |
| 授权回调 | `GET /Sso/LoginInfo/{provider}?code=&state=` |
| 绑定/取消绑定（已登录） | `POST /Admin/User/Binds`、`/Sso/Bind`、`/Sso/UnBind` |
| 回跳待注册预填信息 | `GET /Auth/OAuthPendingInfo?token=` |
| 回跳注册 | `POST /Auth/Register { category:"oauth", oauthToken, ... }` |

**说明**

- 提供商配置见 `OAuthConfig` 表（后台 系统管理 → OAuth配置），`Visible` 控制登录页显示
- 登录页仅显示 `Visible=true` 的提供商；`AutoRegister` 控制未绑定用户是否自动建号
- 单点登录场景：禁用本地登录且仅一个三方 → 登录页直接跳转该三方；多个三方 → 展示图标供选择
- 微信小程序/App 走专用端点 `POST /Sso/WxMiniLogin`、`POST /Sso/WxAppLogin`（`code` + `appId`），不走浏览器跳转

### 2.5 MFA 二步验证

`CubeSetting.EnableMfa=true` 且用户自助开启 TOTP 后：账密通过但不下发正式令牌，`Login` 响应 `message` 以 `mfa_required:` 开头并携带挂起令牌，前端引导进入 `/Auth/Mfa/*` 二步验证流程。

---

## 3. 注册

注册入口统一为 `UserService.Register(AuthRegisterModel, HttpContext)`，按 `Category` 分发：

```
UserService.Register
├── Category=Password → RegisterByPassword   （用户名 + 密码，邮箱/手机可选填，不校验验证码）
├── Category=Mobile   → RegisterByPhoneCode  （手机号 + 短信验证码 + 密码，须已发过验证码）
├── Category=Mail     → RegisterByMailCode   （邮箱 + 邮件验证码 + 密码）
└── Category=OAuth    → RegisterByOAuthBind  （三方回跳补全资料绑定）
```

`AuthRegisterModel` 关键字段：`Username / Email / Mobile / Password / ConfirmPassword / Code（验证码）/ Category / OAuthToken / CaptchaId/CaptchaCode`。

**统一规则**

- `AllowRegister=false` 禁止注册
- 图片验证码：`CaptchaScene & 2` 启用时注册须携带 captcha
- 租户：多租户下须携带 `X-App-Id` 或 `X-Tenant` 头
- 禁止使用 OAuth 前缀用户名（`{provider}_`）
- 用户名 / 邮箱 / 手机 均查重
- 密码强度：`PasswordService.Valid`（`PaswordStrength` 正则，空/`*` 不校验）
- **密码可选（手机/邮箱/OAuth 回跳注册）**：密码留空则生成**随机密码**，该账号无法使用密码登录（仅验证码/三方登录），登录后可再主动设置密码，不打扰用户；用户名注册仍须设置密码
- **协议勾选（合规，强制）**：前端注册表单须勾选同意《用户协议》《隐私政策》（未勾选前端拦截，不提交）；后端不再校验（纯前端强制）
- **联系方式验证状态**：只有验证码校验通过才标 `MailVerified/MobileVerified=true`；用户名注册填写的联系方式**保持未验证**（防"未验证却标已验证"）
- 注册成功后自动登录（`CompleteLogin` 写 Cookie/返回 Token），MVC 版落回登录页

### 3.1 用户名注册

用户名 + 密码 + 确认密码；邮箱/手机**可选填**（不校验验证码，保持未验证状态）。

**MVC**：登录页"注册"面板 → "用户名注册"子 Tab，`POST /Admin/User/Register { category:Password, username, password, confirmPassword }`。
**API**：`POST /Auth/Register { category:"", username, email?, mobile?, password, confirmPassword }`。

### 3.2 手机注册

手机号 + 短信验证码 + 密码（**可选**）+ 确认密码。须先 `SendCode(action=Register)` 发送验证码，后端校验 `SmsRegisterCodePrefix` 缓存。密码留空则随机密码（仅验证码登录，登录后可再设置）。

**MVC**：注册面板"手机注册"子 Tab，`POST /Admin/User/Register { category:Mobile, mobile, code, password, confirmPassword }`。
**API**：`POST /Auth/Register { category:"mobile", mobile, code, password, confirmPassword }`。

### 3.3 邮箱注册

邮箱 + 邮件验证码 + 密码（**可选**）+ 确认密码。校验 `MailRegisterCodePrefix` 缓存。密码留空则随机密码（仅验证码登录，登录后可再设置）。

**MVC**：注册面板"邮箱注册"子 Tab，`POST /Admin/User/Register { category:Mail, email, code, password, confirmPassword }`。
**API**：`POST /Auth/Register { category:"mail", email, code, password, confirmPassword }`。

### 3.4 三方回跳注册/绑定

三方登录后用户不存在且未自动注册时，跳转补全资料页：`GET /Auth/OAuthPendingInfo?token=` 取预填信息（建议用户名/邮箱/手机/头像），提交 `POST /Auth/Register { category:"oauth", oauthToken, username?, password, confirmPassword }` 完成建号 + 绑定。

---

## 4. 忘记密码（重置密码）

重置入口统一为 `UserService.ResetPassword(account, code, newPassword, confirmPassword, challengeId, ip)`，按账号格式自动分发：

```
UserService.ResetPassword
├── 账号是手机号 → ResetBySmsCode   （SmsResetCodePrefix 缓存校验）
└── 账号是邮箱   → ResetByMailCode  （MailResetCodePrefix 缓存校验）
```

**流程（两步）**

```
① POST SendCode { channel: Sms|Mail, username: 手机号|邮箱, action: "ResetPassword" } → 下发验证码
② POST ResetPassword { username: 手机号|邮箱, code, newPassword, confirmPassword, challengeId? }
    → 校验验证码 → 查用户 → 校验密码强度 → 更新密码 → 删除验证码缓存
```

**MVC**：登录页"忘记密码"面板两步表单（账号+发送验证码 → 验证码+新密码+确认），提交 `POST /Admin/User/ForgetPassword`（`ResetPwdModel`，新密码支持 GetLoginKey+RSA）。
**API**：`POST /Auth/ResetPassword`（JSON，新密码支持 Challenge 加密）。

**安全要点**

- 新密码同样受 `PasswordService.Valid` 强度校验
- `AllowPlainPassword=false` 时重置须带 `challengeId`（新密码加密传输）
- 验证码一次性、防重放；重置成功后删除验证码缓存
- 发送验证码按 IP 限频（`SmsResetIpPrefix` 等）

### 4.1 手机取回 / 邮箱取回

两者流程完全一致，仅通道不同：手机号 → 短信，邮箱 → 邮件。前端按输入格式自动识别通道（或提供通道切换按钮）。

### 4.2 验证问题取回（密保 Q&A）—— 未实现

- **`XCode.Membership.User` 实体没有 `Question` / `Answer` 字段**（已核实 `用户.cs` 实体与 `Member.xml` 模型，字段仅含 Name/Password/Mail/Mobile 等），密保功能**完全不存在**，管理端 `UserController` 里的 `RemoveField("Question","Answer")` 只是对不存在的历史字段的防御性清理
- **不建议实现**：验证码取回（短信/邮件）是主流且更安全，密保答案易泄露/遗忘
- 若未来需要，需先在 XCode 的 `Member.xml` 增加 `Question`/`Answer` 字段并重新生成实体，再补充 Q&A 流程设计（校验答案 → 直接重置密码，注意 Answer 需加密存储）

---

## 5. 支撑能力

### 5.1 发送验证码

| 端点 | 参数 |
|------|------|
| MVC `POST /Admin/User/SendVerifyCode` | `channel`(Sms/Mail)、`username`(手机号/邮箱)、`action`(Login/Register/ResetPassword/Bind/Notify)、`captchaId/captchaCode` |
| API `POST /Auth/SendCode` | 同上（JSON） |

- 通道实现：`ISmsVerifyCode`（阿里云短信）/ `IMailVerifyCode`（SMTP），后台"系统管理→短信配置/邮件配置"管理
- 按 IP + 账号限频（发送间隔、10 分钟累计次数）
- 验证码缓存前缀：`Sms{Action}CodePrefix` / `Mail{Action}CodePrefix`，过期自动失效
- `CaptchaScene & 4` 启用时发送验证码前须过图片验证码

### 5.2 图片验证码（Captcha）

- `GET /Admin/User/Captcha`（MVC）/ `GET /Auth/Captcha`（API）→ 返回 `captchaId` + PNG base64 图片（`DrawingCaptchaService` 算术题，TTL 300 秒）
- **场景强制（`CaptchaScene` 位掩码）**：`1`=登录、`2`=注册、`4`=发验证码，可组合（如 `3`=登录+注册）；强制要求**不受**自适应豁免
- **风险自适应（`CaptchaRisk`，默认 true）**：CaptchaScene 未覆盖的场景按"机器安全度"动态决定——内网/可信设备/无异常免验证码，公网+近期登录失败/封禁中要求验证码（`UserService.RequireCaptcha`）
  - 风险评分：`0`=内网（127/10/172.16-31/192.168/::1），`1`=公网，`2`=公网+近期登录失败（IP/账号/子网维度），`3`=封禁中（达到 `MaxLoginError`/子网阈值）
  - `CaptchaRiskThreshold`（默认 2）：风险评分达到该值要求验证码
  - **可信设备**：登录/注册成功后标记设备（`CubeDeviceId` Cookie 指纹），有效期内（`TrustedDeviceDays`，默认 30 天）免**自适应**验证码；换 IP 视为不可信；`CaptchaScene` 强制场景**不豁免**
  - 发码场景（防短信轰炸）**不豁免**可信设备
- 校验成功立即失效（防重放）
- 前端 MVC：登录/注册页按服务端 `RequireCaptcha`/`RequireCaptchaRegister` 初始显示验证码行（动态注入）；提交/发码被拒（"验证码错误或已过期"）时自动显示并刷新验证码，保证动态风险生效

### 5.3 RSA Challenge（密码加密传输）

| MVC | API | 算法 | 用途 |
|-----|-----|------|------|
| `GET /Admin/User/GetLoginKey` | `GET /Auth/Challenge` | 服务端 RSA 密钥对 | 登录密码 / 重置新密码 |

- MVC 用 JSEncrypt（PKCS#1v1.5），API 用 Web Crypto（RSA-OAEP/SHA-256，公钥 PKCS#8 SPKI）
- 挑战 300 秒有效、一次性

### 5.4 登录配置

- API：`GET /Auth/LoginConfig?tenant=` → `{ name, copyright, registration, loginTip, loginLogo, loginBackground, login{password,sms,mail,captcha,sendCode}, register{enabled,password,sms,mail,captcha}, oauth[], security{challengeRequired,mfaAvailable,passwordComplexity,passwordStrength} }`；`login.captcha`/`register.captcha`/`login.sendCode` 按当前请求环境**动态判定**（CaptchaScene 强制 + 风险自适应），非静态位掩码
- MVC：`LoginViewModel` 服务端填充（`DisplayName/AllowLogin/AllowRegister/EnableSms/EnableMail/EnablePasswordComplexity/PasswordStrength/RequireCaptcha/RequireCaptchaRegister/OAuthItems/...`）

### 5.5 令牌与会话

- JWT：`accessToken` + `refreshToken`；`POST /Auth/Refresh` 刷新（自动轮换 + 旧令牌黑名单防重放）
- MVC Cookie：`token-{系统名}`（`TokenCookie` 控制）；`LogoutAll` 支持全局注销
- `POST /Auth/Logout` / `GET /Admin/User/Logout` 注销
- `TokenExpire` / `SessionTimeout` 控制有效期

### 5.6 多租户

- 注册/登录通过 `X-App-Id`（OAuth 应用租户）或 `X-Tenant`（租户编码）定位租户
- 登录/注册成功后自动绑定租户（`EnsureTenantUser`）并写租户 Cookie

### 5.7 外部验证

`ExternalAuthUrl` 配置后，本地密码验证失败自动转外部认证服务（企业统一认证），验证成功自动创建/同步本地用户（`ExternalAuthHelper`）。

### 5.8 账号管理（更换绑定 / 注销 / 导出）

| 功能 | MVC | 说明 |
|------|-----|------|
| 更换手机/邮箱 | `POST /Admin/User/BindByVerifyCode { account, code }` | 验证码校验**新号**所有权后绑定/更换（按格式分发手机/邮箱），旧号自动失效；API 可复用 `UserService.BindByVerifyCode` |
| 注销账号 | `POST /Admin/User/CloseAccount` | 软删除：`Enable=false` 禁用 + 清空敏感字段（Mail/Mobile/DisplayName/Avatar/Password 等）+ 吊销全部令牌 + 解绑三方 + 清理在线记录，保留 ID/Name 防重名与审计 |
| 导出个人数据 | `GET /Admin/User/ExportData` | JSON 文件下载：个人资料 + 第三方绑定 + 令牌记录 |

> 注销与导出均依据《中华人民共和国个人信息保护法》要求提供（注销对应删除权、导出对应数据可携带权），前端入口位于用户信息页"安全中心"区块，页面文案已标注。

---

## 6. 配置项（CubeSetting）速查

| 配置项 | 默认 | 说明 |
|--------|------|------|
| `AllowLogin` | true | 允许密码登录 |
| `AllowRegister` | true | 允许注册 |
| `EnableSms` | false | 短信验证码：手机登录/注册/找回通道 |
| `EnableMail` | false | 邮件验证码：邮箱登录/注册/找回通道 |
| `AutoRegister` | — | 手机/邮箱验证码登录时未注册自动建号 |
| `AllowPlainPassword` | true | 允许明文密码；false 强制 RSA 加密传输 |
| `PaswordStrength` | 强密码正则 | 密码强度正则（空/`*` 不校验），注意拼写 |
| `EnablePasswordComplexity` | true | 是否启用密码复杂度校验 |
| `CaptchaScene` | 0 | 图片验证码**场景强制**位（1=登录,2=注册,4=发码，可组合；强制不受自适应豁免） |
| `CaptchaRisk` | true | 风险自适应验证码：自动感知机器安全度，不安全环境要求验证码 |
| `CaptchaRiskThreshold` | 2 | 风险阈值（0=内网,1=公网,2=公网+失败,3=封禁），达到该值要求验证码 |
| `TrustedDeviceDays` | 30 | 可信设备有效期（天），期内免**自适应**验证码 |
| `MaxLoginError` / `LoginForbiddenTime` | — | 防爆破：错误次数 / 锁定秒数 |
| `MaxLoginErrorBySubnet24/16` | — | 子网级防爆破 |
| `EnableMfa` | false | 开放 MFA（TOTP）能力 |
| `TokenCookie` | — | MVC 是否写令牌 Cookie |
| `TokenExpire` / `SessionTimeout` | — | 令牌 / 会话有效期 |
| `DefaultRole` | — | 注册默认角色 |
| `ExternalAuthUrl` | 空 | 外部验证服务地址 |
| `LoginTip` / `LoginLogo` / `LoginBackground` / `Copyright` / `Registration` | — | 登录页展示配置 |

---

## 7. 端点对照总表

| 业务 | MVC | API |
|------|-----|-----|
| 登录页/配置 | `GET /Admin/User/Login` | `GET /Auth/LoginConfig` |
| 图片验证码 | `GET /Admin/User/Captcha` | `GET /Auth/Captcha` |
| RSA 公钥 | `GET /Admin/User/GetLoginKey` | `GET /Auth/Challenge` |
| 登录 | `POST /Admin/User/Login` | `POST /Auth/Login` |
| 发送验证码 | `POST /Admin/User/SendVerifyCode` | `POST /Auth/SendCode` |
| 注册 | `POST /Admin/User/Register` | `POST /Auth/Register` |
| 重置密码 | `POST /Admin/User/ForgetPassword` | `POST /Auth/ResetPassword` |
| 令牌刷新 | `POST /Admin/User/RefreshToken` | `POST /Auth/Refresh` |
| 当前用户 | `GET /Admin/User/Info` | `GET /Auth/Info` |
| 注销 | `GET /Admin/User/Logout` | `POST /Auth/Logout` |
| 三方登录 | `GET /Sso/Login` / `GET /Sso/LoginInfo` | 同左（全页跳转） |
| 微信登录 | — | `POST /Sso/WxMiniLogin` / `POST /Sso/WxAppLogin` |
| 绑定 | `POST /Admin/User/BindByVerifyCode`、`/Sso/Bind` | `POST /Auth/Register(category=oauth)` |
| 更换手机/邮箱 | `POST /Admin/User/BindByVerifyCode` | 复用 `UserService.BindByVerifyCode` |
| 注销账号 | `POST /Admin/User/CloseAccount` | 复用 `UserService.CloseAccount` |
| 导出个人数据 | `GET /Admin/User/ExportData` | 复用 `UserService` 数据组装 |

---

## 8. MVC 登录页设计（2026-08 版）

登录页采用"三大面板 + 登录内子 Tab"结构（`~/Admin/User/Login`，ACE 风格）：

```
登录卡片
├── 品牌区（Logo / 系统名 / 副标题）
├── 主 Tab：登录 | 注册(可选) | 忘记密码
├── 登录面板
│   ├── 子 Tab：密码登录(AllowLogin) | 手机验证码(EnableSms) | 邮箱验证码(EnableMail)
│   ├── 密码登录：用户名 + 密码 + 记住我 + 忘记密码链接 + 登录按钮（RSA 加密）
│   ├── 手机验证码：手机号 + [发送验证码] + 验证码 + 登录按钮
│   └── 邮箱验证码：邮箱 + [发送验证码] + 验证码 + 登录按钮
│   └── 图形验证码：任一登录表单在 `RequireCaptcha`（CaptchaScene 强制或风险自适应）时显示"图片+输入框"，点击图片刷新，提交/发码被拒时自动显示
├── 注册面板
│   ├── 子 Tab：用户名注册 | 手机注册 | 邮箱注册
│   ├── 用户名注册：用户名 + 密码 + 确认密码（协议勾选）
│   ├── 手机/邮箱注册：账号(+验证码) + 密码(选填) + 确认密码 + 协议勾选（留空密码=仅验证码登录）
│   └── 协议勾选：已阅读并同意《用户协议》《隐私政策》
│   └── 图形验证码：`RequireCaptchaRegister` 时同样显示
├── 忘记密码面板（两步）
│   ├── Step1：手机号/邮箱 + [发送验证码]
│   └── Step2：验证码 + 新密码 + 确认新密码（RSA 加密提交）
└── 第三方登录（OAuthConfig.Visible 图标列表，flex 换行居中）
```

要点：子 Tab / 验证码按钮 / 密码校验提示样式统一；密码强度正则由 `CubeSetting.PaswordStrength` 下发前端动态校验（空/`*` 跳过）；所有密码框支持 RSA 加密传输。

---

## 9. 常见问题

- **注册后是否自动登录？** MVC 版注册成功落回登录页（历史行为，需手动登录）；API 版 `/Auth/Register` 直接返回 Token。
- **手机/邮箱验证码登录提示"验证码已过期"？** 发送验证码与登录间隔超过有效期（默认 10 分钟），重新发送即可。
- **验证码登录提示"用户不存在"？** 未开启 `AutoRegister`；开启后首次验证码登录自动建号。
- **`AllowPlainPassword=false` 但登录报"禁止明文"？** 前端未走 Challenge 流程，检查是否调用 `GetLoginKey`/`Challenge` 并携带 `challengeId`。
- **密保问题找回可用吗？** 未实现，请使用手机/邮箱验证码取回。

---


    
