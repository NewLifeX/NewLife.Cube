using System.Diagnostics;
using NewLife.Cube.Charts;

namespace NewLife.Cube.Widgets.System;

/// <summary>性能监控。CPU/内存实时曲线，由视图定时轮询 <c>/Admin/Index/MonitorData</c> 刷新</summary>
[Widget("Monitor", "性能监控", Icon = "fa-line-chart", Cols = 8, Sort = 70, Category = "系统", AdminOnly = true, WidgetType = WidgetTypes.Chart, ChartUrl = "/Admin/Index/MonitorData", ChartInterval = 5)]
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

        var now = DateTime.Now;
        // CPU / 内存统一为百分比，单 Y 轴 0~100，避免双轴尺度差异（内存MB vs CPU%）导致曲线视觉失真
        var cpuRate = Math.Round(mi.CpuRate * 100, 1);
        var memPct = mi.Memory > 0 ? Math.Round((mi.Memory - mi.AvailableMemory) * 100.0 / mi.Memory, 1) : 0;

        // 预置最近 1 分钟（12 个点）初始数据，避免刚打开只有 1 个点看不出曲线形状，随后由轮询自然替换
        var xs = new List<Object>();
        var cpus = new List<Object>();
        var mems = new List<Object>();
        for (var i = 11; i >= 0; i--)
        {
            xs.Add(now.AddSeconds(-5 * i).ToString("HH:mm:ss"));
            cpus.Add(cpuRate);
            mems.Add(memPct);
        }

        var xAxis = chart.SetX(xs);
        xAxis.BoundaryGap = false;

        var yAxis = chart.SetY("CPU / 内存 %", "value", "{value}%");
        yAxis.Min = 0;
        yAxis.Max = 100;
        chart.SetLegend(new[] { "CPU", "内存" });
        // 注意：SetTooltip 存在 2 参（formatterScript）重载，需用 3 参版本指定 axisPointerType
        chart.SetTooltip("axis", "cross", "#6a7985");

        // 平滑曲线 + 2px 线宽 + 渐变面积，对齐设计稿（smooth 无公共属性，经 Items 扩展字典注入）
        var cpu = new SeriesLine
        {
            Name = "CPU",
            Data = cpus.ToArray(),
            ShowSymbol = false,
        };
        cpu["smooth"] = true;
        cpu["lineStyle"] = new { width = 2, color = "#2b7dbc" };
        cpu["areaStyle"] = new
        {
            color = new
            {
                type = "linear",
                x = 0, y = 0, x2 = 0, y2 = 1,
                colorStops = new[] { new { offset = 0, color = "rgba(43,125,188,.25)" }, new { offset = 1, color = "rgba(43,125,188,0)" } },
            }
        };
        chart.Add(cpu);

        var mem = new SeriesLine
        {
            Name = "内存",
            Data = mems.ToArray(),
            ShowSymbol = false,
        };
        mem["smooth"] = true;
        mem["lineStyle"] = new { width = 2, color = "#27ae60" };
        mem["areaStyle"] = new
        {
            color = new
            {
                type = "linear",
                x = 0, y = 0, x2 = 0, y2 = 1,
                colorStops = new[] { new { offset = 0, color = "rgba(39,174,96,.22)" }, new { offset = 1, color = "rgba(39,174,96,0)" } },
            }
        };
        chart.Add(mem);

        return chart;
    }
}
