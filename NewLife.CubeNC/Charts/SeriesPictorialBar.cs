namespace NewLife.Cube.Charts;

/// <summary>象形柱图</summary>
/// <remark>
/// 象形柱图是可以设置各种具象图形元素（如图片、SVG PathData 等）的柱状图。往往用在信息图中。用于有至少一个类目轴或时间轴的直角坐标系上。
/// 示例：
/// 
/// 布局
/// 象形柱图可以被想象为：它首先是个柱状图，但是柱状图的柱子并不显示。这些柱子我们称为『基准柱（reference bar）』，根据基准柱来定位和显示各种象形图形（包括图片）。
/// 每个象形图形根据基准柱的定位，是通过 symbolPosition、symbolOffset 来调整其于基准柱的相对位置。
/// 参见例子：
/// 可以使用 symbolSize 调整大小，从而形成各种视图效果。
/// 参见例子：
/// 
/// 象形图形类型
/// 每个图形可以配置成『单独』和『重复』两种类型，即通过 symbolRepeat 来设置。
/// 设置为 false（默认），则一个图形来代表一个数据项。
/// 设置为 true，则一组重复的图形来代表一个数据项。
/// 参见例子：
/// 每个象形图形可以是基本图形（如 'circle', 'rect', ...）、SVG PathData、图片，参见：symbolType。
/// 参见例子：
/// 可以使用 symbolClip 对图形进行剪裁。
/// 参见例子：
/// </remark>
public class SeriesPictorialBar : Series
{
    /// <summary>实例化象形柱图</summary>
    public SeriesPictorialBar() => Type = "pictorialBar";

    //public String Type { get; set; } = "pictorialBar";

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

