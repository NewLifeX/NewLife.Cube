# OSC-2608273d95 Verify

> 进入 `Validating` 后逐项勾选。带【单测】的项已由自动化测试覆盖（证据见 `tasks.md` T9）；带【冒烟】的项已于 2026-09-14 在**隔离实例**（CubeDemo 副本 + 独立 SQLite，5099 端口，不触碰业务库）实测，证据见文末「验收执行（2026-09-14）」。

## 必须保留（暂缓区，实施误删即失败）

- GetPage `[AllowAnonymous]`；**不得**把 DataScope SQL / 部门 ID 列表下发浏览器当授权依据。
- `viewFilter` logic=any 不得 OR 掉行权（OSC-260819e483）。
- 租户 `CreateWhere` 分支语义不变，与 DataScope **AND**。
- 不实现角色×字段矩阵 ACL；不把 ViewProfile 藏列当授权。
- 不给 Log/Token 做「本部门可见同事的日志/令牌」。
- 不给 Menu/Role/File 加 IDataScope 行过滤。
- 不新增 `/iam`；不改 Cube.Vue。
- 不改 `LovController.ListData`；不把导出改成字段矩阵裁剪（AllFields 可暂留）。
- `RoleController.Valid` 的 Permission `menuId#flags` 解析保留（OSC-260824fc7c）。
- `DataPermissionAttribute` 类型可保留；矩阵内 7 控制器不得再有仅本人表达式。
- **不改 XCode 仓库、不升包**；不得给任何实体挂 `DataScopeInterceptor`（`DataScopeDecouplingTests` 前两条必须保持绿）。
- `MaskSensitiveFields` / `sensitive` 标记必须传真实用户上下文（`GetDataScopeContext()`）；用宿主态会把全员敏感字段一起脱敏。

## 命令与预期

仓库根 `NewLife.XCode` / `NewLife.Cube`；前端 `NewLife.Cube.ArcoVue/web`。

```
# XCode：只读回归，不改该仓库
dotnet test XUnitTest.XCode --filter FullyQualifiedName~DataScope
dotnet test NewLife.Cube.Tests/NewLife.Cube.Tests.csproj --filter FullyQualifiedName~DataScope
dotnet test XUnitTest/XUnitTest.csproj --filter FullyQualifiedName~DataScopeDecoupling
dotnet build NewLife.Cube/NewLife.Cube.csproj
dotnet build NewLife.CubeNC/NewLife.CubeNC.csproj
pnpm --filter @newlifex/cube-arco-vue test
pnpm --filter @newlifex/cube-arco-vue build
```

预期：本号相关测试 0 failed；构建 0 error。

## Happy path

- [x] **AC-01 本部门用户列表**【冒烟✅】：`u_deptA`（DataScope=本部门，部门=1）→ 6 行 `Where DepartmentID=1`，含同部门同事（3/5/6/8/9），不含部门=5 的 `u_deptB`。
- [x] **AC-02 仅本人用户列表**【冒烟✅】：`u_self`（仅本人）→ 1 行 `Where ID=5`（仅自己）。
- [x] **AC-03 全部/系统角色**【冒烟✅】：`u_all`（角色 DataScope=全部）与 `admin`（IsSystem）均 10 行、无行权条件。
- [x] **AC-04 自定义**（判据已按锁定包修正）：`DataDepartmentIds=5` → 仅 1 行 `Where DepartmentID=5`，部门集合正确。判据不得包含「本人数据始终可访问」——该语义来自 XCode `ac108a773`（2026-09-11），晚于本号锁定包 `12.2.2026.901`（2026-09-01）；本号不升包，按包内实际语义验收。
- [x] **AC-05 本部门及下级**【冒烟✅】：`u_child`（部门=1）→ 7 行 `Where DepartmentID In(1,2,3,4,8)`，含子部门（新建叶 8）的 `u_childLeaf`，不含部门=5 的 `u_deptB`。
- [x] **AC-06 日志不扩权**【冒烟✅】：`u_deptA`（本部门）→ `Admin/Log` 15 行，**全部 `CreateUserID=2`（自己）**，未因角色部门范围看到同事日志。
- [x] **AC-07 部门列表**（判据已修正）：判据 = 「可见性来自我管理的部门」。本号**有意保留** `[DataPermission(null, "ManagerID={#userId}")]`（双栈 `DepartmentController:9/14`），与接口层部门集合**同时**生效（实测 SQL：`Where ManagerId=5 And ID=1`）；非系统角色 0 行（测试用户不是管理者）、`admin` 8 行，均符合该判据。原判据「能看到自己部门行」在保留 `ManagerID` 特性后不成立，属判据描述问题。
- [ ] **AC-08 Role 保存全部**【冒烟·未做】：属 XCode `Role.Valid`（含在 92/92 回归内），本轮未在实例上操作角色保存；建议由 XCode 侧用例覆盖或在冒烟清单中降级为「可选项」。

