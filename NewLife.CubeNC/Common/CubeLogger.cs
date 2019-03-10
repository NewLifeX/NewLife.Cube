using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using XCode.Membership;

namespace NewLife.Cube.Common
{
    /// <summary>
    /// 在这里可以做实时日志，每次请求都会有详细信息输出
    /// </summary>
    public class CubeLogger : ILogger
    {
        public CubeLogger()
        {
        }

        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var remark = formatter?.Invoke(state, exception) ?? state.ToString();

            // 忽略读取session的日志，避免下面WriteLog触发系统写日志操作导致无限循环写日志StackOverflowException
            if (remark.StartsWith("Accessing expired session"))
            {
                return;
            }

            WriteLog<TState>(eventId.ToString(), remark);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (Setting.Current.Debug)
            {
                return true;
            }

            if (logLevel > LogLevel.Warning)
            {
                return true;
            }

            return false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        private void WriteLog<TState>(String action, String remark, String ip = null)
        {
            // 此处记录到了审计日志，造成了污染。
            // 实际上每个请求的相关信息都会记录到框架自带的log，这个文件的记录只是其中一个，用来输出
            // 比如通过webstock实时反馈到web端，方便调试或查看请求过程
            //LogProvider.Provider.WriteLog(typeof(TState), action, remark, ip: ip);
        }
    }

    public class CubeLoggerProvider : ILoggerProvider
    {
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName) => new CubeLogger();
    }
}