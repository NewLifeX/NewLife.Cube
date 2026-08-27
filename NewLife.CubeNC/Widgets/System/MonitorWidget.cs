using System.Diagnostics;
using NewLife.Cube.Charts;

namespace NewLife.Cube.Widgets.System;

/// <summary>性能监控。CPU/内存实时曲线，由视图定时轮询 <c>/Admin/Index/MonitorData</c> 刷新</summary>
[Widget("Monitor", "性能监控", Icon = "fa-line-chart", Cols = 8, Sort = 70, Category = "系统", AdminOnly = true)]
public class MonitorWidget : IWidget
{
    /// <summary>获取组件数据。返回 ECharts 图表对象，由工作台收集到 ViewBag.Charts 触发布局加载 echarts</summary>
    /// <returns>ECharts 实例</returns>
    public Object GetData()
    {
        var mi = MachineInfo.Current ?? new MachineInfo();
        var process = Process.GetCurrentProcess();

        var chart = new ECharts
        {
            Name = "Monitor",
            Height = 320,
        };

        // 双Y轴：CPU百分比 + 内存MB
        chart.SetX(new Object[] { DateTime.Now.ToString("HH:mm:ss") });
        chart.SetY(new[] { "CPU %", "内存MB" }, "value", new[] { "{value}%", "{value}MB" });
        chart.SetLegend(new[] { "CPU", "内存" });
        // 注意：SetTooltip 存在 2 参（formatterScript）重载，需用 3 参版本指定 axisPointerType
        chart.SetTooltip("axis", "cross", "#6a7985");

        var cpu = new SeriesLine
        {
            Name = "CPU",
            YAxisIndex = 0,
            Data = [Math.Round(mi.CpuRate * 100, 1)],
        };
        chart.Add(cpu);

        var mem = new SeriesLine
        {
            Name = "内存",
            YAxisIndex = 1,
            Data = [(mi.Memory - mi.AvailableMemory) / 1024 / 1024],
        };
        chart.Add(mem);

        return chart;
    }
}
