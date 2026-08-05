# 整合方案 vs 现有文档 / 代码 核对

> 时间：2026-08-02
> 目的：核对用户提供的「整合方案（文档一引擎与组件封装规范 + 文档二渐进式开发与测试落地规范）」是否都已体现在 `web/docs`，并区分【规范（已文档化可执行）】/【待做（代码或工具未落地）】/【未做项是否入 TODO】。
> 判定口径：`已规范`=文档正确且与真实代码一致；`已记录·目标`=文档有记录但明确标为 ADR 0005 目标/待实现；`冲突`=方案用名/路径/工具与真实代码或既有文档矛盾；`缺口`=方案有但文档完全没提。

## 一、逐条核对表

### 文档一：Cube 上下文与泛型组件封装规范

| 条目 | 状态 | 是否待做 | 在 TODO? | 证据 |
| --- | --- | --- | --- | --- |
| §1 设计理念（配置即契约 / 上下文全自动 / 视图 inject 消费） | ✅ 已规范 | 否 | — | `architecture/cube-engine.md` 设计理念；`frontend-testable-development.md` §一 |
| §2 核心契约（`src/core/types.ts`：`CubeFieldConfig`/`CubeSchema`/`CubeContext` 等） | ⚠️ 已记录·目标 | 是（引擎 P1） | T1 | 概念见 `cube-engine.md` §2-§4；真实契约来自后端 `GetPage` 元数据，非该路径文件 |
| §3 上下文全自动引擎（`provideCubeContext`/`useCubeContext`） | ⚠️ 已校正·落地 | 否 | T1 | 草案 `provideCubeContext`→`createCubeEngine`(纯工厂)+`useCubeEngine`(setup/provide)；草案 `useCubeContext`(消费端) 与本项目 `useCubeContext`(inject 消费) 语义一致、保留。见 `cube-engine.md` §4 与 §4.3 命名澄清 |
| §4 泛型视图组件 `CubeTable`（`#col-[prop]`/`#table-row-actions` 插槽） | ✅ 已规范（槽名已落地） | 组件未建（P3） | T1 | 插槽命名已在 `cube-engine.md` §5.3、`customize-page.md`、`route-conventions.md` 统一为方案规范槽名；`CubeTable` 实现见 `decisions/0005` P3 路线图 |

### 文档二：渐进式开发与测试落地规范

| 条目 | 状态 | 是否待做 | 在 TODO? | 证据 |
| --- | --- | --- | --- | --- |
| §1 目录结构（`src/api`、`src/components/cube`、`src/core/...`、`src/modules/system/user`、`src/tests`） | 🔴 冲突（路径） | — | — | 真实为单仓多包：`web/core`（别名 `@newlifex/cube-vue`）+ `apps/<app>/src/views/<area>/<entity>/`；见 `frontend-testable-development.md` §二、`docs/README.md` |
| §1 硬规（`index.vue` script ≤50 行、禁 `fetch`/复杂 `filter/map`、`*.logic.ts` 纯函数） | ✅ 已规范 | 否 | — | `standards/code-conventions.md`；`frontend-testable-development.md` §二 |
| §2 L1–L4 渐进式 | ✅ 已规范 | L1 引擎待做 | T1 | `frontend-testable-development.md` §三；`guides/customize-page.md` |
| §3.1 测试矩阵（单元 60% / 组件 30% / 集成 10%） | ✅ 已规范 | 否 | — | `standards/testing-standard.md` 测试范围矩阵 |
| §3.2 编写原则（AAA / 禁 `toMatchSnapshot`） | ✅ 已规范 | 否 | — | `standards/testing-standard.md` 编写原则 |
| §3.2 网络隔离用 **MSW** | 🔴 冲突 | — | — | 文档全境规定 `vi.mock`/依赖注入，**禁止 MSW**；见 `testing-standard.md`、`frontend-testable-development.md` §0 差异表 |
| §4.1 依赖含 `msw` | 🔴 冲突 | — | — | `package.json` devDependencies 无 `msw`；文档禁用 |
| §4.2 Vitest 配置（`vitest.config.ts`、`@` 别名、coverage 80/75/80/80） | ⚠️ 冲突·目标 | 是（工具未装） | T0 | 真实为 `vitest.config.unit.ts` + `@newlifex/cube-vue` 别名；coverage 块/工具未装 |
| §4.3 MSW mock 基建 | 🔴 冲突 | — | — | 同 §3.2，应删；改用 `vi.mock` |
| §5.1 husky+lint-staged + `vitest related --run` | ⚠️ 已记录·目标 | 是（工具未装） | T0 | `operations/build-release.md` CI/CD 卡点；husky/lint-staged 未装 |
| §5.2 CI/CD `test:coverage` 门槛 80 打回 | ⚠️ 已记录·目标 | 是（脚本/工具未装） | T0（D1 已标注） | `operations/build-release.md`；`test:coverage` 脚本与 `@vitest/coverage-v8` 未装 |

