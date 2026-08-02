# 测试规范

> 团队规范总纲见 [前端可测试渐进式开发与测试规范](../standards/frontend-testable-development.md)。本文是其测试部分的强制细则。

## 分层

| 层                 | 工具                 | 适用范围                                                       |
| ------------------ | -------------------- | -------------------------------------------------------------- |
| 单元/组件          | Vitest + jsdom       | 字段转换、composable、引擎纯工厂、请求辅助、组件渲染和边界条件 |
| 组件视觉测试（CT） | Playwright + Gallery | 真实浏览器渲染、组件交互、视觉回归截图对比                     |
| E2E                | Playwright           | 登录、菜单路由、默认 CRUD 和跨页面用户流程                     |
| 类型与静态检查     | vue-tsc、ESLint      | Vue/TS 类型、导入与代码质量                                    |

## 最低要求

- 改变纯函数、字段映射、请求/分页规则、引擎逻辑（`core/engine`）时，新增或更新 Vitest 测试。
- 改变组件 UI（template、CSS、props 行为）时，新增或更新对应 `*.ct.spec.ts` 截图测试。
- 改变页面交互、路由守卫、菜单路径或默认 CRUD 流程时，覆盖对应 Playwright E2E 场景；无法自动化时在 PR/变更说明记录人工验证步骤。
- 修复 bug 时，先补能复现该 bug 的最窄测试，除非环境确实无法建立。

## 命名与组织

- **Vitest 测试**：后缀 `*.spec.ts` 或 `*.test.ts`，放在 `core/__tests__/` 或与被测模块相邻。
- **Playwright CT 测试**：后缀 `*.ct.spec.ts`，放在组件目录（与 `*.story.ts` 并列）。
- **Story 定义**：后缀 `*.story.ts`，放在组件目录，声明组件 props 变体供 gallery 渲染。
- **E2E 测试**：`e2e/*.spec.ts`。
- 用例用 `describe('模块名', () => { it('应该…当…', () => {}) })`，描述业务行为而非技术实现。
- 遵循 AAA（Arrange/Act/Assert）；一个 `it` 只断言一个行为；多组数据用 `it.each`；禁止滥用 `toMatchSnapshot()`（尤其含动态类名的组件）。

## Mock 策略（关键）

- **不使用 MSW。** 本项目网络层 Mock 一律用 **`vi.mock` 模块桩 + 依赖注入**：
  - 纯工厂测试（`createCubeEngine(deps)`）直接注入假 `getPage`/`lookup`/`http`，不依赖真实 `usePageApi` 与配置。
  - 组件测试用 `vi.mock('@newlifex/cube-vue/core/utils/request')`、`vi.mock('@newlifex/cube-vue/core/configure')` 等桩掉外部依赖（参考 `core/components/LovSelectTable/LovSelectTable.test.ts`）。
- 引擎上下文通过 `CubeEngineKey` 注入或由 `CubeTable` 接收 `ctx` prop（mock `ctx`）验证插槽，无需真实后端。

## 组件视觉测试（CT）

### 架构：Gallery 模式

本项目采用自建 Gallery 模式（轻量 Storybook 替代），而非 `@playwright/experimental-ct-vue` 官方包。核心链路：

```
*.story.ts → ct/stories.ts (Vite glob 自动收集) → ct/gallery.html → Playwright *.ct.spec.ts → 截图对比
```

各文件职责：

| 文件                             | 职责                                                                            |
| -------------------------------- | ------------------------------------------------------------------------------- |
| `*.story.ts`                     | 声明组件 + props 变体（"组件在什么状态下"）                                     |
| `ct/stories.ts`                  | 自动收集所有 `*.story.ts`                                                       |
| `ct/gallery.html` + `ct/main.ts` | 渲染 gallery，暴露 `window.mountStory()` / `setStoryProps()` 供 Playwright 控制 |
| `ct/vite.config.ts`              | 独立 Vite 配置（端口 5190），mock 掉后端 API，组件无需后端即可渲染              |
| `ct/mocks/`                      | 统一网络层 mock（lov-api、request、configure）                                  |
| `*.ct.spec.ts`                   | Playwright 截图测试，驱动 gallery 挂载组件并截图                                |

