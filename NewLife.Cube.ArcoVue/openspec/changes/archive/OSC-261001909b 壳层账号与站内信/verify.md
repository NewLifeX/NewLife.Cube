# OSC-261001909b Verify — 壳层账号与站内信

## AC-1 激活

- [x] AC1.1 `/activate?token=a&account=b@c.com` 发起 GET Activate。成功后到 `/login`。 ✅ 路由 mock 捕获 `GET /Auth/Activate`；成功提示后 1s 跳 `/login` 实测。真实邮件 token 未复现（演示无邮件链路，仅记录）。
- [x] AC1.2 无 token 时显示渠道、账号、验证码。账号或验证码为空不能提交。 ✅ 表单实测：渠道 mail/sms、账号、验证码；两者都填才可提交（禁用↔启用两段实测）。
- [x] AC1.3 未登录可打开该页。 ✅ 匿名上下文直达 `/activate` 渲染。

## AC-2 注销

- [x] AC2.1 安全页签有「注销账号」。输入与 `userInfo.name` 不一致时不能确定。 ✅ 空输入禁用 → 错名禁用 → 一致启用（三段实测）。
- [x] AC2.2 成功后本地无 token，位于 `/login`。失败时仍停留在账号中心且 token 还在。 ✅ 真实端到端（一次性账号）：CloseAccount `code=0「账号已注销」` → 跳 `/login` 且 `cube.refreshToken`/`cube.tokenUserName` 已清；再登录被服务器拒绝（响应 `code:-1「账号 tmp909b35696 被禁用！」`）；失败分支由 catch 保留会话（代码核验）。
- [x] AC2.3 `userInfo.name` 为空时按钮禁用。 ✅ 代码核验（`canOpen` 守卫 + 提示）；弹窗空输入禁用路径已实测。

## AC-3 站内信

- [x] AC3.1 Inbox 项含 `target`。 ✅ 真实 `/Cube/Automation/Inbox` 返回项键含 `target`（历史记录值 null 属预期）。
- [x] AC3.2 `Admin/User#12` 打开 `/Admin/User?id=12` 并弹出该记录详情。 ✅ mock `Admin/User#2` → URL `/Admin/User?id=2`、`/api/Admin/User/Detail?id=2`、`.record-drawer` 打开（截图）；关闭后不自动重开。
- [x] AC3.3 `Admin/User#0`、三段路径、带 `..` 的字符串只标已读，不跳转。 ✅ `Admin/User#0` → URL 不变、抽屉保持、条目已读（Read `{id:9002}`）；解析单测 3 用例。
- [x] AC3.4 流程通知在主体有 TypePath 与 EntityKey 时 Target 为二者用 `#` 连接；缺一则 Target 为空。 ✅ `BuildNotifyTarget` 单测：空/缺一 → null；完整 → `Admin/User#9`。
- [x] AC3.5 抽屉为双层时间轴：外层分组（今天/本周/本月/更长时间，从近到远、空桶不渲染、节点带图标）；组内每条消息为时间轴节点；条目内容无竖线。 ✅ 实测分组 `[今天,本周,更长时间]`（空桶不渲染）、图标取主题色、组内 4 节点、条目无竖线（截图）。

## AC-4 租户成员

- [x] AC4.1 租户列表有「成员」，地址为 `/Admin/TenantUser?tenantId=` 加该行 Id，点击是导航不是 POST。 ✅（元数据级）真实 GetPage 含 `Members/成员`，Url `/Admin/TenantUser?tenantId={Id}`、无 DataAction；后端单测通过。演示未启用多租户菜单（MenuTree 无 Tenant）→ `/Admin/Tenant` UI 不可达，仅记录。
- [x] AC4.2 依赖 OSC-2610011ff2：`/Admin/TenantUser` 已注册时页面是通用列表。本号单独验收时若路由 404，记为阻塞于 1ff2，不改成员 Url。 ✅ 直达 `/Admin/TenantUser?tenantId=1` 渲染通用列表（租户用户/默认列表/添加记录/暂无数据/共 0 条），列表 API 200、VTable canvas 渲染。

## 命令

```powershell
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/core/utils/inboxTarget.spec.ts src/core/utils/closeAccount.spec.ts
pnpm exec vue-tsc -b
dotnet test "NewLife.Cube.Tests/NewLife.Cube.Tests.csproj" --filter Osc261001909b
dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0
```

预期：spec 与过滤测试通过；vue-tsc 与 build 无错误。

