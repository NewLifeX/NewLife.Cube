# 关键流程分析（flows）

- 产出技能：`flow-visualizer` + `graphviz`
- 视图状态：**current-state**，2026-09-01，分支 `x_master`
- 图源码：[`auth-login-flow.dot`](./auth-login-flow.dot)、[`entity-crud-pipeline.dot`](./entity-crud-pipeline.dot)

## 流程一：登录与认证链

**入口（二选一）**
- 后台表单：`POST /Admin/User/Login`（`NewLife.Cube\Areas\Admin\Controllers\UserController.cs:302`）
- SPA：`/Auth/Login`（`NewLife.Cube\Controllers\AuthController.cs:42`，RSA 挑战加密防明文 `:258`）

**主链（同步、单进程内）**
1. `AuthEnhancedService.Login`（`Services\Auth\AuthEnhancedService.cs:41`）按密码/短信/邮箱分发。
2. `UserService.LoginByPassword`（`NewLife.CubeNC\Services\UserService.cs:91`）：
   风控计数（账号/IP/子网 `:120-125`）→ 挑战解密 `:133-140` → `ManageProvider.Provider.Login:143`。
3. `ManageProvider2.Login`（`Membership\ManageProvider.cs:87`，OAuth 密码模式分支 `:130`）。
4. `UserService.CompleteLogin`（`UserService.cs:186`）：
   可信设备 `:191` → 登录统计 `:201` → `EnsureTenantUser:209`（无归属自动建 TenantUser，`:253/:273-281`）
   → `ChooseTenant:212`（0=管理后台哨兵）→ `SaveTenant:216`。
5. **MFA 分支**：开启时拦截并颁发挂起令牌（`:221-230`），`MfaController.cs:139` 验证后才继续。
6. `IssueLoginToken`（`ManagerProviderHelper.cs:655`）：refreshToken `:664` → `UserToken.Insert:678`
   → JWT（`jti=ut.ID`）`:689`，密钥 `CubeSetting.JwtSecret`（`GetJwt:570`）。
7. `SaveCookie:833`：Cookie 名 `token-{SysConfig.Name}`。

**后续请求校验**：`LoadToken:591`（Authorization → X-Token → Query → Cookie 四级回退）
→ `LoadUser:137`（jti 查 UserToken 吊销状态 `:179-186`）→ `TryLogin:46`（滑动续期 `:66-95`）。

**登出**：`ManageProvider2.Logout:231` —— 多设备按 jti 精确吊销 `:243-245`，单设备全吊销 `:250`；
`用户令牌.Biz.cs:153/:170`。

**OAuth/SSO 并行路径**
- 客户端：`SsoController.Login:121` → 第三方授权 → 回调 `LoginInfo:168` → `GetAccessToken:201`
  → `GetUserInfo:229` → `SsoClientService.OnLogin:179`（UserConnect 绑定/自动注册）→ `IssueLoginToken:288`。
- 服务端（魔方作 OAuth2 Server）：`Authorize:603` / `Access_Token:646` / `PasswordToken:731`
  / `UserInfo:775` / `Refresh_Token:809`，令牌逻辑在 `TokenService.cs:202-:391`。
  `CubeSSO` 是该服务端角色的独立部署宿主。

**失败路径**：凭据错误/风控超限（登录失败返回）、未认证 401、租户校验失败 400。

## 流程二：实体管理 CRUD 管道

1. **租户中间件**：`DataScopeMiddleware.cs:11`（`CubeService.cs:371` API / `NewLife.CubeNC\CubeService.cs:442` NC 注册）——
   `GetTenantResolution:32`（单一解析入口：X-App-Id → X-Tenant 编码 → X-Tenant-Id → Query → Cookie，
   结果缓存 HttpContext.Items，同一请求只解析一次）→ `SetTenant:38`
   （`TenantContext` AsyncLocal）→ 建 `DataScopeContext:67`；显式无效租户 400（`:43`）。
   注：仓库中无 `TenantProvider` 类；租户能力 = 中间件 + AsyncLocal 上下文 + `TenantContextService`（`ManagerProviderHelper.cs:1149`）组合；
   `ITenantContext` 门面按当前请求解析（复用 `GetTenantResolution`），无请求场景回退 AsyncLocal。
2. **授权过滤器**：`EntityAuthorizeAttribute.cs:17`（IAuthorizationFilter，全局+特性双模式）——
   `OnAuthorization:43` → `ResolveMenu:166`（控制器/URL → Menu 表，缺菜单可 `CreateMenu:211` 扫描注册）
   → `TryLogin:109` → `user2.Has(menu, Permission):115`（权限位 Detail/Insert/Update/Delete 存菜单表）
   → 401/403 `HandleUnauthorized:129`。多租户菜单可见性 `:93-100`。
3. **控制器管道**：`ReadOnlyEntityController.cs:21` → `EntityController.cs:16` /
   `EntityController2.cs:29`：Index `:55`、Detail `:117`、Add/Save `:117`、Edit `:199`、
   Delete `:25`、DeleteSelect `:46`、DeleteAll `:84`、ExportFile `:292`（Excel `:309`/CSV `:355`）、
   ImportExcel `:280`/ImportCsv `:398`。分页 `Extensions\Pager.cs:13`。
4. **数据访问**：实体继承 XCode `Entity<TEntity>`（NuGet 包内）；连接串来自宿主
   `ConnectionStrings` + `provider=` 后缀（`CubeDemo\appsettings.json:11-19`，默认 SQLite 三库，
   注释给出 mysql 示例）；`Migration=On` 自动建表。

## 敏感数据标注

- 密码：经 RSA 挑战加密传输，服务端解密后校验，不落明文日志（证据到解密步骤为止，日志内容未逐一核查）。
- JWT：密钥 `CubeSetting.JwtSecret`；jti 绑定 UserToken 表，吊销即失效。
- 租户边界：fail-closed 校验（`ValidateTenant:254`，Shadow/Enforce 双模式）。

## 不确定项

- `ManageProvider`/`PermissionFlags`/`IUser.Has` 基类实现位于 XCode NuGet 包，仓库内无源码。
- 无独立 `ApiToken` 类——API 令牌即 JWT（IssueLoginToken/TokenService 颁发）。
- IssueLoginToken 的令牌缓存复用细节本次未逐行复核（历史评审曾记录"每次登录双发"问题，
  如需基于该结论行动，请先复核 `ManagerProviderHelper.cs:655-:710`）。
