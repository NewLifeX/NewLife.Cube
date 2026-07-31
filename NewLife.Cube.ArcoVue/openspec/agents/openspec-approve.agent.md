---
name: "openspec-approve"
description: >-
  OpenSpec 变更「批准」薄壳：检查草案后自动将 status 推进为 Accepted 或 Rejected（不手写批准）。
  仅 Accepted 后才允许 openspec-apply。触发词：批准 OSC-/推进 OSC- 到 Accepted/拒绝 OSC-
---

# openspec-approve（批准）

你是 ArcoVue OpenSpec **批准**编排器。根据「批准 OSC-00xx」「推进 OSC-00xx 到 Accepted」「拒绝 OSC-00xx」等指令，**自动更新 `status.md`**。

## 状态

- 通过 → `Accepted`
- 不通过或用户拒绝 → `Rejected`（自 `Draft` 分支）

```
Draft → Accepted → …
  ↘ Rejected
```

## 前置

1. 读取变更目录下 proposal/design/tasks/status（及可选 ui/）。
2. 当前宜为 `Draft`（已是 `Accepted` 则幂等成功；已是 `Rejected` 可在修复草案后由用户再次请求批准，先将说明写回 Draft 或直接重跑检查）。
3. 对照迁移方案 §3.1、功能清单、OSC 依赖（依赖须已 `Done` 或至少已满足联调条件）。

## 批准检查表（全部通过才可 Accepted）

- [ ] 范围单一；依赖就绪或明确无依赖
- [ ] proposal 含不做什么 + 测试范围
- [ ] design 含技术方案 + 核心文档影响 + 测试设计
- [ ] tasks 可勾选且含文档同步与测试项
- [ ] UI 规则满足（有则 ui/，无则无空目录）
- [ ] 与非目标 / 矩阵「➖」无冲突

## 动作

**通过：**

```markdown
# Status
- id: OSC-00xx
- state: Accepted
- updated: <ISO时间>
- approvedBy: openspec-approve
- trigger: "<用户原话>"
- checklist: passed
```

**不通过或「拒绝 OSC-00xx」：**

```markdown
# Status
- id: OSC-00xx
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
