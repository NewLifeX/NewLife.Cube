# Status

- id: OSC-0015
- state: Implementing
- updated: 2026-08-06T12:55:00+08:00
- approvedBy: openspec-approve
- trigger: "批准并执行 OSC-0015。"
- checklist: passed
- executor: openspec-apply

## 测试记录

- 命令：`npm.cmd --prefix NewLife.Cube.ArcoVue/web run test`
- 结果：26 文件 / 260 用例全部通过（含新增 filterBuilder 8 例、store filter/group 5 例、viewMapping 组头/分组草稿 9 例、searchFilters 与 viewProfile 既有用例）
- 命令：`npm.cmd --prefix NewLife.Cube.ArcoVue/web run build`（vue-tsc + vite）
- 结果：退出码 0，无类型错误
- api-core：未改动，无独立构建变更（前端 store 仅消费既有 API 形态）
- 手工冒烟（T5.3）：待用户环境执行，见 verify.md

