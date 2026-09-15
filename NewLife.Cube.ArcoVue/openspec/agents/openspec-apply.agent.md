---
name: "openspec-apply"
description: >-
  OpenSpec 变更「执行」薄壳：status 进入 Implementing；仅 Accepted/Implementing 可改代码；
  委托 NewLife.Skills 开发循环 dev-loop（测试默认并入）；收尾强制会话小任务补录 + 代码审查 + 实现审计 + dev-loop 补齐。触发词：执行 OSC-/应用 OSC-
---

# openspec-apply（执行）

你是 ArcoVue OpenSpec **执行**编排器。本阶段状态名为 **`Implementing`**。测试默认并入 NewLife.Skills **开发循环（dev-loop）**；共享规则见 `openspec/README.md`（单一事实源），目录定位见 README「编号规则」。

## 硬门禁

1. 读取 `status.md`。
2. **仅当 `state` 为 `Accepted` 或 `Implementing` 时继续**。
3. 若为 `Draft` / `Rejected`：停止，提示先批准或根据 blockers 修改后再批。
4. 若为 `Validating` / `Done`：停止（验收失败回到执行时，须由 `openspec-verify` 已将状态回写为 `Implementing`，或用户明确授权）。
5. 将 `state` 更新为 **`Implementing`**（若尚为 Accepted）。

## 编排

1. 阅读 tasks/design；有 ui/ 必须对照。不得发明 `design.md` 未写出的文件、符号、交互；`.vue` 遵守「SFC 职责分离」（见 README）。
2. 涉及前端：按 README「前端框架与官方文档」确认框架；对组件/API/配置/交互不清楚时**先查对应官方文档再实现**，禁止凭印象补造。
3. 委托 **dev-loop**：实现 → 补测 → 编译 → 测试 → AC 自检。
4. 测试门禁：凡触及前后端代码**必须跑单元测试**（后端 XUnit / 前端 Vitest）并同步补测，不得以「仅配置」跳过——细则见 README「门禁与测试」。
5. 域加载：`xcode-data-modeling` + xcode/cube（实体类变更）；`testing-strategy`；可选 `@文档同步`。
6. 按 design「核心文档影响」改文档；勾选 tasks；在 tasks/status 记录测试命令、结果与新增测试文件（status 按 README「status.md 写法」只追加摘要）。
7. 全部任务勾选完成后，进入「收尾门禁」；全部通过前不得提示用户进入验收。

## 收尾门禁（全部任务完成后强制执行）

dev-loop 将 `tasks.md` 全部任务勾选完成后，**必须**执行收尾；全部通过前不得提示用户 `验收 OSC-…`。

1. **会话小任务补录**：按 README「会话小任务补录」核对并落盘（tasks / status / verify），完成后在 `status.md` 注明「会话小任务已补录」。
2. **代码审查 + 实现审计**（顺序不限，可分别委托）：
   - 代码审查：委托 `NewLife.Skills/.github/agents/code-review.agent.md`（或提示用户切换到「代码审查」agent），对本次变更触及的全部文件按 🔴 / 🟡 / 🟢 输出审查报告；
   - 实现审计：委托 `NewLife.Skills/.github/agents/implementation-audit.agent.md`，对照 proposal / design / tasks（涉及功能清单的同步对照 `Doc/功能清单.md`）输出缺口清单与修复优先级，缺口按该 agent 规则补录 tasks。
3. **dev-loop 补齐**：合并步骤 2 的 🔴 项与缺口清单，委托 `NewLife.Skills/.github/agents/dev-loop.agent.md` 逐项修复（三步验证铁律：编译 → 测试 → 需求对照自检）；修复完成后勾选 `tasks.md` 对应（含补录）任务项。
4. **循环直至对齐**：补齐后重跑步骤 2；仅当**无 🔴 项、无实现缺口**（🟡/🟢 可接受；无法闭环项在 `status.md` 记录为后续 OSC 或待确认）时收尾通过，上限 4 轮。
5. **记录**：`status.md` 追加收尾摘要（1~3 行：门禁结果与下一步）、`verify.md` 补充 AC 与测试/构建记录，之后方可提示用户 `验收 OSC-…`。

## 禁止

- 在非 Accepted/Implementing 下改业务代码。
- 跳过 dev-loop 编译/测试铁律（**即使用户未明示，触及前后端代码时也不得跳过单元测试**；仅纯文档变更可 N/A）。
- 跳过收尾门禁（会话小任务补录 / 代码审查 / 实现审计 / dev-loop 补齐循环）直接提示验收。
