---
name: "openspec-retro"
description: >-
  OpenSpec 变更「复盘」薄壳：写 retro.md、追加 harness/lessons.md、归档并将 status 置为 Done。
  触发词：复盘 OSC-/归档 OSC-
---

# openspec-retro（复盘）

你是 ArcoVue OpenSpec **复盘**编排器。终态为 **`Done`**（不再使用 Archived）；共享规则见 `openspec/README.md`（单一事实源），目录定位见 README「编号规则」。

## 前置

- 建议 `status` 为 `Validating` 且 verify checklist passed；否则须用户明确「强制复盘」并在 retro 注明风险。

## 编排

- 参考 NewLife.Skills **`development-process`** 验收回顾结构。

## 动作

1. **会话小任务复核补录**：归档前按 README「会话小任务补录」再次核对自验收至复盘期间的会话内增量；缺失则补录，并同步 `status.md` note 与 `retro.md` 实际完成范围。
2. 写 `retro.md`。
3. 追加 `openspec/harness/lessons.md` 条目：格式 `## OSC-… — <日期>`，**并同步顶部「条目索引」加一行**（条目名 = 正文标题原文）。
4. **先改状态、后移动（防竞态残留）**：将 `status.md` 置为 `state: Done`（连同 retro/verify/tasks 的补录一并在此步完成）。**对本变更目录内全部文件的编辑必须在目录移动之前全部结束**；目录移动完成后禁止再用编辑工具触碰该目录任何文件——否则编辑器文档缓冲会在旧路径重新写回，导致 `changes/` 下残留重复副本（0008/0009/0015 曾多次复现）。
5. **整体搬迁**：目录移至 `openspec/changes/archive/{原文件夹名}/`（新号 `OSC-YYMMDDxxxx <简述>`，历史 `OSC-00xx <简述>`；保持原文件夹名整体搬迁）。用文件系统命令（`Move-Item` / `mv`）一次移动整个目录，禁止逐文件复制或移动后再编辑。
6. **归档后校验（防竞态残留）**：确认 `openspec/changes/` 下该变更目录已消失；若旧路径仍有残留文件（内容与 `archive/` 同名文件比对哈希），**哈希一致则直接删除残留**（纯竞态副本，无需人工确认）；哈希不一致才保留并人工确认。并运行 `powershell -NoProfile -ExecutionPolicy Bypass -File NewLife.Cube.ArcoVue\openspec\harness\verify-lessons.ps1`，确认索引与归档覆盖无 ERROR（退出码 0）。校验通过前不得提交。
7. **提交本轮修改**：复盘归档完成后，按仓库 git 提交规范创建一次（或按 OSC 惯例一条）commit，纳入本 OSC 业务代码、OpenSpec 归档、`harness/lessons.md`、相关文档与必要构建产物；**排除**与本 OSC 无关的 WIP（如临时 patch、他皮肤误改）。提交信息对齐近期风格（如 `feat(arco): OSC-260813c3e9 …` 或历史 `feat(arco): OSC-00xx …`）。用户已说「验收并复盘」或「复盘」且未禁止提交时，本步默认执行，无需再单独要一句「请提交」。

## 禁止

- 借复盘大改业务代码；删除 lessons 历史。
- 把无关 WIP / 密钥文件打进复盘提交。
