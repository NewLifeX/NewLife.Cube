namespace NewLife.Cube.Charts;

/// <summary>图例组件</summary>
/// <remark>
/// 图例组件展现了不同系列的标记(symbol)，颜色和名字。可以通过点击图例控制哪些系列不显示。
/// ECharts 3 中单个 echarts 实例中可以存在多个图例组件，会方便多个图例的布局。
/// 当图例数量过多时，可以使用 滚动图例（垂直） 或 滚动图例（水平），参见：legend.type
/// </remark>
public class Legend
{
    /// <summary>图例的类型</summary>
    /// <remark>
    /// 可选值：
    /// 'plain'：普通图例。缺省就是普通图例。
    /// 'scroll'：可滚动翻页的图例。当图例数量较多时可以使用。
    /// 参见 滚动图例（垂直） 或 滚动图例（水平）。
    /// 当使用 'scroll' 时，使用这些设置进行细节配置：
    /// legend.scrollDataIndex
    /// legend.pageButtonItemGap
    /// legend.pageButtonGap
    /// legend.pageButtonPosition
    /// legend.pageFormatter
    /// legend.pageIcons
    /// legend.pageIconColor
    /// legend.pageIconInactiveColor
    /// legend.pageIconSize
    /// legend.pageTextStyle
    /// legend.animation
    /// legend.animationDurationUpdate
    /// </remark>
    public String Type { get; set; }

    /// <summary>组件 ID</summary>
    /// <remark>默认不指定。指定则可用于在 option 或者 API 中引用组件。</remark>
    public String Id { get; set; }

    /// <summary>是否显示</summary>
    public Boolean? Show { get; set; }

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

    /// <summary>图例组件离容器左侧的距离</summary>
    /// <remark>
    /// left 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比，也可以是 'left', 'center', 'right'。
    /// 如果 left 的值为 'left', 'center', 'right'，组件会根据相应的位置自动对齐。
    /// </remark>
    public Object Left { get; set; }

    /// <summary>图例组件离容器上侧的距离</summary>
    /// <remark>
    /// top 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比，也可以是 'top', 'middle', 'bottom'。
    /// 如果 top 的值为 'top', 'middle', 'bottom'，组件会根据相应的位置自动对齐。
    /// </remark>
    public Object Top { get; set; }

    /// <summary>图例组件离容器右侧的距离</summary>
    /// <remark>
    /// right 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比。
    /// 默认自适应。
    /// </remark>
    public Object Right { get; set; }

    /// <summary>图例组件离容器下侧的距离</summary>
    /// <remark>
    /// bottom 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比。
    /// 默认自适应。
    /// </remark>
    public Object Bottom { get; set; }

    /// <summary>图例组件的宽度</summary>
    /// <remark>默认自适应。</remark>
    public Object Width { get; set; }

    /// <summary>图例组件的高度</summary>
    /// <remark>默认自适应。</remark>
    public Object Height { get; set; }

    /// <summary>图例列表的布局朝向</summary>
    /// <remark>
    /// 可选：
    /// 'horizontal'
    /// 'vertical'
    /// </remark>
    public String Orient { get; set; }

