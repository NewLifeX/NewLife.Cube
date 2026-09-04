# ALM：在线高峰告警与站内信

> 版本：v1.1（极简内置参数） | 日期：2026-09-04
> 适用：NewLife.Cube（WebAPI 主线）+ NewLife.CubeNC（MVC 兼容层，含 UI）

## 1. 背景与目标

使用魔方开发的系统经常遭遇扫描、突发流量、推广活动等，**当前在线数会在短时间内冲高甚至远超平日纪录**。管理员需要第一时间感知这类异常，以便排查是爬虫扫描还是真实业务高峰。

本文描述两个机制：

| 机制 | 说明 |
|------|------|
| 在线高峰告警（ALM-1/2） | 当前在线数**超过近 7 天最高纪录的固定比例**时，向系统管理员发送一条**系统级站内信广播** |
| 站内信未读闭环（NOTI-6） | 导航栏铃铛展示未读红点与最近未读，支持单条/全部标记已读——让"提醒"真正到达人 |

> **设计取舍**：不做后台可配置项，采用**内置固定参数**（近 7 天窗口 + 比例 1.2 + 每天最多一条），把预警逻辑做到极简、可预期。如需调整，改 `OnlineAlertService` 顶部两个常量即可。

## 2. 判定口径（内置，无需配置）

**触发条件（全部满足才告警）**：

1. 当前在线数 `total` > 近 7 天（不含今天）每日最大在线的最大值 `max` × **1.2**（即高出历史峰值 20% 以上，过滤小幅波动噪音）；
2. **每天最多一条**：以当天是否已存在 `Action=OnlineAlert` 记录判断，天然跨进程重启、多节点基本去重。

**前置依赖（自动判断，不满足则不告警）**：魔方「用户在线 > 0」且「用户统计 = true」——否则没有历史纪录可比。

**无历史基线**：若近 7 天没有任何一天 `UserStat.MaxOnline > 0`（全新系统），说明还没有可比纪录，**不告警**，先积累基线。

## 3. 数据流

```
UserService.ClearExpire()  每60秒，清理20分钟不活跃会话
   └─ total = UserOnline.Meta.Count        // 清理后在线总数
        └─ total 相对上次变化时调用 OnlineAlertService.Check(total, now)
             ├─ 取近7天最高纪录 max（UserStat.MaxOnline 聚合）
             ├─ total > max×1.2 且今日未告警 → 写一条 NotificationRecord
             │     Action=OnlineAlert, Channel=InApp, UserId=0（系统级广播）
             └─ 管理员导航栏铃铛轮询未读数（60s）→ 红点 / 最近未读下拉
                  点击条目 / 全部已读 → Read=true（广播任意一人已读即全局消除）
```

当前在线数取 `ClearExpire` 清理后的 `UserOnline.Meta.Count`，与 `UserStat.MaxOnline` 的口径完全一致（后者本来就由同一处维护）。告警检测**无进程内状态**，频控靠查当日已有记录，逻辑最简单且重启不丢语义。

## 4. 站内信可见性与已读语义（FAQ）

### 4.1 系统级站内信只写一条吗？谁能看到？

**只写一条**（`UserId=0` 表示系统级广播），不按管理员数写 N 份副本。

可见性由魔方数据权限天然实现：`NotificationRecordController` 的 `[DataPermission(null, "UserId={#userId}")]` 中，**系统角色（IsSystem）直接放行**（见 `ReadOnlyEntityController2.CreateWhere`），普通用户被 `UserId=当前用户` 过滤。因此：

- **UserId=0 的广播：只有系统管理员能看到**（普通用户不可见）；
- 个人站内信（UserId=某用户）：仅本人可见。

### 4.2 任意一人已读即可？

**是**。广播只有一条共享记录、只有一个 `Read/ReadTime`，**任何管理员标记已读后，其他管理员刷新即消失**——天然满足"任意一人已读即可"，无需每人一份 + 每人生成已读状态的复杂模型，也不会造成数据膨胀。

标记已读会记录已读人（写入 `Result` 字段，如"已读：张三"，便于审计），**无需新增表字段**。

