# AGENTS.md — Cube Vue 前端协作规范入口

你是本仓库前端（`web/`）的协作 Agent。本条文件**只做索引与硬约束**，不承载完整规则。

**动手写任何代码或文档前，先读 `web/docs/README.md`**（问题 → 文档分发表），按任务类型加载对应规范文件。不要凭记忆写规则，以 `docs/` 下对应文件为准；规则更新只改 `docs/`，不要回抄进本文件。

---

## 不可违反的硬约束（invariants）

1. **关注点分离**：`index.vue` 的 `<script setup>` ≤ 50 行；业务逻辑必须抽成 `*.logic.ts` 纯函数；模板与脚本不得写业务逻辑（`new Date()`、`fetch()`、复杂 `filter/map` 等）。→ `docs/standards/code-conventions.md`
2. **CSS 令牌规则**：设计令牌唯一来源 = Element Plus 的 `--el-*`；Tailwind 仅用于页面布局 / 间距 / 响应式工具类，其颜色类只能映射 `--el-*`，**禁止**新增 `--app-*` 等平行 token，也**禁止**硬编码 `bg-red-500` 之类的默认调色板。→ `docs/standards/ui-spec.md` + `docs/decisions/0003-element-plus-tailwind-design-system.md`
3. **页面定制 = CubeTable 插槽**：一切定制用 `CubeTable` 具名插槽；`useSections` / `SectionKeyMap` 覆盖机制为遗留、禁止新增。→ `docs/guides/customize-page.md` + `docs/decisions/0005-cube-engine-context.md`
4. **测试策略**：Vitest + jsdom + `@vue/test-utils` + Playwright（E2E + CT Gallery）；网络层用 `vi.mock` 模块桩 / 依赖注入，**禁止 MSW**；覆盖率门槛 80/75/80/80；CT 截图测试不进覆盖率。→ `docs/standards/testing-standard.md` + `docs/guides/testing.md` + `docs/guides/component-visual-dev.md`
5. **命名约定**：组件文件夹 / 文件大驼峰 PascalCase（如 `core/components/LovSelectTable/`、`CubeTable.vue`）；业务视图目录随 `routeNamingStyle`（pascal/kebab，解析兼容三者）；纯逻辑 / 工具小驼峰 `*.logic.ts` / `utils/*.ts`；测试固定 `*.spec.ts` / `*.test.ts` 后缀。→ `docs/standards/code-conventions.md`

---

## docs/ 目录结构速览

| 目录            | 回答的问题                 | 典型文件                                                                                                             |
| --------------- | -------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `product/`      | 项目要解决什么、如何演进   | `vision-and-roadmap.md`                                                                                              |
| `architecture/` | 系统怎样组织、组件怎样协作 | `overview.md`, `routing.md`, `state-and-data.md`, `cube-engine.md`                                                   |
| `standards/`    | 必须遵守什么（强约束）     | `code-conventions.md`, `ui-spec.md`, `testing-standard.md`, `api-contract.md`                                        |
| `guides/`       | 怎样完成一类任务           | `getting-started.md`, `add-page.md`, `customize-page.md`, `testing.md`, `component-visual-dev.md`, `ai-iteration.md` |
| `reference/`    | 当前事实是什么（紧贴源码） | `route-conventions.md`                                                                                               |
| `decisions/`    | 为什么选 A 而非 B（ADR）   | `0002-*`, `0003-*`, `0004-*`, `0005-*`                                                                               |
| `operations/`   | 怎样运行、测试、发布和排障 | `build-release.md`                                                                                                   |
| `changes/`      | 已完成变更的迁移记录       | —                                                                                                                    |
| `archive/`      | 已失效但仍可追溯的材料     | —                                                                                                                    |

> 完整文档入口见 [`docs/README.md`](./docs/README.md)，**不确定归属的任务先读那里**。

## 渐进式披露：按任务加载对应规范

| 你在做什么                                                     | 先读                                                                   |
| -------------------------------------------------------------- | ---------------------------------------------------------------------- |
| 写 / 改 `CubeTable` 及其子组件（`core/components/CubeTable/`）  | `docs/architecture/cube-engine.md`、`docs/standards/ui-spec.md`        |
| 写组件、想看渲染效果 / 做视觉回归基线                          | `docs/guides/component-visual-dev.md`                                  |
| 写业务页 / 插槽定制                                            | `docs/guides/customize-page.md`、`docs/reference/route-conventions.md` |
| 写 / 改通用组件（含样式，非 CubeTable 族）                    | `docs/guides/add-component.md`、`docs/standards/ui-spec.md`           |
| 写纯逻辑 / 工具函数                                            | `docs/standards/code-conventions.md`                                   |
| 写单元测试 / 组件测试 / E2E                                    | `docs/standards/testing-standard.md`、`docs/guides/testing.md`         |
| 新增页面 / 路由                                                | `docs/guides/add-page.md`、`docs/decisions/0002-menu-driven-routing-and-section-overrides.md`、`0004-controller-first-crud-and-progressive-override.md`、`0005-cube-engine-context.md` |
| AI 迭代需求分级                                                | `docs/guides/ai-iteration.md`                                          |
| 改 CI / 发布流程                                               | `docs/operations/build-release.md`                                     |
| 改样式 / 主题 token                                            | `docs/standards/ui-spec.md`、`docs/decisions/0003`                     |
| 任何不确定归属的任务                                           | `docs/README.md` 定位                                                  |

---

## 执行纪律

- 任何产出（代码或文档）必须能在上表找到依据；找不到先读 `docs/README.md` 定位，不要凭印象。
- 涉及 CSS / 组件开发时，必须确认颜色来自 `--el-*` 且 Tailwind 颜色类已映射，不引入第三套 token。
- 规范文件有更新，只改 `docs/` 对应文件；本文件保持"索引"角色，不冗余承载细节。
- 完整规范总纲见 `docs/standards/frontend-testable-development.md`。
