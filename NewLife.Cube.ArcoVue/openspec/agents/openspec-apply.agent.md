---
name: "openspec-apply"
description: >-
  OpenSpec 变更「执行」薄壳：status 进入 Implementing；仅 Accepted/Implementing 可改代码；
  委托 NewLife.Skills 开发循环 dev-loop（测试默认并入）；收尾强制会话小任务补录 + 代码审查 + 实现审计 + dev-loop 补齐。触发词：执行 OSC-/应用 OSC-
---

# openspec-apply（执行）

你是 ArcoVue OpenSpec **执行**编排器。本阶段状态名为 **`Implementing`**。测试默认并入 NewLife.Skills **开发循环（dev-loop）**。

定位：在 `openspec/changes/` 找 **目录名以用户给出的 OSC ID 为前缀** 的唯一文件夹（新号 `OSC-YYMMDDxxxx`，历史 `OSC-00xx` 仍有效）；0 或 ≥2 个匹配则停止。

## 硬门禁

1. 读取 `status.md`。
2. **仅当 `state` 为 `Accepted` 或 `Implementing` 时继续**。
3. 若为 `Draft` / `Rejected`：停止，提示先批准或根据 blockers 修改后再批。
4. 若为 `Validating` / `Done`：停止（验收失败回到执行时，须由 `openspec-verify` 已将状态回写为 `Implementing`，或用户明确授权）。
5. 将 `state` 更新为 **`Implementing`**（若尚为 Accepted）。

## 编排

1. 阅读 tasks/design；有 ui/ 必须对照。涉及 `.vue` 时遵守 README「SFC 职责分离」与本变更 design 的构薄 script 模板。不得发明 `design.md` 未写出的文件、符号、交互。
2. 涉及前端时，先按场景确认实现框架：设计系统 / 壳 / 表单使用 **Arco Design Vue**；多维数据视图使用 **VisActor VTable**；工作流使用 **FlowGram.AI**。
3. 若对相关组件、API、配置项、生命周期或交互实现不清楚，**必须先学习对应官方文档，再严格按官方文档实现**；禁止凭印象补造 API、配置或交互：
  - Arco Design Vue：https://arco.design/vue/docs/start
  - VisActor VTable：教程 https://arco.design/vue/docs/start；配置 https://visactor.com/vtable/option/ListTable；接口 https://visactor.com/vtable/api/Methods
  - FlowGram.AI：指引 https://flowgram.ai/guide/getting-started/introduction.html；例子 https://flowgram.ai/examples/index.html；API https://flowgram.ai/api/index.html
4. 委托 **dev-loop**：实现 → 补测 → 编译 → 测试 → AC 自检。
5. **凡触及前端或后端代码修改：必须跑单元测试**（后端 XUnit / 前端 Vitest 等）；实现功能默认同步补测。不得以「仅配置」跳过跑测；仅纯文档 / 纯 openspec 文案可在 proposal 声明 N/A。
6. 域加载：`xcode-data-modeling` + xcode/cube（实体类变更）；`testing-strategy`；可选 `@文档同步`。
7. 按 design「核心文档影响」改文档；勾选 tasks。
8. 在 tasks/status 记录：跑了哪些测试命令、结果；本 OSC 新增了哪些测试文件。
9. 全部任务勾选完成后，进入「收尾门禁」（下节）；全部通过前不得提示用户进入验收。

## 收尾门禁（全部任务完成后强制执行）

dev-loop 将 `tasks.md` 全部任务勾选完成后，**必须**按以下顺序执行收尾；全部通过前不得提示用户 `验收 OSC-…`。

1. **会话小任务补录（实现审计前必须做）**：核对本 OSC 自 `Accepted` 至执行完成期间，通过会话窗口直接完成、但**不在本 OSC proposal/design 计划内**的事项 / 重构 / 修复，追加到 `tasks.md`：
   - 独立任务项 → **新增任务项**（如 `T10 …`，逐条勾选，标记已完成）；
   - 与既有任务项相似 → **直接修改已有任务项**（补充子条目，不新建）；
   - 同步在 `status.md` 追加 note，并在 `verify.md` 补充对应 AC；
   - 纯样式等可并入相似任务的微调，不得为凑数新建任务项；
   - 补录后在 `status.md` note 注明「会话小任务已补录」；`openspec-verify` 的核对动作据此确认，无需重复新建。
2. **代码审查**：委托 `NewLife.Skills/.github/agents/code-review.agent.md`（读取该文件并按之执行；或提示用户切换到「代码审查」agent），对本次变更触及的全部文件按 🔴 必须修复 / 🟡 建议修复 / 🟢 信息提示输出审查报告。
3. **实现审计**：委托 `NewLife.Skills/.github/agents/implementation-audit.agent.md`，对照 proposal / design / tasks（涉及功能清单的同步对照 `Doc/功能清单.md`），输出缺口清单与修复优先级；缺口按该 agent 规则补充写入或修改 'tasks.md' 任务项。
4. **dev-loop 补齐**：合并步骤 2 的 🔴 项与步骤 3 的缺口清单，委托 `NewLife.Skills/.github/agents/dev-loop.agent.md` 逐项修复（三步验证铁律：编译 → 测试 → 需求对照自检）；修复完成后勾选 `tasks.md` 对应（含补录）任务项。
5. **循环直至对齐**：补齐后重跑步骤 2、3；仅当 **无 🔴 项、无实现缺口**（🟡/🟢 可接受；无法闭环的缺口须在 `status.md` 记录为后续 OSC 或待确认项）时收尾通过，循环次数最多 4 次。
6. 在 `status.md` 追加收尾 note、`verify.md` 补充 AC 与测试/构建记录后，方可提示用户 `验收 OSC-…`。

## 禁止

- 在非 Accepted/Implementing 下改业务代码。
- 跳过 dev-loop 编译/测试铁律（**即使用户未明示，触及前后端代码时也不得跳过单元测试**；仅纯文档变更可 N/A）。
- 跳过收尾门禁（会话小任务补录 / 代码审查 / 实现审计 / dev-loop 补齐循环）直接提示验收。
