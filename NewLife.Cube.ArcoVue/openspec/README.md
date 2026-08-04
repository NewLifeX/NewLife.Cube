# NewLife.Cube.ArcoVue OpenSpec

本目录为 ArcoVue 产品化增量协作资产（**暂不**迁入 NewLife.Skills）。组织级能力通过**编排** [NewLife.Skills](https://github.com/NewLifeX/NewLife.Skills) 已有 instructions / skills / agents 获得。

权威流程见：[Doc/Api/ArcoVue企业中后台迁移方案.md](../../Doc/Api/ArcoVue企业中后台迁移方案.md) §9。

## 状态流转

每个 `changes/OSC-00xx <简述>/status.md`（归档同名规则，位于 `archive/`）：

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

**目录命名：** `OSC-00xx <简洁中文描述>`（编号与简述间一空格），如 `OSC-0001 协作基线与通路`、`OSC-0002 后端三实体`。进行中与 `archive/` 均使用该规则。

**门禁：** 仅 `Accepted`（首次）或 `Implementing`（续跑）可执行。批准用语：`批准 OSC-0001`、`推进 OSC-0001 到 Accepted.`；拒绝：`拒绝 OSC-0001`。

**测试与构建：** 触及前端或后端代码时——**执行阶段必须跑单元测试**（并补测）；**验收阶段须本 OSC 新增单测全部通过，且构建无错误抛出**。纯文档 / 纯 openspec 文案可声明 N/A。

## 前端框架与官方文档

ArcoVue 前端实现按场景固定使用以下框架；实现细节、组件 API、配置项或生命周期处理存在不确定时，**必须先查阅对应官方文档，再严格按官方文档实现**，不得凭印象补造 API、配置或交互。

| 场景 | 框架 | 官方资料 |
| --- | --- | --- |
| 设计系统、应用壳、表单及通用 UI | 字节跳动 Arco Design Vue | [快速上手](https://arco.design/vue/docs/start) |
| 多维数据视图 | VisActor VTable | [教程](https://arco.design/vue/docs/start) · [ListTable 配置](https://visactor.com/vtable/option/ListTable) · [实例接口](https://visactor.com/vtable/api/Methods) |
| 工作流 | FlowGram.AI | [指引](https://flowgram.ai/guide/getting-started/introduction.html) · [例子](https://flowgram.ai/examples/index.html) · [API](https://flowgram.ai/api/index.html) |

创建 OSC 时，应在 `design.md` 标明适用框架及需查阅的官方资料；执行 OSC 时，`openspec-apply` 负责落实本规则。

## 五壳 Agent

| Agent | 阶段 | 触发示例 |
|-------|------|----------|
| [openspec-create](agents/openspec-create.agent.md) | 创建 | `创建 OSC-0001：…` |
| [openspec-approve](agents/openspec-approve.agent.md) | 批准 | `批准 OSC-0001` |
| [openspec-apply](agents/openspec-apply.agent.md) | 执行 | `执行 OSC-0001` |
| [openspec-verify](agents/openspec-verify.agent.md) | 验收 | `验收 OSC-0001` |
| [openspec-retro](agents/openspec-retro.agent.md) | 复盘 | `复盘 OSC-0001` |
