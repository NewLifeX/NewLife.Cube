# OSC-2608273d95 Tasks（路线 A：接口层行权 · 2026-09-12 重新分解）

> 前提：`audit-merged.md` 选定路线 A —— **不改 XCode 仓库**、不给任何实体挂 `DataScopeInterceptor`、不改写 `DataScopeContext.Current`；行权在控制器层显式构造上下文后调用 XCode 现成 API（`DataScopeHelper.GetFilter/CanAccess`、`FieldScopeHelper.MaskSensitiveFields`）。
> 顺序：T1 前置回归 → T2 实体接口 → T3 上下文与列表接线 → T4 详情/写入 → T5 拆特性+守护同步 → T6 脱敏 → T7 ArcoVue → T8 文档 → T9 测试构建 → T10 手工冒烟。
> 双栈（`NewLife.Cube` WebAPI / `NewLife.CubeNC` MVC）成对勾选；`Common/ReadOnlyEntityController2.cs` 由 NC csproj `<Compile Include>` Link，**只改一处**。

## T1 前置回归（只读，不改 XCode）

- [x] 跑 `dotnet test XUnitTest.XCode --filter FullyQualifiedName~DataScope`：确认上游 12.2.2026.0901 交付项全绿（GetFilter / CanAccess / Role.Valid 0=全部 / 缓存键 deptId / 部门仅本人≡本部门 / OnValid false）——实测 `92/92 通过`
- [x] 复核 `NewLife.Cube/NewLife.Cube.csproj` 的 `NewLife.XCode` 版本为 `12.2.2026.901`（本号不升不降）
- [x] 跑 `dotnet test XUnitTest --filter FullyQualifiedName~DataScopeDecoupling` 记录基线（3 条守护，其中第 3 条本号将按 T5 同步）——基线 `13/13 通过`

## T2 Cube 个人数据实体加接口（只加接口，不挂拦截器）

- [x] `Entity/用户令牌.Biz.cs`、`用户在线.Biz.cs`、`用户链接.Biz.cs`：实现 `IUserScope` + `IDataScopeFieldProvider`（显式 `Int32 IUserScope.UserId { get => UserID; set => UserID = value; }`，`GetUserField() => _.UserID`）
- [x] `Entity/OAuth日志.Biz.cs`、`通知记录.Biz.cs`：实现 `IUserScope` + `IDataScopeFieldProvider`（`UserId` 属性名一致，可隐式实现）
- [x] 自查 `is IUserScope` 判定处无意外分支；**不得**出现 `Meta.Interceptors.Add<DataScopeInterceptor>()`（由 `DataScopeDecouplingTests` 守护）

## T3 行权上下文与列表接线

- [x] `Common/ReadOnlyEntityController2.cs`：新增 `GetDataScopeContext()`（`DataScopeContext.Create(ManageProvider.User as IUser)`，按请求缓存 `HttpContext.Items`，**不写** `DataScopeContext.Current`）
- [x] 新增 `GetDataScopeExpression()` → `DataScopeHelper.GetFilter(Factory, GetDataScopeContext())`
- [x] `SearchData`：GetFilter **AND** 进 `p.State`，`logic=any` 不得 OR 进行权；解析失败时保留行权、放弃 viewFilter
- [x] `Widgets/WidgetQueryService.cs`：聚合/列表 Where 追加 `GetFilter(fact, ctx)`（与列表同一助手）

## T4 详情/写入同一 CanAccess

- [x] `FindData`：`!CanAccess(entity)` → 抛既有「非法访问数据」
- [x] `ValidPermission` 默认改为 `CanAccess`；`UserTokenController.ValidPermission` 等 override 先调 `base.ValidPermission` 再业务规则（新增除外的理由已在 XML 注释注明）
- [x] 详情/导出行集走同一 `SearchData`（禁止另开无 State 的 `FindAll`）；导出经 `ExportDataMasked` 包一层，兼容派生类重写 `ExportData`
- [x] `RoleController`（WebAPI）`DataDepartmentIds` 对齐 CubeNC（Add/EditForm 的 `DataSource`）

