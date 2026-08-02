# 前端可测试渐进式开发与测试规范（cube-vue 落地版）

> **状态：已采纳（团队规范总纲）。** 本文是您提供的《前端可测试渐进式开发与测试规范方案》的**落地校正版**——已结合 `web/core` 实际代码与 `web/docs` 现有规范（`architecture/cube-engine.md`、`decisions/0005`、`standards/testing-standard.md`、`standards/code-conventions.md`、`operations/build-release.md` 等）进行调整。
>
> **如何使用**：本文是「总纲 + 与通用草案的差异表」。详细实现以分层文档为准：
>
> - 引擎与视图集成：`architecture/cube-engine.md` + `decisions/0005-cube-engine-context.md`
> - 路由/视图约定：`reference/route-conventions.md`
> - 测试分层与命令：`standards/testing-standard.md` + `guides/testing.md`
> - 代码边界与样式：`standards/code-conventions.md` + `standards/ui-spec.md`
> - 构建/发布/CI 卡点：`operations/build-release.md`
> - 新增/定制页面：`guides/add-page.md` + `guides/customize-page.md`

---

## 0. 与通用草案的关键差异（落地校正）

您提供的草案方向正确，但其中多数能力**本项目已经具备或已在规划中**，且若干命名/路径/工具假设与 `cube-vue` 事实不符。下表是本规范对草案的校正。

| 维度      | 通用草案假设                                                                                        | cube-vue 实际                                                                                                                                                                                                    | 本规范处理                             |
| ------- | --------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------- |
| 目录      | `src/api`、`src/components/cube/CubeProvider.vue`、`core/cubeContext.ts`、`modules/system/user/` | 单仓多包：`web/core`（框架，别名 `@newlifex/cube-vue`）+ `apps/<app>/src/views/<area>/<entity>/`；业务页在 `apps/cube-admin`、`apps/cube-cube`、`apps/cube-v1`                                                                    | **采用实际结构**，见 §2                   |
| 引擎入口    | `provideCubeContext()` 放进 `<script setup>`                                                    | 纯工厂 `createCubeEngine(deps)`（无生命周期、可单测）+ 薄 composable `useCubeEngine()`（`provide(CubeEngineKey)` + `onMounted(init)` + `watch(route.path)`）                                                                    | **采用实际引擎设计**，见 §2、§3              |
| 泛型组件    | `CubeProvider` / `CubeTable` / `CubeSearch` / `CubeFormDialog` 四件套                            | 单一集成组件 `CubeTable`（`core/components/CubeTable/`）整合搜索/工具栏/表格/分页/表单弹窗，开放**具名插槽**；并**废除**旧的 Section 覆盖（`ListSearchBar` 等）                                                                                        | **采用 CubeTable + 插槽**，见 §3        |
| 页面定制    | 全部靠插槽或整页覆盖                                                                                    | 同上；旧 Section 覆盖（`useSections`/`SectionKeyMap`）为**遗留机制，将被废除**（ADR 0005）                                                                                                                                         | 新页面一律用插槽；存量 Section 过渡期保留，见 §3、§7 |
| 业务目录    | `modules/system/user/{index.vue,user.logic.ts,user.api.ts}`                                   | `apps/<app>/src/views/<area>/<entity>/index.vue`；页面级纯逻辑抽离为**同目录 `*.logic.ts`**（如 `user.logic.ts`），共享引擎逻辑在 `core/engine`                                                                                        | **采用实际约定**，见 §2、§3 L3             |
| 网络 Mock | 引入 `msw` 全量拦截                                                                                 | 项目已用 **`vi.mock` 模块桩 + 依赖注入**：纯工厂 `createCubeEngine(deps)` 直接注入假 `getPage`/`http`；组件测试通过 `vi.mock('@newlifex/cube-vue/core/utils/request')` 等桩掉外部依赖（见 `core/components/LovSelectTable/LovSelectTable.test.ts`） | **不引入 MSW**；沿用 `vi.mock`/DI，见 §5  |
| 覆盖率/钩子  | 假设可直接加门禁                                                                                      | 当前**未安装** `@vitest/coverage-v8`、husky、lint-staged                                                                                                                                                              | 给出**安装与门槛**，见 §5、§7               |
| 实现状态    | 当作已落地                                                                                         | 引擎 `core/engine/*` 与 `core/components/CubeTable/*` 是 ADR 0005 的**目标（P0–P6 路线）**，当前 `core/views/index.vue` 仍是旧 Section 实现                                                                                      | 本文描述**目标架构**，并显式标注遗留，见 §3、§7      |