### 与官方 CT 方案的对比

| 维度         | 官方 `@playwright/experimental-ct-vue` | 本项目 Gallery 模式                          |
| ------------ | -------------------------------------- | -------------------------------------------- |
| 挂载方式     | `mount(MyComponent, { props })`        | `page.evaluate(() => window.mountStory(id))` |
| 组件导入     | 测试文件直接 import                    | 在 `*.story.ts` 中注册，通过 story ID 引用   |
| Mock 策略    | 每个测试写 `page.route()`              | 统一在 `ct/mocks/` 管理                      |
| Story 可复用 | ❌ 每个测试自己 mount                   | ✅ `.story.ts` 供截图+单元测试共用            |
| 额外依赖     | `@playwright/experimental-ct-vue`      | 零额外依赖                                   |

**结论**：Gallery 模式做到了官方 CT 的所有能力（真实浏览器渲染、截图对比、props 传递），还多了 Story 复用和 mock 统一管理的好处。保持现有方案，不引入官方 CT。

### 开发循环

```
改组件源码 → pnpm test:ct:dev (有头截图，不对比) → 看浏览器窗口 → 不满意继续改
                                                      ↓ 满意
                                              pnpm test:ct:update (确立基线)
                                              pnpm test:ct (以后回归保护)
```

### 详细流程

见 [guides/component-visual-dev.md](../guides/component-visual-dev.md)（开发期预览 vs 回归基线）。

## 覆盖率门槛

新增 `@vitest/coverage-v8`（见 `frontend-testable-development.md` §5）。`vitest.config.unit.ts` 设定 v8 硬性指标：

- statements ≥ 80、branches ≥ 75、functions ≥ 80、lines ≥ 80。
- 排除：`node_modules/`、类型声明、`core/main.ts`、测试文件自身。
- 占比目标：单元测试 60% / 组件测试 30% / 集成测试 10%（详见总纲 §4 测试矩阵）。
- CT 截图测试不计入覆盖率（运行在真实浏览器，非 jsdom）。

## 命令

```powershell
# Vitest 单元/组件测试
pnpm run test:unit          # vitest run --config vitest.config.unit.ts
pnpm run test:coverage      # 待安装 @vitest/coverage-v8 并新增脚本后可用（当前未装，见 operations/build-release.md）

# Playwright 组件视觉测试（CT）
pnpm run test:ct            # 无头模式，截图对比基线
pnpm run test:ct:headed     # 有头模式，肉眼观察
pnpm run test:ct:update     # 更新基线截图（确认修改后）
pnpm run test:ct:dev        # 有头 + 截图存档（不对比，开发循环用）
pnpm run ct:server          # 仅启动 gallery（手动浏览器预览）

# Playwright E2E
pnpm run test:e2e           # Playwright
pnpm run test:e2e:ui        # Playwright UI 模式

# 静态检查
pnpm run type-check         # vue-tsc
pnpm run lint:eslint        # ESLint
```

`vitest.config.unit.ts` 使用最小 Vite 配置与 `jsdom`，避免全量框架插件干扰测试；测试 alias 为 `@newlifex/cube-vue`（非 `@`）。  
CT 配置见 `playwright-ct.config.ts`，独立于 E2E 的 `playwright.config.ts`。

## CI 卡点

PR 准入执行 `pnpm run check && pnpm run test:coverage`；新增代码覆盖率低于上述门槛或导致整体覆盖率下降，PR 自动打回。**注意：`test:coverage` 脚本与 `@vitest/coverage-v8` 当前尚未安装（见 [构建与发布](../operations/build-release.md)），该门禁待工具落地后生效。** CT 截图测试（`pnpm run test:ct`）可选接入 CI 作为组件级防回归卡点。详见 [构建与发布](../operations/build-release.md) 的 CI/CD 卡点章节。
