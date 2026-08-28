# OSC-2608273d95 — 角色数据范围行权

## 1. 目标愿景

让角色上的 **数据范围**（`Role.DataScope`）成为行级权限的唯一事实源：列表、详情、导出、写入与 ORM 直查都按该范围过滤；敏感字段按已有 `IFieldScope` / `ViewSensitive` 脱敏并在 GetPage 打标，为后续字段矩阵 ACL 留同一上下文。

- 目标 1：实现 `IDataScope` / `IUserScope` / `IDepartmentScope` 的实体，查询自动按当前用户合并后的 `DataScope` 过滤；`IsSystem` 或范围为「全部」不加行过滤。
- 目标 2：`Admin/User` 等现有 `DataPermission("仅本人")` 不再覆盖「本部门 / 本部门及下级 / 自定义」；仅本人范围仍只见自己。
- 目标 3：详情 / 导出 / 增删改与列表同一套 `CanAccess`；拦截器校验失败不得放行。
- 目标 4：`IFieldScope` 列表/详情脱敏 + GetPage `sensitive` 标记；**不做**角色×字段可见/可写矩阵（另号）。

## 2. 为何做

审查结论：`DataScopeContext` 会算、角色表单能改 `DataScope`，但 Cube `CreateWhere` 只用 `DataPermissionAttribute` 固定表达式（非系统角色一律仅本人）。`DataScopeInterceptor` 从未挂到生产实体。ArcoVue 只信任后端，改角色数据范围不会改变用户列表。文档 `PERM-数据权限.md` 与真实特性 API 不一致。OSC-260824fc7c 曾冻结「不改 User DataPermission」，本号显式推翻该冻结中与行权相关的一条。

## 3. 已锁定范围（创建前确认）

| # | 决策 |
| --- | --- |
| 1 | **DataScope 为唯一行权**。去掉会把本部门压成仅本人的 `DataPermission`（User / Log / Token / Online / Connect / OAuthLog / NotificationRecord）。特性类保留，仅作可选额外 AND。 |
| 2 | **接口自动过滤**：实现 `IDataScope` / `IUserScope` / `IDepartmentScope` 的实体自动过滤。菜单、角色、文件、多数配置表不实现接口 → **不加行权**，靠菜单权限。 |
| 3 | **本号字段**：接通 `IFieldScope` + `ViewSensitive` 脱敏与 GetPage 标记；字段矩阵 ACL 另号。 |
| 4 | **允许普通角色存「全部」**。拆开 Valid 里 `DataScope==0` 当成未设置的逻辑。 |
| 5 | 无部门字段的 `IUserScope` 实体（日志、令牌等）：「本部门 / 下级 / 自定义」**不扩成同事数据**，仍按归属用户过滤（表上没有部门列，禁止 join 用户表扩权）。 |
| 6 | 双栈：WebAPI `NewLife.Cube` 与 MVC `NewLife.CubeNC` 的 `CreateWhere` / `FindData` / `ValidPermission` / Role 表单同源行为。 |
| 7 | 不改 Cube.Vue / NaiveUI 专用页；JSON API 行为变化对它们自然生效。 |

## 4. 做什么

1. 修正 `DataScopeHelper.GetFilter` / `CanAccess` / 部门缓存键；拦截器 `OnValid` 失败返回 false。
2. `Role.Valid` 仅在新增且未写 DataScope 时给默认值；「全部」可持久化。
3. 给 User / Department / Log 及 Cube 个人数据实体补接口 + `DataScopeInterceptor`；拆除对应控制器 `DataPermission`。
4. `CreateWhere` / `SearchData` / `FindData` / `ValidPermission` / 列表序列化接线；`DataField.Sensitive` + PrepareFieldsForApi。
5. ArcoVue：自定义范围才显示 `DataDepartmentIds`；列表隐藏 `sensitive` 列；修订仅自己提示（本部门可见同事时不误报）。
6. 重写 `Doc/PERM-数据权限.md`；功能清单 PERM-6；XUnit + 少量 Vitest。

## 5. 不做什么

- 不实现角色×字段 ACL、不把 ViewProfile 藏列当授权。
- 不按部门 join 放大日志/令牌行权。
- 不把菜单/角色/文件做成 DataScope 过滤。
- 不取消 GetPage `[AllowAnonymous]`；**不把过滤表达式下发浏览器**。
- 不改租户 `ITenantScope` 语义（租户条件仍在 CreateWhere，与行权 AND）。
- 不改 `PermissionFlags`、不新增 `/iam`。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-260824fc7c | 推翻「不改 User DataPermission」；保留授权树 / 系统角色锁 / 仅自己提示文案入口，按本号矩阵改触发条件 |
| XCode `DataScopeContext` / `DataScopeHelper` / `DataScopeInterceptor` | 本号修通并挂载，不平行造第二套行权 |
| OSC-260819e483 | `CreateWhere` / `SearchData` / `viewFilter` 与权限 AND 的顺序保持：行权不得被 logic=any 放大 |

## 7. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| XUnit（XCode） | 是 | GetFilter / CanAccess / 仅本人部门实体 / 拦截器 OnValid 失败 / 缓存键含部门 |
| XUnit（Cube） | 是 | CreateWhere 合并 GetFilter；User 本部门可见同部门；拆除 DataPermission 后非系统不再恒 ID=自己 |
| Vitest | 是 | DataDepartmentIds 显隐；sensitive 列隐藏；self-only 提示仅在「仅本人且仅自己一行」 |
| 构建 | 是 | NewLife.XCode + NewLife.Cube + NewLife.CubeNC + arco-vue |
| 手工 | 是 | 四档 DataScope 用户列表；系统角色看全；自定义部门；敏感字段 *** |

## 8. 成功标准

- [ ] 非系统 +「本部门」打开 `Admin/User` 能看到同部门其他用户，看不到外部门。
- [ ] 非系统 +「仅本人」只看到自己；系统角色或「全部」看到全表（租户条件仍生效）。
- [ ] 无接口实体（如 Role/Menu）不因 DataScope 变空。
- [ ] 直打详情 ID 越权 → 非法访问；拦截器拒绝写入。
- [ ] GetPage 含 `sensitive`；无 ViewSensitive 且非本人时密码类字段脱敏。
- [ ] `PERM-数据权限.md` 与代码一致，不再出现 `DataScopeType` / 错误的 DataPermission 三字段构造器。
