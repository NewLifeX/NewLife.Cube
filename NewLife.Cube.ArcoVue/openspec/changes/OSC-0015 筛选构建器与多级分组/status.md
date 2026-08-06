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
- 结果：26 文件 / 263 用例全部通过（含字段类别/操作符矩阵、matchesViewFilter 全操作符、ViewFilter 归一、store filter/group、groupRows、viewMapping 组头/分组草稿）
- 命令：`npm.cmd --prefix NewLife.Cube.ArcoVue/web run build`（vue-tsc + vite）
- 结果：产物生成成功，vue-tsc 类型检查通过（Windows libuv 收尾崩溃不影响产物；退出码以 vue-tsc 为准）
- 浏览器冒烟：部门页「类型=公司」过滤 6→2 条、total 纠正；字符「名称包含公司」→2 条；「为空」无值控件且过滤生效；数字「排序>1」操作符集合正确；审计日志「创建者」等于/不等于 + 用户实体下拉（smokeuser/admin）；筛选应用即持久化、刷新保留、清除恢复；筛选为纯前端过滤（请求不含筛选参数）；无 JS 错误
- 分组勾选框重构（OSC-0015 需求 2/3 追加工单）：分组渲染从 tree/hierarchy 改为 **VTable 原生 groupBy + rowSeriesNumber checkbox**（参考官方 list-table-group-checkbox）；组标题 checkbox 级联勾选/取消组内子行（部门 4 行/公司 2 行）、数据行单独勾选、表头全选/取消、selectedKeys 正确同步；组标题文本「📁 部门 (4)」（dataSource 翻译 + count）；groupBy 字段名转 camelCase 匹配数据行；非分组回归正常（__checked 列恢复）；树视图禁分组操作
- 工具栏/配置抽屉追加工单：筛选/分组徽标底色改用当前主题 Primary 色（`--cube-primary`，外观设置可换；原 `--color-primary-6` 为 Arco 未定义变量导致透明）；工具栏移除「排序」按钮（排序由列表/树视图标题栏表头承担，自定义配置「工具栏/排序」开关只控制表头图标）；树状视图自定义配置抽屉工具栏删除「分组」选项（仅表格视图显示）
- api-core：未改动，无独立构建变更

