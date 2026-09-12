# OSC-260902ef43 Tasks — 命名工作台发布与菜单挂载

> 阶段顺序依赖：P1 后端 → P2 api-core → P3 路由/壳页 → P4 工作台组合按钮 → P5 测试/构建 → P6 文档。前端 P4 不得在 P2 未合入前接线运行；可先写纯函数/类型与 Vitest。

## P1 后端命名槽与控制器（NewLife.Cube）

- [x] P1-1 新建 `NewLife.Cube/Widgets/WorkbenchNamedStore.cs`：常量 `Category/ParentName/ParentTitle`；`GetList()/Get()/Save()/Delete()/FindItem()/Exists()/IsValidSlug()/IsValidTitle()`（Parameter 双字段：Value=标题、LongValue=配置）；slug 白名单 `^[a-z][a-z0-9-]{0,31}$`（逐字符校验，不引 Regex）。
- [x] P1-2 同文件 `MountMenu(slug,title)/UnmountMenu(slug)` + `EnsureFirstGroup()`：经 `IMenu` 接口找/建 `Workbench` 父分组并**顶置父分组 Sort = 根级顶级菜单最大 Sort+1**（封顶 `Int32.MaxValue`；XCode `BigSort=true`、`Root.Childs` 按 Sort 降序 → 整组永远第一组），再挂/改子菜单（Name=slug、DisplayName=title、Url=`/Workbench/{slug}`、Icon=`fa-th-large`、Visible 由 `Add` 内建）；卸载按 Url 删菜单行、父分组无子项时一并删。查找/计数经 `Menu.Meta.Cache`（规避实例 `Childs` 缓存残留），`IMenu.Add` 自动 Save（XCode XML 注释核对）。
- [x] P1-3 同文件 `IsAccessible(IUser user, String slug)`：按 Url 匹配菜单行；权限取 **主角色 RoleID + 附加 Roles** 并集（`user.Roles` 不含主角色），复制 `/Cube/MenuTree` 的 `allowedIds.Contains || !permissionedIds.Contains` 语义；菜单行不存在/不可见 false。
- [x] P1-4 `Controllers/WorkbenchController.cs` 增 `NamedList`（GET Named，仅系统角色）、`NamedGet`（GET Named/{slug}：非法 slug 400、不存在 404、无权 403、成功 `{slug,title,config}`）、`NamedPut`（PUT/POST Named/{slug}，仅系统角色，upsert：TryNormalize + Save + MountMenu，400 覆盖空标题/非法 slug/空 homeJson/超限）、`NamedDelete`（DELETE Named/{slug}，仅系统角色，UnmountMenu+Delete）；新增 `WorkbenchNamedPutRequest` DTO；修复 `Decode` 上方 doc 注释结构。
- [x] P1-5 补测 `NewLife.Cube.Tests/Osc260902ef43NamedWorkbenchTests.cs`（7 用例：slug/标题校验、Parameter 往返+upsert+Delete、非法 slug 抛参、挂菜单建父分组并顶置第一+重复 PUT 单行、卸载删行/删空组、IsAccessible 存在性/默认可见/声明后未授权不可见/主角色授权可见）。集合独立 SQLite + `DisableParallelization`。

## P2 api-core（packages/api-core）

- [x] P2-1 `widget.ts` 加 `NamedWorkbenchItem { slug; title }`、`NamedWorkbenchResult { slug; title?; config }`；`index.ts` 导出。
- [x] P2-2 `api.ts` `createWorkbenchApi` 追加 `namedList/namedGet/namedPut/namedDelete`（namedPut/namedDelete 走 `requestWithPostFallback`）；`cube.ts` 无需改。
- [x] P2-3 `api.spec.ts` 补 1 条用例 4 个 URL/method/body 断言（37 全过）。

## P3 路由分流与壳页（web/src）

- [x] P3-1 新建 `views/home/WorkbenchPage.vue` 薄壳（`<Workbench :slug="slug" />`，`defineProps<{slug?: string}>`）。
- [x] P3-2 `core/utils/menuRoutes.ts` `buildLeafRoutes` 增 `/Workbench/{slug}`（两段、首段不区分大小写、slug 白名单 `^[a-z][a-z0-9-]+$/i`）前缀分支 → WorkbenchPage loader + `props.slug`（转小写）；`Workbench` 单段/非法段/普通实体菜单不分流。
- [x] P3-3 `menuRoutes.spec.ts` 补：`/Workbench/ops` 分流（slug prop）、`/Workbench` 单段与 `/Admin/User` 不误分流（5 用例全过）。
- [x] P3-4 `views/home/Workbench.vue` 声明可选 `slug` prop 并透传 `useWorkbench(slug)`；`index.vue` 不改。

## P4 工作台组合按钮与动作（web/src）

