using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewLife.Cube.Charts
{
    /// <summary>系列类型</summary>
    public enum SeriesTypes
    {
        /// <summary>折线图</summary>
        Line,

        /// <summary>柱状图</summary>
        Bar,

        /// <summary>饼图</summary>
        Pie,

        /// <summary>散点图</summary>
        Scatter,

        /// <summary>关系图</summary>
        Graph,

        /// <summary>雷达图</summary>
        Radar,

        /// <summary>树图</summary>
        Tree,

        /// <summary>层级数据</summary>
        Treemap,
    }
}