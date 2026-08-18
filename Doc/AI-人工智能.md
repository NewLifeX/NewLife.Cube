# AI 人工智能

魔方内置 AI 能力，通过 `IAIService` 抽象层对接大语言模型，提供对话助手、系统健康诊断、日志摘要、安全周报等智能能力。AI 能力统一受 `AISwitch` 总开关控制，可在系统设置（魔方设置）中配置服务商、模型与密钥。

## 能力总览

| 能力 | 入口 | 前端入口 | 底层方法 | 状态 |
|------|------|----------|----------|------|
| AI 对话助手（含工具调用） | `AiController` `/Ai/AiChat`（SSE） | 右下角悬浮球：`_AiAssistant.cshtml`（MVC）/ `AiAssistant.vue`（Vue） | `IAIService.Client` + NewLife.AI `AiChatService` | ✅ 生产核心 |
| 系统健康诊断 | `IndexController.AiDiagnose`（SSE） | 首页「AI 诊断」按钮：`ai-insight.js` `CubeAI.diagnose()` | `IAIService.DiagnoseSystemStreamAsync` | ✅ 在用 |
| 日志摘要（定时） | `AILogSummaryJob`（CronJob，默认禁用） | 无（后台任务） | `IAIService.ChatAsync` | ✅ 可用 |
| 安全周报（定时） | `AISecurityReportJob`（CronJob，默认禁用） | 无（后台任务） | `IAIService.ChatAsync` | ✅ 可用 |

> 所有用户交互场景（对话、诊断）均采用 SSE 流式输出，即时反馈；后台定时任务采用非流式 `ChatAsync`。

## 配置项

AI 相关配置集中在 `CubeSetting`（`Setting.cs`）的「AI」分类，可在系统设置中修改，运行期即时生效：

| 配置 | 默认值 | 说明 |
|------|--------|------|
| `AISwitch` | `false` | AI 总开关。关闭时所有 AI 端点返回"AI 未启用" |
| `AIProvider` | `NewLifeAI` | 服务商。支持 NewLife / Ollama / DeepSeek / DashScope / OpenAI 等 |
| `AIEndpoint` | `https://ai.newlifex.com` | 服务地址 |
| `AIApiKey` | `sk-CubeAI2026` | API 密钥 |
| `AIModel` | `newlife-flash` | 默认模型 |
| `AIDefaultThink` | `false` | 默认深度推理（think）开关 |
| `AIColorScheme` / `AIPrimaryColor` / `AIPrimaryColor2` | 新生命绿 | AI 助手主题色 |

> 服务商注册表：`AiClientRegistry.Default`。未注册的服务商直接抛异常，不静默回退 OpenAI 兼容，避免掩盖协议不匹配。

## AI 对话助手（核心能力）

所有页面的 AI 对话统一收敛到全局端点 `/Ai/AiChat`（`AiController`，无区域前缀），不再为每个页面重复实现对话端点。浏览器操作回传亦放本控制器（`/Ai/OperationResult`）。

### 数据流

```
右下角悬浮球 → 对话面板（_AiAssistant.cshtml / AiAssistant.vue）
  → POST /Ai/AiChat（携带 area/controller 目标页面标识 + 会话消息）
  → AiController 解析目标控制器（IEntityAiContext / IFormAiContext / IPageDataContext 能力检测）
  → 注册工具（内置 + 联网 + 页面数据 + 浏览器操作）
  → NewLife.AI AiChatService 编排（会话历史 + 工具循环 + 空响应兜底）
  → SSE 事件流回前端（文本 / 工具调用事件 / run_js 下发）
```

### 工具链

