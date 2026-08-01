# OSC-0005 — VTable 表格 + 列布局 + 多命名视图（仅 table）

## 1. 为何做

对齐迁移方案 **M3a** 与飞书多维表「表格视图」体感：高性能列表舞台、列可配置并按用户持久化；在 **EntityViewProfile** 上支持**多个命名视图**（本号仅 `table` 类型），为 OSC-0006 多视图类型切换打底。

## 2. 已锁定范围（澄清结论）

| # | 决策 |
|---|------|
| 1 | **B**：列显隐 + 拖顺序 + 拖宽度 + 表头排序（优先接 `PageParams.sort/desc`；否则仅当前页） |
| 2 | **A**：本号**不做** table/tree/card/gantt 切换器；非 table → OSC-0006 |
| 3 | **B**：列表一律 VTable **扁平**；拿掉 0003 树表启发式（树 → 0006） |
| 4 | **A**：不细胞编辑；行点/操作 → **右侧抽屉** |
| 5 | **A**：`filtersJson` 本号不消费（可预留）；搜索表单维持现状 |
| 6 | **A**：左冻结（含操作列策略写进 design） |
| 7 | **A**：默认列表全面替换 `a-table`→VTable 适配层；LOV 小表可暂留 a-table |
| 8 | **B**：支持**多命名视图**；本号新建/切换的视图类型**仅 table**（默认一条「列表」） |
| 9 | **B**：本变更以 OpenSpec Draft 落盘 |

## 3. 做什么

- 引入 VisActor VTable（`@visactor/vue-vtable` 或 `@visactor/vtable` 适配层）。
- `DefaultList` 主表 → ListTable：列显隐/顺序/宽度、左冻结、表头排序、行选/行点抽屉。
- 消费并扩展 **EntityViewProfile**：`GET/PUT/DELETE`；列写入；**命名视图**（见 design）。
- 后端小扩展：`ViewsJson` + `ActiveViewId`（Cube.xml → 生成 → Upsert；Cube/CubeNC API 透传）。
- 工具条：命名视图切换/新建/重命名/删除（仅 table）+「字段」列设置。
- Vitest 关键路径 + `pnpm build`；后端 XUnit 覆盖 ViewsJson upsert。

## 4. 不做什么

- tree / card / gantt 渲染与类型切换器（→ OSC-0006）。
- 飞书式筛选面板 / 分组 / `filtersJson` 持久化。
- 单元格内编辑；改抽屉方向。
- 完整「多维表」字段类型建模（仍用 Cube DataField）。
- 改 Cube.Vue / NaiveUI。

## 5. 依赖

| 依赖 | 关系 |
|------|------|
| OSC-0002 | Done：EntityViewProfile API |
| OSC-0003 | Done：DefaultList / 抽屉契约（本号替换表体，保留搜索/工具/抽屉） |
| OSC-0004 | 软：壳隔离；CRUD 仍不读 userProfileStore |

## 6. 测试范围

| 类型 | 是否做 |
|------|--------|
| Vitest（列 merge、命名视图、sort 参数形状、冻结映射） | **是** |
| XUnit（ViewsJson/ActiveViewId upsert + 隔离） | **是**（后端有改） |
| `pnpm build`（ArcoVue web）+ 相关 `dotnet` 测试工程 | **是** |
| E2E | 否 |
| 手工冒烟 | 是（列偏好刷新/重登；多命名视图；排序；抽屉右侧） |

## 7. 成功标准

- [ ] 默认列表为 VTable ListTable，无树启发式
- [ ] 列显隐/顺序/宽度/左冻结可改并写入 Profile；刷新仍在
- [ ] 表头排序走 `sort`/`desc`（或文档标明仅当前页降级）
- [ ] 多命名 table 视图：默认「列表」；可新建/切换/重命名/删除；类型不可选非 table
- [ ] 行点/操作打开右侧抽屉；CRUD 不读壳 store
- [ ] 本 OSC 新增单测全过；构建无错误
- [ ] 迁移方案 M3a / 对接文档已回写
