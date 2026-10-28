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
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

魔方是一個快速 Web 開發平臺，面向“資料驅動 + 快速迭代 + 可深度定製”場景。幾分鐘內即可把實體 / 資料庫表轉為帶許可權、列表、詳情、增刪改、匯出、統計的完整後臺介面，並開放標準 API 給 Vue / Blazor / Antd / 移動端或第三方系統呼叫。預設模板已在真實專案中經歷單表 100 億資料增刪改查考驗。

演示站點：<https://cube.newlifex.com> `CentOS7 + CDN` · SSO中心：<https://sso.newlifex.com> `OAuth 服務端` · Swagger：<https://cube3.newlifex.com>  

魔方教程：<https://newlifex.com/cube> · XCode教程：<https://newlifex.com/xcode> · 核心庫教程：<https://newlifex.com/core>

> ℹ️ 本繁體中文版與 [English README](README.md) 同步維護；若兩者內容不一致，以英文版為對外口徑。

**目錄**：[快速開始](#快速開始) · [主要功能](#主要功能) · [生態版圖](#生態版圖) · [文件與示例](#文件與示例) · [參與貢獻](#參與貢獻) · [新生命開發團隊](#新生命開發團隊) · [許可證](#許可證)

## 快速開始

### 安裝

通過 NuGet 引入 `NewLife.Cube.Core`。MIT 協議，可私有化替換字串“NewLife”。支援 VS 與 CLI。`newlife.snk` 已公開便於強命名一致性。

```bash
dotnet add package NewLife.Cube
```

### 模板腳手架

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

訪問 `/` 登入後臺；Swagger 檢視介面；前端可呼叫標準 CRUD API。

### 平臺支援

| 專案 | 覆蓋範圍 |
| --- | --- |
| 目標框架 | 單包 5 個：net6.0、net7.0、net8.0、net9.0、net10.0 |
| 系統與架構 | Windows、Linux、macOS；x64、x86、arm64、arm、loongarch64、riscv64、s390x |
| 資料庫 | MySql / SQLite / SqlServer / Oracle / PostgreSql / SqlCe / Access / 達夢 |
| NativeAOT | 非 MVC 底座 18 個冒煙場景（JIT/AOT 雙跑），當前數量見上方徽章 |

### ASP.NET Core 安裝

* 在 *Visual Studio* 中新建`ASP.NET Core Web`專案
* 通過 *NuGet* 引用`NewLife.Cube.Core`，或自己編譯最新的[魔方 NewLife.CubeNC](http://github.com/NewLifeX/NewLife.Cube)原始碼
* 在`appsettings.json`的`ConnectionStrings`段設定名為`Membership`的連線字串，使用者角色許可權選單等儲存在該資料庫
* 系統自動識別資料庫型別，預設`Data Source=..\Data\Membership.db`
* 編譯專案，專案上點選滑鼠右鍵，`查看`，`在浏览器中查看`，執行魔方平臺
* 系統為`MySql`/`SQLite`/`Oracle`/`SqlCe`資料庫自動下載匹配（`x86/x64`）的資料庫驅動檔案，驅動下載地址可在`Config\Core.config`中修改`PluginServer`
* 系統自動下載指令碼樣式表等資原始檔，下載地址可在`Config/Cube.config`中修改`PluginServer`
* 預設登入使用者名稱是`admin`，密碼是`admin`，也可以使用`NewLife`等第三方OAuth登入，首個進入系統的使用者搶得管理員，原`admin`停用
* 專案釋出時只需要複製`*.dll`、`appsettings.json`、`*.deps.json`、`*.runtimeconfig.json`，以及其它自己新增的資原始檔

### ASP.NET MVC 安裝

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

## 主要功能

<details>
<summary><b>資料到介面生產力</b>（9 項）</summary>

| 功能 | 關鍵特性 |
| --- | --- |
| EntityController 腳手架 | 實體類 + 控制器 = CRUD / 統計 / 匯出 / 許可權 即刻可用；只讀列表用 `ReadOnlyEntityController`。 |
| 靜態構造定製 | 通過控制器靜態建構子配置欄位、操作按鈕、關聯跳轉、統計、匯出。 |
| 欄位後設資料介面 | `meta`（頁面設定 + 列表 / 詳情 / 新增 / 編輯 / 搜尋欄位）驅動動態前端生成，無需硬編碼。 |
| 選單樹 | `/Cube/MenuTree` 按許可權生成多級選單；傳統 MVC 介面使用 `/Admin/Index/GetMenuTree`。 |
| 統一響應 | `ApiResponse` 統一包裝 code / data / page / stat。 |
| 免部署 | 首次執行自動建立資料庫表結構與初始化資料，無需人工干涉。 |
| 強大檢視引擎 | 子專案同路徑同名 `.cshtml` 即可覆蓋父專案檢視，任意定製預設介面。 |
| 內建編輯器 | 富文字（Quill）與 Markdown（EasyMDE）雙編輯器，支援圖片上傳與安全控制。 |
| Scalar 介面文件 | 整合 Scalar 線上文件，配合 Swagger 按區域自動分組，介面線上除錯更便捷。 |

</details>

<details>
<summary><b>欄位與介面定製</b>（3 項）</summary>

| 功能 | 關鍵特性 |
| --- | --- |
| 欄位集合定製 | `ListFields.RemoveField` / `AddListField` 按控制器調整列、連結與開啟方式。 |
| 修改日誌 | `LogOnChange = true` 為每次修改寫入審計記錄。 |
| 前端動態欄位 | 前端呼叫 `/Cube/App/meta` 獲取頁面後設資料與欄位定義，無需硬編碼。 |

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
<summary><b>API 與前後端分離</b>（6 項）</summary>

| 介面 | 用途 |
| --- | --- |
| `/Auth/Login` | 登入並獲取 JWT Token（Header / Cookie / Query）。 |
| `/Cube/MenuTree` | 按許可權生成選單樹。 |
| `/{Area}/{Controller}/meta` | 列表 / 詳情 / 新增 / 編輯表單的欄位後設資料。 |
| 主路由 + 動態引數 | 支援分頁、排序與條件拼裝查詢。 |
| `GET {id}` / `POST` / `PUT {id}` / `DELETE {id}` | RESTful 子資源式增刪改查；基底路徑批次刪除，另有 `charts` / `export` / `import` / `attachments` 子資源。 |
| `stat` / `page` | 統計行與分頁資訊。 |

</details>

<details>
<summary><b>許可權與安全</b>（7 項）</summary>

| 功能 | 關鍵特性 |
| --- | --- |
| 通用許可權管理 | 使用者、角色、選單、許可權，支援控制器 Action 粒度控制。 |
| EntityAuthorize | 每個動作執行登入 + 角色 + 選單 Action 校驗。 |
| 首個使用者即管理員 | 首個進入系統的使用者搶得管理員，原 `admin` 停用；首次可用 `admin/admin` 登入。 |
| 統一身份 | 支援外部 OAuth / SSO / JWT，多端統一身份。 |
| 安全防禦 | 內建威脅檢測（掃描器 / 注入 / 蜜罐）、持久化自動封禁（階梯時長）與安全事件審計。預設自動模式：連續攻擊自動封禁、大範圍攻擊臨時攔截並自動回落；可配置或自動學習可信代理防偽造；可一鍵切換觀察 / 攔截模式。 |
| 生產建議 | 關閉未用第三方登入、啟用 HTTPS、加強密碼策略。 |
| 多租戶開關 | 多租戶能力可一鍵開關，關閉後隱藏租戶選單與欄位，支援執行時租戶切換。 |

</details>

<details>
<summary><b>AI 能力</b>（4 項）</summary>

| 功能 | 關鍵特性 |
| --- | --- |
| AI 對話助手 | 右下角對話面板統一入口，上下文感知（當前頁面 / 實體 / 查詢）。全域性 `AiController.AiChat` 端點（`/Ai/AiChat`）+ NAI `AiChatService` 編排（會話歷史 `ChatSessionService` 保持 / 工具迴圈 / SSE 事件流）。 |
| 擴充套件點 | 實體控制器經 `IEntityAiContext` 能力介面向全域性端點暴露資料查詢 / 工具集 / 提示詞過載點（`SearchData` / `CreateCubeTools` / `BuildChatSystemPrompt`）。工具集含 `get_data_context`、`get_form_schema`、`fill_form`、`get_page_context`、`get_system_info` 與 `run_js`。 |
| AI 洞察 | 原 `AiInsight` 獨立端點已合併到對話助手，資料收集保留 `AiInsightHelper`（14 類敏感欄位黑名單）。一鍵 AI 分析提供自動統計、異常檢測、趨勢預判與可執行建議，支援深度推理，MVC / Vue 雙端適配。 |
| 工作臺 Widget | 工作臺 / 首頁由可配置部件（KPI、圖表、監控）組成，支援個性化佈局與 `/Cube/Widget` 部件管理。 |

</details>

<details>
<summary><b>前端、主題與皮膚</b>（7 項）</summary>

| 功能 | 關鍵特性 |
| --- | --- |
| 多前端共存 | Vue / Blazor / AntDesign / 傳統 Razor 可漸進遷移。 |
| 主題內嵌資源 | `CubeEmbeddedFileProvider` 把主題靜態資源打包進程式集。 |
| 皮膚註冊 | `AddTheme/AddSkin` 註冊皮膚，Use 階段任選其一（`app.UseMetronic(env)` 等）。 |
| 自定義主題 | 仿照 `MetronicModule` 實現 IModule 並在 Use 階段掛載。 |
| 檢視覆寫 | 子專案同路徑同名 `.cshtml` 覆蓋父級。 |
| 多語言 | 11 種執行時語言用於介面訊息，按請求識別區域，並提供 11 語言 Vue 前端。 |
| 新一代皮膚包 | 基於 pnpm workspace + 公共模組 `@newlifex/cube`（統一 API / Token / 型別、欄位後設資料 → 控制元件對映），8 套皮膚開箱即用，NuGet 引包即得完整前端。 |

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

前端 workspace 構建約定：

- 倉庫根目錄 `pnpm build`：僅構建 `packages/` 下公共模組，不再預設遞迴編譯所有皮膚。
- 倉庫根目錄單皮膚構建：使用 `pnpm build:vue`、`pnpm build:naive`、`pnpm build:mui`、`pnpm build:shadcn`、`pnpm build:arco`、`pnpm build:angular`、`pnpm build:vuetify`、`pnpm build:svelte`、`pnpm build:tdesign`。
- 倉庫根目錄單皮膚開發：使用對應的 `pnpm dev:xxx`。
- 需要一次性構建或啟動全部前端時，顯式使用 `pnpm build:all` 或 `pnpm dev:all`。
- 若已進入具體皮膚目錄 `web/`，繼續使用該目錄下的 `pnpm build` / `pnpm dev` 即可，互不干擾。

</details>

<details>
<summary><b>多資料庫與大數據</b>（5 項）</summary>

| 功能 | 關鍵特性 |
| --- | --- |
| XCode 資料層 | 實體同步建表、欄位演進、自動分表、讀寫分離、二級快取。 |
| 多資料庫 | MySql / SQLite / SqlServer / Oracle / PostgreSql / SqlCe / Access / 達夢。 |
| 三庫分離 | 示例：Membership（許可權）/ Cube（業務）/ Log（日誌）。 |
| PluginServer | 首次執行自動下載資料庫驅動與前端資源，可自定義地址。 |
| 規模驗證 | 真實專案單表 100 億資料增刪改查。 |

</details>

<details>
<summary><b>效能與 NativeAOT</b>（4 項）</summary>

| 功能 | 關鍵特性 |
| --- | --- |
| 統一耗時記錄 | 過濾器統一記錄耗時，易接入 Stardust APM。 |
| 低成本日誌 | 日誌判級避免無謂字串拼接。 |
| 多 TFM 最佳化 | 多目標框架允許在高版本啟用 Span / ValueTask 最佳化。 |
| NativeAOT | 非 MVC 底座 18 個冒煙場景，CI 中按 JIT / AOT 雙跑。 |

</details>

<details>
<summary><b>部署與運維</b>（5 項）</summary>

| 場景 | 做法 |
| --- | --- |
| 單機 | `dotnet publish` 後複製 dll + 配置即可。 |
| 服務 | 結合 `NewLife.Agent` 安裝為 Windows Service 或 systemd。 |
| 叢集 | 釋出後打 zip 包，通過星塵代理 `StarAgent` 拉起守護，或通過星塵平臺釋出。 |
| 容器 | 簡單 Dockerfile（複製釋出目錄，設定環境變數）。 |
| 橫向擴充套件 | 無狀態多例項，共享資料庫 & 快取。 |

#### 快速部署使用者中心

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

</details>

<details>
<summary><b>第三代魔方</b>（5 項）</summary>

| 事項 | 說明 |
| --- | --- |
| 聚焦 | 前後端分離 + 多前端生態 + 最小侵入。 |
| 後端 | 介面合併在 master（`NewLife.Cube`）；MVC 保留在 `NewLife.CubeNC`。 |
| 前端 | 倉庫獨立（Vue / QuickVue / Antd / Blazor）。 |
| 目標藍圖 | 現代化 UI + 保留覆寫機制；多框架前端（Vue/React/Angular/Svelte + 8 套開箱即用皮膚包）；NuGet 引包即得預設皮膚；移動端 & 小程式 & 混合應用；資料大屏增強。 |
| 待辦任務 | <https://github.com/orgs/NewLifeX/projects/1> |

前端倉庫：  
https://github.com/NewLifeX/NewLife.CubeVue · https://github.com/NewLifeX/NewLife.QuickVue · https://github.com/NewLifeX/NewLife.CubeAntd · https://github.com/NewLifeX/NewLife.CubeBlazor

線上站點：Vue <https://vue.newlifex.com>、<https://quickvue.newlifex.com> · Antd <https://antd.newlifex.com> · Swagger <https://cube3.newlifex.com/swagger/index.html>

專案參與須知：

1. 加入 NewLifeX 團隊，向 dev 分支提交。
2. WebApi 後臺：`NewLife.Cube`；MVC 保留在 `NewLife.CubeNC`。
3. 新前端獨立倉庫（如 `NewLife.CubeAntd`）。
4. 文件/程式碼標註負責人。
5. 待辦任務：https://github.com/orgs/NewLifeX/projects/1 。

春雨計劃（里程碑摘錄）：

- [*] 新建 WebApi 專案 `NewLife.Cube`
- [*] 遷移 NetCore 專屬程式碼到 `NewLife.CubeNC`
- [*] 新版 `EntityController` / `EntityReadonlyController`
- [*] 介面文件 & Vue 登入主頁
- [] Vue 使用者/角色列表與表單

</details>

<details>
<summary><b>總體架構與子工程</b>（9 項）</summary>

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

| 子工程 | 職責 |
| --- | --- |
| NewLife.Cube | 核心（實體控制器、許可權、選單、欄位後設資料、API）。 |
| NewLife.CubeNC | 舊版 MVC / Razor 相容層。 |
| 經典皮膚 | AdminLTE / Metronic / Metronic8 / Tabler / LayuiAdmin / ElementUI / Blazor（各獨立包）。 |
| 新一代皮膚 | pnpm workspace：NaiveUI / MUI / Shadcn / ArcoVue / Angular / Vuetify / Svelte / TDesign。 |
| NewLife.Cube.Swagger | 按區域自動分組的 Swagger 文件。 |
| CubeDemo / CubeDemoNC | 示例。 |
| CubeSSO | 統一使用者中心 & OAuth 服務端。 |
| NewLife.Cube.Blazor | Blazor 前端實驗。 |
| Test / XUnitTest / E2EMvcTest / AotSmoke / WebSmoke | 測試與驗證（WebSmoke 在非 x64/arm64 指令集上啟動 Kestrel 並完成一次 HTTP 往返）。 |

</details>

<details>
<summary><b>核心概念</b>（7 項）</summary>

| 概念 | 說明 |
| --- | --- |
| EntityController | 通過靜態建構子配置欄位、操作按鈕、關聯跳轉、統計、匯出。 |
| 欄位後設資料介面 | `meta`（頁面設定 + 列表 / 詳情 / 新增 / 編輯 / 搜尋欄位）驅動動態前端生成。 |
| 選單樹 | `/Admin/Index/GetMenuTree` 根據許可權生成多級選單。 |
| 許可權模型 | 使用者 -> 角色 -> 選單（Action 集合）。首個使用者搶管理員，`admin` 被停用。 |
| ApiResponse | 標準響應包裝 code / data / page / stat。 |
| IModule | 啟動模組注入服務和靜態資源（主題、Swagger 等）。 |
| AI 對話助手 | 全域性 `AiController.AiChat` 端點（`/Ai/AiChat`）+ NAI `AiChatService` 編排（會話歷史 / 工具迴圈 / SSE 事件流）；實體控制器經 `IEntityAiContext` 暴露能力。`AiInsightHelper` 保留資料收集（14 類敏感欄位黑名單）。 |

</details>

<details>
<summary><b>常見使用場景</b>（5 項）</summary>

| 場景 | 典型用法 |
| --- | --- |
| 企業與部門管理 | 內部管理後臺、SaaS 管理面。 |
| IoT 運營 | 裝置 / 點位 / 日誌 / 告警管理與資料匯出。 |
| 資料中臺 | 快速釋出標準查詢介面。 |
| 身份中心 | 統一身份認證與授權（SSO / OAuth）。 |
| 運營支撐 | 訂單、配置、任務排程、版本釋出。 |

</details>

## 生態版圖

NewLife 生態系按相依分層，各專案獨立版本、獨立發行。下面的版圖在所有 NewLife 儲存庫中逐字一致；每個專案連結到程式碼儲存庫與主 NuGet 套件。

| 專案 | 一句話定位 | 下載量 |
| --- | --- | --- |
| **基礎底座** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | 日誌、設定、快取、網路、序列化、排程與 APM 追蹤。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 資料中介層：單表百億級、自動分表、讀寫分離，支援 10 多種資料庫。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 高效能網路庫：單機 177 萬 pps、14.5 Gbps（TCP 1KB，2026-10 實測），400 萬 Tcp 長連線。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 協定通訊與 RPC 框架，面向 CS 應用程式與物聯網裝置。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 將 .NET 應用程式安裝為作業系統常駐服務：Windows 服務與 Linux systemd。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new` 專案範本：主控台、Web、API、服務、資料與物聯網。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **資料儲存與訊息** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis 用戶端：微秒級延遲、百萬級吞吐，內建訊息佇列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 純受控 RocketMQ 用戶端，支援 Apache RocketMQ 與阿里雲訊息佇列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT 用戶端與服務端，含阿里雲物聯網接入。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | C# 混合式資料庫：內嵌或網路雙模，關係型、時序、訊息佇列與 KV 於一體。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | 建構於 NewLife 資料堆疊之上的 MySql 驅動與輔助層。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web、AI 與分散式** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 快速後台開發框架：權限、SSO、OAuth 服務端與 CRUD 樣板。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | 分散式服務平台：節點管理、APM、設定中心、註冊中心與發佈中心。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | 分散式任務與即時資料排程平台，支援分片、重試與回溯補算。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | 統一接入 46 家大模型服務商，附帶開箱即用的 ChatAI Web 對話應用。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **工業與物聯網** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 物聯網標準庫，定義通訊協定契約與裝置抽象。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | 建構於 NewLife.IoT 之上的 Modbus TCP/RTU/ASCII 實作。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 建構於 NewLife.IoT 之上的西門子 PLC 協定。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | 建構於 NewLife.IoT 之上的歐姆龍 PLC 協定。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | 建構於 NewLife.IoT 之上的三菱 Melsec PLC 協定。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | 工業資料存取的 OPC 用戶端。 | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet 樓宇自動化協定堆疊。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | 開源輕量級物聯網平台，含閘道與裝置管理。 | — |
| **車聯網** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 車輛定位協定：70+ 訊息體，涵蓋 2011/2013/2019 三個版本。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T 809 部標平台互聯協定，面向運輸管理部門。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 車載視訊：RTP 解析、FLV/fMP4 轉封裝與直播推送。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **領域元件** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | 一個套件涵蓋 Excel、Word、PPT、PDF 等 15+ 文件格式。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 跨平台音訊：編解碼、裝置 IO、DSP、容器與串流處理。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | 視訊編解碼、封裝格式、影格處理與串流。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 百度／高德／騰訊／天地圖統一介面，支援地理編碼與座標轉換。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 歸屬地：離線記憶體映射資料庫 + 線上 API 雙來源。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **工具與產品** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 開發者工具箱：網路、序列埠、加解密、正規表示式、Modbus 與 MQTT。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 自架 Git 原始碼管理系統。 | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | 從較舊的宿主啟動時，自動安裝相符的 .NET 執行階段。 | — |

> 徽章取各專案主套件；主題套件、擴充套件與衛星套件見對應 NuGet 頁面。未在表中列出的 30 多個套件（FTP、LoRa、Office 擴充、各類協定驅動等）見 [NewLifeX 組織](https://github.com/NewLifeX) 與 [newlifex.com](https://newlifex.com)。本表以 NewLife.Core README 為維護來源。

## 文件與示例

### 指南

詳細需求文件、功能清單、架構設計見 [`Doc/`](Doc/) 目錄（中文）：

- 📋 [需求文件](Doc/需求文档.md) — 願景、核心目標、功能需求
- ✅ [功能清單](Doc/功能清单.md) — 實現/測試/註釋三維狀態追蹤
- 🏗️ [架構設計](Doc/架构设计.md) — 分層架構、模組設計、關鍵決策
- 🤖 [AI 人工智慧](Doc/AI-人工智能.md) — AI 能力清單、配置與擴充套件點
- 🔔 [線上告警與站內信](Doc/ALM-在线告警与站内信.md) — 線上新高告警、系統級廣播站內信與未讀閉環
- ✉️ [郵箱/手機驗證](Doc/AUTH-邮箱手机验证.md) — 賬號啟用契約（註冊→啟用→登入），前端皮膚接入規範
- 🛡️ [安全防禦](Doc/PERM-安全防御.md) — 內建威脅檢測、自動封禁與安全事件審計
- 📚 [魔方使用者手冊](Doc/魔方用户手册.md) — 完整的使用者與開發手冊

### 教程與站點

- 魔方教程：<https://newlifex.com/cube>
- XCode 教程：<https://newlifex.com/xcode>
- 核心庫教程：<https://newlifex.com/core>
- 演示站點：<https://cube.newlifex.com> · SSO 中心：<https://sso.newlifex.com> · Swagger：<https://cube3.newlifex.com>

### FAQ 速覽

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

## 參與貢獻

### 構建與測試

魔方使用 .NET SDK 10（或目標框架對應的 SDK）即可編譯，也可用 Visual Studio 開啟；前端皮膚使用 pnpm。

```bash
dotnet build NewLife.Cube/NewLife.Cube.csproj -c Release
dotnet test XUnitTest -c Release
```

- 端到端 MVC 測試：`E2EMvcTest/`
- NativeAOT 冒煙：`AotSmoke/`，CI 見 `.github/workflows/aot.yml`
- Web 冒煙：`WebSmoke/` 啟動 Kestrel 並完成一次 HTTP 往返

### 發版

推送 `v` 字首標籤觸發 `.github/workflows/publish.yml`，工作流打包 `NewLife.Cube` / `NewLife.Cube.Core` 並發布到 NuGet.org。包內 README 由 `sync-readme-package.ps1` 從 `README.md` 生成，語言切換行由 `sync-readme-languages.ps1` 生成，指標徽章由 `sync-readme-metrics.ps1` 生成；三者均支援 `-Check` 供 CI 校驗。

### 反饋

- 規範：參考 `.github/copilot-instructions.md`。
- Issue / PR：描述場景、期望、復現；效能類附資料規模。
- 新主題：獨立倉庫 + README 說明構建方式。
- 優先複用 NewLife 生態，避免不必要依賴。
- 多 TFM 編譯通過後再提交。
## 新生命開發團隊

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

新生命團隊成立於 2002 年，二十餘年專注 .NET 與物聯網技術生態，主導開源專案近百個（生態版圖收錄 34 個主幹專案），為電力、物流、醫療、工控、網際網路等行業提供軟硬體一體化解決方案；以「學無先後達者為師」為信條。

開源元件在 NuGet 累計下載 <!--nuget-total:start-->6.48M<!--nuget-total:end--> 次（全生態口徑，截至 2026-10）。

網站：https://newlifex.com · 開源組織：https://github.com/NewLifeX

<details>
<summary>微信公眾號：智能大石头（掃碼關注）</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## 許可證

[MIT](https://github.com/NewLifeX/NewLife.Cube/blob/master/LICENSE) 協議；強名稱證書 `newlife.snk` 已公開，可自行編譯替換程式集，原始碼可修改與再分發且無需署名。

> 如果這個專案對你有幫助，歡迎 Star、分享給團隊，或通過 Issue/PR 提出改進建議。
