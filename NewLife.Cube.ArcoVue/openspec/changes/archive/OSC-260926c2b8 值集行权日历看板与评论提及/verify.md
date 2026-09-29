# Verify

验收命令（在对应项目目录执行，新增测试全过，构建无错误）：

- `dotnet test` 中筛选 `LovEntityGuardTests`
- `web` 目录 `npx vitest run src/core/utils/viewMapping.spec.ts src/features/views/calendarDay.spec.ts src/features/views/kanbanCollapse.spec.ts src/features/views/kanbanMove.spec.ts src/views/crud/commentMention.spec.ts`
- `dotnet build`（NewLife.Cube）
- `npx vue-tsc --noEmit`（web）

## AC

- [x] AC1（控制器级自动化已锁）无目标实体 Detail 的用户调用 `entity:` ListData，响应 403，body 不含行；无 Detail 时 `LovEntityGuard.CheckMenu=false` 已由单测锁定。（`LovControllerGuardTests.ListData_WithoutDetail_Returns403` 直接驱动控制器验证 403 + JsonResult；2026-09-29 端到端复核：受限账号（普通用户+值集管理查看）`POST /api/Admin/Lov/ListData {lovCode:'Entity.User'}` → 403 `无权访问[User]值集数据`；同码 admin → 200 六行）
- [x] AC2 行权范围外的主键，BatchLabel 字典没有该键；范围内的主键有 label。（`LovControllerGuardTests.BatchLabel_OmitsInvisibleKeys` 控制器级验证省略与保留；省略判定 `LovEntityGuard.CanAccess` 单测锁定；2026-09-29 端到端复核：受限账号 `POST /api/Admin/Lov/BatchLabel {lovCode:'Entity.User', values:['1','2']}` → 200 `data:{}` 键全部省略，不 500）
- [x] AC3 `Enum.*` BatchLabel 仍返回枚举文案。（枚举分支未改动；`LovRegistryTests`/`LovStoreTests`/`LovAutoRegisterInferenceTests` 33/33 通过）
- [x] AC4 租户 Enforce 且无租户上下文时，`entity:` 列表为空。（`LovEntityGuardTests.Tenant_Enforce_NoContext_FailClosed` 单测）
- [x] AC5 日历某日 4 条时可见 `+1`；点击后弹出层里有 4 条；点一条打开详情。（2026-09-29 审计日志日历：25 日 3 条事件 + `+289`；点 `+289` 弹层共 292 条按钮；点首条打开详情抽屉（编号 7509…））
- [x] AC6 有 Insert 时点本月空白日，右侧新增抽屉打开，开始日期字段等于该日。（2026-09-29 实测：点 25 日开新增抽屉，表单模型 `LastLogin=2026-09-25T00:00:00`（datetime 壁钟、无时区后缀）；「用户」视图开始字段为『最后登录』不在新增表单，预填值界面不可见——机制正确，属演示配置约束（实体无表单内 DateTime 字段可换），见「验收记录」）
- [ ] AC7 无 Insert 时点空白日，抽屉不打开，网络无新增请求。（记录：未构造『有日历入口但无新增权』账号；列/格可点门禁由 `canCreateOnCalendarCell` 单测锁定）
- [x] AC8 点非本月格子不打开新增抽屉。（2026-09-29 实测：点 10 月 1 日尾格无抽屉、无请求）
- [x] AC9 看板列头点击后卡片隐藏、条数仍在；折叠写入当前视图配置，刷新后仍收起。拖动中的卡片为完全不透明。（2026-09-29 实测：部门列头点击后 `aria-expanded=false`、卡片隐藏、计数 3 保留；硬刷新后仍收起；已恢复展开。拖动不透明：`opacity:1` + 阴影（CSS，前轮已验））
- [ ] AC10 工作台迷你看板无折叠钮。（记录：本轮未复测；compact 分支不折叠由实现与既有测试保证）
- [x] AC11 讨论选两名用户发送，请求 JSON 含 `mentionUserIds` 长度 2，且含对应 `@显示名`。（2026-09-29 实测：`POST /Cube/EntityComment` body `mentionUserIds:[5,4]` 长度 2，content 含 `@ABCDSW @smokeuser3!!!`；评论列表出现新评论）
- [ ] AC12 第 21 人无法加入；自己不会出现在 `mentionUserIds`。（记录：单测锁定 `commentMention.spec`：去自己/去重/≤20；`addMentionEntry` 对 self 为 no-op；UI 候选列表含自己但选择无效果）
- [x] AC13 不传提及时，评论 POST 与今天一样成功。（2026-09-29 冒烟：无提及时评论 POST 成功（`code:0`）；提及路径 payload 另由单测锁定）
- [x] AC14 同列放下不写库；必填分组字段不能放到「未分组」；布尔列提交 true/false；无编辑权或审批中且字段不可写时卡片不可拖。（`kanbanMove.spec.ts`）
- [x] AC15 有编辑权时把卡片拖到另一列，请求为 PATCH，body 只含分组字段；失败后卡片回到原列。（2026-09-29 部门看板：上海分公司 Id=5 从「公司」拖到「部门」，`PATCH /api/Admin/Department` body 为 `{"id":"5","values":{"Type":"2"}}`，`ok=1`；再拖回「公司」`Type=1`，列计数恢复 2/4/0/0。修复前两次失败（id 为数字、枚举字符串无法转换）卡片都留在原列。）
- [x] AC16 日历月导航位于工具栏「添加记录」右侧，上一月/下一月/今天可用；日历底部提示固定「一次最多加载 1000 条」。（`calendarDay.spec` 游标位移单测；浏览器复核随 T14）
- [x] AC17 看板底部显示分页器并可翻页，不再出现「一次最多加载」提示。（`viewMapping.spec` 锁定看板走分页尺寸；浏览器复核随 T14）
- [x] AC18 日历可切换 日/周/月；周/日按小时定位事件、重叠并列、今天高亮与当前时刻线；「今天 / ‹ / › / 标题」导航按模式位移。（`calendarDay.spec`；浏览器复核随 T14）
- [x] AC19 日/周模式点击列内空白按点击位置取整点打开新增抽屉（datetime 字段写 `THH:00:00`）；点事件块不触发；无新增权/未配 startField 时列不可点；导航位于「关键字」前。（`calendarDay.spec`；浏览器复核随 T14）
- [x] AC20 周视图列序从周一开头、周日最后；周标题为周一到周日范围；月网格表头与补齐一致。（`calendarDay.spec`）
- [x] AC21 记录抽屉（新增/编辑/详情/只读实体分支）内容超过可视区域时可垂直滚动；标签栏固定不回滚；历史/评论 Tab 可滚。（CSS 修复，`RecordDrawer.vue`；浏览器复核随 T14）
- [x] AC22 讨论页签输入框在未聚焦时可见：输入区白卡承载，默认输入框样式与编辑表单观感一致。（CSS 修复，`RecordDrawer.vue`；浏览器复核随 T14）

