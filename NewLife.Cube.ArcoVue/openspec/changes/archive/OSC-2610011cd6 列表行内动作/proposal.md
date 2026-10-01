# OSC-2610011cd6 — 列表行内动作

## 1. 目标愿景

列表上已经由元数据声明的行内动作，按接口的 HTTP 方法真正执行；用户页能清空密码和吊销令牌；在线记录能强制下线。

- 目标 1：`dataAction` 非空的操作列以 POST 调用解析后的地址。定时作业「马上执行」返回成功提示并刷新当前列表。
- 目标 2：单元格链接的地址以 `/api/` 开头时发 GET，不把该地址当成站内路由。访问规则「解封」成功后提示并刷新。
- 目标 3：系统角色在用户记录抽屉（非新增）可「清空密码」「吊销令牌」，均须确认；非系统角色看不到这两个按钮。
- 目标 4：用户在线列表出现「强制下线」，确认后 POST `Kick`，成功刷新。无 Delete 权限不显示。

## 2. 为何做

[`requestDataAction`](../../../web/src/views/crud/useListOpsLinks.ts) 固定 GET，而 [`CronJobController.ExecuteNow`](../../../../NewLife.Cube/Areas/Cube/Controllers/CronJobController.cs) 是 `[HttpPost]`。访问规则把解封挂在过期时间列的 `/api/Admin/AccessRule/Unblock` 上且没有 `dataAction`，前端把它 `router.push` 了。`Kick`、`ClearPassword`、`RevokeTokens` 接口已有，界面没有入口。MVC 编辑表单有清空密码；Cube.Vue 定时作业用 POST。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | `dataAction` 非空 → POST，query 留在 URL，body 为空。成功：`code===0` 或无 `code` 时提示 `message` 或「操作成功」，并触发列表刷新。`code!==0` 提示 `message`，不刷新。 |
| 2 | 单元格链接（无 `dataAction`）且解析后的 path 以 `/api/` 开头 → GET 同一规则的成功/失败提示与刷新。其它链接仍 `router.push` / 新窗口。 |
| 3 | 外链与 `target=_blank` 不改。确认文案：马上执行「确认立即执行该作业？」、解封「确认解除封禁？」、强制下线「确认强制该用户下线？」、清空密码「确认清空该用户密码？空密码时任意密码均可登录。」、吊销令牌「确认吊销该用户的全部访问令牌？」。 |
| 4 | 用户抽屉：`typePath` 规范化后为 `admin/user`，模式为 `edit` 或 `detail`，且 `userInfo.isSystem===true`，且记录主键 `>0` 时，在抽屉底部显示两个按钮。新增模式不显示。接口：`POST /api/Admin/User/ClearPassword?id=`、`POST /api/Admin/User/RevokeTokens?id=`。 |
| 5 | 强制下线不在前端写死按钮。在 API 工程 `UserOnlineController` 静态构造里增加合成列：显示名「强制下线」，`Url=/Admin/UserOnline/Kick?id={Id}`，`DataAction=action`。权限沿用动作上的 Delete。前端 POST 规则覆盖该列。 |
| 6 | 解封列保持单元格链接（有 TypeName），不改成操作列。只修正 `/api/` 的点击语义。 |

## 4. 做什么

1. 改 `requestDataAction` 的方法参数；单元格 `/api/` 走请求。
2. 两个确认框接在现有操作列点击与单元格点击上（Arco `Modal.confirm`）。
3. 用户抽屉两个按钮及 composable。
4. `UserOnlineController` 增加合成列。
5. 前端 Vitest 与后端一条合成列/动作可达性测试（能反射到 `DataAction` 与 `Kick` 的 HttpPost 即可，不要求真踢会话）。

## 5. 不做什么

- 不把所有单元格链接改成 POST。
- 不改 `ExecuteNow` / `Unblock` / `Kick` 的服务端业务（在线列只加链接元数据）。
- 不做菜单上升/下降。
- 不在用户列表工具栏批量清空密码。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-2608178bdb | Url / dataAction 分流已落地；本号只改请求方法与 `/api/` 单元格 |
| OSC-2610011ff2 | 在线列表路由兜底。本号不注册路由；无该路由时操作列仍可在页内 POST |

## 7. 测试范围

触及 `web/` 与 `NewLife.Cube`。执行期：本号 Vitest；`dotnet test` 过滤本号测试类。验收期新增单测全过；`vue-tsc -b` 与 `dotnet build NewLife.Cube -f net10.0` 无错误。
