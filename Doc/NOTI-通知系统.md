# 通知系统

魔方内置通知记录功能，支持站内信、短信、邮件、APP 推送、Webhook、钉钉机器人、企业微信机器人等多渠道通知的统一记录和审计。通知记录与验证码记录分别使用独立实体，覆盖通用通知和验证码两大核心场景。

整个通知体系**不新增数据表**：继续使用 `NotificationRecord`，一行代表“一个收件人 × 一个渠道”；模板、用户偏好、移动端设备令牌复用系统参数表 `Parameter`。`BizId` 是业务消息编号，相同 `BizId` 共享已读/处理状态。

## NotificationRecord 实体

`NotificationRecord` 位于 `NewLife.Cube/Entity/通知记录.cs`，记录每一条通知的完整发送详情。

### 核心字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | Int64 | 编号（时间戳类型，SnowFlake 生成） |
| `TenantId` | Int32 | 租户 ID（多租户隔离） |
| `BizId` | String(50) | 业务消息编号。相同 BizId 共享已读/处理状态；`scope=Each` 时每人一个 |
| `Shared` | Boolean | 团队共享读状态。true 表示同组任一收件人已读即全员已读 |
| `CancelOnRead` | Boolean | 任一渠道已读后取消尚未发送的后续渠道，默认 true |
| `Action` | String | 操作类型：Notify / Alert / Custom |
| `Channel` | String | 渠道标识（见渠道列表） |

### 渠道配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `ConfigId` | Int32 | 绑定的渠道配置 ID |
| `ConfigName` | String | 配置名称（便于识别） |
| `Provider` | String | 提供者标识（如 Aliyun、Tencent） |

### 接收者信息

| 字段 | 类型 | 说明 |
|------|------|------|
| `UserId` | Int32 | 用户 ID（站内信、已知用户场景） |
| `Target` | String | 目标地址（手机号 / 邮箱 / OpenID / 机器人 URL） |

### 内容与结果

| 字段 | 类型 | 说明 |
|------|------|------|
| `Title` | String | 通知标题（站内信标题 / 邮件主题） |
| `Content` | String | 通知正文内容。短信/APP 推送行可存摘要 |
| `Category` | String | 分类：System / Business / Security / Task / Marketing |
| `Level` | String | 级别：Info / Success / Warning / Error / Urgent |
| `Link` | String | 点击通知打开的前端地址 |
| `Data` | String(-1) | JSON 业务载荷，供模板渲染、短信参数与跳转使用 |
| `TemplateCode` | String | 模板编码 |
| `Language` | String | 渲染语言 |
| `SenderId` | Int32 | 发送人，0 表示系统 |
| `Status` | Int32 | 0 待发送 / 1 发送中 / 2 成功 / 3 失败 / 4 跳过 |
| `Attempts` | Int32 | 已尝试次数，最多 3 次 |
| `NextRetryTime` | DateTime | 下次重试时间 |
| `SentTime` | DateTime | 发送成功时间 |
| `ExpireTime` | DateTime | 过期时间，过期后不再展示与发送 |
| `Success` | Boolean | 发送是否成功 |
| `Result` | String | 发送结果（成功时为返回信息，失败时为错误详情） |

### 站内信扩展

| 字段 | 类型 | 说明 |
|------|------|------|
| `Read` | Boolean | 已读标记 || `ReadTime` | DateTime | 已读时间 |
| `ReadBy` | Int32 | 已读人用户编号 |
| `ReadChannel` | String | 触发已读的渠道：InApp / AppPush / Sms / Mail / Batch |
| `Handled` | Boolean | 是否已处理（业务闭环） |
| `HandledBy` | Int32 | 处理人用户编号 |
| `HandledTime` | DateTime | 处理时间 |
| `HandleResult` | String | 处理结果摘要 |
| `Deleted` | Boolean | 软删除标记 |

> `CancelOnRead` 字段位于基础信息区，表示“任一渠道已读后取消尚未发送的后续渠道”，默认 true。

