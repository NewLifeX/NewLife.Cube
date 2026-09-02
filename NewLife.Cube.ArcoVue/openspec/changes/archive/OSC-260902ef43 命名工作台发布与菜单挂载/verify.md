# OSC-260902ef43 Verify — 命名工作台发布与菜单挂载

> 判定前提：`state=Validating` 验收核对；纯前端条目以 `web/` 代码为准，纯后端以 `NewLife.Cube/` 代码为准；需浏览器/真实菜单树的最终视觉行为统一归 AC6.3 宿主冒烟（用户已决策仅记录）。

## AC-1 命名槽存储与菜单挂载（后端）

- [x] AC1.1 Parameter 行 `UserID=0, Category=Workbench.Named, Name={slug}`：`Value`=标题、`LongValue`=归一化配置 JSON；`Get/GetList/Save/Delete` 往返正确，非法 slug 拒绝（单测 StoreRoundTrip/StoreRejectsBadSlug）。
- [x] AC1.2 首次发布自动创建菜单父分组 `Workbench`（DisplayName「系统驾驶舱」=ParentTitle 常量断言）并在其下挂子菜单；重复 `PUT` 同名 slug 更新 DisplayName、不产生重复菜单行；`DELETE` 后菜单行消失（单测 MountMenuCreatesAndPinsGroup/UnmountRemovesRowAndEmptyGroup）。
- [x] AC1.2b **置顶**：发布/更新后 `Workbench` 父分组 `Sort` = 当前根级顶级菜单最大 `Sort`**+1**（BigSort 降序；测试断言 `Sort==Max` 且排序后第一）→ 其在 `/Cube/MenuTree` 返回顺序位于第一组（最终视觉 AC6.3）。
- [x] AC1.3 `IsAccessible`（沿父链 Visible/声明逐级校验，主角色+附加角色并集）：未声明权限默认可见；任一角色声明后仅授权角色可见（单测 IsAccessibleRules/IsAccessible_RespectsVisibilityChain）。

## AC-2 控制器端点（后端；代码核验，action 层自动化用例列入 T7 遗留）

- [x] AC2.1 `GET /Cube/Workbench/Named` 仅系统角色（非系统 403）；返回 `[{slug,title}]`（GetVisibleList：仅菜单行存在且父链可见）。
- [x] AC2.2 `GET /Cube/Workbench/Named/{slug}`：非法 slug 400、不存在 404、无权 403、合法返回 `{slug,title,config}`；非法 JSON 当未配置（`config:null`，不 500）。
- [x] AC2.3 `PUT /Cube/Workbench/Named/{slug}` 仅系统角色：空标题/非法 slug/空 homeJson/超 16 张/64KiB/legacyChart → 400；成功 upsert 槽并同步菜单；**另存为（create=true）撞已存在 slug → 409**（G1 补齐）。
- [x] AC2.4 `DELETE /Cube/Workbench/Named/{slug}` 仅系统角色：删槽 + 删菜单，返回 `{deleted:true}`。
- [x] AC2.5 回归：个人 `GET/PUT /Cube/Workbench` 与 `Workbench.Role` 角色模板行为不变（15a1 回归 12/12）。

## AC-3 前端路由与只读渲染

- [x] AC3.1 菜单 Url `/Workbench/{slug}` 分流到 `WorkbenchPage`（props.slug，含 1 位 slug），不吃 DynamicPage/GetPage 探测；`/Workbench` 单段与普通实体菜单不误分流（menuRoutes.spec 6 用例）。
- [x] AC3.2 `/home`（无 slug）显示「默认工作台」语义，user>role>system 加载/保存/恢复默认不变（`index.vue` 未改 + 15a1 回归）。
- [x] AC3.3 `/Workbench/{slug}`：普通用户只读（编辑按钮渲染但禁用、无 `▾`、无空态添加入口）；系统角色可进入编辑态保存（useWorkbench canEditToggle/代码核验；视觉 AC6.3）。
- [x] AC3.4 `/Cube/MenuTree` 中 `系统驾驶舱` 顶级组恒第一组（后端置顶单测覆盖排序；真实菜单渲染 AC6.3）；`/home` 不产生菜单行。

## AC-4 编辑组合按钮