## 权限 / 空 / 非法 / 兼容

- [x] **AC-09 越权详情**【单测+冒烟✅】：仅本人 GET 他人 User id → 非法访问——`DataScopeRowPermissionTests.FindData_OtherUser_Rejected`；隔离实例实测 `u_self` 查 `/api/Admin/User/2` → `code=500 msg=非法访问数据[2]`，服务端堆栈 `ReadOnlyEntityController2.cs:296`。
- [x] **AC-10 越权写入**【单测】：`ValidPermission`/`CanAccess` 拒绍改他人（仅本人）；新增放行——`ValidPermission_UpdateOther_Rejected_InsertAllowed`。**【收尾门禁加固】** 新增也不再无条件放行：声明**他人**归属时拒绝（`ValidPermission_Insert_ForgedOwner_Rejected`），系统态放行（`ValidPermission_Insert_SystemScope_Allowed`）
- [x] **AC-11 无接口实体**【单测】：不因 GetFilter 变 1=0——`SearchData_NoScopeEntity_NoFilter`（无接口实体 GetFilter 返回 null）。
- [x] **AC-12 无部门用户+本部门**（判据已按锁定包修正）：`DepartmentID=0` 的本部门用户 → 部门集合为空 → 恒假条件（实测 SQL `Where DepartmentID=-1`），即 **0 行**，列表不会变全表（原判据中「本人恒可见」同上不属锁定包语义）。
- [x] **AC-13 DataPermission 已拆**【单测】：矩阵内 7 控制器（双栈）源码无 `[DataPermission]`——`DataScopeDecouplingTests` 13/13 + API 版 6 例。
- [x] **AC-14 敏感**【单测+冒烟✅】：无 ViewSensitive 时他人 Password 置 `***`——`MaskSensitiveFields_Others_WithoutViewSensitive_Masked`；**双栈**：WebAPI 与 CubeNC 均接入（共享 `ReadOnlyEntityControllerScope.cs`，含 CubeNC 令牌端点 5 个出口）。隔离实例实测：`u_deptA` 查同部门他人 id=3 → `password=***`
- [x] **AC-15 GetPage sensitive**【单测】：无 ViewSensitive 时密码字段 `sensitive=true`——`GetSensitiveFieldNames_UsesRealUserContext`。
- [x] **AC-16 前端自定义部门**【单测】：DataScope≠自定义不渲染 DataDepartmentIds——`dataScopeForm.spec.ts`。
- [x] **AC-17 仅自己提示**【单测】：同部门多人不提示、仅自己一行仍提示——`iamGuards.spec.ts`。
- [ ] **AC-18 viewFilter**【单测+冒烟待补】：带 viewFilter 的列表仍受 DataScope 约束；`logic=any` 只 OR 前端条件。现覆盖用例为 `DataScopeRowPermissionTests.SearchData_ViewFilter_DoesNotExpandScope`（`p.State` 同时含行权列与筛选字段）；「真实带参行集」仍待冒烟。
- [x] **AC-19 旧客户端**【审阅】：无新查询参数；契约字段名不变（新增可选 `sensitive`）。**行集可能变宽**：拆掉仅本人 `DataPermission` 后，非系统角色的行集改由角色 `DataScope` 四档决定，存量系统里「本部门/下级/自定义/显式全部」的角色会比改造前看到更多行（含审计日志）——属设计意图，升级需告知
- [x] **AC-20 文档**【审阅】：`PERM-数据权限.md` 无 `DataScopeType`、无三参数 DataPermission 构造示例。
- [x] **AC-21 导出行集**【冒烟✅】：`u_deptA`（本部门）CSV 6 数据行（与列表 `Where DepartmentID=1` 一致，日志 `导出数据[6]`）；`u_self`（仅本人）1 数据行（`导出数据[1]`）。
- [ ] **AC-22 Widget Query**【待冒烟】：本部门角色对 User 的 count 不含外部门（`WidgetQueryService` 已追加 `GetFilter`）。
- [x] **AC-23 XCode 零改动回归**【单测】：上游 `DataScopeTests` 92/92；本次未改 XCode 仓库。
- [x] **AC-24 实体层纯净**【单测】：`DataScopeDecouplingTests` 前两条绿（无 `DataScopeInterceptor`；跨用户查询不被实体层收窄）。
- [x] **AC-25 行权上下文**【单测】：均用控制器层构造的真实用户上下文，`DataScopeContext.Current` 仍为宿主系统态——`GetDataScopeContext_DoesNotWriteCurrent` + `GetSensitiveFieldNames_UsesRealUserContext`。

