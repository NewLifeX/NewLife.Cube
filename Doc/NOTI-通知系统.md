# 通知系统架构设计

> 一句话：业务模块调用 `INotifier` 发一条通知，魔方负责收件人、偏好、落库和投递；站内信实时推送，邮件/短信/推送等外部渠道异步重试。

## 1. 定位

- 通知系统是魔方（`NewLife.Cube`）的内置能力，**接口与实现都先放在 Cube**，暂不进入 `NewLife.Core`。
- 普通业务模块只依赖 `INotifier`；管理端和高级场景使用 `NotificationService.Publish`。`INotifier` 是给下游模块的契约，魔方自己的管理端不经过它。
- **不新增数据表**：`NotificationRecord` 一行代表“一个收件人 × 一个渠道”；模板、用户偏好、移动端设备复用系统参数表 `Parameter`。
- 通知内容**只支持单语言**，不做多语言；`NotificationRecord.Language` 列保留但不再使用。

## 2. 一张图看全流程

```text
业务模块
  │  INotifier.SendAsync(NotifyRequest)
  ▼
NotificationService ── 解析收件人 / 模板 / 用户偏好 ──► NotificationRecord（数据库）
  │                                                        │
  │  InApp：立即成功                                       │  外部渠道：Pending
  │  Created 事件 ──► SSE 推送                              │  Dispatch 事件 ──► 立即唤醒
  ▼                                                        ▼
NotifyController（SSE / 轮询）                      NotificationDispatcher
                                                           │ 60 秒兜底
                                                           │ 分布式锁 + 行抢占
                                                           ▼
                                             Mail / Sms / AppPush / Webhook / DingTalk / WeCom
```

## 3. 两个入口，按场景选择

| 场景 | 入口 | 请求 / 结果模型 |
|------|------|-----------------|
| 普通业务模块发通知 | `INotifier.SendAsync` | `NotifyRequest` / `NotifyResult`（小模型） |
| 管理端、工作流、定时任务 | `NotificationService.Publish` | `NotificationRequest` / `NotificationPublishResult`（完整模型） |

两个入口最终走同一套收件人解析、模板渲染、偏好过滤和落库逻辑，区别只是字段多少。

### 3.1 模块入口：INotifier

```csharp
public class OrderService(INotifier notifier)
{
    public async Task PaidAsync(Order order)
    {
        var result = await notifier.SendAsync(new NotifyRequest
        {
            BizId = $"order:{order.Id}:paid",
            UserIds = [order.UserId],
            Channels = [NotificationChannels.InApp, NotificationChannels.Mail],
            Title = "订单已支付",
            Content = $"订单 {order.Code} 已支付成功",
            Link = $"/orders/{order.Id}",
            Category = "Business",
            Level = "Success",
        });
    }
}
```

`NotifyRequest` 只有 8 个业务字段：

| 字段 | 说明 |
|------|------|
| `BizId` | 业务消息编号，可选。相同编号用于 5 分钟幂等与共享已读 |
| `UserIds` | 收件人用户编号 |
| `Channels` | 渠道列表，为空默认 `InApp` |
| `Title` | 标题 |
| `Content` | 内容 |
| `Link` | 点击跳转地址 |
| `Category` | 分类，默认 `Business` |
| `Level` | 级别，默认 `Info` |

`NotifyResult` 只有 4 个字段：

| 字段 | 说明 |
|------|------|
| `BizId` | 最终业务消息编号 |
| `Recipients` | 收件人数量 |
| `Records` | 写入记录数量（收件人 × 渠道） |
| `Skipped` | 是否因去重或无收件人而跳过 |

**返回语义**：`SendAsync` 返回成功表示“已受理并落库”，不代表外部渠道已经送达；外部渠道的实际结果看 `NotificationRecord.Status` / `Result`。

### 3.2 下游扩展字段：Items

`NotifyRequest`、`NotifyResult`、`NotificationRequest` 都实现 `NewLife.Data.IExtend`，下游可以追加自定义字段：

```csharp
var request = new NotifyRequest
{
    UserIds = [userId],
    Title = "订单 {orderId} 已支付",
    Content = "订单 {orderId} 已支付",
};
request.Items["orderId"] = order.Id;
request.Items["tenantCode"] = "acme";

await notifier.SendAsync(request);
```

