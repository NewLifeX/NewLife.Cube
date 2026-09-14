# 产物清单（Artifact Summary）

- 生成日期：2026-09-01 · 分支 `x_master`（HEAD `6282baa7`）· 全部为 **current-state**
- 产出插件：Architecture Visualization（`explore` 路由）

## 产物一览

| # | 产物 | 回答的问题 | 场景技能 + 格式技能 |
| --- | --- | --- | --- |
| 0 | [`architecture-plan.md`](./architecture-plan.md) | 本次建模的范围、受众、策略 | `explore` |
| 0 | [`scenario-route.md`](./scenario-route.md) | 为什么选这些技能/格式 | `explore` |
| 0 | [`architecture-evidence-plan.md`](./architecture-evidence-plan.md) | 证据核实清单与缺口 | `explore` |
| 1 | [`system-model/newlife-cube.dsl`](./system-model/newlife-cube.dsl) | 系统是什么：landscape/context/container/component 四视图 + 部署视图 | `system-modeler` + `c4model` |
| 1 | [`system-model/system-model.md`](./system-model/system-model.md) | 怎么读模型、关键架构结论 | 同上 |
| 1 | [`system-model/newlife-cube.evidence.md`](./system-model/newlife-cube.evidence.md) | 每个节点/关系的证据与置信度 | 同上 |
| 1 | [`system-model/newlife-cube.summary.md`](./system-model/newlife-cube.summary.md) | 摘要、下一步、维护说明 | 同上 |
| 2 | [`dependencies/module-dependencies.dot`](./dependencies/module-dependencies.dot) | 谁依赖谁、改哪里波及哪里 | `dependency-impact-analyzer` + `graphviz` |
| 2 | [`dependencies/module-dependencies.md`](./dependencies/module-dependencies.md) | 风险点排序与变更影响速查表 | 同上 |
| 3 | [`flows/auth-login-flow.dot`](./flows/auth-login-flow.dot) | 登录/令牌/登出如何流动 | `flow-visualizer` + `graphviz` |
| 3 | [`flows/entity-crud-pipeline.dot`](./flows/entity-crud-pipeline.dot) | CRUD 请求如何过租户/权限/数据层 | 同上 |
| 3 | [`flows/flows.md`](./flows/flows.md) | 两条流程的逐步证据（文件:行号） | 同上 |
| 4 | [`deployment/deployment-topology.dot`](./deployment/deployment-topology.dot) | 在哪运行、如何发布 | `deployment-topology-analyzer` + `graphviz` |
| 4 | [`deployment/deployment-topology.md`](./deployment/deployment-topology.md) | 宿主、数据层、发布管道说明 | 同上 |

## 建议阅读顺序

1. `system-model/system-model.md` —— 建立整体认知（双核心库 + 主题 + 宿主）。
2. `dependencies/module-dependencies.md` —— 理解耦合与变更波及面。
3. `flows/flows.md` —— 深入登录链与 CRUD 管道。
4. `deployment/deployment-topology.md` —— 运行与发布形态。

图表源文件（`.dsl`/`.dot`）可直接在 Qoder Canvas 预览（DSL 格式 / DOT 格式），
也可用 Structurizr CLI 或 Graphviz 重新渲染。

## 证据覆盖与总体局限

- 全部节点/关系均有仓库内文件证据（详见各 evidence 小节）；无法回指代码的结论
  已在图中以 `inferred`/虚线标注并在文档"不确定项"列出。
- 主要盲区：XCode/NewLife.Core 包内部实现（仓库外）、支持数据库全集、
  魔方在集成方处的真实生产部署形态（框架本身只提供嵌入式包与参考宿主）。
- 已知待清理项：React 主题陈旧引用、`NewLife.Cube.Tests` 未入 sln、
  `NewLife.CubeST` 废弃残留。

## 维护约定

- 结构变化先改 `newlife-cube.dsl`（单一事实来源），再同步对应 evidence 行。
- 各图保持"一图一问"；新增问题新加视图，不把无关问题塞进现有图。
