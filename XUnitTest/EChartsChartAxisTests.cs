using System;
using System.Collections.Generic;
using NewLife.Cube;
using NewLife.Cube.Charts;
using NewLife.Cube.ViewModels;
using XCode.Membership;
using Xunit;

namespace XUnitTest;

/// <summary>快捷图表坐标轴测试</summary>
/// <remarks>饼图不走直角坐标系，不能残留单独坐标轴，否则 ECharts 5.6 渲染报错导致图表不显示。</remarks>
public class EChartsChartAxisTests
{
    private sealed class PieChartController : ReadOnlyEntityController<Area>
    {
        public ECharts TestAddChart(IList<Area> data, DataField xAxis, String yAxis, DataField[] yFields, SeriesTypes seriesType, String position)
            => AddChart(data, xAxis, yAxis, yFields, seriesType, position);
    }

    [Fact(DisplayName = "图表 - 饼图不生成X轴与Y轴")]
    public void PieChart_HasNoCartesianAxis()
    {
        // 构造函数可能按 PageSetting.OrderByKey 查询计数，测试环境无该表，临时关闭避免 DB 依赖
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            var controller = new PieChartController();
            var xAxis = new DataField { Name = nameof(Area.Kind), DisplayName = "类型", Type = typeof(String) };
            var yField = new DataField { Name = nameof(Area.Id), DisplayName = "编码", Type = typeof(Int32) };
            var list = new List<Area>
            {
                new() { Id = 1, Kind = "省" },
                new() { Id = 2, Kind = "市" },
            };

            var chart = controller.TestAddChart(list, xAxis, null, [yField], SeriesTypes.Pie, "bottom");

            Assert.Empty(chart.XAxis);
            Assert.Empty(chart.YAxis);
            Assert.Single(chart.Series);
            Assert.Equal("类型", chart.Series[0].Name);
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }

    [Fact(DisplayName = "图表 - 柱状图保留X轴与Y轴")]
    public void BarChart_KeepsCartesianAxis()
    {
        var old = PageSetting.Global.OrderByKey;
        try
        {
            PageSetting.Global.OrderByKey = false;

            var controller = new PieChartController();
            var xAxis = new DataField { Name = nameof(Area.Kind), DisplayName = "类型", Type = typeof(String) };
            var yField = new DataField { Name = nameof(Area.Id), DisplayName = "编码", Type = typeof(Int32) };
            var list = new List<Area>
            {
                new() { Id = 1, Kind = "省" },
                new() { Id = 2, Kind = "市" },
            };

            var chart = controller.TestAddChart(list, xAxis, null, [yField], SeriesTypes.Bar, "top");

            Assert.Single(chart.XAxis);
            Assert.Single(chart.YAxis);
            Assert.Single(chart.Series);
        }
        finally
        {
            PageSetting.Global.OrderByKey = old;
        }
    }
}
