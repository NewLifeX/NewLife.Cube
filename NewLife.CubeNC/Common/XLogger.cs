using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;

namespace NewLife.Cube
{
    /// <summary>XLog日志扩展</summary>
    public static class XLoggerExtensions
    {
        /// <summary>添加XLog日志提供者</summary>
        /// <param name="builder"></param>
        /// <param name="logger">要映射的目标日志，默认XTrace.Log</param>
        /// <returns></returns>
        public static ILoggingBuilder AddXLog(this ILoggingBuilder builder, NewLife.Log.ILog logger = null)
        {
            //builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, XLoggerProvider>());

            if (logger == null) logger = Log.XTrace.Log;

            var provider = new XLoggerProvider { Logger = logger };
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider>(provider));

            return builder;
        }
    }

    /// <summary>XLog日志提供者</summary>
    [ProviderAlias("XLog")]
    public class XLoggerProvider : DisposeBase, ILoggerProvider
    {
        /// <summary>日志</summary>
        public NewLife.Log.ILog Logger { get; set; }

        private XLogger _log;
        /// <summary>创建日志</summary>
        /// <param name="categoryName"></param>
        /// <returns></returns>
        public ILogger CreateLogger(String categoryName)
        {
            if (_log != null) return _log;

            var log = Logger ?? Log.XTrace.Log;
            if (log is NewLife.Log.CompositeLog cp)
            {
                var tf = cp.Get<NewLife.Log.TextFileLog>();
                if (tf != null) log = tf;
            }

            return _log = new XLogger { Logger = log };
        }

        ///// <summary>销毁</summary>
        //public void Dispose() { }
    }

    /// <summary>XLog日志</summary>
    public class XLogger : ILogger
    {
        /// <summary>日志</summary>
        public NewLife.Log.ILog Logger { get; set; }

        /// <summary>区域开始</summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        /// <returns></returns>
        public IDisposable BeginScope<TState>(TState state) => null;

        /// <summary>是否启用指定日志等级</summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public Boolean IsEnabled(LogLevel logLevel)
        {
            //var set = NewLife.Setting.Current;
            //return logLevel >= (LogLevel)set.LogLevel;

            return Logger != null && Logger.Enable && logLevel >= (LogLevel)Logger.Level;
        }

        /// <summary>写日志</summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="logLevel"></param>
        /// <param name="eventId"></param>
        /// <param name="state"></param>
        /// <param name="exception"></param>
        /// <param name="formatter"></param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, String> formatter)
        {
            if (!Logger.Enable) return;

            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

            var txt = formatter(state, exception);
            if (txt.IsNullOrEmpty() && exception == null) return;

            if (exception != null) txt = exception.GetTrue()?.Message;

            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    Logger.Debug(txt);
                    break;
                case LogLevel.Information:
                    Logger.Info(txt);
                    break;
                case LogLevel.Warning:
                    Logger.Warn(txt);
                    break;
                case LogLevel.Error:
                    Logger.Error(txt);
                    break;
                case LogLevel.Critical:
                    Logger.Fatal(txt);
                    break;
                case LogLevel.None:
                    break;
                default:
                    break;
            }

            //if (exception != null) Logger.Error("{0}", exception);
        }
    }
}