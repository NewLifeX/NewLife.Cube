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
[![AOT smoke](https://img.shields.io/badge/AOT%20smoke-16%2F16-brightgreen?logo=dotnet)](Doc/AOT兼容性分析.md)
[![TFM](https://img.shields.io/badge/TFM-5%20%28net6.0%20%E2%86%92%20net10.0%29-512BD4?logo=dotnet)](https://www.nuget.org/packages/NewLife.Cube)
[![locales](https://img.shields.io/badge/locales-31-2ea44f?logo=dotnet)](Doc/多语言本地化.md)
<!-- metrics:end -->
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20arm64-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Cube?logo=github)](https://github.com/NewLifeX/NewLife.Cube/graphs/contributors)

<!-- lang:start -->
🌐 [中文](README.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

魔方是一個快速 Web 開發平臺，面向“資料驅動 + 快速迭代 + 可深度定製”場景。幾分鐘內即可把實體 / 資料庫錶轉為帶許可權、列表、詳情、增刪改、匯出、統計的完整後臺介面，並開放標準 API 給 Vue / Blazor / Antd / 移動端或第三方系統呼叫。預設模板已在真實專案中經歷單表 100 億資料增刪改查考驗。

演示站點：<https://cube.newlifex.com> `CentOS7 + CDN`  
SSO中心：<https://sso.newlifex.com> `OAuth 服务端`  
Swagger：[https://cube3.newlifex.com@@P191@@

魔方教程：<https://newlifex.com/cube>  
XCode教程：<https://newlifex.com/xcode>  
核心庫教程：<https://newlifex.com/core>  

> **文件索引**：詳細需求文件、功能清單、架構設計見 [`Doc/`](Doc/) 目錄：
> - 📋 [需求文件](Doc/需求文档.md) — 願景、核心目標、功能需求
> - ✅ [功能清單](Doc/功能清单.md) — 實現/測試/註釋三維狀態追蹤
> - 🏗️ [架構設計](Doc/架构设计.md) — 分層架構、模組設計、關鍵決策
> - 🤖 [AI 人工智慧](Doc/AI-人工智能.md) — AI 能力清單、配置與擴充套件點
> - 🔔 [線上告警與站內信](Doc/ALM-在线告警与站内信.md) — 線上新高告警、系統級廣播站內信與未讀閉環
> - � [郵箱/手機驗證](Doc/AUTH-邮箱手机验证.md) — 賬號啟用契約（註冊→啟用→登入），前端皮膚接入規範
> - 🛡️ [安全防禦](Doc/PERM-安全防御.md) — 內建威脅檢測、自動封禁與安全事件審計
> - �📚 [魔方使用者手冊](Doc/魔方用户手册.md) — 完整的使用者與開發手冊

---
## 目錄索引
- [魔方 NewLife.Cube](#魔方-newlifecube)
  - [目錄索引](#目录索引)
  - [核心價值](#核心价值)
  - [總體架構](#总体架构)
  - [專案結構與子工程說明](#项目结构与子工程说明)
  - [核心概念](#核心概念)
  - [快速擁有](#快速拥有)
  - [最小示例（.NET 8/9 Minimal Hosting）](#最小示例net-89-minimal-hosting)
  - [欄位與介面定製示例](#字段与界面定制示例)
  - [API 與前後端分離](#api-与前后端分离)
  - [許可權與安全](#权限与安全)
  - [主題 / 皮膚 / UI 擴充套件](#主题--皮肤--ui-扩展)
    - [新一代前端皮膚包](#新一代前端皮肤包)
    - [前端 workspace 構建約定](#前端-workspace-构建约定)
  - [多資料庫與大數據支援](#多数据库与大数据支持)
  - [效能特性](#性能特性)
  - [部署方式](#部署方式)
  - [快速部署使用者中心](#快速部署用户中心)
    - [快速部署使用者中心](#快速部署用户中心-1)
  - [第三代魔方](#第三代魔方)
    - [專案參與須知](#项目参与须知)
    - [目標藍圖](#目标蓝图)
    - [春雨計劃（里程碑摘錄）](#春雨计划里程碑摘录)
    - [WebApi 介面說明（節選）](#webapi-接口说明节选)
  - [常見使用場景](#常见使用场景)
  - [貢獻指南](#贡献指南)
  - [FAQ 速覽](#faq-速览)
  - [魔方特性](#魔方特性)
  - [ASP.NET Core 安裝](#aspnet-core-安装)
  - [ASP.NET MVC 安裝](#aspnet-mvc-安装)
  - [新生命專案矩陣](#新生命项目矩阵)
  - [新生命開發團隊](#新生命开发团队)

---
## 核心價值
1. 極速“資料到介面”生產力：實體類 + 控制器 = CRUD / 統計 / 匯出 / 許可權 即刻可用。  
2. 全維許可權：使用者 / 角色 / 選單 / Action 粒度，支援 SSO + OAuth + JWT。  
3. 多前端共存：Vue / Blazor / AntDesign / 傳統 Razor 可漸進遷移。  
4. 多資料庫 + 自動建表 + 自動分表 + 讀寫分離（依賴 XCode）支援超大規模資料。  
5. 可擴充套件：欄位後設資料、檢視覆寫、主題外掛、IModule 模組化。  
7. **AI 智慧洞察**：任何資料頁面一鍵 AI 分析——自動統計、異常檢測、趨勢預判、可執行建議。支援 SSE 流式輸出和深度推理模式。MVC / Vue 雙前端適配。  
8. **AI 對話助手**：右下角對話面板統一入口，上下文感知（當前頁面/實體/查詢）。基於 NewLife.AI 工具層，對話即可完成資料分析、智慧填表（後設資料驅動通用填表）、記錄分析與系統診斷，支援深度推理開關。  
9. **工作臺 Widget 部件體系**：工作臺 / 首頁由可配置部件（KPI、圖表、監控）組成，支援個性化佈局與 `/Cube/Widget` 部件管理。  
10. **匯出增強**：資料匯出支援 xlsx 全量匯出、Zip 打包、CSV 表頭與列舉數字，並帶 SYLK 防護。  
11. **SVG 文字頭像**：使用者頭像缺失時自動兜底生成 SVG 文字頭像（中文取前 N 字 + 漸變視覺）。  
12. **Scalar API 文件**：整合 Scalar 線上介面文件，配合 Swagger 按區域自動分組，介面線上除錯更便捷。  
13. **多租戶開關**：多租戶能力可一鍵開關，關閉後隱藏租戶選單與欄位，支援執行時租戶切換。  
6. 長期演進 & 多目標框架，易升級、穩定可控。  

---
## 總體架構
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

---
## 專案結構與子工程說明
- NewLife.Cube：核心（實體控制器、許可權、選單、欄位後設資料、API）。
- NewLife.CubeNC：舊版 MVC / Razor 相容層。
- 皮膚主題：AdminLTE / Metronic / Metronic8 / Tabler / LayuiAdmin / ElementUI / Blazor（各獨立包）。
- 新一代前端皮膚（pnpm workspace）：NaiveUI / MUI / Shadcn / ArcoVue / Angular / Vuetify / Svelte / TDesign。
- NewLife.Cube.Swagger：按區域自動分組的 Swagger 文件。 
- CubeDemo / CubeDemoNC：示例。  
- CubeSSO：統一使用者中心 & OAuth 服務端。  
- NewLife.Cube.Blazor：Blazor 前端實驗。  
- Test / XUnitTest：測試與驗證。  

---
## 核心概念
- EntityController：通過靜態建構子配置欄位、操作按鈕、關聯跳轉、統計、匯出。  
- 欄位後設資料介面：`GetFields?kind=List|Detail|AddForm|EditForm` 驅動動態前端生成。  
- 選單樹：`/Admin/Index/GetMenuTree` 根據許可權生成多級選單。  
- 許可權模型：使用者 -> 角色 -> 選單(Action 集合)。首個使用者搶管理員，`admin` 被停用。  
- ApiResponse：標準響應包裝 code / data / page / stat。  
- IModule：啟動模組注入服務和靜態資源（主題、Swagger 等）。  
- **AI 對話助手**：全域性 `AiController` 統一 `AiChat` 端點（`/Ai/AiChat`）+ NAI `AiChatService` 編排（會話歷史 `ChatSessionService` 保持/工具迴圈/SSE 事件流）。實體控制器經 `IEntityAiContext` 能力介面向全域性端點暴露資料查詢/工具集/提示詞過載點（保留子類過載 `SearchData`/`CreateCubeTools`/`BuildChatSystemPrompt`）。工具集含 `get_data_context`（資料上下文）/`get_form_schema`（編輯模式帶已有值）/`fill_form`（智慧填表）/`get_page_context`（頁面上下文，`IPageDataContext` 服務端優先/瀏覽器採集兜底）/`get_system_info`（系統診斷）/`run_js`（頁面指令碼執行，SSE 下發 + 前端回傳，檢查點經事件匯流排廣播支援分散式叢集），均帶中文描述供 LLM 決策。SSE 事件驅動前端填表與工具卡片展示，MVC 與 Vue 雙端浮窗。
- **AI 洞察（已合併）**：原 `AiInsight` 獨立端點已合併到 AI 對話助手，資料收集保留 `AiInsightHelper`（14 類敏感欄位黑名單）。

---
## 快速擁有
通過 NuGet 引入 `NewLife.Cube.Core`。MIT 協議，可私有化替換字串“NewLife”。支援 VS 與 CLI。`newlife.snk` 已公開便於強命名一致性。  

命令列體驗：
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
## 最小示例（.NET 8/9 Minimal Hosting）
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCube(); // 注册权限、菜单、实体控制器扫描
var app = builder.Build();
app.UseMetronic(app.Environment); // or UseAdminLTE / UseTabler ...
app.UseCube(app.Environment);      // 路由 & 静态资源 & 中间件
app.Run();
```
訪問 `/` 登入後臺；Swagger 檢視介面；前端可呼叫標準 CRUD API。

---
## 欄位與介面定製示例
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
前端：`/Cube/App/GetFields?kind=1` 獲取列表欄位定義，無需硬編碼。

---
## API 與前後端分離
- 登入：`/Auth/Login` -> JWT Token（Header/Cookie/Query）。
- 選單：`/Cube/MenuTree`。
- 欄位：`/{Area}/{Controller}/GetFields?kind=1..4`。
- 查詢：主路由 + 動態引數（支援分頁、排序、條件拼裝）。
- CRUD：`Detail` / `Insert` / `Update` / `Delete`。
- 統計：返回 `stat`；分頁資訊：`page`。

---
## 許可權與安全
- EntityAuthorize：登入 + 角色 + 選單 Action 校驗。  
- 首個使用者自動管理員，原 `admin` 停用。  
- 支援外部 OAuth / SSO / JWT，多端統一身份。  
- 建議生產：關閉未用第三方登入、啟用 HTTPS、加強密碼策略。
- 安全防禦：內建威脅檢測（掃描器/注入/蜜罐）、持久化自動封禁（階梯時長）與安全事件審計，預設自動模式（連續攻擊自動封禁、大範圍攻擊臨時攔截並自動回落），可配置或自動學習可信代理防偽造，可一鍵切換觀察/攔截模式。  

---
## 主題 / 皮膚 / UI 擴充套件
- 主題內嵌靜態資源：`CubeEmbeddedFileProvider`。  
- UIService 註冊皮膚：`AddTheme/AddSkin`。  
- 自定義：仿照 `MetronicModule` 實現 IModule 並在 Use 階段掛載。  
- 檢視覆寫：子專案同路徑同名 `.cshtml` 覆蓋父級。  

### 新一代前端皮膚包
基於 pnpm workspace + 公共模組 `@newlifex/api-core`（統一 API / Token / 型別）和 `@newlifex/field-mapping`（欄位後設資料 → 控制元件對映），8 套皮膚均開箱即用，NuGet 引包即得完整前端。

### 前端 workspace 構建約定

- 倉庫根目錄 `pnpm build`：僅構建 `packages/` 下公共模組，不再預設遞迴編譯所有皮膚。
- 倉庫根目錄單皮膚構建：使用 `pnpm build:vue`、`pnpm build:react`、`pnpm build:naive`、`pnpm build:mui`、`pnpm build:shadcn`、`pnpm build:arco`、`pnpm build:angular`、`pnpm build:vuetify`、`pnpm build:svelte`、`pnpm build:tdesign`。
- 倉庫根目錄單皮膚開發：使用對應的 `pnpm dev:xxx`。
- 需要一次性構建或啟動全部前端時，顯式使用 `pnpm build:all` 或 `pnpm dev:all`。
- 若已進入具體皮膚目錄 `web/`，繼續使用該目錄下的 `pnpm build` / `pnpm dev` 即可，互不干擾。

| NuGet 包 | 框架 | UI 庫 | 開發埠 |
|---------|------|-------|---------|
| `NewLife.Cube.NaiveUI` | Vue 3 | Naive UI | 5180 |
| `NewLife.Cube.MUI` | React 19 | MUI 6 (Material) | 5181 |
| `NewLife.Cube.Shadcn` | React 19 | Radix UI + Tailwind CSS 4 | 5182 |
| `NewLife.Cube.ArcoVue` | Vue 3 | Arco Design Vue (位元組) | 5183 |
| `NewLife.Cube.Angular` | Angular 19 | NG-ZORRO (Ant Design) | 5184 |
| `NewLife.Cube.Vuetify` | Vue 3 | Vuetify 3 (Material) | 5185 |
| `NewLife.Cube.Svelte` | SvelteKit 2 + Svelte 5 | Tailwind CSS 4 | 5186 |
| `NewLife.Cube.TDesign` | Vue 3 | TDesign (騰訊) | 5187 |

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

---
## 多資料庫與大數據支援
- XCode：實體同步建表、欄位演進、自動分表、讀寫分離、二級快取。  
- 三庫分離示例：Membership(許可權) / Cube(業務) / Log(日誌)。  
- 支援 MySql / SQLite / SqlServer / Oracle / PostgreSql / SqlCe / Access / 達夢。  
- PluginServer 自動下載資料庫驅動 & 前端資源。  

---
## 效能特性
- 過濾器統一記錄耗時，易接入 Stardust APM。  
- 日誌判級避免無謂字串拼接。  
- 多 TFM 架構允許在高版本啟用 Span/ValueTask 最佳化。  

---
## 部署方式
- 單機：`dotnet publish` 後複製 dll + 配置即可。  
- 服務：結合 `NewLife.Agent` 安裝為 Windows Service 或 systemd。  
- 叢集：釋出後打zip包，通過星塵代理`StarAgent`拉起守護，或通過星塵平臺釋出。  
- 容器：簡單 Dockerfile（複製釋出目錄，設定環境變數）。  
- 橫向擴充套件：無狀態多例項，共享資料庫 & 快取。  

---
## 快速部署使用者中心
(下方為原始詳盡步驟，保持一致)

1. 拉取原始碼並編譯 CubeSSO 專案…… 詳見下節。

---
### 快速部署使用者中心
1. 拉取[原始碼](https://github.com/NewLifeX/NewLife.Cube)並編譯CubeSSO專案，切換到輸出目錄Bin/SSO，按需修改appsettings.json配置，打包exe/dll/appsettings.json/CubeSSO.runtimeconfig.json等檔案，併發布到伺服器。
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
2. 啟動並訪問SSO系統，首次登入可以用admin/admin進去，也可以使用第三方登入（新生命使用者中心），第一個使用者將會作為系統管理員，同時停用admin。  
3. 作為使用者中心正式部署時，需要關閉SSO的第三方登入，魔方設定->OAuth設定->停用所有。  
   ![OAuthConfig](Doc/OAuthConfig.png)  
4. 修改系統名稱為新生命使用者中心，魔方設定->系統設定->顯示名稱。  

---
## 第三代魔方
第三代聚焦“前後端分離 + 多前端生態 + 最小侵入”。後端介面合併在 master，前端倉庫獨立（Vue / QuickVue / Antd / Blazor）。  

Vue版：https://vue.newlifex.com, https://quickvue.newlifex.com  
Antd版：https://antd.newlifex.com  
Swagger：https://cube3.newlifex.com/swagger/index.html  

### 專案參與須知
1. 加入 NewLifeX 團隊，向 dev 分支提交。  
2. WebApi 後臺：`NewLife.Cube`；MVC 保留在 `NewLife.CubeNC`。  
3. 新前端獨立倉庫（如 `NewLife.CubeAntd`）。  
4. 文件/程式碼標註負責人。  
5. 待辦任務：https://github.com/orgs/NewLifeX/projects/1 。  

### 目標藍圖
1. 現代化 UI + 保留覆寫機制。  
2. 多框架前端：Vue/React/Angular/Svelte + 8 套開箱即用皮膚包。  
3. NuGet 引包即得預設皮膚。  
4. 移動端 & 小程式 & 混合應用。  
5. 資料大屏增強。  

### 春雨計劃（里程碑摘錄）
- [*] 新建 WebApi 專案 `NewLife.Cube`  
- [*] 遷移 NetCore 專屬程式碼到 `NewLife.CubeNC`  
- [*] 新版 `EntityController` / `EntityReadonlyController`  
- [*] 介面文件 & Vue 登入主頁  
- [] Vue 使用者/角色列表與表單  

前端倉庫：  
https://github.com/NewLifeX/NewLife.CubeVue  
https://github.com/NewLifeX/NewLife.QuickVue  
https://github.com/NewLifeX/NewLife.CubeAntd  
https://github.com/NewLifeX/NewLife.CubeBlazor  

### WebApi 介面說明（節選）
1. 地址 https://cube3.newlifex.com/swagger/index.html  
2. 登入 `/Auth/Login` 測試賬號 `admin/admin` `test/test`  
3. JWT：Header / Cookie / Query(token)  
4. 選單 `/Cube/MenuTree`  
5. 欄位 `/{Area}/{Controller}/GetFields?kind=1` (`1=List 2=Detail 3=AddForm 4=EditForm`)  
6. 列表：主路由 + 動態查詢引數  
7. `page`：分頁資訊  
8. `stat`：統計行  
9. 詳情 `Detail` | 新增 `Insert` | 修改 `Update` | 刪除 `Delete`  

---
## 常見使用場景
- 企業/部門內部管理後臺、SaaS 管理面。  
- IoT 裝置/點位/日誌/告警管理與資料匯出。  
- 資料中臺快速釋出標準查詢介面。  
- 統一身份認證與授權中心（SSO/OAuth）。  
- 運營支撐系統（訂單、配置、任務排程、版本釋出）。  

---
## 貢獻指南
- 規範：參考 `.github/copilot-instructions.md`。  
- Issue / PR：描述場景、期望、復現；效能類附資料規模。  
- 新主題：獨立倉庫 + README 說明構建方式。  
- 優先複用 NewLife 生態，避免不必要依賴。  
- 多 TFM 編譯通過後再提交。  

---
## FAQ 速覽
Q: 隱藏欄位?  
A: 控制器靜態構造：`ListFields.RemoveField("字段")`。  
Q: 自定義統計?  
A: 重寫搜尋邏輯，填充返回集合的 `stat`。  
Q: 切換主題?  
A: 引入主題包並 `app.UseXXX(env)`，後臺可配置預設。  
Q: 資料庫驅動來源?  
A: 首次執行自動從 `PluginServer` 下載，可自定義。  
Q: 整合自定義前端?  
A: 依靠欄位後設資料 + 標準 CRUD/JWT 登入。  

---
## 魔方特性
* 通用許可權管理，使用者、角色、選單、許可權，支援控制器Action許可權控制
* 多資料庫，支援 `MySql / SQLite / Sql Server / Oracle / PostgreSql / SqlCe / Access`
* 免部署，系統自動建立資料庫表結構，以及初始化資料，無需人工干涉
* 強大的檢視引擎，支援子專案檢視重寫父專案相同位置檢視，任意覆蓋修改預設介面
* 內建富文本（Quill）與 Markdown（EasyMDE）雙編輯器，支援圖片上傳與安全控制

---
## ASP.NET Core 安裝
* 在 *Visual Studio* 中新建`ASP.NET Core Web`專案
* 通過 *NuGet* 引用`NewLife.Cube.Core`，或自己編譯最新的[魔方 NewLife.CubeNC](http://github.com/NewLifeX/NewLife.Cube)原始碼
* 在`appsettings.json`的`ConnectionStrings`段設定名為`Membership`的連線字串，使用者角色許可權選單等儲存在該資料庫
* 系統自動識別資料庫型別，預設`Data Source=..\Data\Membership.db`
* 編譯專案，專案上點選滑鼠右鍵，`查看`，`在浏览器中查看`，執行魔方平臺
* 系統為`MySql`/`SQLite`/`Oracle`/`SqlCe`資料庫自動下載匹配（`x86/x64`）的資料庫驅動檔案，驅動下載地址可在`Config\Core.config`中修改`PluginServer`
* 系統自動下載指令碼樣式表等資原始檔，下載地址可在`Config/Cube.config`中修改`PluginServer`
* 預設登入使用者名稱是`admin`，密碼是`admin`，也可以使用`NewLife`等第三方OAuth登入，首個進入系統的使用者搶得管理員，原`admin`停用
* 專案釋出時只需要複製`*.dll`、`appsettings.json`、`*.deps.json`、`*.runtimeconfig.json`，以及其它自己新增的資原始檔

---
## ASP.NET MVC 安裝
* 在 *Visual Studio* 中新建`ASP.NET MVC`專案
* 通過 *NuGet* 引用`NewLife.Cube`，或自己編譯最新的[魔方 NewLife.Cube](http://github.com/NewLifeX/NewLife.Cube)原始碼
* 在`Web.config`的`<connectionStrings>`段設定名為`Membership`的連線字串，使用者角色許可權選單等儲存在該資料庫
* 系統自動識別資料庫型別，預設`<add name="Membership" connectionString="Data Source=..\Data\Membership.db" providerName="Sqlite"/>`
* 編譯專案，專案上點選滑鼠右鍵，`查看`，`在浏览器中查看`，執行魔方平臺
* 系統為`MySql`/`SQLite`/`Oracle`/`SqlCe`資料庫自動下載匹配（`x86/x64`）的資料庫驅動檔案，驅動下載地址可在`Config\Core.config`中修改`PluginServer`
* 系統自動下載指令碼樣式表等資原始檔，下載地址可在`Config/Cube.config`中修改`PluginServer`
* 預設登入使用者名稱是`admin`，密碼是`admin`，也可以使用`NewLife`等第三方OAuth登入，首個進入系統的使用者搶得管理員，原`admin`停用
* 推薦安裝 *Visual Studio* 外掛 *Razor Generator*，給`.cshtml`檔案設定`自定义工具`為`RazorGenerator`，可以把`.cshtml`編譯生成到`DLL`裡面
* 專案釋出時只需要複製`Bin`、`web.config`、`Global.asax`，以及其它自己新增的資原始檔

## 新生命專案矩陣
各專案預設支援net9.0/netstandard2.1/netstandard2.0/net4.62/net4.5，舊版（2024.0801）支援net4.0/net2.0  

|                               專案                               | 年份  | 說明                                                                                        |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
|                             基礎元件                             |       | 支撐其它中介軟體以及產品專案                                                                  |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 核心庫，日誌、配置、快取、網路、序列化、APM效能追蹤                                         |
|    [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode)    | 2005  | 大數據中介軟體，單表百億級，MySql/SQLite/SqlServer/Oracle/PostgreSql/達夢，自動分表，讀寫分離 |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 網路庫，單機千萬級吞吐率（2266萬tps），單機百萬級連線（400萬Tcp長連線）                     |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011  | 協議通訊庫，提供CS應用通訊框架，支援Http/RPC通訊框架，高吞吐，物聯網裝置低開銷易接入        |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 魔方快速開發平臺，集成了使用者許可權、SSO登入、OAuth服務端等，單表100億級專案驗證               |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 服務管理元件，把應用安裝成為作業系統守護程序，Windows服務、Linux的Systemd                   |
|     [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero)     | 2020  | Zero零代腳手架，基於NewLife元件生態的專案模板NewLife.Templates，Web、WebApi、Service        |
|                              中介軟體                              |       | 對接知名中介軟體平臺                                                                          |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | Redis客戶端，微秒級延遲，百萬級吞吐，豐富的訊息佇列，百億級資料量專案驗證                   |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | RocketMQ純託管客戶端，支援Apache RocketMQ和阿里雲訊息佇列，十億級專案驗                     |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 物聯網訊息協議，MqttClient/MqttServer，客戶端支援阿里雲物聯網                               |
|      [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT)      | 2022  | IoT標準庫，定義物聯網領域的各種通訊協議標準規範                                             |
|   [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus)   | 2022  | ModbusTcp/ModbusRTU/ModbusASCII，基於IoT標準庫實現，支援ZeroIoT平臺和IoTEdge閘道器            |
|  [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens)  | 2022  | 西門子PLC協議，基於IoT標準庫實現，支援IoT平臺和IoTEdge                                      |
|      [NewLife.Map](https://github.com/NewLifeX/NewLife.Map)      | 2022  | 地圖元件庫，封裝百度地圖、高德地圖、騰訊地圖、天地圖                                        |
|    [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio)    | 2023  | 音訊編解碼庫，PCM/ADPCMA/G711A/G722U/WAV/AAC                                                |
|                             產品平臺                             |       | 產品平臺級，編譯部署即用，個性化自定義                                                      |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 星塵，分散式服務平臺，節點管理、APM監控中心、配置中心、註冊中心、釋出中心                   |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 螞蟻排程，分散式大數據計算平臺（即時/離線），螞蟻搬家分片思想，萬億級資料量專案驗證         |
|      [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP)      | 2021  | 企業ERP，產品管理、客戶管理、銷售管理、供應商管理                                           |
|         [CrazyCoder](https://github.com/NewLifeX/XCoder)         | 2006  | 碼神工具，眾多開發者工具，網路、序列埠、加解密、正規表示式、Modbus、MQTT                      |
|           [EasyIO](https://github.com/NewLifeX/EasyIO)           | 2023  | 簡易檔案儲存，支援分散式系統中檔案集中儲存。                                                |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 產品級反向代理，NAT代理、Http代理                                                           |
|        [HttpMeter](https://github.com/NewLifeX/HttpMeter)        | 2022  | Http壓力測試工具                                                                            |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | Git原始碼管理系統                                                                           |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 嵌入式作業系統，完全獨立自主，支援ARM Cortex-M晶片架構                                      |
|          [SmartA2](https://github.com/NewLifeX/SmartA2)          | 2019  | 嵌入式工業計算機，物聯網邊緣閘道器，高效能.NET8主機，應用於工業、農業、交通、醫療             |
|                          FIoT物聯網平臺                          | 2020  | 物聯網整體解決方案，建築、環保、農業，軟硬體及大數據分析一體化，單機十萬級點位專案驗證      |
|                        UWB高精度室內定位                         | 2020  | 釐米級（10~20cm）高精度室內定位，軟硬體一體化，與其它系統聯動，大型展廳專案驗證             |

## 新生命開發團隊
![XCode](https://newlifex.com/logo.png)  

新生命團隊（NewLife）成立於2002年，是新時代物聯網行業解決方案提供者，致力於提供軟硬體應用方案諮詢、系統架構規劃與開發服務。  
團隊主導的80多個開源專案已被廣泛應用於各行業，Nuget累計下載量高達400餘萬次。  
團隊開發的大數據中介軟體NewLife.XCode、螞蟻排程計算平臺AntJob、星塵分散式平臺Stardust、快取佇列元件NewLife.Redis以及物聯網平臺FIoT，均成功應用於電力、高校、網際網路、電信、交通、物流、工控、醫療、文博等行業，為客戶提供了大量先進、可靠、安全、高質量、易擴充套件的產品和系統整合服務。  

我們將不斷通過服務的持續改進，成為客戶長期信賴的合作伙伴，通過不斷的創新和發展，成為國內優秀的IoT服務供應商。  

`新生命团队始于2002年，部分开源项目具有20年以上漫长历史，源码库保留有2010年以来所有修改记录`  
網站：https://newlifex.com  
開源：https://github.com/newlifex  
QQ群：1600800/1600838  
微信公眾號：  
![智慧大石頭](https://newlifex.com/Stone.jpg)