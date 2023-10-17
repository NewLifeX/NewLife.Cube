using System.ComponentModel;
using System.Diagnostics;
using NewLife.Caching;
using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;
using XCode.DataAccessLayer;
using XCode.Membership;
using IHostedService = Microsoft.Extensions.Hosting.IHostedService;

namespace NewLife.Cube.Services;

/// <summary>定时作业服务</summary>
public class JobService : IHostedService
{
    #region 核心控制

    private static readonly IList<MyJob> _jobs = new List<MyJob>();
    private readonly IServiceProvider _serviceProvider;
    private readonly ITracer _tracer;

    /// <summary>实例化作业服务</summary>
    /// <param name="serviceProvider"></param>
    /// <param name="tracer"></param>
    public JobService(IServiceProvider serviceProvider, ITracer tracer)
    {
        _tracer = tracer;
        _serviceProvider = serviceProvider;
    }

    private static TimerX _timer;
    /// <summary>启动</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new TimerX(DoJob, null, 1_000, 600_000);

        return Task.CompletedTask;
    }

    /// <summary>停止</summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.TryDispose();
        _jobs.TryDispose();
        _jobs.Clear();

        return Task.CompletedTask;
    }

    /// <summary>唤醒作业调度，作业配置有变更</summary>
    public static void Wake() => _timer.SetNext(1_000);

    /// <summary>唤醒具体作业</summary>
    /// <param name="jobId"></param>
    /// <param name="ms"></param>
    public static void Wake(Int32 jobId, Int32 ms)
    {
        var job = _jobs.FirstOrDefault(e => e.Job.Id == jobId);
        job?.Wake(ms);
    }

    private void DoJob(Object state)
    {
        if (_jobs.Count == 0)
        {
            // 添加默认作业
            CronJob.Add(null, RunSql, "15 * * * * ? *", false);
            CronJob.Add(null, BackupDb, "5 0 0 * * ? *", false);
        }

        var list = CronJob.FindAll();
        foreach (var item in list)
        {
            var job = _jobs.FirstOrDefault(e => e.Job.Id == item.Id);
            if (job == null)
            {
                //将ICacheProvider 改为IServiceProvider注入，避免没有星辰注册导致的Job注入错误
                job = new MyJob { Job = item, CacheProvider = _serviceProvider.GetService<ICacheProvider>(), Tracer = _tracer };
                _jobs.Add(job);
            }
            job.Job = item;

            try
            {
                if (item.Enable)
                    job.Start();
                else
                    job.Stop();
            }
            catch (Exception ex)
            {
                WriteLog("控制", false, $"作业[{item.Name}/{item.DisplayName}]失败，{ex.Message}", item);

                item.Enable = false;
                item.Update();
            }
        }

        //// 如果没有作业，10分钟跑一次
        //_timer.Period = list.Any(e => e.Enable) ? 60_000 : 3600_000;
    }
    #endregion

    #region 辅助
    internal static void WriteLog(String action, Boolean success, String remark, CronJob job)
    {
        if (!job.EnableLog) return;

        var log = LogProvider.Provider.CreateLog("JobService", action, success, remark);
        if (job != null) log.LinkID = job.Id;
        log.TraceId = DefaultSpan.Current?.TraceId;

        log.SaveAsync();
    }
    #endregion

    #region 常用任务
    /// <summary>运行Sql语句，格式 connName#sql</summary>
    /// <param name="argument"></param>
    [DisplayName("运行Sql")]
    [Description("参数格式 connName#sql")]
    public static void RunSql(String argument)
    {
        //var argument = job?.Argument;
        if (argument.IsNullOrEmpty()) return;

        var p = argument.IndexOf('#');
        if (p <= 0) return;

        var connName = argument[..p].Trim();
        var sql = argument[(p + 1)..].Trim();

        var message = $"执行 在[{connName}]上执行SQL。";
        var success = true;
        try
        {
            var rs = DAL.Create(connName).Execute(sql);
            message += "返回：" + rs;
        }
        catch (Exception ex)
        {
            success = false;
            message += ex.ToString();
        }

        var job = TimerX.Current?.State as CronJob;
        WriteLog(nameof(RunSql), success, message, job);
    }

    /// <summary></summary>
    /// <param name="connNames"></param>
    [DisplayName("备份数据库")]
    [Description("参数是连接名connName，多个逗号隔开。仅支持SQLite")]
    public static void BackupDb(String connNames)
    {
        var ns = connNames.Split(",", ";");
        foreach (var name in ns)
        {
            if (DAL.ConnStrs.ContainsKey(name))
            {
                // 仅支持备份SQLite
                var dal = DAL.Create(name);
                if (dal.DbType == DatabaseType.SQLite)
                {
                    XTrace.WriteLine("在[{0}]上备份数据库", name);

                    var sw = Stopwatch.StartNew();

                    //var bak = dal.Db.CreateMetaData().SetSchema(DDLSchema.BackupDatabase, dal.ConnName, null, false);
                    var bak = dal.Db.CreateMetaData().Invoke("Backup", dal.ConnName, null, false);

                    sw.Stop();

                    var job = TimerX.Current?.State as CronJob;
                    WriteLog(nameof(BackupDb), true, $"备份数据库 {name} 到 {bak}，耗时 {sw.Elapsed}", job);
                }
            }
        }
    }
    #endregion
}

