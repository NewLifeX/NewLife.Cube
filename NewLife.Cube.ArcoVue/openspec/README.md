# NewLife.Cube.ArcoVue OpenSpec

本目录为 ArcoVue 产品化增量协作资产（**暂不**迁入 NewLife.Skills）。组织级能力通过**编排** [NewLife.Skills](https://github.com/NewLifeX/NewLife.Skills) 已有 instructions / skills / agents 获得。

权威流程见：[ArcoVue企业中后台迁移方案.md](../ArcoVue企业中后台迁移方案.md) §9。

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
- 定位：触发语中的 ID 必须能在 `changes/` 或 `archive/` 唯一匹配目录名前缀。

**门禁：** 仅 `Accepted`（首次）或 `Implementing`（续跑）可执行。批准用语：`批准 OSC-260813c3e9`、`推进 OSC-260813c3e9 到 Accepted.`；拒绝：`拒绝 OSC-260813c3e9`。旧号同理。

**测试与构建：** 触及前端或后端代码时——**执行阶段必须跑单元测试**（并补测）；**验收阶段须本 OSC 新增单测全部通过，且构建无错误抛出**。纯文档 / 纯 openspec 文案可声明 N/A。

## 前端框架与官方文档

ArcoVue 前端实现按场景固定使用以下框架；实现细节、组件 API、配置项或生命周期处理存在不确定时，**必须先查阅对应官方文档，再严格按官方文档实现**，不得凭印象补造 API、配置或交互。

| 场景 | 框架 | 官方资料 |
| --- | --- | --- |
| 设计系统、应用壳、表单及通用 UI | 字节跳动 Arco Design Vue | [快速上手](https://arco.design/vue/docs/start) |
| 图标（全局 `<icon-park type>`） | IconPark `@icon-park/vue-next` | [官方图标库](https://iconpark.oceanengine.com/official) · GitHub [bytedance/IconPark](https://github.com/bytedance/IconPark)（vue-next README） |
| 多维数据视图 | VisActor VTable | [教程](https://arco.design/vue/docs/start) · [ListTable 配置](https://visactor.com/vtable/option/ListTable) · [实例接口](https://visactor.com/vtable/api/Methods) |
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