- `Items` 会合并进 `Data`，参与 `{key}` 模板渲染，并随记录 JSON 保存；
- 自定义渠道可读取 `Items` 做业务扩展；
- `NotifyResult.Items` 供自定义 `INotifier` 实现返回额外信息。

### 3.3 高级入口：NotificationService.Publish

`NotificationRequest` 保留完整能力：

| 分组 | 字段 |
|------|------|
| 受众 | `TenantId`、`UserIds`、`RoleIds`、`DepartmentIds`、`ToAdmins`、`All` |
| 业务 | `BizId`、`Scope`、`Action`、`Category`、`Level`、`SenderId`、`ExpireTime` |
| 内容 | `TemplateCode`、`Title`、`Content`、`Link`、`Data`、`Items` |
| 投递 | `Channels`、`ChannelDelays`、`CancelOnRead`、`Target` |

- `ToAdmins=true`：按系统管理员解析收件人，生成管理员个人站内信；与 `All` 互斥。
- `All=true`：全员广播，单次最多 1000 人；不能与 `Scope=Any` 一起使用。
- `Scope=Any`：团队共享已读，要求 `Channels` 必须包含 `InApp`。

## 4. 数据模型

### 4.1 一行记录代表什么

`NotificationRecord` 一行 = **一个收件人 × 一个渠道**。

- `Channel=InApp` 的行就是收件箱正文；
- `Channel=Mail/Sms/AppPush/Webhook...` 的行是投递副本、重试状态和审计记录；
- 同一业务消息的多个收件人、多个渠道通过 `BizId` 关联。

### 4.2 核心字段

| 字段 | 说明 |
|------|------|
| `Id` | 记录编号（Int64 雪花/时间戳） |
| `TenantId` | 租户编号 |
| `BizId` | 业务消息编号，≤50 字符；相同编号共享已读/处理状态 |
| `Shared` | 团队共享读状态，`Scope=Any` 时为 true |
| `CancelOnRead` | 任一渠道已读后取消未发送渠道，默认 true |
| `Action` | 操作类型：`Notify` / `Alert` / `Task` 等 |
| `Channel` | 渠道标识 |
| `UserId` | 收件人用户编号 |
| `Target` | 外部渠道目标：手机号 / 邮箱 / Webhook URL |
| `Title` / `Content` | 标题与正文 |
| `Category` / `Level` | 分类与级别 |
| `Link` | 点击跳转地址 |
| `Data` | JSON 业务载荷，供模板渲染、短信参数与自定义渠道使用 |
| `TemplateCode` | 模板编码（单语言，不再拼语言后缀） |
| `Status` | 0 待发送 / 1 发送中 / 2 成功 / 3 失败 / 4 跳过 |
| `Attempts` | 已尝试次数，最多 3 次 |
| `NextRetryTime` | 下次重试时间 |
| `SentTime` | 发送成功时间 |
| `ExpireTime` | 过期时间 |
| `Success` / `Result` | 是否成功 / 结果或错误详情 |

站内信扩展字段：`Read`、`ReadTime`、`ReadBy`、`ReadChannel`、`Handled`、`HandledBy`、`HandledTime`、`HandleResult`、`Deleted`。

审计字段：`TraceId`、`CreateIP`、`CreateTime`、`UpdateTime`、`UpdateIP`、`Remark`，由 XCode 自动模块填充。

> `Language` 列保留以兼容旧数据，但发布、模板和调度都不再使用；通知内容按单语言处理。

### 4.3 BizId 与幂等

- 调用方显式传 `BizId` 时，相同租户 5 分钟内重复发布直接返回 `Skipped=true`，不写新行；用于 MQ 重投、重复点击、多节点触发。
- 不传 `BizId` 时每次生成新值，不做跨请求幂等。
- `Scope=Each` 且多收件人时，每人一个 `BizId`（`{base}:{userId}`）；`Scope=Any` 全组共用同一个。
- 这是尽力而为的软幂等，不是唯一约束；强幂等需要业务侧发件箱或唯一状态配合。

### 4.4 索引

| 索引 | 字段 | 用途 |
|------|------|------|
| `IX_NotificationRecord_UserId_Channel_Deleted_Read_Id` | UserId, Channel, Deleted, Read, Id | 用户未读站内信 |
| `IX_NotificationRecord_TenantId_Channel_Id` | TenantId, Channel, Id | 租户/渠道列表 |
| `IX_NotificationRecord_TenantId_BizId_Channel` | TenantId, BizId, Channel | 按业务消息批量已读/处理 |
| `IX_NotificationRecord_Status_NextRetryTime` | Status, NextRetryTime | 投递调度扫描 |

