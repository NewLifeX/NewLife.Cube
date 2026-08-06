# OSC-0014 Retro

## 结果摘要

- 状态：已完成（验收通过）。
- 计划：系统管理员维护全局模板——视图/筛选域按 `个人 > 模板 > 系统默认` 独立继承；表单域全局唯一（OSC-0013）仅回归。
- 不做：角色/租户模板、协同编辑、审批、版本历史、字段级 patch 与跨实体模板；不为表单域引入个人可覆盖模板。

## 实际结果

- **代码范围**：
  - 后端：`视图配置.Biz.cs`（`SaveGlobalTemplate`/`DeleteGlobalTemplate`/`IsEmptyTemplateJson`，固定 UserId=0，与 `SaveGlobalFormJson` 共存于同记录）、`CubeController.cs`（`ViewProfileTemplate` GET/PUT/POST/DELETE，`Roles.Any(e => e.IsSystem)` 授权、`WriteLog` 审计）、`CubeController.cs` ViewProfile PUT（FormJson 仅管理员写全局）、双栈（Cube/CubeNC）一致。
  - 前端：`api-core`（`getViewProfileTemplate`/`putViewProfileTemplate`/`deleteViewProfileTemplate`，PUT→POST fallback）、`viewProfile.ts`（`hasViewsDomain`/`hasFiltersDomain` 判定、`ViewDomainSource`/`FilterDomainSource` 类型）、`stores/viewProfile.ts`（并行拉取个人+模板、按域 overall select、`carryViews`/`carryFilters` materialize、`restoreViewDomain`/`restoreFilterDomain`、失败 rollback）、`TemplateManageDrawer.vue`（管理员专用，仅 isAdmin 可见）、`DefaultList.vue`（isAdmin 条件渲染"管理模板"入口、"恢复默认"调用 `restoreViewDomain`）。
- **偏差**：无架构偏差。前端 `isAdmin` 使用 `roleName === '管理员'` 与后端 `Roles.Any(e => e.IsSystem)` 语义不完全对齐——安全关键路径在后端 403，前端仅 UI 可见性控制。
- **测试证据**：XUnit 13（含 `ViewProfile_GlobalTemplate_Lifecycle` 与 `ViewProfile_GlobalFormJson_Delete` 模板+表单共存与个人隔离）；api-core 11（含 template API URL/PUT→POST/DELETE）；web 219（含九格继承矩阵/materialize/恢复/round-trip）；4 处构建 0 错误。
- **API 冒烟**：22/22 通过（管理员模板 CRUD、普通用户/匿名越权拒绝、个人化 + body.userId=0 忽略、FormJson 管理员写+普通用户读共享+普通用户写拒、删除回落、审计含记录不泄 JSON、清理无残留）。
- **文档**：`核心接口架构.md`、`附录C_实体参考.md`、`功能清单.md`、`迁移方案.md`、`web/README.md` 已同步。

## 经验沉淀候选

- 将受控模板与个人 API 分离，避免 UserId=0 成为可伪造主体。
- 继承按配置域整体选取，可避免 JSON patch 和字段级合并的不可预测性。
- 全局唯一表单布局与两层模板共享同一 `UserId=0` 记录：删除模板保留表单布局、删除表单布局保留模板，共存收口逻辑需维护 `hasContent` 判断。
- 前端管理员判定应与后端权限语义对齐（`roleName` 字符串 vs `IsSystem` 角色属性），避免跨部署环境不一致导致管理员界面不显示。
