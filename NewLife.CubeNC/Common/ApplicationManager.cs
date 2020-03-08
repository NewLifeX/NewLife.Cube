using System;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NewLife.Log;

namespace NewLife.Cube
{
    /// <summary>应用管理</summary>
    public class ApplicationManager
    {
        private static ApplicationManager _appManager;
        private CancellationTokenSource _tokenSource;
        private Boolean _running;
        private Boolean _restart;

        /// <summary>
        /// 启动时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>是否重启中</summary>
        public Boolean Restarting => _restart;

        /// <summary>实例化</summary>
        public ApplicationManager()
        {
            _running = false;
            _restart = false;
        }

        /// <summary>加载实例</summary>
        /// <returns></returns>
        public static ApplicationManager Load()
        {
            if (_appManager == null)
                _appManager = new ApplicationManager();

            return _appManager;
        }

        /// <summary>启动</summary>
        /// <param name="host"></param>
        public void Start(IHost host)
        {
            if (_running)
                return;

            if (_tokenSource != null && _tokenSource.IsCancellationRequested)
                return;

            _tokenSource = new CancellationTokenSource();
            _tokenSource.Token.ThrowIfCancellationRequested();
            _running = true;

            StartTime = DateTime.Now;
            var t = host.RunAsync(_tokenSource.Token);
            XTrace.WriteLine("系统已启动");
            t.Wait();
            XTrace.WriteLine("系统已停止");
        }

        /// <summary>停止</summary>
        public void Stop()
        {
            if (!_running)
                return;
            _tokenSource.Cancel();
            _running = false;
        }

        /// <summary>重启</summary>
        public void Restart()
        {
            XTrace.WriteLine("系统重启中");
            Stop();
            _restart = true;
            _tokenSource = null;
        }
    }
}