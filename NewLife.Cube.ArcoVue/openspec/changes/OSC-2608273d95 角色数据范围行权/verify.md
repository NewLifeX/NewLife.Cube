# OSC-2608273d95 Verify

> 进入 `Validating` 后逐项勾选。

## 必须保留（暂缓区，实施误删即失败）

- GetPage `[AllowAnonymous]`；**不得**把 DataScope SQL / 部门 ID 列表下发浏览器当授权依据。
- `viewFilter` logic=any 不得 OR 掉行权（OSC-260819e483）。
- 租户 `CreateWhere` 分支语义不变，与 DataScope **AND**。
- 不实现角色×字段矩阵 ACL；不把 ViewProfile 藏列当授权。
- 不给 Log/Token 做「本部门可见同事的日志/令牌」。
- 不给 Menu/Role/File 加 IDataScope 行过滤。
- 不新增 `/iam`；不改 Cube.Vue。
- `RoleController.Valid` 的 Permission `menuId#flags` 解析保留（OSC-260824fc7c）。
- `DataPermissionAttribute` 类型可保留；禁止再给 User 挂回仅本人表达式。

## 命令与预期

仓库根 `NewLife.XCode` / `NewLife.Cube`；前端 `NewLife.Cube.ArcoVue/web`。

```
dotnet test <XCode 测试工程> --filter FullyQualifiedName~DataScope
dotnet test NewLife.Cube.Tests/NewLife.Cube.Tests.csproj --filter FullyQualifiedName~DataScope
dotnet build NewLife.Cube/NewLife.Cube.csproj
dotnet build NewLife.CubeNC/NewLife.CubeNC.csproj
pnpm --filter @cube/arco-vue test
pnpm --filter @cube/arco-vue build
```

预期：本号相关测试 0 failed；构建 0 error。

## Happy path

- [ ] **AC-01 本部门用户列表**：非系统、DataScope=本部门、部门 D；GetPage/Index `Admin/User` 含同部门其他用户，不含部门≠D。
- [ ] **AC-02 仅本人用户列表**：非系统、仅本人；列表仅自己。
- [ ] **AC-03 全部/系统角色**：DataScope=全部或 IsSystem；用户列表不受部门裁剪（租户仍裁）。
- [ ] **AC-04 自定义**：仅 `DataDepartmentIds` 内部门的用户出现。
- [ ] **AC-05 本部门及下级**：含子部门用户，不含旁支。
- [ ] **AC-06 日志不扩权**：本部门角色 `Admin/Log` 仍只有 CreateUserID=自己的行。
- [ ] **AC-07 部门列表仅本人**：能看到自己的部门行，不是空表。
- [ ] **AC-08 Role 保存全部**：普通角色 DataScope=0 更新后读回仍为全部。

## 权限 / 空 / 非法 / 兼容

- [ ] **AC-09 越权详情**：仅本人 GET 他人 User id → 非法访问 / 非 200 实体。
- [ ] **AC-10 越权写入**：拦截器或 ValidPermission 拒绝改他人（仅本人）。
- [ ] **AC-11 无接口实体**：本部门角色打开 Admin/Role、Admin/Menu 仍有数据（不因 GetFilter 变 1=0）。
- [ ] **AC-12 无部门用户+本部门**：User.DepartmentID=0 → 用户列表空或仅符合恒假，不得变全表。
- [ ] **AC-13 DataPermission 已拆**：UserController 源码无 `[DataPermission]`。
- [ ] **AC-14 敏感**：无 ViewSensitive 时他人 Password 不为原哈希（`***` 或字段不在列表）。
- [ ] **AC-15 GetPage sensitive**：无 ViewSensitive 时密码字段 `sensitive=true`。
- [ ] **AC-16 前端自定义部门**：DataScope≠自定义不渲染 DataDepartmentIds。
- [ ] **AC-17 仅自己提示**：两名同部门用户时不出现 self-only alert；仅自己一行仍出现原本文案。
- [ ] **AC-18 viewFilter**：带 viewFilter 的列表仍受 DataScope 约束。
- [ ] **AC-19 旧客户端**：无新查询参数；仅行集变严/变准，契约字段名不变。
- [ ] **AC-20 文档**：`PERM-数据权限.md` 无 `DataScopeType`、无三参数 DataPermission 构造示例。

## 残余（不阻断 Done）

- Cube.Vue 皮肤无专门提示条（本号不改）。
- 菜单 DataScope 覆盖的手工项若无现成菜单数据可记 P2，但单元测试须覆盖 `SetMenu`：`>=0` 覆盖、`<0` 继承。
