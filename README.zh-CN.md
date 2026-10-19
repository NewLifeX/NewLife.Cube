# 魔方 NewLife.Cube

![GitHub top language](https://img.shields.io/github/languages/top/NewLifeX/NewLife.Cube?logo=github)
![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.Cube?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.Cube?label=NewLife.Cube&logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.Cube?label=NewLife.Cube&logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.Cube?label=dev%20nuget&logo=nuget)
![NuGet Downloads (Cube.Core)](https://img.shields.io/nuget/dt/NewLife.Cube.Core?label=NewLife.Cube.Core&logo=nuget)
![NuGet (Cube.Core)](https://img.shields.io/nuget/v/NewLife.Cube.Core?label=NewLife.Cube.Core&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml)
[![AOT](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/aot.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/aot.yml)
<!-- metrics:start -->
[![AOT smoke](https://img.shields.io/badge/AOT%20smoke-18%2F18-brightgreen?logo=dotnet)](Doc/AOT兼容性分析.md)
[![TFM](https://img.shields.io/badge/TFM-5%20%28net6.0%20%E2%86%92%20net10.0%29-512BD4?logo=dotnet)](https://www.nuget.org/packages/NewLife.Cube)
[![locales](https://img.shields.io/badge/locales-31-2ea44f?logo=dotnet)](Doc/多语言本地化.md)
<!-- metrics:end -->
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20x86%20%7C%20arm64%20%7C%20arm%20%7C%20loongarch64%20%7C%20riscv64%20%7C%20s390x-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Cube?logo=github)](https://github.com/NewLifeX/NewLife.Cube/graphs/contributors)

<!-- lang:start -->
🌐 [English](README.md) | [繁體中文](README.zh-TW.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

魔方是一个快速 Web 开发平台，面向“数据驱动 + 快速迭代 + 可深度定制”场景。几分钟内即可把实体 / 数据库表转为带权限、列表、详情、增删改、导出、统计的完整后台界面，并开放标准 API 给 Vue / Blazor / Antd / 移动端或第三方系统调用。默认模板已在真实项目中经历单表 100 亿数据增删改查考验。

演示站点：<https://cube.newlifex.com> `CentOS7 + CDN` · SSO中心：<https://sso.newlifex.com> `OAuth 服务端` · Swagger：[https://cube3.newlifex.com](https://cube3.newlifex.com)  

魔方教程：<https://newlifex.com/cube> · XCode教程：<https://newlifex.com/xcode> · 核心库教程：<https://newlifex.com/core>

> ℹ️ 本中文版与 [English README](README.md) 同批维护；若两者内容不一致，以英文版为对外口径。

**目录**：[快速开始](#快速开始) · [主要功能](#主要功能) · [生态全景](#生态全景) · [文档与示例](#文档与示例) · [参与贡献](#参与贡献) · [新生命开发团队](#新生命开发团队) · [许可证](#许可证)

## 快速开始

### 安装

通过 NuGet 引入 `NewLife.Cube.Core`。MIT 协议，可私有化替换字符串“NewLife”。支持 VS 与 CLI。`newlife.snk` 已公开便于强命名一致性。

```bash
dotnet add package NewLife.Cube
```

### 模板脚手架

```
dotnet new install NewLife.Templates
dotnet new cube --name CubeWeb
dotnet new xcode --name Zero.Data
cd CubeWeb
dotnet build
start http://localhost:6080
dotnet run
```

### 最小示例（.NET 8/9 Minimal Hosting）

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCube(); // 注册权限、菜单、实体控制器扫描
var app = builder.Build();
app.UseMetronic(app.Environment); // or UseAdminLTE / UseTabler ...
app.UseCube(app.Environment);      // 路由 & 静态资源 & 中间件
app.Run();
```

访问 `/` 登录后台；Swagger 查看接口；前端可调用标准 CRUD API。

### 平台支持

| 项目 | 覆盖范围 |
| --- | --- |
| 目标框架 | 单包 5 个：net6.0、net7.0、net8.0、net9.0、net10.0 |
| 系统与架构 | Windows、Linux、macOS；x64、x86、arm64、arm、loongarch64、riscv64、s390x |
| 数据库 | MySql / SQLite / SqlServer / Oracle / PostgreSql / SqlCe / Access / 达梦 |
| NativeAOT | 非 MVC 底座 18 个冒烟场景（JIT/AOT 双跑），当前数量见上方徽章 |

### ASP.NET Core 安装

* 在 *Visual Studio* 中新建`ASP.NET Core Web`项目
* 通过 *NuGet* 引用`NewLife.Cube.Core`，或自己编译最新的[魔方 NewLife.CubeNC](http://github.com/NewLifeX/NewLife.Cube)源码
* 在`appsettings.json`的`ConnectionStrings`段设置名为`Membership`的连接字符串，用户角色权限菜单等存储在该数据库
* 系统自动识别数据库类型，默认`Data Source=..\Data\Membership.db`
* 编译项目，项目上点击鼠标右键，`查看`，`在浏览器中查看`，运行魔方平台
* 系统为`MySql`/`SQLite`/`Oracle`/`SqlCe`数据库自动下载匹配（`x86/x64`）的数据库驱动文件，驱动下载地址可在`Config\Core.config`中修改`PluginServer`
* 系统自动下载脚本样式表等资源文件，下载地址可在`Config/Cube.config`中修改`PluginServer`
* 默认登录用户名是`admin`，密码是`admin`，也可以使用`NewLife`等第三方OAuth登录，首个进入系统的用户抢得管理员，原`admin`禁用
* 项目发布时只需要拷贝`*.dll`、`appsettings.json`、`*.deps.json`、`*.runtimeconfig.json`，以及其它自己添加的资源文件

### ASP.NET MVC 安装

* 在 *Visual Studio* 中新建`ASP.NET MVC`项目
* 通过 *NuGet* 引用`NewLife.Cube`，或自己编译最新的[魔方 NewLife.Cube](http://github.com/NewLifeX/NewLife.Cube)源码
* 在`Web.config`的`<connectionStrings>`段设置名为`Membership`的连接字符串，用户角色权限菜单等存储在该数据库
* 系统自动识别数据库类型，默认`<add name="Membership" connectionString="Data Source=..\Data\Membership.db" providerName="Sqlite"/>`
* 编译项目，项目上点击鼠标右键，`查看`，`在浏览器中查看`，运行魔方平台
* 系统为`MySql`/`SQLite`/`Oracle`/`SqlCe`数据库自动下载匹配（`x86/x64`）的数据库驱动文件，驱动下载地址可在`Config\Core.config`中修改`PluginServer`
* 系统自动下载脚本样式表等资源文件，下载地址可在`Config/Cube.config`中修改`PluginServer`
* 默认登录用户名是`admin`，密码是`admin`，也可以使用`NewLife`等第三方OAuth登录，首个进入系统的用户抢得管理员，原`admin`禁用
* 推荐安装 *Visual Studio* 插件 *Razor Generator*，给`.cshtml`文件设置`自定义工具`为`RazorGenerator`，可以把`.cshtml`编译生成到`DLL`里面
* 项目发布时只需要拷贝`Bin`、`web.config`、`Global.asax`，以及其它自己添加的资源文件

## 主要功能

<details>
<summary><b>数据到界面生产力</b>（9 项）</summary>

| 功能 | 关键特性 |
| --- | --- |
| EntityController 脚手架 | 实体类 + 控制器 = CRUD / 统计 / 导出 / 权限 即刻可用；只读列表用 `ReadOnlyEntityController`。 |
| 静态构造定制 | 通过控制器静态构造器配置字段、操作按钮、关联跳转、统计、导出。 |
| 字段元数据接口 | `GetFields?kind=1..4`（列表 / 详情 / 新增 / 编辑）驱动动态前端生成，无需硬编码。 |
| 菜单树 | `/Cube/MenuTree` 按权限生成多级菜单；传统 MVC 界面使用 `/Admin/Index/GetMenuTree`。 |
| 统一响应 | `ApiResponse` 统一包装 code / data / page / stat。 |
| 免部署 | 首次运行自动创建数据库表结构与初始化数据，无需人工干涉。 |
| 强大视图引擎 | 子项目同路径同名 `.cshtml` 即可覆盖父项目视图，任意定制默认界面。 |
| 内置编辑器 | 富文本（Quill）与 Markdown（EasyMDE）双编辑器，支持图片上传与安全控制。 |
| Scalar 接口文档 | 集成 Scalar 在线文档，配合 Swagger 按区域自动分组，接口在线调试更便捷。 |

</details>

<details>
<summary><b>字段与界面定制</b>（3 项）</summary>

| 功能 | 关键特性 |
| --- | --- |
| 字段集合定制 | `ListFields.RemoveField` / `AddListField` 按控制器调整列、链接与打开方式。 |
| 修改日志 | `LogOnChange = true` 为每次修改写入审计记录。 |
| 前端动态字段 | 前端调用 `/Cube/App/GetFields?kind=1` 获取列表字段定义，无需硬编码。 |

```csharp
public class AppController : EntityController<App, AppModel>
{
    static AppController()
    {
        LogOnChange = true;                 // 修改日志
        ListFields.RemoveField("Secret");   // 隐藏敏感列
        var log = ListFields.AddListField("Log", "UpdateUserId");
        log.DisplayName = "修改日志";
        log.Url = "/Admin/Log?category=应用系统&linkId={ID}";
        log.Target = "_blank";
    }
}
```

</details>

<details>
<summary><b>API 与前后端分离</b>（6 项）</summary>

| 接口 | 用途 |
| --- | --- |
| `/Auth/Login` | 登录并获取 JWT Token（Header / Cookie / Query）。 |
| `/Cube/MenuTree` | 按权限生成菜单树。 |
| `/{Area}/{Controller}/GetFields?kind=1..4` | 列表 / 详情 / 新增 / 编辑表单的字段元数据。 |
| 主路由 + 动态参数 | 支持分页、排序与条件拼装查询。 |
| `Detail` / `Insert` / `Update` / `Delete` | 标准增删改查动作。 |
| `stat` / `page` | 统计行与分页信息。 |

</details>

<details>
<summary><b>权限与安全</b>（7 项）</summary>

| 功能 | 关键特性 |
| --- | --- |
| 通用权限管理 | 用户、角色、菜单、权限，支持控制器 Action 粒度控制。 |
| EntityAuthorize | 每个动作执行登录 + 角色 + 菜单 Action 校验。 |
| 首个用户即管理员 | 首个进入系统的用户抢得管理员，原 `admin` 禁用；首次可用 `admin/admin` 登录。 |
| 统一身份 | 支持外部 OAuth / SSO / JWT，多端统一身份。 |
| 安全防御 | 内置威胁检测（扫描器 / 注入 / 蜜罐）、持久化自动封禁（阶梯时长）与安全事件审计。默认自动模式：连续攻击自动封禁、大范围攻击临时拦截并自动回落；可配置或自动学习可信代理防伪造；可一键切换观察 / 拦截模式。 |
| 生产建议 | 关闭未用第三方登录、启用 HTTPS、加强密码策略。 |
| 多租户开关 | 多租户能力可一键开关，关闭后隐藏租户菜单与字段，支持运行时租户切换。 |

</details>

<details>
<summary><b>AI 能力</b>（4 项）</summary>

| 功能 | 关键特性 |
| --- | --- |
| AI 对话助手 | 右下角对话面板统一入口，上下文感知（当前页面 / 实体 / 查询）。全局 `AiController.AiChat` 端点（`/Ai/AiChat`）+ NAI `AiChatService` 编排（会话历史 `ChatSessionService` 保持 / 工具循环 / SSE 事件流）。 |
| 扩展点 | 实体控制器经 `IEntityAiContext` 能力接口向全局端点暴露数据查询 / 工具集 / 提示词重载点（`SearchData` / `CreateCubeTools` / `BuildChatSystemPrompt`）。工具集含 `get_data_context`、`get_form_schema`、`fill_form`、`get_page_context`、`get_system_info` 与 `run_js`。 |
| AI 洞察 | 原 `AiInsight` 独立端点已合并到对话助手，数据收集保留 `AiInsightHelper`（14 类敏感字段黑名单）。一键 AI 分析提供自动统计、异常检测、趋势预判与可执行建议，支持深度推理，MVC / Vue 双端适配。 |
| 工作台 Widget | 工作台 / 首页由可配置部件（KPI、图表、监控）组成，支持个性化布局与 `/Cube/Widget` 部件管理。 |

</details>

<details>
<summary><b>前端、主题与皮肤</b>（7 项）</summary>

| 功能 | 关键特性 |
| --- | --- |
| 多前端共存 | Vue / Blazor / AntDesign / 传统 Razor 可渐进迁移。 |
| 主题内嵌资源 | `CubeEmbeddedFileProvider` 把主题静态资源打包进程序集。 |
| 皮肤注册 | `AddTheme/AddSkin` 注册皮肤，Use 阶段任选其一（`app.UseMetronic(env)` 等）。 |
| 自定义主题 | 仿照 `MetronicModule` 实现 IModule 并在 Use 阶段挂载。 |
| 视图覆写 | 子项目同路径同名 `.cshtml` 覆盖父级。 |
| 多语言 | 11 种运行时语言用于接口消息，按请求识别区域，并提供 11 语言 Vue 前端。 |
| 新一代皮肤包 | 基于 pnpm workspace + 公共模块 `@newlifex/api-core`（统一 API / Token / 类型）和 `@newlifex/field-mapping`（字段元数据 → 控件映射），8 套皮肤开箱即用，NuGet 引包即得完整前端。 |

| NuGet 包 | 框架 | UI 库 | 开发端口 |
|---------|------|-------|---------|
| `NewLife.Cube.NaiveUI` | Vue 3 | Naive UI | 5180 |
| `NewLife.Cube.MUI` | React 19 | MUI 6 (Material) | 5181 |
| `NewLife.Cube.Shadcn` | React 19 | Radix UI + Tailwind CSS 4 | 5182 |
| `NewLife.Cube.ArcoVue` | Vue 3 | Arco Design Vue (字节) | 5183 |
| `NewLife.Cube.Angular` | Angular 19 | NG-ZORRO (Ant Design) | 5184 |
| `NewLife.Cube.Vuetify` | Vue 3 | Vuetify 3 (Material) | 5185 |
| `NewLife.Cube.Svelte` | SvelteKit 2 + Svelte 5 | Tailwind CSS 4 | 5186 |
| `NewLife.Cube.TDesign` | Vue 3 | TDesign (腾讯) | 5187 |

```csharp
// 使用示例（任选其一）
app.UseNaiveUI(app.Environment);
app.UseMUI(app.Environment);
app.UseShadcn(app.Environment);
app.UseArcoVue(app.Environment);
app.UseAngular(app.Environment);
app.UseVuetify(app.Environment);
app.UseSvelte(app.Environment);
app.UseTDesign(app.Environment);
```

前端 workspace 构建约定：

- 仓库根目录 `pnpm build`：仅构建 `packages/` 下公共模块，不再默认递归编译所有皮肤。
- 仓库根目录单皮肤构建：使用 `pnpm build:vue`、`pnpm build:react`、`pnpm build:naive`、`pnpm build:mui`、`pnpm build:shadcn`、`pnpm build:arco`、`pnpm build:angular`、`pnpm build:vuetify`、`pnpm build:svelte`、`pnpm build:tdesign`。
- 仓库根目录单皮肤开发：使用对应的 `pnpm dev:xxx`。
- 需要一次性构建或启动全部前端时，显式使用 `pnpm build:all` 或 `pnpm dev:all`。
- 若已进入具体皮肤目录 `web/`，继续使用该目录下的 `pnpm build` / `pnpm dev` 即可，互不干扰。

</details>

<details>
<summary><b>多数据库与大数据</b>（5 项）</summary>

| 功能 | 关键特性 |
| --- | --- |
| XCode 数据层 | 实体同步建表、字段演进、自动分表、读写分离、二级缓存。 |
| 多数据库 | MySql / SQLite / SqlServer / Oracle / PostgreSql / SqlCe / Access / 达梦。 |
| 三库分离 | 示例：Membership（权限）/ Cube（业务）/ Log（日志）。 |
| PluginServer | 首次运行自动下载数据库驱动与前端资源，可自定义地址。 |
| 规模验证 | 真实项目单表 100 亿数据增删改查。 |

</details>

<details>
<summary><b>性能与 NativeAOT</b>（4 项）</summary>

| 功能 | 关键特性 |
| --- | --- |
| 统一耗时记录 | 过滤器统一记录耗时，易接入 Stardust APM。 |
| 低成本日志 | 日志判级避免无谓字符串拼接。 |
| 多 TFM 优化 | 多目标框架允许在高版本启用 Span / ValueTask 优化。 |
| NativeAOT | 非 MVC 底座 18 个冒烟场景，CI 中按 JIT / AOT 双跑。 |

</details>

<details>
<summary><b>部署与运维</b>（5 项）</summary>

| 场景 | 做法 |
| --- | --- |
| 单机 | `dotnet publish` 后拷贝 dll + 配置即可。 |
| 服务 | 结合 `NewLife.Agent` 安装为 Windows Service 或 systemd。 |
| 集群 | 发布后打 zip 包，通过星尘代理 `StarAgent` 拉起守护，或通过星尘平台发布。 |
| 容器 | 简单 Dockerfile（复制发布目录，设置环境变量）。 |
| 横向扩展 | 无状态多实例，共享数据库 & 缓存。 |

#### 快速部署用户中心

1. 拉取[源码](https://github.com/NewLifeX/NewLife.Cube)并编译CubeSSO项目，切换到输出目录Bin/SSO，按需修改appsettings.json配置，打包exe/dll/appsettings.json/CubeSSO.runtimeconfig.json等文件，并发布到服务器。
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://*:8080;https://*:8081",
  //"StarServer": "http://star.newlifex.com:6600",
  "ConnectionStrings": {
    "Membership": "Data Source=..\\Data\\Membership.db;provider=sqlite",
    "Cube": "Data Source=..\\Data\\Cube.db;provider=sqlite",
    "Log": "Data Source=..\\Data\\Log.db;provider=sqlite"

    //"Membership": "Server=.;Port=3306;Database=Membership;Uid=root;Pwd=root;provider=mysql",
    //"Cube": "Server=.;Port=3306;Database=Membership;Uid=root;Pwd=root;provider=mysql",
    //"Log": "Server=.;Port=3306;Database=Membership;Uid=root;Pwd=root;provider=mysql"
  }
}
```
2. 启动并访问SSO系统，首次登录可以用admin/admin进去，也可以使用第三方登录（新生命用户中心），第一个用户将会作为系统管理员，同时禁用admin。
3. 作为用户中心正式部署时，需要关闭SSO的第三方登录，魔方设置->OAuth设置->禁用所有。
   ![OAuthConfig](Doc/OAuthConfig.png)
4. 修改系统名称为新生命用户中心，魔方设置->系统设置->显示名称。

</details>

<details>
<summary><b>第三代魔方</b>（5 项）</summary>

| 事项 | 说明 |
| --- | --- |
| 聚焦 | 前后端分离 + 多前端生态 + 最小侵入。 |
| 后端 | 接口合并在 master（`NewLife.Cube`）；MVC 保留在 `NewLife.CubeNC`。 |
| 前端 | 仓库独立（Vue / QuickVue / Antd / Blazor）。 |
| 目标蓝图 | 现代化 UI + 保留覆写机制；多框架前端（Vue/React/Angular/Svelte + 8 套开箱即用皮肤包）；NuGet 引包即得默认皮肤；移动端 & 小程序 & 混合应用；数据大屏增强。 |
| 待办任务 | <https://github.com/orgs/NewLifeX/projects/1> |

前端仓库：  
https://github.com/NewLifeX/NewLife.CubeVue · https://github.com/NewLifeX/NewLife.QuickVue · https://github.com/NewLifeX/NewLife.CubeAntd · https://github.com/NewLifeX/NewLife.CubeBlazor

在线站点：Vue <https://vue.newlifex.com>、<https://quickvue.newlifex.com> · Antd <https://antd.newlifex.com> · Swagger <https://cube3.newlifex.com/swagger/index.html>

项目参与须知：

1. 加入 NewLifeX 团队，向 dev 分支提交。
2. WebApi 后台：`NewLife.Cube`；MVC 保留在 `NewLife.CubeNC`。
3. 新前端独立仓库（如 `NewLife.CubeAntd`）。
4. 文档/代码标注负责人。
5. 待办任务：https://github.com/orgs/NewLifeX/projects/1 。

春雨计划（里程碑摘录）：

- [*] 新建 WebApi 项目 `NewLife.Cube`
- [*] 迁移 NetCore 专属代码到 `NewLife.CubeNC`
- [*] 新版 `EntityController` / `EntityReadonlyController`
- [*] 接口文档 & Vue 登录主页
- [] Vue 用户/角色列表与表单

</details>

<details>
<summary><b>总体架构与子工程</b>（9 项）</summary>

```
+-------------------------------------------------------------+
|         前端 (Vue / Blazor / Antd / 传统 Razor UI)           |
+----------------------+------------------+-------------------+
|  皮肤视图(Razor)     |  API Controllers | Swagger 分组文档  |
+----------------------+------------------+-------------------+
|  EntityController / ReadOnlyEntityController 基础能力        |
+------------------+---------------------+--------------------+
| 权限(EntityAuthorize) | 过滤(ApiFilter) | 统一响应(ApiResponse)|
+-------------------------------------------------------------+
|            实体 & 业务逻辑 (XCode ORM + 扩展)                |
+-------------------------------------------------------------+
| 数据访问：MySql/SqlServer/SQLite/Oracle/PostgreSQL/达梦等    |
+-------------------------------------------------------------+
| NewLife.Core(日志/配置/缓存/序列化/追踪) + 生态组件          |
+-------------------------------------------------------------+
```

| 子工程 | 职责 |
| --- | --- |
| NewLife.Cube | 核心（实体控制器、权限、菜单、字段元数据、API）。 |
| NewLife.CubeNC | 旧版 MVC / Razor 兼容层。 |
| 经典皮肤 | AdminLTE / Metronic / Metronic8 / Tabler / LayuiAdmin / ElementUI / Blazor（各独立包）。 |
| 新一代皮肤 | pnpm workspace：NaiveUI / MUI / Shadcn / ArcoVue / Angular / Vuetify / Svelte / TDesign。 |
| NewLife.Cube.Swagger | 按区域自动分组的 Swagger 文档。 |
| CubeDemo / CubeDemoNC | 示例。 |
| CubeSSO | 统一用户中心 & OAuth 服务端。 |
| NewLife.Cube.Blazor | Blazor 前端实验。 |
| Test / XUnitTest / E2EMvcTest / AotSmoke / WebSmoke | 测试与验证（WebSmoke 在非 x64/arm64 指令集上启动 Kestrel 并完成一次 HTTP 往返）。 |

</details>

<details>
<summary><b>核心概念</b>（7 项）</summary>

| 概念 | 说明 |
| --- | --- |
| EntityController | 通过静态构造器配置字段、操作按钮、关联跳转、统计、导出。 |
| 字段元数据接口 | `GetFields?kind=List\|Detail\|AddForm\|EditForm` 驱动动态前端生成。 |
| 菜单树 | `/Admin/Index/GetMenuTree` 根据权限生成多级菜单。 |
| 权限模型 | 用户 -> 角色 -> 菜单（Action 集合）。首个用户抢管理员，`admin` 被禁用。 |
| ApiResponse | 标准响应包装 code / data / page / stat。 |
| IModule | 启动模块注入服务和静态资源（主题、Swagger 等）。 |
| AI 对话助手 | 全局 `AiController.AiChat` 端点（`/Ai/AiChat`）+ NAI `AiChatService` 编排（会话历史 / 工具循环 / SSE 事件流）；实体控制器经 `IEntityAiContext` 暴露能力。`AiInsightHelper` 保留数据收集（14 类敏感字段黑名单）。 |

</details>

<details>
<summary><b>常见使用场景</b>（5 项）</summary>

| 场景 | 典型用法 |
| --- | --- |
| 企业与部门管理 | 内部管理后台、SaaS 管理面。 |
| IoT 运营 | 设备 / 点位 / 日志 / 告警管理与数据导出。 |
| 数据中台 | 快速发布标准查询接口。 |
| 身份中心 | 统一身份认证与授权（SSO / OAuth）。 |
| 运营支撑 | 订单、配置、任务调度、版本发布。 |

</details>

## 生态全景

NewLife 生态按依赖分层，各项目独立版本、独立发版。下面的分层图在所有 NewLife 仓库中逐字一致；项目明细额外给出每个项目的定位与历史。

| 层 | 项目 |
| --- | --- |
| 基础底座 | Core · XCode · Net · Remoting · Agent · Templates |
| 数据存储与消息 | Redis · RocketMQ · MQTT · NovaDb · MySql |
| Web、AI 与分布式 | Cube · Stardust · AntJob · NewLife.AI |
| 工业与物联网 | IoT · Modbus · Siemens · Omron · Melsec · OPC · BACnet · ZeroIoT |
| 车联网 | JT808 · JT809 · JT1078 |
| 领域组件 | Office · Audio · Video · Map · IP |
| 工具与产品 | CrazyCoder · GitCandy · LuckyClover |

**完整生态** → https://github.com/NewLifeX/X#生态全景

<details>
<summary><b>项目明细</b>（30 项）</summary>

各项目默认支持net9.0/netstandard2.1/netstandard2.0/net4.62/net4.5，旧版（2024.0801）支持net4.0/net2.0

|                               项目                               | 年份  | 说明                                                                                        |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
|                             基础组件                             |       | 支撑其它中间件以及产品项目                                                                  |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 核心库，日志、配置、缓存、网络、序列化、APM性能追踪                                         |
|    [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode)    | 2005  | 大数据中间件，单表百亿级，MySql/SQLite/SqlServer/Oracle/PostgreSql/达梦，自动分表，读写分离 |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 网络库，单机千万级吞吐率（2266万tps），单机百万级连接（400万Tcp长连接）                     |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011  | 协议通信库，提供CS应用通信框架，支持Http/RPC通信框架，高吞吐，物联网设备低开销易接入        |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 魔方快速开发平台，集成了用户权限、SSO登录、OAuth服务端等，单表100亿级项目验证               |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 服务管理组件，把应用安装成为操作系统守护进程，Windows服务、Linux的Systemd                   |
|     [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero)     | 2020  | Zero零代脚手架，基于NewLife组件生态的项目模板NewLife.Templates，Web、WebApi、Service        |
|                              中间件                              |       | 对接知名中间件平台                                                                          |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | Redis客户端，微秒级延迟，百万级吞吐，丰富的消息队列，百亿级数据量项目验证                   |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | RocketMQ纯托管客户端，支持Apache RocketMQ和阿里云消息队列，十亿级项目验                     |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 物联网消息协议，MqttClient/MqttServer，客户端支持阿里云物联网                               |
|      [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT)      | 2022  | IoT标准库，定义物联网领域的各种通信协议标准规范                                             |
|   [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus)   | 2022  | ModbusTcp/ModbusRTU/ModbusASCII，基于IoT标准库实现，支持ZeroIoT平台和IoTEdge网关            |
|  [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens)  | 2022  | 西门子PLC协议，基于IoT标准库实现，支持IoT平台和IoTEdge                                      |
|      [NewLife.Map](https://github.com/NewLifeX/NewLife.Map)      | 2022  | 地图组件库，封装百度地图、高德地图、腾讯地图、天地图                                        |
|    [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio)    | 2023  | 音频编解码库，PCM/ADPCMA/G711A/G722U/WAV/AAC                                                |
|                             产品平台                             |       | 产品平台级，编译部署即用，个性化自定义                                                      |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 星尘，分布式服务平台，节点管理、APM监控中心、配置中心、注册中心、发布中心                   |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 蚂蚁调度，分布式大数据计算平台（实时/离线），蚂蚁搬家分片思想，万亿级数据量项目验证         |
|      [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP)      | 2021  | 企业ERP，产品管理、客户管理、销售管理、供应商管理                                           |
|         [CrazyCoder](https://github.com/NewLifeX/XCoder)         | 2006  | 码神工具，众多开发者工具，网络、串口、加解密、正则表达式、Modbus、MQTT                      |
|           [EasyIO](https://github.com/NewLifeX/EasyIO)           | 2023  | 简易文件存储，支持分布式系统中文件集中存储。                                                |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 产品级反向代理，NAT代理、Http代理                                                           |
|        [HttpMeter](https://github.com/NewLifeX/HttpMeter)        | 2022  | Http压力测试工具                                                                            |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | Git源代码管理系统                                                                           |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 嵌入式操作系统，完全独立自主，支持ARM Cortex-M芯片架构                                      |
|          [SmartA2](https://github.com/NewLifeX/SmartA2)          | 2019  | 嵌入式工业计算机，物联网边缘网关，高性能.NET8主机，应用于工业、农业、交通、医疗             |
|                          FIoT物联网平台                          | 2020  | 物联网整体解决方案，建筑、环保、农业，软硬件及大数据分析一体化，单机十万级点位项目验证      |
|                        UWB高精度室内定位                         | 2020  | 厘米级（10~20cm）高精度室内定位，软硬件一体化，与其它系统联动，大型展厅项目验证             |

</details>

## 文档与示例

### 指南

详细需求文档、功能清单、架构设计见 [`Doc/`](Doc/) 目录（中文）：

- 📋 [需求文档](Doc/需求文档.md) — 愿景、核心目标、功能需求
- ✅ [功能清单](Doc/功能清单.md) — 实现/测试/注释三维状态追踪
- 🏗️ [架构设计](Doc/架构设计.md) — 分层架构、模块设计、关键决策
- 🤖 [AI 人工智能](Doc/AI-人工智能.md) — AI 能力清单、配置与扩展点
- 🔔 [在线告警与站内信](Doc/ALM-在线告警与站内信.md) — 在线新高告警、系统级广播站内信与未读闭环
- ✉️ [邮箱/手机验证](Doc/AUTH-邮箱手机验证.md) — 账号激活契约（注册→激活→登录），前端皮肤接入规范
- 🛡️ [安全防御](Doc/PERM-安全防御.md) — 内置威胁检测、自动封禁与安全事件审计
- 📚 [魔方用户手册](Doc/魔方用户手册.md) — 完整的用户与开发手册

### 教程与站点

- 魔方教程：<https://newlifex.com/cube>
- XCode 教程：<https://newlifex.com/xcode>
- 核心库教程：<https://newlifex.com/core>
- 演示站点：<https://cube.newlifex.com> · SSO 中心：<https://sso.newlifex.com> · Swagger：<https://cube3.newlifex.com>

### FAQ 速览

Q: 隐藏字段?  
A: 控制器静态构造：`ListFields.RemoveField("字段")`。

Q: 自定义统计?  
A: 重写搜索逻辑，填充返回集合的 `stat`。

Q: 切换主题?  
A: 引入主题包并 `app.UseXXX(env)`，后台可配置默认。

Q: 数据库驱动来源?  
A: 首次运行自动从 `PluginServer` 下载，可自定义。

Q: 集成自定义前端?  
A: 依靠字段元数据 + 标准 CRUD/JWT 登录。

## 参与贡献

### 构建与测试

魔方使用 .NET SDK 10（或目标框架对应的 SDK）即可编译，也可用 Visual Studio 打开；前端皮肤使用 pnpm。

```bash
dotnet build NewLife.Cube/NewLife.Cube.csproj -c Release
dotnet test XUnitTest -c Release
```

- 端到端 MVC 测试：`E2EMvcTest/`
- NativeAOT 冒烟：`AotSmoke/`，CI 见 `.github/workflows/aot.yml`
- Web 冒烟：`WebSmoke/` 启动 Kestrel 并完成一次 HTTP 往返

### 发版

推送 `v` 前缀标签触发 `.github/workflows/publish.yml`，工作流打包 `NewLife.Cube` / `NewLife.Cube.Core` 并发布到 NuGet.org。包内 README 由 `sync-readme-package.ps1` 从 `README.md` 生成，语言切换行由 `sync-readme-languages.ps1` 生成，指标徽章由 `sync-readme-metrics.ps1` 生成；三者均支持 `-Check` 供 CI 校验。

### 反馈

- 规范：参考 `.github/copilot-instructions.md`。
- Issue / PR：描述场景、期望、复现；性能类附数据规模。
- 新主题：独立仓库 + README 说明构建方式。
- 优先复用 NewLife 生态，避免不必要依赖。
- 多 TFM 编译通过后再提交。

## 新生命开发团队

![XCode](https://newlifex.com/logo.png)

新生命团队（NewLife）成立于2002年，是新时代物联网行业解决方案提供者，致力于提供软硬件应用方案咨询、系统架构规划与开发服务。  
团队主导的80多个开源项目已被广泛应用于各行业，Nuget累计下载量高达400余万次。  
团队开发的大数据中间件NewLife.XCode、蚂蚁调度计算平台AntJob、星尘分布式平台Stardust、缓存队列组件NewLife.Redis以及物联网平台FIoT，均成功应用于电力、高校、互联网、电信、交通、物流、工控、医疗、文博等行业，为客户提供了大量先进、可靠、安全、高质量、易扩展的产品和系统集成服务。

我们将不断通过服务的持续改进，成为客户长期信赖的合作伙伴，通过不断的创新和发展，成为国内优秀的IoT服务供应商。

`新生命团队始于2002年，部分开源项目具有20年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：https://newlifex.com  
开源：https://github.com/newlifex  
QQ群：1600800/1600838  
微信公众号：  
![智能大石头](https://newlifex.com/Stone.jpg)

## 许可证

[MIT](https://github.com/NewLifeX/NewLife.Cube/blob/master/LICENSE) 协议；强名称证书 `newlife.snk` 已公开，可自行编译替换程序集，源码可修改与再分发且无需署名。

> 如果这个项目对你有帮助，欢迎 Star、分享给团队，或通过 Issue/PR 提出改进建议。
