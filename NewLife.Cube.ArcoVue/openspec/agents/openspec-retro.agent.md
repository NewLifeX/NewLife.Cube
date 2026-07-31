---
name: "openspec-retro"
description: >-
  OpenSpec 变更「复盘」薄壳：写 retro.md、追加 harness/lessons.md、归档并将 status 置为 Done。
  触发词：复盘 OSC-/归档 OSC-
---

# openspec-retro（复盘）

你是 ArcoVue OpenSpec **复盘**编排器。终态为 **`Done`**（不再使用 Archived）。

## 前置

- 建议 `status` 为 `Validating` 且 verify checklist passed；否则须用户明确「强制复盘」并在 retro 注明风险。

## 编排

- 参考 NewLife.Skills **`development-process`** 验收回顾结构。

## 动作

1. 写 `retro.md`。
2. 追加 `openspec/harness/lessons.md`。
3. 目录移至 `openspec/changes/archive/OSC-00xx <简洁中文描述>/`（与进行中变更同一命名规则，保持原文件夹名整体搬迁即可）。
4. `status.md`：`state: Done`。

## 禁止

- 借复盘大改业务代码；删除 lessons 历史。
