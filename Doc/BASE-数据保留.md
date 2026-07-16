# 数据保留

魔方内置数据保留服务（`DataRetentionService`），定时清理过期的日志数据和备份文件，防止数据库和磁盘空间无限增长。该服务实现 `IHostedService`，随应用启动自动运行，无需额外配置。

## 服务架构

`DataRetentionService` 位于 `NewLife.CubeNC/Services/DataRetentionService.cs`，使用 `TimerX` 定时器驱动。

```csharp
public class DataRetentionService(CubeSetting setting, ITracer tracer) : IHostedService
{
    private TimerX _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 每天随机时刻执行，间隔1小时
        _timer = new TimerX(DoWork, null,
            DateTime.Today.AddMinutes(Rand.Next(60)),
            3600 * 1000)
        { Async = true };

        // 启动后10秒临时执行一次
        TimerX.Delay(DoWork, 10_000);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.TryDispose();
        return Task.CompletedTask;
    }

    private void DoWork(Object state)
    {
        TrimData();    // 清理日志数据
        TrimFile();    // 清理备份文件
    }
}
```

### 执行策略

| 参数 | 值 | 说明 |
|------|-----|------|
| 首次执行 | 启动后 10 秒 | 通过 `TimerX.Delay` 快速触发一次 |
| 定时执行 | 每天随机分钟 | `DateTime.Today.AddMinutes(Rand.Next(60))` 避免集群同时执行 |
| 执行间隔 | 3600 秒 | 每小时检查一次 |
| 异步执行 | 是 | `Async = true` 不阻塞其他定时任务 |

## 配置参数

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `DataRetention` | Int32 | 30 | 日志数据保留天数。0 或负数表示不清理 |
| `FileRetention` | Int32 | 15 | 备份文件保留天数。0 或负数表示不清理 |
| `FileRetentionSize` | Int32 | 1024 | 保留文件最小大小（KB）。小于此值的文件不删除 |

### appsettings.json 配置示例

```json
{
  "CubeSetting": {
    "DataRetention": 30,
    "FileRetention": 15,
    "FileRetentionSize": 1024
  }
}
```

## 日志数据清理

`TrimData()` 方法清理以下日志表的过期数据：

### 清理目标

| 实体 | 说明 | 清理条件 |
|------|------|---------|
| `OAuthLog` | OAuth 认证日志 | `CreateTime < 保留时间点` |
| `AppLog` | 应用操作日志 | `CreateTime < 保留时间点` |
| `Log`（XLog） | 系统日志 | `ID < SnowFlakeId(保留时间点)` |

### 清理实现

```csharp
private void TrimData()
{
    var set = setting;
    if (set.DataRetention <= 0) return;  // 禁用时跳过

    var time = DateTime.Now.AddDays(-set.DataRetention);

    using var span = tracer?.NewSpan("DataRetention", new { time });
    try
    {
        // 1. 清理 OAuth 日志
        var rs = OAuthLog.DeleteBefore(time);
        XTrace.WriteLine("删除[{0}]之前的 OAuthLog 共：{1:n0}",
            time.ToFullString(), rs);

        // 2. 清理应用日志
        rs = AppLog.DeleteBefore(time);
        XTrace.WriteLine("删除[{0}]之前的 AppLog 共：{1:n0}",
            time.ToFullString(), rs);

        // 3. 清理系统日志（使用 SnowFlake ID 优化）
        rs = DeleteLogBefore(time);
        XTrace.WriteLine("删除[{0}]之前的 Log 共：{1:n0}",
            time.ToFullString(), rs);
    }
    catch (Exception ex)
    {
        span?.SetError(ex, null);
    }
}
```

### SnowFlake ID 优化

系统日志表使用雪花算法（SnowFlake）生成 ID，ID 中包含时间信息。清理时通过 ID 范围过滤代替时间字段过滤，充分利用主键索引，性能远超按时间字段查询。

```csharp
static Int32 DeleteLogBefore(DateTime date)
{
    // 通过 SnowFlake 工厂将时间转换为 ID 范围
    var where = XLog._.ID < XLog.Meta.Factory.Snow.GetId(date)
               & XLog._.CreateUserID == 0;

    try
    {
        return XLog.Delete(where);
    }
    catch (XSqlException)
    {
        // SQLite 大量删除可能异常，自动降级为分批删除
        var rs = 0;
        for (var i = 0; i < 100; i++)
        {
            var list = XLog.FindAll(where, null, null, 0, 10000);
            if (list.Count == 0) break;
            rs += list.Delete();
        }
        return rs;
    }
}
```

