# Retro

> 复盘 2026-10-01 | 状态 Done
> 验收决策：无 P0/P1 缺口；目标 3 布局以执行期用户决策为准（整行压缩固定部件）

## 概述

隐藏菜单用静态白名单直达通用列表/对象页，不碰 `Menu.Visible`。账号中心「关联」与对象「更多」给出入口；用户统计前两张图进洞察区作固定部件；效率页补静态路由。箱线乱序塌缩在验收前已修。

## 目标达成（对照 proposal §1）

| 目标 | 结论 | 证据 |
| --- | --- | --- |
| 1 白名单直达，不依赖 visible | 达成 | `hiddenEntryRoutes` + `router` 静态子路由；`[Menu(..., false)]` 未改 |
| 2 关联页签 + 对象更多，侧栏仍藏 | 达成 | `accountLinks` / `objectMoreLinks`；侧栏仍 `visible!==false` |
| 3 UserStat 前 2 张图 | 达成 | `useUserStatChart` → `mergeDeveloperCharts`；整行 w=12 |
| 4 Efficiency 无菜单仍可开 | 达成 | `oaLeafRoutes` 含 Efficiency |

## 测试与构建

- Vitest 19：hiddenEntry 2、accountLinks 3、objectMore 2、chartOptions 12。
- `vue-tsc -b` 无错误。
- `dotnet build NewLife.Cube -f net10.0` 0 错误。
- 浏览器冒烟在执行期做过；验收未再全点。

## 实际完成范围

计划内 T1–T4。会话小任务：洞察区固定整行图、spin/ResizeObserver、boxplot 前后端升序与 tall。顺带改了 `UserStatController` 演示数据排序（不改 Menu）。

## 过程中的坑

- 半宽方案被用户改成整行压缩固定部件；AC/proposal 须跟执行期决策同步，不能验收时当缺口。
- `a-spin` 默认 `inline-block` 在空图容器里宽为 0，ECharts 初始化失败；要块级布局 + 监听 `chartEl`。
- ECharts boxplot 五项必须升序，乱序会画成横线；后端演示指标与五数概括含义不同，前端仍要再排一次。
- 固定图 id 前缀 `dev-chart-` 运行时注入，持久化前必须滤掉，否则会写进个人 DashboardJson。
