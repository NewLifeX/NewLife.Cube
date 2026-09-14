# 架构证据计划（Evidence Plan）

检查清单：已核实的证据来源（均为仓库内文件，2026-09-01 于 `x_master`/HEAD `6282baa7` 核实）。

## 1. 项目结构与依赖（支撑 C4 + 依赖图）

- [x] `魔方.sln`、`魔方3.sln` —— 解决方案组成（MVC版 / WebApi版 / 主题 / 测试分组）
- [x] `NewLife.Cube\NewLife.Cube.csproj` —— net6.0–net10.0、PackageId=NewLife.Cube、
      NewLife.Core/XCode/IP/AI/Office/Stardust.Extensions 依赖、wwwroot EmbeddedResource、
      与 CubeNC 的双向 `<Compile Include>` 源码链接（:89/:95/:105/:123）
- [x] `NewLife.CubeNC\NewLife.CubeNC.csproj` —— Sdk.Web、AssemblyName=NewLife.Cube、
      PackageId=NewLife.Cube.Core、DefineConstants=MVC
- [x] `CubeDemo\CubeDemo.csproj`、`CubeDemoNC\CubeDemoNC.csproj`、`CubeSSO\CubeSSO.csproj`
- [x] `NewLife.Cube.Tests\NewLife.Cube.Tests.csproj`（注意：不在两个 sln 中）、`E2EMvcTest`
- [x] `pnpm-workspace.yaml`、根 `package.json`、`packages/` 下 5 个 @newlifex/* 共享 TS 包
- [x] `Directory.Build.props`（仅 NuGetAudit=false，无集中版本）
- [x] `NewLife.CubeST/`（仅 bin/obj 残留 → 标注为 deprecated）

## 2. 登录与认证链（支撑流程图一）

- [x] `NewLife.Cube\Areas\Admin\Controllers\UserController.cs:302`（Login 入口）
- [x] `NewLife.Cube\Controllers\AuthController.cs:42/:133/:258`（SPA 登录 + RSA 挑战）
- [x] `NewLife.Cube\Services\Auth\AuthEnhancedService.cs:41`（密码/短信/邮箱分发）
- [x] `NewLife.CubeNC\Services\UserService.cs:91`（LoginByPassword 风控/解密）与 `:186`
      （CompleteLogin：可信设备→统计→EnsureTenantUser→ChooseTenant→SaveTenant→MFA 拦截→IssueLoginToken→SaveCookie）
- [x] `NewLife.Cube\Membership\ManageProvider.cs:12/:87/:130/:185/:231`
- [x] `NewLife.CubeNC\Membership\ManagerProviderHelper.cs:655`（IssueLoginToken）/`:570`（GetJwt）/`:833`（SaveCookie）/`:591`（LoadToken 四级回退）/`:46`（TryLogin 滑动续期）
- [x] `NewLife.Cube\Entity\用户令牌.cs:23` 与 `用户令牌.Biz.cs:153/:170`（吊销）
- [x] `NewLife.Cube\Controllers\SsoController.cs`（客户端 :121-:288；服务端 :603-:915）
- [x] `NewLife.Cube\Web\OAuthClient.cs:26/:114` + `Web\OAuth\` 14 个 provider
- [x] `CubeSSO\Program.cs`（独立 SSO 宿主）

## 3. 实体 CRUD 管道（支撑流程图二）

- [x] `NewLife.Cube\Common\ReadOnlyEntityController.cs:21/:55/:117/:292`
- [x] `NewLife.Cube\Common\EntityController.cs:16/:117/:199/:25`、`EntityController2.cs`
- [x] `NewLife.Cube\Common\EntityAuthorizeAttribute.cs:17/:43/:103/:166`（菜单解析与权限判定）
- [x] `NewLife.Cube\Common\DataPermissionAttribute.cs` + `NewLife.CubeNC\WebMiddleware\DataScopeMiddleware.cs:12/:33/:70`
- [x] `NewLife.Cube\Extensions\Pager.cs:13`、`CubeDemo\appsettings.json:11-19`（连接串 provider= 机制）

## 4. 部署与发布（支撑部署拓扑图）

- [x] `CubeDemo\Properties\launchSettings.json`（https://localhost:7116）、`CubeDemoNC\appsettings.json:9`（7080/7081）、`CubeSSO\Properties\launchSettings.json`（8080）
- [x] `CubeSSO\Dockerfile`（aspnet，EXPOSE 80，卷 /cube）、`NewLife.Cube.Vue\web\docker\Dockerfile`（node20+pnpm→nginx:alpine）
- [x] `.github\workflows\test.yml`、`publish.yml`（tag→NuGet+GitHub Packages）、`publish-beta.yml`、`publish-npm.yml`
- [x] `scripts\e2e-mvc.cs`、`CubeDemoNC\bootstrap.sh`（Linux nohup 部署方式）
- [x] `CubeDemo\appsettings.json`（SQLite 默认三库：Membership/Cube/Log；注释给出 mysql 示例）
- [x] `NewLife.Cube\CubeService.cs:106-108`（默认内存缓存）、`CubeDemoNC\Startup.cs:34-57`（可选 Redis）
- [x] 三个宿主 `AddStardust(null)`（星尘注册/配置中心，地址为注释证据）

## 5. 文档证据（human-readable 佐证）

- [x] `Doc\架构设计.md`、`Doc\项目架构建议.md`、`Doc\Api\认证接口设计.md`、
      `Doc\OAUTH-OAuth与SSO.md`、`Doc\MVC-*`、`Doc\SPA-*` 系列（用于交叉验证，不作为唯一依据）

## 已知证据缺口（各工件中须保持标注）

1. `NewLife.Cube.React.csproj` 被 `魔方3.sln`/CubeDemo.csproj 引用但磁盘缺失 —— 陈旧引用，状态待清理。
2. XCode/NewLife.Core 包内实现（ManageProvider 基类、PermissionFlags、Entity DAL 全集）不在仓库，
   仅按引用关系标注。
3. 支持的数据库全集未在本仓库验证（仅见 sqlite 在用、mysql 示例、NewLife.MySql 依赖）。
4. `NewLife.Cube.Tests` 未入 sln，其 CI 触发路径未核实。
5. 星尘服务端地址仅存在于注释（`http://star.newlifex.com:6600`），按 low 置信处理。
