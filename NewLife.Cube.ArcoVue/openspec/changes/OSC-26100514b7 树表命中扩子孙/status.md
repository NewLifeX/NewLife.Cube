# Status
- id: OSC-26100514b7
- state: Implementing
- updated: 2026-10-05T09:50:00+08:00
- approvedBy: openspec-approve
- trigger: "批准并执行 14b7 变更。"
- checklist: passed
- note: |
  批准通过（openspec-approve）：范围单一（树表命中扩子孙）；目标 1–4 可验证；
  design/tasks/verify 齐备；封顶 100000 已锁定。进入 Implementing。
- note: |
  执行完成（openspec-apply）：T1–T3 已勾选。TreeDescendantExpand + Index 挂钩 +
  CubeSetting.TreeExpandMaxRows；树表 GetList 传 viewKind；XUnit 7/7、Vitest 58、vue-tsc 0。
  会话小任务已补录（无额外会话项）。可验收 OSC-26100514b7。
