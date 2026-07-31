# OSC-0001 Design

## 技术方案

### 1. Vite 代理

在 [`NewLife.Cube.ArcoVue/web/vite.config.ts`](../../../web/vite.config.ts) 的 `server.proxy` 增加：

- `/Auth` → `http://localhost:5000`（与现有 Admin/Cube/Sso/api 一致）
- `/Mfa` → `http://localhost:5000`

依据：

- `/Auth/*`：[核心接口架构](../../../../Doc/Api/核心接口架构.md) 必要认证
- `/Mfa/*`：[认证接口设计](../../../../Doc/Api/认证接口设计.md) AUTH-10（不在核心接口最小集，但 `@cube/api-core` / auth-logic 会调用）

### 2. 文档

在核心接口架构 §2.2 高级接口表增加一行，例如：

| MFA 二步验证 | * | `/Mfa/*` | 详见 [认证接口设计.md](./认证接口设计.md)；非本文件最小集 |

不复制 MFA 请求体全文，避免双源漂移。

### 3. UseArcoVue 冒烟

- 现状：`CubeDemo/Program.cs` 为 `UseVue`，`UseArcoVue` 已注释。
- 本变更：**不切换仓库默认皮肤**；在 ArcoVue `web/README.md` 或本 OSC verify 写明切换步骤：`app.UseArcoVue(env)`、端口 5183、`pnpm dev`。
- 若执行环境允许，可临时切换验证后恢复，并在 verify 记录。

### 4. OpenSpec 自检

确认 `openspec/agents/openspec-*.agent.md` 与状态机文档一致（Implementing/Validating/Done/Rejected）。

## 规格与界面

- ui/：无

## 核心文档影响（必填）

| 文档路径 | 影响类型 | 说明 |
|----------|----------|------|
| Doc/Api/核心接口架构.md | 修改 | 高级接口增加 MFA 交叉引用 |
| NewLife.Cube.ArcoVue/web/README.md | 修改或无 | 补充代理路径与 UseArcoVue 开发说明（若尚未写清） |
| Doc/Api/ArcoVue企业中后台迁移方案.md | 无 | 已描述 OSC-0001；无需为本变更再改除非验收发现偏差 |
| Doc/功能清单.md | 无 | 无新功能编码 |

## 测试设计

| 项 | 方法 | 落点 |
|----|------|------|
| proxy 配置 | **Vitest** 断言 `DEV_PROXY_PREFIXES` / `createDevProxy` 含 `/Auth`、`/Mfa` | `web/devProxy.ts` + `devProxy.spec.ts` |
| 前端构建 | `pnpm build` 无 error | ArcoVue/web |
| 文档交叉引用 | 人工/检索 `/Mfa` | 核心接口架构.md |
| 登录冒烟 | 后端可用时 POST /Auth/Login 经代理或浏览器 | 可选 |

硬门禁：触及 `vite.config.ts`（前端代码）→ 执行期必须跑上述单元测试；验收期新增单测全过 + 构建无错误。