    /// <summary>是否启用图例 hover 时的联动高亮</summary>
    public Boolean? LegendHoverLink { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 该系列使用的坐标系，可选：
    /// 'cartesian2d'
    ///   使用二维的直角坐标系（也称笛卡尔坐标系），通过 xAxisIndex, yAxisIndex指定相应的坐标轴组件。
    /// </remark>
    public String CoordinateSystem { get; set; }

    ///// <summary>使用的 x 轴的 index，在单个图表实例中存在多个 x 轴的时候有用。</summary>
    //public Double? XAxisIndex { get; set; }

    ///// <summary>使用的 y 轴的 index，在单个图表实例中存在多个 y轴的时候有用。</summary>
    //public Double? YAxisIndex { get; set; }

    /// <summary>鼠标悬浮时在图形元素上时鼠标的样式是什么</summary>
    /// <remark>同 CSS 的 cursor。</remark>
    public String Cursor { get; set; }

    /// <summary>图形上的文本标签，可用于说明图形的一些数据信息，比如值，名称等。</summary>
    public Object Label { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 标签的视觉引导线配置。
    /// </remark>
    public Object LabelLine { get; set; }

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

    /// <summary>图形样式</summary>
    public Object ItemStyle { get; set; }

    /// <summary>高亮状态配置</summary>
    public Object Emphasis { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 淡出状态配置。开启 emphasis.focus 后有效。
    /// </remark>
    public Object Blur { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 选中状态配置。开启 selectedMode 后有效。
    /// </remark>
    public Object Select { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 选中模式的配置，表示是否支持多个选中，默认关闭，支持布尔值和字符串，字符串取值可选'single'，'multiple'，'series' 分别表示单选，多选以及选择整个系列。
    /// 从 v5.3.0 开始支持 'series'。
    /// </remark>
    public Object SelectedMode { get; set; }

    /// <summary>柱条的宽度，不设时自适应</summary>
    /// <remark>
    /// 可以是绝对值例如 40 或者百分数例如 '60%'。百分数基于自动计算出的每一类目的宽度。
    /// 在同一坐标系上，此属性会被多个 'pictorialBar' 系列共享。此属性应设置于此坐标系中最后一个 'pictorialBar' 系列上才会生效，并且是对此坐标系中所有 'pictorialBar' 系列生效。
    /// </remark>
    public Object BarWidth { get; set; }

    /// <summary>柱条的最大宽度</summary>
    /// <remark>
    /// 比 barWidth 优先级高。
    /// 可以是绝对值例如 40 或者百分数例如 '60%'。百分数基于自动计算出的每一类目的宽度。
    /// 在同一坐标系上，此属性会被多个 'pictorialBar' 系列共享。此属性应设置于此坐标系中最后一个 'pictorialBar' 系列上才会生效，并且是对此坐标系中所有 'pictorialBar' 系列生效。
    /// </remark>
    public Object BarMaxWidth { get; set; }

    /// <summary>柱条的最小宽度</summary>
    /// <remark>
    /// 在直角坐标系中，默认值是 1。否则默认值是 null。
    /// 比 barWidth 优先级高。
    /// 可以是绝对值例如 40 或者百分数例如 '60%'。百分数基于自动计算出的每一类目的宽度。
    /// 在同一坐标系上，此属性会被多个 'pictorialBar' 系列共享。此属性应设置于此坐标系中最后一个 'pictorialBar' 系列上才会生效，并且是对此坐标系中所有 'pictorialBar' 系列生效。
    /// </remark>
    public Object BarMinWidth { get; set; }

    /// <summary>柱条最小高度，可用于防止某数据项的值过小而影响交互</summary>
    public Double? BarMinHeight { get; set; }

    /// <summary>柱条最小角度，可用于防止某数据项的值过小而影响交互</summary>
    /// <remark>仅对极坐标系柱状图有效。</remark>
    public Double? BarMinAngle { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 不同系列的柱间距离，为百分比（如 '20%'，表示柱子宽度的 20%）。
    /// 如果想要两个系列的柱子重叠，可以设置 barGap 为 '-100%'。这在用柱子做背景的时候有用。
    /// 在同一坐标系上，此属性会被多个 'pictorialBar' 系列共享。此属性应设置于此坐标系中最后一个 'pictorialBar' 系列上才会生效，并且是对此坐标系中所有 'pictorialBar' 系列生效。
    /// 例子：
    /// </remark>
    public String BarGap { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 同一系列的柱间距离，默认情况下根据柱状图的系列数量计算得到合适的间距，系列较多时间距会适当调小，可设固定值
    /// 在同一坐标系上，此属性会被多个 'pictorialBar' 系列共享。此属性应设置于此坐标系中最后一个 'pictorialBar' 系列上才会生效，并且是对此坐标系中所有 'pictorialBar' 系列生效。
    /// </remark>
    public Object BarCategoryGap { get; set; }

    ///// <summary>图形类型</summary>
    ///// <remark>
    ///// ECharts 提供的标记类型包括
    ///// 'circle', 'rect', 'roundRect', 'triangle', 'diamond', 'pin', 'arrow', 'none'
    ///// 可以通过 'image://url' 设置为图片，其中 URL 为图片的链接，或者 dataURI。
    ///// URL 为图片链接例如：
    ///// 'image://http://example.website/a/b.png'
    ///// URL 为 dataURI 例如：
    ///// 'image://data:image/gif;base64,R0lGODlhEAAQAMQAAORHHOVSKudfOulrSOp3WOyDZu6QdvCchPGolfO0o/XBs/fNwfjZ0frl3/zy7////wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACH5BAkAABAALAAAAAAQABAAAAVVICSOZGlCQAosJ6mu7fiyZeKqNKToQGDsM8hBADgUXoGAiqhSvp5QAnQKGIgUhwFUYLCVDFCrKUE1lBavAViFIDlTImbKC5Gm2hB0SlBCBMQiB0UjIQA7'
    ///// 可以通过 'path://' 将图标设置为任意的矢量路径。这种方式相比于使用图片的方式，不用担心因为缩放而产生锯齿或模糊，而且可以设置为任意颜色。路径图形会自适应调整为合适的大小。路径的格式参见 SVG PathData。可以从 Adobe Illustrator 等工具编辑导出。
    ///// 例如：
    ///// 'path://M30.9,53.2C16.8,53.2,5.3,41.7,5.3,27.6S16.8,2,30.9,2C45,2,56.4,13.5,56.4,27.6S45,53.2,30.9,53.2z M30.9,3.5C17.6,3.5,6.8,14.4,6.8,27.6c0,13.3,10.8,24.1,24.101,24.1C44.2,51.7,55,40.9,55,27.6C54.9,14.4,44.1,3.5,30.9,3.5z M36.9,35.8c0,0.601-0.4,1-0.9,1h-1.3c-0.5,0-0.9-0.399-0.9-1V19.5c0-0.6,0.4-1,0.9-1H36c0.5,0,0.9,0.4,0.9,1V35.8z M27.8,35.8 c0,0.601-0.4,1-0.9,1h-1.3c-0.5,0-0.9-0.399-0.9-1V19.5c0-0.6,0.4-1,0.9-1H27c0.5,0,0.9,0.4,0.9,1L27.8,35.8L27.8,35.8z'
    ///// 例子：
    ///// 此属性可以被设置在系列的 根部，表示对此系列中所有数据都生效；也可以被设置在 data 中的 每个数据项中，表示只对此数据项生效。
    ///// 例如：
    ///// series: [{
    /////     symbol: ... // 对 data 中所有数据项生效。
    /////     data: [23, 56]
    ///// }]
    ///// 或者
    ///// series: [{
    /////     data: [{
    /////         value: 23
    /////         symbol: ... // 只对此数据项生效
    /////     }, {
    /////         value: 56
    /////         symbol: ... // 只对此数据项生效
    /////     }]
    ///// }]
    ///// </remark>
    //public String Symbol { get; set; }

    /// <summary>图形的大小</summary>
    /// <remark>
    /// 可以用数组分开表示宽和高，例如 [20, 10] 表示标记宽为20，高为10，也可以设置成诸如 10 这样单一的数字，表示 [10, 10]。
    /// 可以设置成绝对值（如 10），也可以设置成百分比（如 '120%'、['55%', 23]）。
    /// 当设置为百分比时，图形的大小是基于 基准柱 的尺寸计算出来的。
    /// 例如，当基准柱基于 x 轴（即柱子是纵向的），symbolSize 设置为 ['30%', '50%']，那么最终图形的尺寸是：
    /// 宽度：基准柱的宽度 * 30%。
    /// 高度：
    /// 如果 symbolRepeat 为 false：基准柱的高度 * 50%。
    /// 如果 symbolRepeat 为 true：基准柱的宽度 * 50%。
    /// 基准柱基于 y 轴（即柱子是横向的）的情况类似对调可得出。
    /// 例子：
    /// 此属性可以被设置在系列的 根部，表示对此系列中所有数据都生效；也可以被设置在 data 中的 每个数据项中，表示只对此数据项生效。
    /// 例如：
    /// series: [{
    ///     symbolSize: ... // 对 data 中所有数据项生效。
    ///     data: [23, 56]
    /// }]
    /// 或者
    /// series: [{
    ///     data: [{
    ///         value: 23
    ///         symbolSize: ... // 只对此数据项生效
    ///     }, {
    ///         value: 56
    ///         symbolSize: ... // 只对此数据项生效
    ///     }]
    /// }]
    /// </remark>
    public Object SymbolSize { get; set; }

    /// <summary>图形的定位位置</summary>
    /// <remark>
    /// 可取值：
    /// 'start'：图形边缘与柱子开始的地方内切。
    /// 'end'：图形边缘与柱子结束的地方内切。
    /// 'center'：图形在柱子里居中。
    /// 例子：
    /// 此属性可以被设置在系列的 根部，表示对此系列中所有数据都生效；也可以被设置在 data 中的 每个数据项中，表示只对此数据项生效。
    /// 例如：
    /// series: [{
    ///     symbolPosition: ... // 对 data 中所有数据项生效。
    ///     data: [23, 56]
    /// }]
    /// 或者
    /// series: [{
    ///     data: [{
    ///         value: 23
    ///         symbolPosition: ... // 只对此数据项生效
    ///     }, {
    ///         value: 56
    ///         symbolPosition: ... // 只对此数据项生效
    ///     }]
    /// }]
    /// </remark>
    public String SymbolPosition { get; set; }

    /// <summary>图形相对于原本位置的偏移</summary>
    /// <remark>
    /// symbolOffset 是图形定位中最后计算的一个步骤，可以对图形计算出来的位置进行微调。
    /// 可以设置成绝对值（如 10），也可以设置成百分比（如 '120%'、['55%', 23]）。
    /// 当设置为百分比时，表示相对于自身尺寸 symbolSize 的百分比。
    /// 例如 [0, '-50%'] 就是把图形向上移动了自身尺寸的一半的位置。
    /// 例子：
    /// 此属性可以被设置在系列的 根部，表示对此系列中所有数据都生效；也可以被设置在 data 中的 每个数据项中，表示只对此数据项生效。
    /// 例如：
    /// series: [{
    ///     symbolOffset: ... // 对 data 中所有数据项生效。
    ///     data: [23, 56]
    /// }]
    /// 或者
    /// series: [{
    ///     data: [{
    ///         value: 23
    ///         symbolOffset: ... // 只对此数据项生效
    ///     }, {
    ///         value: 56
    ///         symbolOffset: ... // 只对此数据项生效
    ///     }]
    /// }]
    /// </remark>
    public Double[] SymbolOffset { get; set; }

    /// <summary>图形的旋转角度</summary>
    /// <remark>
    /// 注意，symbolRotate 并不会影响图形的定位（哪怕超出基准柱的边界），而只是单纯得绕自身中心旋转。
    /// 此属性可以被设置在系列的 根部，表示对此系列中所有数据都生效；也可以被设置在 data 中的 每个数据项中，表示只对此数据项生效。
    /// 例如：
    /// series: [{
    ///     symbolRotate: ... // 对 data 中所有数据项生效。
    ///     data: [23, 56]
    /// }]
    /// 或者
    /// series: [{
    ///     data: [{
    ///         value: 23
    ///         symbolRotate: ... // 只对此数据项生效
    ///     }, {
    ///         value: 56
    ///         symbolRotate: ... // 只对此数据项生效
    ///     }]
    /// }]
    /// </remark>
    public Double? SymbolRotate { get; set; }

    /// <summary>指定图形元素是否重复</summary>
    /// <remark>
    /// 值可为：
    /// false/null/undefined：不重复，即每个数据值用一个图形元素表示。
    /// true：使图形元素重复，即每个数据值用一组重复的图形元素表示。重复的次数依据 data 计算得到。
    /// a number：使图形元素重复，即每个数据值用一组重复的图形元素表示。重复的次数是给定的定值。
    /// 'fixed'：使图形元素重复，即每个数据值用一组重复的图形元素表示。重复的次数依据 symbolBoundingData 计算得到，即与 data 无关。这在此图形被用于做背景时有用。
    /// 例子：
    /// 此属性可以被设置在系列的 根部，表示对此系列中所有数据都生效；也可以被设置在 data 中的 每个数据项中，表示只对此数据项生效。
    /// 例如：
    /// series: [{
    ///     symbolRepeat: ... // 对 data 中所有数据项生效。
    ///     data: [23, 56]
    /// }]
    /// 或者
    /// series: [{
    ///     data: [{
    ///         value: 23
    ///         symbolRepeat: ... // 只对此数据项生效
    ///     }, {
    ///         value: 56
    ///         symbolRepeat: ... // 只对此数据项生效
    ///     }]
    /// }]
    /// </remark>
    public Object SymbolRepeat { get; set; }

    /// <summary>指定图形元素重复时，绘制的顺序</summary>
    /// <remark>
    /// 这个属性在两种情况下有用处：
    /// 当 symbolMargin 设置为负值时，重复的图形会互相覆盖，这是可以使用 symbolRepeatDirection 来指定覆盖顺序。
    /// 当 animationDelay 或 animationDelayUpdate 被使用时，symbolRepeatDirection 指定了 index 顺序。
    /// 这个属性的值可以是：'start' 或 'end'。
    /// 例子：
    /// 此属性可以被设置在系列的 根部，表示对此系列中所有数据都生效；也可以被设置在 data 中的 每个数据项中，表示只对此数据项生效。
    /// 例如：
    /// series: [{
    ///     symbolRepeatDirection: ... // 对 data 中所有数据项生效。
    ///     data: [23, 56]
    /// }]
    /// 或者
    /// series: [{
    ///     data: [{
    ///         value: 23
    ///         symbolRepeatDirection: ... // 只对此数据项生效
    ///     }, {
    ///         value: 56
    ///         symbolRepeatDirection: ... // 只对此数据项生效
    ///     }]
    /// }]
    /// </remark>
    public String SymbolRepeatDirection { get; set; }

    /// <summary>图形的两边间隔（『两边』是指其数值轴方向的两边）</summary>
    /// <remark>
    /// 可以是绝对数值（如 20），或者百分比值（如 '-30%'），表示相对于自身尺寸 symbolSize 的百分比。只有当 symbolRepeat 被使用时有意义。
    /// 可以是正值，表示间隔大；也可以是负数。当 symbolRepeat 被使用时，负数时能使图形重叠。
    /// 可以在其值结尾处加一个 "!"，如 "30%!" 或 25!，表示第一个图形的开始和最后一个图形结尾留白，不紧贴边界。默认会紧贴边界。
    /// 注意：
    /// 当 symbolRepeat 为 true/'fixed' 的时候：
    ///   这里设置的 symbolMargin 只是个参考值，真正最后的图形间隔，是根据 symbolRepeat、symbolMargin、symbolBoundingData 综合计算得到。
    /// 当 symbolRepeat 为一个固定数值的时候：
    ///   这里设置的 symbolMargin 无效。
    /// 例子：
    /// 此属性可以被设置在系列的 根部，表示对此系列中所有数据都生效；也可以被设置在 data 中的 每个数据项中，表示只对此数据项生效。
    /// 例如：
    /// series: [{
    ///     symbolMargin: ... // 对 data 中所有数据项生效。
    ///     data: [23, 56]
    /// }]
    /// 或者
    /// series: [{
    ///     data: [{
    ///         value: 23
    ///         symbolMargin: ... // 只对此数据项生效
    ///     }, {
    ///         value: 56
    ///         symbolMargin: ... // 只对此数据项生效
    ///     }]
    /// }]
    /// </remark>
    public Object SymbolMargin { get; set; }

    /// <summary>是否剪裁图形</summary>
    /// <remark>
    /// false/null/undefined：图形本身表示数值大小。
    /// true：图形被剪裁后剩余的部分表示数值大小。
    /// symbolClip 常在这种场景下使用：同时表达『总值』和『当前数值』。在这种场景下，可以使用两个系列，一个系列是完整的图形，当做『背景』来表达总数值，另一个系列是使用 symbolClip 进行剪裁过的图形，表达当前数值。
    /// 例子：
    /// 在这个例子中：
    /// 『背景系列』和『当前值系列』使用相同的 symbolBoundingData，使得绘制出的图形的大小是一样的。
    /// 『当前值系列』设置了比『背景系列』更高的 z，使得其覆盖在『背景系列』上。
    /// 此属性可以被设置在系列的 根部，表示对此系列中所有数据都生效；也可以被设置在 data 中的 每个数据项中，表示只对此数据项生效。
    /// 例如：
    /// series: [{
    ///     symbolClip: ... // 对 data 中所有数据项生效。
    ///     data: [23, 56]
    /// }]
    /// 或者
    /// series: [{
    ///     data: [{
    ///         value: 23
    ///         symbolClip: ... // 只对此数据项生效
    ///     }, {
    ///         value: 56
    ///         symbolClip: ... // 只对此数据项生效
    ///     }]
    /// }]
    /// </remark>
    public Boolean? SymbolClip { get; set; }

    /// <summary>这个属性是『指定图形界限的值』</summary>
    /// <remark>
    /// 它指定了一个 data，这个 data 映射在坐标系上的位置，是图形绘制的界限。也就是说，如果设置了 symbolBoundingData，图形的尺寸则由 symbolBoundingData 决定。
    /// 当柱子是水平的，symbolBoundingData 对应到 x 轴上，当柱子是竖直的，symbolBoundingData 对应到 y 轴上。
    /// 规则：
    /// 没有使用 symbolRepeat 时：
    ///   symbolBoundingData 缺省情况下和『参考柱』的尺寸一样。图形的尺寸由零点和 symbolBoundingData 决定。举例，当柱子是竖直的，柱子对应的 data 为 24，symbolSize 设置为 [30, '50%']，symbolBoundingData 设置为 124，那么最终图形的高度为 124 * 50% = 62。如果 symbolBoundingData 不设置，那么最终图形的高度为 24 * 50% = 12。
    /// 使用了 symbolRepeat 时：
    ///   symbolBoundingData 缺省情况取当前坐标系所显示出的最值。symbolBoundingData 定义了一个 bounding，重复的图形在这个 bounding 中，依据 symbolMargin 和 symbolRepeat 和 symbolSize 进行排布。这几个变量决定了图形的间隔大小。
    /// 在这些场景中，你可能会需要设置 symbolBoundingData：
    /// 使用了 symbolCilp 时：
    ///   使用一个系列表达『总值』，另一个系列表达『当前值』的时候，需要两个系列画出的图形一样大。那么就把两个系列的 symbolBoundingData 设为一样大。
    /// 例子：
    /// 
    /// 使用了 symbolRepeat 时：
    ///   如果需要不同柱子中的图形的间隔保持一致，那么可以把 symbolBoundingData 设为一致的数值。当然，不设置 symbolBoundingData 也是可以的，因为其缺省值就是一个定值（坐标系所显示出的最值）。
    /// 例子：
    /// 
    /// symbolBoundingData 可以是一个数组，例如 [-40, 60]，表示同时指定了正值的 symbolBoundingData 和负值的 symbolBoundingData。
    /// 参见例子：
    /// 此属性可以被设置在系列的 根部，表示对此系列中所有数据都生效；也可以被设置在 data 中的 每个数据项中，表示只对此数据项生效。
    /// 例如：
    /// series: [{
    ///     symbolBoundingData: ... // 对 data 中所有数据项生效。
    ///     data: [23, 56]
    /// }]
    /// 或者
    /// series: [{
    ///     data: [{
    ///         value: 23
    ///         symbolBoundingData: ... // 只对此数据项生效
    ///     }, {
    ///         value: 56
    ///         symbolBoundingData: ... // 只对此数据项生效
    ///     }]
    /// }]
    /// </remark>
    public Object SymbolBoundingData { get; set; }

    /// <summary>可以使用图片作为图形的 pattern</summary>
    /// <remark>
    /// var textureImg = new Image();
    /// textureImg.src = 'data:image/jpeg;base64,...'; // dataURI
    /// // 或者
    /// // textureImg.src = 'http://example.website/xx.png'; // URL
    /// ...
    /// itemStyle: {
    ///     color: {
    ///         image: textureImg,
    ///         repeat: 'repeat'
    ///     }
    /// }
    /// 这时候，symbolPatternSize 指定了 pattern 的缩放尺寸。比如 symbolPatternSize 为 400 时表示图片显示为 400px * 400px 的尺寸。
    /// 例子：
    /// 此属性可以被设置在系列的 根部，表示对此系列中所有数据都生效；也可以被设置在 data 中的 每个数据项中，表示只对此数据项生效。
    /// 例如：
    /// series: [{
    ///     symbolPatternSize: ... // 对 data 中所有数据项生效。
    ///     data: [23, 56]
    /// }]
    /// 或者
    /// series: [{
    ///     data: [{
    ///         value: 23
    ///         symbolPatternSize: ... // 只对此数据项生效
    ///     }, {
    ///         value: 56
    ///         symbolPatternSize: ... // 只对此数据项生效
    ///     }]
    /// }]
    /// </remark>
    public Double? SymbolPatternSize { get; set; }

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
    public Object AnimationEasingUpdate { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 动画开始之前的延迟，支持回调函数，可以通过每个数据返回不同的 delay 时间实现更戏剧的更新动画效果。
    /// 如下示例：
    /// animationDelay: function (dataIndex, params) {
    ///     return params.index * 30;
    /// }
    /// 或者反向：
    /// animationDelay: function (dataIndex, params) {
    ///     return (params.count - 1 - params.index) * 30;
    /// }
    /// 例子：
    /// </remark>
    public Object AnimationDelay { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 数据更新动画的延迟，支持回调函数，可以通过每个数据返回不同的 delay 时间实现更戏剧的更新动画效果。
    /// 如下示例：
    /// animationDelay: function (dataIndex, params) {
    ///     return params.index * 30;
    /// }
    /// 或者反向：
    /// animationDelay: function (dataIndex, params) {
    ///     return (params.count - 1 - params.index) * 30;
    /// }
    /// 例子：
    /// </remark>
    public Object AnimationDelayUpdate { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 使用 dimensions 定义 series.data 或者 dataset.source 的每个维度的信息。
    /// 注意：如果使用了 dataset，那么可以在 dataset.dimensions 中定义 dimension ，或者在 dataset.source 的第一行/列中给出 dimension 名称。于是就不用在这里指定 dimension。但如果在这里指定了 dimensions，那么优先使用这里的。
    /// 例如：
    /// option = {
    ///     dataset: {
    ///         source: [
    ///             // 有了上面 dimensions 定义后，下面这五个维度的名称分别为：
    ///             // 'date', 'open', 'close', 'highest', 'lowest'
    ///             [12, 44, 55, 66, 2],
    ///             [23, 6, 16, 23, 1],
    ///             ...
    ///         ]
    ///     },
    ///     series: {
    ///         type: 'xxx',
    ///         // 定义了每个维度的名称。这个名称会被显示到默认的 tooltip 中。
    ///         dimensions: ['date', 'open', 'close', 'highest', 'lowest']
    ///     }
    /// }
    /// series: {
    ///     type: 'xxx',
    ///     dimensions: [
    ///         null,                // 如果此维度不想给出定义，则使用 null 即可
    ///         {type: 'ordinal'},   // 只定义此维度的类型。
    ///                              // 'ordinal' 表示离散型，一般文本使用这种类型。
    ///                              // 如果类型没有被定义，会自动猜测类型。
    ///         {name: 'good', type: 'number'},
    ///         'bad'                // 等同于 {name: 'bad'}
    ///     ]
    /// }
    /// dimensions 数组中的每一项可以是：
    /// string，如 'someName'，等同于 {name: 'someName'}
    /// Object，属性可以有：
    /// name: string。
    /// type: string，支持
    /// number，默认，表示普通数据。
    /// ordinal，对于类目、文本这些 string 类型的数据，如果需要能在数轴上使用，须是 'ordinal' 类型。ECharts 默认会自动判断这个类型。但是自动判断也是不可能很完备的，所以使用者也可以手动强制指定。
    /// float，即 Float64Array。
    /// int，即 Int32Array。
    /// time，表示时间类型。设置成 'time' 则能支持自动解析数据成时间戳（timestamp），比如该维度的数据是 '2017-05-10'，会自动被解析。时间类型的支持参见 data。
    /// displayName: 一般用于 tooltip 中维度名的展示。string 如果没有指定，默认使用 name 来展示。
    /// 值得一提的是，当定义了 dimensions 后，默认 tooltip 中对个维度的显示，会变为『竖排』，从而方便显示每个维度的名称。如果没有定义 dimensions，则默认 tooltip 会横排显示，且只显示数值没有维度名称可显示。
    /// </remark>
    public Double[] Dimensions { get; set; }

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

    /// <summary></summary>
    /// <remark>
    /// 从 v4.4.0 开始支持
    /// 是否裁剪超出坐标系部分的图形，具体裁剪效果根据系列决定：
    /// 散点图/带有涟漪特效动画的散点（气泡）图：忽略中心点超出坐标系的图形，但是不裁剪单个图形
    /// 柱状图：裁掉完全超出的柱子，但是不会裁剪只超出部分的柱子
    /// 折线图：裁掉所有超出坐标系的折线部分，拐点图形的逻辑按照散点图处理
    /// 路径图：裁掉所有超出坐标系的部分
    /// K 线图：忽略整体都超出坐标系的图形，但是不裁剪单个图形
    /// 象形柱图：裁掉所有超出坐标系的部分（从 v5.5.0 开始支持）
    /// 自定义系列：裁掉所有超出坐标系的部分
    /// 除了象形柱图和自定义系列，其它系列的默认值都为 true，及开启裁剪，如果你觉得不想要裁剪的话，可以设置成 false 关闭。
    /// </remark>
    public Boolean? Clip { get; set; }

    /// <summary>象形柱图所有图形的 zlevel 值</summary>
    /// <remark>
    /// zlevel用于 Canvas 分层，不同zlevel值的图形会放置在不同的 Canvas 中，Canvas 分层是一种常见的优化手段。我们可以把一些图形变化频繁（例如有动画）的组件设置成一个单独的zlevel。需要注意的是过多的 Canvas 会引起内存开销的增大，在手机端上需要谨慎使用以防崩溃。
    /// zlevel 大的 Canvas 会放在 zlevel 小的 Canvas 的上面。
    /// </remark>
    public Double? Zlevel { get; set; }

    /// <summary>象形柱图组件的所有图形的z值</summary>
    /// <remark>
    /// 控制图形的前后顺序。z值小的图形会被z值大的图形覆盖。
    /// z相比zlevel优先级更低，而且不会创建新的 Canvas。
    /// </remark>
    public Double? Z { get; set; }

    /// <summary>图形是否不响应和触发鼠标事件，默认为 false，即响应和触发鼠标事件。</summary>
    public Boolean? Silent { get; set; }

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