> **事实校正（务必记住）**：不要把草案里的 `CubeProvider`/`modules/system/user`/`msw` 当成本项目已有能力。本文与分层文档一致地描述**目标状态**，落地按 `architecture/cube-engine.md` §7 的分阶段路线执行。

> 方案草案术语（如 `CubeFieldConfig`/`CubeSchema`/`CubeContext`/`provideCubeContext`）与本项目的对应关系见 [cube-engine.md §3 术语对照](../architecture/cube-engine.md)。

---

## 1. 核心指导思想

为保证项目从简单业务到超复杂业务都保持高可维护性，全栈开发遵循三大原则（与 `decisions/0005` 一致）：

1. **关注点强制分离**：视图只负责渲染，逻辑只负责计算，基础设施只负责通信。严禁在 `index.vue` 的 `<template>` 和 `<script setup>` 中编写业务逻辑（转换、拼装、取数）。
2. **配置与上下文驱动**：常规页面采用「后端 `GetPage` 下发字段元数据 + 前端 `CubeEngine` 上下文自动消费」模式，消灭胶水代码。字段契约复用现有 `FieldMeta`/`BackendField` 六数组（`setting/list/addForm/editForm/detail/search`）。
3. **测试金字塔模型**：重点投入单元测试（纯逻辑/引擎）和组件测试（交互表现），最小化昂贵且脆弱的端到端测试。

---

## 2. 项目目录与结构规范（实际结构）

```text
web/
├── core/                         # 【框架层】@newlifex/cube-vue，所有应用共享
│   ├── engine/                   # 【引擎】ADR 0005 目标（P1–P2 落地）
│   │   ├── types.ts              # CubeEngineContext / CubeEngineDeps / PageSetting / BackendField
│   │   ├── createCubeEngine.ts   # 纯工厂：无生命周期、依赖注入、可单测
│   │   └── useCubeEngine.ts      # 薄 composable：provide(CubeEngineKey) + onMounted(init) + watch(route)
│   ├── components/
│   │   ├── CubeTable/           # 【视图集成】单一 CubeTable + 5 个受控子组件（P3）
│   │   │   ├── CubeTable.vue           # 集成宿主 + 上下文三级解析 + 插槽分发
│   │   │   ├── CubeTableSearch.vue     # 搜索区
│   │   │   ├── CubeTableToolbar.vue    # 工具栏（#extra 追加按钮）
│   │   │   ├── CubeTableGrid.vue       # 表格（#table-row-actions / #col-[prop] 插槽）
│   │   │   ├── CubeTablePagination.vue # 分页
│   │   │   └── CubeTableFormDialog.vue # 新增/编辑弹窗（声明式绑定 ctx）
│   │   └── ...（现有 LovSelectTable、Uploader 等）
│   ├── composables/
│   │   ├── useCubeApi.ts         # usePageApi：getPage / getList / lookup（元数据与 LOV）
│   │   └── useSections.ts        # ⚠️ 遗留：Section 覆盖（将被废除）
│   ├── dataset/data-set/DataSet.ts  # 【数据层底座】read/create/remoteUpdate/destroy + 分页
│   ├── utils/request.ts          # Axios 入口（401/错误拦截/通知）
│   ├── types/field.ts            # FieldMeta / ControlType（字段契约唯一真理源）
│   └── views/index.vue           # 默认列表页（过渡期仍是 Section 实现；目标 = <CubeTable />）
├── apps/                         # 【业务层】按应用划分
│   ├── cube-admin/src/views/admin/user/index.vue        # 业务页（极薄，只组合）
│   ├── cube-admin/src/views/admin/user/user.logic.ts    # 页面级纯函数（L3 抽离）
│   └── cube-cube/ ... cube-v1/ ...
├── e2e/                          # 【E2E】Playwright（登录/菜单/默认 CRUD）
└── （单元/组件测试）core/__tests__/ 或与被测 core 模块相邻的 *.spec.ts / *.test.ts
```

**规范要求**：

- `index.vue` 的 `<script setup>` **不允许超过 50 行**，不允许出现 `new Date()`、`fetch()`、复杂 `filter/map`、直接 `request` 取数等逻辑。
- 任何数据结构转换、复杂计算、提交前拼装，必须提取到 **`*.logic.ts`** 中作为纯函数导出（或在 `core/engine` 中复用纯工厂逻辑）。
- 跨页面/跨布局共享状态才进 Pinia（`core/stores/`）；局部状态留在组件/composable。
- 网络请求统一走 `core/utils/request.ts`；标准响应/分页/错误遵守 `standards/api-contract.md`。

