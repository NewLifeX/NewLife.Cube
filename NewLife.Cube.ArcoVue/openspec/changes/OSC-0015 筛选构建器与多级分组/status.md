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
- 结果：26 文件 / 261 用例全部通过（含新增 filterBuilder 8 例、store filter/group 5 例、viewMapping 组头/分组草稿 9 例、matchesViewFilter 枚举字符串匹配 1 例）
- 命令：`npm.cmd --prefix NewLife.Cube.ArcoVue/web run build`（vue-tsc + vite）
- 结果：产物生成成功，vue-tsc 类型检查通过（Windows libuv 收尾崩溃不影响产物；退出码以 vue-tsc 为准）
- 浏览器冒烟：部门页「类型=公司」筛选应用后表格 6→2 条、分页 total 纠正为 2、刷新后筛选/分组保留（应用即持久化）、清除后刷新恢复 6 条、筛选字段候选为本视图可见列、搜索面板折叠、无 JS 错误
- api-core：未改动，无独立构建变更

