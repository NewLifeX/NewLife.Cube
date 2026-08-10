using System.ComponentModel;
using System.Reflection;
using NewLife.AI.Tools;
using NewLife.Cube.AI;
using Xunit;

namespace XUnitTest;

/// <summary>系统信息工具服务单元测试 — 全局 AI 端点 get_system_info 工具</summary>
public class SystemInfoToolServiceTests
{
    [Fact]
    [DisplayName("GetSystemInfo - 返回服务器运行指标")]
    public void GetSystemInfo_ReturnsMetrics()
    {
        var tools = new SystemInfoToolService();
        var json = tools.GetSystemInfo();

        Assert.Contains("cpu", json);
        Assert.Contains("memoryTotal", json);
        Assert.Contains("workingSet", json);
        Assert.Contains("machineName", json);
    }

    [Fact]
    [DisplayName("GetSystemInfo - 带工具描述特性，可被 ToolRegistry 注册")]
    public void GetSystemInfo_HasToolDescription()
    {
        var method = typeof(SystemInfoToolService).GetMethod(nameof(SystemInfoToolService.GetSystemInfo));
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<ToolDescriptionAttribute>());
    }
}
