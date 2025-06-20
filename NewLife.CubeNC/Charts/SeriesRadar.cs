namespace NewLife.Cube.Charts;

/// <summary>雷达图</summary>
/// <remark>
/// 雷达图主要用于表现多变量的数据，例如球员的各个属性分析。依赖 radar 组件。
/// 下面是 AQI 数据用雷达图表现的示例。
/// </remark>
public class SeriesRadar : Series
{
    //public String Type { get; set; } = "radar";

    ///// <summary>组件 ID</summary>
    ///// <remark>默认不指定。指定则可用于在 option 或者 API 中引用组件。</remark>
    //public String Id { get; set; }

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

    /// <summary>雷达图所使用的 radar 组件的 index</summary>
    public Double? RadarIndex { get; set; }

    /// <summary>标记的图形</summary>
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
    /// 如果需要每个数据的图形不一样，可以设置为如下格式的回调函数：
    /// (value: Array|number, params: Object) => string
    /// 其中第一个参数 value 为 data 中的数据值。第二个参数params 是其它的数据项参数。
    /// </remark>
    public override String Symbol { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 标记的大小，可以设置成诸如 10 这样单一的数字，也可以用数组分开表示宽和高，例如 [20, 10] 表示标记宽为20，高为10。
    /// 如果需要每个数据的图形大小不一样，可以设置为如下格式的回调函数：
    /// (value: Array|number, params: Object) => number|Array
    /// 其中第一个参数 value 为 data 中的数据值。第二个参数params 是其它的数据项参数。
    /// </remark>
    public Object SymbolSize { get; set; }

    /// <summary>标记的旋转角度（而非弧度）</summary>
    /// <remark>
    /// 正值表示逆时针旋转。注意在 markLine 中当 symbol 为 'arrow' 时会忽略 symbolRotate 强制设置为切线的角度。
    /// 如果需要每个数据的旋转角度不一样，可以设置为如下格式的回调函数：
    /// (value: Array|number, params: Object) => number
    /// 其中第一个参数 value 为 data 中的数据值。第二个参数params 是其它的数据项参数。
    /// 从 4.8.0 开始支持回调函数。
    /// </remark>
    public Object SymbolRotate { get; set; }

    /// <summary></summary>
    /// <remark>如果 symbol 是 path:// 的形式，是否在缩放时保持该图形的长宽比。</remark>
    public Boolean? SymbolKeepAspect { get; set; }

    /// <summary>标记相对于原本位置的偏移</summary>
    /// <remark>
    /// 默认情况下，标记会居中置放在数据对应的位置，但是如果 symbol 是自定义的矢量路径或者图片，就有可能不希望 symbol 居中。这时候可以使用该配置项配置 symbol 相对于原本居中的偏移，可以是绝对的像素值，也可以是相对的百分比。
    /// 例如 [0, '-50%'] 就是把自己向上移动了一半的位置，在 symbol 图形是气泡的时候可以让图形下端的箭头对准数据点。
    /// </remark>
    public Double[] SymbolOffset { get; set; }

    /// <summary>图形上的文本标签，可用于说明图形的一些数据信息，比如值，名称等。</summary>
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

    /// <summary>折线拐点标志的样式</summary>
    public Object ItemStyle { get; set; }

    /// <summary>线条样式</summary>
    public Object LineStyle { get; set; }

    /// <summary>区域填充样式</summary>
    public Object AreaStyle { get; set; }

    /// <summary>高亮状态的配置</summary>
    public Object Emphasis { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 淡出状态的配置。开启 emphasis.focus 后有效。
    /// </remark>
    public Object Blur { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 选中状态的配置。开启 selectedMode 后有效。
    /// </remark>
    public Object Select { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 选中模式的配置，表示是否支持多个选中，默认关闭，支持布尔值和字符串，字符串取值可选'single'，'multiple'，'series' 分别表示单选，多选以及选择整个系列。
    /// 从 v5.3.0 开始支持 'series'。
    /// </remark>
    public Object SelectedMode { get; set; }

    /// <summary>该系列所有数据项的组 ID，优先级低于groupId</summary>
    /// <remark>详见series.data.groupId。</remark>
    public String DataGroupId { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 雷达图的数据是多变量（维度）的，如下示例：
    /// data : [
    ///     {
    ///         value : [4300, 10000, 28000, 35000, 50000, 19000],
    ///         name : '预算分配（Allocated Budget）'
    ///     },
    ///     {
    ///         value : [5000, 14000, 28000, 31000, 42000, 21000],
    ///         name : '实际开销（Actual Spending）'
    ///     }
    /// ]
    /// 其中的value项数组是具体的数据，每个值跟 radar.indicator 一一对应。
    /// </remark>
    public override Object[] Data { get; set; }

    /// <summary>雷达图所有图形的 zlevel 值</summary>
    /// <remark>
    /// zlevel用于 Canvas 分层，不同zlevel值的图形会放置在不同的 Canvas 中，Canvas 分层是一种常见的优化手段。我们可以把一些图形变化频繁（例如有动画）的组件设置成一个单独的zlevel。需要注意的是过多的 Canvas 会引起内存开销的增大，在手机端上需要谨慎使用以防崩溃。
    /// zlevel 大的 Canvas 会放在 zlevel 小的 Canvas 的上面。
    /// </remark>
    public Double? Zlevel { get; set; }

    /// <summary>雷达图组件的所有图形的z值</summary>
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

    /// <summary></summary>
    /// <remark>
    /// 从 v5.2.0 开始支持
    /// 全局过渡动画相关的配置。
    /// 全局过渡动画（Universal Transition）提供了任意系列之间进行变形动画的功能。开启该功能后，每次setOption，相同id的系列之间会自动关联进行动画的过渡，更细粒度的关联配置见universalTransition.seriesKey配置。
    /// 通过配置数据项的groupId和childGroupId，还可以实现诸如下钻，聚合等一对多或者多对一的动画。
    /// 可以直接在系列中配置 universalTransition: true 开启该功能。也可以提供一个对象进行更多属性的配置。
    /// </remark>
    public Object UniversalTransition { get; set; }

    /// <summary>本系列特定的 tooltip 设定</summary>
    public Object Tooltip { get; set; }

}
