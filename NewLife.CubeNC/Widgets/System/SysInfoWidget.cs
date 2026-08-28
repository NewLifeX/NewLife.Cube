using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using NewLife.Reflection;

namespace NewLife.Cube.Widgets.System;

/// <summary>系统信息摘要。操作系统/运行时/应用启动等，提供完整服务器信息页链接。KV 卡固定表格渲染</summary>
[Widget("SysInfo", "系统信息", Icon = "fa-server", Cols = 4, Sort = 100, Category = "系统", AdminOnly = true, WidgetType = WidgetTypes.Kv)]
public class SysInfoWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    /// <returns>系统信息键值字典，值为文本或 HTML</returns>
    public Object GetData()
    {
        var mi = MachineInfo.Current ?? new MachineInfo();
        var asm = Assembly.GetExecutingAssembly();
        var att = asm.GetCustomAttribute<TargetFrameworkAttribute>();
        var ver = att?.FrameworkDisplayName ?? att?.FrameworkName;
        var process = Process.GetCurrentProcess();
        var startTime = DateTime.Now.AddMilliseconds(-Environment.TickCount64);
        var asmCount = AssemblyX.GetAssemblies(null).Count();

        // Kv 卡返回键值字典，固定表格视图渲染；值可为 HTML
        var dic = new Dictionary<String, Object>
        {
            ["操作系统"] = $"{mi.OSName} {mi.OSVersion}",
            ["机器"] = $"{Environment.MachineName} / {Environment.UserName}",
            ["处理器"] = $"{mi.Processor}，{Environment.ProcessorCount} 核心",
            ["运行时"] = ver,
            ["应用"] = $"{process.ProcessName}，程序集 {asmCount} 个",
            ["启动时间"] = startTime.ToFullString(),
            ["更多信息"] = "<a href=\"/Admin/Index/Main\" target=\"_blank\">查看完整服务器信息 »</a>",
        };

        return dic;
    }
}
