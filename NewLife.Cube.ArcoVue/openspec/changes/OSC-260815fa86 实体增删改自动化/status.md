# Status
- id: OSC-260815fa86
- state: Implementing
- updated: 2026-08-16T12:45:00+08:00
- approvedBy: openspec-approve
- trigger: "批准并执行 OSC-260815fa86。"
- checklist: passed
- note: |
    实现完成，未改 state（仍为 Implementing，待 openspec-verify）。
    2026-08-16 文档回写：对齐近期 UI/API 定稿——飞书双栏编辑器、字段条件卡片、
    notify 用户/角色/部门三选一、Recipients/Entities/Inbox API、动作 ⋯ 菜单、
    壳站内通知 remind、添加动作不含 runAutomation、found 链路校验等。
    详见 proposal/design/`ui/information-architecture.md`。
    测试基线（此前）：pnpm api-core/arco-vue test+build；dotnet Osc260815 8 pass。
    手工冒烟（Admin/User insert→InApp + 站内通知）verify 阶段补。