## 二、分类汇总

### ✅ 已规范（直接可作为团队规范执行）
- 三大设计理念（配置即契约 / 上下文全自动 / 视图 inject 消费）
- 关注点分离硬规（`index.vue` ≤50 行、`*.logic.ts` 纯函数、模板/脚本无业务逻辑）
- L1–L4 渐进式范式
- 测试范围矩阵（60/30/10）与编写原则（AAA、禁脆弱快照）

### ⚠️ 已记录·目标 / 待做（需在 TODO 跟踪）
> （2026-08-02 补：方案草案术语已落地——`CubeFieldConfig`/`CubeSchema`/`CubeContext`/`provideCubeContext` 经 `cube-engine.md` §3 术语对照 + §4.3 命名澄清映射为 `FieldMeta`/`CubePageMeta`/`CubeEngineContext`/`createCubeEngine`+`useCubeEngine`；插槽命名 `#col-[prop]`/`#table-row-actions` 已统一落到 `cube-engine.md` §5.3、`customize-page.md`、`route-conventions.md`。）
- 引擎与契约（`createCubeEngine`/`useCubeEngine`/`CubeContext` 契约类型）→ **T1**
- `CubeTable` 泛型组件与插槽渲染 → **T1**
- 覆盖率门禁（`@vitest/coverage-v8`、`vitest.config.unit.ts` coverage 块、`test:coverage` 脚本）→ **T0**
- husky+lint-staged 本地卡点（`vitest related`）→ **T0**

### 🔴 与真实代码/文档冲突（方案下发前必须纠正，不应写进 TODO）
1. **`provideCubeContext`** → `createCubeEngine`（纯工厂）+ `useCubeEngine`（setup/provide）。草案的 `useCubeContext`（消费端）与本项目 `useCubeContext`（inject 消费）语义一致、**保留**，非弃用名；`CUBE_INJECTION_KEY` 对应本项目 `CubeEngineKey`。
2. **`CubeSearch.vue`/`CubeFormDialog.vue`/`CubeProvider` 四件套** → ADR 0005 已废除，改为单一 `CubeTable` + 子组件（`CubeTableSearch`/`CubeTableToolbar`/`CubeTableFormDialog` 等）。
3. **目录路径 `src/api`、`src/components/cube`、`src/core/cubeContext.ts`、`src/modules/system/user`、`src/tests`** → 真实为 `web/core` + `apps/<app>/src/views/...`；别名 `@` → `@newlifex/cube-vue`。
4. **MSW（§3.2 / §4.1 / §4.3）** → 文档全境禁止，改用 `vi.mock`/依赖注入。这三处必须整段删除，不是"待做"。
5. **`vitest.config.ts` `@` 别名** → 真实 `vitest.config.unit.ts` + `@newlifex/cube-vue`。

### 缺口（方案有但文档不该写）
- MSW 相关全部——文档明确禁用，无需补充，只需从方案中剔除。

## 三、TODO 覆盖度结论

| 方案「待做」项 | TODO 收纳 | 位置 |
| --- | --- | --- |
| 引擎 + 契约 + `CubeTable` 组件 | ✅ | T1 |
| 覆盖率工具（coverage-v8 / coverage 块 / `test:coverage`） | ✅ | T0 |
| husky + lint-staged 卡点 | ✅ | T0 |
| `test:coverage` 当已生效的矛盾 | ✅ 已修 | D1 |

**结论**：方案的「待做（代码/工具）」项**全部已被 `docs/TODO.md` 覆盖**（T0/T1）。方案的「冲突（错误）」项（MSW、`provideCubeContext`、四件套、`src/modules` 路径、`@` 别名）**不是 TODO 项**——它们是方案本身的过时草稿，必须在方案文本中直接纠正后才能作为团队规范下发，不能在 TODO 里当作"待做"搁置。

