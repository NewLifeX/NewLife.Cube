# NewLife.Cube 架构可视化计划

- 生成日期：2026-09-01
- 产出技能：`architecture-visualization` 插件（路由：`explore`）
- 状态：current-state（现状建模，不含目标态）

## 架构问题

用户要求按 Architecture Visualization 插件的能力，为 NewLife.Cube 仓库产出其标准产物：
系统模型、依赖分析、流程图、部署拓扑及配套说明文档。

## 受众与用途

- 受众：框架维护者与二次开发者（集成方）。
- 用途：快速建立"魔方是什么、由什么组成、关键请求如何流动、如何部署发布"的整体认知，
  并为后续变更影响分析提供可追溯的基线图。

## 范围

| 在范围内 | 不在范围内 |
| --- | --- |
| 仓库内项目结构与引用关系（双核心库 + 主题包 + 演示站 + 测试） | XCode/NewLife.Core NuGet 包内部实现（仅作外部依赖标注） |
| 登录/认证链、实体 CRUD 管道两条关键流程 | 每个第三方 OAuth provider 的细节差异 |
| 构建/发布/容器化部署拓扑 | 各前端主题内部组件级结构（仅到"主题包"粒度） |
| 多租户请求解析（中间件层） | 性能压测、运行时实测数据（无 telemetry 证据） |

## 计划产出的工件

| 场景技能 | 基础/格式技能 | 工件 |
| --- | --- | --- |
| `system-modeler` | `c4model` | `system-model/newlife-cube.dsl` + 说明/证据/摘要 |
| `dependency-impact-analyzer` | `graphviz` | `dependencies/module-dependencies.dot` + 说明 |
| `flow-visualizer` | `graphviz` | `flows/auth-login-flow.dot`、`flows/entity-crud-pipeline.dot` + 说明 |
| `deployment-topology-analyzer` | `graphviz` | `deployment/deployment-topology.dot` + 说明 |
| `explore`（路由） | — | `scenario-route.md`、`architecture-evidence-plan.md`、`artifact-summary.md` |

## 证据策略

全部结论必须能回指仓库内文件（代码/配置/文档）。无法回指的结论降级为
`low` 置信并标注 `inferred/assumed`，列入各工件的"不确定项"。

## 下一步动作

产出后按 `artifact-summary.md` 的阅读顺序使用；仓库结构变化时优先更新
`newlife-cube.dsl`（模型是各视图的单一事实来源）。