## 执行记录（2026-09-27 执行阶段）

| 项 | 命令 | 结果 |
| --- | --- | --- |
| 后端构建（API/MVC） | `dotnet build NewLife.Cube -f net10.0` | ✅ 0 错误 |
| 后端构建（NC 合并版） | `dotnet build NewLife.CubeNC -f net10.0` | ✅ 0 错误 |
| 新增单测（值集守卫） | `dotnet test XUnitTest --filter LovEntityGuardTests` | ✅ 15/15 |
| 租户/行权回归 | Cube.Tests 筛选 `Tenant*|DataScope*|CubeDataScope*` | ✅ 48/48 |
| 值集既有测试（枚举翻译） | Cube.Tests 筛选 `LovRegistryTests|LovStoreTests|LovAutoRegisterInferenceTests` | ✅ 33/33 |
| 新增前端单测 | `vitest run calendarDay.spec / kanbanCollapse.spec / commentMention.spec` | ✅ 23/23（6+3+14） |
| 前端全量 | `npm run test` | 983 通过 / 4 失败（均为 sfcThin 存量：FilterBuilderPanel / RecordDrawer / WfInstanceGraph / WorkflowTaskActions，HEAD 基线同样失败） |
| 前端类型 | `npx vue-tsc -b` | ✅ 退出码 0 |
| 后端全量对照 | Cube.Tests 全量（工作树 vs HEAD 临时 worktree 基线） | 工作树 72 失败 < 基线 148 失败；存量测试隔离问题非本号引入；与本号直接相关筛选回归 48/48 全过 |
| 收尾修复后回归 | `dotnet test XUnitTest --filter LovEntityGuardTests` + Cube.Tests 筛选（Tenant*/DataScope*/CubeDataScope*/LovRegistry*/LovStore*） | ✅ 15/15 + 62/62 |
| G5 控制器级补测试 | `dotnet test NewLife.Cube.Tests --filter LovControllerGuardTests` | ✅ 7/7（列表 403 / 行权过滤 / Enforce 空集 / 省略与非法键 / Enum 翻译）；Lov 系列合计 44/44 |
| 手工冒烟（T14） | — | ⏳ 留待 `验收 OSC-260926c2b8`（AC1/AC2 端到端、AC5–AC13） |

