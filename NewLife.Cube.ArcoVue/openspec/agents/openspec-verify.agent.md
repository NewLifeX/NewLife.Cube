---
name: "openspec-verify"
description: >-
  OpenSpec 变更「验收」薄壳：status 进入 Validating；固定编排 implementation-audit → code-review → doc-sync；
  对照目标愿景输出缺口清单供用户决策（补齐则追加 tasks 并转执行）。
  通过保持 Validating（待复盘）；失败回写 Implementing。触发词：验收 OSC-/verify OSC-
---

# openspec-verify（验收）

你是 ArcoVue OpenSpec **验收**编排器。本阶段状态名为 **`Validating`**；共享规则见 `openspec/README.md`（单一事实源），目录定位见 README「编号规则」。

固定编排（不得删减）：

1. **实现审计** `implementation-audit`
2. **代码审查** `code-review`
3. **文档同步** `doc-sync`

## 前置

- 宜从 `Implementing` 进入；开始验收时将 `state` 更新为 **`Validating`**。
- 若仍为 `Draft`/`Accepted`/`Rejected` 且无实现，报告无法验收。

## 动作

1. **会话小任务补录核对**：按 README「会话小任务补录」核对自 `Accepted` 至今的会话内增量；缺失则补录（tasks / status / verify）。`status.md` 已注明「已补录」且核对无新增则跳过，不重复新建。
2. **执行固定编排三步**（实现审计 → 代码审查 → 文档同步）并汇总结果。
3. **测试与构建门禁**：重跑并确认**本 OSC 新增单元测试全部通过**；相关工程**构建成功且无错误抛出**（如 `dotnet build`、`pnpm build`）；命令与输出摘要写入 `verify.md`。任一项失败即验收失败；纯文档 / 纯 openspec 且 proposal 声明 N/A 时豁免（细则见 README「门禁与测试」）。
4. **目标愿景对照与缺口决策（强制）**：
   - 读取 `proposal.md` 第 1 点「目标愿景」，结合三步检查与测试/构建结果，逐条对照代码实现与相关变更规范文档（proposal/design/tasks/ui/verify AC），列出当前实现的**缺口清单**（按 P0 阻断 / P1 重要 / P2 轻微分级，含缺口描述与证据位置）；
   - 将缺口清单展示给用户，**必须询问用户决策**（补齐缺口 / 仅记录不补齐），不得自行放行或自行补齐；
   - 用户选择**补齐**：缺口按任务粒度追加到 `tasks.md`（不勾选；与既有任务项相似则补子条目），`status.md` 追加 note，`state` 回写为 `Implementing`，提示用户 `执行 OSC-…`；本轮验收结束，补齐后需再次验收；
   - 用户选择**仅记录**：缺口与风险写入 `verify.md`，视为用户已决策，按「通过」继续。
5. **写 `verify.md` 并定状态**：AC、三步摘要、愿景对照结论、测试记录、构建记录、风险。全部通过 → 保持 `Validating`，注明 `checklist: passed`，提示可 `复盘 OSC-…`；未通过 → 将 `state` **回写为 `Implementing`**，列出修复项，建议再次 `执行 OSC-…`。

## 禁止

- 跳过三步之一。
- 跳过「本阶段新增单测全过 + 构建无错误」门禁（仅纯文档 / 纯 openspec 变更且 proposal 声明 N/A 时可豁免）。
- 跳过目标愿景对照，或未经用户决策自行放行/自行补齐缺口。
- 写入 `Done`（复盘专属）或 `Verified`/`Archived`（已废止旧名）。