索引刻意收敛为 4 个；`BizId` 同时承担 5 分钟幂等窗口，不单独建去重键索引。

> 查询条件统一封装在 `通知记录.Biz.cs`（`FindDispatchable`、`TryClaimForSending`、`ExistsReadOrHandled`、`ExistsBizId` 等），应用层不再拼 `NotificationRecord._`。XCode 的 `XC0001` 分析器只在跨程序集使用时告警；调度器与实体同属 `NewLife.Cube`，不会告警，但规则同样适用。

## 5. 发布流程

`Publish` 一次完成 7 步：

1. **确定租户**：未指定时取当前租户上下文。
2. **取模板**：按租户 + `TemplateCode` 从 `Parameter`（`Category=NotifyTemplate`）读取，用 `Data` + `Items` 渲染 `{key}`。
3. **计算 BizId**：调用方传入优先；否则按 `Action + 时间 + 随机` 生成。
4. **解析收件人**：`UserIds` / `RoleIds` / `DepartmentIds` / `ToAdmins` / `All`；单次最多 1000 人。
5. **过滤用户偏好**：读取 `Parameter`（`Category=Notify`），只保留用户允许的渠道。
6. **写记录**：`InApp` 立即成功；外部渠道写 `Pending`，按 `ChannelDelays` 设置 `NextRetryTime`。
7. **发事件**：站内信写 `Created` 事件唤醒 SSE；有外部渠道待投递时写 `Dispatch` 事件唤醒调度器。

## 6. 投递：事件唤醒 + 定时兜底

### 6.1 为什么不再每 10 秒扫库

通知量小时，10 秒一次扫描大部分是空查询；通知量大时，多节点又会同时扫同一批记录。现在改成：

- **事件唤醒**：发布时如果有 `Pending` 外部渠道，立即发 `Dispatch` 事件；
- **定时兜底**：调度器每 60 秒自己扫一次，防止事件丢失、节点重启或总线不可用；
- **多实例互斥**：整个投递轮次先拿锁，再逐行抢占。

### 6.2 一次投递轮次

```text
DispatchOnce
  ├─ 拿锁：ICacheProvider.Cache.AcquireLock("CubeNotificationDispatch", 0, 120秒)
  │    ├─ 有缓存提供者：Redis/Stardust → 分布式锁
  │    └─ 无缓存提供者：进程内 SemaphoreSlim（单实例）
  ├─ 重置超过 5 分钟的 Sending 记录为 Pending
  ├─ 查询 Pending/到期 Failed 的外部渠道记录（每批 50 条）
  ├─ 逐条 UPDATE ... WHERE Status IN (Pending, Failed) 抢占
  ├─ 检查 CancelOnRead：同 BizId 已读/已处理 → 标记 Skipped
  ├─ 调用 INotificationChannel.SendAsync
  └─ 成功/失败/重试，释放锁
```

- 拿不到锁直接返回 0，说明其它节点正在投递；
- 行抢占是第二道保险，即使锁过期或跨节点时钟不一致，也不会重复发送同一条记录；
- `Sending` 超过 5 分钟视为节点崩溃遗留，自动回到 `Pending`。

### 6.3 重试策略

| 第几次失败 | 下次重试 |
|------------|----------|
| 1 | 1 分钟后 |
| 2 | 5 分钟后 |
| 3 | 15 分钟后 |

3 次后保持失败，不再自动重试；管理端可以手动重试。

### 6.4 已读取消

`CancelOnRead=true`（默认）时，任一渠道已读或处理会：

1. 把同一 `BizId` 下当前用户的全部渠道记录标记已读（团队共享则全组）；
2. 把尚未发送的 `Pending` 外部渠道改为 `Skipped`；
3. 调度器发送前再次检查同组是否已读/已处理，避免已读后仍发出邮件。

## 7. 渠道

| 渠道标识 | 名称 | Target | 说明 |
|----------|------|--------|------|
| `InApp` | 站内信 | 无 | 立即写入收件箱，支持已读/处理/软删除 |
| `Sms` | 短信 | 手机号 | 通过短信服务商发送，模板编号取 `SmsConfig.NotifyTemplateCode` |
| `Mail` | 邮件 | 邮箱 | 通过邮件服务发送 |
| `AppPush` | APP 推送 | 无（按 UserId） | 通过 `IAppPushProvider` 扩展点发送 |
| `Webhook` | 通用 Webhook | URL | POST JSON |
| `DingTalk` | 钉钉机器人 | URL | 钉钉文本消息格式 |
| `WeCom` | 企业微信机器人 | URL | 企业微信文本消息格式 |

