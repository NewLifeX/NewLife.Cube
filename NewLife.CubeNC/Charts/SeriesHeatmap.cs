namespace NewLife.Cube.Charts;

/// <summary>热力图</summary>
/// <remark>
/// 热力图主要通过颜色去表现数值的大小，必须要配合 visualMap 组件使用。
/// 可以应用在直角坐标系以及地理坐标系上，这两个坐标系上的表现形式相差很大，直角坐标系上必须要使用两个类目轴。
/// 下面是在直角坐标系上应用的例子：
/// 直角坐标系：
/// </remark>
public class SeriesHeatmap : Series
{
    /// <summary>实例化热力图</summary>
    public SeriesHeatmap() => Type = "heatmap";

    //public String Type { get; set; } = "heatmap";

    ///// <summary>组件 ID</summary>
    ///// <remark>默认不指定。指定则可用于在 option 或者 API 中引用组件。</remark>
    //public String Id { get; set; }

    ///// <summary></summary>
    ///// <remark>系列名称，用于tooltip的显示，legend 的图例筛选，在 setOption 更新数据和配置项时用于指定对应的系列。</remark>
    //public String Name { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 该系列使用的坐标系，可选：
    /// 'cartesian2d'
    ///   使用二维的直角坐标系（也称笛卡尔坐标系），通过 xAxisIndex, yAxisIndex指定相应的坐标轴组件。
    /// 'geo'
    ///   使用地理坐标系，通过 geoIndex 指定相应的地理坐标系组件。
    /// 'calendar'
    ///   使用日历坐标系，通过 calendarIndex 指定相应的日历坐标系组件。
    /// </remark>
    public String CoordinateSystem { get; set; }

    ///// <summary>使用的 x 轴的 index，在单个图表实例中存在多个 x 轴的时候有用。</summary>
    //public Double? XAxisIndex { get; set; }

    ///// <summary>使用的 y 轴的 index，在单个图表实例中存在多个 y轴的时候有用。</summary>
    //public Double? YAxisIndex { get; set; }

    /// <summary>使用的地理坐标系的 index，在单个图表实例中存在多个地理坐标系的时候有用。</summary>
    public Double? GeoIndex { get; set; }

    /// <summary>使用的日历坐标系的 index，在单个图表实例中存在多个日历坐标系的时候有用。</summary>
    public Double? CalendarIndex { get; set; }

    /// <summary></summary>
    /// <remark>每个点的大小，在地理坐标系(coordinateSystem: 'geo')上有效。</remark>
    public Double? PointSize { get; set; }

    /// <summary></summary>
    /// <remark>每个点模糊的大小，在地理坐标系(coordinateSystem: 'geo')上有效。</remark>
    public Double? BlurSize { get; set; }

    /// <summary></summary>
    /// <remark>最小的透明度，在地理坐标系(coordinateSystem: 'geo')上有效。</remark>
    public Double? MinOpacity { get; set; }