## 残余（不阻断 Done）

- Cube.Vue 皮肤无专门提示条（本号不改）。
- 菜单 DataScope 覆盖的手工项若无现成菜单数据可记 P2，但单元测试须覆盖 `SetMenu`：`>=0` 覆盖、`<0` 继承。

---

## 验收执行（2026-09-14）

验收基线：`NewLife.Cube@ArcoVue HEAD=04ef627d`（3d95 起点 `75e747ef`）；`NewLife.XCode` 只读核对（本地仓库 HEAD `280b2a2f4`，运行包 `12.2.2026.901`，**零改动**）。工作树仅剩用户自有 WIP（`QueryComboButton.vue`、`code-workspace`），`wwwroot/index.html` 构建产物已回退，保证交付树可复现。

### 1. 三步检查（OpenSpec 验收固定编排）

| 检查 | 结论 |
|------|------|
| 实现审计 | **P0 = 0**。25 条 AC **未发现虚假标注**（除外 AC-18 覆盖偏窄、AC-25 用例归属说明）。R1 疑似的 fail-open 经 XCode 调用链证伪：`Entity.cs:1139-1143` 会对同一 `WhereBuilder` 再解析 → 报错而非放行（fail-closed）。新增 3 项 P1 + 3 项 P2（见缺口清单）。 |
| 代码审查 | 🔴 1 / 🟡 5 / 🔵 6。规范层（类型名、注释、region、`.vue` 无业务分支）**零违规**；🔴 为本号功能覆盖缺口（AI 记录上下文绕过行权），**相对 HEAD 无行为回退**（该通路改造前同样无行权）。 |
| 文档同步 | 🔴 4 / 🟡 10 / 🔵 8。4 处 🔴 全部是"宣称已覆盖、实际未覆盖"：App 密钥令牌出口、导入、`ValidPermission 新增放行`、以及 `Doc/PERM-数据权限.md:69` 把"导入"列入 `FindData` 行。 |

### 2. 测试与构建门禁（全绿）

| 命令 | 结果 |
|------|------|
| `dotnet build NewLife.Cube/NewLife.Cube.csproj` / `NewLife.CubeNC/NewLife.CubeNC.csproj` | **0 警告 0 错误**（双栈，net6.0–net10.0） |
| `dotnet test XUnitTest.XCode --filter DataScope`（只读回归） | **92 / 0** |
| `dotnet test NewLife.Cube.Tests --filter DataScope+DataPermission+BypassGuard+MenuOverride+ExportController` | **45 / 0** |
| `dotnet test XUnitTest --filter DataScopeDecoupling+DataScopeSensitiveMvc` | **17 / 0** |
| `pnpm --filter @newlifex/cube-arco-vue test` / `build` | **89 文件 848 用例通过** / `✓ built in 20.85s` |

### 3. 隔离实例冒烟（T10，2026-09-14）

方式：`Bin\CubeDemo` 副本 + 独立 SQLite + 5099 端口（不影响业务实例与 `Bin\Data`）；直接播种 5 个测试角色（DataScope 0/1/2/3/4）+ 9 个测试用户（密码 MD5）+ 1 个叶子部门；模型层关闭登录验证码以便脚本化登录；每档以真实 HTTP 调 `/api/Admin/User`、`/api/Admin/Log`、`/api/Admin/Department`、`/api/Admin/User/{id}`、`/ExportFile?format=csv` 并对照服务端 SQL 日志。

