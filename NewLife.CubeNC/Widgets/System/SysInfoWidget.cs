using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using NewLife.Reflection;

namespace NewLife.Cube.Widgets.System;

/// <summary>系统信息摘要。操作系统/运行时/应用启动等，提供完整服务器信息页链接</summary>
[Widget("SysInfo", "系统信息", Icon = "fa-server", Cols = 6, Sort = 50, Category = "系统", AdminOnly = true)]
public class SysInfoWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    /// <returns>系统信息摘要匿名对象</returns>
    public Object GetData()
    {
        var mi = MachineInfo.Current ?? new MachineInfo();
        var asm = Assembly.GetExecutingAssembly();
        var att = asm.GetCustomAttribute<TargetFrameworkAttribute>();
        var ver = att?.FrameworkDisplayName ?? att?.FrameworkName;
        var process = Process.GetCurrentProcess();
        var startTime = DateTime.Now.AddMilliseconds(-Environment.TickCount64);

        return new
        {
            OSName = mi.OSName,
            OSVersion = mi.OSVersion,
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            Product = mi.Product,
            Processor = mi.Processor,
            CoreCount = Environment.ProcessorCount,
            Framework = ver,
            ProcessName = process.ProcessName,
            StartTime = startTime.ToFullString(),
            AssemblyCount = AssemblyX.GetAssemblies(null).Count(),
            FullUrl = "/Admin/Index/Main",
        };
    }
}
