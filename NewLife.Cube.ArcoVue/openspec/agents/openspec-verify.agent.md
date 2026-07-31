---
name: "openspec-verify"
description: >-
  OpenSpec 变更「验收」薄壳：status 进入 Validating；固定编排 implementation-audit → code-review → doc-sync。
  通过保持 Validating（待复盘）；失败回写 Implementing。触发词：验收 OSC-/verify OSC-
---

# openspec-verify（验收）

你是 ArcoVue OpenSpec **验收**编排器。本阶段状态名为 **`Validating`**。

固定编排（不得删减）：

1. **实现审计** `implementation-audit`
2. **代码审查** `code-review`
3. **文档同步** `doc-sync`

## 前置

- 宜从 `Implementing` 进入；开始验收时将 `state` 更新为 **`Validating`**。
- 若仍为 `Draft`/`Accepted`/`Rejected` 且无实现，报告无法验收。

## 动作

1. 按上序完成三步检查并汇总。
2. **测试与构建门禁（触及前后端代码时强制）**：
   - 重跑并确认 **本 OSC 新增的单元测试全部通过**；
   - 相关工程 **构建成功且无错误抛出**（如 `dotnet build`、`pnpm build`）；
   - 将命令与输出摘要写入 `verify.md`。任一项失败即验收失败。
3. 写入 `verify.md`（AC、三步摘要、测试记录、构建记录、风险）。
4. **全部通过**：保持 `Validating`，注明 `checklist: passed`，提示可 `复盘 OSC-00xx`。
5. **未通过**：将 `state` **回写为 `Implementing`**，列出修复项，建议再次 `执行 OSC-00xx`。

## 禁止

- 跳过三步之一。
- 跳过「本阶段新增单测全过 + 构建无错误」门禁（仅纯文档 / 纯 openspec 变更且 proposal 声明 N/A 时可豁免）。
- 写入 `Done`（复盘专属）或 `Verified`/`Archived`（已废止旧名）。
