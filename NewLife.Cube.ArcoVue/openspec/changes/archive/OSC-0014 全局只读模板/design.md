# OSC-0014 Design — 全局模板（表单全局唯一）

## 1. 目标与契约边界

模板是一条受保护的 ViewProfile 记录，而不是个人 API 的特例：`UserId=0`、每个 `TypePath` 一条、只由系统管理员经专用 endpoint 管理。个人 endpoint 和 `UpsertForUser` 必须继续拒绝 `userId <= 0`，防止请求体篡改跨越边界。

本号对**视图/筛选两个独立域**（ViewsJson 含 insight、FiltersJson）做 `个人 > 模板 > 系统默认` 解析，域内采用整体选取，不做字段/数组/JSON patch 合并；**表单域（FormJson）不参与模板分层**——已按「全局唯一」模式落地（OSC-0013）：`UserId=0` 一份、仅管理员可写、所有用户共享读取、空壳删除，本号仅做回归与来源展示。系统种子是前端由 GetPage/既有默认 view 计算的临时值，不持久化为 UserId=0。

## 2. 文件级改动地图

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `NewLife.Cube/Entity/视图配置.Biz.cs` | 新增模板 find/upsert/delete 与 domain 验证、操作审计辅助 | `UpsertForUser`/`DeleteForUser` 对 userId<=0 的拒绝 |
| `NewLife.Cube/Controllers/CubeController.cs` | 新增独立 template GET/PUT/DELETE，系统管理员授权与日志 | `/Cube/ViewProfile` 个人 GET/PUT/POST/DELETE 路径与语义 |
| `NewLife.Cube/Entity/Models/ViewProfileModel.cs` | 表单域 FormJson 已由 OSC-0013 生成并消费；本号不新增字段 | 用户个人模型字段兼容 |
| `packages/api-core/src/api.ts`、`types.ts` | template API（视图/筛选域）、模板读取模型与错误处理 | 个人 API 的 PUT→POST 兼容 |
| `web/src/core/utils/viewProfile.ts` | 视图/筛选两域 presence、三层 resolve、来源和 materialize helper；表单域直接读全局 FormJson | 列/排序/表单布局各域的内部 schema |
| `web/src/stores/viewProfile.ts` | 同时维护 raw personal/raw template/resolved state（视图/筛选域）；按域首次 materialize；表单域沿用 setFormJson 全局提交 | 个人保存 debounce/失败回滚 |
| `web/src/views/crud/ViewTabsToolbar.vue` | 恢复视图域由视图菜单「恢复默认」承接（不显示独立来源徽标）；管理员模板管理入口 | 普通用户已有视图切换能力 |
| `web/src/views/crud/DefaultList.vue` | 消费 resolved state；视图/搜索域不显示独立来源徽标，恢复能力由既有菜单/搜索入口承接 | GetPage、CRUD、搜索/分页业务契约 |
| 表单布局配置组件 | 无改动：仅管理员可配置全局布局、所有用户共享读取（OSC-0013）；本号仅回归 | OSC-0013 全局唯一内部操作 |
| `NewLife.Cube/**Tests.cs`、`packages/api-core/**/*.spec.ts`、`web/src/**/*.spec.ts` | 授权、审计、继承、materialize 与 UI 测试 | 既有个人 profile 测试 |

## 3. API 与授权

建议固定在个人 API 邻近的专用路由（实施前核查当前 MVC attribute routing）：

| 方法 | 路径 | 授权 | 行为 |
| --- | --- | --- | --- |
| GET | `/Cube/ViewProfileTemplate?typePath=...` | 系统管理员 | 返回该 typePath 模板（视图/筛选域）或空响应 |
| PUT | `/Cube/ViewProfileTemplate` | 系统管理员 | 完整替换可支持的模板 JSON 域（视图/筛选） |
| POST | `/Cube/ViewProfileTemplate` | 系统管理员 | 仅作为 PUT 不可用时的兼容入口 |
| DELETE | `/Cube/ViewProfileTemplate?typePath=...` | 系统管理员 | 删除模板，回落系统种子 |

系统管理员判定采用已存在的 `CurrentUser.Roles.Any(e => e.IsSystem)` 模式；无 CurrentUser、非系统角色、空 typePath 均拒绝。服务端忽略并固定 `UserId=0`，不信任请求的 UserId。PUT 只接受 `TypePath`、`ViewsJson`、`FiltersJson`（以及模型中已批准的相关 JSON）；**不处理 FormJson**——表单域走 `/Cube/ViewProfile` 的全局唯一逻辑（OSC-0013：FormJson 非空时管理员写 `UserId=0` 全局记录，普通用户 403）。拒绝或忽略 UserId、Id、其他主体字段。每次成功发布/删除写入 Cube 既有操作日志，含 action、typePath、操作者与结果，不记录完整敏感 JSON 值。

## 4. 视图/筛选三层解析与表单全局唯一

定义域是否存在：合法 JSON 且该域含有实际配置内容即 present；`null`、空串、`{}`、空 ViewsJson/空 views map、空 FiltersJson views map 都是不存在（表单域无此判定——全局 FormJson 空壳即未配置）。

