using System;
using System.Collections.Generic;

namespace NewLife.Cube.Charts;

/// <summary></summary>
/// <remark>直角坐标系 grid 中的 x 轴，一般情况下单个 grid 组件最多只能放上下两个 x 轴，多于两个 x 轴需要通过配置 offset 属性防止同个位置多个 x 轴的重叠。</remark>
public class XAxis
{
    /// <summary>组件 ID</summary>
    /// <remark>默认不指定。指定则可用于在 option 或者 API 中引用组件。</remark>
    public String Id { get; set; }

    /// <summary>是否显示 x 轴</summary>
    public Boolean? Show { get; set; }

    /// <summary>x 轴所在的 grid 的索引，默认位于第一个 grid</summary>
    public Double? GridIndex { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.3.0 开始支持
    /// 在多个 x 轴为数值轴的时候，可以开启该配置项自动对齐刻度。只对'value'和'log'类型的轴有效。
    /// </remark>
    public Boolean? AlignTicks { get; set; }

    /// <summary>x 轴的位置</summary>
    /// <remark>
    /// 可选：
    /// 'top'
    /// 'bottom'
    /// 默认 grid 中的第一个 x 轴在 grid 的下方（'bottom'），第二个 x 轴视第一个 x 轴的位置放在另一侧。注：若未将 xAxis.axisLine.onZero 设为 false , 则该项无法生效
    /// </remark>
    public String Position { get; set; }

    /// <summary></summary>
    /// <remark>X 轴相对于默认位置的偏移，在相同的 position 上有多个 X 轴的时候有用。注：若未将 xAxis.axisLine.onZero 设为 false , 则该项无法生效</remark>
    public Double? Offset { get; set; }

    /// <summary>坐标轴类型</summary>
    /// <remark>
    /// 可选：
    /// 'value'
    ///   数值轴，适用于连续数据。
    /// 'category'
    ///   类目轴，适用于离散的类目数据。为该类型时类目数据可自动从 series.data 或 dataset.source 中取，或者可通过 xAxis.data 设置类目数据。
    /// 'time'
    ///   时间轴，适用于连续的时序数据，与数值轴相比时间轴带有时间的格式化，在刻度计算上也有所不同，例如会根据跨度的范围来决定使用月，星期，日还是小时范围的刻度。
    /// 'log'
    ///   对数轴。适用于对数数据。对数轴下的堆积柱状图或堆积折线图可能带来很大的视觉误差，并且在一定情况下可能存在非预期效果，应避免使用。
    /// </remark>
    public String Type { get; set; }="category";

    /// <summary>坐标轴名称</summary>
    public String Name { get; set; }

    /// <summary>坐标轴名称显示位置</summary>
    /// <remark>
    /// 可选：
    /// 'start'
    /// 'middle' 或者 'center'
    /// 'end'
    /// </remark>
    public String NameLocation { get; set; }

    /// <summary>坐标轴名称的文字样式</summary>
    public Object NameTextStyle { get; set; }

    /// <summary>坐标轴名称与轴线之间的距离</summary>
    public Double? NameGap { get; set; }

    /// <summary>坐标轴名字旋转，角度值</summary>
    public Double? NameRotate { get; set; }

    /// <summary>坐标轴名字的截断</summary>
    public Object NameTruncate { get; set; }

    /// <summary>是否是反向坐标轴</summary>
    public Boolean? Inverse { get; set; }

    /// <summary>坐标轴两边留白策略，类目轴和非类目轴的设置和表现不一样</summary>
    /// <remark>
    /// 类目轴中 boundaryGap 可以配置为 true 和 false。默认为 true，这时候刻度只是作为分隔线，标签和数据点都会在两个刻度之间的带(band)中间。
    /// 非类目轴，包括时间，数值，对数轴，boundaryGap是一个两个值的数组，分别表示数据最小值和最大值的延伸范围，可以直接设置数值或者相对的百分比，在设置 min 和 max 后无效。
    /// 示例：
    /// boundaryGap: ['20%', '20%']
    /// </remark>
    public Object BoundaryGap { get; set; }

    /// <summary>坐标轴刻度最小值</summary>
    /// <remark>
    /// 可以设置成特殊值 'dataMin'，此时取数据在该轴上的最小值作为最小刻度。
    /// 不设置时会自动计算最小值保证坐标轴刻度的均匀分布。
    /// 在类目轴中，也可以设置为类目的序数（如类目轴 data: ['类A', '类B', '类C'] 中，序数 2 表示 '类C'。也可以设置为负数，如 -3）。
    /// 当设置成 function 形式时，可以根据计算得出的数据最大最小值设定坐标轴的最小值。如：
    /// min: function (value) {
    ///     return value.min - 20;
    /// }
    /// 其中 value 是一个包含 min 和 max 的对象，分别表示数据的最大最小值，这个函数可返回坐标轴的最小值，也可返回 null/undefined 来表示“自动计算最小值”（返回 null/undefined 从 v4.8.0 开始支持）。
    /// </remark>
    public Object Min { get; set; }

    /// <summary>坐标轴刻度最大值</summary>
    /// <remark>
    /// 可以设置成特殊值 'dataMax'，此时取数据在该轴上的最大值作为最大刻度。
    /// 不设置时会自动计算最大值保证坐标轴刻度的均匀分布。
    /// 在类目轴中，也可以设置为类目的序数（如类目轴 data: ['类A', '类B', '类C'] 中，序数 2 表示 '类C'。也可以设置为负数，如 -3）。
    /// 当设置成 function 形式时，可以根据计算得出的数据最大最小值设定坐标轴的最小值。如：
    /// max: function (value) {
    ///     return value.max - 20;
    /// }
    /// 其中 value 是一个包含 min 和 max 的对象，分别表示数据的最大最小值，这个函数可返回坐标轴的最大值，也可返回 null/undefined 来表示“自动计算最大值”（返回 null/undefined 从 v4.8.0 开始支持）。
    /// </remark>
    public Object Max { get; set; }

    /// <summary>只在数值轴中（type: 'value'）有效</summary>
    /// <remark>
    /// 是否是脱离 0 值比例。设置成 true 后坐标刻度不会强制包含零刻度。在双数值轴的散点图中比较有用。
    /// 在设置 min 和 max 之后该配置项无效。
    /// </remark>
    public Boolean? Scale { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 坐标轴的分割段数，需要注意的是这个分割段数只是个预估值，最后实际显示的段数会在这个基础上根据分割后坐标轴刻度显示的易读程度作调整。
    /// 在类目轴中无效。
    /// </remark>
    public Double? SplitNumber { get; set; }

    /// <summary>自动计算的坐标轴最小间隔大小</summary>
    /// <remark>
    /// 例如可以设置成1保证坐标轴分割刻度显示成整数。
    /// {
    ///     minInterval: 1
    /// }
    /// 只在数值轴或时间轴中（type: 'value' 或 'time'）有效。
    /// </remark>
    public Double? MinInterval { get; set; }

    /// <summary>自动计算的坐标轴最大间隔大小</summary>
    /// <remark>
    /// 例如，在时间轴（（type: 'time'））可以设置成 3600 * 24 * 1000 保证坐标轴分割刻度最大为一天。
    /// {
    ///     maxInterval: 3600 * 24 * 1000
    /// }
    /// 只在数值轴或时间轴中（type: 'value' 或 'time'）有效。
    /// </remark>
    public Double? MaxInterval { get; set; }

    /// <summary>强制设置坐标轴分割间隔</summary>
    /// <remark>
    /// 因为 splitNumber 是预估的值，实际根据策略计算出来的刻度可能无法达到想要的效果，这时候可以使用 interval 配合 min、max 强制设定刻度划分，一般不建议使用。
    /// 无法在类目轴中使用。在时间轴（type: 'time'）中需要传时间戳，在对数轴（type: 'log'）中需要传指数值。
    /// </remark>
    public Double? Interval { get; set; }

    /// <summary>对数轴的底数，只在对数轴中（type: 'log'）有效</summary>
    public Double? LogBase { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.5.1 开始支持
    /// 用于指定轴的起始值。
    /// </remark>
    public Double? StartValue { get; set; }

    /// <summary>坐标轴是否是静态无法交互</summary>
    public Boolean? Silent { get; set; }

    /// <summary>坐标轴的标签是否响应和触发鼠标事件，默认不响应</summary>
    /// <remark>
    /// 事件参数如下：
    /// {
    ///     // 组件类型，xAxis, yAxis, radiusAxis, angleAxis
    ///     // 对应组件类型都会有一个属性表示组件的 index，例如 xAxis 就是 xAxisIndex
    ///     componentType: string,
    ///     // 未格式化过的刻度值, 点击刻度标签有效
    ///     value: '',
    ///     // 坐标轴名称, 点击坐标轴名称有效
    ///     name: ''
    /// }
    /// </remark>
    public Boolean? TriggerEvent { get; set; }

    /// <summary>坐标轴轴线相关设置</summary>
    public Object AxisLine { get; set; }

    /// <summary>坐标轴刻度相关设置</summary>
    public Object AxisTick { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v4.6.0 开始支持
    /// 坐标轴次刻度线相关设置。
    /// 注意：次刻度线无法在类目轴（type: 'category'）中使用。
    /// 示例：
    /// 1) 函数绘图中使用次刻度线
    /// 2) 在对数轴中使用次刻度线
    /// </remark>
    public Object MinorTick { get; set; }

    /// <summary>坐标轴刻度标签的相关设置</summary>
    public Object AxisLabel { get; set; }

    /// <summary>坐标轴在 grid 区域中的分隔线</summary>
    public Object SplitLine { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v4.6.0 开始支持
    /// 坐标轴在 grid 区域中的次分隔线。次分割线会对齐次刻度线 minorTick
    /// </remark>
    public Object MinorSplitLine { get; set; }

    /// <summary>坐标轴在 grid 区域中的分隔区域，默认不显示</summary>
    public Object SplitArea { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 类目数据，在类目轴（type: 'category'）中有效。
    /// 如果没有设置 type，但是设置了 axis.data，则认为 type 是 'category'。
    /// 如果设置了 type 是 'category'，但没有设置 axis.data，则 axis.data 的内容会自动从 series.data 中获取，这会比较方便。不过注意，axis.data 指明的是 'category' 轴的取值范围。如果不指定而是从 series.data 中获取，那么只能获取到 series.data 中出现的值。比如说，假如 series.data 为空时，就什么也获取不到。
    /// 示例：
    /// // 所有类目名称列表
    /// data: ['周一', '周二', '周三', '周四', '周五', '周六', '周日']
    /// // 每一项也可以是具体的配置项，此时取配置项中的 `value` 为类目名
    /// data: [{
    ///     value: '周一',
    ///     // 突出周一
    ///     textStyle: {
    ///         fontSize: 20,
    ///         color: 'red'
    ///     }
    /// }, '周二', '周三', '周四', '周五', '周六', '周日']
    /// </remark>
    public Object[] Data { get; set; }

    /// <summary>坐标轴指示器配置项</summary>
    public Object AxisPointer { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.6.0 开始支持
    /// 坐标轴 tooltip 设置，注意需设置 triggerEvent 为 true 并启用全局 option.tooltip 组件。
    /// </remark>
    public Object Tooltip { get; set; }

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

    /// <summary>X 轴所有图形的 zlevel 值</summary>
    /// <remark>
    /// zlevel用于 Canvas 分层，不同zlevel值的图形会放置在不同的 Canvas 中，Canvas 分层是一种常见的优化手段。我们可以把一些图形变化频繁（例如有动画）的组件设置成一个单独的zlevel。需要注意的是过多的 Canvas 会引起内存开销的增大，在手机端上需要谨慎使用以防崩溃。
    /// zlevel 大的 Canvas 会放在 zlevel 小的 Canvas 的上面。
    /// </remark>
    public Double? Zlevel { get; set; }

    /// <summary>X 轴组件的所有图形的z值</summary>
    /// <remark>
    /// 控制图形的前后顺序。z值小的图形会被z值大的图形覆盖。
    /// z相比zlevel优先级更低，而且不会创建新的 Canvas。
    /// </remark>
    public Double? Z { get; set; }

}
