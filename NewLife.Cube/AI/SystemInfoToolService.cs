using System.ComponentModel;
using System.Diagnostics;
using NewLife.AI.Tools;
using NewLife.Serialization;

namespace NewLife.Cube.AI;

/// <summary>系统信息工具服务。提供服务器运行指标供 AI 健康诊断使用</summary>
/// <remarks>
/// 供全局 AI 端点（<see cref="Controllers.AiController"/>）注册 <c>get_system_info</c> 工具，
/// 非实体页面（首页、魔方设置、系统信息等）对话时可按需采集服务器指标。
/// 实体页面由 <see cref="CubeTools{TEntity}"/> 提供同名工具，两者共用 <see cref="BuildSystemInfo"/> 逻辑。
/// 工具方法为 virtual，二次开发者可继承重写。
/// </remarks>
public class SystemInfoToolService
{
    /// <summary>收集服务器运行指标，供系统健康诊断使用</summary>
    [ToolDescription("get_system_info", ReadOnly = true)]
    [DisplayName("获取系统状态")]
    [Description("获取当前服务器运行指标，包括 CPU 使用率、内存、进程工作集、系统信息等，供系统健康诊断使用")]
    public virtual String GetSystemInfo() => BuildSystemInfo();

    /// <summary>构建服务器运行指标 JSON。实体工具集与全局工具服务共用同一实现，避免重复</summary>
    /// <returns>运行指标 JSON 字符串</returns>
    public static String BuildSystemInfo()
    {
        var mi = MachineInfo.Current ?? new MachineInfo();
        var process = Process.GetCurrentProcess();
        var sysInfo = new
        {
            cpu = $"{mi.CpuRate:P0}",
            temperature = mi.Temperature,
            memoryAvailable = $"{mi.AvailableMemory / 1024 / 1024:N0}M",
            memoryTotal = $"{mi.Memory / 1024 / 1024:N0}M",
            workingSet = $"{process.WorkingSet64 / 1024 / 1024:N0}M",
            openTime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss"),
            os = mi.OSName + " " + mi.OSVersion,
            machineName = Environment.MachineName,
        };
        return sysInfo.ToJson();
    }
}
