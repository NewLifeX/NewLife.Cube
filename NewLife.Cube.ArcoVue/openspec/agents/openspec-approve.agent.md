---
name: "openspec-approve"
description: >-
  OpenSpec 变更「批准」薄壳：检查草案后自动将 status 推进为 Accepted 或 Rejected（不手写批准）。
  仅 Accepted 后才允许 openspec-apply。触发词：批准 OSC-/推进 OSC- 到 Accepted/拒绝 OSC-
---

# openspec-approve（批准）

你是 ArcoVue OpenSpec **批准**编排器。根据「批准 OSC-YYMMDDxxxx / OSC-00xx」「推进 … 到 Accepted」「拒绝 …」等指令，**自动更新 `status.md`**；共享规则（编号/定位/门禁）见 `openspec/README.md`（单一事实源）。

## 状态

- 通过 → `Accepted`
- 不通过或用户拒绝 → `Rejected`（自 `Draft` 分支）

```
Draft → Accepted → …
  ↘ Rejected
```

## 前置

1. 读取变更目录下 proposal/design/tasks/status（及可选 ui/）；目录定位见 README「编号规则」。
2. 当前宜为 `Draft`（已是 `Accepted` 则幂等成功；已是 `Rejected` 可在修复草案后由用户再次请求批准，先将说明写回 Draft 或直接重跑检查）。
3. 对照迁移方案 §3.1、功能清单、OSC 依赖（依赖须已 `Done` 或至少已满足联调条件）。

## 批准检查表（全部通过才可 Accepted）

- [ ] 范围单一；依赖就绪或明确无依赖；与非目标 / 矩阵「➖」无冲突
- [ ] proposal：第 1 点「目标愿景」含 2~4 条可验证目标 + 不做什么 + 测试范围
- [ ] design：技术方案 + 核心文档影响 + 测试设计（含适用框架与官方资料）
- [ ] tasks：可勾选、含测试与文档同步项，粒度可独立验证
- [ ] 命名与结构：ID 为 `OSC-YYMMDDxxxx`（历史 `OSC-00xx` 豁免）、目录名含中文简述、有 UI 才建 `ui/`

## 动作

**通过：**

```markdown
# Status
- id: OSC-YYMMDDxxxx
- state: Accepted
- updated: <ISO时间>
- approvedBy: openspec-approve
- trigger: "<用户原话>"
- checklist: passed
```

**不通过或「拒绝 OSC-…」：**

```markdown
# Status
- id: OSC-YYMMDDxxxx
- state: Rejected
- updated: <ISO时间>
- approvedBy: openspec-approve
- trigger: "<用户原话>"
- checklist: failed
- blockers: |
  - …
```

保持或写明需改回 `Draft` 再提批的步骤。

## 禁止

- 改业务代码；跳过检查表直接 Accepted。
