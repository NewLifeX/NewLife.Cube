# OSC-2610011cd6 Design — 列表行内动作

## 0. 适用框架与官方资料

| 场景 | 框架 | 资料 |
| --- | --- | --- |
| 确认框、按钮、消息 | Arco Design Vue | https://arco.design/vue/docs/start （Modal、Message） |
| 操作列 | 现有 VTable customLayout | 不改 VTable 配置结构 |
| SFC | README | 确认与请求放在 `useListOpsLinks` / `useRecordDrawer` |

## 1. 文件级改动地图

| 文件 | 改动 | 保留 |
| --- | --- | --- |
| `web/src/views/crud/useListOpsLinks.ts` | `requestDataAction(url, method)`；`runCellFieldLink` 对 `/api/` 走 GET 请求 | `resolveRowUrl`、外链、站内 push |
| `web/src/core/utils/opsRequest.ts` | 新建。`opsHttpMethod(link)`、`isApiActionUrl(url)`、`confirmText(label)` | — |
| `web/src/core/utils/opsRequest.spec.ts` | 新建 | — |
| `web/src/views/crud/useDefaultList.ts` | 动作与 `/api/` 单元格在请求前 `Modal.confirm` | CRUD 删除确认保持原样 |
| `web/src/views/crud/RecordDrawer.vue` | 底部两个按钮，仅绑定可见性与点击 | 表单页签 |
| `web/src/views/crud/useRecordDrawer.ts` | `showUserSecurityActions`、`clearPassword`、`revokeTokens` | 保存/上下条 |
| `NewLife.Cube/Areas/Admin/Controllers/UserOnlineController.cs` | 静态构造添加合成列「强制下线」 | `Kick` 方法体、Search、Valid |

不改 `ExecuteNow`、`Unblock`、`ClearPassword`、`RevokeTokens` 的方法体。

## 2. 请求真值表

| 链接 | 方法 | 导航 |
| --- | --- | --- |
| `dataAction` 非空 | POST | 否 |
| 无 dataAction，解析 path 以 `/api/` 开头 | GET | 否 |
| 无 dataAction，站内业务路径 | — | `router.push` |
| `target=_blank` 或 `http(s)` | — | 新窗口 |

POST/GET 的成功判定与现 `requestDataAction` 相同：响应体 `code` 为数字且非 0 则抛 `message`。

确认文案由显示名决定，缺省「确认执行该操作？」：

| 显示名包含 | 文案 |
| --- | --- |
| 马上执行 | 确认立即执行该作业？ |
| 解封 | 确认解除封禁？ |
| 强制下线 | 确认强制该用户下线？ |

用户取消确认则不发请求。

## 3. 用户抽屉

| 条件 | 清空密码 | 吊销令牌 |
| --- | --- | --- |
| type 不是 `admin/user` | 隐藏 | 隐藏 |
| 模式 `add` | 隐藏 | 隐藏 |
| `userInfo.isSystem !== true` | 隐藏 | 隐藏 |
| 主键缺失或 ≤0 | 隐藏 | 隐藏 |
| 以上皆否 | 显示 | 显示 |

两个按钮都先确认（文案见 proposal §3）。成功 `Message.success`，不关闭抽屉，不改表单脏状态。失败 `Message.error`。

## 4. 在线合成列

在 `UserOnlineController` 静态构造末尾：

- `ListFields.AddListField("Kick", null, "Name")`（插在名称附近；若 `Name` 不存在则 `AddListField("Kick")`）。
- `DisplayName = "强制下线"`。
- `Url = "/Admin/UserOnline/Kick?id={Id}"`。
- `DataAction = "action"`。

`Kick` 保持 `[HttpPost]` 与 Delete 权限。前端把它当作 dataAction 操作列，因此 POST。

## 5. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| `ArcoVue企业中后台迁移方案.md` | 修改：dataAction 为 POST、`/api/` 单元格不导航 |
| `web/README.md` | 无 |
| `Doc/功能清单.md` | 无新接口；不改清单编码 |
| `Doc/Api/核心接口架构.md` | 无 |

## 6. 测试设计

前端：

- dataAction → POST。
- `/api/Admin/AccessRule/Unblock?id=1` → GET，且 `isApiActionUrl` true。
- `/Admin/Log?userId=1` → 不是 API 动作。
- 显示名「马上执行」得到指定确认文案。
- `showUserSecurityActions('Admin/User','edit',{isSystem:true}, 5)` 为 true；add、非系统、id=0 为 false。

后端：

- 反射 `UserOnlineController` 的 ListFields 含 Url 以 `/Admin/UserOnline/Kick` 开头且 `DataAction==action`。
- `Kick` 方法带 `HttpPostAttribute`。
