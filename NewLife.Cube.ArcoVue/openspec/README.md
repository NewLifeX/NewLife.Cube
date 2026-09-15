# NewLife.Cube.ArcoVue OpenSpec

本目录为 ArcoVue 产品化增量协作资产（**暂不**迁入 NewLife.Skills）。组织级能力通过**编排** [NewLife.Skills](https://github.com/NewLifeX/NewLife.Skills) 已有 instructions / skills / agents 获得。

权威流程见：[ArcoVue企业中后台迁移方案.md](../ArcoVue企业中后台迁移方案.md) §9。

**本 README 是共享规则的单一事实源**：编号、门禁、测试、会话补录、目标愿景、`status.md` 写法、SFC 与框架文档均以本文为准；五壳 Agent 只写阶段动作并引用本文。Cursor 或未来 Agent 同样只认本文 + `status.md` + 五壳职责。

## 状态流转

每个 `changes/{OSC-ID} <简述>/status.md`（归档同名规则，位于 `archive/`）：

```
Draft → Accepted → Implementing → Validating → Done
  ↘ Rejected
```

| 状态 | 阶段 |
|------|------|
| `Draft` | 创建后 |
| `Accepted` | 批准通过 |
| `Rejected` | 批准未通过 / 明确拒绝（自 Draft） |
| `Implementing` | **执行**（含测试） |
| `Validating` | **验收** |
| `Done` | **复盘**归档终态 |

## 编号规则（团队并行、禁止抢号）

**新变更 ID：** `OSC-YYMMDDxxxx`（`YYMMDD` = 创建日 Asia/Shanghai；`xxxx` = 4 位随机小写 hex，紧接日期、中间无 `-`）。

**目录命名：** `{ID} <简洁中文描述>`（编号与简述间**一空格**）。  
例：`OSC-260813c3e9 页面TS抽离与协作编号`。

- 创建时在 `changes/` **与** `archive/` 查前缀唯一；冲突则重抽 `xxxx`。
- **禁止**按 `max+1` / 落地顺序递增 / 为依赖预留空洞号。
- **历史豁免：** 已存在的 `OSC-0001` … `OSC-0019` 不改名；`批准 OSC-0018` 等旧触发语仍有效。
- 定位：触发语中的 ID 必须能在 `changes/` 或 `archive/` 唯一匹配目录名前缀；找到 0 或 ≥2 个则停止并询问。

## 门禁与测试

**门禁：** 仅 `Accepted`（首次）或 `Implementing`（续跑）可执行。批准用语：`批准 OSC-260813c3e9`、`推进 OSC-260813c3e9 到 Accepted.`；拒绝：`拒绝 OSC-260813c3e9`。旧号同理。

**测试与构建：** 触及前端或后端代码时——**执行阶段必须跑单元测试**（并补测）；**验收阶段须本 OSC 新增单测全部通过，且构建无错误抛出**。纯文档 / 纯 openspec 文案可声明 N/A。

## 共享细则

五壳 Agent 引用本节，不复制正文。

### 会话小任务补录

- 范围：自 `Accepted` 起，通过会话窗口直接完成、**不在本 OSC proposal/design 计划内**的事项 / 重构 / 修复。
- 独立事项 → `tasks.md` **新增任务项**（如 `T10 …`，逐条勾选）；与既有任务项相似 → **补充已有任务项子条目**，不新建。
- 同步在 `status.md` 追加 note 摘要，并在 `verify.md` 补充对应 AC。
- 纯样式等可并入相似任务的微调，不得为凑数新建任务项。
- 补录完成后在 `status.md` note 注明「会话小任务已补录」；后续阶段核对无新增即跳过，不重复新建。

### 目标愿景（proposal 第 1 点）

- **位置**：`proposal.md` 第 1 点，标题固定为 **`1. 目标愿景`**。
- **内容**：一句话愿景（本变更完成后达到的状态）+ 2~4 条**可验证目标**（避免空泛口号；每条目标应有对应 `tasks.md` 任务项或 `verify.md` AC 可追溯）。
- **用途**：总览该变更；验收阶段（`openspec-verify`）以此逐条对照代码实现与相关变更规范文档，未达成项即缺口。
- 示例：

```markdown
# OSC-YYMMDDxxxx — 主题

## 1. 目标愿景

让业务管理员无需代码即可配置实体增删改的自动化流程，一次配置永久生效。

- 目标 1：任意 XCode 持久化写入（控制器/导入/直接 Insert）均能触发启用规则；
- 目标 2：线性图执行器跑通 notify/update/create/find/http/delay 等动作，未知节点失败不静默；
- 目标 3：配置 UI 对标飞书「自动化」双栏，无 Update 权限不可见入口。

## 2. 为何做
…
```

### status.md 写法

- 字段：`id` / `state` / `updated`（ISO 8601）/ `approvedBy` / `trigger`（用户原话）/ `checklist` / `note`。
- `note` 只追加**状态摘要（每轮 1~3 行：做了什么、门禁结果、下一步）**；详细过程、审查报告与测试输出写 `verify.md` / `tasks.md` / `retro.md`，不粘贴进 `note`。
- 历史长 `note` 不追溯改写，新推进按本节执行。

## 前端框架与官方文档

ArcoVue 前端实现按场景固定使用以下框架；实现细节、组件 API、配置项或生命周期处理存在不确定时，**必须先查阅对应官方文档，再严格按官方文档实现**，不得凭印象补造 API、配置或交互。

| 场景 | 框架 | 官方资料 |
| --- | --- | --- |
| 设计系统、应用壳、表单及通用 UI | 字节跳动 Arco Design Vue | [快速上手](https://arco.design/vue/docs/start) |
| 图标（全局 `<icon-park type>`） | IconPark `@icon-park/vue-next` | [官方图标库](https://iconpark.oceanengine.com/official) · GitHub [bytedance/IconPark](https://github.com/bytedance/IconPark)（vue-next README） |
| 多维数据视图 | VisActor VTable | [教程](https://visactor.com/vtable/guide/Getting_Started/Getting_Started) · [ListTable 配置](https://visactor.com/vtable/option/ListTable) · [实例接口](https://visactor.com/vtable/api/Methods) |
| 工作流 | FlowGram.AI | [指引](https://flowgram.ai/guide/getting-started/introduction.html) · [例子](https://flowgram.ai/examples/index.html) · [API](https://flowgram.ai/api/index.html) |

> 图标名以 IconPark `IconType` 为准，统一注册于 `web/src/core/utils/iconRegistry.ts`（唯一事实源）；新图标必须先经 IconPark 站点确认存在再注册。

### SFC 职责分离（Vue 页面不嵌入业务 TS）

存量清零见 `OSC-260813c3e9`。自该号起，**新增或修改**的 `.vue` 必须遵守：

- `.vue` 只含 `<template>`、`<style>`、构薄 `<script setup lang="ts">`：组件 import、`defineProps` / `defineEmits` / `defineExpose`、调用同目录 `useXxx(...)`、把返回值交给模板。
- 除 import 与宏外，script 建议不超过约 20 行；**禁止**在 `.vue` 写业务 `ref`/`watch`/`onMounted`、`cubeApi.*`、领域计算函数。
- 业务进同目录 `useFoo.ts`（`Foo.vue` → `useFoo.ts`）；无响应式的纯函数进 `core/utils/*.ts` 或 sibling `*Helpers.ts`。
- 已足够薄的展示组件（无业务状态）不必造空 composable。
- 不采用 `<script setup src>`。测试仍以 Vitest node + 纯函数/composable 为主，不强制挂载 SFC。

创建 OSC 时，应在 `design.md` 标明适用框架及需查阅的官方资料；执行 OSC 时，`openspec-apply` 负责落实本规则。

## 五壳 Agent

| Agent | 阶段 | 触发示例 |
|-------|------|----------|
| [openspec-create](agents/openspec-create.agent.md) | 创建 | `创建 OSC：页面 TS 抽离`（自动生成 `OSC-YYMMDDxxxx`） |
| [openspec-approve](agents/openspec-approve.agent.md) | 批准 | `批准 OSC-260813c3e9` |
| [openspec-apply](agents/openspec-apply.agent.md) | 执行 | `执行 OSC-260813c3e9` |
| [openspec-verify](agents/openspec-verify.agent.md) | 验收 | `验收 OSC-260813c3e9` |
| [openspec-retro](agents/openspec-retro.agent.md) | 复盘 | `复盘 OSC-260813c3e9` |

## Harness 教训库

`harness/` 存放跨变更教训与索引校验：

- `lessons.md`：正文条目（`## OSC-… — <日期>`）与顶部「条目索引」**双向锁定**；新条目由 `openspec-retro` 追加并同步索引行；「待办」集中在文件头部。
- `verify-lessons.ps1`：只读校验——索引 ↔ 正文不一致、重复条目、归档 OSC 缺教训条目时报错（退出码 1）。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File NewLife.Cube.ArcoVue\openspec\harness\verify-lessons.ps1
```
