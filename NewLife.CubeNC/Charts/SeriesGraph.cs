namespace NewLife.Cube.Charts;

/// <summary></summary>
/// <remark>
/// 关系图
/// 用于展现节点以及节点之间的关系数据。
/// 示例：
/// </remark>
public class SeriesGraph : Series
{
    //public String Type { get; set; } = "graph";

    ///// <summary>组件 ID</summary>
    ///// <remark>默认不指定。指定则可用于在 option 或者 API 中引用组件。</remark>
    //public String Id { get; set; }

    ///// <summary></summary>
    ///// <remark>系列名称，用于tooltip的显示，legend 的图例筛选，在 setOption 更新数据和配置项时用于指定对应的系列。</remark>
    //public String Name { get; set; }

    /// <summary>是否启用图例 hover 时的联动高亮</summary>
    public Boolean? LegendHoverLink { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 该系列使用的坐标系，可选：
    /// null 或者 'none'
    ///   无坐标系。
    /// 'cartesian2d'
    ///   使用二维的直角坐标系（也称笛卡尔坐标系），通过 xAxisIndex, yAxisIndex指定相应的坐标轴组件。
    /// 'polar'
    ///   使用极坐标系，通过 polarIndex 指定相应的极坐标组件
    /// 'geo'
    ///   使用地理坐标系，通过 geoIndex 指定相应的地理坐标系组件。
    /// 'calendar'
    ///   使用日历坐标系，通过 calendarIndex 指定相应的日历坐标系组件。
    /// 'none'
    ///   不使用坐标系。
    /// </remark>
    public String CoordinateSystem { get; set; }

    ///// <summary>使用的 x 轴的 index，在单个图表实例中存在多个 x 轴的时候有用。</summary>
    //public Double? XAxisIndex { get; set; }

    ///// <summary>使用的 y 轴的 index，在单个图表实例中存在多个 y轴的时候有用。</summary>
    //public Double? YAxisIndex { get; set; }

    /// <summary>使用的极坐标系的 index，在单个图表实例中存在多个极坐标系的时候有用。</summary>
    public Double? PolarIndex { get; set; }

    /// <summary>使用的地理坐标系的 index，在单个图表实例中存在多个地理坐标系的时候有用。</summary>
    public Double? GeoIndex { get; set; }

    /// <summary>使用的日历坐标系的 index，在单个图表实例中存在多个日历坐标系的时候有用。</summary>
    public Double? CalendarIndex { get; set; }

    /// <summary>当前视角的中心点</summary>
    /// <remark>
    /// 可以是包含两个 number 类型（表示像素值）或 string 类型（表示相对容器的百分比）的数组。
    /// 从 5.3.3 版本开始支持 string 类型。
    /// 例如：
    /// center: [115.97, '30%']
    /// </remark>
    public Double[] Center { get; set; }

    /// <summary>当前视角的缩放比例</summary>
    public Double? Zoom { get; set; }

    /// <summary>图的布局</summary>
    /// <remark>
    /// 可选：
    /// 'none' 不采用任何布局，使用节点中提供的 x， y 作为节点的位置。
    /// 'circular' 采用环形布局，见示例 Les Miserables，布局相关的配置项见 graph.circular
    /// 'force' 采用力引导布局，见示例 Force，布局相关的配置项见 graph.force
    /// </remark>
    public String Layout { get; set; }

    /// <summary>环形布局相关配置</summary>
    public Object Circular { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 力引导布局相关的配置项，力引导布局是模拟弹簧电荷模型在每两个节点之间添加一个斥力，每条边的两个节点之间添加一个引力，每次迭代节点会在各个斥力和引力的作用下移动位置，多次迭代后节点会静止在一个受力平衡的位置，达到整个模型的能量最小化。
    /// 力引导布局的结果有良好的对称性和局部聚合性，也比较美观。
    /// </remark>
    public Object Force { get; set; }

    /// <summary>是否开启鼠标缩放和平移漫游</summary>
    /// <remark>默认不开启。如果只想要开启缩放或者平移，可以设置成 'scale' 或者 'move'。设置成 true 为都开启</remark>
    public Object Roam { get; set; }

    /// <summary>滚轮缩放的极限控制，通过 min 和 max 限制最小和最大的缩放值。</summary>
    public Object ScaleLimit { get; set; }

    /// <summary>鼠标漫游缩放时节点的相应缩放比例</summary>
    /// <remark>当设为0时节点不随着鼠标的缩放而缩放</remark>
    public Double? NodeScaleRatio { get; set; }

    /// <summary>节点是否可拖拽</summary>
    /// <remark>注意：v5.4.1 之前的版本只在使用力引导布局的时候才有用。</remark>
    public Boolean? Draggable { get; set; }

    /// <summary>节点标记的图形</summary>
    /// <remark>
    /// ECharts 提供的标记类型包括
    /// 'circle', 'rect', 'roundRect', 'triangle', 'diamond', 'pin', 'arrow', 'none'
    /// 可以通过 'image://url' 设置为图片，其中 URL 为图片的链接，或者 dataURI。
    /// URL 为图片链接例如：
    /// 'image://http://example.website/a/b.png'
    /// URL 为 dataURI 例如：
    /// 'image://data:image/gif;base64,R0lGODlhEAAQAMQAAORHHOVSKudfOulrSOp3WOyDZu6QdvCchPGolfO0o/XBs/fNwfjZ0frl3/zy7////wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACH5BAkAABAALAAAAAAQABAAAAVVICSOZGlCQAosJ6mu7fiyZeKqNKToQGDsM8hBADgUXoGAiqhSvp5QAnQKGIgUhwFUYLCVDFCrKUE1lBavAViFIDlTImbKC5Gm2hB0SlBCBMQiB0UjIQA7'
    /// 可以通过 'path://' 将图标设置为任意的矢量路径。这种方式相比于使用图片的方式，不用担心因为缩放而产生锯齿或模糊，而且可以设置为任意颜色。路径图形会自适应调整为合适的大小。路径的格式参见 SVG PathData。可以从 Adobe Illustrator 等工具编辑导出。
    /// 例如：
    /// 'path://M30.9,53.2C16.8,53.2,5.3,41.7,5.3,27.6S16.8,2,30.9,2C45,2,56.4,13.5,56.4,27.6S45,53.2,30.9,53.2z M30.9,3.5C17.6,3.5,6.8,14.4,6.8,27.6c0,13.3,10.8,24.1,24.101,24.1C44.2,51.7,55,40.9,55,27.6C54.9,14.4,44.1,3.5,30.9,3.5z M36.9,35.8c0,0.601-0.4,1-0.9,1h-1.3c-0.5,0-0.9-0.399-0.9-1V19.5c0-0.6,0.4-1,0.9-1H36c0.5,0,0.9,0.4,0.9,1V35.8z M27.8,35.8 c0,0.601-0.4,1-0.9,1h-1.3c-0.5,0-0.9-0.399-0.9-1V19.5c0-0.6,0.4-1,0.9-1H27c0.5,0,0.9,0.4,0.9,1L27.8,35.8L27.8,35.8z'
    /// </remark>
    public override String Symbol { get; set; }

    /// <summary></summary>
    /// <remark>节点标记的大小，可以设置成诸如 10 这样单一的数字，也可以用数组分开表示宽和高，例如 [20, 10] 表示标记宽为20，高为10。</remark>
    public Object SymbolSize { get; set; }

    /// <summary>节点标记的旋转角度（而非弧度）</summary>
    /// <remark>正值表示逆时针旋转。注意在 markLine 中当 symbol 为 'arrow' 时会忽略 symbolRotate 强制设置为切线的角度。</remark>
    public Double? SymbolRotate { get; set; }

    /// <summary></summary>
    /// <remark>如果 symbol 是 path:// 的形式，是否在缩放时保持该图形的长宽比。</remark>
    public Boolean? SymbolKeepAspect { get; set; }

    /// <summary>节点标记相对于原本位置的偏移</summary>
    /// <remark>
    /// 默认情况下，标记会居中置放在数据对应的位置，但是如果 symbol 是自定义的矢量路径或者图片，就有可能不希望 symbol 居中。这时候可以使用该配置项配置 symbol 相对于原本居中的偏移，可以是绝对的像素值，也可以是相对的百分比。
    /// 例如 [0, '-50%'] 就是把自己向上移动了一半的位置，在 symbol 图形是气泡的时候可以让图形下端的箭头对准数据点。
    /// </remark>
    public Double[] SymbolOffset { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 边两端的标记类型，可以是一个数组分别指定两端，也可以是单个统一指定。默认不显示标记，常见的可以设置为箭头，如下：
    /// edgeSymbol: ['circle', 'arrow']
    /// </remark>
    public Object EdgeSymbol { get; set; }

    /// <summary>边两端的标记大小，可以是一个数组分别指定两端，也可以是单个统一指定。</summary>
    public Object EdgeSymbolSize { get; set; }

    /// <summary>鼠标悬浮时在图形元素上时鼠标的样式是什么</summary>
    /// <remark>同 CSS 的 cursor。</remark>
    public String Cursor { get; set; }

    /// <summary>图形样式</summary>
    public Object ItemStyle { get; set; }

    /// <summary>关系边的公用线条样式</summary>
    /// <remark>其中 lineStyle.color 支持设置为'source'或者'target'特殊值，此时边会自动取源节点或目标节点的颜色作为自己的颜色。</remark>
    public Object LineStyle { get; set; }

    /// <summary>图形上的文本标签，可用于说明图形的一些数据信息，比如值，名称等。</summary>
    public Object Label { get; set; }

    /// <summary>边的标签</summary>
    public Object EdgeLabel { get; set; }

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

    /// <summary>高亮状态的图形样式</summary>
    public Object Emphasis { get; set; }

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

    /// <summary>节点分类的类目，可选</summary>
    /// <remark>如果节点有分类的话可以通过 data[i].category 指定每个节点的类目，类目的样式会被应用到节点样式上。图例也可以基于categories名字展现和筛选。</remark>
    public Object[] Categories { get; set; }

    /// <summary>针对节点之间存在多边的情况，自动计算各边曲率，默认不开启</summary>
    /// <remark>
    /// 设置为 true 时，开启自动曲率计算，默认边曲率数组长度为 20，如果两点间边数大于 20，请使用 number 或 Array 设置边曲率数组。
    /// 设置为 number 时，表示两点间边曲率数组的长度，由内部算法给出计算结果。
    /// 设置为 Array 时，表示直接指定边曲率数组，多边曲率会从数组中直接按顺序选取。
    /// 注意： 如果设置 lineStyle.curveness 则此属性失效。
    /// </remark>
    public Object AutoCurveness { get; set; }

    /// <summary>关系图的节点数据列表</summary>
    /// <remark>
    /// data: [{
    ///     name: '1',
    ///     x: 10,
    ///     y: 10,
    ///     value: 10
    /// }, {
    ///     name: '2',
    ///     x: 100,
    ///     y: 100,
    ///     value: 20,
    ///     symbolSize: 20,
    ///     itemStyle: {
    ///         color: 'red'
    ///     }
    /// }]
    /// 注意: 节点的name不能重复。
    /// </remark>
    public override Object[] Data { get; set; }

    /// <summary>别名</summary>
    /// <remark>同 data</remark>
    public Double[] Nodes { get; set; }

    /// <summary>节点间的关系数据</summary>
    /// <remark>
    /// 示例：
    /// links: [{
    ///     source: 'n1',
    ///     target: 'n2'
    /// }, {
    ///     source: 'n2',
    ///     target: 'n3'
    /// }]
    /// </remark>
    public Object[] Links { get; set; }

    /// <summary>别名</summary>
    /// <remark>同 links</remark>
    public Double[] Edges { get; set; }

    ///// <summary>图表标注</summary>
    //public Object MarkPoint { get; set; }

    ///// <summary>图表标线</summary>
    //public Object MarkLine { get; set; }

    /// <summary>图表标域，常用于标记图表中某个范围的数据，例如标出某段时间投放了广告。</summary>
    public Object MarkArea { get; set; }

    /// <summary>所有图形的 zlevel 值</summary>
    /// <remark>
    /// zlevel用于 Canvas 分层，不同zlevel值的图形会放置在不同的 Canvas 中，Canvas 分层是一种常见的优化手段。我们可以把一些图形变化频繁（例如有动画）的组件设置成一个单独的zlevel。需要注意的是过多的 Canvas 会引起内存开销的增大，在手机端上需要谨慎使用以防崩溃。
    /// zlevel 大的 Canvas 会放在 zlevel 小的 Canvas 的上面。
    /// </remark>
    public Double? Zlevel { get; set; }

    /// <summary>组件的所有图形的z值</summary>
    /// <remark>
    /// 控制图形的前后顺序。z值小的图形会被z值大的图形覆盖。
    /// z相比zlevel优先级更低，而且不会创建新的 Canvas。
    /// </remark>
    public Double? Z { get; set; }

    /// <summary>组件离容器左侧的距离</summary>
    /// <remark>
    /// left 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比，也可以是 'left', 'center', 'right'。
    /// 如果 left 的值为 'left', 'center', 'right'，组件会根据相应的位置自动对齐。
    /// </remark>
    public Object Left { get; set; }

    /// <summary>组件离容器上侧的距离</summary>
    /// <remark>
    /// top 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比，也可以是 'top', 'middle', 'bottom'。
    /// 如果 top 的值为 'top', 'middle', 'bottom'，组件会根据相应的位置自动对齐。
    /// </remark>
    public Object Top { get; set; }

    /// <summary>组件离容器右侧的距离</summary>
    /// <remark>
    /// right 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比。
    /// 默认自适应。
    /// </remark>
    public Object Right { get; set; }

    /// <summary>组件离容器下侧的距离</summary>
    /// <remark>
    /// bottom 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比。
    /// 默认自适应。
    /// </remark>
    public Object Bottom { get; set; }

    /// <summary>组件的宽度</summary>
    public Object Width { get; set; }

    /// <summary>组件的高度</summary>
    public Object Height { get; set; }

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

    /// <summary>本系列特定的 tooltip 设定</summary>
    public Object Tooltip { get; set; }

}
