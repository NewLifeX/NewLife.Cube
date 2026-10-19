<!-- generated from README.md by sync-readme-package.ps1; do not edit -->

# NewLife.Cube - Web rapid development and admin framework

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
[![AOT smoke](https://img.shields.io/badge/AOT%20smoke-18%2F18-brightgreen?logo=dotnet)](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/AOT兼容性分析.md)
[![TFM](https://img.shields.io/badge/TFM-5%20%28net6.0%20%E2%86%92%20net10.0%29-512BD4?logo=dotnet)](https://www.nuget.org/packages/NewLife.Cube)
[![locales](https://img.shields.io/badge/locales-31-2ea44f?logo=dotnet)](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/多语言本地化.md)
<!-- metrics:end -->
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20arm64-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Cube?logo=github)](https://github.com/NewLifeX/NewLife.Cube/graphs/contributors)

<!-- lang:start -->
🌐 [简体中文](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.zh-CN.md) | [繁體中文](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.zh-TW.md) | [Español](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.es.md) | [Deutsch](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.de.md) | [日本語](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.ja.md) | [Français](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.fr.md) | [Português](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.pt.md) | [Русский](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.ru.md) | [한국어](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.ko.md) | [العربية](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.ar.md)
<!-- lang:end -->

NewLife.Cube is a rapid web development platform for "data-driven + fast iteration + deep customization" scenarios. In minutes, an entity class or database table becomes a complete admin UI with permissions, lists, details, CRUD, export and statistics, while standard APIs are exposed for Vue / Blazor / Antd / mobile or third-party systems. The default template has been proven in real projects with a single table of 10 billion rows.

Demo: <https://cube.newlifex.com> `CentOS7 + CDN`  
SSO center: <https://sso.newlifex.com> `OAuth server`  
Swagger: <https://cube3.newlifex.com>

Cube tutorial: <https://newlifex.com/cube>  
XCode tutorial: <https://newlifex.com/xcode>  
Core library tutorial: <https://newlifex.com/core>

> **Documentation index** (Chinese): requirements, feature list and architecture live in [`Doc/`](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/):
> - 📋 [Requirements](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/需求文档.md) — vision, goals, functional requirements
> - ✅ [Feature list](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/功能清单.md) — implementation/test/comment tracking
> - 🏗️ [Architecture](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/架构设计.md) — layering, modules, key decisions
> - 🤖 [AI](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/AI-人工智能.md) — AI capabilities, configuration and extension points
> - 🔔 [Alerts and inbox](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/ALM-在线告警与站内信.md) — online threshold alerts and broadcast messages
> - ✉️ [Email/phone verification](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/AUTH-邮箱手机验证.md) — activation contract and UI integration
> - 🛡️ [Security](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/PERM-安全防御.md) — threat detection, auto-ban and security audit
> - 📚 [User manual](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/魔方用户手册.md) — full user and developer guide

---
## Contents
- [Core value](#core-value)
- [Architecture](#architecture)
- [Projects](#projects)
- [Core concepts](#core-concepts)
- [Quick start](#quick-start)
- [Minimal example](#minimal-example-net-89-minimal-hosting)
- [Field and UI customization](#field-and-ui-customization)
- [API and front-end separation](#api-and-front-end-separation)
- [Permissions and security](#permissions-and-security)
- [Themes / skins / UI extensions](#themes--skins--ui-extensions)
- [Multi-database and big data](#multi-database-and-big-data)
- [Performance](#performance)
- [Deployment](#deployment)
- [Deploying the user center](#deploying-the-user-center)
  - [Deploying the user center](#deploying-the-user-center-1)
- [Cube generation 3](#cube-generation-3)
- [Common scenarios](#common-scenarios)
- [Contributing](#contributing)
- [FAQ](#faq)
- [Features](#features)
- [ASP.NET Core installation](#aspnet-core-installation)
- [ASP.NET MVC installation](#aspnet-mvc-installation)
- [NewLife project matrix](#newlife-project-matrix)
- [NewLife team](#newlife-team)

---
## Core value
1. Extreme "data-to-UI" productivity: entity class + a controller gives you CRUD / statistics / export / permissions immediately.
2. Full-dimensional permissions: user / role / menu / action granularity, with SSO + OAuth + JWT.
3. Multiple front-ends: Vue / Blazor / AntDesign / classic Razor can coexist and migrate gradually.
4. Multi-database + automatic schema creation + automatic sharding + read/write splitting (via XCode) for very large data.
5. Extensible: field metadata, view overrides, theme plugins and IModule modularity.
6. **AI insight**: one-click AI analysis on any data page — statistics, anomaly detection, trend prediction and actionable advice, with SSE streaming and deep reasoning. Works on MVC and Vue.
7. **AI assistant**: a single chat panel in the lower-right corner with page/entity/query context. Built on NewLife.AI tools, it performs data analysis, metadata-driven form filling, record analysis and system diagnostics.
8. **Dashboard widgets**: dashboards are composed of configurable widgets (KPI, charts, monitoring) with personalized layouts and `/Cube/Widget` management.
9. **Export enhancements**: xlsx full export, Zip packaging, CSV headers and numeric enums, with SYLK protection.
10. **SVG text avatars**: when a user has no avatar, an SVG text avatar is generated (first N Chinese characters + gradient).
11. **Scalar API docs**: integrated Scalar UI grouped by area alongside Swagger, for easier online debugging.
12. **Multi-tenancy switch**: enable or disable multi-tenancy with one switch; hidden tenant menus/fields and runtime tenant switching are supported.
13. Long-term evolution and multi-target frameworks: easy to upgrade, stable and controllable.

---
## Architecture
```
+-------------------------------------------------------------+
|         Front-end (Vue / Blazor / Antd / classic Razor)      |
+----------------------+------------------+-------------------+
|  Skin views (Razor)  |  API Controllers | Swagger by area   |
+----------------------+------------------+-------------------+
|  EntityController / ReadOnlyEntityController core            |
+------------------+---------------------+--------------------+
| Permissions (EntityAuthorize) | Filters (ApiFilter) | ApiResponse |
+-------------------------------------------------------------+
|            Entities & business logic (XCode ORM + extensions)|
+-------------------------------------------------------------+
| Data access: MySql/SqlServer/SQLite/Oracle/PostgreSQL/DM ... |
+-------------------------------------------------------------+
| NewLife.Core (logging/config/cache/serialization/tracing)     |
+-------------------------------------------------------------+
```

---
## Projects
- NewLife.Cube: core (entity controllers, permissions, menus, field metadata, API).
- NewLife.CubeNC: legacy MVC / Razor compatibility layer.
- Skin themes: AdminLTE / Metronic / Metronic8 / Tabler / LayuiAdmin / ElementUI / Blazor (separate packages).
- New-generation skins (pnpm workspace): NaiveUI / MUI / Shadcn / ArcoVue / Angular / Vuetify / Svelte / TDesign.
- NewLife.Cube.Swagger: Swagger documents grouped by area.
- CubeDemo / CubeDemoNC: samples.
- CubeSSO: unified user center and OAuth server.
- NewLife.Cube.Blazor: Blazor front-end experiment.
- Test / XUnitTest / E2EMvcTest / AotSmoke: tests and verification.

---
## Core concepts
- EntityController: configure fields, action buttons, related links, statistics and export in a static constructor.
- Field metadata API: `GetFields?kind=List|Detail|AddForm|EditForm` drives dynamic front-end generation.
- Menu tree: `/Admin/Index/GetMenuTree` builds multi-level menus from permissions.
- Permission model: user -> role -> menu (action set). The first registered user becomes administrator and `admin` is disabled.
- ApiResponse: standard response wrapper with code / data / page / stat.
- IModule: startup module that injects services and static assets (themes, Swagger, etc.).
- **AI assistant**: global `AiController.AiChat` endpoint (`/Ai/AiChat`) plus NAI `AiChatService` orchestration (session history, tool loop, SSE events). Entity controllers expose data queries, tool sets and prompt overrides through `IEntityAiContext` (`SearchData` / `CreateCubeTools` / `BuildChatSystemPrompt`). Tools include `get_data_context`, `get_form_schema`, `fill_form`, `get_page_context`, `get_system_info` and `run_js`.
- **AI insight** has been merged into the assistant; `AiInsightHelper` remains for data collection (14 sensitive-field blacklists).

---
## Quick start
Install `NewLife.Cube.Core` from NuGet. MIT licensed; you may replace the "NewLife" string for private builds. Works with Visual Studio and the CLI. `newlife.snk` is public for strong-name consistency.

CLI:
```
dotnet new install NewLife.Templates
dotnet new cube --name CubeWeb
dotnet new xcode --name Zero.Data
cd CubeWeb
dotnet build
start http://localhost:6080
dotnet run
```

---
## Minimal example (.NET 8/9 Minimal Hosting)
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCube(); // registers permissions, menus, entity controller scanning
var app = builder.Build();
app.UseMetronic(app.Environment); // or UseAdminLTE / UseTabler ...
app.UseCube(app.Environment);      // routing, static assets and middleware
app.Run();
```
Open `/` to sign in, use Swagger for APIs, and call the standard CRUD APIs from any front-end.

---
## Field and UI customization
```csharp
public class AppController : EntityController<App, AppModel>
{
    static AppController()
    {
        LogOnChange = true;                 // change log
        ListFields.RemoveField("Secret");   // hide a sensitive column
        var log = ListFields.AddListField("Log", "UpdateUserId");
        log.DisplayName = "Change log";
        log.Url = "/Admin/Log?category=App&linkId={ID}";
        log.Target = "_blank";
    }
}
```
The front-end fetches `/Cube/App/GetFields?kind=1` for list field definitions — no hard coding.

---
## API and front-end separation
- Login: `/Auth/Login` -> JWT token (header / cookie / query).
- Menus: `/Cube/MenuTree`.
- Fields: `/{Area}/{Controller}/GetFields?kind=1..4`.
- Query: main route + dynamic parameters (paging, sorting, conditions).
- CRUD: `Detail` / `Insert` / `Update` / `Delete`.
- Statistics: `stat`; paging: `page`.

---
## Permissions and security
- EntityAuthorize: login + role + menu action checks.
- The first user becomes administrator and the built-in `admin` is disabled.
- External OAuth / SSO / JWT are supported for unified identity across clients.
- Production advice: disable unused third-party logins, enable HTTPS and enforce a strong password policy.
- Security defense: built-in threat detection (scanners/injection/honeypots), persistent auto-ban with escalating durations and security audit. Automatic mode bans repeated attackers, temporarily blocks broad attacks and recovers automatically; trusted proxies can be configured or learned to prevent spoofing; observation/blocking modes are switchable.

---
## Themes / skins / UI extensions
- Embedded theme assets: `CubeEmbeddedFileProvider`.
- Register skins via `AddTheme/AddSkin`.
- Custom themes: implement IModule like `MetronicModule` and mount it in the Use phase.
- View overrides: a `.cshtml` with the same path in a child project overrides the parent.

### New-generation skin packages
Built on a pnpm workspace with shared packages `@newlifex/api-core` (API / token / types) and `@newlifex/field-mapping` (field metadata -> controls). Eight skins work out of the box; reference the NuGet package to get the complete front-end.

### Front-end workspace conventions
- Root `pnpm build`: builds only the shared modules under `packages/`, it no longer recurses into every skin.
- Build one skin: `pnpm build:vue`, `pnpm build:react`, `pnpm build:naive`, `pnpm build:mui`, `pnpm build:shadcn`, `pnpm build:arco`, `pnpm build:angular`, `pnpm build:vuetify`, `pnpm build:svelte`, `pnpm build:tdesign`.
- Develop one skin: the matching `pnpm dev:xxx`.
- Build or start everything: `pnpm build:all` / `pnpm dev:all`.
- Inside a skin's `web/` directory, use its own `pnpm build` / `pnpm dev`.

| NuGet package | Framework | UI library | Dev port |
|---------------|-----------|------------|----------|
| `NewLife.Cube.NaiveUI` | Vue 3 | Naive UI | 5180 |
| `NewLife.Cube.MUI` | React 19 | MUI 6 (Material) | 5181 |
| `NewLife.Cube.Shadcn` | React 19 | Radix UI + Tailwind CSS 4 | 5182 |
| `NewLife.Cube.ArcoVue` | Vue 3 | Arco Design Vue | 5183 |
| `NewLife.Cube.Angular` | Angular 19 | NG-ZORRO | 5184 |
| `NewLife.Cube.Vuetify` | Vue 3 | Vuetify 3 | 5185 |
| `NewLife.Cube.Svelte` | SvelteKit 2 + Svelte 5 | Tailwind CSS 4 | 5186 |
| `NewLife.Cube.TDesign` | Vue 3 | TDesign | 5187 |

```csharp
// Pick one
app.UseNaiveUI(app.Environment);
app.UseMUI(app.Environment);
app.UseShadcn(app.Environment);
app.UseArcoVue(app.Environment);
app.UseAngular(app.Environment);
app.UseVuetify(app.Environment);
app.UseSvelte(app.Environment);
app.UseTDesign(app.Environment);
```

---
## Multi-database and big data
- XCode: schema synchronization, field evolution, automatic sharding, read/write splitting and second-level cache.
- Three-database example: Membership (permissions) / Cube (business) / Log (logs).
- Supported: MySql / SQLite / SqlServer / Oracle / PostgreSql / SqlCe / Access / DM.
- PluginServer downloads database drivers and front-end assets automatically.

---
## Performance
- Filters record elapsed time uniformly and integrate with Stardust APM.
- Log level checks avoid unnecessary string concatenation.
- Multiple TFMs allow Span/ValueTask optimizations on newer frameworks.

---
## Deployment
- Single machine: `dotnet publish`, copy DLLs and configuration.
- Service: use `NewLife.Agent` for Windows Service or systemd.
- Cluster: publish, zip, start under the StarAgent daemon or deploy via Stardust.
- Container: a simple Dockerfile (copy the publish folder, set environment variables).
- Scale-out: stateless instances sharing the database and cache.

---
## Deploying the user center
(The original detailed steps follow below; kept consistent.)

1. Clone the source and build the CubeSSO project … see the next section.

---
### Deploying the user center
1. Clone the [source](https://github.com/NewLifeX/NewLife.Cube), build CubeSSO, go to `Bin/SSO`, adjust `appsettings.json`, package the exe/dll/appsettings/runtimeconfig files and deploy.
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
    "Membership": "Data Source=../Data/Membership.db;provider=sqlite",
    "Cube": "Data Source=../Data/Cube.db;provider=sqlite",
    "Log": "Data Source=../Data/Log.db;provider=sqlite"
  }
}
```
2. Start and open the SSO site. The first login can use `admin/admin` or a third-party login; the first user becomes administrator and `admin` is disabled.
3. For a production user center, disable third-party login: Cube settings -> OAuth -> disable all.
4. Rename the system in Cube settings -> System -> Display name.

---
## Cube generation 3
Generation 3 focuses on "front-end/back-end separation + multi-front-end ecosystem + minimal intrusion". Backend APIs live in master; front-ends are independent repositories (Vue / QuickVue / Antd / Blazor).

Vue: <https://vue.newlifex.com>, <https://quickvue.newlifex.com>  
Antd: <https://antd.newlifex.com>  
Swagger: <https://cube3.newlifex.com/swagger/index.html>

### Participation notes
1. Join the NewLifeX team and submit to the dev branch.
2. WebApi backend: `NewLife.Cube`; MVC stays in `NewLife.CubeNC`.
3. New front-ends live in independent repositories (e.g. `NewLife.CubeAntd`).
4. Mark owners in docs and code.
5. Tasks: <https://github.com/orgs/NewLifeX/projects/1>.

### Blueprint
1. Modern UI while keeping the override mechanism.
2. Multi-framework front-ends: Vue/React/Angular/Svelte + 8 ready-to-use skins.
3. Reference the NuGet package and get the default skin.
4. Mobile, mini-programs and hybrid apps.
5. Enhanced data dashboards.

### Milestones (excerpt)
- [x] New WebApi project `NewLife.Cube`
- [x] Move NetCore-specific code to `NewLife.CubeNC`
- [x] New `EntityController` / `EntityReadOnlyController`
- [x] API docs and Vue login page
- [ ] Vue user/role list and forms

Front-end repositories:
<https://github.com/NewLifeX/NewLife.CubeVue>
<https://github.com/NewLifeX/NewLife.QuickVue>
<https://github.com/NewLifeX/NewLife.CubeAntd>
<https://github.com/NewLifeX/NewLife.CubeBlazor>

### WebApi endpoints (excerpt)
1. <https://cube3.newlifex.com/swagger/index.html>
2. Login `/Auth/Login` with `admin/admin` or `test/test`
3. JWT: header / cookie / query (`token`)
4. Menus `/Cube/MenuTree`
5. Fields `/{Area}/{Controller}/GetFields?kind=1` (`1=List 2=Detail 3=AddForm 4=EditForm`)
6. List: main route + dynamic query parameters
7. `page`: paging information
8. `stat`: statistics row
9. Detail `Detail` | insert `Insert` | update `Update` | delete `Delete`

---
## Common scenarios
- Enterprise/department admin backends and SaaS control panels.
- IoT device/point/log/alert management and data export.
- Rapid publication of standard query APIs from a data platform.
- Unified identity and authorization center (SSO/OAuth).
- Operations support systems (orders, configuration, scheduling, releases).

---
## Contributing
- Conventions: see `.github/copilot-instructions.md`.
- Issues/PRs: describe the scenario, expectation and reproduction; include data scale for performance topics.
- New themes: separate repository + README describing the build.
- Prefer the NewLife ecosystem and avoid unnecessary dependencies.
- Make sure all TFMs compile before submitting.

---
## FAQ
Q: Hide a field?  
A: In the controller static constructor: `ListFields.RemoveField("Field")`.

Q: Custom statistics?  
A: Override search logic and fill `stat` on the returned collection.

Q: Switch themes?  
A: Reference a theme package and call `app.UseXXX(env)`; the default is configurable in the admin UI.

Q: Where do database drivers come from?  
A: Downloaded from `PluginServer` on first run; configurable.

Q: Integrate a custom front-end?  
A: Use the field metadata API plus the standard CRUD/JWT endpoints.

---
## Features
* Generic permission management: users, roles, menus and actions with controller-action granularity.
* Multiple databases: `MySql / SQLite / Sql Server / Oracle / PostgreSql / SqlCe / Access`.
* Zero deployment: schema creation and seed data happen automatically.
* Powerful view engine: a child project can override any parent view at the same path.
* Built-in rich text (Quill) and Markdown (EasyMDE) editors with upload and security controls.
* 11 runtime languages for API messages, request-level culture detection and an 11-language Vue front-end.
* NativeAOT smoke coverage for the non-MVC foundation (18 scenarios, JIT/AOT).

---
## ASP.NET Core installation
* Create an `ASP.NET Core Web` project in Visual Studio.
* Reference `NewLife.Cube.Core` from NuGet, or build the latest [Cube NewLife.CubeNC](http://github.com/NewLifeX/NewLife.Cube) source.
* Set a connection string named `Membership` in `appsettings.json`; users, roles, permissions and menus are stored there.
* The database type is detected automatically; the default is `Data Source=../Data/Membership.db`.
* Build the project, right-click -> View -> View in Browser to run Cube.
* MySQL/SQLite/Oracle/SqlCe drivers are downloaded automatically (x86/x64); change `PluginServer` in `Config/Core.config` if needed.
* Scripts and styles are downloaded automatically; change `PluginServer` in `Config/Cube.config` if needed.
* The default login is `admin/admin`; third-party OAuth such as NewLife also works. The first user becomes administrator and `admin` is disabled.
* When publishing, copy `*.dll`, `appsettings.json`, `*.deps.json`, `*.runtimeconfig.json` and your own assets.

---
## ASP.NET MVC installation
* Create an `ASP.NET MVC` project in Visual Studio.
* Reference `NewLife.Cube` from NuGet, or build the latest [Cube NewLife.Cube](http://github.com/NewLifeX/NewLife.Cube) source.
* Set a connection string named `Membership` in the `<connectionStrings>` section of `Web.config`.
* The database type is detected automatically; the default is `<add name="Membership" connectionString="Data Source=../Data/Membership.db" providerName="Sqlite"/>`.
* Build the project, right-click -> View -> View in Browser to run Cube.
* MySQL/SQLite/Oracle/SqlCe drivers are downloaded automatically (x86/x64); change `PluginServer` in `Config/Core.config` if needed.
* Scripts and styles are downloaded automatically; change `PluginServer` in `Config/Cube.config` if needed.
* The default login is `admin/admin`; third-party OAuth such as NewLife also works. The first user becomes administrator and `admin` is disabled.
* Installing the *Razor Generator* Visual Studio extension and setting it as the custom tool for `.cshtml` files compiles views into the DLL.
* Publish and copy the DLLs, configuration and assets.

---
## NewLife project matrix
| Project | Period | Description |
|---------|--------|-------------|
| NewLife.Core | 2002- | Logging, configuration, caching, networking, serialization, tracing and more; 11 TFMs from net45 to net10.0 |
| NewLife.XCode | 2005- | Data middleware: ORM, schema synchronization, sharding, caching; 10 billion rows per table |
| NewLife.Cube | 2016- | Rapid web/admin framework: entity controllers, permissions, menus, APIs and themes |
| NewLife.Redis | 2018- | High-performance Redis client with cluster and second-level cache |
| NewLife.Remoting | 2019- | RPC/HTTP remoting framework |
| NewLife.AI | 2023- | AI integration layer: providers, tools and streaming |
| NewLife.Stardust | 2020- | APM, configuration center, service registry and deployment agent |
| NewLife.IP | 2019- | IP address database and geolocation |
| NewLife.Office | 2020- | Excel/Word/PDF processing |
| NewLife.MySql | 2019- | MySQL driver |
| NewLife.Security | 2019- | Cryptography and security helpers |
| AntJob | 2017- | Distributed task scheduling |
| XCoder | 2010- | Code generator for XCode |
| NewLife.Agent | 2015- | Windows Service / systemd host |
| NewLife.Net | 2008- | High-performance TCP/UDP/WebSocket networking |
| NewLife.MQTT | 2020- | MQTT client and server |
| NewLife.RocketMQ | 2021- | RocketMQ client |
| NewLife.ElasticSearch | 2021- | ElasticSearch client |
| NewLife.NoSql | 2020- | Redis/MongoDB/Cassandra abstractions |
| NewLife.Sync | 2022- | Data synchronization |
| NewLife.Plugin | 2018- | Plugin framework |
| NewLife.Templates | 2021- | `dotnet new` templates |
| NewLife.Cube.Vue | 2024- | Vue 3 + Element Plus skin |
| NewLife.Cube.Blazor | 2023- | Blazor skin |
| NewLife.Cube.Antd | 2023- | Ant Design skin |
| FIoT | 2020- | IoT solution for buildings, environment and agriculture |
| UWB | 2020- | High-precision indoor positioning (10-20 cm) |

---
## NewLife team
![XCode](https://newlifex.com/logo.png)

NewLife has been building open-source .NET components since 2002. MIT licensed; contributions and issues are welcome.