    /// <summary></summary>
    /// <remark>最大的透明度，在地理坐标系(coordinateSystem: 'geo')上有效。</remark>
    public Double? MaxOpacity { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 渐进式渲染时每一帧绘制图形数量，设为 0 时不启用渐进式渲染，支持每个系列单独配置。
    /// 在图中有数千到几千万图形元素的时候，一下子把图形绘制出来，或者交互重绘的时候可能会造成界面的卡顿甚至假死。ECharts 4 开始全流程支持渐进渲染（progressive rendering），渲染的时候会把创建好的图形分到数帧中渲染，每一帧渲染只渲染指定数量的图形。
    /// 该配置项就是用于配置该系列每一帧渲染的图形数，可以根据图表图形复杂度的需要适当调整这个数字使得在不影响交互流畅性的前提下达到绘制速度的最大化。比如在 lines 图或者平行坐标中线宽大于 1 的 polyline 绘制会很慢，这个数字就可以设置小一点，而线宽小于等于 1 的 polyline 绘制非常快，该配置项就可以相对调得比较大。
    /// </remark>
    public Double? Progressive { get; set; }

    /// <summary>启用渐进式渲染的图形数量阈值，在单个系列的图形数量超过该阈值时启用渐进式渲染。</summary>
    public Double? ProgressiveThreshold { get; set; }

    /// <summary></summary>
    /// <remark>在直角坐标系(coordinateSystem: 'cartesian2d')上有效。</remark>
    public Object Label { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 标签的统一布局配置。
    /// 该配置项是在每个系列默认的标签布局基础上，统一调整标签的(x, y)位置，标签对齐等属性以实现想要的标签布局效果。
    /// 该配置项也可以是一个有如下参数的回调函数
    /// // 标签对应数据的 dataIndex
    /// dataIndex: number
    /// // 标签对应的数据类型，只在关系图中会有 node 和 edge 数据类型的区分
    /// dataType?: string
    /// // 标签对应的系列的 index
    /// seriesIndex: number
    /// // 标签显示的文本
    /// text: string
    /// // 默认的标签的包围盒，由系列默认的标签布局决定
    /// labelRect: {x: number, y: number, width: number, height: number}
    /// // 默认的标签水平对齐
    /// align: 'left' | 'center' | 'right'
    /// // 默认的标签垂直对齐
    /// verticalAlign: 'top' | 'middle' | 'bottom'
    /// // 标签所对应的数据图形的包围盒，可用于定位标签位置
    /// rect: {x: number, y: number, width: number, height: number}
    /// // 默认引导线的位置，目前只有饼图(pie)和漏斗图(funnel)有默认标签位置
    /// // 如果没有该值则为 null
    /// labelLinePoints?: number[][]
    /// 示例：
    /// 将标签显示在图形右侧 10px 的位置，并且垂直居中：
    /// labelLayout(params) {
    ///     return {
    ///         x: params.rect.x + 10,
    ///         y: params.rect.y + params.rect.height / 2,
    ///         verticalAlign: 'middle',
    ///         align: 'left'
    ///     }
    /// }
    /// 根据图形的包围盒尺寸决定文本尺寸
    /// labelLayout(params) {
    ///     return {
    ///         fontSize: Math.max(params.rect.width / 10, 5)
    ///     };
    /// }
    /// </remark>
    public Object LabelLayout { get; set; }

    /// <summary></summary>
    /// <remark>样式设置，在直角坐标系(coordinateSystem: 'cartesian2d')上有效。</remark>
    public Object ItemStyle { get; set; }

    /// <summary>高亮状态的图形样式</summary>
    public Object Emphasis { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.2.0 开始支持
    /// 全局过渡动画相关的配置。
    /// 全局过渡动画（Universal Transition）提供了任意系列之间进行变形动画的功能。开启该功能后，每次setOption，相同id的系列之间会自动关联进行动画的过渡，更细粒度的关联配置见universalTransition.seriesKey配置。
    /// 通过配置数据项的groupId和childGroupId，还可以实现诸如下钻，聚合等一对多或者多对一的动画。
    /// 可以直接在系列中配置 universalTransition: true 开启该功能。也可以提供一个对象进行更多属性的配置。
    /// </remark>
    public Object UniversalTransition { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 淡出状态的图形样式。开启 emphasis.focus 后有效。
    /// </remark>
    public Object Blur { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 选中状态的图形样式。开启 selectedMode 后有效。
    /// </remark>
    public Object Select { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 选中模式的配置，表示是否支持多个选中，默认关闭，支持布尔值和字符串，字符串取值可选'single'，'multiple'，'series' 分别表示单选，多选以及选择整个系列。
    /// 从 v5.3.0 开始支持 'series'。
    /// </remark>
    public Object SelectedMode { get; set; }

    /// <summary>可以定义 data 的哪个维度被编码成什么</summary>
    /// <remark>
    /// 比如：
    /// option = {
    ///     dataset: {
    ///         source: [
    ///             // 每一列称为一个『维度』。
    ///             // 这里分别是维度 0、1、2、3、4。
    ///             [12, 44, 55, 66, 2],
    ///             [23, 6, 16, 23, 1],
    ///             ...
    ///         ]
    ///     },
    ///     series: {
    ///         type: 'xxx',
    ///         encode: {
    ///             x: [3, 1, 5],      // 表示维度 3、1、5 映射到 x 轴。
    ///             y: 2,              // 表示维度 2 映射到 y 轴。
    ///             tooltip: [3, 2, 4] // 表示维度 3、2、4 会在 tooltip 中显示。
    ///         }
    ///     }
    /// }
    /// 当使用 dimensions 给维度定义名称后，encode 中可直接引用名称，例如：
    /// series: {
    ///     type: 'xxx',
    ///     dimensions: ['date', 'open', 'close', 'highest', 'lowest'],
    ///     encode: {
    ///         x: 'date',
    ///         y: ['open', 'close', 'highest', 'lowest']
    ///     }
    /// }
    /// encode 声明的基本结构如下，其中冒号左边是坐标系、标签等特定名称，如 'x', 'y', 'tooltip' 等，冒号右边是数据中的维度名（string 格式）或者维度的序号（number 格式，从 0 开始计数），可以指定一个或多个维度（使用数组）。通常情况下，下面各种信息不需要所有的都写，按需写即可。
    /// 下面是 encode 支持的属性：
    /// // 在任何坐标系和系列中，都支持：
    /// encode: {
    ///     // 使用 “名为 product 的维度” 和 “名为 score 的维度” 的值在 tooltip 中显示
    ///     tooltip: ['product', 'score']
    ///     // 使用第一个维度和第三个维度的维度名连起来作为系列名。（有时候名字比较长，这可以避免在 series.name 重复输入这些名字）
    ///     seriesName: [1, 3],
    ///     // 表示使用第二个维度中的值作为 id。这在使用 setOption 动态更新数据时有用处，可以使新老数据用 id 对应起来，从而能够产生合适的数据更新动画。
    ///     itemId: 2,
    ///     // 指定数据项的名称使用第三个维度在饼图等图表中有用，可以使这个名字显示在图例（legend）中。
    ///     itemName: 3,
    ///     // 指定数据项的组 ID (groupId)。当全局过渡动画功能开启时，setOption 前后拥有相同 groupId 的数据项会进行动画过渡。
    ///     itemGroupId: 4,
    ///     // 指定数据项对应的子数据组 ID (childGroupId)，用于实现多层下钻和聚合。详见 childGroupId。
    ///     // 从 v5.5.0 开始支持
    ///     itemChildGroupId: 5
    /// }
    /// // 直角坐标系（grid/cartesian）特有的属性：
    /// encode: {
    ///     // 把 “维度1”、“维度5”、“名为 score 的维度” 映射到 X 轴：
    ///     x: [1, 5, 'score'],
    ///     // 把“维度0”映射到 Y 轴。
    ///     y: 0
    /// }
    /// // 单轴（singleAxis）特有的属性：
    /// encode: {
    ///     single: 3
    /// }
    /// // 极坐标系（polar）特有的属性：
    /// encode: {
    ///     radius: 3,
    ///     angle: 2
    /// }
    /// // 地理坐标系（geo）特有的属性：
    /// encode: {
    ///     lng: 3,
    ///     lat: 2
    /// }
    /// // 对于一些没有坐标系的图表，例如饼图、漏斗图等，可以是：
    /// encode: {
    ///     value: 3
    /// }
    /// 这是个更丰富的 encode 的示例：
    /// 特殊地，在 自定义系列（custom series） 中，encode 中轴可以不指定或设置为 null/undefined，从而使系列免于受这个轴控制，也就是说，轴的范围（extent）不会受此系列数值的影响，轴被 dataZoom 控制时也不会过滤掉这个系列：
    /// var option = {
    ///     xAxis: {},
    ///     yAxis: {},
    ///     dataZoom: [{
    ///         xAxisIndex: 0
    ///     }, {
    ///         yAxisIndex: 0
    ///     }],
    ///     series: {
    ///         type: 'custom',
    ///         renderItem: function (params, api) {
    ///             return {
    ///                 type: 'circle',
    ///                 shape: {
    ///                     cx: 100, // x 位置永远为 100
    ///                     cy: api.coord([0, api.value(0)])[1],
    ///                     r: 30
    ///                 },
    ///                 style: {
    ///                     fill: 'blue'
    ///                 }
    ///             };
    ///         },
    ///         encode: {
    ///             // 这样这个系列就不会被 x 轴以及 x
    ///             // 轴上的 dataZoom 控制了。
    ///             x: -1,
    ///             y: 1
    ///         },
    ///         data: [ ... ]
    ///     }
    /// };
    /// </remark>
    public Object Encode { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 当使用 dataset 时，seriesLayoutBy 指定了 dataset 中用行还是列对应到系列上，也就是说，系列“排布”到 dataset 的行还是列上。可取值：
    /// 'column'：默认，dataset 的列对应于系列，从而 dataset 中每一列是一个维度（dimension）。
    /// 'row'：dataset 的行对应于系列，从而 dataset 中每一行是一个维度（dimension）。
    /// 参见这个 示例
    /// </remark>
    public String SeriesLayoutBy { get; set; }

    /// <summary></summary>
    /// <remark>如果 series.data 没有指定，并且 dataset 存在，那么就会使用 dataset。datasetIndex 指定本系列使用哪个 dataset。</remark>
    public Double? DatasetIndex { get; set; }

    /// <summary>该系列所有数据项的组 ID，优先级低于groupId</summary>
    /// <remark>详见series.data.groupId。</remark>
    public String DataGroupId { get; set; }

    /// <summary>系列中的数据内容数组</summary>
    /// <remark>
    /// 数组项通常为具体的数据项。
    /// 注意，如果系列没有指定 data，并且 option 有 dataset，那么默认使用第一个 dataset。如果指定了 data，则不会再使用 dataset。
    /// 可以使用 series.datasetIndex 指定其他的 dataset。
    /// 通常来说，数据用一个二维数组表示。如下，每一列被称为一个『维度』。
    /// series: [{
    ///     data: [
    ///         // 维度X   维度Y   其他维度 ...
    ///         [  3.4,    4.5,   15,   43],
    ///         [  4.2,    2.3,   20,   91],
    ///         [  10.8,   9.5,   30,   18],
    ///         [  7.2,    8.8,   18,   57]
    ///     ]
    /// }]
    /// 在 直角坐标系 (grid) 中『维度X』和『维度Y』会默认对应于 xAxis 和 yAxis。
    /// 在 极坐标系 (polar) 中『维度X』和『维度Y』会默认对应于 radiusAxis 和 angleAxis。
    /// 后面的其他维度是可选的，可以在别处被使用，例如：
    /// 在 visualMap 中可以将一个或多个维度映射到颜色，大小等多个图形属性上。
    /// 在 series.symbolSize 中可以使用回调函数，基于某个维度得到 symbolSize 值。
    /// 使用 tooltip.formatter 或 series.label.formatter 可以把其他维度的值展示出来。
    /// 特别地，当只有一个轴为类目轴（axis.type 为 'category'）的时候，数据可以简化用一个一维数组表示。例如：
    /// xAxis: {
    ///     data: ['a', 'b', 'm', 'n']
    /// },
    /// series: [{
    ///     // 与 xAxis.data 一一对应。
    ///     data: [23,  44,  55,  19]
    ///     // 它其实是下面这种形式的简化：
    ///     // data: [[0, 23], [1, 44], [2, 55], [3, 19]]
    /// }]
    /// 『值』与 轴类型 的关系：
    /// 当某维度对应于数值轴（axis.type 为 'value' 或者 'log'）的时候：
    ///   其值可以为 number（例如 12）。（也可以兼容 string 形式的 number，例如 '12'）
    /// 当某维度对应于类目轴（axis.type 为 'category'）的时候：
    ///   其值须为类目的『序数』（从 0 开始）或者类目的『字符串值』。例如：
    ///   xAxis: {
    ///       type: 'category',
    ///       data: ['星期一', '星期二', '星期三', '星期四']
    ///   },
    ///   yAxis: {
    ///       type: 'category',
    ///       data: ['a', 'b', 'm', 'n', 'p', 'q']
    ///   },
    ///   series: [{
    ///       data: [
    ///           // xAxis    yAxis
    ///           [  0,        0,    2  ], // 意思是此点位于 xAxis: '星期一', yAxis: 'a'。
    ///           [  '星期四',  2,    1  ], // 意思是此点位于 xAxis: '星期四', yAxis: 'm'。
    ///           [  2,       'p',   2  ], // 意思是此点位于 xAxis: '星期三', yAxis: 'p'。
    ///           [  3,        3,    5  ]
    ///       ]
    ///   }]
    ///   双类目轴的示例可以参考 Github Punchcard 示例。
    /// 当某维度对应于时间轴（type 为 'time'）的时候，值可以为：
    /// 一个时间戳，如 1484141700832，表示 UTC 时间。
    /// 或者字符串形式的时间描述：
    /// ISO 8601 的子集，只包含这些形式（这几种格式，除非指明时区，否则均表示本地时间，与 moment 一致）：
    /// 部分年月日时间: '2012-03', '2012-03-01', '2012-03-01 05', '2012-03-01 05:06'.
    /// 使用 'T' 或空格分割: '2012-03-01T12:22:33.123', '2012-03-01 12:22:33.123'.
    /// 时区设定: '2012-03-01T12:22:33Z', '2012-03-01T12:22:33+8000', '2012-03-01T12:22:33-05:00'.
    /// 其他的时间字符串，包括（均表示本地时间）:
    /// '2012', '2012-3-1', '2012/3/1', '2012/03/01',
    /// '2009/6/12 2:00', '2009/6/12 2:05:08', '2009/6/12 2:05:08.123'
    /// 或者用户自行初始化的 Date 实例：
    /// 注意，用户自行初始化 Date 实例的时候，浏览器的行为有差异，不同字符串的表示也不同。
    /// 例如：在 chrome 中，new Date('2012-01-01') 表示 UTC 时间的 2012 年 1 月 1 日，而 new Date('2012-1-1') 和 new Date('2012/01/01') 表示本地时间的 2012 年 1 月 1 日。在 safari 中，不支持 new Date('2012-1-1') 这种表示方法。
    /// 所以，使用 new Date(dataString) 时，可使用第三方库解析（如 moment），或者使用 echarts.time.parse，或者参见 这里。
    /// 当需要对个别数据进行个性化定义时：
    /// 数组项可用对象，其中的 value 像表示具体的数值，如：
    /// [
    ///     12,
    ///     34,
    ///     {
    ///         value : 56,
    ///         //自定义标签样式，仅对该数据项有效
    ///         label: {},
    ///         //自定义特殊 itemStyle，仅对该数据项有效
    ///         itemStyle:{}
    ///     },
    ///     10
    /// ]
    /// // 或
    /// [
    ///     [12, 33],
    ///     [34, 313],
    ///     {
    ///         value: [56, 44],
    ///         label: {},
    ///         itemStyle:{}
    ///     },
    ///     [10, 33]
    /// ]
    /// 空值：
    /// 当某数据不存在时（ps：不存在不代表值为 0），可以用 '-' 或者 null 或者 undefined 或者 NaN 表示。
    /// 例如，无数据在折线图中可表现为该点是断开的，在其它图中可表示为图形不存在。
    /// </remark>
    public override Object[] Data { get; set; }

    ///// <summary>图表标注</summary>
    //public Object MarkPoint { get; set; }

    ///// <summary>图表标线</summary>
    //public Object MarkLine { get; set; }

    /// <summary>图表标域，常用于标记图表中某个范围的数据，例如标出某段时间投放了广告。</summary>
    public Object MarkArea { get; set; }

    /// <summary>热力图所有图形的 zlevel 值</summary>
    /// <remark>
    /// zlevel用于 Canvas 分层，不同zlevel值的图形会放置在不同的 Canvas 中，Canvas 分层是一种常见的优化手段。我们可以把一些图形变化频繁（例如有动画）的组件设置成一个单独的zlevel。需要注意的是过多的 Canvas 会引起内存开销的增大，在手机端上需要谨慎使用以防崩溃。
    /// zlevel 大的 Canvas 会放在 zlevel 小的 Canvas 的上面。
    /// </remark>
    public Double? Zlevel { get; set; }

    /// <summary>热力图组件的所有图形的z值</summary>
    /// <remark>
    /// 控制图形的前后顺序。z值小的图形会被z值大的图形覆盖。
    /// z相比zlevel优先级更低，而且不会创建新的 Canvas。
    /// </remark>
    public Double? Z { get; set; }

    /// <summary>图形是否不响应和触发鼠标事件，默认为 false，即响应和触发鼠标事件。</summary>
    public Boolean? Silent { get; set; }

    /// <summary>本系列特定的 tooltip 设定</summary>
    public Object Tooltip { get; set; }

}
