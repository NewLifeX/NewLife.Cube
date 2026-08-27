using NewLife;

namespace NewLife.Cube.Widgets.System;

/// <summary>CPU使用率。工作台 KPI 指标</summary>
[Widget("CpuRate", "CPU使用率", Icon = "fa-tachometer", Cols = 2, Sort = 60, Category = "系统", AdminOnly = true, Color = "orange", WidgetType = WidgetTypes.Kpi)]
public class CpuRateWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData()
    {
        var mi = MachineInfo.Current ?? new MachineInfo();
        var total = mi.Memory / 1024 / 1024;
        var used = total - mi.AvailableMemory / 1024 / 1024;
        var memRate = total <= 0 ? 0d : (Double)used / total * 100;

        return new
        {
            Value = Math.Round(mi.CpuRate * 100, 1).ToString("0.0") + "%",
            Trend = "内存 " + Math.Round(memRate, 1).ToString("0.0") + "%",
            Url = "/Admin/Index/Main",
        };
    }
}
