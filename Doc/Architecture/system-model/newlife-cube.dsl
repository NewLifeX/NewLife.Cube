workspace "NewLife.Cube" "魔方：基于 NewLife.XCode 的 .NET 管理后台框架（现状模型，2026-09-01，分支 x_master）" {

    !identifiers hierarchical

    model {

        // ---------- 人员 ----------
        browserUser = person "浏览器用户" "通过内置后台界面或 SPA 皮肤使用魔方管理功能"
        integrator = person "集成开发者" "通过 NuGet/npm 包把魔方嵌入自己的 .NET 应用"
        ssoAdmin = person "SSO 管理员" "在 CubeSSO 中管理 OAuth 应用与统一用户"

        // ---------- 系统边界 ----------
        cube = softwareSystem "魔方 NewLife.Cube" "管理后台框架：双形态核心库 + 主题/皮肤包 + 演示与 SSO 宿主" {

            cubeCore = container "NewLife.Cube（WebAPI 核心）" "前后端分离形态；NuGet 包 NewLife.Cube；net6.0-net10.0；实体控制器管道、认证令牌、OAuth、多租户、内置后台区域；wwwroot 以嵌入资源随 DLL 分发" ".NET Class Library" {
                adminArea = component "内置后台区域（Areas/Admin + Areas/Cube）" "用户、角色、菜单、日志、租户、OAuth 配置、数据库、文件、应用、定时作业、附件、小组件等管理控制器" "Controllers"
                crudPipeline = component "实体控制器管道（Common）" "EntityController/EntityController2/ReadOnlyEntityController：Index/Detail/Add/Edit/Delete/Export/Import；EntityAuthorizeAttribute 权限拦截；DataPermissionAttribute" "Controllers + Filters"
                authServices = component "认证与用户服务（Services/Auth）" "AuthEnhancedService、UserService：密码/短信/邮箱登录、风控计数、CompleteLogin、MFA、可信设备" "Services"
                membership = component "Membership 与令牌（Membership）" "ManageProvider2、ManagerProviderHelper：登录态管理、IssueLoginToken/JWT、UserToken 颁发与吊销、Cookie、委托代理 CheckAgent" "Membership"
                oauthClients = component "OAuth 客户端与 SSO 客户端（Web）" "OAuthClient 反射工厂 + 14 个第三方 provider；SsoClient；SsoController 登录回调" "OAuth Clients"
                ssoServer = component "OAuth/SSO 服务端（Services/Sso）" "SsoServerService、TokenService、OAuthAppService：Authorize/AccessToken/UserInfo/RefreshToken 端点，魔方可作为 OAuth2 Server" "Services"
                multiTenant = component "多租户与数据权限（WebMiddleware）" "DataScopeMiddleware + TenantContext（AsyncLocal）：请求租户解析（X-App-Id/X-Tenant/Cookie）、fail-closed 校验、数据范围上下文" "Middleware"
                foundationServices = component "基础服务（Services）" "验证码、短信/邮件、附件存储（Local/对象存储/S3）、字典 Lov" "Services"
                dataAccess = component "实体与数据访问（Entity）" "XCode 实体（含中文命名实体，如 用户令牌）、实体模型；DAL 由宿主 ConnectionStrings + provider= 后缀决定" "XCode Entities"
            }
            cubeNc = container "NewLife.CubeNC（MVC 核心）" "服务端渲染形态；NuGet 包 NewLife.Cube.Core；DefineConstants=MVC；Razor 视图、Modules/Jobs；与 WebAPI 核心经 csproj Compile Include 双向共享源码" ".NET Web (Sdk.Web)"

            spaThemes = container "SPA 皮肤包（9 个）" "Vue、ArcoVue、NaiveUI、MUI、Shadcn、Vuetify、Svelte、TDesign、Angular；各自独立前端工程（pnpm workspace），Vite 构建产物写入对应项目 wwwroot 后随 NuGet 包分发" "Vue3/React/Svelte/Angular + Vite"
            mvcThemes = container "MVC Razor 主题包（7 个）" "AdminLTE、Tabler、Metronic、Metronic8、ElementUI、LayuiAdmin、Blazor；静态资源+Razor 视图" ".NET Web"
            swaggerUi = container "NewLife.Cube.Swagger" "Swashbuckle + Scalar 的 API 文档集成" ".NET Class Library"

            cubeDemo = container "CubeDemo" "WebAPI 形态演示宿主；集成 Vue/ArcoVue 皮肤与 Swagger；net10.0" ".NET Web"
            cubeDemoNc = container "CubeDemoNC" "MVC 形态演示宿主；集成 7 个 Razor 主题；可选 Redis" ".NET Web"
            cubeSso = container "CubeSSO" "独立部署的统一用户中心 / OAuth2 服务端宿主" ".NET Web"

            tests = container "测试工程" "NewLife.Cube.Tests（xunit+SQLite，未入 sln）、E2EMvcTest（Playwright）、Test、XUnitTest" "xunit / Playwright"
        }

        // ---------- 外部系统与存储 ----------
        db = container "关系数据库（XCode DAL）" "演示默认 SQLite 三库：Membership / Cube / Log；连接串 provider= 后缀切换，注释中给出 MySQL 示例" "SQLite/MySQL/..."
        redis = container "Redis（可选）" "分布式缓存、DataProtection 密钥、事件总线；CubeDemoNC 演示配置，默认关闭" "Redis"
        oauthProviders = softwareSystem "第三方 OAuth 提供商" "QQ、微信、企业微信、小程序、GitHub、Microsoft、支付宝、淘宝、百度、微博、钉钉、IdentityServer4" "[external]"
        stardust = softwareSystem "星尘 Stardust" "微服务治理：服务注册与配置中心；宿主启动时 RegisterService 注册" "[external]"
        xcodeStack = softwareSystem "NewLife 基础栈（NuGet）" "NewLife.Core 11.18、NewLife.XCode 12.1（Entity/Membership 基类）、NewLife.IP、NewLife.AI、NewLife.Office、Stardust.Extensions" "[external]"
        downstreamApps = softwareSystem "下游业务应用" "通过 NuGet/npm 消费魔方构建管理后台的第三方系统" "[external]"
        npmRegistry = softwareSystem "包分发渠道" "NuGet（nuget.org + GitHub Packages）与 npm 仓库" "[external]"

        // ---------- 关系 ----------
        browserUser -> cubeDemo "使用 WebAPI 演示后台" "HTTPS"
        browserUser -> cubeDemoNc "使用 MVC 演示后台" "HTTPS"
        browserUser -> cubeSso "第三方登录 / 单点登录" "HTTPS"
        integrator -> cubeCore "NuGet 集成（前后端分离）" "NuGet"
        integrator -> cubeNc "NuGet 集成（服务端渲染）" "NuGet"
        integrator -> spaThemes "npm 定制前端皮肤" "npm"
        ssoAdmin -> cubeSso "管理 OAuth 应用与用户" "HTTPS"

        cubeDemo -> cubeCore "引用（ProjectReference）" "in-process"
        cubeDemo -> swaggerUi "API 文档" "in-process"
        cubeDemo -> spaThemes "UseVue/UseReact 等托管 SPA 产物" "in-process"
        cubeDemoNc -> cubeNc "引用（ProjectReference）" "in-process"
        cubeDemoNc -> mvcThemes "加载 Razor 主题" "in-process"
        cubeSso -> cubeNc "引用（ProjectReference）" "in-process"
        mvcThemes -> cubeNc "引用（ProjectReference）" "in-process"
        spaThemes -> cubeCore "引用（ProjectReference）" "in-process"
        swaggerUi -> cubeCore "引用（ProjectReference）" "in-process"
        tests -> cubeCore "单元/集成测试" "in-process"
        tests -> cubeNc "E2E/回归测试" "in-process"

        // WebAPI 核心内部组件关系
        adminArea -> crudPipeline "控制器继承实体管道基类" "in-process"
        crudPipeline -> membership "EntityAuthorizeAttribute.TryLogin / 用户权限判定" "in-process"
        crudPipeline -> dataAccess "实体查询/增删改、分页导出" "in-process"
        adminArea -> authServices "登录/用户管理接口" "in-process"
        authServices -> membership "Login/CompleteLogin/IssueLoginToken" "in-process"
        authServices -> dataAccess "用户/租户实体读写" "in-process"
        membership -> dataAccess "UserToken/用户实体读写" "in-process"
        oauthClients -> membership "第三方登录成功颁发令牌" "in-process"
        oauthClients -> authServices "UserConnect 绑定/自动注册" "in-process"
        ssoServer -> membership "OAuth2 Server 令牌颁发与校验" "in-process"
        ssoServer -> dataAccess "OAuth 应用/用户令牌读写" "in-process"
        multiTenant -> crudPipeline "请求级租户与数据范围上下文" "in-process"
        adminArea -> foundationServices "验证码/短信/邮件/附件" "in-process"

        cubeCore -> xcodeStack "Entity ORM、Membership 基类、日志" "in-process"
        cubeNc -> xcodeStack "Entity ORM、Membership 基类、日志" "in-process"

        cubeCore -> db "XCode DAL 读写（Membership / Cube / Log）" "SQL"
        cubeNc -> db "XCode DAL 读写（Membership / Cube / Log）" "SQL"
        cubeDemoNc -> redis "缓存/DataProtection/事件（可选）" "Redis 协议"

        cubeCore -> oauthProviders "第三方登录客户端（14 个 provider）" "HTTPS OAuth2"
        cubeNc -> oauthProviders "第三方登录客户端（14 个 provider）" "HTTPS OAuth2"
        cubeSso -> oauthProviders "作为 SSO 服务端可级联第三方登录（inferred）" "HTTPS OAuth2"

        cubeDemo -> stardust "RegisterService 注册 / 配置下发" "HTTP"
        cubeDemoNc -> stardust "RegisterService 注册 / 配置下发" "HTTP"
        cubeSso -> stardust "RegisterService 注册 / 配置下发" "HTTP"

        downstreamApps -> npmRegistry "获取 NewLife.Cube* 包" "NuGet/npm"
        cube -> npmRegistry "GitHub Actions 发布（tag → publish.yml）" "CI"
    }

    views {

        systemLandscape "landscape" "总览" {
            include *
            autoLayout
        }

        systemContext cube "context" "魔方系统上下文" {
            include *
            autoLayout
        }

        container cube "containers" "魔方容器视图" {
            include *
            autoLayout
        }

        component cubeCore "components-core" "WebAPI 核心组件视图" {
            include *
            autoLayout
        }

        deploymentEnvironment "演示与开发机" {
            deploymentNode "开发/演示环境" "Windows 或 Linux 单机" {
                deploymentNode "CubeDemo 进程" "net10.0 Kestrel" {
                    containerInstance cubeDemo
                    containerInstance db
                }
                deploymentNode "CubeDemoNC 进程" "net10.0 Kestrel" {
                    containerInstance cubeDemoNc
                    containerInstance redis
                }
                deploymentNode "CubeSSO 进程" "Kestrel 或 Docker（EXPOSE 80）" {
                    containerInstance cubeSso
                }
            }
        }

        styles {
            element "Person" {
                shape person
            }
            element "[external]" {
                background #999999
                color #ffffff
            }
        }
    }
}
