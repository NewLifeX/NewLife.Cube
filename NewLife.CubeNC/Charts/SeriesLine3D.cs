namespace NewLife.Cube.Charts;

/// <summary>三维折线图</summary>
/// <remark>可以用于三维直角坐标系 grid3D。</remark>
public class SeriesLine3D : Series
{
    /// <summary>实例化三维折线图</summary>
    public SeriesLine3D() => Type = "line3D";

    //public String Type { get; set; } = "line3D";

    ///// <summary></summary>
    ///// <remark>系列名称，用于 tooltip 的显示，legend 的图例筛选，在 setOption 更新数据和配置项时用于指定对应的系列。</remark>
    //public String Name { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 该系列使用的坐标系，可选：
    /// 'cartesian3D'
    ///   使用三维笛卡尔坐标系，通过 grid3DIndex 指定相应的三维笛卡尔坐标系组件。
    /// </remark>
    public String CoordinateSystem { get; set; }

    /// <summary>使用的 grid3D 组件的索引</summary>
    /// <remark>默认使用第一个 grid3D 组件。</remark>
    public Double? Grid3DIndex { get; set; }

    /// <summary>线条样式</summary>
    public Object LineStyle { get; set; }

    /// <summary>数据数组</summary>
    /// <remark>
    /// 数组每一项为一个数据。通常这个数据是用数组存储数据的每个属性/维度。例如下面：
    /// data: [
    ///     [[12, 14, 10], [34, 50, 15], [56, 30, 20], [10, 15, 12], [23, 10, 14]]
    /// ]
    /// 数组中的每一项的前三个值分别是x, y, z。除了这三个值也可以添加其它值给 visualMap 组件映射到颜色等其它图形属性。
    /// 有些时候我们需要指定每个数据项的名称，这时候需要每个项为一个对象：
    /// [{
    ///     // 数据项的名称
    ///     name: '数据1',
    ///     // 数据项值
    ///     value: [12, 14, 10]
    /// }, {
    ///     name: '数据2',
    ///     value: [34, 50, 15]
    /// }]
    /// 需要对个别内容指定进行个性化定义时：
    /// [{
    ///     name: '数据1',
    ///     value: [12, 14, 10]
    /// }, {
    ///     // 数据项名称
    ///     name: '数据2',
    ///     value : [34, 50, 15],
    ///     //自定义特殊itemStyle，仅对该item有效
    ///     itemStyle:{}
    /// }]
    /// </remark>
    public override Object[] Data { get; set; }

    /// <summary>组件所在的层</summary>
    /// <remark>
    /// zlevel用于 Canvas 分层，不同zlevel值的图形会放置在不同的 Canvas 中，Canvas 分层是一种常见的优化手段。我们可以把一些图形变化频繁（例如有动画）的组件设置成一个单独的zlevel。需要注意的是过多的 Canvas 会引起内存开销的增大，在手机端上需要谨慎使用以防崩溃。
    /// zlevel 大的 Canvas 会放在 zlevel 小的 Canvas 的上面。
    /// 注: echarts-gl 中组件的层需要跟 echarts 中组件的层分开。同一个 zlevel 不能同时用于 WebGL 和 Canvas 的绘制。
    /// </remark>
    public Double? Zlevel { get; set; }

    /// <summary>图形是否不响应和触发鼠标事件，默认为 false，即响应和触发鼠标事件。</summary>
    public Boolean? Silent { get; set; }

    /// <summary>是否开启动画</summary>
    public Boolean? Animation { get; set; }

    /// <summary>过渡动画的时长</summary>
    public Double? AnimationDurationUpdate { get; set; }

    /// <summary>过渡动画的缓动效果</summary>
    public String AnimationEasingUpdate { get; set; }

}
