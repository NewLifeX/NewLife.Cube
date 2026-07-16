# 定时作业

魔方内置定时作业框架，支持通过 Cron 表达式调度后台任务。作业可在管理后台动态配置，支持启用/禁用、修改执行计划、传递 JSON 参数。

## 核心概念

| 组件 | 说明 |
|------|------|
| `ICubeJob` | 作业接口，所有自定义作业需实现该接口 |
| `CubeJobBase` | 作业基类，提供 `Job` 属性访问作业实体 |
| `CubeJobBase<T>` | 泛型作业基类，自动将 JSON 参数反序列化为强类型对象 |
| `CronJobAttribute` | 作业标记特性，声明作业名称、Cron 表达式和默认启用状态 |
| `JobService` | 后台宿主服务，负责扫描、调度和执行作业 |
| `CronJob` | 作业实体，持久化作业配置和执行状态 |

## 实现自定义作业

### 简单作业

实现 `ICubeJob` 接口，用 `CronJobAttribute` 标记：

```csharp
[DisplayName("清理临时数据")]
[Description("每天凌晨清理过期的临时数据")]
[CronJob("CleanTemp", "0 0 2 * * ? *", Enable = true)]
public class CleanTempService : CubeJobBase
{
    public override async Task<String> Execute(String argument)
    {
        var count = TempData.DeleteExpired();
        return $"清理 {count} 条记录";
    }
}
```

### 带参数的作业

继承 `CubeJobBase<T>` 实现强类型参数作业：

```csharp
public class SyncArgument
{
    /// <summary>同步来源</summary>
    public String Source { get; set; }

    /// <summary>批次大小</summary>
    public Int32 BatchSize { get; set; } = 1000;
}

[DisplayName("数据同步")]
[CronJob("DataSync", "0 */30 * * * ? *")]
public class DataSyncService : CubeJobBase<SyncArgument>
{
    protected override async Task<String> OnExecute(SyncArgument argument)
    {
        // argument 已自动从 JSON 反序列化
        var count = await SyncFromSource(argument.Source, argument.BatchSize);
        return $"同步 {count} 条";
    }
}
```

在管理后台编辑该作业时，参数字段填写 JSON：
```json
{"Source":"erp","BatchSize":500}
```

## CronJobAttribute 说明

| 属性 | 类型 | 说明 |
|------|------|------|
| `Name` | String | 作业唯一名称，用于数据库匹配 |
| `Cron` | String | Cron 表达式，仅在首次创建时使用，后续以管理后台修改为准 |
| `Enable` | Boolean | 创建时是否默认启用，默认 `false` |

## 内置作业

魔方默认提供三个内置作业：

### 备份数据库（BackupDbService）

- 默认 Cron：`5 0 0 * * ? *`（每天 00:00:05）
- 参数：连接名（多个用逗号分隔）
- 仅支持 SQLite 数据库

### HTTP 请求（HttpService）

- 默认 Cron：`25 0 0 * * ? *`（默认禁用）
- 参数格式：`HttpJobArgument`
  - `Method` — 请求方法（Get/Post）
  - `Url` — 请求地址
  - `Body` — 请求参数

### SQL 执行（SqlService）

- 默认 Cron：`15 * * * * ? *`（默认禁用）
- 参数格式：`SqlJobArgument`
  - `ConnName` — 数据库连接名
  - `Sql` — SQL 语句

## 作业调度流程

```
应用启动
  └─ JobService.StartAsync()
       └─ ScanJobs() 扫描所有 ICubeJob 实现类
            ├─ 解析 [CronJob] 特性
            ├─ 查找或创建 CronJob 实体（数据库）
            └─ 按 Cron 表达式创建 TimerX 定时器
                 └─ 到期执行 → 记录日志 → 更新状态
```

## 管理后台

定时作业在管理后台 **系统管理 → 定时作业** 中管理：

- **启用/禁用**：动态控制作业是否执行
- **修改 Cron**：调整执行计划
- **修改参数**：传递不同的 JSON 参数
- **查看日志**：查看最近执行时间和结果
- **手动唤醒**：调用 `JobService.Wake(jobId)` 立即触发

## 注意事项

- 作业名称（`CronJobAttribute.Name`）是全局唯一标识，修改后将创建新的作业记录
- Cron 表达式仅在首次创建时写入数据库，后续以数据库中的配置为准
- 作业执行异常会被捕获并记录到作业实体的日志字段，不会导致应用崩溃
- `JobService` 每 600 秒检测一次作业配置变更，也可通过 `Wake()` 立即生效
