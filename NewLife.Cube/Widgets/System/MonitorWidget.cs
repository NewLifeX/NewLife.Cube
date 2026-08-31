namespace NewLife.Cube.Widgets.System;

/// <summary>性能监控。CPU/内存实时曲线，API 版由前端轮询 <c>/Admin/Index/MonitorData</c> 刷新，
/// 此处仅注册部件元数据（图表数据不下发，避免工作台接口携带大体积图表配置）</summary>
[Widget("Monitor", "性能监控", Icon = "fa-line-chart", Cols = 8, Sort = 70, Category = "系统", AdminOnly = true, WidgetType = WidgetTypes.Chart)]
public class MonitorWidget : IWidget
{
    /// <summary>获取组件数据。图表数据由前端独立轮询 MonitorData 接口，返回空</summary>
    /// <returns>null</returns>
    public Object GetData() => null;
}
