using System;
using System.ComponentModel;
using NewLife.Cube.Widgets;
using Xunit;

namespace XUnitTest;

/// <summary>KPI 数据归一化测试。验证组件返回的匿名类型被 WidgetManager 转换为公开 KpiModel，
/// 避免具体应用（外部程序集）组件数据在魔方工作台 dynamic 绑定失败</summary>
public class KpiModelTests
{
    [Fact]
    [DisplayName("匿名类型 KPI 数据归一化为公开 KpiModel")]
    public void Normalize_Anonymous_ToKpiModel()
    {
        var info = new WidgetAttribute("TestA", "测试A")
        {
            WidgetType = WidgetTypes.Kpi,
            Type = typeof(AnonymousKpiWidget),
        };

        var data = new WidgetManager().GetData(info);

        var model = Assert.IsType<KpiModel>(data);
        Assert.Equal("123", model.Value);
        Assert.Equal("趋势描述", model.Trend);
        Assert.Equal("/test", model.Url);
    }

    [Fact]
    [DisplayName("KpiModel 数据原样保留不重复转换")]
    public void Normalize_KpiModel_Keep()
    {
        var info = new WidgetAttribute("TestB", "测试B")
        {
            WidgetType = WidgetTypes.Kpi,
            Type = typeof(ModelKpiWidget),
        };

        var data = new WidgetManager().GetData(info);

        var model = Assert.IsType<KpiModel>(data);
        Assert.Equal("456", model.Value);
        Assert.Equal("/model", model.Url);
    }

    [Fact]
    [DisplayName("非 KPI 类型组件数据不做归一化")]
    public void Normalize_NonKpi_KeepRaw()
    {
        var info = new WidgetAttribute("TestC", "测试C")
        {
            WidgetType = WidgetTypes.Html,
            Type = typeof(HtmlWidget),
        };

        var data = new WidgetManager().GetData(info);

        Assert.Equal("<b>html</b>", data);
    }
}

/// <summary>测试用匿名 KPI 组件，模拟外部程序集返回匿名类型的场景</summary>
[Widget("TestA", "测试A", WidgetType = WidgetTypes.Kpi)]
class AnonymousKpiWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData() => new { Value = "123", Trend = "趋势描述", Url = "/test" };
}

/// <summary>测试用公开模型 KPI 组件</summary>
[Widget("TestB", "测试B", WidgetType = WidgetTypes.Kpi)]
class ModelKpiWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData() => new KpiModel { Value = "456", Trend = "趋势", Url = "/model" };
}

/// <summary>测试用 HTML 组件</summary>
[Widget("TestC", "测试C", WidgetType = WidgetTypes.Html)]
class HtmlWidget : IWidget
{
    /// <summary>获取组件数据</summary>
    public Object GetData() => "<b>html</b>";
}