> 备忘：`packages/api-core` 改动后需 `npm run build` 重建 dist（web 的 vue-tsc 引用 dist 类型）。

## 执行记录（2026-09-29 看板跨列写回）

| 项 | 命令 / 操作 | 结果 |
| --- | --- | --- |
| 拖放纯函数 | `vitest run kanbanMove.spec.ts kanbanCollapse.spec.ts` | ✅ 8/8 |
| 主键按字符串提交 | `vitest run src/api.spec.ts -t patchFields` | ✅ 2/2 |
| 枚举列 key | `dotnet test NewLife.Cube.Tests --filter PatchFields_JsonElementString\|ChangeTypeValue_EnumKey` | ✅ 2/2 |
| 浏览器 | 部门看板跨列拖放、拖回、按钮不发起、折叠列不接收、同列不写库 | ✅ 见 AC15；数据已拖回原列 |

## 执行记录（2026-09-29 月导航与分页收口）

| 项 | 命令 / 操作 | 结果 |
| --- | --- | --- |
| 纯函数与门禁 | `vitest run viewMapping.spec.ts calendarDay.spec.ts kanbanCollapse.spec.ts kanbanMove.spec.ts` | ✅ 61/61（46+7+3+5） |
| 前端类型 | `npx vue-tsc --noEmit` | ✅ 退出码 0 |
| 前端全量 | `npx vitest run` | 996 通过 / 4 失败（仍为 sfcThin 四处存量，见 2026-09-27 记录，非本号引入） |

## 执行记录（2026-09-29 日历 日/周/月）

| 项 | 命令 / 操作 | 结果 |
| --- | --- | --- |
| 日历模式纯函数 | `vitest run src/features/views/calendarDay.spec.ts` | ✅ 14/14（模式位移/周起始（周一）/标题/时间轴布局/空白取整点） |
| 前端类型 | `npx vue-tsc --noEmit` | ✅ 退出码 0 |
| 前端全量 | `npx vitest run` | 1003 通过 / 4 失败（仍为 sfcThin 四处存量，非本号引入） |

## 执行记录（2026-09-29 抽屉滚动与讨论输入修复）

| 项 | 命令 / 操作 | 结果 |
| --- | --- | --- |
| 前端类型 | `npx vue-tsc --noEmit` | ✅ 退出码 0 |
| 前端全量 | `npx vitest run` | 1004 通过 / 4 失败（仍为 sfcThin 四处存量，非本号引入） |
| 结构复核 | 回读 `RecordDrawer.vue` 样式区 | ✅ `.detail-field` 声明归位；`.detail-image/.detail-json` 移至 `.detail-field__value` 前；`.comment-box` 白卡化 |
| 同类扫描 | `^\}\s+[a-z-]+\s*:`（web/src 悬挂声明） | ✅ 无其余同类拼坏（OSC-0009 仅此一处） |

## 验收记录（2026-09-29，Validating）

**三步编排**：

- **实现审计**：愿景 4 目标逐条对照代码与实机——A 值集（`LovEntityGuard.CheckMenu` 403 / `AppendEntityLabels` 省略键；admin 200+6 行 / 受限 403+`{}`）；B 日历（`+N` 弹层 292 条、空白日预填模型值、日/周/月 时间轴、周一起始、导航位于「关键字」前、底部 1000 提示）与看板（折叠写入 mapping 刷新仍在、分页器、拖动不透明）；C 提及（`buildMentionIds` 去自己去重≤20；POST `mentionUserIds:[5,4]`+`@显示名`）。无缺口。
- **代码审查**：0🔴；观察项：`useViewConfigDrawer` 同分组才保留 collapsedColumns；`resolveViewPageSize` 看板走分页尺寸（存储偏好可 1000）；`RecordDrawer` 滚动链三分支核对。sfcThin 4 处存量为 HEAD 基线（含 RecordDrawer），非本号新增。
- **文档同步**：迁移方案（09-27b…09-29e 五条版本行 + §7.4/§10.4 #17/#18/#23 + BE-D2/D3）、竞品报告（v1.9…v1.13 + 行更新）、`Doc/功能清单.md`（SPA-19 + 视图行续项）、本号 `ui/information-architecture.md`（补记 日/周/月、导航位置、周一起始、看板分页器）均一致。

