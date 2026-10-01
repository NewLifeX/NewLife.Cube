# OSC-2610011ff2 Tasks — 隐藏功能入口

## T1 路由白名单

- [x] T1-1 新建 `hiddenEntryRoutes.ts` 与 spec（design §8 前两条）。
- [x] T1-2 `router/index.ts` 按白名单生成 `DynamicPage` 子路由。不改 `Menu` 实体、不改控制器 `[Menu]` 可见参数。页签标题用白名单中文名，避免路由名进页签。
- [x] T1-3 `oaLeafRoutes` 增加 Efficiency。

## T2 入口

- [x] T2-1 `accountLinks.ts` + spec。账号中心页签「关联」调用 `useAccountLinks`。`userInfo` 为空时补拉一次 `fetchUserInfo`，仍无有限正整数 id 则除用户统计外禁用。
- [x] T2-2 `objectMoreLinks.ts` + spec。`useDefaultObject` 左侧追加「更多」，点击 push。

## T3 用户统计图

- [x] T3-1 抽出 `chartOptions.ts`，列表图表弹窗改为调用。补空数组与原样数组两条测试。
- [x] T3-2 `useUserStatChart.ts`：仅 `Admin/UserStat` 请求 GetChartData。经 `mergeDeveloperCharts` 合成最多 2 张固定 `legacyChart`（整行 w=12；折线略压、箱线/K 线略增高），按迷你图表部件外观展示；标题取自 option 并去掉图内 title；不接升级/编辑/删除，可在其后添加自定义部件。
  - [x] **会话小任务**：洞察区固定部件（非整半栅格、非工具栏上方独立面板）；`WidgetHost` 对 `dev-chart-*` 隐藏设置/升级/删除。
  - [x] **会话小任务**：`arco-spin` 改为块级避免宽为 0；`useLegacyChartWidget` 监听 `chartEl` + `ResizeObserver` 修复空白图。
  - [x] **会话小任务**：boxplot 五数升序（前端 `prepareFixedChartOption` + 后端 `UserStatController.OnGetChartData`）；`tall` 绘图约 220px。

## T4 验证与文档

- [x] T4-1 `pnpm exec vitest run src/core/utils/hiddenEntryRoutes.spec.ts src/core/utils/accountLinks.spec.ts src/core/utils/objectMoreLinks.spec.ts src/core/utils/chartOptions.spec.ts`。验收重跑 19 通过。
- [x] T4-2 `pnpm exec vue-tsc -b` 无错误。
- [x] T4-3 迁移方案补白名单与效率页静态路由一句。