- [x] P4-1 `useWorkbench.ts`：签名 `useWorkbench(slug='')`；`isNamed/currentSlug/isSystem/namedList/showNamedMenu/canEditToggle/canRename/canDelete/canRestore/namedTitle/currentLabel` 状态与真值（默认工作台恒禁重命名/删除）；加载分支（slug 空走现状 `GET /Cube/Workbench`；有走 `namedGet`，404/403 黄条）；`saveDashboard` 命名槽分支（`namedPut`，仅系统角色可编辑）。
- [x] P4-2 `useWorkbench.ts`：`publish/rename/delete/onNamedSelect` 动作 + 发布/重命名对话框状态与校验、路由跳转（`/home` / `/Workbench/{slug}`）；默认 slug 预填 `wb-<4位hex>`；客户端校验（后端 400 兜底）；删除回 `/home`。
- [x] P4-3 `Workbench.vue`：标题栏「编辑（左）+ ▾（右）」组合按钮（`a-dropdown trigger="click"` + `a-doption` + `a-divider` + 分组标题 + 当前项勾选，样式对齐 `QueryComboButton`）；`▾` = 发布/重命名/删除/分隔符/切换工作台（默认工作台 + 命名列表）；恢复默认仅默认工作台渲染（`!isNamed` 时不渲染）；命名空槽占位文案；发布/重命名/删除三个 `a-modal`；命名 chip 标题展示。
- [x] P4-4 可测逻辑抽纯函数 `core/utils/workbenchNamed.ts`（`isValidNamedSlug/randomSlugSuffix/defaultPublishSlug/parseNamedMenuValue/workbenchRoutePath`）+ `workbenchNamed.spec.ts`（4 用例 4 测试全过）。
  - ⚠️ 偏差（记录）：design §7「useWorkbench.spec.ts slug 分支挂载用例」未采用——仓库无 store/router 依赖 composable 的挂载测试先例（既有 spec 均为纯函数）；改为纯函数 + menuRoutes 分流单测覆盖可判定逻辑，`useWorkbench` 跳转/保存集成由宿主手工冒烟（P5-3）覆盖。

## P5 测试与构建

- [x] P5-1 后端：`dotnet build NewLife.Cube -f net10.0` 0 错；`dotnet test --filter "Osc260902ef43|Osc26082815a1"` → 19/19 过（ef43 7 + 15a1 回归 12；T4-4 补可见性链后 ef43=8，验收复核 20/20 见 T12）。
- [x] P5-2 前端：`vue-tsc -b` 0 错；Vitest：web `menuRoutes.spec + workbenchNamed.spec` 9/9 过、api-core `api.spec` 37/37 过（先 `tsup` 重建 api-core dist 供 web 类型）。
- [ ] P5-3 宿主手工冒烟（发布→普通用户只读→更新→重命名/删除→默认墙回归；`/Cube/MenuTree` 第一组）——需起 CubeDemoNC(WebAPI)+web dev+登录，留待**验收阶段**执行（收尾记录）。

## P6 文档同步

- [x] P6-1 `ArcoVue企业中后台迁移方案.md`：版本行加 2026-09-02 备注；§8.5.2 后增补「8.5.2a 命名工作台：另存为、切换与菜单挂载（OSC-260902ef43 已实现）」。
- [x] P6-2 功能清单 `Doc/功能清单.md`：DASH-2 后新增 DASH-3「命名工作台发布与菜单挂载」行并回写 OSC 号。
- [x] P6-3 `openspec/changes/README.md` 无需改（归档由 retro 处理）。

## 会话小任务补录（openspec-apply 收尾）

