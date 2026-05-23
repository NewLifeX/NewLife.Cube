# NewLife.CubeNC MVC E2E 测试规划

> 版本：2026-05-23
> 适用范围：第二代魔方（NewLife.CubeNC），启动项目 CubeSSO

---

## 1. 背景与目标

第二代魔方（NewLife.CubeNC）基于 ASP.NET Core MVC 服务端渲染架构，虽然不再新增重大功能，但仍有数年维护期。为保障每次代码修改不引入回归，并为 Copilot 辅助修正提供可解析的失败报告，需要建立一套覆盖核心功能的 E2E 自动化测试体系。

**测试目标**：

1. 认证链路（注册、登录、注销、忘记密码、OAuth 跳转）无回归
2. 后台全部菜单页面可正常打开并显示有效数据
3. 用户信息相关的关键操作（修改、清空密码、导航栏）符合预期
4. 每次测试输出结构化报告，人类和 AI 均可阅读

**不在本测试范围内**：

- 第三代魔方（NewLife.Cube API 版）— 由 `XUnitTest/` 等独立项目覆盖
- 性能基准测试 — 由 `Benchmark/` 项目覆盖
- 单元测试 — 由 `XUnitTest/` 项目覆盖

---

## 2. 技术选型

### 2.1 浏览器自动化：Microsoft.Playwright for .NET

| 选项 | 选择原因 |
|------|---------|
| Microsoft.Playwright | 官方 .NET 支持，Chromium/Firefox/WebKit 三浏览器，内置截图/视频录制，selector API 丰富 |
| Selenium | 不选：API 较陈旧，异步支持弱，截图功能受限 |
| Puppeteer.Sharp | 不选：仅支持 Chromium，.NET 生态集成弱 |

### 2.2 测试框架：xUnit

与现有 `XUnitTest/` 保持一致，复用 xUnit 生态（`[Trait]` 分类、`IClassFixture` 共享状态、`[DisplayName]` 中文说明）。

### 2.3 编排脚本：.NET 10 file-based program

```csharp
// Test/e2e-mvc.cs — 顶部 directive 示例
#:sdk Microsoft.NET.Sdk
#:package Microsoft.Data.Sqlite 9.0.4
```

用户执行 `dotnet run Test/e2e-mvc.cs`，脚本负责：构建 → 启动 → 测试 → 报告 → 清理。不依赖 PowerShell 或 Makefile，跨平台一致。

> **为何不用纯脚本做 E2E**：Playwright 需要浏览器上下文生命周期管理、xUnit 提供并行控制和结构化报告，`dotnet test` 比脚本更完整。脚本仅负责编排，测试逻辑在 csproj 测试项目内。

### 2.4 报告格式

| 格式 | 工具 | 用途 |
|------|------|------|
| `.trx` (TRX) | `dotnet test --logger trx` | CI 解析、Copilot 定位失败用例 |
| `report.html` | Playwright Trace Viewer 或 自定义 XSLT | 人类浏览器查看，含截图嵌入 |
| 截图 | Playwright `Page.ScreenshotAsync` | 失败时自动保存到 `screenshots/` |

### 2.5 数据库验证：Microsoft.Data.Sqlite

测试结束后直接查询 `Bin/E2ETest/Run-.../app/Data/Membership.db`，验证数据库层面的预期变更（用户新增、密码清空、登录次数）。

---

## 3. 目录结构

```
NewLife.Cube/
├── Test/
│   ├── e2e-mvc.cs                   # .NET 10 编排脚本（一键运行入口）
│   └── E2EMvcTest/
│       ├── E2EMvcTest.csproj        # net10.0，Playwright + xUnit
│       ├── Fixtures/
│       │   ├── TestFixture.cs       # 启动 CubeSSO 进程 + Playwright 初始化
│       │   └── DatabaseHelper.cs   # SQLite 查询助手
│       ├── Tests/
│       │   ├── AuthTests.cs         # Session A — 认证用例
│       │   ├── MenuNavigationTests.cs  # Session B — 菜单导航用例
│       │   └── UserProfileTests.cs  # Session C — 用户信息用例
│       └── Helpers/
│           └── PageHelpers.cs       # 公共等待方法、选择器封装
├── Bin/
│   └── E2ETest/                     # 运行时自动创建（不提交 Git）
│       └── Run-20260523-143022/
│           ├── app/                 # CubeSSO publish 输出（纯净环境）
│           │   └── Data/            # SQLite 数据库
│           ├── screenshots/         # 失败测试截图
│           ├── report.html          # HTML 报告
│           └── results.trx          # TRX 报告
└── Doc/
    └── mvc/
        ├── README.md
        ├── E2E测试规划.md           # 本文件
        └── 测试用例清单.md
```

---

## 4. 测试环境隔离

每次测试使用全新的独立目录，避免上次测试残留数据干扰本次结果。

### 4.1 构建与输出目录

编排脚本在每次运行前：