### 4.3 为什么不用 CronJob / 独立调度？

在线统计链路本就有 `UserService` 60 秒定时器，`ClearExpire` 每次清理后已经算出 `total` 并维护 `UserStat.MaxOnline`。在此处内联检查**零新增调度、零重复全表计数、口径一致**。若用 CronJob 每分钟自行轮询，需额外全表计数 + 额外窗口聚合，还会出现"统计口径与告警口径不一致"的隐患。

### 4.4 多租户？

告警为**系统级**（`TenantId=0`）：`UserOnline/UserStat` 本身非租户隔离，在线数是全站口径，故告警发送到系统管理后台可见的广播。面向租户的定向告警不在本期范围。

### 4.5 进程重启 / 多节点会重复告警吗？

频控基于**当日是否已有 `Action=OnlineAlert` 记录**（数据库判断），因此进程重启不会重复补发；多节点并发下极小概率同秒双写，可接受（每天最多一条的上限基本保证）。

## 5. 内置参数

以下常量位于 `OnlineAlertService.cs` 顶部，后台无配置页：

| 常量 | 值 | 说明 |
|------|:--:|------|
| `Days` | 7 | 比较窗口天数，取近 7 天（不含今天）每日最大在线 |
| `Ratio` | 1.2 | 触发比例，当前在线数需超过近 7 天最高纪录的该倍数（高出 20%） |

## 6. 代码结构

| 文件 | 角色 |
|------|------|
| `NewLife.Cube/OnlineAlertService.cs` | 检测与发送（无状态）：取 7 天纪录 → 比例比较 → 每日频控 → 写广播；`Check(total, now[, set])` |
| `NewLife.CubeNC/Services/UserService.cs` | `ClearExpire` 接线（在线数变化时调用 `Check`，并顺带修正 `MaxOnline` 纯增长漏刷新） |
| `NewLife.Cube/Entity/通知记录.Biz.cs` | 站内信未读/已读辅助：`GetInAppExp/CountUnread/GetRecentUnread/MarkRead/MarkAllRead` |
| `NewLife.CubeNC/Areas/Admin/Controllers/NotificationRecordController.cs` | JSON 端点：`NotifyCount/NotifyRecent/NotifyMarkRead/NotifyMarkAllRead` + 列表行内"标记已读" |
| `NewLife.CubeNC/Views/Shared/_NotifyBell.cshtml` | 主题无关铃铛（自包含样式/脚本），注入 ACE 与 layui 导航栏 |

> 工程文件共享说明：`OnlineAlertService` 物理位于 `NewLife.Cube`（经典工程自动包含），`NewLife.CubeNC` 经 csproj `<Compile Include>` 链接共享；NC 导航栏视图仅 NC 侧存在。

## 7. 测试

- `XUnitTest/OnlineAlertServiceTests.cs`：超比例写系统级广播、每天最多一条、次日再提醒、恰在阈值/低于阈值不告警、无历史基线不告警、统计未开启不告警（SQLite 集成）。
- `XUnitTest/NotificationRecordBizTests.cs`：广播仅管理员可见可读、个人仅本人、任意一人已读广播即全局消除、全部已读作用域。
- 上述 DB 用例与 `RolePermissionTests` 同处 `SqliteDb` 测试集合：**共用同一 SQLite 文件**（`SqliteDb.Ensure()`），避免多类各自重定向同一逻辑连接导致 XCode 表结构缓存错乱；各用例自清理数据保证独立。

## 8. 后续增强（本期不做）

1. 外部通道：邮件/钉钉/企微机器人的告警推送（`MailService/SmsService` 的 Notify 分支目前是 TODO，本期只做站内信）；
2. 告警参数后台可配（若需要，再引入 `Config<T>` + 配置页）；
3. 工作台"待办通知"卡片（`NotificationRecord` 与 Widget 联动）；
4. 告警内容附带 IP/请求维度线索（如同窗口期登录 IP 暴增），辅助区分扫描与真实高峰；
5. 非 NC 内置皮肤（Tabler 等外部皮肤工程）各自注入铃铛。