| 工具服务 | 说明 |
|----------|------|
| `BuiltinToolService` | 内置工具（get_current_time / calculate 等） |
| `SystemInfoToolService` | `get_system_info`，`GetSystemInfo` 为 virtual 可重写 |
| `NetworkToolService` | 联网工具（网页抓取 / 搜索 / 天气 / 翻译 / IP 定位），免费无需密钥 |
| `PageDataContextToolService` | `get_page_context`：目标控制器实现 `IPageDataContext` 走服务端取数，否则浏览器采集兜底 |
| `BrowserToolService` | `run_js` 等浏览器操作，经页面检查点服务下发脚本到前端执行 |
| `CubeTools<TEntity>` | 实体页工具（get_data_context / get_form_schema / fill_form） |
| `ConfigFormToolService` | 配置表单页「帮我填表」（get_form_schema / fill_form） |

### 扩展点（二次开发）

| 能力接口 | 实现者 | 能力 |
|----------|--------|------|
| `IEntityAiContext` | `ReadOnlyEntityController<TEntity>`（partial 声明） | 实体页数据上下文、工具工厂、系统提示词 |
| `IFormAiContext` | `ConfigController<TConfig>` | 配置表单页填表 |
| `IPageDataContext` | 任意控制器 | 非实体页服务端数据上下文（服务器信息 / 数据库信息 / 魔方设置等） |

### 前端

- MVC：`_AiAssistant.cshtml` + `wwwroot/js/ai-assistant.js`。服务端注入 `data-ai-url=/Ai/AiChat` + `data-ai-area` / `data-ai-controller`
- Vue：`NewLife.Cube.Vue/web/core/components/ai/AiAssistant.vue`，由 `props.url` 解析 area/controller

## 系统健康诊断

`IndexController.AiDiagnose`（`[HttpGet]`，`EntityAuthorize(Detail)`）以 SSE 流式输出诊断报告。首页「AI 诊断」按钮经 `ai-insight.js` 的 `CubeAI.diagnose()` 调用，弹窗打字机效果展示。

- 输入：服务器运行指标（CPU / 温度 / 内存 / 工作集 / 运行时长 / 24h 错误数 / OS / 机器名）
- 输出：SSE 事件流（`meta` / `text` / `done`）
- 权限：需 `PermissionFlags.Detail`

## 定时任务

| 作业 | Cron | 默认 | 说明 |
|------|------|------|------|
| `AILogSummaryJob` | `0 0 8 * * ? *`（每天 8 点） | 禁用 | 汇总过去 24h 系统日志，AI 生成摘要并推送 |
| `AISecurityReportJob` | `0 0 9 ? * MON`（每周一 9 点） | 禁用 | 分析 OAuth 登录记录，AI 生成安全态势周报并推送 |

两个作业均继承 `CubeJobBase`，走 `ChatAsync` 非流式，推送结果经通知系统（`NotificationRecord`）。可在管理后台「系统管理 → 定时作业」中启用。

## 测试

- 单元测试（XUnitTest）：`AiDataHelperTests`、`AiFormHelperTests`、`AiInsightHelperTests`、`IEntityAiContextTests`、`IFormAiContextTests`、`PageDataContextToolTests`、`PageCheckpointServiceTests`、`CubeToolsOverrideTests`
- E2E（E2EMvcTest）：`AiAssistantTests`、`AiBasicFeaturesTests`、`AiInsightTests`、`AiPageContextTests`

## 演进记录（已移除）

| 已移除 | 说明 |
|--------|------|
| `IAIService.AnalyzeDataAsync / AnalyzeDataStreamAsync` | 无调用者后删除，数据洞察改由 `get_data_context` 工具提供 |
| `IAIService.ChatAgentStreamAsync / ChatAgentAsync` | 无调用者后删除，工具对话编排已由 NewLife.AI `AiChatService` 承担 |
| `IAIService.PolishNotificationAsync` + `NotificationRecordController.AiPolish` | 后端端点无前端入口，删除 |
| `IAIService.DiagnoseSystemAsync`（非流式） | 前端统一走流式，`AiDiagnose` 去掉 `stream` 参数 |
| `AiInsight` Action / `AiInsightDrawer.vue` / `AiPageControllerBase` / `AIVisibleAttribute` | 均已合并或删除，见架构设计.md |