- [x] AC4.1 标题栏为「编辑（左）+ `▾`（右）」组合：左键进出编辑态；右键弹出菜单含 `发布 / 重命名 / 删除 / 分隔符 / 默认工作台 / 命名工作台1…N`（Workbench.vue 结构核对）。
- [x] AC4.2 菜单项真值：默认工作台（无 slug）→ 发布可用、**重命名/删除恒禁用**；命名工作台 → 发布/重命名/删除（系统角色）；非系统角色不渲染 `▾`；当前项打勾（disabled 绑定 + showNamedMenu=isSystem 代码核验；视觉 AC6.3）。
- [x] AC4.3 发布（无 slug）弹窗含 标题(必填≤40) + slug(必填、预填 `wb-<4hex>`、正则校验 + **已占用预检红字**）；成功后 `refreshMenus`+跳 `/Workbench/{slug}`（代码+单测；菜单出现新条目视觉 AC6.3）。
- [x] AC4.4 切换菜单项跳转：默认工作台 → `/home`；命名项 → `/Workbench/{slug}`（onNamedSelect/workbenchRoutePath 代码核验）。

## AC-5 重命名 / 删除 / 错误态

- [x] AC5.1 重命名只改标题（Parameter Value + 菜单 DisplayName），Url/slug/部件配置不变；重命名先拉服务端最新防陈旧覆盖（confirmRename）。
- [x] AC5.2 删除当前命名工作台经确认后：槽与菜单删除、`refreshMenus` 后跳回 `/home` 默认工作台。
- [x] AC5.3 边界：`namedGet` 404/403 黄条空墙不白屏；空标题/非法 slug 客户端拦截不发请求；**另存为撞已存在 slug → 前端预检红字 + 后端 409 兜底（G1 补齐）**。
- [x] AC5.4 命名工作台配置超 16 张 / 64KiB 在保存侧被拒（400）+ 前端提示（DashboardJson 既有用例）。

## AC-6 测试与构建门禁

- [x] AC6.1 后端 `dotnet build NewLife.Cube -f net10.0` 0 错；`dotnet test --filter "Osc260902ef43|Osc26082815a1"` → **20/20 过**（ef43 8 + 15a1 回归 12，连跑两次稳定）。
- [x] AC6.2 前端 `vue-tsc -b` 0 错；Vitest：web（menuRoutes 6 + workbenchNamed 4 + iconRegistry 17）**27/27**、api-core `api.spec` **37/37**。
  - ⚠️ 覆盖说明：design §7 的 `useWorkbench.spec` 挂载式用例未采用（仓库无 store/router composable 挂载测试先例）；slug/动作判定以 `workbenchNamed.ts` 纯函数 + `menuRoutes`/`iconRegistry` 分流与图标单测覆盖，跳转/保存集成归 AC6.3 宿主冒烟。
- [ ] AC6.3 宿主手工冒烟 §8 五场景（发布→普通用户只读→系统角色更新→重命名/删除→默认墙回归 + `/Cube/MenuTree`「系统驾驶舱」第一组/图标/chip 对齐）——需起 CubeDemoNC(WebAPI)+web dev；**验收决策：仅记录不补齐**（写入后续）。

## 验收记录（openspec-verify 2026-09-02）

- 三步检查：实现审计（目标 4 条全达成，无 P0）、代码审查（0 🔴）、doc-sync（无 P0）——均先于缺口补齐执行；本段记录补齐轮后复核。
- 补齐轮（用户决策“补齐易修项后放行”）：**G1** 另存为 create:true → 后端 409 + 前端 namedList 预检红字（防静默覆盖既有看板）；**G3** `GetVisibleList` 沿父链过滤（IsChainVisible）+ 可见性链测试断言；**G4** `iconRegistry.spec` 增加 `workbench` 值有效性；**G2** 文档数字/组名收尾（见下）。
- 缺口决策（用户拍板）：G1=阻止覆盖（409+预检）；G5（AC6.3 宿主冒烟）与 T7（controller action 自动化/禁 DELETE 兜底/并发幂等/useWorkbench 拆分）=**仅记录不补齐**，作为后续。
- 测试数据修正：DASH-3 XUnit=8、verify 数字 20/20/27/27/37/37；迁移方案版本行顶级组名改「系统驾驶舱」。

## 明确暂缓区（不得误删/误改）

- `WorkbenchRoleStore`、`WorkbenchResolver`、`WorkbenchSeeds`、`/Cube/Workbench` 个人端点、`/settings/workbench-role`、`WidgetSurfaceContext/WidgetHost/Grid/ConfigDrawer`、各 kind 渲染器、`views/home/index.vue`、角色授权树 UI——本号保持不动。
- `UserProfile` 不加列；不新建数据库表；`Workbench.Role` Category 不并入 `Workbench.Named`。