    /// <summary>图例标记和文本的对齐</summary>
    /// <remark>
    /// 默认自动，根据组件的位置和 orient 决定，当组件的 left 值为 'right' 以及纵向布局（orient 为 'vertical'）的时候为右对齐，即为 'right'。
    /// 可选：
    /// 'auto'
    /// 'left'
    /// 'right'
    /// </remark>
    public String Align { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 图例内边距，单位px，默认各方向内边距为5，接受数组分别设定上右下左边距。
    /// 使用示例：
    /// // 设置内边距为 5
    /// padding: 5
    /// // 设置上下的内边距为 5，左右的内边距为 10
    /// padding: [5, 10]
    /// // 分别设置四个方向的内边距
    /// padding: [
    ///     5,  // 上
    ///     10, // 右
    ///     5,  // 下
    ///     10, // 左
    /// ]
    /// </remark>
    public Object Padding { get; set; }

    /// <summary>图例每项之间的间隔</summary>
    /// <remark>横向布局时为水平间隔，纵向布局时为纵向间隔。</remark>
    public Double? ItemGap { get; set; }

    /// <summary>图例标记的图形宽度</summary>
    public Double? ItemWidth { get; set; }

    /// <summary>图例标记的图形高度</summary>
    public Double? ItemHeight { get; set; }

    /// <summary>图例的图形样式</summary>
    /// <remark>其属性的取值为 'inherit' 时，表示继承系列中的属性值。</remark>
    public Object ItemStyle { get; set; }

    /// <summary>图例图形中线的样式，用于诸如折线图图例横线的样式设置</summary>
    /// <remark>其属性的取值为 'inherit' 时，表示继承系列中的属性值。</remark>
    public Object LineStyle { get; set; }

    /// <summary>图形旋转角度，类型为 number | 'inherit'</summary>
    /// <remark>如果为 'inherit'，表示取系列的 symbolRotate。</remark>
    public Object SymbolRotate { get; set; }

    /// <summary>用来格式化图例文本，支持字符串模板和回调函数两种形式</summary>
    /// <remark>
    /// 示例：
    /// // 使用字符串模板，模板变量为图例名称 {name}
    /// formatter: 'Legend {name}'
    /// // 使用回调函数
    /// formatter: function (name) {
    ///     return 'Legend ' + name;
    /// }
    /// </remark>
    public String Formatter { get; set; }

    /// <summary>图例选择的模式，控制是否可以通过点击图例改变系列的显示状态</summary>
    /// <remark>
    /// 默认开启图例选择，可以设成 false 关闭。
    /// 除此之外也可以设成 'single' 或者  'multiple' 使用单选或者多选模式。
    /// </remark>
    public Object SelectedMode { get; set; }

    /// <summary>图例关闭时的颜色</summary>
    public String InactiveColor { get; set; }

    /// <summary>图例关闭时的描边颜色</summary>
    public String InactiveBorderColor { get; set; }

    /// <summary>图例关闭时的描边粗细</summary>
    /// <remark>
    /// 如果为 'auto' 表示：如果系列存在描边，则取 2，如果系列不存在描边，则取 0。
    /// 如果为 'inherit' 表示：始终取系列的描边粗细。
    /// </remark>
    public Object InactiveBorderWidth { get; set; }

    /// <summary>图例选中状态表</summary>
    /// <remark>
    /// 示例：
    /// selected: {
    ///     // 选中'系列1'
    ///     '系列1': true,
    ///     // 不选中'系列2'
    ///     '系列2': false
    /// }
    /// </remark>
    public Object Selected { get; set; }

    /// <summary>图例的公用文本样式</summary>
    public Object TextStyle { get; set; }

    /// <summary>图例的 tooltip 配置，配置项同 tooltip</summary>
    /// <remark>
    /// 默认不显示，可以在 legend 文字很多的时候对文字做裁剪并且开启 tooltip，如下示例：
    /// legend: {
    ///     formatter: function (name) {
    ///         return echarts.format.truncateText(name, 40, '14px Microsoft Yahei', '…');
    ///     },
    ///     tooltip: {
    ///         show: true
    ///     }
    /// }
    /// </remark>
    public Tooltip Tooltip { get; set; }

    /// <summary>图例项的 icon</summary>
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
    public String Icon { get; set; }

    /// <summary>图例的数据数组</summary>
    /// <remark>
    /// 数组项通常为一个字符串，每一项代表一个系列的 name（如果是饼图，也可以是饼图单个数据的 name）。图例组件会自动根据对应系列的图形标记（symbol）来绘制自己的颜色和标记，特殊字符串 ''（空字符串）或者 '\n'（换行字符串）用于图例的换行。
    /// 如果 data 没有被指定，会自动从当前系列中获取。多数系列会取自 series.name 或者 series.encode 的 seriesName 所指定的维度。如 饼图 and 漏斗图 等会取自 series.data 中的 name。
    /// 如果要设置单独一项的样式，也可以把该项写成配置项对象。此时必须使用 name 属性对应表示系列的 name。
    /// 示例
    /// data: [{
    ///     name: '系列1',
    ///     // 强制设置图形为圆。
    ///     icon: 'circle',
    ///     // 设置文本为红色
    ///     textStyle: {
    ///         color: 'red'
    ///     }
    /// }]
    /// </remark>
    public Object[] Data { get; set; }

    /// <summary>图例背景色，默认透明</summary>
    /// <remark>颜色可以使用 RGB 表示，比如 'rgb(128, 128, 128)'   ，如果想要加上 alpha 通道，可以使用 RGBA，比如 'rgba(128, 128, 128, 0.5)'，也可以使用十六进制格式，比如 '#ccc'</remark>
    public String BackgroundColor { get; set; }

    /// <summary>图例的边框颜色</summary>
    /// <remark>支持的颜色格式同 backgroundColor。</remark>
    public String BorderColor { get; set; }

    /// <summary>图例的边框线宽</summary>
    public Double? BorderWidth { get; set; }

    /// <summary>圆角半径，单位px，支持传入数组分别指定 4 个圆角半径</summary>
    /// <remark>
    /// 如:
    /// borderRadius: 5, // 统一设置四个角的圆角大小
    /// borderRadius: [5, 5, 0, 0] //（顺时针左上，右上，右下，左下）
    /// </remark>
    public Object BorderRadius { get; set; }

    /// <summary>图形阴影的模糊大小</summary>
    /// <remark>
    /// 该属性配合 shadowColor,shadowOffsetX, shadowOffsetY 一起设置图形的阴影效果。
    /// 示例：
    /// {
    ///     shadowColor: 'rgba(0, 0, 0, 0.5)',
    ///     shadowBlur: 10
    /// }
    /// 注意：此配置项生效的前提是，设置了 show: true 以及值不为 transparent 的背景色 backgroundColor。
    /// </remark>
    public Double? ShadowBlur { get; set; }

    /// <summary>阴影颜色</summary>
    /// <remark>
    /// 支持的格式同color。
    /// 注意：此配置项生效的前提是，设置了 show: true。
    /// </remark>
    public String ShadowColor { get; set; }

    /// <summary>阴影水平方向上的偏移距离</summary>
    /// <remark>注意：此配置项生效的前提是，设置了 show: true。</remark>
    public Double? ShadowOffsetX { get; set; }

    /// <summary>阴影垂直方向上的偏移距离</summary>
    /// <remark>注意：此配置项生效的前提是，设置了 show: true。</remark>
    public Double? ShadowOffsetY { get; set; }

    /// <summary>legend.type 为 'scroll' 时有效</summary>
    /// <remark>
    /// 图例当前最左上显示项的 dataIndex。
    /// setOption 时指定此项的话，可决定当前图例滚动到哪里。
    /// 但是，如果仅仅想改变图例翻页，一般并不调用 setOption（因为这太重量了），仅仅使用 action legendScroll 即可。
    /// 参见 滚动图例（垂直） 或 滚动图例（水平）。
    /// </remark>
    public Double? ScrollDataIndex { get; set; }

    /// <summary>legend.type 为 'scroll' 时有效</summary>
    /// <remark>
    /// 图例控制块中，按钮和页信息之间的间隔。
    /// 参见 滚动图例（垂直） 或 滚动图例（水平）。
    /// </remark>
    public Double? PageButtonItemGap { get; set; }

    /// <summary>legend.type 为 'scroll' 时有效</summary>
    /// <remark>
    /// 图例控制块和图例项之间的间隔。
    /// 参见 滚动图例（垂直） 或 滚动图例（水平）。
    /// </remark>
    public Double? PageButtonGap { get; set; }

    /// <summary>legend.type 为 'scroll' 时有效</summary>
    /// <remark>
    /// 图例控制块的位置。可选值为：
    /// 'start'：控制块在左或上。
    /// 'end'：控制块在右或下。
    /// 参见 滚动图例（垂直） 或 滚动图例（水平）。
    /// </remark>
    public String PageButtonPosition { get; set; }

    /// <summary>legend.type 为 'scroll' 时有效</summary>
    /// <remark>
    /// 图例控制块中，页信息的显示格式。默认为 '{current}/{total}'，其中 {current} 是当前页号（从 1 开始计数），{total} 是总页数。
    /// 如果 pageFormatter 使用函数，须返回字符串，参数为：
    /// {
    ///     current: number
    ///     total: number
    /// }
    /// 参见 滚动图例（垂直） 或 滚动图例（水平）。
    /// </remark>
    public String PageFormatter { get; set; }

    /// <summary>legend.type 为 'scroll' 时有效</summary>
    /// <remark>图例控制块的图标。</remark>
    public Object PageIcons { get; set; }

    /// <summary>legend.type 为 'scroll' 时有效</summary>
    /// <remark>
    /// 翻页按钮的颜色。
    /// 参见 滚动图例（垂直） 或 滚动图例（水平）。
    /// </remark>
    public String PageIconColor { get; set; }

    /// <summary>legend.type 为 'scroll' 时有效</summary>
    /// <remark>
    /// 翻页按钮不激活时（即翻页到头时）的颜色。
    /// 参见 滚动图例（垂直） 或 滚动图例（水平）。
    /// </remark>
    public String PageIconInactiveColor { get; set; }

    /// <summary>legend.type 为 'scroll' 时有效</summary>
    /// <remark>
    /// 翻页按钮的大小。可以是数字，也可以是数组，如 [10, 3]，表示 [宽，高]。
    /// 参见 滚动图例（垂直） 或 滚动图例（水平）。
    /// </remark>
    public Object PageIconSize { get; set; }

    /// <summary>legend.type 为 'scroll' 时有效</summary>
    /// <remark>图例页信息的文字样式。</remark>
    public Object PageTextStyle { get; set; }

    /// <summary>图例翻页是否使用动画</summary>
    public Boolean? Animation { get; set; }

    /// <summary>图例翻页时的动画时长</summary>
    public Double? AnimationDurationUpdate { get; set; }

    /// <summary>强调重要性</summary>
    public Object Emphasis { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v4.4.0 开始支持
    /// 图例组件中的选择器按钮，目前包括“全选”和“反选”两种功能。默认不显示，用户可手动开启，也可以手动配置每个按钮的标题。
    /// 使用方式如下：
    /// selector: [
    ///     {
    ///         // 全选
    ///         type: 'all',
    ///         // 可以是任意你喜欢的标题
    ///         title: '全选'
    ///     },
    ///     {
    ///         // 反选
    ///         type: 'inverse',
    ///         // 可以是任意你喜欢的标题
    ///         title: '反选'
    ///     }
    /// ]
    /// // 或
    /// selector: true
    /// // 或
    /// selector: ['all', 'inverse']
    /// </remark>
    public Object Selector { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v4.4.0 开始支持
    /// 选择器按钮的文本标签样式，默认显示。
    /// </remark>
    public Object SelectorLabel { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v4.4.0 开始支持
    /// 选择器的位置，可以放在图例的尾部或者头部，对应的值分别为 'end' 和 'start'。默认情况下，图例横向布局的时候，选择器放在图例的尾部；图例纵向布局的时候，选择器放在图例的头部。
    /// </remark>
    public String SelectorPosition { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v4.4.0 开始支持
    /// 选择器按钮之间的间隔。
    /// </remark>
    public Double? SelectorItemGap { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v4.4.0 开始支持
    /// 选择器按钮与图例组件之间的间隔。
    /// </remark>
    public Double? SelectorButtonGap { get; set; }

}
