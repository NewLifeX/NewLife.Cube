using System.Runtime.CompilerServices;
using NewLife.Log;

namespace NewLife.Cube.Tests;

/// <summary>测试程序集初始化器。进程早期固定日志提供者与等级，避免日志链路触发配置初始化</summary>
/// <remarks>
/// NewLife.Core 12.0.2026.1115 起，Logger.Level 未显式赋值时会读 Setting.Current，
/// 而配置初始化本身要写日志（Config.Create → Save → FileConfigProvider.OnWrite → XTrace.WriteLine → Level），
/// 无配置文件时形成 Setting → OnWrite → Level → Setting 的递归并栈溢出（测试宿主直接崩溃）。
/// 此处固定日志等级，使日志链路不再触碰 Setting。根治方案见 NewLife.Core Logger.Level 改用 TryGetCurrent。
/// </remarks>
internal static class TestLogInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        try
        {
            XTrace.UseConsole(true, false);
            XTrace.Log.Level = LogLevel.Info;
        }
        catch
        {
            // 日志初始化失败不阻断测试
        }
    }
}
