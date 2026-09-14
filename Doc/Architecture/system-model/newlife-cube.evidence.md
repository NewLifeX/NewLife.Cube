# 系统模型证据（newlife-cube.evidence）

置信度：high = 直接代码/配置证据；medium = 多个间接信号一致；low = 推断/注释证据。

## 节点证据

| 节点 | 置信度 | 证据 |
| --- | --- | --- |
| NewLife.Cube（WebAPI 核心） | high | `NewLife.Cube\NewLife.Cube.csproj`（net6.0–net10.0、PackageId、FrameworkReference Microsoft.AspNetCore.App、EmbeddedResource wwwroot :141） |
| NewLife.CubeNC（MVC 核心） | high | `NewLife.CubeNC\NewLife.CubeNC.csproj`（Sdk.Web、AssemblyName=NewLife.Cube、PackageId=NewLife.Cube.Core、DefineConstants=MVC） |
| 双向源码链接 | high | `NewLife.Cube\NewLife.Cube.csproj:89/:95/:105/:123`（Compile Include 链接 CubeNC 的 Jobs/Modules/Services/ViewModels/WebMiddleware；反向链接 AI/Common/Entity/OAuth） |
| SPA 皮肤包 ×9 | high | `pnpm-workspace.yaml`（10 个 web 目录含 React）、各 `web/package.json`（Vue 包名 @newlifex/cube-vue）、`NewLife.Cube.Vue\web\vite.config.ts:83`（outDir ../wwwroot） |
| MVC 主题包 ×7 | high | `魔方.sln` 主题分组、各主题 `.csproj`（Sdk.Web、ProjectReference CubeNC） |
| NewLife.Cube.Swagger | high | `NewLife.Cube.Swagger\NewLife.Cube.Swagger.csproj`（Swashbuckle 10.2.3 + Scalar） |
| CubeDemo / CubeDemoNC / CubeSSO | high | 各自 `Program.cs`（AddCube/UseCube/RegisterService）、csproj |
| 测试工程 | high | `NewLife.Cube.Tests\*.csproj`（xunit+Sqlite）、`E2EMvcTest`（Playwright）；注意两者均未入两个 sln |
| 数据库（XCode DAL） | high | `CubeDemo\appsettings.json:11-19`（Membership/Cube/Log 三连接串，provider=sqlite，注释 mysql 示例） |
| Redis（可选） | high | `CubeDemoNC\Startup.cs:34-57`、`CubeDemoNC\appsettings.json:11`（默认注释） |
| 第三方 OAuth 提供商 | high | `NewLife.Cube\Web\OAuth\` 目录 14 个 provider 文件 |
| 星尘 Stardust | medium | 三个宿主 `Program.cs` 的 `AddStardust(null)`/`RegisterService`；服务端地址仅注释证据（`CubeDemo\appsettings.json:9` 附近） |
| NewLife 基础栈 | high | 两个核心 csproj 的 PackageReference：NewLife.Core 11.18、NewLife.XCode 12.1、NewLife.IP 2.5、NewLife.AI、NewLife.Office、NewLife.Stardust.Extensions |
| NewLife.CubeST | high | 目录仅存 `bin`/`obj`（netcoreapp3.0）与 `.csproj.user`，不在任一 sln → deprecated |

## 组件证据（WebAPI 核心）

| 组件 | 置信度 | 证据 |
| --- | --- | --- |
| 内置后台区域 | high | `NewLife.Cube\Areas\Admin\Controllers\`（User/Role/Menu/Log/Tenant/OAuthConfig/Db/File/Lov…）、`Areas\Cube\Controllers\`（App/CronJob/Attachment/Widget…） |
| 实体控制器管道 | high | `NewLife.Cube\Common\ReadOnlyEntityController.cs:21`、`EntityController.cs:16`、`EntityController2.cs:29`、`EntityAuthorizeAttribute.cs:17` |
| 认证与用户服务 | high | `NewLife.Cube\Services\Auth\AuthEnhancedService.cs:41`、`NewLife.CubeNC\Services\UserService.cs:91/:186` |
| Membership 与令牌 | high | `NewLife.Cube\Membership\ManageProvider.cs:12`、`NewLife.CubeNC\Membership\ManagerProviderHelper.cs:655/:570/:833`、`Entity\用户令牌.cs:23` |
| OAuth 客户端 | high | `NewLife.Cube\Web\OAuthClient.cs:26/:114`、`Controllers\SsoController.cs:121-:288` |
| OAuth/SSO 服务端 | high | `Controllers\SsoController.cs:603-:915`、`Services\Sso\SsoServerService.cs`、`TokenService.cs:202/:227/:332/:391`、`OAuthAppService.cs` |
| 多租户与数据权限 | high | `NewLife.CubeNC\WebMiddleware\DataScopeMiddleware.cs:12/:33/:70`、`ManagerProviderHelper.cs:911/:383/:199/:254/:880` |
| 基础服务 | high | `NewLife.Cube\Services\`（验证码/MFA/Sms/Mail/附件存储 Local·Object·S3/Lov） |
| 实体与数据访问 | medium | `NewLife.Cube\Entity\` 中文命名实体；DAL 实现在 XCode NuGet 包内，本仓库仅见连接串约定 |

## 关系证据要点

- 项目引用关系：各 `.csproj` 的 `<ProjectReference>`（CubeDemo→Cube+Swagger+Vue+React+ArcoVue；
  CubeDemoNC→CubeNC+7 主题；CubeSSO→CubeNC；SPA 主题→Cube；MVC 主题→CubeNC；Swagger→Cube）。
- `CubeSSO → 第三方 OAuth`：**inferred（low）**——服务端端点齐全但无直接级联调用证据。
- `host → db`：XCode DAL 约定（连接串驱动），实体写入行为在 XCode 包内，仓库侧证据为连接串与实体定义。

## 未核实清单

1. React 主题磁盘缺失但被引用（陈旧引用）。
2. `NewLife.Cube.Tests` 的 CI 触发路径（不在两个 sln）。
3. 数据库支持全集、UserToken 物理表名映射。
4. 星尘服务端生产地址（仅注释）。
