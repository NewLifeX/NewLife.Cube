# OSC-261001909b — 壳层账号与站内信

## 1. 目标愿景

未激活账号能打开激活页；已登录用户能注销自己的账号；站内信在能解析关联对象时打开对应记录；租户行能进入该租户的成员列表。

- 目标 1：公开页 `/activate` 支持邮件链接 `?token=&account=` 直激活，也支持邮箱/短信验证码激活。成功后进入登录页。
- 目标 2：账号安全页提供「注销账号」。须在确认框输入当前用户名且一致后才 `POST /Auth/CloseAccount`。成功后清除会话并到 `/login`。
- 目标 3：站内信点击仍先标已读。`target` 能解析为 `{Area}/{Controller}#{数字主键}` 时再打开该实体并弹出详情；解析失败则只标已读。
- 目标 4：租户列表有「成员」链接，打开 `/Admin/TenantUser?tenantId=`。

## 2. 为何做

Cube.Vue 有 `/activate`，ArcoVue 没有。`CloseAccount` 已在 Auth 接口，账号中心未接。站内信抽屉只标已读；提及通知写在 `NotificationRecord.Target`（`{category}#{linkId}`），Inbox JSON 未返回 `target`。流程通知未写 `Target`。租户成员在 MVC 有管理页，API 没有 `Manage`，通用列表加链接即可。

本号依赖 OSC-2610011ff2 把 `/Admin/TenantUser` 等隐藏地址注册成路由。未落地时「成员」链接会 404，验收须两号一起看。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | `/activate` 放在公开路由，无布局。`token` 与 `account` 查询都非空时立刻 `GET /api/Auth/Activate`。否则显示表单：渠道 `mail` 或 `sms`、账号、验证码。发送 `POST /api/Auth/SendActivateCode`，提交 `POST /api/Auth/Activate`。失败展示接口 `message`。 |
| 2 | 注销按钮在账号安全页签底部。确认框有一个输入框，与 `userInfo.name` 全字匹配（区分大小写）才可确定。不匹配不发请求。接口失败保持登录态并提示。 |
| 3 | `GET /Cube/Automation/Inbox` 的每条增加 `target`（实体已有列）。`InboxMessageItem` 增加可选 `target`。 |
| 4 | 流程 `WorkflowEngine.Notify` 在插入前：取该实例第一条 `WorkflowSubject`，若 `TypePath` 与 `EntityKey` 都非空，则 `Target = TypePath + "#" + EntityKey`。提及通知的 Target 写法不改。 |
| 5 | 前端 `parseInboxTarget`：仅接受 `^[A-Za-z][A-Za-z0-9]*/[A-Za-z][A-Za-z0-9]*#[0-9]+$`。命中则 `router.push({ path, query: { id } })`。含 `..`、空格、`?`、`http` 的字符串视为无法解析。 |
| 6 | 实体列表发现查询 `id` 为正整数时，加载完成后打开该主键的详情抽屉（不在当前页则 `getDetail`）。只处理一次，随后去掉对打开动作的重复触发；不把列表筛选改成只剩这一条。 |
| 7 | 租户「成员」用合成列，不用第二套成员页。`TenantController` 增加列表字段：显示名「成员」，`Url=/Admin/TenantUser?tenantId={Id}`，无 `DataAction`。 |

## 4. 做什么

1. 激活页与注销确认。
2. Inbox 返回 `target`；流程通知写入关联主键；点击跳转；列表识别 `?id=` 打开详情。
3. 租户合成列。
4. 纯函数单测与后端 Inbox 投影/Target 拼装测试。
5. 站内信抽屉按日期分桶双层时间轴（执行期补录，见 tasks T2-6）。

## 5. 不做什么

- 不做菜单搜索（三种布局均不增加搜索框）。
- 不做菜单树上升/下降。
- 不做地区地图。
- 不做文案外置 / 语言切换。
- 不做工作台部件全局管理页。
- 不新增租户成员专用页（不加用户、改角色仍走租户用户通用表单）。
- 不改站内信列表的已读接口语义；无法解析的通知不发明的跳转地址。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-2610011ff2 | `/Admin/TenantUser`、令牌等隐藏路由。本号的「成员」链接假定该路由存在 |
| OSC-26081903c0 | 站内信抽屉已有标已读 |
| OSC-26090347f1 | 流程 `Notify` 已写站内信，本号只补 Target |
| Auth `Activate` / `CloseAccount` | 接口已有，本号只接页面 |

## 7. 测试范围

触及 `web/`、`packages/api-core`、`NewLife.Cube`。执行期跑本号 Vitest 与 XUnit。验收期新增单测全过；`vue-tsc -b` 与 `dotnet build NewLife.Cube -f net10.0` 无错误。
