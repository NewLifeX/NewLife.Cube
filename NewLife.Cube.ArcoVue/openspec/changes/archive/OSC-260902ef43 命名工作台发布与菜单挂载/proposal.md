# OSC-260902ef43 — 命名工作台发布与菜单挂载

## 1. 目标愿景

把 `/home` 自定义工作台从「每人一份个人墙」升级为「可另存为多份**命名工作台**、由系统角色发布并**挂载为系统菜单**、普通用户按菜单只读查看」的共享看板能力；无 slug 的个人墙语义（用户 > 主角色 > 系统）保持不变。

- 目标 1：系统角色在 `/home` 可将当前个人墙「另存为」命名工作台（标题 + slug），落 Parameter（新 Category `Workbench.Named`）并自动创建一条系统菜单（Url=`/Workbench/{slug}`）；普通用户点菜单进入只读命名工作台。
- 目标 2：`Workbench` 页标题栏「编辑」组合按钮：左侧进入/退出编辑态；右侧 `▾` 弹出菜单含 `发布 / 重命名 / 删除 / 分隔符 / 默认工作台 / 命名工作台1 / 命名工作台2 / …`（样式对齐 OSC-260830a1b2 `QueryComboButton`）。菜单项按「是否系统角色、当前是否命名工作台」真值表显隐/禁用。
- 目标 3：无 slug（`/home`）显示「默认工作台」，走既有 user > role > system 个人墙；有 slug（`/Workbench/{slug}`）走只读命名槽——普通用户只读，系统角色可再次编辑并「发布（更新）/ 重命名 / 删除」，删除当前工作台后回落默认工作台。
- 目标 4：后端命名槽读取按**菜单行权限** fail-closed（与 `/Cube/MenuTree` 同一 IsAccessible 语义），防止直接猜 slug 越权拉配置；每份配置仍受 `DashboardJson.TryNormalize(workbench)`（16 张 / 64KiB）约束。

## 2. 为何做

OSC-26082815a1 交付的 `/home` 工作台只有**一份个人 HomeJson + 角色模板 + 系统种子**的整份链，无法表达「把自定义好的布局沉淀为固定看板给团队看」。上一轮审计（2026-09-02）结论：方案 B 与现有架构同构——`WorkbenchRoleStore`（Parameter Category+Name+LongValue）可直接复制为命名槽；`WidgetSurfaceContext` 表面无关、整套 WidgetHost 可复用于 slug 页；菜单行来自 `ManageProvider.Menu`，`menuRoutes.buildLeafRoutes` 目前只把菜单映射为 DynamicPage/实体页，**运行期生成的看板没有菜单通道**，需为其加前缀分支。用户已确认：管理员发布共享菜单页、只读不可个人覆盖、Parameter 新 Category 免建表。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **命名槽 = Parameter**：`UserID=0`、`Category=Workbench.Named`、`Name={slug}`；`Value` 存标题（≤40）、`LongValue` 存归一化后配置 JSON（与 `WorkbenchRoleStore` 同构，复用双字段，**不改 UserProfile / 不建表**）。 |
| 2 | **发布者 = 系统角色**（`WorkbenchResolver.IsSystem`）；普通用户仅按菜单授权只读 `GET`。不做「个人在共享页上再叠个人覆盖」。 |
| 3 | **slug**：小写 `^[a-z][a-z0-9-]{0,31}$`、全局唯一；由发布者在「另存为」对话框填写（预填 `wb-<4位hex>`），标题仅用于菜单展示。 |
| 4 | **挂菜单**：发布时在菜单根下查找/创建 `Name='Workbench'`（DisplayName=「系统驾驶舱」）父分组，其下挂子菜单 `Name={slug}`、`DisplayName={标题}`、`Url=/Workbench/{slug}`、Icon=`fa-th-large`、Visible=true。菜单授权沿用既有角色 `Resources` 模型（未声明权限默认全员可见；限角色由授权树配置，本号不重做授权树 UI）。**父分组置顶**：XCode `EntityTree` 默认 `BigSort=true`，菜单子节点按 `Sort` **降序**；每次发布/更新时把父分组 `Sort` 提升为当前根级顶级菜单最大 `Sort+1`（封顶 `Int32.MaxValue`）→ `系统驾驶舱` 组（Name=Workbench）与其全部命名工作台在前端菜单**永远显示在第一组**。 |
| 5 | **无 slug = 默认工作台**（`/home`，user>role>system 不变，个人可编辑）；**有 slug = 命名工作台**（`/Workbench/{slug}`，普通用户只读；系统角色经「编辑」可改）。 |
| 6 | **编辑组合按钮**：左键 = 现有 `toggleEdit`；右键 `▾` 弹出菜单项 = `发布 / 重命名 / 删除 / 分隔符 / 默认工作台 / 命名工作台1…N`。样式与交互对齐 `QueryComboButton`（`a-dropdown trigger=click` + `a-doption` + `a-divider` + 勾选当前项）。 |
| 7 | **删除** = 删除 Parameter 命名槽 + 删除对应菜单行；当前正在查看的命名工作台被删后跳回 `/home`。**重命名**仅改标题（`Value` + 菜单 DisplayName），不改 slug/Url。**发布（无 slug）** = 把当前个人墙「另存为」新命名槽并跳转；**发布（有 slug）** = 把当前命名槽编辑结果保存更新。 |
| 8 | 每份命名配置独立受 `DashboardJson.TryNormalize(SurfaceWorkbench)`：16 张 / 64KiB / `w∈{2,3,4,6,8,12}` / 禁 legacyChart；未知 kind 占位允许。 |
| 9 | 只读鉴权：`GET /Cube/Workbench/Named/{slug}` 校验该 slug 对应菜单行对当前用户 **IsAccessible**（`Visible && (allowedIds.Contains || !permissionedIds.Contains)`，与 `/Cube/MenuTree` 一致）；菜单行不存在 → 404。 |
| 10 | `GET /Cube/Workbench/Named`（列表）仅系统角色；普通用户前端不渲染 `▾` 工作台切换列表。 |
| 11 | **默认工作台（无 slug）禁止重命名/删除**：`重命名…/删除…` 仅对有 slug 的命名工作台且为系统角色时可用；默认工作台 `▾` 中该两项**恒禁用**（后端也无默认工作台重命名/删除端点，语义上个人墙不属于命名槽）。 |