1. 生成时间戳目录：`Bin/E2ETest/Run-{yyyyMMdd-HHmmss}/`
2. 删除旧目录（若存在同时间前缀的）
3. 执行 `dotnet publish CubeSSO/CubeSSO.csproj -o Bin/E2ETest/Run-.../app/`
4. 确保输出目录下的 `Data/` 为空（首次启动时魔方会自动建库建表）

### 4.2 端口冲突处理

CubeSSO 的 `appsettings.json` 配置监听端口：

```json
"Urls": "http://*:8080;https://*:8081"
```

编排脚本在启动前检测 8080 端口是否被占用，若占用则输出错误并退出，不强行启动。

### 4.3 就绪等待

进程启动后，编排脚本轮询 `GET http://localhost:8080/` 返回 200/302 为就绪信号，最多等待 30 秒，超时则标记为环境失败。

---

## 5. 会话分组策略

E2E 测试按业务语义分为三个会话组，同组内的测试共享浏览器上下文，跨组不共享。这样既减少浏览器启动开销，又保证测试之间的状态隔离。

### Session A — 认证全流程（22 个用例）

**会话特点**：从无状态开始，依次完成注册、多种登录方式、OAuth 跳转、注销、忘记密码。每个场景用完即清除 Cookie，保持后续场景的初始状态。

**关键验证链**：

```
注册(TC-AUTH-001~004)
  → 用户名/邮箱/手机/编码密码登录(TC-AUTH-010~018)
    → 密文传输验证(TC-AUTH-016)
      → NewLife OAuth全流程(TC-AUTH-020~024)
        → 注销(TC-AUTH-030~031)
          → 忘记密码(TC-AUTH-040~042)
```

### Session B — 后台菜单全遍历（47 个用例）

**会话特点**：以 admin 账号登录后保持会话，遍历全部左侧菜单项目，包括系统管理区（Admin 区 25 个控制器对应的菜单）和魔方管理区（Cube 区 9 个控制器对应的菜单）。

**菜单遍历策略**：

- 每个菜单点击后等待内容区 `.col-md-12` 或 `#panel` 区域出现
- 断言页面无 HTTP 5xx 响应
- 断言内容区非空（`innerText.length > 0`）
- 若有表格，断言至少有 `<thead>` 结构
- 若有搜索框，执行一次搜索并断言结果区刷新

**Admin 区菜单对应控制器清单**：

| 控制器 | 对应菜单路径 | 搜索验证 |
|--------|------------|---------|
| UserController | /Admin/User | Q + dtStart/dtEnd + 状态下拉 |
| RoleController | /Admin/Role | Q |
| MenuController | /Admin/Menu | Q |
| DepartmentController | /Admin/Department | Q |
| UserOnlineController | /Admin/UserOnline | dtStart/dtEnd |
| UserStatController | /Admin/UserStat | dtStart/dtEnd |
| UserTokenController | /Admin/UserToken | Q |
| UserConnectController | /Admin/UserConnect | Q |
| AccessRuleController | /Admin/AccessRule | Q |
| OAuthConfigController | /Admin/OAuthConfig | Q |
| OAuthLogController | /Admin/OAuthLog | Q + 应用下拉 |
| LogController | /Admin/Log | Q + dtStart/dtEnd |
| NotificationRecordController | /Admin/NotificationRecord | Q |
| MailConfigController | /Admin/MailConfig | — |
| SmsConfigController | /Admin/SmsConfig | — |
| DbController | /Admin/Db | — |
| ParameterController | /Admin/Parameter | Q |
| SysController | /Admin/Sys | — |
| FileController | /Admin/File | Q |
| TenantController | /Admin/Tenant | Q |
| TenantUserController | /Admin/TenantUser | Q |
| StarController | /Admin/Star | — |
| XCodeController | /Admin/XCode | — |
| CoreController | /Admin/Core | — |

**Cube 区菜单对应控制器清单**：

| 控制器 | 对应菜单路径 | 搜索验证 |
|--------|------------|---------|
| AppController | /Cube/App | Q |
| AppLogController | /Cube/AppLog | Q + dtStart/dtEnd |
| AppModuleController | /Cube/AppModule | Q |
| AttachmentController | /Cube/Attachment | Q |
| CronJobController | /Cube/CronJob | Q |
| PrincipalAgentController | /Cube/PrincipalAgent | Q |
| ModelTableController | /Cube/ModelTable | Q |
| ModelColumnController | /Cube/ModelColumn | Q |
| AreaController | /Cube/Area | Q |

### Session C — 用户信息专项（30 个用例）

**会话特点**：以 OAuth 登录的测试用户身份进入，专项测试用户信息页的 10 个导航标签、修改保存、清空密码流程（含多次注销重登）、登录统计变化。

**关键验证链**：