渠道扩展实现 `INotificationChannel`，在 `AddCube` 之后注册即可；调度器按渠道名匹配。

内置渠道名统一放在 `NotificationChannels` 常量类，发送时写 `[NotificationChannels.InApp, NotificationChannels.Mail]`，避免手写字符串拼错。

### 7.1 移动端设备令牌

设备令牌仍然保留，用于宿主实现 `IAppPushProvider` 时按用户找设备：

| 项 | 值 |
|----|----|
| `UserId` | 用户编号 |
| `Category` | `NotifyDevice` |
| `Name` | 设备令牌（超过 50 字符用 `Dev_{md5}`） |
| `LongValue` | 原始令牌 |
| `Value` | 平台 |

用户侧接口：`POST /Notify/RegisterDevice`、`POST /Notify/UnregisterDevice`。魔方只负责登记/注销，实际推送由宿主注册的 `IAppPushProvider` 完成。

## 8. 用户偏好（已实现）

用户偏好按“分类”控制允许的渠道，存放在 `Parameter`：

| 项 | 值 |
|----|----|
| `UserId` | 用户编号 |
| `Category` | `Notify` |
| `Name` | 通知分类，如 `Business` / `System` |
| `Value` | 允许的渠道，逗号分隔，如 `InApp,Mail` |

- 发布前调用 `NotificationService.GetPreferences(userId, category)`，只投递用户允许的渠道；
- 团队共享通知只允许 `InApp`，避免共享已读与外部渠道冲突；
- 用户侧接口：`GET /Notify/Preferences`、`POST /Notify/SavePreferences`；
- Vue 消息中心 `NotificationPage.vue` 已提供分类偏好设置。

## 9. 模板

模板存放在 `Parameter`，不新增表：

| 项 | 值 |
|----|----|
| `UserId` | 租户编号（用租户隔离模板） |
| `Category` | `NotifyTemplate` |
| `Name` | 模板编码，如 `task.done` |
| `LongValue` | 模板 JSON |

模板 JSON 字段：`Code`、`Title`、`Content`、`SmsContent`、`PushContent`、`Link`、`Channels`。

- **模板是可选的**：模块直接传 `Title`/`Content` 时完全不需要 `TemplateCode`；只有希望管理端统一改文案、或同一套文案要复用到短信/推送摘要时，才用 `TemplateCode` 读取模板。
- `NotifyTemplate` 不是独立表，只是 `Parameter` 里的 JSON 结构；`TemplateCode` 是它的键，记录在 `NotificationRecord.TemplateCode` 里便于审计。
- 只按 `Code` 读取，**不再拼 `{code}:{language}`**；
- `{key}` 用发布请求的 `Data` + `Items` 替换；
- 短信内容只做摘要，点击链接回到站内信正文；短信模板编号在 `SmsConfig.NotifyTemplateCode` 配置。

## 10. 实时推送与事件总线

### 10.1 事件类型

`NotificationBus` 使用一个事件类型 `NotificationEvent`，通过 `Kind` 区分用途：

| Kind | 用途 | 订阅方 |
|------|------|--------|
| `Created` | 站内信已创建 | `NotifyController` 的 SSE |
| `Dispatch` | 有外部渠道待投递 | `NotificationDispatcher` |

- 事件只带 `Kind` / `Id` / `UserId`，不携带标题正文；消费方按 `Id` 从数据库读取，避免把持久化实体或请求模型当成传输契约。
- 总线优先使用 `IEventBusFactory`，其次使用 `ICacheProvider` 创建；
- 使用星尘/Redis 时，天然跨节点；单机 MemoryCache 时，进程内广播；
- 总线只做“唤醒”，通知内容仍从数据库读取，不承担持久化；
- 总线不可用时，SSE 继续 3 秒轮询，投递继续 60 秒兜底。

### 10.2 SSE

| 端点 | 说明 |
|------|------|
| `POST /Notify/StreamTicket` | 签发 60 秒一次性票据，绑定用户 + IP |
| `GET /Notify/Stream?ticket=&lastId=` | `text/event-stream`，3 秒增量、10 秒未读数变化、20 秒心跳 |
| `GET /Notify/Count` | 未读数轮询兜底 |
| `GET /Notify/Incremental?lastId=` | 增量拉取 |