## 4. 做什么

1. 后端新建 `WorkbenchNamedStore`（Parameter 读写 + 菜单行 Mount/Unmount + `EnsureFirstGroup` 父分组 Sort 顶置 + `IsAccessible(user, slug)`），`DashboardJson.TryNormalize` 复用。
2. `WorkbenchController` 增：`GET Named`（系统角色列表）、`GET Named/{slug}`（菜单授权只读）、`PUT Named/{slug}`（upsert：建/改 + 挂/改菜单，仅系统角色）、`DELETE Named/{slug}`（下架：删槽 + 删菜单，仅系统角色）。
3. `@newlifex/api-core`：`createWorkbenchApi` 增 `namedList / namedGet / namedPut / namedDelete` 与 `NamedWorkbenchItem` 类型。
4. ArcoVue：`menuRoutes.buildLeafRoutes` 为 `/Workbench/{slug}` 前缀菜单分流到工作台组件（新增薄壳 `WorkbenchPage.vue` 接收 `slug` prop）；`useWorkbench` 支持 `slug`（只读命名槽 / 系统角色可编辑），`Workbench.vue` 标题栏改造为「编辑」组合按钮 + 发布/重命名对话框。
5. 测试：后端参照 `Osc26082815a1WorkbenchTests` 补命名槽/菜单/鉴权用例；前端补 `useWorkbench` 组合逻辑 Vitest。
6. 文档同步：`ArcoVue企业中后台迁移方案.md` §8.5 增补命名工作台小节；功能清单标注（如适用）。

## 5. 不做什么

- 不做「共享命名工作台上再叠个人覆盖」（保持只读发布语义）。
- 不做租户层工作台 / 整页画布 / 第三方 Widget 市场 / 用户脚本公式。
- 不新建数据库表；不改 `UserProfile`（`HomeJson` 语义不变）；不新建 `RoleWorkspace`。
- 不改 `/home` 个人墙的 user>role>system 解析链；`WorkbenchRole` 角色模板管理页不动。
- 不重做菜单授权树 UI / 角色资源勾选（复用 OSC-260824fc7c）；发布不提供「选择哪些角色可见」对话框（由管理员在授权树配置）。
- 不为普通用户提供命名工作台写入口；列表 API 不对普通用户开放。
- 不给默认工作台（无 slug）加重命名/删除能力（前端恒禁用、后端无端点）。
- 不做命名工作台之间的字段级合并 / 部件复用复制（另存为 = 整份复制当前墙）。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-26082815a1 | 个人墙 / `/Cube/Workbench` / `WorkbenchResolver` / 13+Inbox 部件——本号在其上叠加命名槽，不重写 Widget 栈 |
| OSC-2608280e9e | `DashboardJson.TryNormalize(surface)` 校验、WidgetHost 表面无关注入——直接复用 |
| OSC-260830a1b2 | `QueryComboButton` 为 `▾` 弹出菜单样式参考；查询/预定义收口与本号无关 |
| OSC-260824fc7c | 角色授权树（菜单权限声明）为命名工作台可见性的事实源 |
| 迁移方案 §8.5.2 / §5.1 | 首页工作台「用户 > 主角色 > 系统」分层；本号只加「命名槽 + 菜单」发布层，不混入该链 |