/// <summary>定时作业项</summary>
internal class MyJob : IDisposable
{
    public CronJob Job { get; set; }

    public ICacheProvider CacheProvider { get; set; }

    public ITracer Tracer { get; set; }

    private TimerX _timer;
    private String _id;
    private Action<String> _action;

    public void Dispose() => Stop();

    public void Start()
    {
        var job = Job;
        //using var span = Tracer?.NewSpan($"job:{job}:Start");
        //try
        //{
        // 参数检查
        var expession = job.Cron;
        if (expession.IsNullOrEmpty()) throw new ArgumentNullException(nameof(job.Cron));

        var cmd = job.Method;
        if (cmd.IsNullOrEmpty()) throw new ArgumentNullException(nameof(job.Method));

        // 标识相同，不要处理
        var id = $"{expession}@{cmd}";
        if (id == _id && _timer != null) return;

        var cron = new Cron();
        if (!cron.Parse(expession)) throw new InvalidOperationException($"无效表达式 {expession}");

        // 找到类和方法
        var p = cmd.LastIndexOf('.');
        if (p <= 0) throw new InvalidOperationException($"无效作业方法 {cmd}");

        var type = cmd[..p].GetTypeEx();
        var method = type?.GetMethodEx(cmd[(p + 1)..]);
        if (method == null || !method.IsStatic) throw new InvalidOperationException($"无效作业方法 {cmd}");

        _action = method.As<Action<String>>();
        if (_action == null) throw new InvalidOperationException($"无效作业方法 {cmd}");

        JobService.WriteLog("启用", true, $"作业[{job.Name}]，定时 {job.Cron}，方法 {job.Method}", job);

        // 实例化定时器，原定时器销毁
        _timer.TryDispose();
        _timer = new TimerX(DoJobWork, job, expession) { Async = true, Tracer = Tracer };

        job.NextTime = _timer.NextTime;
        job.Update();

        _id = id;
        //}
        //catch (Exception ex)
        //{
        //    span?.SetError(ex, null);

        //    throw;
        //}
    }

    public void Stop()
    {
        if (_timer != null)
        {
            //using var span = Tracer?.NewSpan($"job:{Job}:Stop");

            JobService.WriteLog("停用", true, $"作业[{Job.Name}]", Job);

            _timer.TryDispose();
            _timer = null;
        }

        _id = null;
    }

    public void Wake(Int32 ms) => _timer?.SetNext(ms);

    private Boolean CheckRunning(CronJob job)
    {
        // 检查分布式锁，避免多节点重复执行
        var key = $"Job:{job.Id}";
        if (CacheProvider!=null&&!CacheProvider.Cache.Add(key, job.Name, 10)) return false;

        // 有时候可能并没有配置Redis，借助数据库事务实现去重，需要20230804版本的XCode
        using var tran = CronJob.Meta.CreateTrans();

        // 如果短时间内重复执行，跳过
        var job2 = CronJob.FindByKey(job.Id);
        if (job2 != null && job2.LastTime.AddSeconds(10) > DateTime.Now) return false;

        job2.LastTime = DateTime.Now;
        job2.Update();

        tran.Commit();

        return true;
    }

    private void DoJobWork(Object state)
    {
        var job = Job;

        // 检查分布式锁，避免多节点重复执行
        if (!CheckRunning(job))
        {
            var set = CubeSetting.Current;
            if (set.Debug)
                JobService.WriteLog(job.Name, false, "分布式锁检查失败，跳过执行", job);
            return;
        }

        job.LastTime = DateTime.Now;

        using var span = Tracer?.NewSpan($"job:{job}", job);
        var sw = Stopwatch.StartNew();
        var message = "";
        var success = true;
        try
        {
            _action?.Invoke(job.Argument);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            XTrace.WriteException(ex);

            success = false;
            message = ex.ToString();
        }
        sw.Stop();
        message += $" 耗时 {sw.Elapsed}";

        JobService.WriteLog(job.Name, success, message, job);

        job.NextTime = _timer.Cron.GetNext(_timer.NextTime);
        job.Update();
    }
}