访问令牌不放入 URL；票据一次性消费。前端 SSE 连接失败自动降级 60 秒轮询，多标签页通过 `BroadcastChannel` 同步已读变化。MVC 不做 SSE，继续使用 `_NotifyBell` 60 秒轮询。

## 11. 已读、处理与审计

- **个人通知**（`Scope=Each`）：每人一个 `BizId`，各自标记已读，互不影响。
- **团队通知**（`Scope=Any`）：发布时为每位成员写一条 `InApp` 行，共享同一个 `BizId`；任一成员已读或处理，会把同组全部渠道记录标记已读，并取消未发送渠道。
- `Read` 与 `Handled` 分离：已读表示看过，处理表示业务闭环；两者都记录操作人与渠道/结果。
- 审计事件写入 `Log`：`Category=Notification`，`Action=Read/TeamRead/BatchRead/Handle`，`LinkId=记录编号`，`Ex4=BizId`，`Ex5=渠道`。

## 12. 扩展点

| 扩展点 | 用途 |
|--------|------|
| `INotificationChannel` | 新增投递渠道（企业微信、飞书、自定义网关等） |
| `IAppPushProvider` | 接入极光/个推/FCM/APNs 等 APP 推送厂商 |
| `NotifyRequest.Items` / `NotificationRequest.Items` | 下游追加自定义字段，参与模板渲染与载荷 |
| `NotificationBus` 构造函数 `topic` | 多套通知总线隔离（少见） |
| `NotificationService.Publish` | 管理端/工作流使用完整受众、模板、延迟能力 |

## 13. 边界与不做

- **不放 `NewLife.Core`**：当前只服务魔方；等出现跨宿主（独立 Web、Server、Jobs）需求时，再抽最小契约。
- **不做多语言**：通知内容单语言；`Language` 列保留但不再使用。
- **不引入 Provider SPI**：渠道扩展用 `INotificationChannel`；钉钉/企微已由 `WebhookNotificationChannel` 覆盖。
- **事件总线不是消息队列**：只做唤醒，不保证持久化；丢事件由定时兜底补偿。
- **不删除设备令牌、模板管理、跨节点总线**：这三项都是现有能力；模板与设备复用 `Parameter`，跨节点总线复用星尘/Redis 的 `IEventBusFactory` / `ICacheProvider`，不新增基础设施。
- **不做受众表**：团队共享用 `BizId` + 逐人站内信行，发布时快照。
- **不新增表**：模板/偏好/设备继续复用 `Parameter`。

## 14. 验证

```powershell
dotnet build NewLife.Cube\NewLife.Cube.csproj -f net8.0
dotnet test NewLife.Cube.Tests\NewLife.Cube.Tests.csproj --filter "FullyQualifiedName~Notification"
```

测试覆盖：

- `NotificationServiceTests`：发布、渠道延迟、已读取消、偏好过滤、模板渲染、调度重试；
- `NotificationServiceNotifierTests`：模块入口、共享已读、空内容、ToAdmins、`Items` 扩展渲染；
- `NotificationBusTests`：Created/Dispatch 事件路由、缓存总线、无总线降级；
- `NotificationDiTests`：`AddCube` 后 `INotifier` / `NotificationService` 可解析。

## 15. 验证码记录

验证码与通知共用投递渠道，但使用独立实体 `VerifyCodeRecord`，记录验证码的发送与校验生命周期（`Action`、`Channel`、`Code`、`ExpireTime`、`VerifyTimes`、`LastVerifyResult` 等）。它不参与通知调度，避免验证码被重试或站内信化。

## 16. 管理后台

- 通知记录管理：按渠道、用户、分类、级别、成功/失败过滤，查看错误详情与 `TraceId`；
- 验证码记录管理：查看发送历史、验证状态、过期状态，按 IP/目标排查异常；
- 发送管理：`NotificationSendController` 提供发送、重试、模板管理；
- 两个实体都配置 `TenantModule`，按 `TenantId` 自动隔离。

## 17. 数据存储

`NotificationRecord` 与 `VerifyCodeRecord` 均使用日志库（`ConnName=Log`），与主业务库分离，便于独立备份、清理与性能隔离；过期数据由 `DataRetentionService` 按保留策略清理。
