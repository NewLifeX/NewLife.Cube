using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;
using XCode.DataAccessLayer;
using XCode.Membership;
using ILog = NewLife.Log.ILog;

namespace NewLife.Cube.Services
{
    /// <summary>定时作业服务</summary>
    public class JobService : IHostedService
    {
        #region 核心控制
        private static readonly ILog _log;
        private readonly ITracer _tracer;

        static JobService()
        {
            var log = LogProvider.Provider.AsLog("JobService");
            _log = new CompositeLog(XTrace.Log, log);
        }

        /// <summary>实例化作业服务</summary>
        /// <param name="tracer"></param>
        public JobService(ITracer tracer) => _tracer = tracer;

        private TimerX _timer;
        /// <summary>启动</summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new TimerX(DoJob, null, 1_000, 60_000);

            return Task.CompletedTask;
        }

        /// <summary>停止</summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.TryDispose();
            _timers.TryDispose();
            _timers.Clear();

            return Task.CompletedTask;
        }

        private readonly IList<MyJob> _timers = new List<MyJob>();

        private void DoJob(Object state)
        {
            if (_timers.Count == 0)
            {
                // 添加默认作业
                CronJob.Add(null, RunSql, "0 0 * * * ? *", false);
                CronJob.Add(null, BackupDb, "5 0 0 * * ? *", false);
            }

            var list = CronJob.FindAll();
            foreach (var item in list)
            {
                var job = _timers.FirstOrDefault(e => e.Job.Id == item.Id);
                if (job == null)
                {
                    job = new MyJob { Job = item, Tracer = _tracer, Log = _log };
                    _timers.Add(job);
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
                    _log.Error("控制 作业[{0}/{1}]失败，{2}", item.Name, item.DisplayName, ex.Message);

                    item.Enable = false;
                    item.Update();
                }
            }
        }
        #endregion

        #region 常用任务
        /// <summary>运行Sql语句，格式 connName#sql</summary>
        /// <param name="argument"></param>
        [DisplayName("运行Sql")]
        [Description("参数格式 connName#sql")]
        public static void RunSql(String argument)
        {
            if (argument.IsNullOrEmpty()) return;

            var p = argument.IndexOf('#');
            if (p <= 0) return;

            var connName = argument.Substring(0, p).Trim();
            var sql = argument.Substring(p + 1).Trim();
            _log.Info("执行 在[{0}]上执行SQL：{1}", connName, sql);
            try
            {
                DAL.Create(connName).Execute(sql);
            }
            catch (Exception ex)
            {
                _log.Error("执行 在[{0}]上执行SQL出错。{1}", connName, ex);
            }
        }

        /// <summary></summary>
        /// <param name="connName"></param>
        [DisplayName("备份数据库")]
        [Description("参数是连接名connName")]
        public static void BackupDb(String connName) => _log.Info("在[{0}]上备份数据库", connName);
        #endregion
    }

    /// <summary>定时作业项</summary>
    internal class MyJob : IDisposable
    {
        public CronJob Job { get; set; }

        public ITracer Tracer { get; set; }

        public ILog Log { get; set; }

        private TimerX _timer;
        private String _id;
        private Action<String> _action;

        public void Dispose() => Stop();

        public void Start()
        {
            // 参数检查
            var expession = Job.Cron;
            if (expession.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Job.Cron));

            var cmd = Job.Method;
            if (cmd.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Job.Method));

            // 标识相同，不要处理
            var id = $"{expession}@{cmd}";
            if (id == _id) return;

            var cron = new Cron();
            if (!cron.Parse(expession)) throw new InvalidOperationException($"无效表达式 {expession}");

            // 找到类和方法
            var p = cmd.LastIndexOf('.');
            if (p <= 0) throw new InvalidOperationException($"无效作业方法 {cmd}");

            var type = cmd.Substring(0, p).GetTypeEx();
            var method = type?.GetMethodEx(cmd.Substring(p + 1));
            if (method == null || !method.IsStatic) throw new InvalidOperationException($"无效作业方法 {cmd}");

            _action = method.As<Action<String>>();
            if (_action == null) throw new InvalidOperationException($"无效作业方法 {cmd}");

            Log.Info("加载 作业[{0}/{1}]，定时 {2}，方法 {3}", Job.Name, Job.DisplayName, Job.Cron, Job.Method);

            // 实例化定时器，原定时器销毁
            _timer.TryDispose();
            _timer = new TimerX(DoJobWork, null, expession) { Async = true };

            _id = id;
        }

        public void Stop()
        {
            if (_timer != null)
            {
                _timer.TryDispose();
                _timer = null;
            }

            _id = null;
        }

        private void DoJobWork(Object state) => _action?.Invoke(Job.Argument);
    }
}