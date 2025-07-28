using System;
using System.Collections.Generic;

namespace NewLife.Cube.Charts;

/// <summary>平行坐标系的系列</summary>
/// <remark>
/// 平行坐标系介绍
/// 平行坐标系（Parallel Coordinates） 是一种常用的可视化高维数据的图表。
/// 例如 series-parallel.data 中有如下数据：
/// [
///     [1,  55,  9,   56,  0.46,  18,  6,  '良'],
///     [2,  25,  11,  21,  0.65,  34,  9,  '优'],
///     [3,  56,  7,   63,  0.3,   14,  5,  '良'],
///     [4,  33,  7,   29,  0.33,  16,  6,  '优'],
///     { // 数据项也可以是 Object，从而里面能含有对线条的特殊设置。
///         value: [5,  42,  24,  44,  0.76,  40,  16, '优']
///         lineStyle: {...},
///     }
///     ...
/// ]
/// 数据中，每一行是一个『数据项』，每一列属于一个『维度』。（例如上面数据每一列的含义分别是：『日期』,『AQI指数』, 『PM2.5』, 『PM10』, 『一氧化碳值』, 『二氧化氮值』, 『二氧化硫值』）。
/// 平行坐标系适用于对这种多维数据进行可视化分析。每一个维度（每一列）对应一个坐标轴，每一个『数据项』是一条线，贯穿多个坐标轴。在坐标轴上，可以进行数据选取等操作。如下：
/// 配置方式概要
/// 『平行坐标系』的 option 基本配置如下例：
/// option = {
///     parallelAxis: [                     // 这是一个个『坐标轴』的定义
///         {dim: 0, name: schema[0].text}, // 每个『坐标轴』有个 'dim' 属性，表示坐标轴的维度号。
///         {dim: 1, name: schema[1].text},
///         {dim: 2, name: schema[2].text},
///         {dim: 3, name: schema[3].text},
///         {dim: 4, name: schema[4].text},
///         {dim: 5, name: schema[5].text},
///         {dim: 6, name: schema[6].text},
///         {dim: 7, name: schema[7].text,
///             type: 'category',           // 坐标轴也可以支持类别型数据
///             data: ['优', '良', '轻度污染', '中度污染', '重度污染', '严重污染']
///         }
///     ],
///     parallel: {                         // 这是『坐标系』的定义
///         left: '5%',                     // 平行坐标系的位置设置
///         right: '13%',
///         bottom: '10%',
///         top: '20%',
///         parallelAxisDefault: {          // 『坐标轴』的公有属性可以配置在这里避免重复书写
///             type: 'value',
///             nameLocation: 'end',
///             nameGap: 20
///         }
///     },
///     series: [                           // 这里三个系列共用一个平行坐标系
///         {
///             name: '北京',
///             type: 'parallel',           // 这个系列类型是 'parallel'
///             data: [
///                 [1,  55,  9,   56,  0.46,  18,  6,  '良'],
///                 [2,  25,  11,  21,  0.65,  34,  9,  '优'],
///                 ...
///             ]
///         },
///         {
///             name: '上海',
///             type: 'parallel',
///             data: [
///                 [3,  56,  7,   63,  0.3,   14,  5,  '良'],
///                 [4,  33,  7,   29,  0.33,  16,  6,  '优'],
///                 ...
///             ]
///         },
///         {
///             name: '广州',
///             type: 'parallel',
///             data: [
///                 [4,  33,  7,   29,  0.33,  16,  6,  '优'],
///                 [5,  42,  24,  44,  0.76,  40,  16, '优'],
///                 ...
///             ]
///         }
///     ]
/// };
/// 需要涉及到三个组件：parallel、parallelAxis、series-parallel
/// parallel
///   这个配置项是平行坐标系的『坐标系』本身。一个系列（series）或多个系列（如上图中的『北京』、『上海』、『广州』分别各是一个系列）可以共用这个『坐标系』。
///   和其他坐标系一样，坐标系也可以创建多个。
///   位置设置，也是放在这里进行。
/// parallelAxis
///   这个是『坐标系』中的坐标轴的配置。自然，需要有多个坐标轴。
///   其中有 parallelAxis.parallelIndex 属性，指定这个『坐标轴』在哪个『坐标系』中。默认使用第一个『坐标系』。
/// series-parallel
///   这个是『系列』的定义。系列被画到『坐标系』上。
///   其中有 series-parallel.parallelIndex 属性，指定使用哪个『坐标系』。默认使用第一个『坐标系』。
/// 配置注意和最佳实践
/// 配置多个 parallelAxis 时，有些值一样的属性，如果书写多遍则比较繁琐，那么可以放置在 parallel.parallelAxisDefault 里。在坐标轴初始化前，parallel.parallelAxisDefault 里的配置项，会分别融合进 parallelAxis，形成最终的坐标轴的配置。
/// 如果数据量很大并且发生卡顿
/// 建议把 series-parallel.lineStyle.width 设为 0.5（或更小），
/// 可能显著改善性能。
/// 高维数据的显示
/// 维度比较多时，比如有 50+ 的维度，那么就会有 50+ 个轴。那么可能会页面显示不下。
/// 可以通过 parallel.axisExpandable 来改善显示效果。
/// </remark>
public class SeriesParallel : Series
{
    /// <summary>实例化平行坐标系</summary>
    public SeriesParallel() => Type = "parallel";

