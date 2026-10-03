# OSC-261001909b Design — 壳层账号与站内信

## 0. 适用框架与官方资料

| 场景 | 框架 | 资料 |
| --- | --- | --- |
| 表单、确认 | Arco Design Vue | https://arco.design/vue/docs/start （Form、Modal） |
| SFC | README | 激活与注销逻辑在 composable / 纯函数 |

## 1. 文件级改动地图

| 文件 | 改动 | 保留 |
| --- | --- | --- |
| `web/src/views/login/activate.vue` | 新建公开页 | — |
| `web/src/views/login/useActivatePage.ts` | token 直达与验证码表单 | — |
| `web/src/router/index.ts` | 公开路由 `/activate` | 登录/注册 |
| `web/src/views/account/SecuritySettings.vue` | 底部注销按钮 | TOTP |
| `web/src/views/account/useCloseAccount.ts` | 用户名确认与 POST | — |
| `web/src/core/utils/inboxTarget.ts` | `parseInboxTarget` | — |
| `web/src/core/utils/inboxTarget.spec.ts` | 新建 | — |
| `web/src/views/inbox/useInboxDrawer.ts` | 点击：标已读；可解析则 push | 全部已读 |
| `packages/api-core/src/api.ts` | `InboxMessageItem.target?` | 其它字段 |
| `NewLife.Cube/Controllers/AutomationController.cs` | Inbox 投影加 `e.Target` | 已读接口 |
| `NewLife.Cube/Workflow/WorkflowEngine.cs` | `Notify` 写 Target | 通知正文截断 |
| `web/src/views/crud/useDefaultList.ts` | 查询 `id` 为正整数时打开详情一次 | 列表筛选 |
| `NewLife.Cube/Areas/Admin/Controllers/TenantController.cs` | 合成列「成员」 | 租户 CRUD |

## 2. 激活页

| 查询 | 行为 |
| --- | --- |
| `token` 与 `account` 都非空 | 进入页即 GET `/api/Auth/Activate`，成功提示并 1 秒后 `router.push('/login')`，失败展示 message 与返回登录 |
| 否则 | 表单。渠道单选 mail / sms，默认 mail。发送验证码 POST `/api/Auth/SendActivateCode`，body `{ channel, username: account }`。提交 POST `/api/Auth/Activate`，body `{ channel, account, code }` |

账号或验证码为空时提交按钮禁用。发送中按钮 loading。

## 3. 注销

安全页签最底「注销账号」危险按钮。确认框输入值 `=== userInfo.name` 时确定按钮可点。POST `/api/Auth/CloseAccount` 无 body。成功：`clearSession`、`router.push('/login')`。失败：不清除会话。

`userInfo.name` 为空时按钮禁用，提示「无法确认当前用户名」。

## 4. 站内信与详情

`parseInboxTarget` 只接受 `Area/Controller#正整数`。区域与控制器各一段，字符为字母后接字母或数字。拒绝：

- `Admin/User#0`（主键须 ≥1）
- `Admin/User/Extra#1`（三段）
- `http://x#1`
- `Admin/../User#1`

命中后 path 为 `/${area}/${controller}`，query `id` 为该数字的十进制字符串。

列表侧：`route.query.id` 匹配 `^[1-9][0-9]*$` 时，在首次列表成功返回后调用已有打开详情函数；同一路由实例只自动打开一次。用户随后关掉抽屉不再自动打开，直到 `id` 查询变化。

流程通知：`Notify` 内 `WorkflowSubject.FindAll(InstanceId==instance.Id).OrderBy(Id).FirstOrDefault()`。两者皆非空才写 Target。查不到主体则 Target 保持空，点击只标已读。

## 5. 租户成员列

`TenantController` 静态构造：

- `AddListField("Members")`，`DisplayName=成员`，`Url=/Admin/TenantUser?tenantId={Id}`，不设 `DataAction`。

这是导航链接，沿用 OSC-2608178bdb 的单元格/操作列分流（无 TypeName 的合成列进操作列）。本号不改分流规则。

## 6. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| `ArcoVue企业中后台迁移方案.md` | 修改：激活页、注销、站内信跳转各一句 |
| `Doc/Api/核心接口架构.md` | 修改：Inbox 项增加 `target`；Activate / CloseAccount 若文档未列则补路径 |
| `web/README.md` | 无 |
| `Doc/功能清单.md` | 无新模块编码则不新增；Inbox 字段变更写入 API 文档即可 |

## 7. 测试设计

- `parseInboxTarget('Admin/User#12')` → path `/Admin/User`、id `12`。
- `parseInboxTarget('Admin/User#0')` 与三段路径为 null。
- 注销确认：输入与 name 不一致时 `canSubmit=false`。
- 后端：Inbox 匿名投影对象包含 Target 属性（可用现有通知插入一条后调 Inbox，或直接断言 Select 匿名类型——优先插入一条 InApp 再调控制器方法，沿用 `NotificationRecordBizTests` 的库）。流程 Target 拼装抽成 `WorkflowEngine` 内部可见的静态函数 `BuildNotifyTarget(typePath, entityKey)` 以便单测：空主键返回 null，两者有值返回 `Admin/User#9`。
