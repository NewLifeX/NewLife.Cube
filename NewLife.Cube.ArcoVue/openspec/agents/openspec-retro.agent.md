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

1. **会话小任务复核补录**：归档前再次核对 `tasks.md` 是否完整覆盖自验收至复盘期间通过会话窗口直接完成、且不在 OSC 计划内的事项 / 重构 / 修复；缺失则按「独立 → 新增任务项、相似 → 并入已有任务项」补录，并同步 `status.md` note 与 `retro.md` 实际完成范围。
2. 写 `retro.md`。
3. 追加 `openspec/harness/lessons.md`。
4. 目录移至 `openspec/changes/archive/OSC-00xx <简洁中文描述>/`（与进行中变更同一命名规则，保持原文件夹名整体搬迁即可）。
   - **归档后校验（防竞态残留）**：确认 `openspec/changes/` 下该变更目录已消失；若旧路径残留文件（编辑工具与文件移动竞态导致，0008/0009 曾复现），先与 `archive/` 同名文件比对哈希，一致则删除残留，不一致则保留并人工确认。
5. `status.md`：`state: Done`。
6. **提交本轮修改**：复盘归档完成后，按仓库 git 提交规范创建一次（或按 OSC 惯例一条）commit，纳入本 OSC 业务代码、OpenSpec 归档、`harness/lessons.md`、相关文档与必要构建产物；**排除**与本 OSC 无关的 WIP（如临时 patch、他皮肤误改）。提交信息对齐近期风格（如 `feat(arco): OSC-00xx …`）。用户已说「验收并复盘」或「复盘」且未禁止提交时，本步默认执行，无需再单独要一句「请提交」。

## 禁止

- 借复盘大改业务代码；删除 lessons 历史。
- 把无关 WIP / 密钥文件打进复盘提交。