**目录与文件命名约定（避免「横线 vs 大驼峰」歧义）：**

- **组件文件夹：大驼峰 PascalCase**（与组件名一致），如 `core/components/LovSelectTable/`、`core/components/CubeTable/`（内含 `CubeTable.vue`、`CubeTableSearch.vue` 等）。现役代码目录即此风格，新增必须沿用。
- **组件文件：大驼峰 `.vue`**（`CubeTable.vue`）。
- **业务视图目录**（`apps/<app>/src/views/<area>/<entity>/`）：跟随 `router.routeNamingStyle`（`pascal` 或 `kebab`），解析兼容三者；入口固定 `index.vue`。
- **纯逻辑/工具文件：小驼峰** `*.logic.ts`、`utils/*.ts`（不使用横线）。
- **测试文件：固定后缀 `*.spec.ts` / `*.test.ts`**，基名跟随被测对象（现役 `LovSelectTable.test.ts`）。

> 完整分层命名规则以 `standards/code-conventions.md` 为准。

---

## 3. 渐进式页面开发范式（L1 – L4，已映射到 CubeTable/引擎）

面对不同复杂度，提供标准化落地路径。L2 的「插槽」**取代**了旧的 Section 覆盖文件。

### Level 1：极简页面（常规 CRUD）— 接近零代码

**场景**：标准增删改查列表页。**实现**：默认 `core/views/index.vue` 渲染 `<CubeTable />`（引擎按当前路由自动创建上下文），只需菜单配置了对应后端实体与 `GetPage` 元数据。

```vue

<template>
  <CubeTable />   
</template>
```

### Level 2：轻度定制页面（插槽介入）— 取代 Section 覆盖

**场景**：大部分复用，仅某列/某区域需特殊渲染。**实现**：利用 `CubeTable` 预留的具名插槽（整块 `#search/#toolbar/#table/#pagination/#form`，手术式 `#toolbar-extra/#table-row-actions/#col-[prop]/#header/#footer`）。

```vue

<template>
  <CubeTable>
    <template #toolbar-extra="{ ctx }">
      <el-button @click="ctx.openChart()">图表</el-button>
    </template>
    <template #table-row-actions="{ row, ctx }">
      <el-button link type="primary" @click="ctx.openEdit(row)">编辑</el-button>
      <el-button link type="danger"  @click="ctx.remove(row)">删除</el-button>
    </template>
    <template #col-Status="{ row, field, value }">
      <el-tag v-if="field.name === 'Status'">{{ value ? '启用' : '禁用' }}</el-tag>
      <span v-else>{{ value }}</span>
    </template>
  </CubeTable>
</template>
```

> 定制组件取数三选一：`#slot` 作用域 `{ ctx }`、`useCubeContext()`（父级已 `provide`）、或父级 `:ctx` 显式传入。推荐前两者以保持显式依赖。

### Level 3：中度复杂页面（领域逻辑抽离）

**场景**：提交前复杂拼装、表格数据需多字段联合计算。**实现**：引入 `*.logic.ts` 纯函数，在 `index.vue` 调用。

```typescript
// apps/<app>/src/views/<area>/<entity>/user.logic.ts（纯函数，极易测试）
export const transformUserBeforeSave = (formData: Record<string, unknown>) => {
  const payload = { ...formData }
  payload.deptName = (deptMap as Record<string, string>)[payload.deptId as string] ?? ''
  payload.fullName = `${payload.firstName} ${payload.lastName}`
  delete payload.firstName
  delete payload.lastName
  return payload
}
```

### Level 4：超复杂页面（多上下文与状态机）

**场景**：主从表联动、带复杂权限判断的审批流。**实现**：页面内 `const ctx = useCubeEngine({ routePath })` 后 `<CubeTable :context="ctx" />`，或引入局部状态机（如 XState），通过组合式函数接管部分逻辑；也可完全自管（不使用 `CubeTable`，但不得调用 `useCubeContext()`）。

---

## 4. 测试规范与策略

### 4.1 测试命名与组织

- **文件命名**：与源码同目录或 `core/__tests__/`，后缀统一 `*.spec.ts` 或 `*.test.ts`（单元/组件）；E2E 在 `e2e/*.spec.ts`。
- **用例命名**：`describe('模块名', () => { it('应该…当…', () => {}) })`，描述**业务行为**而非技术实现。

