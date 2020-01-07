using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Serialization;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube.Charts
{
    /// <summary>ECharts实例</summary>
    public class ECharts
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; } = "c1";

        /// <summary>宽度。单位px，负数表示百分比，默认-100</summary>
        public Int32 Width { get; set; } = -100;

        /// <summary>高度。单位px，负数表示百分比，默认300px</summary>
        public Int32 Height { get; set; } = 300;

        /// <summary>标题。字符串或匿名对象</summary>
        public ChartTitle Title { get; set; }

        /// <summary>提示</summary>
        public Object Tooltip { get; set; } = new Object();

        /// <summary>提示</summary>
        public Object Legend { get; set; }

        /// <summary>X轴</summary>
        public Object XAxis { get; set; }

        /// <summary>Y轴</summary>
        public Object YAxis { get; set; }

        /// <summary>系列数据</summary>
        public IList<Series> Series { get; set; }
        #endregion

        #region 方法
        /// <summary>添加系列数据</summary>
        /// <param name="series"></param>
        public void Add(Series series)
        {
            if (Series == null) Series = new List<Series>();

            Series.Add(series);
        }

        /// <summary>添加系列数据</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="field"></param>
        /// <param name="type"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public Series Add<T>(IList<T> list, FieldItem field, String type = "line", Boolean smooth = false) where T : IEntity
        {
            if (type.IsNullOrEmpty()) type = "line";

            var sr = new Series
            {
                Name = field.DisplayName ?? field.Name,
                Type = type,
                Data = list.Select(e => e[field.Name]).ToArray(),
                Smooth = smooth,
            };

            Add(sr);
            return sr;
        }

        /// <summary>设置X轴</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="field"></param>
        /// <param name="selector"></param>
        public void SetX<T>(IList<T> list, FieldItem field, Func<T, String> selector = null) where T : IEntity
        {
            XAxis = list.Select(e => selector == null ? e[field.Name] + "" : selector(e)).ToArray();
            YAxis = new { type = "value" };
        }

        /// <summary>设置工具栏</summary>
        /// <param name="trigger">
        /// 触发类型。
        /// item, 数据项图形触发，主要在散点图，饼图等无类目轴的图表中使用。
        /// axis, 坐标轴触发，主要在柱状图，折线图等会使用类目轴的图表中使用。
        /// none, 什么都不触发。
        /// </param>
        /// <param name="axisPointerType">坐标轴指示器配置项。cross，坐标系会自动选择显示哪个轴的 axisPointer</param>
        /// <param name="backgroundColor"></param>
        public void SetTooltip(String trigger = "axis", String axisPointerType = "cross", String backgroundColor = "#6a7985")
        {
            Tooltip = new
            {
                trigger = trigger,
                axisPointer = new
                {
                    type = axisPointerType,
                    label = new
                    {
                        backgroundColor = backgroundColor
                    }
                },
            };
        }

        /// <summary>构建选项Json</summary>
        /// <returns></returns>
        public String Build()
        {
            var dic = new Dictionary<String, Object>();

            // 标题
            var title = Title;
            if (title != null) dic[nameof(title)] = title;

            // 提示
            var tooltip = Tooltip;
            if (tooltip != null) dic[nameof(tooltip)] = tooltip;

            // 提示
            var legend = Legend;
            if (legend == null) legend = Series.Select(e => e.Name).ToArray();
            if (legend != null)
            {
                if (legend is String str)
                    legend = new { data = new[] { str } };
                else if (legend is String[] ss)
                    legend = new { data = ss };

                dic[nameof(legend)] = legend;
            }

            // X轴
            var xAxis = XAxis;
            if (xAxis != null)
            {
                if (xAxis is String str)
                    xAxis = new { data = new[] { str } };
                else if (xAxis is String[] ss)
                    xAxis = new { data = ss };

                dic[nameof(xAxis)] = xAxis;
            }

            // Y轴
            var yAxis = YAxis;
            if (yAxis != null) dic[nameof(yAxis)] = yAxis;

            // 系列数据
            var series = Series;
            if (series != null) dic[nameof(series)] = series;

            return dic.ToJson(false, false, true);
        }
        #endregion
    }
}