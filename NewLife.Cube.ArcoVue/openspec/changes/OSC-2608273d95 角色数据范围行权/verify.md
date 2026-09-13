# OSC-2608273d95 Verify

> 进入 `Validating` 后逐项勾选。带【单测】的项已由自动化测试覆盖（证据见 `tasks.md` T9）；带【待冒烟】的项需在真实实例按 T10 人工执行（本机无法拉起 CubeDemo + 浏览器会话）。

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

- [ ] **AC-01 本部门用户列表**【待冒烟】：非系统、DataScope=本部门、部门 D；GetPage/Index `Admin/User` 含同部门其他用户，不含部门≠D。
- [ ] **AC-02 仅本人用户列表**【待冒烟】：非系统、仅本人；列表仅自己。
- [ ] **AC-03 全部/系统角色**【待冒烟】：DataScope=全部或 IsSystem；用户列表不受部门裁剪（租户仍裁）。
- [ ] **AC-04 自定义**【待冒烟】：仅 `DataDepartmentIds` 内部门的用户出现。
- [ ] **AC-05 本部门及下级**【待冒烟】：含子部门用户，不含旁支。
- [ ] **AC-06 日志不扩权**【待冒烟】：本部门角色 `Admin/Log` 仍只有 CreateUserID=自己的行。
- [ ] **AC-07 部门列表仅本人**【待冒烟】：能看到自己的部门行，不是空表。
- [ ] **AC-08 Role 保存全部**【待冒烟】：普通角色 DataScope=0 更新后读回仍为全部。

## 权限 / 空 / 非法 / 兼容

- [x] **AC-09 越权详情**【单测】：仅本人 GET 他人 User id → 非法访问——`DataScopeRowPermissionTests.FindData_OtherUser_Rejected`。
- [x] **AC-10 越权写入**【单测】：`ValidPermission`/`CanAccess` 拒绍改他人（仅本人）；新增放行——`ValidPermission_UpdateOther_Rejected_InsertAllowed`。**【收尾门禁加固】** 新增也不再无条件放行：声明**他人**归属时拒绝（`ValidPermission_Insert_ForgedOwner_Rejected`），系统态放行（`ValidPermission_Insert_SystemScope_Allowed`）
- [x] **AC-11 无接口实体**【单测】：不因 GetFilter 变 1=0——`SearchData_NoScopeEntity_NoFilter`（无接口实体 GetFilter 返回 null）。
- [ ] **AC-12 无部门用户+本部门**【待冒烟】：User.DepartmentID=0 → 列表不得变全表（XCode 实际退化为「本人恒可见 + 部门集合恒假」，即仅本人一行）
- [x] **AC-13 DataPermission 已拆**【单测】：矩阵内 7 控制器（双栈）源码无 `[DataPermission]`——`DataScopeDecouplingTests` 13/13 + API 版 6 例。
- [x] **AC-14 敏感**【单测】：无 ViewSensitive 时他人 Password 置 `***`——`MaskSensitiveFields_Others_WithoutViewSensitive_Masked`；**双栈**：WebAPI 与 CubeNC 均接入（共享 `ReadOnlyEntityControllerScope.cs`，含 CubeNC 令牌端点 5 个出口）
- [x] **AC-15 GetPage sensitive**【单测】：无 ViewSensitive 时密码字段 `sensitive=true`——`GetSensitiveFieldNames_UsesRealUserContext`。
- [x] **AC-16 前端自定义部门**【单测】：DataScope≠自定义不渲染 DataDepartmentIds——`dataScopeForm.spec.ts`。
- [x] **AC-17 仅自己提示**【单测】：同部门多人不提示、仅自己一行仍提示——`iamGuards.spec.ts`。
- [ ] **AC-18 viewFilter**【单测+待冒烟】：带 viewFilter 的列表仍受 DataScope 约束；`logic=any` 只 OR 前端条件（`SearchData_SelfScope_FiltersByOwner` 钡住 State，真实带参行集待冒烟）。
- [x] **AC-19 旧客户端**【审阅】：无新查询参数；契约字段名不变（新增可选 `sensitive`）。**行集可能变宽**：拆掉仅本人 `DataPermission` 后，非系统角色的行集改由角色 `DataScope` 四档决定，存量系统里「本部门/下级/自定义/显式全部」的角色会比改造前看到更多行（含审计日志）——属设计意图，升级需告知
- [x] **AC-20 文档**【审阅】：`PERM-数据权限.md` 无 `DataScopeType`、无三参数 DataPermission 构造示例。
- [ ] **AC-21 导出行集**【待冒烟】：仅本人导出 User 不含他人行（导出走 `ExportDataMasked` → 同一 `SearchData`）。
- [ ] **AC-22 Widget Query**【待冒烟】：本部门角色对 User 的 count 不含外部门（`WidgetQueryService` 已追加 `GetFilter`）。
- [x] **AC-23 XCode 零改动回归**【单测】：上游 `DataScopeTests` 92/92；本次未改 XCode 仓库。
- [x] **AC-24 实体层纯净**【单测】：`DataScopeDecouplingTests` 前两条绿（无 `DataScopeInterceptor`；跨用户查询不被实体层收窄）。
- [x] **AC-25 行权上下文**【单测】：均用控制器层构造的真实用户上下文，`DataScopeContext.Current` 仍为宿主系统态——`GetDataScopeContext_DoesNotWriteCurrent` + `GetSensitiveFieldNames_UsesRealUserContext`。

## 残余（不阻断 Done）

- Cube.Vue 皮肤无专门提示条（本号不改）。
- 菜单 DataScope 覆盖的手工项若无现成菜单数据可记 P2，但单元测试须覆盖 `SetMenu`：`>=0` 覆盖、`<0` 继承。