### 审计字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `TraceId` | String | 链路追踪 ID（`TraceModule` 自动记录） |
| `CreateIP` | String | 创建 IP（`IPModule` 自动记录） |
| `CreateTime` | DateTime | 创建时间（`TimeModule` 自动记录） |
| `UpdateTime` | DateTime | 更新时间 |
| `UpdateIP` | String | 更新 IP |
| `Remark` | String | 备注 |

### 自动模块

实体配置了以下自动模块：
- `TimeModule`：自动管理 CreateTime / UpdateTime
- `IPModule`：自动记录 CreateIP / UpdateIP
- `TraceModule`：自动记录链路追踪 ID
- `TenantModule`：多租户自动隔离

## 支持渠道

| 渠道标识 | 名称 | Target 格式 | 说明 |
|---------|------|------------|------|
| `InApp` | 站内信 | 无 | 系统内部通知，支持已读/处理/软删除 |
| `Sms` | 短信 | 手机号 | 通过短信服务商发送，使用 `SmsConfig.NotifyTemplateCode` |
| `Mail` | 邮件 | 邮箱地址 | 通过邮件服务发送 |
| `AppPush` | APP 推送 | 无（按 UserId） | 通过 `IAppPushProvider` 扩展点发送 |
| `Webhook` | 通用 Webhook | Webhook URL | POST JSON |
| `DingTalk` | 钉钉机器人 | Webhook URL | 钉钉文本消息格式 |
| `WeCom` | 企业微信机器人 | Webhook URL | 企业微信文本消息格式 |

## 索引设计

| 索引 | 字段 | 用途 |
|------|------|------|
| `IX_NotificationRecord_UserId_Channel_Deleted_Read_Id` | UserId, Channel, Deleted, Read, Id | 查询用户未读站内信 |
| `IX_NotificationRecord_TenantId_Channel_Id` | TenantId, Channel, Id | 租户/渠道列表 |
| `IX_NotificationRecord_TenantId_BizId_Channel` | TenantId, BizId, Channel | 按业务消息批量已读/处理 |
| `IX_NotificationRecord_Status_NextRetryTime` | Status, NextRetryTime | 投递调度扫描 |

索引刻意收敛为 4 个：站内信收件箱、租户渠道列表、业务消息已读/处理、投递扫描；`BizId` 同时承担 5 分钟幂等窗口，不再单独建去重键索引。

## 发布流程

业务调用 `NotificationService.Publish(NotificationRequest)` 一次完成：

1. 解析收件人：显式 `UserIds`、`RoleIds`、`DepartmentIds` 或 `All`；单次最多 1000 人，广播 + `scope=Any` 直接拒绝。
2. 取模板（可选）：按租户 + `TemplateCode` + `Language` 从 `Parameter`（`Category=NotifyTemplate`）读取，用 `Data` 渲染 `{key}`；站内信、邮件、短信、APP 推送可使用不同摘要。
3. 计算 `BizId`：调用方传入优先；否则按 `Action + 时间 + 随机` 生成。`scope=Any` 全组同一个；`scope=Each` 多人时追加 `:{userId}`。
4. 按“收件人 × 渠道”写记录：`InApp` 立即成功；邮件/短信/APP 推送/Webhook 按 `ChannelDelays` 计算 `NextRetryTime`，交给 `NotificationDispatcher` 排队发送。
5. 用户偏好过滤：发布前读取 `Parameter`（`Category=Notify`，`Name=分类`），只投递用户允许的渠道。
6. 幂等：调用方显式传入的 `BizId` 在 5 分钟窗口内重复发布时整批跳过；不传则每次生成新 BizId。

```csharp
var result = notifyService.Publish(new NotificationRequest
{
    UserIds = [userId],
    Scope = NotificationScope.Each,
    TemplateCode = "task.done",
    Data = new { name = "部署" },
    Channels = ["InApp", "AppPush", "Mail"],
    ChannelDelays = [0, 30, 60], // 站内信立即，APP 30 秒，邮件 60 秒
    CancelOnRead = true,
    Category = "Task",
    Level = "Info",
});
```

