using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Serialization;

namespace NewLife.Cube.Charts
{
    /// <summary>ECharts实例</summary>
    public class ECharts
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; } = "c1";

        /// <summary>标题。字符串或匿名对象</summary>
        public ChartTitle Title { get; set; }

        /// <summary>提示</summary>
        public Object Tooltip { get; set; }

        /// <summary>提示</summary>
        public Object Legend { get; set; }

        public Object XAxis { get; set; }
        public Object YAxis { get; set; }

        public IList<Series> Series { get; set; }
        #endregion

        #region 方法
        public void Add(Series series)
        {
            if (Series == null) Series = new List<Series>();

            Series.Add(series);
        }

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
            if (legend != null)
            {
                if (legend is String str)
                    legend = new { data = new[] { str } };
                else if (legend is String[] ss)
                    legend = new { data = ss };

                dic[nameof(legend)] = legend;
            }

            // 提示
            var xAxis = XAxis;
            if (xAxis != null)
            {
                if (xAxis is String str)
                    xAxis = new { data = new[] { str } };
                else if (xAxis is String[] ss)
                    xAxis = new { data = ss };

                dic[nameof(xAxis)] = xAxis;
            }

            // 提示
            var yAxis = YAxis;
            if (yAxis != null) dic[nameof(yAxis)] = yAxis;

            // 系列
            var series = Series;
            if (series != null) dic[nameof(series)] = series;

            return dic.ToJson(false, false, true);
        }
        #endregion
    }
}