## T5 拆仅本人特性 + 守护测试同步

- [x] 双栈去 `[DataPermission]`：User、Log、UserToken、UserOnline、UserConnect、OAuthLog；NotificationRecord **仅 CubeNC**
- [x] `XUnitTest/DataScopeDecouplingTests.cs`：仅改 `Controllers_HaveExpectedDataPermission`（矩阵内 7 控制器改为断言「无特性」）；`Entities_DoNotRegisterDataScopeInterceptor`、`CrossUserQuery_NotFilteredByEntityLayer` **不动**
- [x] Parameter / Attachment / PrincipalAgent 原表达式保留（未纳入本号）；另在 `NewLife.Cube.Tests/Web/DataPermissionArchitectureTests.cs` 同步 API 版矩阵

## T6 字段脱敏与 GetPage 标记

- [x] `ViewModels/DataField.cs` 增 `Boolean Sensitive`（JSON `sensitive`）；`packages/api-core/src/types.ts` 与前端 `FieldMeta`/`fieldNormalize` 透传
- [x] `PrepareFieldsForApi`：`IFieldScope.GetSensitiveFields()` 命中且真实 ctx 未授予 `ViewSensitive` → `Sensitive=true`（`GetSensitiveFieldNames` 按请求缓存）
- [x] `Common/ReadOnlyEntityController.cs`：列表 / 详情 / 导出返回前 `FieldScopeHelper.MaskSensitiveFields(entity, ctx)`（**不得**用宿主态上下文；导出经 `ExportDataMasked` 惰性遮蔽）

## T7 ArcoVue

- [x] `web/src/core/utils/dataScopeForm.ts` + spec：`isCustomDataScope` / `shouldShowDataDepartmentIds` / `shouldShowDataScopeField`
- [x] Role 表单薄接入（判定在 `core/utils/dataScopeForm.ts`，`useFormContent.visibleFields` 一处 filter，`.vue` 无分支）
- [x] `rejectSensitiveColumns` + 列表接线（`selectListColumns` 内应用，覆盖列表与部件）+ spec
- [x] `iamGuards.shouldShowSelfOnlyUserAlert` 复核：本部门多行不提示、仅自己一行仍提示（补 spec 两例，逻辑本身无需改）
- [x] `web/README.md` 一句：行权在服务端 DataScope，前端筛选/藏列不作为授权

## T8 文档

- [x] `Doc/PERM-数据权限.md` **增量**补「DataScope 四档矩阵 + 接口层 GetFilter 接线」（保留合并后 §9.6「实体层不承担行权」结论）
- [x] `Doc/功能清单.md` PERM-6 改为 DataScope 行权并注明本号
- [x] `ArcoVue企业中后台迁移方案.md` §8.6（新增 8.6.1b 已交付）+ `竞品分析报告.md` 行权限事实回写
- [x] `NewLife.XCode-Issue.md` 顶部标注「已由 12.2.2026.0901 修复，仅存档」；`Doc/ALM-在线告警与站内信.md`、`Doc/BASE-中间件与过滤器.md` 与现码不符处各改一句

## T9 测试与构建

