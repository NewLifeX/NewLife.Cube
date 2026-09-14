# 场景路由记录（Scenario Route）

## 用户请求

"根据该插件功能生成产出其应该产出的产物" —— 对 NewLife.Cube 全仓库做现状架构可视化。

## 路由决策

这是一个"理解当前系统全貌"的宽请求，按 `explore` 路由规则：

| 架构任务 | 主技能 | 支撑技能 | 选择理由 |
| --- | --- | --- | --- |
| 这个系统是什么、边界在哪 | `system-modeler` | `c4model` | 宽请求的第一优先级：先建立结构与边界模型 |
| 谁依赖谁、改核心库波及什么 | `dependency-impact-analyzer` | `graphviz` | 双核心库 + 17 个主题/演示项目的引用网络是主要耦合风险点 |
| 请求如何在系统中流动 | `flow-visualizer` | `graphviz` | 登录链与实体 CRUD 是框架的两条价值主线 |
| 在哪里运行、如何发布 | `deployment-topology-analyzer` | `graphviz` | 仓库自带演示宿主、Dockerfile、GitHub Actions 发布链 |

未选用的技能及原因：
- `legacy-system-visualizer`：项目文档充足（Doc/ 下有完整架构文档系列），非低证据遗留系统。
- `evolution-planner` / `risk-quality-reviewer`：用户未要求演进规划或风险评审，避免过度产出。
- `architecture-communicator`：受众单一（维护者/集成方），无需分受众改写。
- `drawio`：用户未要求可编辑交付格式。

## 视图状态声明

全部工件均为 **current-state（现状）**，基于 2026-09-01 的 `x_master` 分支
（HEAD `6282baa7`）。不含目标态/假设态内容。

## 输出格式选择依据

- C4 视图（landscape/context/container/component）→ Structurizr DSL：
  符合 `diagram-output-formats.md` 对 C4 建模的默认选择，可在 Qoder Canvas 预览。
- 依赖网络、流程、部署拓扑 → Graphviz DOT：
  关系密集，DOT 布局表现优于 Mermaid；默认 `rankdir=TB` 保证 Canvas 纵向预览。