## 渠道顺序与已读取消

- `Channels` 的顺序就是投递顺序，`ChannelDelays` 是与之一一对应的延迟秒数；`InApp` 始终立即写入收件箱，延迟只影响外部渠道。
- 外部渠道记录写入时 `Status=Pending`、`NextRetryTime=当前时间+延迟`，调度器到点才发送。
- `CancelOnRead=true`（默认）时，任一渠道标记已读或处理，会：
  1. 把同一 `BizId` 下当前用户的全部渠道记录标记已读（团队共享则全组）；
  2. 把尚未发送的 `Pending` 外部渠道改为 `Status=Skipped`，结果写“Cancelled because read/handled”；
  3. 调度器发送前再次检查同组是否已读/已处理，避免已读后仍发出邮件。
- 例：站内信、APP、邮件延迟 0/30/60 秒。站内信读了，APP 和邮件不再发送；APP 读了，站内信置为已读、邮件不再发送。
- 设置 `CancelOnRead=false` 可保留“各渠道独立送达”的旧行为。

## BizId 幂等

`BizId` 就是业务消息号，同时承担短窗口幂等：

- 调用方显式传 `BizId`（如 `order:123:paid`）时，相同租户 5 分钟内重复发布直接返回 `Skipped=true`，不写新行；用于 MQ 重投、重复点击、多节点触发。
- 不传 `BizId` 时每次生成新的，不做跨请求幂等；适合一次性通知。
- 不同业务消息用不同 `BizId`；需要同一业务对象发多条通知时，在 `BizId` 里体现事件（`order:123:paid`、`order:123:shipped`）。
- 这是尽力而为的软幂等，不是唯一约束；并发发布仍可能同时通过，强幂等需业务侧发件箱/唯一状态配合。

## 已读与处理

- **个人通知**（`scope=Each`，`Shared=false`）：每人一个 `BizId`，各自标记已读，互不影响。
- **团队通知**（`scope=Any`，`Shared=true`）：发布时为每位成员写一条 `InApp` 行，全部共享同一个 `BizId`；任一成员在任意终端已读或处理，会把同组全部渠道记录标记已读，并把未发送渠道置为跳过；历史已读行仍保留在各自收件箱。
- `CancelOnRead=true` 时，任一渠道已读/处理都会取消同 `BizId` 下尚未发送的外部渠道；调度器发送前再次检查，避免已读后仍发出邮件或 APP 推送。
- `Read` 与 `Handled` 分离：已读只表示看过，处理表示业务闭环；两者都记录操作人与渠道/结果。
- 审计事件写入系统日志 `Log`：`Category=Notification`，`Action=Read/TeamRead/BatchRead/Handle`，`LinkId=记录编号`，`Ex4=BizId`，`Ex5=渠道`。

## 模板、偏好与设备

| 数据 | 存放 | 键 |
|------|------|----|
| 通知模板 | `Parameter` | `Category=NotifyTemplate`，`Name={code}` 或 `{code}:{language}`，`LongValue=JSON` |
| 用户偏好 | `Parameter` | `UserId=用户`，`Category=Notify`，`Name=分类`，`Value=逗号分隔渠道` |
| 移动端设备 | `Parameter` | `UserId=用户`，`Category=NotifyDevice`，`Name=设备令牌`（超50字符用 `Dev_{md5}`），`LongValue=原始令牌`，`Value=平台` |

不新增表，租户模板用 `Parameter.UserId=tenantId` 隔离。设备令牌只负责登记/注销，实际推送由宿主注册的 `IAppPushProvider` 完成。

## 实时推送