## 执行阶段记录（openspec-apply，2026-10-01）

- Vitest：`inboxTarget.spec.ts` 3、`closeAccount.spec.ts` 1，全过。
- XUnit：`Osc261001909b` 通过 2。
- `vue-tsc -b` 0 错误；`dotnet build NewLife.Cube -f net10.0` 0 错误。
- 代码审查：首轮 1 🔴（注销未清 Pinia）已修为 `userStore.logout()`；无剩余 🔴。
- 实现审计：对照 proposal/design/tasks 无实现缺口（菜单搜索按范围不做）。

## 验收编排（openspec-verify，2026-10-04）

| 环节 | 结论 |
| --- | --- |
| 实现审计 | 对照 proposal 目标 / design §1 文件地图 / tasks T1–T4 与 T2-6，无实现缺口 |
| 代码审查 | 复核 909b 提交 `da0f6432` 全量 diff：无遗留调试代码；纯函数与守卫均有 spec 覆盖 |
| 文档同步 | design §1 文件地图补 3 行（inboxBucket、iconRegistry/iconComponents、useInboxDrawer 描述）；`ui/information-architecture.md` 站内信补分桶双层时间轴；proposal §4 补第 5 条；`Doc/功能清单.md` SPA-7 追加 OSC-261001909b 注解 |

### 目标愿景对照（proposal §目标）

| 目标 | 达成 | 证据 |
| --- | --- | --- |
| 公开激活页与注销确认 | ✅ | AC1.1–1.3、AC2.1；AC2.2 真实注销（一次性账号） |
| 站内信 target 跳转与已读 | ✅ | AC3.1–3.4；真实 Inbox 响应含 `target` 键 |
| 租户列表「成员」 | ✅ | AC4.1 元数据 + 单测（演示无租户菜单，UI 不可达） |
| 纯函数与投影测试 | ✅ | 前端 inboxTarget 3 + closeAccount 1 + inboxBucket 7；后端 2 |

### 缺口/环境限制清单（用户决定：仅补齐 AC2.2）

| 项 | 类型 | 处置 |
| --- | --- | --- |
| AC2.2 真实注销 | 销毁性操作 | **用户选择补齐**：一次性账号端到端实测（见执行记录） |
| AC1.1 真实邮件 token | 环境限制（演示无邮件链路） | 仅记录（页面逻辑 mock 验证 + 跳转实测） |
| AC2.3 空用户名禁用 | 状态不可构造 | 代码核验 + 弹窗空/错名禁用路径实测 |
| AC4.1 租户页 UI | 环境限制（未启用多租户菜单） | 仅记录；真实 GetPage 元数据 + 后端单测替代取证 |

## 执行记录（openspec-verify，2026-10-04）

- 门禁：`vitest run` 全量 121 文件 /1096 例中 1092 过；4 例失败为存量 `sfcThin`（FilterBuilderPanel/RecordDrawer/WfInstanceGraph/WorkflowTaskActions，与 HEAD 基线一致，非本号引入）。本号涉及 inboxTarget 3、closeAccount 1、inboxBucket 7 等均全过。`vue-tsc -b` 0 错误；XUnit `Osc261001909b` 2/2；`dotnet build -f net10.0` 0 错误。
- 浏览器验收（Playwright，dev 5184，截图见运行产物）：激活页（mock 链路）、注销弹窗三段、站内信跳转详情、`Admin/User#0` 已读、分桶时间轴、TenantUser 通用列表。
- AC2.2 真实注销（一次性账号 `tmp909b*`）：创建 → 登录（验证码自动识别）→ 安全页签注销（空/错名禁用、一致启用）→ CloseAccount `{code:0,"账号已注销"}` → 跳 `/login` 且 refresh/userName 已清 → 再登录被拒（响应文案「账号…被禁用！」；拒绝文案与注册未激活路径的「账号未激活」不同名，属禁用校验路径）→ 管理员接口核验 `enable=false` 且昵称已脱敏。观察：注销后 `Detail.online` 快照短暂为 true（在线记录按心跳超时释放，令牌已吊销，接口不可再用）。
- 环境说明：演示后端登录风控（`MaxLoginError=5`、`LoginForbiddenTime=600s`）对本地回环 IP 生效，验收脚本重试时曾触发限流；最终通过「每次仅提交识别稳定的验证码」策略完成。临时账号 `tmp909b*`（id 8–14）已在验收后全部删除。
