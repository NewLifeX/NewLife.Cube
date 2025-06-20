namespace NewLife.Cube.Charts;

/// <summary>折线/面积图</summary>
/// <remark>
/// 折线图是用折线将各个数据点标志连接起来的图表，用于展现数据的变化趋势。可用于直角坐标系和极坐标系上。
/// Tip: 设置 areaStyle 后可以绘制面积图。
/// Tip: 配合分段型 visualMap 组件可以将折线/面积图通过不同颜色分区间。如下示例
/// </remark>
public class SeriesLine : Series
{
    //public String Type { get; set; } = "line";

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

    /// <summary></summary>
    /// <remark>
    /// 该系列使用的坐标系，可选：
    /// 'cartesian2d'
    ///   使用二维的直角坐标系（也称笛卡尔坐标系），通过 xAxisIndex, yAxisIndex指定相应的坐标轴组件。
    /// 'polar'
    ///   使用极坐标系，通过 polarIndex 指定相应的极坐标组件
    /// </remark>
    public String CoordinateSystem { get; set; }

    ///// <summary>使用的 x 轴的 index，在单个图表实例中存在多个 x 轴的时候有用。</summary>
    //public Double? XAxisIndex { get; set; }

    ///// <summary>使用的 y 轴的 index，在单个图表实例中存在多个 y轴的时候有用。</summary>
    //public Double? YAxisIndex { get; set; }

    /// <summary>使用的极坐标系的 index，在单个图表实例中存在多个极坐标系的时候有用。</summary>
    public Double? PolarIndex { get; set; }

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
    public String Symbol { get; set; }

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

    /// <summary></summary>
    /// <remark>是否显示 symbol, 如果 false 则只有在 tooltip hover 的时候显示。</remark>
    public Boolean? ShowSymbol { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 只在主轴为类目轴（axis.type 为 'category'）时有效。
    /// 可选值：
    /// 'auto'：默认，如果有足够空间则显示标志图形，否则随主轴标签间隔隐藏策略。
    /// true：显示所有图形。
    /// false：随主轴标签间隔隐藏策略。
    /// </remark>
    public Boolean? ShowAllSymbol { get; set; }

    /// <summary>是否启用图例 hover 时的联动高亮</summary>
    public Boolean? LegendHoverLink { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 数据堆叠，同个类目轴上系列配置相同的 stack 值可以堆叠放置。关于如何定制数值的堆叠方式，参见 stackStrategy。
    /// 注：目前 stack 只支持堆叠于 'value' 和 'log' 类型的类目轴上，不支持 'time' 和 'category' 类型的类目轴。
    /// 下面示例可以通过右上角 toolbox 中的堆叠切换看效果：
    /// </remark>
    public String Stack { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.3.3 开始支持
    /// 堆积数值的策略，前提是stack属性已被设置。其值可以是：
    /// 'samesign' 只在要堆叠的值与当前累积的堆叠值具有相同的正负符号时才堆叠。
    /// 'all' 堆叠所有的值，不管当前或累积的堆叠值的正负符号是什么。
    /// 'positive' 只堆积正值。
    /// 'negative' 只堆叠负值。
    /// </remark>
    public String StackStrategy { get; set; }

    /// <summary>鼠标悬浮时在图形元素上时鼠标的样式是什么</summary>
    /// <remark>同 CSS 的 cursor。</remark>
    public String Cursor { get; set; }

    /// <summary>是否连接空数据</summary>
    public Boolean? ConnectNulls { get; set; }

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

    /// <summary></summary>
    /// <remark>
    /// 从 v5.2.2 开始支持
    /// 线条和区域面积是否触发事件
    /// </remark>
    public Boolean? TriggerLineEvent { get; set; }

    /// <summary>是否是阶梯线图</summary>
    /// <remark>
    /// 可以设置为 true 显示成阶梯线图，也支持设置成 'start', 'middle', 'end' 分别配置在当前点，当前点与下个点的中间点，下个点拐弯。
    /// 不同的配置效果如下：
    /// </remark>
    public Object Step { get; set; }

    /// <summary>图形上的文本标签，可用于说明图形的一些数据信息，比如值，名称等。</summary>
    public Object Label { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 折线端点的标签。
    /// </remark>
    public Object EndLabel { get; set; }

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

    /// <summary>折线拐点标志的样式</summary>
    public Object ItemStyle { get; set; }

    /// <summary>线条样式</summary>
    /// <remark>注： 修改 lineStyle 中的颜色不会影响图例颜色，如果需要图例颜色和折线图颜色一致，需修改 itemStyle.color，线条颜色默认也会取该颜色。</remark>
    public Object LineStyle { get; set; }

    /// <summary>区域填充样式</summary>
    /// <remark>设置后显示成区域面积图。</remark>
    public Object AreaStyle { get; set; }

    /// <summary>折线图的高亮状态</summary>
    public Object Emphasis { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 折线图的淡出状态。开启 emphasis.focus 后有效。
    /// </remark>
    public Object Blur { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 折线图的选中状态。开启 selectedMode 后有效。
    /// </remark>
    public Object Select { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 选中模式的配置，表示是否支持多个选中，默认关闭，支持布尔值和字符串，字符串取值可选'single'，'multiple'，'series' 分别表示单选，多选以及选择整个系列。
    /// 从 v5.3.0 开始支持 'series'。
    /// </remark>
    public Object SelectedMode { get; set; }

    /// <summary>是否平滑曲线显示</summary>
    /// <remark>
    /// 如果是 boolean 类型，则表示是否开启平滑处理。如果是 number 类型（取值范围 0 到 1），表示平滑程度，越小表示越接近折线段，反之则反。设为 true 时相当于设为 0.5。
    /// 如果需要修改平滑算法，请参考 smoothMonotone。
    /// </remark>
    public Object Smooth { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 折线平滑后是否在一个维度上保持单调性，可以设置成'x', 'y'来指明是在 x 轴或者 y 轴上保持单调性。
    /// 通常在双数值轴上使用。
    /// 下面两张图分别是双数值轴中的折线图smoothMonotone不设置以及设置为'x'的区别。
    /// 不设置smoothMonotone:
    /// 设置为 'x':
    /// </remark>
    public String SmoothMonotone { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 折线图在数据量远大于像素点时候的降采样策略，开启后可以有效的优化图表的绘制效率，默认关闭，也就是全部绘制不过滤数据点。
    /// 可选：
    /// 'lttb' 采用 Largest-Triangle-Three-Bucket 算法，可以最大程度保证采样后线条的趋势，形状和极值。
    /// 'average' 取过滤点的平均值
    /// 'min' 取过滤点的最小值
    /// 'max' 取过滤点的最大值
    /// 'minmax' 取过滤点绝对值的最大极值 (从 v5.5.0 开始支持)
    /// 'sum' 取过滤点的和
    /// </remark>
    public String Sampling { get; set; }

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

    /// <summary>折线图所有图形的 zlevel 值</summary>
    /// <remark>
    /// zlevel用于 Canvas 分层，不同zlevel值的图形会放置在不同的 Canvas 中，Canvas 分层是一种常见的优化手段。我们可以把一些图形变化频繁（例如有动画）的组件设置成一个单独的zlevel。需要注意的是过多的 Canvas 会引起内存开销的增大，在手机端上需要谨慎使用以防崩溃。
    /// zlevel 大的 Canvas 会放在 zlevel 小的 Canvas 的上面。
    /// </remark>
    public Double? Zlevel { get; set; }

    /// <summary>折线图组件的所有图形的z值</summary>
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