| 端点 | 说明 |
|------|------|
| `POST /Notify/StreamTicket` | 签发 60 秒一次性票据，绑定当前用户 + IP |
| `GET /Notify/Stream?ticket=&lastId=` | `text/event-stream`，3 秒增量、10 秒未读数变化、20 秒心跳、`X-Accel-Buffering: no` |
| `GET /Notify/Count` | 未读数轮询兜底 |
| `GET /Notify/Incremental?lastId=` | 增量拉取 |

访问令牌不放入 URL；票据一次性消费。前端 SSE 连接失败自动降级 60 秒轮询，多标签页通过 `BroadcastChannel` 同步已读变化。MVC 不做 SSE，继续使用 `_NotifyBell` 60 秒轮询。

## 投递与重试

`NotificationDispatcher` 每 10 秒扫描 `Status=Pending` 或到期 `Failed` 的外部渠道记录：

| 次数 | 失败后下次重试 |
|------|----------------|
| 1 | 1 分钟 |
| 2 | 5 分钟 |
| 3 | 15 分钟 |

3 次后保持失败，不再自动重试；管理端可手动重试。记录使用 `UPDATE ... WHERE Status IN (Pending, Failed)` 抢占，避免多节点重复投递。

## 升级说明

本次扩展在 `NotificationRecord` 增加 `BizId/Shared/CancelOnRead/Category/Level/Link/Data/TemplateCode/Language/SenderId/Status/Attempts/NextRetryTime/SentTime/ExpireTime/ReadBy/ReadChannel/Handled/HandledBy/HandledTime/HandleResult/Deleted` 字段及 4 个索引，`SmsConfig` 增加 `NotifyTemplateCode`，`MailConfig` 增加 `EnableNotify`。

- 开发环境 `Migration=On` 时由 XCode 自动补列；生产环境通常 `Migration=Off`，需要 DBA 按 `Cube.xml` 或 `Cube.htm` 手工执行加列/加索引脚本。
- 新列均可空或有默认值，旧记录按“未删除、非共享、未处理、未过期”处理；未指定过期时间时写入“当前时间 + 100 年”，查询同时兼容 NULL 与最小时间，避免不同数据库对空日期的差异。
- 回滚只删除新增列/索引即可，旧字段与旧接口保持兼容。

## VerifyCodeRecord 实体

`VerifyCodeRecord` 位于 `NewLife.Cube/Entity/验证码记录.cs`，专门记录验证码的完整生命周期。

### 核心字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | Int64 | 编号（时间戳类型） |
| `TenantId` | Int32 | 租户 ID |
| `Action` | String | 操作：Login / Reset / Bind |
| `Channel` | String | 渠道：Sms / Mail |
| `ConfigId` | Int32 | 配置 ID |
| `ConfigName` | String | 配置名 |
| `Provider` | String | 提供者（Aliyun / Tencent 等） |
| `UserId` | Int32 | 已知用户 ID |
| `Target` | String | 目标（手机号 / 邮箱） |
| `Code` | String | 验证码值 |
| `Content` | String | 发送时的完整内容 |

### 发送结果

| 字段 | 类型 | 说明 |
|------|------|------|
| `Success` | Boolean | 发送成功标记 |
| `Result` | String | 发送结果 / 错误信息 |

### 验证生命周期

| 字段 | 类型 | 说明 |
|------|------|------|
| `ExpireTime` | DateTime | 验证码过期时间 |
| `VerifyTimes` | Int32 | 验证次数（0 = 未验证） |
| `LastVerifyTime` | DateTime | 最后验证时间 |
| `LastVerifyResult` | Boolean | 最后验证结果 |

### 审计字段

`VerifyCodeRecord` 同样配置了 `TimeModule`、`IPModule`、`TraceModule`、`TenantModule`。

## 集成场景

