# OSC-2608273d95 Tasks

> 顺序：XCode 矩阵与拦截器 → 实体挂载 → Cube CreateWhere/脱敏 → 拆 DataPermission → ArcoVue → 文档 → 测试构建。双栈文件成对勾选。

## T1 XCode 行权修正

- [ ] `DataScopeHelper.GetFilter` / `CanAccess`：IUserScope 非 IDataScope 非全部则只滤用户字段；IDepartmentScope 仅本人≡本部门（当前 DepartmentId）；空部门 → Equal(-1)
- [ ] 缓存键含 `deptId`；ClearCache(userId) 清旧键+新键
- [ ] `DataScopeInterceptor.OnValid`：失败 return false（或抛出），禁止 catch 后 true
- [ ] `角色.Biz.cs` Valid：仅 Insert 且 DataScope 未脏时写默认；Update 保留 0=全部
- [ ] 补 `DataScopeTests.cs`：上列矩阵 + OnValid 拒绝 + Role 保存 0

## T2 实体接口与拦截器

- [ ] User：`Add<DataScopeInterceptor>()`
- [ ] Department：IDepartmentScope + FieldProvider(`_.ID`) + 拦截器
- [ ] Log：IUserScope + FieldProvider(`_.CreateUserID`) + 拦截器
- [ ] Cube：UserToken / UserOnline / UserConnect / OAuthLog / NotificationRecord：IUserScope 显式映射 + FieldProvider + 拦截器

## T3 Cube 查询/详情/写入接线

- [ ] `ReadOnlyEntityController2.SearchData`：GetFilter AND 进 p.State；不得被 viewFilter logic=any 放大
- [ ] `FindData` + 默认 `ValidPermission`：CanAccess
- [ ] Index/详情/导出返回前 `MaskSensitiveFields`
- [ ] `DataField.Sensitive`；`PrepareFieldsForApi` 按 IFieldScope + ViewSensitive 打标
- [ ] 确认 CubeNC 与 WebAPI 共享同一 `ReadOnlyEntityController2` 或两份同步

## T4 拆除仅本人 DataPermission

- [ ] 两栈去掉 User/Log/UserToken/UserOnline/UserConnect/OAuthLog/NotificationRecord 上的 `[DataPermission]`
- [ ] 特性类与 CreateWhere 额外 AND 分支保留（无特性则不进入）
- [ ] WebAPI `RoleController`：DataDepartmentIds DataSource 对齐 CubeNC

## T5 ArcoVue

- [ ] `dataScopeForm.ts` + spec：自定义才显示 DataDepartmentIds
- [ ] Role 表单接入显隐（薄 SFC，逻辑在 util / useXxx）
- [ ] `rejectSensitiveColumns` + 列表用；spec
- [ ] `shouldShowSelfOnlyUserAlert` spec：本部门两行不提示；仅自己一行仍提示
- [ ] `web/README.md` 一行说明行权在服务端

## T6 文档

- [ ] 重写 `Doc/PERM-数据权限.md`（与 design §1 矩阵一致）
- [ ] `Doc/功能清单.md` PERM-6
- [ ] 迁移方案 / 竞品分析行权限一句回写本号 ID

## T7 测试与构建

- [ ] `dotnet test`：XCode DataScopeTests + Cube 本号新增测试 全过
- [ ] `dotnet build` NewLife.Cube + NewLife.CubeNC 0 error
- [ ] `pnpm --filter @cube/arco-vue test` 本号 spec 全过；`pnpm --filter @cube/arco-vue build` 0 error

## T8 手工冒烟（验收勾）

- [ ] 普通角色 仅本人 / 本部门 / 本部门及下级 / 自定义 / 全部 各一用户，打开 Admin/User
- [ ] 系统角色看全用户
- [ ] 仅本人打他人用户详情 ID → 拒绝
- [ ] Admin/Role、Admin/Menu 列表不因本部门变空
- [ ] 无 ViewSensitive 时他人用户不出现真实密码哈希
