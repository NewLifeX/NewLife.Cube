namespace NewLife.Cube.Charts;

/// <summary>三维的飞线图</summary>
/// <remark>
/// 跟二维的飞线图一样用于表现起点终点的线数据。更多用在地理可视化上。
/// 下图是使用 lines3D 在 globe 上可视化飞机航班的一个例子。
/// </remark>
public class SeriesLines3D : Series
{
    /// <summary>实例化三维飞线图</summary>
    public SeriesLines3D() => Type = "lines3D";

    //public String Type { get; set; } = "lines3D";

    ///// <summary></summary>
    ///// <remark>系列名称，用于 tooltip 的显示，legend 的图例筛选，在 setOption 更新数据和配置项时用于指定对应的系列。</remark>
    //public String Name { get; set; }

    /// <summary>该系列使用的坐标系</summary>
    /// <remark>
    /// 可选：
    /// 'geo3D'
    ///   使用三维地理坐标系，通过 geo3DIndex 指定相应的三维地理坐标系组件
    /// 'globe'
    ///   使用地球坐标系，通过 globeIndex 指定相应的地球坐标系组件
    /// </remark>
    public String CoordinateSystem { get; set; }

    /// <summary>坐标轴使用的 geo3D 组件的索引</summary>
    /// <remark>默认使用第一个 geo3D 组件。</remark>
    public Double? Geo3DIndex { get; set; }

    /// <summary>坐标轴使用的 globe 组件的索引</summary>
    /// <remark>默认使用第一个 globe 组件。</remark>
    public Double? GlobeIndex { get; set; }

    /// <summary>是否是多段线</summary>
    /// <remark>
    /// 默认为 false，只能用于绘制只有两个端点的线段（表现为被赛尔曲线）。
    /// 如果该配置项为 true，则可以在 data.coords 中设置多于 2 个的顶点用来绘制多段线，在绘制路线轨迹的时候比较有用。
    /// </remark>
    public Boolean? Polyline { get; set; }

    /// <summary>飞线的尾迹特效</summary>
    public Object Effect { get; set; }

    /// <summary>飞线的线条样式</summary>
    public Object LineStyle { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 三维飞线图的数据数组，通常数据的每一项可以是一个包含起点和终点的坐标集。在 polyline 设置为 true 时支持多于两个的坐标。
    /// 如下：
    /// data: [
    ///     [
    ///         [120, 66, 1], // 起点的经纬度和海拔坐标
    ///         [122, 67, 2]  // 终点的经纬度和海拔坐标
    ///     ]
    /// ]
    /// 有些时候需要配置数据项的名字或者单独的样式。需要把经纬度坐标写到 coords 属性下。如下：
    /// data: [
    ///     {
    ///         coords: [ [120, 66], [122, 67] ],
    ///         // 数据值
    ///         value: 10,
    ///         // 数据名
    ///         name: 'foo',
    ///         // 线条样式
    ///         lineStyle: {}
    ///     }
    /// ]
    /// </remark>
    public override Object[] Data { get; set; }

    /// <summary></summary>
    /// <remark>混合模式，目前支持'source-over'，'lighter'，默认使用的'source-over'是通过 alpha 混合，而'lighter'是叠加模式，该模式可以让数据集中的区域因为叠加而产生高亮的效果。</remark>
    public String BlendMode { get; set; }

    /// <summary>组件所在的层</summary>
    /// <remark>
    /// zlevel用于 Canvas 分层，不同zlevel值的图形会放置在不同的 Canvas 中，Canvas 分层是一种常见的优化手段。我们可以把一些图形变化频繁（例如有动画）的组件设置成一个单独的zlevel。需要注意的是过多的 Canvas 会引起内存开销的增大，在手机端上需要谨慎使用以防崩溃。
    /// zlevel 大的 Canvas 会放在 zlevel 小的 Canvas 的上面。
    /// 注: echarts-gl 中组件的层需要跟 echarts 中组件的层分开。同一个 zlevel 不能同时用于 WebGL 和 Canvas 的绘制。
    /// </remark>
    public Double? Zlevel { get; set; }

    /// <summary>图形是否不响应和触发鼠标事件，默认为 false，即响应和触发鼠标事件。</summary>
    public Boolean? Silent { get; set; }

}