### 4.2 测试范围矩阵（对齐 `standards/testing-standard.md`）

| 测试层级     | 测试对象                                                                                | 工具                                 | 关注点                                                             | 占比目标 |
| :------- | :---------------------------------------------------------------------------------- | :--------------------------------- | :-------------------------------------------------------------- | :--- |
| **单元测试** | `core/engine/createCubeEngine.ts`、`*.logic.ts`、`core/utils/*`、`core/types/field.ts` | Vitest + jsdom                     | 输入输出、边界、异常；**禁止依赖 Vue/DOM**                                     | 60%  |
| **组件测试** | `CubeTable.vue` 及 5 个子组件、`LovSelectTable` 等基础组件                                     | Vitest + `@vue/test-utils` + jsdom | Props/Slots 渲染、事件抛出；通过 `vi.mock` 桩掉 `request`/`configure` 注入上下文 | 30%  |
| **组件视觉测试（CT）** | 各组件的 UI 变体（Story 定义的 props 组合）                                             | Playwright + Gallery（真实浏览器）   | 像素级截图对比，捕获样式/布局/颜色问题；通过 `ct/mocks/` 统一 mock 后端 | 不计入覆盖率 |
| **集成测试** | `CubeTable` + `useCubeEngine` 上下文流转                                                 | Vitest + VTU                       | 上下文生命周期、组件间联动（mock `ctx` prop 验证插槽）                             | 10%  |
| **E2E**  | 核心主流程（登录、菜单直达、默认 CRUD）                                                              | Playwright                         | 真实用户操作流，不关心实现                                                   | 按需   |

### 4.3 核心编写原则

- **AAA 模式**：每个用例必须含 Arrange / Act / Assert 三阶段。
- **单一职责**：一个 `it` 只断言一个行为。
- **避免逻辑**：测试代码不出现 `if/for`；多组数据用 `it.each`。
- **拒绝脆弱快照**：禁止滥用 `toMatchSnapshot()`（尤其含动态类名的组件），改为精准断言文本/属性。
- **Mock 策略（关键校正）**：**不使用 MSW**。纯工厂测试直接注入假 `getPage`/`http`；组件测试用 `vi.mock(...)` 桩掉 `@newlifex/cube-vue/core/utils/request`、`core/configure` 等外部模块（参考 `LovSelectTable.test.ts`）。

---

## 5. 测试基础设施落地指南

### 5.1 现有工具链（已安装，直接复用）

`vitest@^3`、`@vue/test-utils@^2`、`jsdom@^29`、`@playwright/test@^1.54`、`vue-tsc`、`eslint`、`oxlint`。命令见 `standards/testing-standard.md`：`pnpm run test:unit` / `test:e2e` / `type-check` / `lint:eslint`。

### 5.2 需新增的工具（覆盖率）

```bash
pnpm add -D @vitest/coverage-v8
```

并在 `package.json` 增加脚本：

```json
{ "scripts": { "test:coverage": "vitest run --config vitest.config.unit.ts --coverage" } }
```

### 5.3 Vitest 配置规范（扩展 `vitest.config.unit.ts`）

现有 `vitest.config.unit.ts` 已是**最小配置**（仅 `@vitejs/plugin-vue` + `jsdom` + 虚拟配置桩），足以驱动单元/组件测试且不拉起重型插件。在其 `test` 内追加覆盖率：

```ts
test: {
  environment: 'jsdom',
  globals: true,
  include: ['core/__tests__/**/*.{spec,test}.ts', 'core/**/*.{spec,test}.ts'],
  exclude: ['**/*.ct.{spec,test}.ts', 'e2e/**'],
  coverage: {
    provider: 'v8',
    reporter: ['text', 'json', 'html'],
    statements: 80, branches: 75, functions: 80, lines: 80,
    exclude: ['node_modules/', 'core/**/*.d.ts', 'core/main.ts', 'core/**/*.spec.ts', 'core/**/*.test.ts'],
  },
}
```

> 注意：本项目**使用别名 `@newlifex/cube-vue`**（指向 `web/core`），**不是** `@`；测试 `include` 限定在 `core/` 与相邻 `*.spec.ts`，CT 测试 `*.ct.spec.ts` 由 Playwright 单独跑，E2E 在 `e2e/` 由 Playwright 单独跑。

### 5.4 网络层 Mock（沿用 `vi.mock`/DI，不新增 MSW）