- [x] Cube XUnit：`Common/DataScopeRowPermissionTests.cs`（列表 State 合 GetFilter / 无接口实体 GetFilter=null / FindData 越权拒绝 / ValidPermission（含新增伪造归属）/ 实体接口映射 / 脱敏用真实上下文 / 上下文不写 Current）——与 `Web/DataPermissionArchitectureTests` 合计 `35/35 通过`（拆：13 + 22）；另新增 `Web/UserScopeQueryBypassGuardTests`（?id= 不走 FindData 的回退守护）与 `Common/DataScopeMenuOverrideTests`（菜单 SetMenu 覆盖/继承）。**验收修正**：计入后两族，本号相关 Cube XUnit 子集实测 **39 通过 / 0 失败**；再加 `ExportController` 族为 **45/45**（见 `verify.md` 门禁表）
- [x] CubeNC XUnit：`XUnitTest/DataScopeSensitiveMvcTests.cs`（MVC 侧列级 sensitive 标记 + 列表/详情遮蔽用真实上下文）`4/4 通过`；`DataScopeDecouplingTests` `13/13 通过`
- [x] Vitest：`dataScopeForm` + `rejectSensitiveColumns` + `iamGuards`——全量 `89 文件/848 用例通过`
- [x] `dotnet build NewLife.Cube` + `NewLife.CubeNC` 0 error 0 warning；`pnpm --filter @newlifex/cube-arco-vue test` 与 `build` 通过（`✓ built in 25.25s`）
- [x] 回归对比（`git worktree` HEAD=75e747ef 基线）：本次涉及子集与基线一致，**0 新增失败**；全量失败均为环境/租户夹具既有问题（与基线同类）

## T10 手工冒烟（验收勾）

> 2026-09-14 已在**隔离实例**（CubeDemo 副本 + 独立 SQLite，5099 端口，不触碰业务库）执行完毕，证据见 `verify.md`「验收执行」。

- [x] 四档 DataScope（仅本人 / 本部门 / 本部门及下级 / 自定义 / 全部）各一用户打开 `Admin/User`：1 / 6 / 7 / 1 / 10 行，SQL 与各档定义一致
- [~] 系统角色看全（✅ 10 行）；Role / Menu 列表不因行权变空（未单独采样，由单测与 XCode 92/92 回归覆盖）
- [x] 仅本人直打他人 User 详情 id → `非法访问数据[2]`（`ReadOnlyEntityController2.cs:296`）
- [x] 无 `ViewSensitive` 时他人 Password 为 `***`
- [ ] Widget（WebAPI）本部门角色的 count 不含外部门——**本轮未采样**（留 AC-22，代码路径已由单测覆盖）
- [x] 实体层守护：`DataScopeDecouplingTests` 前两条仍绿（未挂拦截器）
- [x] 额外汇总：日志不扩权（15 行全自己）、导出跟随行集（6 行 / 1 行）
## 执行回填（执行期新增的小任务，均已随本号完成）

- [x] API 版矩阵同步：`NewLife.Cube.Tests/Web/DataPermissionArchitectureTests.cs` 与 `XUnitTest/DataScopeDecouplingTests.cs` 同款断言（design 只写了 XUnitTest；API 版存在同一份矩阵，不同步会直接红灯）
- [x] 导出口收口：新增 `ExportDataMasked(max)` 惰性遮蔽，5 个 `OnExportXxx` 统一改走它——兼容派生类重写 `ExportData`（`NewLife.Cube.Tests/Common/ExportControllerTests` 就是这种派生）
- [x] `GetSensitiveFieldNames()` 按请求缓存（`HttpContext.Items["CubeSensitiveFields"]`）：GetPage 单次请求会多次 `PrepareFieldsForApi`，避免重复 `new TEntity()`
- [x] 前端契约透传：`packages/api-core/src/types.ts` 的 `DataField.sensitive` + `core/types/field.ts` 的 `FieldMeta.sensitive` + `fieldNormalize.pickSensitive`（design 只写到 `FieldMeta`，缺前两者则后端标记到不了前端）
- [x] 表单显隐接入点定为 `views/crud/useFormContent.ts` 的 `visibleFields`（design 写 FieldInput/DefaultForm 显隐；实际唯一薄接入点是该 filter，`.vue` 保持零分支）
- [x] 旁路排查：`iPaged`/`GetCachePager`/`FindData`/`ExportData` 全部落在同一 `SearchData`，无需额外改造
- [x] 回归基线对比：`git worktree` 拉 HEAD(`75e747ef`) 跑同一组过滤，本次涉及子集 **0 新增失败**，且当前树把基线 11 条环境性失败跑成通过；全量剩余失败均为租户/菜单夹具既有问题
- [x] 文档漂移回写（design T8 未列）：`Doc/ALM-在线告警与站内信.md`（NotificationRecord 可见性来源）、`Doc/BASE-中间件与过滤器.md`（特性示例补归属接口优先说明）
- [x] `RoleController`（WebAPI）`DataDepartmentIds` DataSource 对齐 CubeNC（design §4.4 明确要求，原 T4 清单未单列）