## 四、方案落地前必须改的 5 处
1. 删除所有 MSW 引用（§3.2 / §4.1 / §4.3），统一改为 `vi.mock` 依赖注入。
2. `provideCubeContext`/`useCubeContext` → `createCubeEngine` + `useCubeEngine`。
3. `CubeProvider`/`CubeSearch`/`CubeFormDialog` 四件套 → 单一 `CubeTable` + 子组件（`#col-[prop]`/`#table-row-actions` 插槽命名 ✅ 已落地到 docs）。
4. 目录 `src/...` → `web/core` + `apps/<app>/src/views/<area>/<entity>`；别名 `@` → `@newlifex/cube-vue`。
5. `vitest.config.ts` → `vitest.config.unit.ts`；显式标注 coverage 门禁为「待 T0 工具落地后生效」。

---

## 五、方案规范已落实到 docs（闭环，2026-08-02）

用户要求「不仅是核对 TODO，还要把方案的规范落实到 docs」。在上一轮核对基础上，本补充将方案两份文档的规范**真正写入**分层文档，并修正了核对阶段遗留的命名偏差。

### 方案文档 ↔ 本项目权威文档（规范落点）
| 用户整合方案 | 落地权威文档 | 说明 |
| --- | --- | --- |
| 文档一 §1 设计理念 | `architecture/cube-engine.md` §1 + `frontend-testable-development.md` §1 | 配置即契约 / 上下文全自动 / 视图 inject 消费 |
| 文档一 §2 核心契约 | `cube-engine.md` §3（类型）+ **§3 术语对照** | `CubeFieldConfig`→`FieldMeta`/`BackendField`、`CubeSchema`→`CubePageMeta`、`CubeContext`→`CubeEngineContext` |
| 文档一 §3 引擎 | `cube-engine.md` §4 + §4.3 命名澄清 | `provideCubeContext`→`createCubeEngine`+`useCubeEngine`；`useCubeContext`(消费) 保留 |
| 文档一 §4 CubeTable 插槽 | `cube-engine.md` §5.3、`customize-page.md`、`route-conventions.md`、`decisions/0005` | 槽名统一为 `#col-[prop]` / `#table-row-actions`（与方案一致） |
| 文档二 §1 目录/硬规 | `frontend-testable-development.md` §2、`code-conventions.md` | 实际单仓多包结构 + `index.vue` ≤50 行 / `*.logic.ts` 纯函数 |
| 文档二 §2 L1–L4 | `frontend-testable-development.md` §3 | 渐进式范式 |
| 文档二 §3 测试矩阵/编写原则/禁 MSW | `testing-standard.md` | 60/30/10 + AAA + `vi.mock`/DI（禁 MSW） |
| 文档二 §4 工具链/Vitest 配置 | `frontend-testable-development.md` §5.2/§5.3、`testing-standard.md` | `vitest.config.unit.ts` + `@newlifex/cube-vue` 别名 + coverage 块 |
| 文档二 §5 husky/CI | `build-release.md`、`frontend-testable-development.md` §7 | 本地卡点 + PR 覆盖率门禁（待 T0 工具生效） |

### 本轮（2026-08-02）新增落地项
- ✅ 插槽命名 `#cell`/`#row-actions` → 方案规范 `#col-[prop]`/`#table-row-actions`，全文档统一（`cube-engine.md` / `customize-page.md` / `frontend-testable-development.md` / `route-conventions.md` / `decisions/0005`）。
- ✅ 方案术语 ↔ 本项目类型对照表（`cube-engine.md` §3），并加总纲指针。
- ✅ 组合式命名澄清（`useCubeEngine` 仅做 setup/provide；`useCubeContext` 为合法消费端 inject，非弃用名）——`cube-engine.md` §4.3。
- ⏳ 仍属待做（**非文档项**）：引擎与 `CubeTable` 组件实际落地（T1）、覆盖率/husky 工具安装（T0），继续在 `docs/TODO.md` 跟踪。

### 结论
方案「规范（应强制约束团队）」部分**已全部落到 `web/docs` 分层文档**；方案「待做（代码/工具）」部分（T0/T1）**已全部在 `docs/TODO.md` 跟踪**；方案「冲突（过时草稿）」部分（MSW、`provideCubeContext` 四件套、`src/modules` 路径、`@` 别名）**已在 docs 中纠正为 cube-vue 真实命名**，并修正了核对阶段把 `useCubeContext`（合法消费端）误判为弃用名的偏差。
