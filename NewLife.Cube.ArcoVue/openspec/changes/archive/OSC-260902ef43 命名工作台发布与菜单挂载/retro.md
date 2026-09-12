# OSC-260902ef43 Retro — 命名工作台发布与菜单挂载

> 复盘时间：2026-09-02T23:50:00+08:00  
> 触发：验收并复盘 ef43 变更（用户授权补齐易修项后放行复盘）

## 摘要

| 项 | 结论 |
| --- | --- |
| 目标愿景 | 1–4 全部达成（实现审计/代码审查/doc-sync 三步复核） |
| 三步编排 | implementation-audit → code-review → doc-sync 完成（apply 收尾两轮 + verify 复核轮） |
| 自动化门禁 | 后端 20/20（ef43 8 + 15a1 12，连跑稳定）；前端 vue-tsc 0 错 + web 27/27；api-core 37/37 |
| 缺口处置 | G1 另存为防覆盖（409+预检）已补齐；AC6.3 宿主冒烟与 T7 遗留 = 仅记录不补齐（用户决策） |
| 归档 | 状态 → Done；目录移至 `changes/archive/` |

## 实际完成范围

- 命名工作台存储：`Parameter(UserID=0, Category=Workbench.Named, Name={slug})`，Value=标题(≤40)、LongValue=归一化配置；复用双字段免建表（与 `WorkbenchRoleStore` 同构）。
- `WorkbenchController`：`NamedList/Get/Put/Delete`（列表与写删仅系统角色；读按菜单行授权 fail-closed 400/404/403；另存为 `create=true` 撞 slug → 409）。
- 菜单挂载与置顶：`WorkbenchNamedStore` Mount/Unmount/`EnsureFirstGroup`（父分组 `Workbench`、显示名「系统看板」，每次发布把 Sort 顶置为根级最大+1 → 永远第一组，幂等同步旧分组名）；`GetVisibleList` 过滤悬空与隐藏项（沿父链）；`IsAccessible` 沿父链 Visible/声明校验（主角色+附加角色并集）。
- 前端：`/Workbench/{slug}` 分流 WorkbenchPage；`useWorkbench(slug)` 加载/保存/发布/重命名/删除/切换；`Workbench` 标题栏「编辑(左)+▾(右)」组合按钮（样式对齐查询簇）；默认工作台禁重命名/删除；chip（workbench 图标）与日期水平对齐；发布/删除/重命名后 `refreshMenus` 刷新路由与菜单树。
- api-core：`namedList/namedGet/namedPut/namedDelete` + 类型；menuRoutes/iconRegistry 等纯函数化并有 spec 锁死。
- 体验微调：顶级组名「系统看板」（T8）、图标 workbench 双键映射、命名页 chip 对齐（T5/T6）。

## 做得好

- `IsAccessible` 与 `/Cube/MenuTree` 语义 parity 经 XCode 源码核实（BigSort 降序、父链不可达即子树不可达），且显式并入主角色 RoleID 比既有实现更稳。
- 复用 `DashboardJson.TryNormalize(surface=workbench)` 整套约束（16/64KiB/禁 legacyChart），未另起校验。
- 审计两轮暴露的真问题（1 位 slug 正则漂移、发布后动态路由不注册、隐藏菜单死胡同、另存为静默覆盖）均在小循环内闭环并补测试，未流入验收。
- 会话增量（T5–T12）均即时补录 tasks/verify/status，规避归档时的“隐形缺口”。

## 偏离与原因（记录）

| # | 偏离 | 原因 |
| --- | --- | --- |
| D1 | design §7 `useWorkbench.spec` 挂载式用例 → 改为 `workbenchNamed.ts` 纯函数 + menuRoutes/iconRegistry 分流单测 | 仓库无 store/router composable 挂载测试先例；纯函数化更贴既有 spec 风格（tasks P4-4 / verify 记录） |
| D2 | 宿主手工冒烟（P5-3/AC6.3）未执行 | 需起 CubeDemoNC+web dev+登录；验收决策=仅记录不补齐，作为后续 |
| D3 | `NamedGet` 悬空槽（Parameter 在、菜单行无）返回 403 而非 404 | `Exists` 先于菜单授权判定；更保守安全，语义已在 design/AC 校准 |
| D4 | 控制器层 401/403/400 无自动化 | 缺 HTTP 宿主测试样板（同 OSC-0002 lessons）；留 T7 |

## 教训（已写入 harness/lessons.md）

- 菜单“第一组”= XCode `BigSort=true` 下 Sort 降序，置顶取根级最大+1（勿写死小数字）。
- XCode 实例 `Childs` 缓存删除后残留 → 判定一律走 `Menu.Meta.Cache`。
- 动态路由仅首轮注册 → 发布/删除后须 `resetMenuRoutesFlag()+fetchMenus()`（租户切换同款）。
- 单测 SQLite 跨 run 残留会污染可见性断言 → class fixture 每次运行重建库。
- 图标/白名单只保留一份纯函数源（`workbenchNamed`/`iconRegistry`），勿内联正则（曾漏 1 位 slug）。

## 遗留与后续

- AC6.3 宿主手工冒烟（发布→普通用户只读→更新→重命名/删除→菜单「系统看板」第一组/图标/chip 对齐）。
- T7：控制器 action 自动化、禁 DELETE 环境 `?delete=1` 兜底、`MountMenu` 并发幂等、`IsAccessible` 附加角色用例+缓存、`useWorkbench` 拆分。

