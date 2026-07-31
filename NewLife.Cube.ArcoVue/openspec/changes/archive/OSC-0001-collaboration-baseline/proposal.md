# OSC-0001 — 协作基线与通路

## 1. 为何做

打通 ArcoVue 产品化第一条变更链路：OpenSpec 状态机可跑、开发代理能打到 `/Auth` 与 `/Mfa`、文档标明 MFA 权威出处，避免后续登录/MFA 联调失败与文档歧义。

## 2. 范围（做）

- 确认 `NewLife.Cube.ArcoVue/openspec/` 五壳 Agent 可用（本变更自检）。
- ArcoVue Vite 开发代理补齐 `/Auth`、`/Mfa`（转发至后端，默认 `localhost:5000`）。
- [Doc/Api/核心接口架构.md](../../../../Doc/Api/核心接口架构.md) 高级接口表增加 MFA 交叉引用（权威定义仍在认证接口设计）。
- 记录 `UseArcoVue` 切换与登录冒烟步骤（verify）；可选在 CubeDemo 用注释说明如何切换皮肤（不强制改默认 `UseVue`，以免影响现有演示默认）。

## 3. 不做什么

- 不实现 UserProfile / EntityViewProfile / EntityComment。
- 不改 CRUD / VTable / 抽屉 / FlowGram。
- 不迁入 NewLife.Skills；不改写 Skills 或既有 `.github/instructions` 正文。
- 不整仓切换 CubeDemo 默认皮肤为 ArcoVue（仅文档/verify 冒烟说明）。

## 4. 依赖

- 无前置 OSC。
- 后端需本机可起（CubeDemo 或等价）以便代理冒烟；若环境不可用，verify 中标注风险并完成代理与文档项。

## 5. 验收 / 测试范围

| 类型 | 是否做 | 说明 |
|------|--------|------|
| 文档 diff 检查 | 是 | 核心接口架构含 MFA 交叉引用 |
| 配置检查 | 是 | `vite.config.ts` 含 `/Auth`、`/Mfa` proxy |
| **单元测试** | **是** | 触及前端代码：须新增 Vitest（断言 proxy 含 `/Auth`、`/Mfa`）；执行期跑通；验收期**新增单测全过** |
| **构建** | **是** | 验收期 `pnpm build`（ArcoVue web）**无错误** |
| 手工/冒烟 | 是 | 代理路径存在；若后端可用则尝试 Login 或至少确认代理命中 |
| XUnit | 否 | 本变更无后端代码 |
| E2E | 否 | |

> 修订（2026-07-31）：对齐硬门禁——前后端代码修改必须跑单元测试；验收须本阶段新增单测全过且构建无错误。

## 6. 成功标准

- [ ] `vite.config.ts` 代理含 `/Auth`、`/Mfa`
- [ ] 核心接口架构高级表含 MFA → 认证接口设计 的交叉引用
- [ ] 本 OSC 新增 Vitest 覆盖 proxy 关键键且执行/验收全过
- [ ] `pnpm build` 无错误
- [ ] OSC-0001 经批准→执行→验收→复盘状态机可走通（本变更为首跑样板）
- [ ] verify 记录冒烟结果或环境受限说明