### SQLite 降级处理

SQLite 数据库在执行大批量 DELETE 时可能抛出异常（锁超时、日志溢出等）。`DeleteLogBefore` 方法自动捕获 `XSqlException` 并降级为分批删除模式：

| 参数 | 值 | 说明 |
|------|-----|------|
| 每批大小 | 10,000 条 | 每次查询并删除 10,000 条记录 |
| 最大循环 | 100 次 | 最多循环 100 次（即最多清理 100 万条） |
| 退出条件 | 查询结果为空 | 所有符合条件的记录已清理完毕 |

## 备份文件清理

`TrimFile()` 方法清理过期的数据库备份文件。

### 清理逻辑

```csharp
private void TrimFile()
{
    var set = setting;
    if (set.FileRetention <= 0) return;  // 禁用时跳过

    // 获取备份目录
    var di = NewLife.Setting.Current.BackupPath.AsDirectory();
    if (!di.Exists) return;

    var minSize = set.FileRetentionSize * 1024;  // 最小保护大小
    var time = DateTime.Now.AddDays(-set.FileRetention);

    using var span = tracer?.NewSpan("FileRetention", new { time });
    try
    {
        foreach (var fi in di.GetAllFiles("*.*", false))
        {
            // 保护小于限制大小的文件
            if (minSize > 0 && fi.Length < minSize) continue;

            // 解析文件名中的日期
            var name = fi.Name;
            var p = name.LastIndexOf('_');
            if (p > 0)
            {
                var p2 = name.LastIndexOf('.');
                if (p2 > 0)
                {
                    var dt = name.Substring(p + 1, p2 - p - 1).ToDateTime();

                    if (dt.Year > 2000 && dt < time)
                    {
                        XTrace.WriteLine($"删除[{time.ToFullString()}]之前的备份文件：{fi.Name}");
                        LogProvider.Provider?.WriteLog("FileRetention", "Delete",
                            true, $"删除备份文件：{fi.Name}");
                        try { fi.Delete(); }
                        catch (Exception ex) { XTrace.WriteLine(ex.Message); }
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        span?.SetError(ex, null);
    }
}
```

### 文件命名约定

备份文件需遵循命名规则 `{名称}_{yyyyMMdd}.{扩展名}`，服务通过解析文件名中最后一个下划线后的日期来判断文件年龄。

| 文件名示例 | 解析日期 | 说明 |
|-----------|---------|------|
| `MyDB_20250101.bak` | 2025-01-01 | 数据库备份 |
| `Log_20241215.zip` | 2024-12-15 | 日志压缩包 |
| `config.json` | 无法解析 | 不含日期，跳过 |

### 保护机制

| 保护条件 | 说明 |
|---------|------|
| 文件大小 < `FileRetentionSize` KB | 小文件不删除（如配置文件、小型备份） |
| 文件名不含日期 | 无法解析日期的文件跳过 |
| 日期年份 < 2000 | 无效日期跳过 |
| 删除失败 | 捕获异常并记录，不中断后续清理 |

## 链路追踪

`DataRetentionService` 集成了链路追踪（Tracer），每次清理操作会创建追踪 Span：

| Span 名称 | 参数 | 说明 |
|-----------|------|------|
| `DataRetention` | `{ time }` | 日志数据清理 |
| `FileRetention` | `{ time }` | 备份文件清理 |

清理结果通过 `XTrace.WriteLine` 输出到日志，可在管理后台查看。

## 最佳实践

1. **生产环境推荐**：`DataRetention=30`（日志保留 30 天），`FileRetention=15`（备份保留 15 天）
2. **磁盘敏感环境**：适当缩短保留天数，如 `DataRetention=7`
3. **审计合规**：如需长期保留日志，设置较大的 `DataRetention` 值或设为 0 禁用清理
4. **SQLite 用户**：无需额外配置，服务自动处理 SQLite 的批量删除限制
5. **备份保护**：通过 `FileRetentionSize` 保护重要的小型配置备份，默认保护 1MB 以下文件
6. **集群部署**：各节点独立执行清理，通过随机延迟避免同时操作数据库
