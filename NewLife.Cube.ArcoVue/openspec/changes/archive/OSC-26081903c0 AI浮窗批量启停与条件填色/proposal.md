# OSC-26081903c0 — AI 浮窗、批量启停与条件填色

## 1. 目标愿景

让已登录的 ArcoVue 管理员在不改权限契约、不新增写通道的前提下，用上与 MVC/Cube.Vue 对等的 AI 助手、对选中行批量启停，以及按命名视图规则给表格填色。

- 目标 1：`CubeSetting.AISwitch=true` 时壳内出现 AI 浮窗；对话走现有 `/Ai/AiChat` SSE；无开关/未登录不出现。
- 目标 2：表格「高级」菜单可对选中行调用现有 `EnableSelect`/`DisableSelect`；无 Enable 列或无 Update 时入口不可见。
- 目标 3：列表工具栏「填色」弹层可配最多 50 条**单条件**填色规则（单元格/行侧边/整行/整列），写入 `ViewsJson`；表格即时着色；**不是**列权限。

## 2. 为何做

竞品分析 §6.1 P0 #1 / #3 / #4 均为「方案内或低成本刚需」：后端 AI-7 与启停 API 已具备，条件格式前端即可。三项都是皮肤接线，共享「不破坏 GetPage / 菜单权限 / 行级 PERM-6」约束，适合一个 OSC。

与 **OSC-260819e483** 的边界：e483 P3 已覆盖「批量改任意字段」API + 高级菜单「批量修改」。本号 **只补启停**，不重做 PATCH/BatchUpdateFields。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **一个 OSC**，任务分 A（AI）/ B（启停）/ C（填色），可并行，禁止改通用基类方法签名。 |
| 2 | **不改** `/Ai/AiChat`、`GetAiConfig`、`EnableSelect`/`DisableSelect`/`EnableOrDisableSelect`、`GetPage`、ViewProfile 表结构。 |
| 3 | AI 仅 ArcoVue 壳；不改 Cube.Vue / NaiveUI / MVC 浮窗。协议对齐 Cube.Vue。**面板抄竞品右侧停靠 + 问候/三 Tab/建议列表**；标题仍为「AI 助手」。 |
| 4 | 批量启停仅 **table** 视图（与现有批量删除门禁一致）；树/卡/看板/日历/甘特不做多选启停。 |
| 5 | 填色入口对齐飞书：**工具栏「填色」+ 数字徽标 + 弹层「设置填色条件」**；不放进 ViewConfigDrawer。规则存当前 NamedView。按钮：table/tree/**card**。 |
| 6 | **一条规则 = 一个条件**（字段+操作符+值），不是 ViewFilter 多条件 AND/OR。背景通道与行侧边通道各自上→下先匹配。拖拽改序。 |
| 7 | 填色绘制：table/tree 用 VTable（cell/row/column 背景 + **side 行左侧 3px 竖条**）；card 仅 `side`（卡片左缘竖条）与 `row`（标题行底）；calendar/gantt **不**套用。 |
| 8 | 通用批量改字段、InsightPanel、列右冻结、i18n、字段级权限、公式列、**智能全局配色**：**不做**。 |

## 4. 做什么

**A. AI 助手浮窗**：RootLayout 挂载；右侧停靠面板（380px 全高）+ FAB；问候与推荐/提问/分析 Tab；快捷指令为可点列表；SSE 流式；`fill_form` / `run_js` 与 Vue 同协议。

**B. 批量启停**：高级菜单「批量启用」「批量禁用」；确认框；`cubeApi.page.enableSelect/disableSelect`；成功刷新列表。修正 `advancedVisible`：有批量修改或启停时也要出现「高级」。

**C. 条件填色**：工具栏「填色」弹层；`NamedView.format[]`；改即持久化。范围顺序：单元格 / **行侧边** / 整行 / 整列。卡片仅行侧边+整行。

## 5. 不做什么

- 不改 AI 工具集、提示词、AiController 鉴权。
- 不把 AI 放到登录/注册/忘记密码页。
- 不抄竞品品牌名「表哥」；不做会话历史列表、附件上传、「搭建」Tab、生成图表类快捷指令。
- 不把 EnableSelect 从 GET 改成 POST；不把 `EnableOrDisableSelect` 改成 `FindData`（既有行为，另号）。
- 不新增批量改任意字段 API（e483 P3）。
- 不用条件格式藏薪资列；不在 GetPage 下发规则当权限。
- 不做「智能全局配色」、渐变/色阶/图标/数据条、规则级 AND/OR、ViewConfigDrawer「条件格式」Tab。
- `column`（整列）按所选字段无条件铺满该列，不设操作符/值。卡片不提供单元格/整列。
- 卡片整行只涂标题条；行侧边只涂卡片左缘 3px，不涂整卡底。
- 不做填色弹层内的自然语言配色输入（截图顶栏「描述哪些内容用什么颜色」另号）。
- 不改 FlowGram、不引入 marked 以外的新 UI 框架。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| AI-7 / `AiController` / `GetAiConfig` | Done：本号只接线 |
| OSC-0007 / OSC-0009 | 高级菜单、Enable 徽标、`enableSelect` API |
| OSC-0012 / OSC-0015 | ViewsJson round-trip、`ViewFilter`/`matchesViewFilter` |
| OSC-0014 | 全局模板覆盖；`format` 作普通视图字段 |
| OSC-260819e483 P3 | 并行；本号不改其 BatchUpdateFields；仅修 `advancedVisible` 以免「只有修改/启停时高级菜单消失」 |

## 7. 测试范围

| 类型 | 是否做 | 说明 |
|------|--------|------|
| Vitest | 是 | AI markdown XSS、SSE、问候/快捷 Tab、启停门禁、format 归一与着色 |
| api-core | 是 | `config.getAiConfig` URL |
| XUnit | 否 | 不改 C# 行为 |
| 构建 | 是 | `@cube/api-core` + `@cube/arco-vue` |
| 手工 | 是 | 见 verify |

硬门禁：本号新增单测全过 + `pnpm --filter @cube/arco-vue test|build` 无错误。

## 8. 成功标准

- [ ] verify AC 覆盖开关关/无权限/空选中/非法 format/旧 ViewsJson。
- [ ] AISwitch=false 或 401 时无 FAB。
- [ ] 无 Enable 列的实体高级菜单无启停项。
- [ ] 条件格式刷新后仍在；导出/GetDetail 不因填色而少字段。
