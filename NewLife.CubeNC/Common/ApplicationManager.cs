using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NewLife.Log;

namespace NewLife.Cube
{
    public class ApplicationManager
    {
        private static ApplicationManager _appManager; 
        private CancellationTokenSource _tokenSource;
        private bool _running;
        private bool _restart;
        
        
        /// <summary>
        /// 启动时间
        /// </summary>
        public DateTime StartTime { get; set; }
 
        public bool Restarting => _restart;
 
        public ApplicationManager()
        {
            _running = false;
            _restart = false;
         
        }
 
        public static ApplicationManager Load()
        {
            if (_appManager == null)
                _appManager = new ApplicationManager();
 
            return _appManager;
        }
 
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
 
        public void Stop()
        {
            if (!_running)
                return;
            _tokenSource.Cancel();
            _running = false;
            
        }
 
        public void Restart()
        {
            XTrace.WriteLine("系统重启中");
            Stop(); 
            _restart = true;
            _tokenSource = null;
            
        }
    }
}