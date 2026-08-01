# OSC-0006 — 多视图类型（表/树/卡片/看板/日历/甘特）+ Tab 工作台

## 1. 为何做

承接 OSC-0005 命名视图与 VTable 表格舞台，对齐飞书多维表「基础视图」子集：在同一实体列表上切换 **表格 / 树表格 / 卡片 / 看板 / 日历 / 甘特**，用视图提升浏览体验；配置仍落在 EntityViewProfile `ViewsJson`，**不新增后端查询协议**。

参考：[飞书多维表格 · 视图](https://www.feishu.cn/hc/zh-CN/category/7006240778276110337-%E8%A7%86%E5%9B%BE)。

## 2. 已锁定范围（澄清结论）

| # | 决策 |
|---|------|
| 1 | 日历自定义配置：**开始日期必选 + 结束日期可选 + 标题展示 + 颜色显示** |
| 2 | 看板：**只读分列**；本号不做拖拽改分组写回 |
| 3 | 看板/日历/甘特：进入视图时 **较大 pageSize（建议 200，可至 500）** 仍走 GetList；table/tree/card 维持常规分页 |
| 4 | 树表格：无 Parent/`children` 等树元数据时 **禁止创建** |
| 5 | 卡片/看板左下操作：有权则 **详情 · 编辑 · 删除**（与表格操作列一致） |
| 6 | Tab 条 + `···` 菜单 + 最右 `+` 新建（替换 OSC-0005 下拉主路径） |
| 7 | 自定义配置「列表区」按 `ViewKind` 替换为类型映射字段 |
| 8 | 映射存 `NamedView.mapping`（`ViewsJson` 内）；不强制写 `ganttJson`/`cardJson` |
| 9 | 本变更以 OpenSpec **Draft** 落盘；不含画册/表单/保护视图/筛选面板 |

## 3. 做什么

- 扩展 `ViewKind`：`table | tree | card | kanban | calendar | gantt`；解除 FE「舞台仅 table」强制。
- `ViewTabsToolbar`：Tab 切换、活跃视图菜单（重命名/删除）、`+` 按类型新建（含创建门禁）。
- `ViewConfigDrawer`：按类型替换「列表区」——卡片标题/图；看板分组依据+卡片设置；甘特/日历起止+标题+颜色。
- 渲染：`CardList`、`KanbanBoard`、`CalendarMonth`、`GanttView`；tree 复用/扩展 VTable hierarchy。
- Vitest（mapping、门禁、kanban 分桶、pageSize 策略）+ `pnpm build`；回写 M3b / 对接指南 / README。

## 4. 不做什么

- 画册、表单视图、视图保护/独立分享。
- 看板/日历/甘特 **拖拽写回** 字段。
- `filtersJson` 筛选条、跨视图协同。
- 新后端 API 或改 Cube 实体列结构。
- 改记录抽屉方向；读 `userProfileStore`。

## 5. 依赖

| 依赖 | 关系 |
|------|------|
| OSC-0005 | Done：VTable + ViewsJson/ActiveViewId + ViewConfigDrawer |
| OSC-0002 | Done：EntityViewProfile API |
| OSC-0003 | Done：DefaultList / 右侧抽屉 / 权限 |

## 6. 测试范围

| 类型 | 是否做 |
|------|--------|
| Vitest（ViewKind/mapping normalize、创建门禁、kanban 分桶、大 pageSize 策略） | **是** |
| XUnit | **否**（无后端契约变更；N/A） |
| `pnpm build`（ArcoVue web） | **是** |
| E2E | 否 |
| 手工冒烟 | 是（各类型创建/切换/配置持久化/操作按钮） |

## 7. 成功标准

- [x] Tab + `+` 可创建/切换 table/tree/card/kanban/calendar/gantt（门禁生效）
- [x] 各类型「列表区」配置正确替换并写入 `ViewsJson.mapping`
- [x] 卡片/看板左下：有权显示详情/编辑/删除并打开右侧抽屉或删除确认
- [x] 看板只读分列；日历/甘特按映射渲染；非表视图使用大 pageSize
- [x] 无树元数据时不能创建 tree
- [x] 本 OSC 新增单测全过；`pnpm build` 无错误
- [x] 迁移方案 M3b / 对接指南 / README 已回写