| 场景 | 实体 | Action | Channel | 说明 |
|------|------|--------|---------|------|
| 登录验证码 | VerifyCodeRecord | Login | Sms / Mail | 发送登录验证码 |
| 重置密码 | VerifyCodeRecord | Reset | Sms / Mail | 密码重置验证码 |
| 绑定手机 | VerifyCodeRecord | Bind | Sms | 绑定新手机号验证 |
| 站内通知 | NotificationRecord | Notify | InApp | 系统推送消息 |
| 告警通知 | NotificationRecord | Alert | Sms / Mail / DingTalk | 系统告警 |
| 业务通知 | NotificationRecord | Custom | WxMp / WeCom | 自定义业务事件 |

## 发送服务集成

### 短信验证码发送示例

```csharp
public async Task<VerifyCodeRecord> SendVerifyCode(String action, String mobile,
    String code, SmsConfigModel config)
{
    var record = new VerifyCodeRecord
    {
        TenantId = config.TenantId,
        Action = action,                    // Login / Reset / Bind
        Channel = "Sms",
        ConfigId = config.Id,
        ConfigName = config + "",
        Provider = config.Provider,         // Aliyun / Tencent 等
        UserId = ManageProvider.User?.Id ?? 0,
        Target = mobile,
        ExpireTime = DateTime.Now.AddSeconds(config.Expire > 0 ? config.Expire : 300),
    };

    // 调用短信提供者发送
    var provider = GetProvider(config);
    var result = await provider.Send(mobile, code);

    // 记录发送结果
    record.Success = result.IsValid;
    record.Result = result.Message;
    record.Insert();

    return record;
}
```

### 验证码校验

```csharp
// 校验验证码时更新记录
record.VerifyTimes++;
record.LastVerifyTime = DateTime.Now;
record.LastVerifyResult = (record.Code == inputCode && DateTime.Now < record.ExpireTime);
record.Update();
```

## 查询方法

### NotificationRecord 查询

```csharp
// 获取渠道列表（字段缓存，10分钟刷新）
var channels = NotificationRecord.GetChannelList();
// 返回：{ "InApp": "5", "Sms": "12", "Mail": "3" }
```

### VerifyCodeRecord 查询

```csharp
// 获取渠道列表
var channels = VerifyCodeRecord.GetChannelList();
// 返回：{ "Sms": "100", "Mail": "50" }
```

## 数据存储

`NotificationRecord` 和 `VerifyCodeRecord` 均存储在日志数据库（`ConnName = "Log"`），与主业务库分离。这样设计可以：

- 避免大量通知记录影响主库性能
- 独立配置日志库的备份和清理策略
- 通过 `DataRetentionService` 自动清理过期记录（参见 [数据保留](BASE-数据保留.md)）

## 管理后台

通知记录和验证码记录在管理后台中可查看，用于审计和排查问题。

### 通知记录管理

支持的功能：
- **按渠道过滤**：InApp / Sms / Mail / DingTalk 等
- **成功/失败统计**：快速定位发送失败的通知
- **用户追踪**：按 UserId 查看某用户的全部通知历史
- **错误排查**：通过 Result 字段查看详细错误信息
- **链路追踪**：通过 TraceId 关联完整的请求链路

### 验证码记录管理

支持的功能：
- **发送记录查看**：查看验证码发送历史和成功率
- **验证状态**：查看验证码是否被使用（VerifyTimes > 0）
- **过期状态**：查看验证码是否在有效期内
- **安全审计**：按 IP 和目标地址追踪异常请求

## 多租户支持

两个实体均配置了 `TenantModule`，自动按 `TenantId` 进行数据隔离。在多租户环境下，每个租户只能看到属于自己的通知和验证码记录。

## 最佳实践

1. **渠道选择**：根据通知紧急程度选择合适渠道 — 紧急告警用短信/钉钉，一般通知用站内信/邮件
2. **验证码安全**：设置合理的过期时间（默认 300 秒），限制验证次数防止暴力枚举
3. **日志清理**：配合 `DataRetentionService` 定期清理过期的通知和验证码记录
4. **链路追踪**：利用 TraceId 快速定位通知发送失败的根因
5. **渠道配置**：通过 ConfigId/ConfigName 关联渠道配置，支持同一渠道多套配置（如多个短信账号）
