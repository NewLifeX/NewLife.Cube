---
name: cube-design
description: |
  设计一致的页面视觉与交互：在 NewLife.Cube.Vue 项目中新增/审查页面时消灭"这个间距多大、这个场景用什么组件、加载态怎么表现"等未定义决策点。
  当用户说"设计页面"、"新页面视觉/布局"、"这个页面看起来不一致"、"要不要用 el-table 还是卡片"、"加载态/空态/确认怎么做"、"设计规格"、"design spec"、"原型设计"时使用。
  基于 Element Plus + Tailwind 双系统心智模型，覆盖页面类型骨架、组件选型决策树、交互模式、暗色主题、设计规格检查单。
---

# cube-design：页面设计一致性技能

## 核心心智模型（一句话）

**设计系统的本质是把审美决策变成工程约束**——不是"建议这样做"，而是"只能这样做"。每个未定义的决策（间距、加载态、圆角、色彩语义……）都是一次不一致的机会，规格要把这些分叉点变成单行道。完整推导见 [references/principles.md](./references/principles.md)。

## 两套系统 + 组件优先级

```
Element Plus  → 组件长什么样（颜色/圆角/hover/disabled/暗色，组件已解决，不用重造）
Tailwind      → 元素怎么摆（flex/grid/间距/响应式，utility 已覆盖，不用写 <style>）
连接点        → Tailwind 的颜色/圆角映射 --el-* 变量，主题切换零改动
```

本项目在此基础上多一层：**优先复用框架已封装的业务组件**（`CbTable`/`CubeSearch`/`LovSelect`/`DefaultEntity` 等），命中即停，不新建。完整选型顺序、按场景对照表见 [references/component-decision.md](./references/component-decision.md)。

**不加第三套系统**：不引入自定义 CSS 变量层（`--app-*`）；页面级布局不与 `el-row`/`el-col` 栅格并存（表单内部字段网格除外）。

## 何时用 / 何时不用

| 用                                 | 不用                                 |
| ---------------------------------- | ------------------------------------ |
| 新建业务页面前确定骨架和组件       | 纯后端逻辑、无 UI 改动的任务         |
| 审查一个页面"看起来不像同一个产品" | 营销页/落地页（不受组件库约束）      |
| 决定加载/空态/错误/确认怎么表现    | 设计探索阶段（还在找方向，先别固化） |
| 新增主题/暗色适配                  | —                                    |

## 执行流程

### 第 0 步：确认基础设施已就绪（一次性，通常已完成）

本项目的"设计系统基础设施"已经存在，无需每个页面重新搭建：

| 基础设施                 | 位置                                                                   |
| ------------------------ | ---------------------------------------------------------------------- |
| Element Plus 语义变量    | `core/global.css`、各 `core/themes/*.css`                              |
| 布局结构变量             | `core/cube-layout-vars.css`                                            |
| Tailwind → `--el-*` 映射 | `src/theme/tailwind.css`                                               |
| 项目权威 UI 规范         | [`web/docs/standards/ui-spec.md`](../../web/docs/standards/ui-spec.md) |
| 架构决策记录             | [`web/docs/decisions/`](../../web/docs/decisions/)                     |

若这些文件缺失或映射不完整，先补齐，再进入下一步。

### 第 1 步：确定页面类型与骨架

四选一：详情/设置页、列表页、仪表盘、全屏工作区。列表页优先判断能否由框架 `DefaultEntity` 自动生成，只做局部覆盖用 Section 机制（`cube-page-override` 技能），不重写整页。
判断是否需要"子壳"（二级导航）。
→ 详见 [references/page-types.md](./references/page-types.md)

### 第 2 步：组件选型

按优先级：框架业务组件 → Element Plus 原生组件 → Tailwind 摆放 → 都不满足才新建。
→ 详见 [references/component-decision.md](./references/component-decision.md)

### 第 3 步：摆布局

Tailwind utility（`flex`/`grid`/`gap-4`/`p-6`/响应式前缀），颜色圆角用映射到 `--el-*` 的语义类，不用默认调色板（`bg-red-500`）。看到自己想写 `<style>` 硬编码尺寸/颜色，先停下查 spec。

### 第 4 步：加交互

加载（skeleton vs v-loading）、空态（初始 vs 筛选）、错误（页面级 vs 操作级）、确认（轻量 popconfirm vs 重量 dialog）、反馈（Message vs Notification），全部是"选择预设模式"，不现场发明。
→ 详见 [references/interaction-patterns.md](./references/interaction-patterns.md)

### 第 5 步：对照检查单

提交前逐条自检：栅格是否重复、硬编码色值、hover 位移、圆角超限、暗色模式、决策是否写入日志。
→ 详见 [references/spec-checklist.md](./references/spec-checklist.md)

### 第 6 步（可选）：新决策沉淀为准则

若本次做出了 UI 规范未覆盖的新决策，按 ADR 格式写入 `web/docs/decisions/`，并同步更新 `web/docs/standards/ui-spec.md` 对应规则。

## 与其他技能的关系

| 技能                        | 关系                                                                           |
| --------------------------- | ------------------------------------------------------------------------------ |
| `cube-add-page`             | 创建页面文件本身；cube-design 决定页面里的骨架/组件/交互怎么选                 |
| `cube-page-override`        | Section 覆盖机制；cube-design 的"列表页优先判断能否自动生成"依赖它             |
| `cube-layout`               | 布局（外壳/侧边栏）Token 规范；cube-design 的"两套系统"原则与其 Token 规则一致 |
| `cube-add-app` / `cube-lov` | 应用/关联对象场景下同样遵循本技能的组件选型与交互规范                          |

## 参考文档索引（按需加载，不要一次性全读）

- [原则与心智模型（为什么）](./references/principles.md)
- [页面类型与骨架](./references/page-types.md)
- [组件选型决策树](./references/component-decision.md)
- [交互模式](./references/interaction-patterns.md)
- [设计规格检查单](./references/spec-checklist.md)
- [项目权威 UI 规范](../../web/docs/standards/ui-spec.md)
- [架构决策记录](../../web/docs/decisions/)
