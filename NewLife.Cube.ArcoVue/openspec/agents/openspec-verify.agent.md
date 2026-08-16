---
name: "openspec-verify"
description: >-
  OpenSpec 变更「验收」薄壳：status 进入 Validating；固定编排 implementation-audit → code-review → doc-sync；
  对照目标愿景输出缺口清单供用户决策（补齐则追加 tasks 并转执行）。
  通过保持 Validating（待复盘）；失败回写 Implementing。触发词：验收 OSC-/verify OSC-
---

# openspec-verify（验收）

你是 ArcoVue OpenSpec **验收**编排器。本阶段状态名为 **`Validating`**。

定位：在 `openspec/changes/` 找 **目录名以用户给出的 OSC ID 为前缀** 的唯一文件夹（新号 `OSC-YYMMDDxxxx`，历史 `OSC-00xx` 仍有效）。

固定编排（不得删减）：

1. **实现审计** `implementation-audit`
2. **代码审查** `code-review`
3. **文档同步** `doc-sync`

## 前置

- 宜从 `Implementing` 进入；开始验收时将 `state` 更新为 **`Validating`**。
- 若仍为 `Draft`/`Accepted`/`Rejected` 且无实现，报告无法验收。

## 动作

1. **会话小任务补录**：核对自 `Accepted` 至验收期间，通过会话窗口直接完成、但**不在本 OSC proposal/design 计划内**的事项 / 重构 / 修复，追加到 `tasks.md`：
   - 独立任务项 → **新增任务项**（如 `T10 …`，逐条勾选）；
   - 与既有任务项相似 → **直接修改已有任务项**（补充子条目，不新建）；
   - 同步在 `status.md` 追加 note，并在 `verify.md` 补充对应 AC；
   - 纯样式等可并入相似任务的微调，不得为凑数新建任务项；
   - 若 `status.md` 已注明「会话小任务已补录」（`openspec-apply` 收尾门禁完成）且核对无新增，则跳过，不得重复新建。
2. 按上序完成三步检查并汇总。
3. **测试与构建门禁（触及前后端代码时强制）**：
   - 重跑并确认 **本 OSC 新增的单元测试全部通过**；
   - 相关工程 **构建成功且无错误抛出**（如 `dotnet build`、`pnpm build`）；
   - 将命令与输出摘要写入 `verify.md`。任一项失败即验收失败。
4. **目标愿景对照与缺口决策（强制）**：
   - 读取 `proposal.md` 第 1 点「目标愿景」，结合三步检查与测试/构建结果，逐条对照代码实现与相关变更规范文档（proposal/design/tasks/ui/verify AC），列出当前实现的**缺口清单**（按 P0 阻断 / P1 重要 / P2 轻微分级，含缺口描述与证据位置）；
   - 将缺口清单展示给用户，**必须询问用户决策**（补齐缺口 / 仅记录不补齐），不得自行放行或自行补齐；
   - 用户选择**补齐缺口**：将缺口按任务粒度**追加到 `tasks.md`**（新增任务项，不勾选；与既有任务项相似则补充子条目，不新建），`status.md` 追加 note（验收发现缺口待补），将 `state` 回写为 `Implementing`，并**调用执行智能体补齐**——提示用户 `执行 OSC-…`（`openspec-apply`），补齐后需再次 `验收 OSC-…`；本轮验收结束；
   - 用户选择**仅记录不补齐**：缺口与风险写入 `verify.md`，视为用户已决策，按「通过」继续。
5. 写入 `verify.md`（AC、三步摘要、愿景对照结论、测试记录、构建记录、风险）。
6. **全部通过**：保持 `Validating`，注明 `checklist: passed`，提示可 `复盘 OSC-YYMMDDxxxx`（历史号同理）。
7. **未通过**：将 `state` **回写为 `Implementing`**，列出修复项，建议再次 `执行 OSC-YYMMDDxxxx`。

## 禁止

- 跳过三步之一。
- 跳过「本阶段新增单测全过 + 构建无错误」门禁（仅纯文档 / 纯 openspec 变更且 proposal 声明 N/A 时可豁免）。
- 跳过目标愿景对照，或未经用户决策自行放行/自行补齐缺口。
- 写入 `Done`（复盘专属）或 `Verified`/`Archived`（已废止旧名）。