组件/集成测试不受影响后端接口，靠 `vi.mock` 桩模块 + 纯工厂依赖注入（见 §0 差异表与 `architecture/cube-engine.md` §6 的引擎测试示例）。**不要**引入 `msw`。

---

## 6. 实战案例：如何测试 Cube 引擎

被测对象 = 纯工厂 `createCubeEngine(deps)`。注入假 `getPage`/`lookup`/`http`，不依赖真实 `usePageApi` 与配置。完整示例见 `architecture/cube-engine.md` §6（**示例文件 `core/engine/__tests__/createCubeEngine.spec.ts` 待 ADR 0005 P1 落地**），覆盖：首查自动触发、`editForm` 只读主键、`init` 失败分支、`remove` 路径式 `DELETE`、搜索参数过滤、分页、LOV 缓存、`autoQuery:false` 不首查。组件组合测试则传入 mock `ctx` prop，断言默认子组件渲染与插槽覆盖生效。

---

## 7. CI/CD 落地建议（对齐 `operations/build-release.md`）

为确保规范不被破坏，在工程流水线加入强制卡点：

1. **提交前检查（需新增 husky + lint-staged）**：
   ```bash
   pnpm add -D husky lint-staged
   ```
   `lint-staged` 在 `pre-commit` 运行 ESLint（`pnpm run lint:eslint` 作用于改动文件），并可跑相关单元测试；不通过禁止提交。
2. **PR 准入检查**：CI 执行 `pnpm run check && pnpm run test:coverage`（类型检查 + 主题 token 检查 + ESLint + 覆盖率）。**硬性规则**：新增代码覆盖率低于 §5.3 门槛，或导致整体覆盖率下降，PR 自动打回。CT 截图测试（`pnpm run test:ct`）可选接入 CI 作为组件级防回归卡点。
3. **定期审查**：每个迭代结束，Tech Lead 审查覆盖率报告，识别薄弱模块并补测试。
4. **存量 Section 覆盖迁移**：过渡期 `index.vue` 用路由 meta 开关在 `CubeTable` 与旧 Section 布局间切换（兼容桥）；按 `architecture/cube-engine.md` §7 的 P4–P6 逐步迁移并移除旧机制（`useSections.ts`、`ListSearchBar.vue` 等）。

---

## 8. 本规范如何落到 docs（更新清单）

本文为总纲，以下文档需与之保持一致（已在本规范落地过程中同步更新）：

| 文档                                      | 与本文的关系 / 更新点                                                    |
| --------------------------------------- | --------------------------------------------------------------- |
| `architecture/cube-engine.md`           | 引擎与 CubeTable 的详细设计与分阶段路线（P0–P6），本文 §2/§3 的来源                   |
| `decisions/0005-cube-engine-context.md` | 引擎 + CubeTable 插槽取代 Section 覆盖的 ADR，本文 §3 的权威依据                 |
| `reference/route-conventions.md`        | 已更新为 CubeTable + 插槽；Section 覆盖标为遗留（本文 §3 落地依据）                  |
| `standards/testing-standard.md`         | 已补充覆盖率门槛、CT 组件视觉测试层、明确 `vi.mock`/DI（非 MSW）、CI 卡点引用（本文 §4/§5） |
| `standards/code-conventions.md`         | 已补充关注点分离规则：`index.vue` ≤50 行、`*.logic.ts` 纯函数、模板/脚本无业务逻辑（本文 §2） |
| `operations/build-release.md`           | 已新增 CI/CD 卡点章节：husky + lint-staged + 覆盖率门槛 + PR 准入（本文 §7）       |
| `guides/customize-page.md`              | 已重写为 CubeTable 具名插槽定制，废除 Section 覆盖（本文 §3 L2）                   |
| `guides/add-page.md`                    | 局部定制引用改为 CubeTable 插槽（本文 §3）                                    |
| `guides/component-visual-dev.md`        | 新增：组件可视化开发完整指南（Story 概念、截图对比原理、Gallery 模式、开发循环） |
| `guides/testing.md`                     | 已补充 CT 组件视觉测试的选层判断和命令                              |
| `architecture/state-and-data.md`        | 默认页描述更新为 CubeTable/引擎目标，旧 Section 组合标为遗留（本文 §2/§3）              |
| `reference/default-page-engine.md`      | 流程更新为 CubeTable/引擎目标，Section 覆盖标为遗留（本文 §2/§3）                   |
| `decisions/0002`、`decisions/0004`       | 增加「Section 覆盖被 CubeTable 插槽取代」的遗留标注（本文 §3）                      |