## 收尾门禁记录

- [x] 小任务回填（本节）+ verify.md AC 逐项标注【单测】/【待冒烟】
- [x] 代码审查（「代码审查」agent）——见下方“门禁结论”
- [x] 实现审计（「实现审计」agent）——见下方“门禁结论”
- [x] dev-loop 修复 🔴 / gaps（同节记录）
- [x] 循环 ≤4 次后无未决项 → 提示 `验收 OSC-2608273d95`

### 门禁结论

**轮次 1（2026-09-12）**：实现审计 + 代码审查各一轮，修复后复验通过。
**轮次 2（2026-09-13）**：实现复审计（HEAD `74475939`）发现 N1–N12，已按用户裁定「修复 N1–N5 + 若干 🔵」执行完毕，详见下方第 2 轮记录。

#### 已修复（本轮）

| # | 来源 | 问题 | 修复 |
|---|------|------|------|
| 1 | 审查 🔴-1 | CubeNC `UserController.OnExportZip` 往 zip 塞全表 `UserConnect`/`UserToken`（令牌=共享端点凭据，泄露即等于伪造任意用户身份），绕开行权 | 该方法新增 `if (!IsSystemUser()) return;` 门禁（`IsSystemUser()` 落在共享 partial，双栈同源），非系统角色只导出主数据集 |
| 2 | 审查 🔴-2 | `ValidPermission` 新增无条件放行 → 非系统用户可建「他人名义」令牌/绑定（**本变更引入的回归**：改造前 `UserTokenController.ValidPermission` 会拒绝） | 新增分支改为「提交阶段校验归属」：`entity is IUserScope` 且声明了**他人** `UserId` 时拒绝；0（未声明）与本人放行；系统态放行 |
| 3 | 审查 🟡 | CubeNC（MVC）栈此前完全没有列表/详情/导出脱敏与 GetPage `sensitive` 标记（design §4.1/§4.5 要求双栈） | 抽出共享 `Common/ReadOnlyEntityControllerScope.cs`（`GetSensitiveFieldNames`/`MaskSensitiveFields`/`MaskSensitiveList`/`ExportDataMasked`/`IsSystemUser`），`NewLife.CubeNC.csproj` Link；CubeNC 在 `IndexView`/`Detail`/6 处导出/`PrepareFieldsForApi` 接入 |
| 4 | 审查 🔵 | CubeNC 令牌端点 5 个出口（`Html`/`Json`/`Xml`/`Csv`/`Excel`）未遮蔽 | 统一 `MaskSensitiveList(SearchData(...))` |
| 5 | 审查 🟡 | 脱敏早于 `OnFillListValues`/字段计算，派生钩子会读到 `***`；敏感列名匹配区分大小写；缓存键未带实体类型 | 脱敏后移到行计算/流程覆盖之后；改用 `HashSet(StringComparer.OrdinalIgnoreCase)`；缓存键加 `typeof(TEntity).FullName` |
| 6 | 审查 🟡 | 注释与事实相反（宿主态 `ViewSensitive` 写成 true）＋空 `<returns>` | 修正注释方向与语义，补齐 `<returns>` |
| 7 | 审查/审计 🟡🔵 | 文档漂移：`PERM-数据权限.md` 示例仍用已拆的 `UserTokenController` 特性、迁移指引只写特性；`design.md §5` 接入点、`Doc/Api` 缺 `sensitive` 契约 | 改为真正的特例示例（Department/Parameter/Attachment/PrincipalAgent）+ 两步迁移指引；design §5 补实际接入点；`Doc/Api/字段元数据.md` 补 `sensitive` 小节 |

#### 接受的残留（不阻断验收，已在代码注释/文档登记）