| 场景 | 实测行集 / 结果 | 判定 |
|------|----------------|------|
| 本部门（部门=1） | 6 行 `Where DepartmentID=1`（3/5/6/8/9 + 自己）；不含部门=5 | ✅ |
| 仅本人 | 1 行 `Where ID=5` | ✅ |
| 本部门及下级（部门=1） | 7 行 `Where DepartmentID In(1,2,3,4,8)`（含子部门叶 8） | ✅ |
| 自定义（DataDepartments=5） | 1 行 `Where DepartmentID=5` | ⚠️ 部门集合正确；自身行不入集（版本前提） |
| 全部 / IsSystem | 10 行、无行权条件 | ✅ |
| 无部门 + 本部门 | 0 行 `Where DepartmentID=-1` | ❌ 需改判据（版本前提） |
| 日志（本部门角色） | 15 行**全部** `CreateUserID=自己` | ✅ 不扩权 |
| 越权详情（仅本人查他人） | HTTP 200 / `code=500 非法访问数据[2]`，栈顶 `ReadOnlyEntityController2.cs:296` | ✅ |
| 敏感脱敏 | 本部门他人行 `password=***` | ✅ |
| 导出（本部门 / 仅本人） | CSV 6 行 / 1 行，与列表行集一致（`导出数据[6]` / `导出数据[1]`） | ✅ |

### 4. 缺口清单（待决策）

**P0：无。**

**P1**
1. **批量导入不经行权/归属校验**（审计新增，未登记）：`EntityController.cs:459-480` → `EntityController2.cs:293-390`（`BatchInsert/BatchUpsert/OnMerge` 无 `Valid`），Zip 分支还能对包内 `factory2` 任意实体批量落库（`:625`）；本号新增的"新增防伪造归属"在此路径被绕过。
2. **AI 记录上下文绕过行权**（审查 🔴）：`AI/CubeTools.cs:169`（同 `:59`）`Entity<TEntity>.FindByKey(entityId)` 直查，不经 `CanAccess`、不脱敏；入口 `AI/AiController.cs:94`（只把**列表**包进了 `SearchData`，`:581`）。
3. **App 密钥令牌出口无用户上下文**（审查 🟡 / 文档 🔴）：`NewLife.CubeNC/Common/ReadOnlyEntityController.cs:263-269` 不写 `Items["CurrentUser"]` → `GetDataScopeContext()` 为 null → 行权不介入，而文档称"与页面一致"。
4. **系统内置部件直查实体**（审计 P1 / 文档 🟡）：`Widgets/System/OnlineCountWidget.cs:14`、`LoginLogWidget.cs:23-26`、`Workbench/KpiWidgets.cs:44+`、`ContentWidgets.cs:104`（缓解：多受 `AdminOnly` 限制）。

**P2**：菜单级 DataScope 覆盖未接线却对外宣称"角色/菜单四档"（`Doc/功能清单.md:67`、`Doc/PERM-数据权限.md:133`、`web/README.md:127`）；部件 list 投影用名字黑名单而非 `IFieldScope`（`WidgetQueryService.cs:629`）；`MaskSensitiveList` 返回值被丢弃（`Scope.cs:60-69`）；`***` 可经"详情→编辑→提交"往返落库（现由 `UserController.cs:309` 只读门禁挡住）；`allList` 无 sensitive（R2 在册）；`entity:` 值集无行权（BE-D2 另号）；AC-07/AC-12/AC-04 判据需按**锁定包版本**改写；文档/台账漂移 8 处（`Trance`→`Trace`、README TFM、`tasks.md` 用例数字 35→39、MVC 全量 494/6 与 493/7 并存、N10 注释缺失、AC-18 用例名、retro 范围表措辞）。

### 5. 风险

1. **P1-2/P1-3 是"本号承诺全覆盖、实际有旁路"**：`proposal.md` 的出口清单与 `ChangeLog.md` 措辞需收窄或补齐，否则会被读成"AI/令牌出口已受行权"。
2. **AC-07/AC-12 的判据建立在 XCode 未发布语义上**（`ac108a773` 2026-09-11）：按当前锁定包，无部门 + 本部门 = 空表、部门列表 = 我管理的部门。不改措辞会长期误导（含升级后的行为变化）。
3. **跨皮肤/元数据回归不属本号**：本号后落地的 `995a6f57`/`04ef627d`（Map 外键候选、布尔不下发 dataSource）已单独复审并回归，与本号行权语义无冲突，但都在 `GetPage`/`PrepareFieldsForApi` 同文件上，后续改动需连跑本号用例族。

