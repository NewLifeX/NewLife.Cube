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
![arch](https://img.shields.io/badge/arch-x64%20%7C%20x86%20%7C%20arm64%20%7C%20arm%20%7C%20loongarch64%20%7C%20riscv64%20%7C%20s390x-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Cube?logo=github)](https://github.com/NewLifeX/NewLife.Cube/graphs/contributors)

<!-- lang:start -->
🌐 [简体中文](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.zh-CN.md) | [繁體中文](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.zh-TW.md) | [Español](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.es.md) | [Deutsch](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.de.md) | [日本語](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.ja.md) | [Français](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.fr.md) | [Português](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.pt.md) | [Русский](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.ru.md) | [한국어](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.ko.md) | [العربية](https://github.com/NewLifeX/NewLife.Cube/blob/master/README.ar.md)
<!-- lang:end -->

NewLife.Cube is a rapid web development platform for "data-driven + fast iteration + deep customization" scenarios. In minutes, an entity class or database table becomes a complete admin UI with permissions, lists, details, CRUD, export and statistics, while standard APIs are exposed for Vue (official front-end) / React / Antd / mobile or third-party systems. The default template has been proven in real projects with a single table of 10 billion rows.

Demo: <https://cube.newlifex.com> `CentOS7 + CDN` · SSO center: <https://sso.newlifex.com> `OAuth server` · Swagger: <https://cube3.newlifex.com>  

Cube tutorial: <https://newlifex.com/cube> · XCode tutorial: <https://newlifex.com/xcode> · Core library tutorial: <https://newlifex.com/core>

**Contents**: [Quick Start](#quick-start) · [Main Features](#main-features) · [Ecosystem Map](#ecosystem-map) · [Documentation](#documentation) · [Contributing](#contributing) · [Team](#team) · [License](#license)

## Quick Start

### Install

Install `NewLife.Cube.Core` from NuGet. MIT licensed; you may replace the "NewLife" string for private builds. Works with Visual Studio and the CLI. `newlife.snk` is public for strong-name consistency.

```bash
dotnet add package NewLife.Cube
```

### Template scaffolding

```
dotnet new install NewLife.Templates
dotnet new cube --name CubeWeb
dotnet new xcode --name Zero.Data
cd CubeWeb
dotnet build
start http://localhost:6080
dotnet run
```

### Minimal example (.NET 8/9 Minimal Hosting)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCube(); // registers permissions, menus, entity controller scanning
var app = builder.Build();
app.UseMetronic(app.Environment); // or UseAdminLTE / UseTabler ...
app.UseCube(app.Environment);      // routing, static assets and middleware
app.Run();
```

Open `/` to sign in, use Swagger for APIs, and call the standard CRUD APIs from any front-end.

### Platform support

| Item | Coverage |
| --- | --- |
| Target frameworks | 5 in one package: net6.0, net7.0, net8.0, net9.0, net10.0 |
| OS and architecture | Windows, Linux, macOS; x64, x86, arm64, arm, loongarch64, riscv64, s390x |
| Databases | MySql / SQLite / SqlServer / Oracle / PostgreSql / SqlCe / Access / DM |
| NativeAOT | 18 smoke scenarios for the non-MVC foundation (JIT/AOT); see the badge above for the current count |

### ASP.NET Core installation

* Create an `ASP.NET Core Web` project in Visual Studio.
* Reference `NewLife.Cube.Core` from NuGet, or build the latest [Cube NewLife.CubeNC](http://github.com/NewLifeX/NewLife.Cube) source.
* Set a connection string named `Membership` in `appsettings.json`; users, roles, permissions and menus are stored there.
* The database type is detected automatically; the default is `Data Source=../Data/Membership.db`.
* Build the project, right-click -> View -> View in Browser to run Cube.
* MySQL/SQLite/Oracle/SqlCe drivers are downloaded automatically (x86/x64); change `PluginServer` in `Config/Core.config` if needed.
* Scripts and styles are downloaded automatically; change `PluginServer` in `Config/Cube.config` if needed.
* The default login is `admin/admin`; third-party OAuth such as NewLife also works. The first user becomes administrator and `admin` is disabled.
* When publishing, copy `*.dll`, `appsettings.json`, `*.deps.json`, `*.runtimeconfig.json` and your own assets.

### ASP.NET MVC installation

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

## Main Features

<details>
<summary><b>Data-to-UI productivity</b> (9)</summary>

| Feature | Key Characteristics |
| --- | --- |
| EntityController scaffolding | Entity class + controller gives CRUD, statistics, export and permissions immediately; `ReadOnlyEntityController` covers read-only lists. |
| Static-constructor configuration | Configure fields, action buttons, related links, statistics and export in a controller static constructor. |
| Field metadata API | `meta` (page settings + List / Detail / AddForm / EditForm / Search fields) drives dynamic front-end generation — no hard coding. |
| Menu tree | `/Cube/MenuTree` builds multi-level menus from permissions; `/Admin/Index/GetMenuTree` serves the classic MVC UI. |
| Standard API response | `ApiResponse` wraps code / data / page / stat for every endpoint. |
| Zero deployment | Schema creation and seed data happen automatically on first run, with no manual intervention. |
| Powerful view engine | A child project can override any parent view at the same path (`.cshtml`). |
| Built-in editors | Rich text (Quill) and Markdown (EasyMDE) with image upload and security controls. |
| Scalar API docs | Scalar UI grouped by area alongside Swagger, for easier online debugging. |

</details>

<details>
<summary><b>Field and UI customization</b> (3)</summary>

| Feature | Key Characteristics |
| --- | --- |
| Field set customization | `ListFields.RemoveField` / `AddListField` adjust columns, links and targets per controller. |
| Change log | `LogOnChange = true` writes an audit entry for every modification. |
| Dynamic front-end fields | The front-end fetches `/Cube/App/meta` for page metadata and field definitions — no hard coding. |

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

</details>

<details>
<summary><b>API and front-end separation</b> (6)</summary>

| Endpoint | Purpose |
| --- | --- |
| `/Auth/Login` | Sign in and receive a JWT token (header / cookie / query). |
| `/Cube/MenuTree` | Permission-aware menu tree. |
| `/{Area}/{Controller}/meta` | Page metadata for list / detail / add form / edit form / search. |
| Main route + dynamic parameters | Query with paging, sorting and condition composition. |
| `GET {id}` / `POST` / `PUT {id}` / `DELETE {id}` | RESTful CRUD sub-resources; batch delete on the base path, plus `charts` / `export` / `import` / `attachments`. |
| `stat` / `page` | Statistics row and paging information. |

</details>

<details>
<summary><b>Permissions and security</b> (7)</summary>

| Feature | Key Characteristics |
| --- | --- |
| Generic permission management | Users, roles, menus and actions with controller-action granularity. |
| EntityAuthorize | Login + role + menu action checks on every action. |
| First user becomes administrator | The built-in `admin` account is disabled afterwards; `admin/admin` works for the first sign-in. |
| Unified identity | External OAuth / SSO / JWT across desktops, web and mobile clients. |
| Security defense | Built-in threat detection (scanners / injection / honeypots), persistent auto-ban with escalating durations and a security audit trail. Automatic mode bans repeated attackers, temporarily blocks broad attacks and recovers automatically; trusted proxies can be configured or learned to prevent spoofing; observation / blocking modes are switchable. |
| Production advice | Disable unused third-party logins, enable HTTPS and enforce a strong password policy. |
| Multi-tenancy switch | Enable or disable multi-tenancy with one switch; tenant menus and fields are hidden when off, and runtime tenant switching is supported. |

</details>

<details>
<summary><b>AI capabilities</b> (4)</summary>

| Feature | Key Characteristics |
| --- | --- |
| AI assistant | A single chat panel in the lower-right corner with page / entity / query context. The global `AiController.AiChat` endpoint (`/Ai/AiChat`) plus NAI `AiChatService` orchestrate session history, the tool loop and SSE streaming events. |
| Extension point | Entity controllers expose data queries, tool sets and prompt overrides through `IEntityAiContext` (`SearchData` / `CreateCubeTools` / `BuildChatSystemPrompt`). Tools include `get_data_context`, `get_form_schema`, `fill_form`, `get_page_context`, `get_system_info` and `run_js`. |
| AI insight | Merged into the assistant; `AiInsightHelper` remains for data collection (14 sensitive-field blacklists). One-click analysis offers statistics, anomaly detection, trend prediction and actionable advice with deep reasoning, on both MVC and Vue. |
| Dashboard widgets | Dashboards are composed of configurable widgets (KPI, charts, monitoring) with personalized layouts and `/Cube/Widget` management. |

</details>

<details>
<summary><b>Front-ends, themes and skins</b> (7)</summary>

| Feature | Key Characteristics |
| --- | --- |
| Multiple front-ends | Vue (official), eight in-repo reference SPA skins and classic Razor coexist; React (Ant Design) is maintained in a separate CubeAntd repository; Blazor is a frozen MVC-theme experiment. |
| Embedded theme assets | `CubeEmbeddedFileProvider` ships theme assets inside the package. |
| Theme registration | Register skins via `AddTheme/AddSkin` and pick one in `UseCube` (`app.UseMetronic(env)` etc.). |
| Custom themes | Implement `IModule` like `MetronicModule` and mount it in the use phase. |
| View overrides | A `.cshtml` with the same path in a child project overrides the parent. |
| Localization | 11 runtime languages for API messages, request-level culture detection and an 11-language Vue front-end. |
| New-generation skins | pnpm workspace with the shared package `@newlifex/cube` (API / token / types, field metadata -> controls). `NewLife.Cube.Vue` is the official skin; the eight SPA skins live in this repo as reference implementations and are not published to NuGet — build their `web` project with pnpm, then reference the .NET project. React lives in the separate CubeAntd repository. |

| Skin project | Framework | UI library | Dev port |
|---------------|-----------|------------|----------|
| `NewLife.Cube.NaiveUI` | Vue 3 | Naive UI | 5180 |
| `NewLife.Cube.MUI` | React 19 | MUI 6 (Material) | 5181 |
| `NewLife.Cube.Shadcn` | React 19 | Radix UI + Tailwind CSS 4 | 5182 |
| `NewLife.Cube.ArcoVue` | Vue 3 | Arco Design Vue | 5183 |
| `NewLife.Cube.Angular` | Angular 19 | NG-ZORRO | 5184 |
| `NewLife.Cube.Vuetify` | Vue 3 | Vuetify 3 | 5185 |
| `NewLife.Cube.Svelte` | SvelteKit 2 + Svelte 5 | Tailwind CSS 4 | 5186 |
| `NewLife.Cube.TDesign` | Vue 3 | TDesign | 5187 |

> The skins above are reference implementations kept in this repository, not published NuGet packages. `NewLife.Cube.Vue` is the official skin with full unit and E2E coverage.

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

Front-end workspace conventions:

- Root `pnpm build`: builds only the shared modules under `packages/`; it no longer recurses into every skin.
- Build one skin: `pnpm build:vue`, `pnpm build:naive`, `pnpm build:mui`, `pnpm build:shadcn`, `pnpm build:arco`, `pnpm build:angular`, `pnpm build:vuetify`, `pnpm build:svelte`, `pnpm build:tdesign`.
- Develop one skin: the matching `pnpm dev:xxx`.
- Build or start everything: `pnpm build:all` / `pnpm dev:all`.
- Inside a skin's `web/` directory, use its own `pnpm build` / `pnpm dev`.

</details>

<details>
<summary><b>Data and big data</b> (5)</summary>

| Feature | Key Characteristics |
| --- | --- |
| XCode data layer | Schema synchronization, field evolution, automatic sharding, read/write splitting and second-level cache. |
| Multiple databases | MySql / SQLite / SqlServer / Oracle / PostgreSql / SqlCe / Access / DM. |
| Database separation | Three-database example: Membership (permissions) / Cube (business) / Log (logs). |
| PluginServer | Downloads database drivers and front-end assets automatically on first run; configurable. |
| Proven scale | Real projects with a single table of 10 billion rows. |

</details>

<details>
<summary><b>Performance and NativeAOT</b> (4)</summary>

| Feature | Key Characteristics |
| --- | --- |
| Unified timing | Filters record elapsed time uniformly and integrate with Stardust APM. |
| Cheap logging | Log level checks avoid unnecessary string concatenation. |
| Modern frameworks | Multiple TFMs allow Span / ValueTask optimizations on newer frameworks. |
| NativeAOT | 18 smoke scenarios for the non-MVC foundation, run as JIT/AOT pairs in CI. |

</details>

<details>
<summary><b>Deployment and operations</b> (5)</summary>

| Scenario | How |
| --- | --- |
| Single machine | `dotnet publish`, then copy the DLLs and configuration. |
| Service | Use `NewLife.Agent` for Windows Service or systemd. |
| Cluster | Publish, zip, start under the StarAgent daemon or deploy via Stardust. |
| Container | A simple Dockerfile (copy the publish folder, set environment variables). |
| Scale-out | Stateless instances sharing the database and cache. |

#### Deploying the user center

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

</details>

<details>
<summary><b>Cube generation 3</b> (5)</summary>

| Item | Description |
| --- | --- |
| Focus | Front-end/back-end separation + multi-front-end ecosystem + minimal intrusion. |
| Backend | WebApi APIs live in master (`NewLife.Cube`); MVC stays in `NewLife.CubeNC`. |
| Front-ends | Independent repositories (Vue / QuickVue / Antd / Blazor). |
| Blueprint | Modern UI while keeping the override mechanism; multi-framework front-ends (Vue/React/Angular/Svelte + 8 ready-to-use skins); NuGet package brings the default skin; mobile, mini-programs and hybrid apps; enhanced data dashboards. |
| Tasks | <https://github.com/orgs/NewLifeX/projects/1> |

Front-end repositories:  
<https://github.com/NewLifeX/NewLife.CubeVue> · <https://github.com/NewLifeX/NewLife.QuickVue> · <https://github.com/NewLifeX/NewLife.CubeAntd> · <https://github.com/NewLifeX/NewLife.CubeBlazor>

Live sites: Vue <https://vue.newlifex.com>, <https://quickvue.newlifex.com> · Antd <https://antd.newlifex.com> · Swagger <https://cube3.newlifex.com/swagger/index.html>

Participation notes:

1. Join the NewLifeX team and submit to the dev branch.
2. WebApi backend: `NewLife.Cube`; MVC stays in `NewLife.CubeNC`.
3. New front-ends live in independent repositories (e.g. `NewLife.CubeAntd`).
4. Mark owners in docs and code.
5. Tasks: <https://github.com/orgs/NewLifeX/projects/1>.

Milestones (excerpt):

- [x] New WebApi project `NewLife.Cube`
- [x] Move NetCore-specific code to `NewLife.CubeNC`
- [x] New `EntityController` / `EntityReadOnlyController`
- [x] API docs and Vue login page
- [ ] Vue user/role list and forms

</details>

<details>
<summary><b>Architecture and projects</b> (9)</summary>

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

| Project | Role |
| --- | --- |
| NewLife.Cube | Core: entity controllers, permissions, menus, field metadata, API. |
| NewLife.CubeNC | Legacy MVC / Razor compatibility layer. |
| Classic skins | AdminLTE / Metronic / Metronic8 / Tabler / LayuiAdmin / ElementUI / Blazor (separate packages; feature-frozen — compile-only, no new features and no bug fixes). |
| New-generation skins | pnpm workspace: NaiveUI / MUI / Shadcn / ArcoVue / Angular / Vuetify / Svelte / TDesign (reference implementations, not published to NuGet). |
| NewLife.Cube.Swagger | Swagger documents grouped by area. |
| CubeDemo / CubeDemoNC | Samples. |
| CubeSSO | Unified user center and OAuth server. |
| NewLife.Cube.Blazor | Blazor experiment (frozen MVC theme). |
| Test / XUnitTest / NewLife.Cube.Tests / E2EMvcTest / AotSmoke / WebSmoke | Tests and verification (WebSmoke starts Kestrel and does one HTTP round-trip on non-x64/arm64 instruction sets). |

</details>

<details>
<summary><b>Core concepts</b> (7)</summary>

| Concept | Description |
| --- | --- |
| EntityController | Configure fields, action buttons, related links, statistics and export in a static constructor. |
| Field metadata API | `meta` (page settings + List / Detail / AddForm / EditForm / Search fields) drives dynamic front-end generation. |
| Menu tree | `/Admin/Index/GetMenuTree` builds multi-level menus from permissions. |
| Permission model | User -> role -> menu (action set). The first registered user becomes administrator and `admin` is disabled. |
| ApiResponse | Standard response wrapper with code / data / page / stat. |
| IModule | Startup module that injects services and static assets (themes, Swagger, etc.). |
| AI assistant | Global `AiController.AiChat` endpoint (`/Ai/AiChat`) plus NAI `AiChatService` orchestration (session history, tool loop, SSE events); entity controllers expose capabilities through `IEntityAiContext`. `AiInsightHelper` remains for data collection (14 sensitive-field blacklists). |

</details>

<details>
<summary><b>Common scenarios</b> (5)</summary>

| Scenario | Typical use |
| --- | --- |
| Enterprise / department admin | Internal management backends and SaaS control panels. |
| IoT operations | Device / point / log / alert management and data export. |
| Data platform | Rapid publication of standard query APIs. |
| Identity center | Unified identity and authorization (SSO / OAuth). |
| Operations support | Orders, configuration, scheduling and release management. |

</details>

## Ecosystem Map

NewLife is a layered ecosystem of independently versioned open-source projects. The map below is shared verbatim by every NewLife repository; each project links to its repository and its primary NuGet package.

| Project | What it does | Downloads |
| --- | --- | --- |
| **Foundation** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | Logging, configuration, caching, networking, serialization, scheduling and APM tracing. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | Data middleware: single tables at the tens-of-billions scale, automatic sharding, read-write splitting, 10+ databases. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | High-performance network library: 1.77M pps / 14.5 Gbps on a single machine (TCP 1KB, 2026-10), 4M long-lived TCP connections. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | Protocol communication and RPC framework for client/server applications and IoT devices. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | Installs .NET applications as OS daemons: Windows services and Linux systemd. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new` project templates for console, web, API, service, data and IoT projects. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **Data & Messaging** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis client with microsecond latency, millions of ops/s and built-in message queues. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | Pure managed RocketMQ client for Apache RocketMQ and Alibaba Cloud Message Queue. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT client and server, including Alibaba Cloud IoT connectivity. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | Hybrid database in C#: embedded or networked, relational, time-series, message queue and KV in one. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | MySql driver and helper layer built on the NewLife data stack. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web, AI & Distributed** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | Rapid admin-platform framework: permissions, SSO, OAuth server and CRUD scaffolding. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | Distributed service platform: node management, APM, configuration center, registry and release center. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | Distributed task and real-time data scheduling platform with sharding, retries and backfill. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | Unified client for 46 LLM providers plus ChatAI, a ready-to-run web chat application. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **Industrial & IoT** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | IoT standard library defining protocol contracts and device abstractions. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | Modbus TCP/RTU/ASCII implementation on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | Siemens PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | Omron PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | Mitsubishi Melsec PLC protocol on top of NewLife.IoT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | OPC client for industrial data access. | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet building-automation protocol stack. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | Lightweight open-source IoT platform with gateway and device management. | — |
| **Vehicle Telematics** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 vehicle positioning protocol: 70+ message bodies, 2011/2013/2019. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T 809 platform-to-platform protocol for transport authorities. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 in-vehicle video: RTP parsing, FLV/fMP4 remuxing and live streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **Domain Components** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | One package for Excel, Word, PPT, PDF and 15+ document formats. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | Cross-platform audio: codecs, device I/O, DSP, containers and streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | Video codecs, container formats, frame processing and streaming. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | Unified interface over Baidu, AMap, Tencent and Tianditu with geocoding and coordinate conversion. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 geolocation with an offline memory-mapped database and an online API fallback. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **Tools & Products** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | Developer toolbox: network, serial port, crypto, regex, Modbus and MQTT. | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | Self-hosted Git source code management system. | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | Installs the matching .NET runtime automatically when launched from an older host. | — |

> Badges show the primary package of each project; theme, extension and satellite packages are listed on their NuGet pages.The 30+ packages not listed here (FTP, LoRa, Office extensions, protocol drivers and more) are available under the [NewLifeX organization](https://github.com/NewLifeX) and on [newlifex.com](https://newlifex.com). This table is maintained in the NewLife.Core README.

## Documentation

### Guides

Detailed requirements, feature tracking and architecture live in [`Doc/`](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/) (written in Chinese):

- 📋 [Requirements](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/需求文档.md) — vision, goals, functional requirements
- ✅ [Feature list](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/功能清单.md) — implementation/test/comment tracking
- 🏗️ [Architecture](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/架构设计.md) — layering, modules, key decisions
- 🧩 [Page presentation](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/Api/页面呈现架构.md) — dynamic / hybrid / custom pages, backend and frontend extension ladders
- 🤖 [AI](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/AI-人工智能.md) — AI capabilities, configuration and extension points
- 🔔 [Alerts and inbox](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/ALM-在线告警与站内信.md) — online threshold alerts and broadcast messages
- ✉️ [Email/phone verification](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/AUTH-邮箱手机验证.md) — activation contract and UI integration
- 🛡️ [Security](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/PERM-安全防御.md) — threat detection, auto-ban and security audit
- 📚 [User manual](https://github.com/NewLifeX/NewLife.Cube/blob/master/Doc/魔方用户手册.md) — full user and developer guide

### Tutorials

- Cube tutorial: <https://newlifex.com/cube>
- XCode tutorial: <https://newlifex.com/xcode>
- Core library tutorial: <https://newlifex.com/core>
- Online demo: <https://cube.newlifex.com> · SSO center: <https://sso.newlifex.com> · Swagger: <https://cube3.newlifex.com>

### FAQ

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

## Contributing

### Build and test

Cube builds with the .NET SDK 10 (or the matching SDK for your target framework) and also opens in Visual Studio. The front-end skins use pnpm.

```bash
dotnet build NewLife.Cube/NewLife.Cube.csproj -c Release
dotnet test XUnitTest -c Release
```

- End-to-end MVC tests: `E2EMvcTest/`
- NativeAOT smoke: `AotSmoke/`, CI in `.github/workflows/aot.yml`
- Web smoke: `WebSmoke/` starts Kestrel and performs one HTTP round-trip

### Release

Push a `v`-prefixed tag to trigger `.github/workflows/publish.yml`; the workflow packs `NewLife.Cube` / `NewLife.Cube.Core` and publishes them to NuGet.org. Package README is generated from `README.md` by `sync-readme-package.ps1`, language switchers by `sync-readme-languages.ps1` and metric badges by `sync-readme-metrics.ps1`; each script supports `-Check` for CI.

### Feedback

- Conventions: see `.github/copilot-instructions.md`.
- Issues/PRs: describe the scenario, expectation and reproduction; include data scale for performance topics.
- New themes: separate repository + README describing the build.
- Prefer the NewLife ecosystem and avoid unnecessary dependencies.
- Make sure all TFMs compile before submitting.

## Team

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

Founded in 2002, the NewLife team has spent more than two decades on the .NET and IoT ecosystem, maintains nearly 100 open-source projects (34 of them core to this map), and delivers integrated software and hardware solutions for power, logistics, healthcare, industrial control and internet industries. We live by the motto "learning has no seniority — mastery earns respect".

Open-source components have been downloaded <!--nuget-total:start-->6.48M<!--nuget-total:end--> times from NuGet (ecosystem-wide, as of 2026-10).

Website: https://newlifex.com · Open source organization: https://github.com/NewLifeX

<details>
<summary>WeChat Official Account: 智能大石头 (scan to follow)</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## License

This project is licensed under [MIT](https://github.com/NewLifeX/NewLife.Cube/blob/master/LICENSE); the strong-name certificate `newlife.snk` is published so you can build and replace assemblies yourself, and the source may be modified and redistributed without attribution.

> If this project is helpful to you, welcome to Star it, share it with your team, or provide improvement suggestions via Issue/PR.