| # | 内容 | 取舍理由 |
|---|------|----------|
| R1 | 审查 🔴-3：`SearchData` 的 `catch` 分支在 `builder != null`（租户/特性 WhereBuilder 解析失败）时保持原 `WhereBuilder`，不叠加行权 | ①该分支相对 HEAD 无行为变化（原逻辑即如此）；②XCode 侧会再次解析同一 WhereBuilder 并抛错 → 实际 fail-closed（表现为错误而非放行），已由审查确认「不是 fail-open」；③改为「只保留行权」会丢租户过滤（跨租户风险更高）；④根治需改 `CreateWhere` 契约或 XCode，超出本号「不改 XCode」边界。已在 `catch` 内显式 `XTrace` 记录并改写注释（不再宣称「一律保留行权」），`Doc/PERM-数据权限.md §9.4` 保留行权的前提是表达式可解析 |
| R2 | 审查 🟡-4：WebAPI `GetPage.allList` 未标 `sensitive` | 列设置面板据此可加回敏感列，但服务端 `MaskSensitiveFields` 已遮蔽为 `***`，不构成泄露；且当前 ArcoVue 未消费 `allList`。改动会影响列设置面板语义，留待需要时收口 |
| R3 | 审查 🟡-6/🟡-7：导出迭代器二次枚举（既有）；就地改写实体实例与 XCode 单对象缓存的邻接风险（上游 `SingleEntityCache.AddItem` 写入已注释，未复现污染） | 前者是既有行为，改成 `ToList()` 需评估 `MaxExport` 内存上限；后者需把脱敏下沉到序列化层，属独立设计 |
| R4 | 审计 G9：无「真实行集（本部门含同事/不含外部门）」自动化用例；G12：菜单 `SetMenu` 覆盖/继承无用例 | 行集端到端需真机（T10 冒烟覆盖 AC-01…AC-08）；`SetMenu` 属 XCode 侧且本号不得改该仓库，记入后续号 |
| R5 | 审查 🟡-8 剩余的 Widget/CubeNC 侧用例缺口 | WebAPI 已覆盖 `WidgetQueryService` 追加 `GetFilter` 的代码路径（读审 + 既有 `Osc260903WidgetQueryTests` 绿）；CubeNC 侧无测试工程可直接引用，靠 T10 冒烟 |

#### 测试证据（复验）

| 命令 | 结果 |
|------|------|
| `dotnet test NewLife.Cube.Tests --filter DataScopeRowPermissionTests\|DataPermissionArchitectureTests` | 33 → 修复后 **34 通过 / 0 失败**（含新增「新增伪造他人归属拒绝」「系统态放行」两例） |
| `dotnet test NewLife.Cube.Tests --filter 行权相关子集 + UserPageReadOnly + Osc260819P3` | **56 通过 / 0 失败** |
| `dotnet test XUnitTest`（MVC 全量） | **494 通过 / 6 失败**（另一轮实测 493/7；失败全部为 `QyWeiXinTests` 企业微信外网用例，基线同为环境性失败） |
| `dotnet test XUnitTest --filter DataScopeDecoupling` | **13 通过 / 0 失败** |
| `dotnet test XUnitTest.XCode --filter DataScope`（只读回归） | **92 通过 / 0 失败** |
| `dotnet build NewLife.Cube` + `NewLife.CubeNC` | **0 警告 0 错误** |
| `pnpm --filter @newlifex/cube-arco-vue test` / `build` | **89 文件 848 用例通过** / `✓ built` |
| 基线对照（`git worktree` HEAD=75e747ef） | 涉及子集 **0 新增失败** |

#### 结论

🔴 全部修复并复验；🟡 中 5 项修复、4 项登记为接受残留（R1–R5，均已注明理由与后续归属）。

## 验收期新增缺口（2026-09-14 已裁定：补齐 G1–G4 + G9；G5–G8/G10 仅记录）