    //public String Type { get; set; }="parallel";

    ///// <summary>组件 ID</summary>
    ///// <remark>默认不指定。指定则可用于在 option 或者 API 中引用组件。</remark>
    //public String Id { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 该系列使用的坐标系，可选：
    /// 'parallel'
    ///   使用平行坐标系，通过 parallelIndex 指定相应的平行坐标系组件。
    /// </remark>
    public String CoordinateSystem { get; set; }

    /// <summary>使用的平行坐标系的 index，在单个图表实例中存在多个平行坐标系的时候有用。</summary>
    public Double? ParallelIndex { get; set; }

    ///// <summary></summary>
    ///// <remark>系列名称，用于tooltip的显示，legend 的图例筛选，在 setOption 更新数据和配置项时用于指定对应的系列。</remark>
    //public String Name { get; set; }

    ///// <summary></summary>
    ///// <remark>
    ///// 从 v5.2.0 开始支持
    ///// 从调色盘 option.color 中取色的策略，可取值为：
    ///// 'series'：按照系列分配调色盘中的颜色，同一系列中的所有数据都是用相同的颜色；
    ///// 'data'：按照数据项分配调色盘中的颜色，每个数据项都使用不同的颜色。
    ///// </remark>
    //public String ColorBy { get; set; }

    /// <summary>线条样式</summary>
    public Object LineStyle { get; set; }

    /// <summary></summary>
    public Object Emphasis { get; set; }

    /// <summary>框选时，未被选中的条线会设置成这个『透明度』（从而可以达到变暗的效果）。</summary>
    public Double? InactiveOpacity { get; set; }

    /// <summary>框选时，选中的条线会设置成这个『透明度』（从而可以达到高亮的效果）。</summary>
    public Double? ActiveOpacity { get; set; }

    /// <summary>是否实时刷新</summary>
    public Boolean? Realtime { get; set; }