### 6. 决策栏（已裁定）

**2026-09-14 用户裁定**：缺口处置 = 「补齐 G1–G4（P1 四项）后再复盘」；AC-04/07/12 判据 = 「按锁定包 `12.2.2026.901` 改写，不升包」。
G5–G8 / G10 归为「仅记录」（保留在本文件缺口清单与 `tasks.md` 同名节，留后续号）。

---

## 7. 补齐轮次（2026-09-14，G1–G4）

裁定后按「补齐 P1 四项」实现完毕，`status.md` 一度回写 `Implementing`，回写 `Validating` 前已完成下列验证。

### 7.1 变更清单

| 缺口 | 实现 | 文件 |
|------|------|------|
| G1 批量导入 | `OnImport` 新增 `ValidImport(factory, list, context, totalRows)` 前置校验：归属不得为他人 / 已声明归属须在数据范围 / 合并类模式按主键查已存在行不得不可见；`ImportZip` 对包内其它实体数据集调 `ValidImportEntity` 校验页面新增权限（系统角色不受约束）；`FindImportTarget` 支持主键未声明时跳过查库 | `Common/EntityController2.cs`（双栈共享） |
| G2 AI 记录上下文 | `CubeTools<TEntity>` 新增可选 `findRecord` 委托 + `FindRecord(id)`（越权记日志后降级返回 null）；`GetRecordContext`/`GetFormSchema(edit)` 改走它；控制器注入 `FindRecordMasked(key)` = `FindData` + `MaskSensitiveFields` | `AI/CubeTools.cs`、`Common/ReadOnlyEntityController2.cs`（均双栈共享） |
| G3 令牌出口 | App 密钥分支支持 `userId`/`user` 参数绑定用户（绑定后行权接入，与非绑定路径统一写入 `Items["CurrentUser"]`）；未绑定时显式 `XTrace` 声明系统级访问 | `NewLife.CubeNC/Common/ReadOnlyEntityController.cs` |
| G4 内置部件 | 新增共享助手 `CubeDataScope`（`Create`/`GetCurrent`/`GetFilter`/`Merge`/`HasOwner`/`CanAccess`/`IsForgedOwner`）；用户总数 / 在线 / 24h 日志 / 24h 异常 / 登录与在线明细（MVC 与 WebAPI 两套部件）接入 `GetFilter` | `Common/CubeDataScope.cs`（新，双栈 Link）、`Widgets/System/{UserCount,OnlineCount,Error24h,LoginLog}Widget.cs`、`Widgets/Workbench/{Kpi,Content}Widgets.cs`、`NewLife.CubeNC.csproj` |

> G3 不改变现有调用方行为（不带 `userId` 仍为系统级）；G1 对违规行为由「静默写入」改为「整次拒绝」，属升级需知，已写入 ChangeLog。

### 7.2 新增测试与门禁

| 命令 | 结果 |
|------|------|
| `dotnet test NewLife.Cube.Tests --filter DataScopeRowPermission+DataPermissionArchitecture+UserScopeQueryBypassGuard+DataScopeMenuOverride+ExportController+CubeDataScope+ImportRowPermission+WidgetData+CubeTools` | **79 / 0**（新增 `CubeDataScopeTests` 14 例、`ImportRowPermissionTests` 11 例、`WidgetDataTests` 新增 4 例） |
| `dotnet test XUnitTest --filter DataScope+CubeTools+IEntityAiContext+Sensitive` | **36 / 0** |
| `dotnet build NewLife.Cube` / `NewLife.CubeNC` | **0 警告 0 错误**（双栈） |

覆盖点：导入四类归属判定与跨实体数据集拒绝、合并模式覆盖不可见行拒绝、主键未声明不查库；AI 越权降级与正常取值；部件仅本人范围计数归零/明细清空与系统态不过滤；`CubeDataScope` 判定矩阵。

### 7.3 未补齐项（按裁定仅记录）

G5 菜单级覆盖未接线、G6 部件投影黑名单、G7 `MaskSensitiveList` 返回值、G8 `***` 往返写回、G10 文档/台账漂移（含 `PERM-数据权限.md:138` 的 `Trance` 错字、README TFM、`tasks.md` 历史用例数）。