> 来源：2026-09-14 验收的三步检查（实现审计 / 代码审查 / 文档同步）。**P0 = 0**。用户裁定「补齐 G1–G4（P1 四项）后再复盘」+「AC 判据按锁定包改写」，已于 2026-09-14 实现完毕（见 `verify.md` §7）；G5–G8/G10 按「仅记录」保留，留后续号。

- [x] **G1（P1）批量导入收口** —— 已修：`OnImport` 新增 `ValidImport`（归属不得为他人 / 已声明归属须在范围 / 合并类按主键不得覆盖不可见行）；`ImportZip` 对包内其它实体数据集校验页面新增权限（系统角色不受约束）；实测 `ImportRowPermissionTests` 11 例 + `EntityController2.cs`。同时修正 `Doc/PERM-数据权限.md` 生效范围表（导入从 `FindData` 行移出）。
- [x] **G2（P1）AI 记录上下文走行权** —— 已修：`CubeTools<TEntity>` 新增可选 `findRecord` 委托（越权降级为「记录不存在或无权访问」），`GetRecordContext`/`GetFormSchema(edit)` 改走它；控制器注入 `FindRecordMasked` = `FindData` + `MaskSensitiveFields`；实测 `CubeDataScopeTests` 3 例。
- [x] **G3（P1）App 密钥令牌出口行权策略** —— 已修：`ValidToken` 的 App 密钥分支支持 `userId`/`user` 参数绑定用户（绑定后与页面同一套行权），未绑定时显式 `XTrace` 声明系统级；`Doc/PERM-数据权限.md` 生效范围表与小节 2 按实现区分（无单测：CubeNC 令牌路径需 App 表数据，与 R5 同口径）。
- [x] **G4（P1）系统内置部件纳入行权** —— 已修：新增共享助手 `Common/CubeDataScope.cs`（双栈 Link），用户总数 / 在线 / 24h 日志 / 24h 异常 / 登录与在线明细（MVC+WebAPI 两套）接入 `GetFilter`；实测 `WidgetDataTests` 新增 4 例 + `CubeDataScopeTests` 判定矩阵。
- [ ] **G5（P2·仅记录）菜单级覆盖接线或降级宣传**：`DataScopeContext.Create(user)` 未传 `menu`，XCode `SetMenu` 覆盖未接线；但 `Doc/功能清单.md:67`、`Doc/PERM-数据权限.md:133`、`web/README.md:127` 已宣称「角色/菜单四档」。二选一：接线 `Create(user, menu)`，或把文档改为「本期仅角色级」。
- [ ] **G6（P2·仅记录）部件投影改 `IFieldScope`**：`WidgetQueryService.cs:629` 用名字黑名单（`Password/Secret/Salt`）过滤列，自定义敏感列（工资/身份证）在部件 list 不受遮蔽；`GetSensitiveFieldNames` 已就绪，改走它即可。
- [ ] **G7（P2·仅记录）`MaskSensitiveList` 返回值收口**：`ReadOnlyEntityControllerScope.cs:60-69` 非 `IList` 输入时返回新实例但 4 处调用点丢弃返回值 → 潜在静默漏脱敏；改为 `Ts = MaskSensitiveList(Ts)` 或改就地。
- [ ] **G8（P2·仅记录）`***` 往返写回收口**：遮蔽值可经「详情 → 编辑 → 提交」落库，当前仅靠 `Areas/Admin/Controllers/UserController.cs:309` 的只读门禁挡住；建议后端忽略等于当前遮蔽标记的敏感字段提交。
- [x] **G9（P2）AC 判据按锁定包版本改写** —— 已按裁定改写：`verify.md` AC-04/AC-07/AC-12 改为「部门集合正确 / 部门列表 = 我管理的部门 / 无部门 + 本部门 = 恒假条件（0 行）」，不再引用锁定包不具备的 `ac108a773` 语义。
- [ ] **G10（P2·仅记录）文档/台账漂移清理**：`Doc/PERM-数据权限.md:138` `Trance`→`Trace`；`README.md:392` TFM 写 net45（实为 net6–net10）；本文件 T9/T10 数字（已改）；`retro.md` 范围表「接口自动过滤」措辞；`Doc/功能清单.md` 单测清单漏列本号新增用例。