    /// <summary>是否使用平滑曲线</summary>
    /// <remark>默认为 false，可以设置为 true 或者一个范围为 0 到 1 的小数值，指定平滑程度。</remark>
    public Object Smooth { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 渐进式渲染时每一帧绘制图形数量，设为 0 时不启用渐进式渲染，支持每个系列单独配置。
    /// 在图中有数千到几千万图形元素的时候，一下子把图形绘制出来，或者交互重绘的时候可能会造成界面的卡顿甚至假死。ECharts 4 开始全流程支持渐进渲染（progressive rendering），渲染的时候会把创建好的图形分到数帧中渲染，每一帧渲染只渲染指定数量的图形。
    /// 该配置项就是用于配置该系列每一帧渲染的图形数，可以根据图表图形复杂度的需要适当调整这个数字使得在不影响交互流畅性的前提下达到绘制速度的最大化。比如在 lines 图或者平行坐标中线宽大于 1 的 polyline 绘制会很慢，这个数字就可以设置小一点，而线宽小于等于 1 的 polyline 绘制非常快，该配置项就可以相对调得比较大。
    /// </remark>
    public Double? Progressive { get; set; }

    /// <summary>启用渐进式渲染的图形数量阈值，在单个系列的图形数量超过该阈值时启用渐进式渲染。</summary>
    public Double? ProgressiveThreshold { get; set; }

    /// <summary>分片的方式</summary>
    /// <remark>
    /// 可选值：
    /// 'sequential': 按照数据的顺序分片。缺点是渲染过程不自然。
    /// 'mod': 取模分片，即每个片段中的点会遍布于整个数据，从而能够视觉上均匀得渲染。
    /// </remark>
    public String ProgressiveChunkMode { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 例如 series-parallel.data 中有如下数据：
    /// [
    ///     [1,  55,  9,   56,  0.46,  18,  6,  '良'],
    ///     [2,  25,  11,  21,  0.65,  34,  9,  '优'],
    ///     [3,  56,  7,   63,  0.3,   14,  5,  '良'],
    ///     [4,  33,  7,   29,  0.33,  16,  6,  '优'],
    ///     { // 数据项也可以是 Object，从而里面能含有对线条的特殊设置。
    ///         value: [5,  42,  24,  44,  0.76,  40,  16, '优']
    ///         lineStyle: {...},
    ///     }
    ///     ...
    /// ]
    /// 数据中，每一行是一个『数据项』，每一列属于一个『维度』。（例如上面数据每一列的含义分别是：『日期』,『AQI指数』, 『PM2.5』, 『PM10』, 『一氧化碳值』, 『二氧化氮值』, 『二氧化硫值』）。
    /// </remark>
    public override Object[] Data { get; set; }

    /// <summary>平行坐标所有图形的 zlevel 值</summary>
    /// <remark>
    /// zlevel用于 Canvas 分层，不同zlevel值的图形会放置在不同的 Canvas 中，Canvas 分层是一种常见的优化手段。我们可以把一些图形变化频繁（例如有动画）的组件设置成一个单独的zlevel。需要注意的是过多的 Canvas 会引起内存开销的增大，在手机端上需要谨慎使用以防崩溃。
    /// zlevel 大的 Canvas 会放在 zlevel 小的 Canvas 的上面。
    /// </remark>
    public Double? Zlevel { get; set; }

    /// <summary>平行坐标组件的所有图形的z值</summary>
    /// <remark>
    /// 控制图形的前后顺序。z值小的图形会被z值大的图形覆盖。
    /// z相比zlevel优先级更低，而且不会创建新的 Canvas。
    /// </remark>
    public Double? Z { get; set; }

    /// <summary>图形是否不响应和触发鼠标事件，默认为 false，即响应和触发鼠标事件。</summary>
    public Boolean? Silent { get; set; }

    /// <summary>是否开启动画</summary>
    public Boolean? Animation { get; set; }

    /// <summary>是否开启动画的阈值，当单个系列显示的图形数量大于这个阈值时会关闭动画。</summary>
    public Double? AnimationThreshold { get; set; }

    /// <summary>初始动画的时长</summary>
    /// <remark>
    /// 支持回调函数，可以通过每个数据返回不同的时长实现更戏剧的初始动画效果：
    /// animationDuration: function (idx) {
    ///     // 越往后的数据时长越大
    ///     return idx * 100;
    /// }
    /// </remark>
    public Object AnimationDuration { get; set; }

    /// <summary>初始动画的缓动效果</summary>
    /// <remark>不同的缓动效果可以参考 缓动示例。</remark>
    public String AnimationEasing { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 初始动画的延迟，支持回调函数，可以通过每个数据返回不同的 delay 时间实现更戏剧的初始动画效果。
    /// 如下示例：
    /// animationDelay: function (idx) {
    ///     // 越往后的数据延迟越大
    ///     return idx * 100;
    /// }
    /// 也可以看该示例
    /// </remark>
    public Object AnimationDelay { get; set; }

    /// <summary>数据更新动画的时长</summary>
    /// <remark>
    /// 支持回调函数，可以通过每个数据返回不同的时长实现更戏剧的更新动画效果：
    /// animationDurationUpdate: function (idx) {
    ///     // 越往后的数据时长越大
    ///     return idx * 100;
    /// }
    /// </remark>
    public Object AnimationDurationUpdate { get; set; }

    /// <summary>数据更新动画的缓动效果</summary>
    public String AnimationEasingUpdate { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 数据更新动画的延迟，支持回调函数，可以通过每个数据返回不同的 delay 时间实现更戏剧的更新动画效果。
    /// 如下示例：
    /// animationDelayUpdate: function (idx) {
    ///     // 越往后的数据延迟越大
    ///     return idx * 100;
    /// }
    /// 也可以看该示例
    /// </remark>
    public Object AnimationDelayUpdate { get; set; }

}