| 域 | 个人 present | 模板 present | 有效值 | 来源 |
| --- | ---: | ---: | --- | --- |
| ViewsJson | 是 | 任意 | 个人完整 ViewsJson | personal |
| ViewsJson | 否 | 是 | 模板完整 ViewsJson | template |
| ViewsJson | 否 | 否 | 系统默认 view | system |
| FiltersJson | 是 | 任意 | 个人完整 FiltersJson | personal |
| FiltersJson | 否 | 是 | 模板完整 FiltersJson | template |
| FiltersJson | 否 | 否 | 空过滤 | system |

**表单域（FormJson）**：不参与上述分层——全局唯一一份（OSC-0013），管理员可写、所有用户共享读取；空壳（无 add/edit/detail 任何模式）等价于未配置（回落系统默认布局）。

禁止跨域 merge：例如 personal 只存在 FiltersJson 时，ViewsJson 仍可继承模板；personal ViewsJson 即使只有一个 named view，也整体覆盖模板 ViewsJson。模板更新只影响来源为 template 的域；表单域全局更新即对所有用户生效。

## 5. 用户操作与 materialize

| 用户操作 | 当前来源 | 写入 | 结果 |
| --- | --- | --- | --- |
| 保存视图/洞察 | template/system | personal ViewsJson = 当前有效完整域 | 后续模板视图更新不影响该域 |
| 保存默认筛选 | template/system | personal FiltersJson = 当前有效完整域后仅替换当前 view 条目 | 其它域继续继承 |
| 保存表单布局（管理员） | global | `UserId=0` 全局 FormJson（OSC-0013 `SaveGlobalFormJson`） | 全局生效；普通用户无保存入口 |
| 恢复视图/筛选域 | personal | 删除个人记录中的该完整域；空个人记录可删除 | 回落 template 或 system |
| 恢复表单布局（管理员） | global | 空壳 FormJson → 删除全局记录（OSC-0013） | 回落系统默认布局 |
| 管理员发布模板 | 任意 | UserId=0 完整指定域（视图/筛选） | 不改任何 UserId>0 记录 |
| 管理员删除模板 | 任意 | 删除 UserId=0 记录 | 未个人化域回落 system |

模板管理界面仅系统管理员可见；视图域恢复由视图菜单「恢复默认」承接（删除个人视图副本、回落模板/系统默认），**不显示独立来源徽标**；表单布局入口保持「仅管理员可配置全局布局」（OSC-0013），普通用户只读共享。配置入口应沿用 OSC-0012/0013 UI，不能给普通用户模板编辑按钮。窄屏 `<768px` 模板管理抽屉全宽；无模板显示空态；加载失败不覆盖已解析个人状态。

## 6. 一致性与并发

本号不引入 Revision 或多人协作锁。管理员发布采用既有最后成功保存语义；接口返回最终模板。前端保存 template 失败必须回滚本地 template 状态且提示错误。个人保存只更新个人域，不把 resolved 值误写回所有域。若一次操作需同时变更多域，必须显式带出这些域，不得因序列化遗漏未来未知 JSON 属性。

## 7. 适用框架与官方资料

- 管理抽屉、状态标签、确认对话框与权限禁用态：[Arco Design Vue](https://arco.design/vue/docs/start)。
- 多维数据视图不新增功能；若需适配，先查 VisActor VTable [配置](https://visactor.com/vtable/option/ListTable)与[实例接口](https://visactor.com/vtable/api/Methods)。

## 8. 核心文档影响

| 文档路径 | 影响 | 说明 |
| --- | --- | --- |
| `Doc/Api/核心接口架构.md` | 修改 | Template API、授权与优先级 |
| `Doc/附录B_API参考.md` | 修改 | 三个模板 endpoint 与请求/错误语义 |
| `Doc/附录C_实体参考.md` | 修改 | UserId=0 的受控模板语义 |
| `Doc/功能清单.md` | 修改 | 权限、模板与 SPA 实现状态 |
| `Doc/Api/ArcoVue企业中后台迁移方案.md` | 修改 | OSC-0014 里程碑与范围 |
| `NewLife.Cube.ArcoVue/web/README.md` | 修改 | 管理员模板/个人覆盖说明 |

## 9. 测试设计与风险

| 目标/风险 | 证据/缓解 |
| --- | --- |
| 越权 UserId=0 写入 | XUnit：个人 endpoint/ Biz 拒绝；普通用户 template endpoint 403/错误；表单全局唯一 403 回归（OSC-0013） |
| 视图/筛选两域误合并 | resolver 测试九格矩阵、各域独立 materialize/restore |
| 模板覆盖个人 | 发布模板后 personal 域字节级保持、有效值不变 |
| 表单全局唯一 | 表单布局管理员更新即全局生效、普通用户无写入口、空壳删除（OSC-0013 回归） |
| 审计缺失/泄密 | XUnit/mock 验证成功 action；日志不含完整 JSON |
| 模板不存在/损坏 | 回落 system，个人配置仍可用 |
| API 兼容 | api-core template URL/PUT→POST/错误测试；个人 API 回归 |