- [x] T1 修正 `WorkbenchController` 既有 `Decode` 上方错位/缺 `<summary>` 的 doc 注释结构（编译告警 CS1570 触发，顺手修复，不改语义）。
- [x] T2 执行中 `IMenu.Childs` 类型歧义（接口声明 `IList<IMenu>`）→ store 全程经 `IMenu` 接口 + `Menu.Meta.Cache`，规避实例缓存残留（`group.Childs` 在删除后 stale 导致单测暴露）。
- [x] T3 前端 `Workbench.vue` 删除模态 `ok-button-props` 字符串误写（TS2559）修复为绑定对象。
- [x] T5 「工作台」顶级组菜单图标用 `workbench`：`iconRegistry.MENU_NAME_ICONS` 增 `工作台→workbench`（显示名精确命中，先于 fa/关键词/默认），命名子菜单不误命；补 spec 断言（工作台→workbench、运营看板→application）。
- [x] T6 命名工作台页标题 chip 与日期水平对齐：chip 图标 `dashboard→workbench`；`.wb-hello-meta` 与 chip/date 同 flex 行 `align-items:center`（去 date `margin-top`、统一 `line-height`、chip svg `display:block`）；清理重复 `.wb-hello-date` 规则。
- [x] T8 顶级组名「工作台」→「系统看板」（仅左侧导航组显示名）：`WorkbenchNamedStore.ParentTitle="系统看板"`（Name=Workbench 标识不变）+ `EnsureFirstGroup` 对已存在分组幂等同步 DisplayName（旧库升级即生效）；iconRegistry `MENU_NAME_ICONS` 双键（系统看板/工作台→workbench）兼容旧数据；补父分组 DisplayName 断言（用常量）+ iconRegistry.spec（系统看板→workbench）；文档同步 proposal/design/verify/ui/迁移方案 §8.5.2a/功能清单 DASH-3。**概念词「命名工作台/默认工作台/首页工作台」不改**。
- [ ] T7 遗留后续（不阻塞验收）：①控制器层 401/403/404/400 自动化用例；②禁 DELETE 环境 POST `?delete=1` 兜底；③`MountMenu` 并发幂等；④`IsAccessible` 附加角色用例 + `permissionedIds` 缓存；⑤`useWorkbench` 拆分 `useNamedWorkbenchActions`。
- [x] T9 验收补齐 G1（另存为防覆盖）：后端 `WorkbenchNamedPutRequest.Create` + `NamedPut` create=true 撞已存在 slug → 409；前端另存为 `namedPut(...,create:true)` + 弹窗对 `namedList` 已占用 slug 红字预检（不发请求）。api-core `namedPut` body 增 `create?`。
- [x] T10 验收补齐 G3：`GetVisibleList` 沿父链过滤可见（新增 `IsChainVisible`，隐藏的菜单行/父分组不再出现在列表，消除“点开恒 403”死胡同）；可见性链测试补 Save+Mount 与列表断言（修复测试库跨 run 残留 → class fixture 每次运行重建库）。
- [x] T11 验收补齐 G4：`iconRegistry.spec` 产品命名专用区补 `all.add('workbench')`，防未来移除组件不回警。
- [x] T12 验收文档收尾 G2：迁移方案版本行顶级组名「工作台」→「系统看板」；功能清单 DASH-3 XUnit 7→8；verify AC1~AC6 数字与勾选更新（20/20、27/27、37/37）+ design/ui G1 语义（create 409/预检）；P5-1 历史数字补注。
- 说明：本 OSC 无 plan 外的功能新增；上述均为实现路径上的必要修正/对齐/体验微调，已在 tasks/status/verify 记录（会话小任务已补录）。

## 收尾修复轮（代码审查 🔴/🟡 + 实现审计缺口，2026-09-02 第二轮）

- [x] T4-1 🔴 slug 白名单三处重复漂移：`menuRoutes` 内联正则（`+` 漏 1 位 slug）统一为 import `isValidNamedSlug`；补 1 位 slug 分流 spec（menuRoutes.spec 第 6 用例）。
- [x] T4-2 🔴 发布闭环：`confirmPublish/confirmDelete` 成功后 `refreshMenus()`（`resetMenuRoutesFlag()` + `userStore.fetchMenus()`，租户切换同款）→ 新 `/Workbench/{slug}` 路由可命中、侧栏菜单即时出现/移除。
- [x] T4-3 🟡 重命名陈旧覆盖：`confirmRename` 先 `namedGet` 拉服务端最新配置再保存（仅改标题不回传陈旧墙）；成功后 `fetchMenus()` 刷新菜单显示名。
- [x] T4-4 🟡 读授权只验叶子：`IsAccessible` 沿父链（含父分组）逐级校验 `Visible` + 声明/授权；补可见性链单测（子菜单隐藏/父分组隐藏 → 不可达）。
- [x] T4-5 🟡 悬空槽列表：`GetVisibleList()`（仅含已挂菜单行的槽）供 `NamedList`；`GetList()` 保留全量槽（存储事实，Store 往返测试语义不变）。
- [x] T4-6 🟡 `__publish` 补 `:disabled="!canPublish"` 绑定（useWorkbench 暴露 canPublish）；ui 文档改为命名页编辑按钮「渲染但禁用」。
- [x] T4-7 🟢 多行 `<summary>` 单行化（类头/EnsureFirstGroup），对齐注释规范 4.5。
- [ ] T4-8 后续项（记录不阻塞，见 status）：① 禁 DELETE 环境 POST `?delete=1` 兜底；② `MountMenu`/`EnsureFirstGroup` 并发幂等锁；③ 控制器层 401/403/404/400 action 自动化用例（现依赖代码审查 + 宿主冒烟）；④ `IsAccessible` 附加角色（`user.Roles` 非空）分支用例与全量角色 Resources 性能缓存；⑤ `Workbench.vue`/`useWorkbench.ts` 体量再拆分。

