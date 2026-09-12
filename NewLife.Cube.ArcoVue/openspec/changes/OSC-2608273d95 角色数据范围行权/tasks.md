# OSC-2608273d95 Tasks（路线 A：接口层行权 · 2026-09-12 重新分解）

> 前提：`audit-merged.md` 选定路线 A —— **不改 XCode 仓库**、不给任何实体挂 `DataScopeInterceptor`、不改写 `DataScopeContext.Current`；行权在控制器层显式构造上下文后调用 XCode 现成 API（`DataScopeHelper.GetFilter/CanAccess`、`FieldScopeHelper.MaskSensitiveFields`）。
> 顺序：T1 前置回归 → T2 实体接口 → T3 上下文与列表接线 → T4 详情/写入 → T5 拆特性+守护同步 → T6 脱敏 → T7 ArcoVue → T8 文档 → T9 测试构建 → T10 手工冒烟。
> 双栈（`NewLife.Cube` WebAPI / `NewLife.CubeNC` MVC）成对勾选；`Common/ReadOnlyEntityController2.cs` 由 NC csproj `<Compile Include>` Link，**只改一处**。

## T1 前置回归（只读，不改 XCode）

- [ ] 跑 `dotnet test XUnitTest.XCode --filter FullyQualifiedName~DataScope`：确认上游 12.2.2026.0901 交付项全绿（GetFilter / CanAccess / Role.Valid 0=全部 / 缓存键 deptId / 部门仅本人≡本部门 / OnValid false）
- [ ] 复核 `NewLife.Cube/NewLife.Cube.csproj` 的 `NewLife.XCode` 版本为 `12.2.2026.901`（本号不升不降）
- [ ] 跑 `dotnet test XUnitTest --filter FullyQualifiedName~DataScopeDecoupling` 记录基线（3 条守护，其中第 3 条本号将按 T5 同步）

## T2 Cube 个人数据实体加接口（只加接口，不挂拦截器）

- [ ] `Entity/用户令牌.Biz.cs`、`用户在线.Biz.cs`、`用户链接.Biz.cs`：实现 `IUserScope` + `IDataScopeFieldProvider`（显式 `Int32 IUserScope.UserId { get => UserID; set => UserID = value; }`，`GetUserField() => _.UserID`）
- [ ] `Entity/OAuth日志.Biz.cs`、`通知记录.Biz.cs`：实现 `IUserScope` + `IDataScopeFieldProvider`（`UserId` 属性名一致，可隐式实现）
- [ ] 自查 `is IUserScope` 判定处无意外分支；**不得**出现 `Meta.Interceptors.Add<DataScopeInterceptor>()`

## T3 行权上下文与列表接线

- [ ] `Common/ReadOnlyEntityController2.cs`：新增 `GetDataScopeContext()`（`DataScopeContext.Create(ManageProvider.User as IUser)`，按请求缓存 `HttpContext.Items`，**不写** `DataScopeContext.Current`）
- [ ] 新增 `GetDataScopeExpression()` → `DataScopeHelper.GetFilter(Factory, GetDataScopeContext())`
- [ ] `SearchData`：GetFilter **AND** 进 `p.State`，`logic=any` 不得 OR 进行权；解析失败时保留行权、放弃 viewFilter
- [ ] `Widgets/WidgetQueryService.cs`：聚合/列表 Where 追加 `GetFilter(fact, ctx)`（与列表同一助手）

## T4 详情/写入同一 CanAccess

- [ ] `FindData`：`!CanAccess(entity)` → 抛既有「非法访问数据」
- [ ] `ValidPermission` 默认改为 `CanAccess`；`UserTokenController.ValidPermission` 等 override 先调 `base.ValidPermission` 再业务规则
- [ ] 详情/导出行集走同一 `SearchData`（禁止另开无 State 的 `FindAll`）

## T5 拆仅本人特性 + 守护测试同步

- [ ] 双栈去 `[DataPermission]`：User、Log、UserToken、UserOnline、UserConnect、OAuthLog；NotificationRecord **仅 CubeNC**
- [ ] `XUnitTest/DataScopeDecouplingTests.cs`：仅改 `Controllers_HaveExpectedDataPermission`（矩阵内 7 控制器改为断言「无特性」）；`Entities_DoNotRegisterDataScopeInterceptor`、`CrossUserQuery_NotFilteredByEntityLayer` **不动**
- [ ] Parameter / Attachment / PrincipalAgent 原表达式保留（未纳入本号）

## T6 字段脱敏与 GetPage 标记

- [ ] `ViewModels/DataField.cs` 增 `Boolean Sensitive`（JSON `sensitive`）
- [ ] `PrepareFieldsForApi`：`IFieldScope.GetSensitiveFields()` 命中且真实 ctx 未授予 `ViewSensitive` → `Sensitive=true`
- [ ] `Common/ReadOnlyEntityController.cs`：列表 / 详情 / 导出返回前 `FieldScopeHelper.MaskSensitiveFields(entity, ctx)`（**不得**用宿主态上下文）

## T7 ArcoVue

- [ ] `web/src/core/utils/dataScopeForm.ts` + spec：`isCustomDataScope` / `shouldShowDataDepartmentIds`
- [ ] Role 表单薄接入（FieldInput / DefaultForm 显隐，不写 vue 分支）
- [ ] `rejectSensitiveColumns` + 列表接线 + spec
- [ ] `iamGuards.shouldShowSelfOnlyUserAlert` 复核：本部门多行不提示、仅自己一行仍提示
- [ ] `web/README.md` 一句：行权在服务端 DataScope，前端筛选不作为授权

## T8 文档

- [ ] `Doc/PERM-数据权限.md` **增量**补「DataScope 四档矩阵 + 接口层 GetFilter 接线」（保留合并后 §9.6「实体层不承担行权」结论）
- [ ] `Doc/功能清单.md` PERM-6 改为 DataScope 行权并注明本号
- [ ] `ArcoVue企业中后台迁移方案.md` §8.6 / `竞品分析报告.md` 行权限事实回写
- [ ] `NewLife.XCode-Issue.md` 顶部标注「已由 12.2.2026.0901 修复，仅存档」；`web/docs/**` 若仍写「DataPermission 仅本人」则改一句

## T9 测试与构建

- [ ] Cube XUnit：CreateWhere 合 GetFilter（本部门可见同事）/ 无接口实体 GetFilter=null / FindData 越权拒绝 / ValidPermission / Cube 实体接口映射 / Mask 用真实上下文
- [ ] Vitest：`dataScopeForm` + `rejectSensitiveColumns` + `iamGuards`
- [ ] `dotnet build NewLife.Cube` + `NewLife.CubeNC` 0 error；`pnpm --filter @newlifex/cube-arco-vue test` 与 `build` 通过

## T10 手工冒烟（验收勾）

- [ ] 四档 DataScope（仅本人 / 本部门 / 本部门及下级 / 自定义 / 全部）各一用户打开 `Admin/User`
- [ ] 系统角色看全；Role / Menu 列表不因行权变空
- [ ] 仅本人直打他人 User 详情 id → 拒绝；写入被拒
- [ ] 无 `ViewSensitive` 时他人 Password 不出现原哈希
- [ ] Widget（WebAPI）本部门角色的 count 不含外部门
- [ ] 实体层守护：`DataScopeDecouplingTests` 前两条仍绿（未挂拦截器）
