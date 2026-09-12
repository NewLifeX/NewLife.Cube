using System.Runtime.CompilerServices;
using NewLife.Log;

namespace XUnitTest;

/// <summary>测试程序集初始化器。进程早期固定日志提供者，避免并行首触发 XTrace.InitLog 依赖 Setting 配置导致的 flaky 崩溃</summary>
/// <remarks>
/// 背景：XTrace.InitLog 内部访问 Setting.Current 无 try/catch，测试环境无配置文件时其静态构造抛异常，
/// 且 Setting 类型一旦静态构造失败在进程内永久损坏，连锁导致大量测试 TypeInitializationException（全量偶发 90+ 失败）。
/// 此处直接使用控制台日志（useFileLog=false，不读取 Log getter，绕过 Setting），此后 InitLog 短路不再触碰 Setting。
/// </remarks>
internal static class TestModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        try
        {
            XTrace.UseConsole(true, false);
        }
        catch
        {
            // 日志初始化失败不阻断测试
        }
    }
}