#### 轮次 2：实现复审计发现与修复（2026-09-13）

| # | 来源 | 问题 | 修复 |
|---|------|------|------|
| N1 | 复审计 🔴 | 双栈 `UserController.Search(Pager)` 的 `id>0` 分支直接 `FindByID` 返回，绕过 `SearchData` 行权管道（`?id=<他人ID>` 可读他人行） | 双栈改走 `FindData(id)`（含 `CanAccess`，越权抛「非法访问数据」）；顺带修掉 WebAPI 同分支 `list.Add(entity)` 重复两次的合并遗留 bug；新增源码守护用例 `UserScopeQueryBypassGuardTests` |
| N2 | 复审计 🟡 | `DepartmentController` 详情不叠加 `CanAccess`，与列表语义不一致；design §1.7 把 Department 写成「无特性」与现码矛盾 | 保留 ManagerId 判定（部门表「一行即一个部门」与 DataScope 部门集合退化重叠，叠加会误挡），在双栈 `FindData` 补 XML 备注说明取舍；修订 design §1.7 |
| N3 | 复审计 🟡 | ①`Doc/Api` 的 `sensitive` 契约未落地却被记为已修（虚标）；②迁移方案 8.6.1b 仍写「ValidPermission 新增放行」；③「解析失败保留行权」仍作普适宣称 | 补 `Doc/Api/字段元数据.md` 6.1 敏感字段小节 + `Doc/Api/核心接口架构.md` 一句；更正迁移方案两处；更正 `Doc/PERM-数据权限.md` 行权/新增/解析失败三处措辞 |
| N4 | 复审计 🟡 | 升级语义（非系统角色行集可能变宽）未登记 | `ChangeLog.md` 新增 `v6.15.2026.0912` 安全语义变更节；`verify.md` AC-19 措辞更正 |
| N5 | 复审计 🟡 | AC-14 标【单测】称「双栈」，实际只有 WebAPI 用例；R5 理由「NC 无测试工程可引用」不成立 | 新增 `XUnitTest/DataScopeSensitiveMvcTests.cs`（4 例，CubeNC 侧列级标记与遮蔽）`4/4 通过` |
| N6 | 复审计 🔵 | `verify.md` 残留要求「单元测试须覆盖 SetMenu 覆盖/继承」全仓零用例 | 新增 `Common/DataScopeMenuOverrideTests.cs`（2 例）`2/2 通过` |
| N7 | 复审计 🔵 | WebAPI 详情脱敏仍在流程覆盖之前 | 后移到 `WorkflowPageOverlay.ApplyRow` 之后，与列表时序一致 |
| N8 | 复审计 🔵 | `GetPage` 中 `allList` 与 `addForm` 两条语句被合并到一行 | 拆回两行 |
| N11 | 复审计 🔵 | tasks/门禁测试数字过时（写 9/9、34、494/6） | 按实测更新为 35、13、4、2，MVC 全量实测 493/7（6×`QyWeiXinTests` 外网 + 1×`TryFetchRemoteAvatar` 抖动，单跑通过） |
| N12 | 复审计 🔵 | AC-12 字面与 XCode 实际退化语义不符 | 改为「本人恒可见 + 部门集合恒假，即仅本人一行」 |

**依旧保留的残留**：R1（`builder != null` 时行权退让，已同步文档）、R2（`GetPage.allList` 未标 sensitive）、R3（导出迭代器二次枚举 / 就地改写与单对象缓存邻接）、R4 前半（无真实行集用例，靠 T10 冒烟）、R5→已由 N5 消除、N9（Widget 无断言级用例）、N10（`IsSystemUser` 与 `DataScopeContext.IsSystem` 语义分歧，已在代码注释说明）。

#### 结论（轮次 2 后）

本轮 N1–N8 全部修复、N9–N12 已登记或修复；无未决 🔴。可进入 `验收 OSC-2608273d95`（T10 冒烟由验收方在真实实例执行）。