**验收补齐（用户决策：先补齐再复盘）**：

1. 值集越权端到端复核：构造受限账号 osclovverify（普通用户，仅补授「值集管理→查看」；账号已删除、授权已回滚）。`POST /api/Admin/Lov/ListData {lovCode:'Entity.User'}` → **403 `无权访问[User]值集数据`**；`BatchLabel` → **200 `data:{}` 键全省略**；同码 admin → 200 六行。值集码格式：`Entity.{Type.FullName}` 或短名（`Entity.User`）。
2. 日历空白日预填可见性：机制正确（模型 `LastLogin=2026-09-25T00:00:00`）；「用户」实体无表单内 DateTime 字段（候选字段为最后登录/在线时间，均不在新增表单），视图配置无更优项可换——属演示配置约束，非代码缺口，登记说明。

**门禁记录**：

| 项 | 命令 / 操作 | 结果 |
| --- | --- | --- |
| 前端聚焦 | `vitest run calendarDay/kanbanCollapse/kanbanMove/commentMention/viewMapping` | ✅ 82/82 |
| 前端类型 | `npx vue-tsc --noEmit` | ✅ 0 |
| 前端构建 | `npx vite build` | ✅ 0（10286 modules） |
| 前端全量 | `npx vitest run` | 1004 通过 / 4 失败（sfcThin 存量） |
| 后端构建 | `dotnet build NewLife.Cube -f net10.0` | ✅ 0 错误（3 存量警告） |
| 值集单测 | `dotnet test XUnitTest --filter LovEntityGuardTests` | ✅ 15/15 |
| 控制器级 | `dotnet test NewLife.Cube.Tests --filter LovControllerGuardTests` | ✅ 7/7 |
| 浏览器冒烟 | `+N`/空白日/日周月/折叠刷新/提及/抽屉滚动/评论框 | ✅ 见 AC5–AC22（AC7/AC10/AC12 见备注） |

**未复测项（记录）**：AC7（无 Insert 用户 UI 负例，单测锁定）；AC10（工作台迷你看板，compact 不折叠由实现保证）；AC12（self/第 21 人，单测锁定）。**冒烟数据遗留**：admin 用户记录下两条冒烟评论（“验收冒烟…”与探测“x”）；测试账号已删除。

**共享环境观察**：页面级 pageSize（如部门/菜单页 1000 条/页）与关键字为演示账号已存偏好，非代码行为；并发会话共享同一演示实例（后端 5000 + Vite 5183）。

## 收尾门禁记录（2026-09-27）

- 代码审查：0🔴 / 4🟡 / 13🟢（「可放行」）；🟡1/🟡2/🟡4 已修，🟡3（`GetTenantField` 自定义实现防御）记入 T17 后续项。
- 实现审计：A/B/C 三条线全落实；缺口 G1–G4 已修（G1 `FetchRemoteList` 菜单裁决、G2 `BatchLabel` 批量化、G3 看板重建、G4 见 T16），G5 已补控制器级测试（`LovControllerGuardTests`）。
- 测试隔离备忘：G5 测试类使用独立连接名 `LovCtrlGuard` + 线程级重映射 `Menu/Role/User/Parameter.Meta.ConnName`，并在夹具结束时移除连接、还原映射；禁止占用全局 `Membership`/`Cube` 连接名（否则 `TenantAuthFixture` 的 `TryAdd(Cube→Membership)` 会静默失效，HEAD 基线组合 `TenantAuthRepro+Osc260813397e` 本身就存在该隔离冲突）。
- 基线对照：`TenantAuthRepro + Osc260813397e` 组合在 HEAD worktree 同样失败（存量），非本号引入。

## 必须保留

- 日历不做拖改日期（日/周/月三模式为 2026-09-29 续补，只读展示 + 空白新建，不改变此约束）。甘特不做拖拽写回。
- 看板只写回分组字段，不改列内顺序。
- `SendMentionNotification` 不改。
- `DataScopeContext.Current` 不被本号写入。