```
进入用户信息页(TC-USER-001~002)
  → 10个导航标签全打开(TC-USER-010~019)
    → 修改基本信息并DB验证(TC-USER-020~021)
      → 清空密码(TC-USER-030)
        → 注销 → 新密码登录成功(TC-USER-031)
          → 注销 → 旧密码登录失败(TC-USER-032)
            → 新密码登录成功确认(TC-USER-033)
              → 登录统计验证(TC-USER-040~042)
```

---

## 6. 测试账号与配置

### 6.1 默认账号

| 角色 | 用户名 | 密码 | 说明 |
|------|--------|------|------|
| 管理员 | admin | Admin@2025 | 初始 seed 账号，自动创建 |
| OAuth测试账号 | test | test | NewLife OAuth 服务端配置的测试账号 |

> 账号信息仅用于测试环境。正式部署时 CubeSSO 使用独立数据库，与生产数据无关。

### 6.2 NewLife OAuth 配置要求

测试前需确认 CubeSSO 数据库中 `OAuthConfig` 表有以下配置：

| 字段 | 值 |
|------|-----|
| Name | NewLife |
| AppId | （已配置） |
| AppSecret | （已配置） |
| Server | https://sso.newlifex.com 或本地 OAuth 服务地址 |
| Visible | true |

若本地无 OAuth 服务，TC-AUTH-020~024 相关用例标记为 `[Trait("Requires", "OAuthServer")]`，可通过 `--filter "Requires!=OAuthServer"` 跳过。

### 6.3 环境变量（可选覆盖）

| 变量名 | 默认值 | 说明 |
|--------|--------|------|
| `E2E_BASE_URL` | `http://localhost:8080` | 被测地址 |
| `E2E_ADMIN_USER` | `admin` | 管理员用户名 |
| `E2E_ADMIN_PASS` | `Admin@2025` | 管理员密码 |
| `E2E_OAUTH_USER` | `test` | OAuth 测试账号 |
| `E2E_OAUTH_PASS` | `test` | OAuth 测试密码 |
| `E2E_HEADLESS` | `true` | 是否无头模式 |

---

## 7. 测试失败断言规范

每个测试用例的失败断言必须包含以下信息，便于 Copilot 定位问题：

```csharp
// 示例：断言带上下文信息
Assert.True(
    await page.IsVisibleAsync(".table tbody tr"),
    $"[TC-MENU-001] 用户管理列表无数据行。" +
    $"当前URL: {page.Url}，" +
    $"页面标题: {await page.TitleAsync()}"
);
```

失败时自动截图，命名规范：`{TestId}-{yyyyMMddHHmmss}.png`，保存至 `screenshots/` 目录。

---

## 8. 数据库验证规范

部分用例需在浏览器操作后查询数据库确认数据变更：

```csharp
// 示例：验证注册后 User 表新增行
var before = DatabaseHelper.CountUsers(dbPath);
// ...执行注册操作...
var after = DatabaseHelper.CountUsers(dbPath);
Assert.True(after > before, $"[TC-AUTH-001] 注册后 User 表行数未增加（before={before}, after={after}）");
```

数据库路径在 `TestFixture.cs` 中根据当前运行目录自动解析：`{RunDir}/app/Data/Membership.db`。

---

## 9. 日志文件验证规范

魔方日志默认写入 `{RunDir}/app/Log/{yyyy}/{MM}/` 目录，文件名格式 `yyyyMMdd.log`。

```csharp
// 示例：验证日志中有登录成功记录
var logContent = DatabaseHelper.ReadLatestLog(runDir);
Assert.Contains("登录成功", logContent,
    $"[TC-AUTH-010] 日志文件中未找到'登录成功'记录");
```

---

## 10. CI 集成说明

编排脚本 `Test/e2e-mvc.cs` 支持以 `--ci` 参数运行，区别于本地模式：

- `--ci`：无头模式，报告输出到标准目录，失败时 exit code 非零
- 本地模式：默认有头（可见浏览器），便于调试

GitHub Actions 示例：

```yaml
- name: Run MVC E2E Tests
  run: dotnet run Test/e2e-mvc.cs -- --ci
```

---

## 11. 与现有测试的边界划分

| 测试类型 | 位置 | 范围 |
|---------|------|------|
| 单元测试 | `XUnitTest/` | RSA 加解密逻辑、URL 解析、菜单树算法等纯逻辑 |
| API 集成测试 | `XUnitTest/` + `WebApplicationFactory` | 接口响应格式、HTTP 状态码、认证 Token 格式 |
| **MVC E2E 测试** | `Test/E2EMvcTest/` | **浏览器端到端，MVC 视图渲染、表单提交、Cookie、页面跳转** |

三类测试相互不重叠：MVC E2E 专注于"用户视角看到的页面行为"，不重复覆盖纯逻辑层。

---

## 12. 参考文档

- [测试用例清单.md](测试用例清单.md)
- [README.md](README.md)
- [../测试/登录功能测试用例.md](../测试/登录功能测试用例.md)
- [../用户认证.md](../用户认证.md)
- [../OAuth与SSO.md](../OAuth与SSO.md)
