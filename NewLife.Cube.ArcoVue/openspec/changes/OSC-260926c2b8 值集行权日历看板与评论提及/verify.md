# Verify

验收命令（在对应项目目录执行，新增测试全过，构建无错误）：

- `dotnet test` 中筛选 `LovEntityGuardTests`
- `web` 目录 `npx vitest run src/features/views/calendarDay.spec.ts src/features/views/kanbanCollapse.spec.ts src/features/views/kanbanMove.spec.ts src/views/crud/commentMention.spec.ts`
- `dotnet build`（NewLife.Cube）
- `npx vue-tsc --noEmit`（web）

## AC

- [x] AC1（控制器级自动化已锁）无目标实体 Detail 的用户调用 `entity:` ListData，响应 403，body 不含行；无 Detail 时 `LovEntityGuard.CheckMenu=false` 已由单测锁定。（`LovControllerGuardTests.ListData_WithoutDetail_Returns403` 直接驱动控制器验证 403 + JsonResult；浏览器端到端仍随 T14 冒烟复核）
- [x] AC2 行权范围外的主键，BatchLabel 字典没有该键；范围内的主键有 label。（`LovControllerGuardTests.BatchLabel_OmitsInvisibleKeys` 控制器级验证省略与保留；省略判定 `LovEntityGuard.CanAccess` 单测锁定；端到端仍随 T14 冒烟复核）
- [x] AC3 `Enum.*` BatchLabel 仍返回枚举文案。（枚举分支未改动；`LovRegistryTests`/`LovStoreTests`/`LovAutoRegisterInferenceTests` 33/33 通过）
- [x] AC4 租户 Enforce 且无租户上下文时，`entity:` 列表为空。（`LovEntityGuardTests.Tenant_Enforce_NoContext_FailClosed` 单测）
- [ ] AC5 日历某日 4 条时可见 `+1`；点击后弹出层里有 4 条；点一条打开详情。
- [ ] AC6 有 Insert 时点本月空白日，右侧新增抽屉打开，开始日期字段等于该日。
- [ ] AC7 无 Insert 时点空白日，抽屉不打开，网络无新增请求。
- [ ] AC8 点非本月格子不打开新增抽屉。
- [ ] AC9 看板列头点击后卡片隐藏、条数仍在；刷新页面后已折叠的列仍然收起。拖动中的卡片透明度为 0.4。
- [ ] AC10 工作台迷你看板无折叠钮。
- [ ] AC11 讨论选两名用户发送，请求 JSON 含 `mentionUserIds` 长度 2，且含对应 `@显示名`。
- [ ] AC12 第 21 人无法加入；自己不会出现在 `mentionUserIds`。
- [ ] AC13 不传提及时，评论 POST 与今天一样成功。
- [x] AC14 同列放下不写库；必填分组字段不能放到「未分组」；布尔列提交 true/false；无编辑权或审批中且字段不可写时卡片不可拖。（`kanbanMove.spec.ts`）
- [x] AC15 有编辑权时把卡片拖到另一列，请求为 PATCH，body 只含分组字段；失败后卡片回到原列。（2026-09-29 部门看板：上海分公司 Id=5 从「公司」拖到「部门」，`PATCH /api/Admin/Department` body 为 `{"id":"5","values":{"Type":"2"}}`，`ok=1`；再拖回「公司」`Type=1`，列计数恢复 2/4/0/0。修复前两次失败（id 为数字、枚举字符串无法转换）卡片都留在原列。）

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

## 收尾门禁记录（2026-09-27）

- 代码审查：0🔴 / 4🟡 / 13🟢（「可放行」）；🟡1/🟡2/🟡4 已修，🟡3（`GetTenantField` 自定义实现防御）记入 T17 后续项。
- 实现审计：A/B/C 三条线全落实；缺口 G1–G4 已修（G1 `FetchRemoteList` 菜单裁决、G2 `BatchLabel` 批量化、G3 看板重建、G4 见 T16），G5 已补控制器级测试（`LovControllerGuardTests`）。
- 测试隔离备忘：G5 测试类使用独立连接名 `LovCtrlGuard` + 线程级重映射 `Menu/Role/User/Parameter.Meta.ConnName`，并在夹具结束时移除连接、还原映射；禁止占用全局 `Membership`/`Cube` 连接名（否则 `TenantAuthFixture` 的 `TryAdd(Cube→Membership)` 会静默失效，HEAD 基线组合 `TenantAuthRepro+Osc260813397e` 本身就存在该隔离冲突）。
- 基线对照：`TenantAuthRepro + Osc260813397e` 组合在 HEAD worktree 同样失败（存量），非本号引入。

## 必须保留

- 日历仍只有月网格，无拖改日期。甘特不做拖拽写回。
- 看板只写回分组字段，不改列内顺序。
- `SendMentionNotification` 不改。
- `DataScopeContext.Current` 不被本号写入。
