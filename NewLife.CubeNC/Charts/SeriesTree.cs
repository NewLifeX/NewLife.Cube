namespace NewLife.Cube.Charts;

/// <summary>树图</summary>
/// <remark>
/// 树图主要用来可视化树形数据结构，是一种特殊的层次类型，具有唯一的根节点，左子树，和右子树。
/// 注意：目前不支持在单个 series 中直接绘制森林，可以通过在一个 option 中配置多个 series 实现森林
/// 树图示例：
/// 多个 series 组合成森林示例：
/// </remark>
public class SeriesTree : Series
{
    //public String Type { get; set; } = "tree";

    ///// <summary>组件 ID</summary>
    ///// <remark>默认不指定。指定则可用于在 option 或者 API 中引用组件。</remark>
    //public String Id { get; set; }

    ///// <summary></summary>
    ///// <remark>系列名称，用于tooltip的显示，legend 的图例筛选，在 setOption 更新数据和配置项时用于指定对应的系列。</remark>
    //public String Name { get; set; }

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

    /// <summary>tree组件离容器左侧的距离</summary>
    /// <remark>
    /// left 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比，也可以是 'left', 'center', 'right'。
    /// 如果 left 的值为 'left', 'center', 'right'，组件会根据相应的位置自动对齐。
    /// </remark>
    public Object Left { get; set; }

    /// <summary>tree组件离容器上侧的距离</summary>
    /// <remark>
    /// top 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比，也可以是 'top', 'middle', 'bottom'。
    /// 如果 top 的值为 'top', 'middle', 'bottom'，组件会根据相应的位置自动对齐。
    /// </remark>
    public Object Top { get; set; }

    /// <summary>tree组件离容器右侧的距离</summary>
    /// <remark>right 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比。</remark>
    public Object Right { get; set; }

    /// <summary>tree组件离容器下侧的距离</summary>
    /// <remark>bottom 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比。</remark>
    public Object Bottom { get; set; }

    /// <summary>tree组件的宽度</summary>
    public Object Width { get; set; }

    /// <summary>tree组件的高度</summary>
    public Object Height { get; set; }

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

    /// <summary>树图的布局，有 正交 和 径向 两种</summary>
    /// <remark>
    /// 这里的 正交布局 就是我们通常所说的 水平 和 垂直 方向，对应的参数取值为 'orthogonal' 。而 径向布局 是指以根节点为圆心，每一层节点为环，一层层向外发散绘制而成的布局，对应的参数取值为 'radial' 。
    /// 正交布局示例：
    /// 
    /// 径向布局示例：
    /// </remark>
    public String Layout { get; set; }

    /// <summary></summary>
    /// <remark>树图中 正交布局 的方向，也就是说只有在 layout = 'orthogonal' 的时候，该配置项才生效。对应有 水平 方向的 从左到右，从右到左；以及垂直方向的 从上到下，从下到上。取值分别为 'LR' , 'RL', 'TB', 'BT'。注意，之前的配置项值 'horizontal' 等同于 'LR'， 'vertical' 等同于 'TB'。</remark>
    public String Orient { get; set; }

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

    /// <summary></summary>
    /// <remark>
    /// 从 v4.7.0 开始支持
    /// 树图在 正交布局 下，边的形状。分别有曲线和折线两种，对应的取值是 curve 和 polyline.
    /// 注意：该配置项只在 正交布局 下有效，在经向布局下的开发环境中会报错。
    /// </remark>
    public String EdgeShape { get; set; }

    /// <summary>正交布局下当边的形状是折线时，子树中折线段分叉的位置</summary>
    /// <remark>
    /// 这里的位置指的是分叉点与子树父节点的距离占整个子树高度的百分比。默认取值是 '50%'，可以是 ['0', '100%'] 之间。
    /// 注意：该配置项只有在 edgeShape = 'polyline' 时才有效。
    /// </remark>
    public String EdgeForkPosition { get; set; }

    /// <summary>是否开启鼠标缩放和平移漫游</summary>
    /// <remark>默认不开启。如果只想要开启缩放或者平移，可以设置成 'scale' 或者 'move'。设置成 true 为都开启</remark>
    public Object Roam { get; set; }

    /// <summary>子树折叠和展开的交互，默认打开</summary>
    /// <remark>
    /// 由于绘图区域是有限的，而通常一个树图的节点可能会比较多，这样就会出现节点之间相互遮盖的问题。为了避免这一问题，可以将暂时无关的子树折叠收起，等到需要时再将其展开。如上面径向布局树图示例，节点中心用蓝色填充的就是折叠收起的子树，可以点击将其展开。
    /// 注意：如果配置自定义的图片作为节点的标记，是无法通过填充色来区分当前节点是否有折叠的子树的。而目前暂不支持，上传两张图片分别表示节点折叠和展开两种状态。所以，如果想明确地显示节点的两种状态，建议使用 ECharts 常规的标记类型，如 'emptyCircle' 等。
    /// </remark>
    public Boolean? ExpandAndCollapse { get; set; }

    /// <summary>树图初始展开的层级（深度）</summary>
    /// <remark>根节点是第 0 层，然后是第 1 层、第 2 层，... ，直到叶子节点。该配置项主要和 折叠展开 交互一起使用，目的还是为了防止一次展示过多节点，节点之间发生遮盖。如果设置为 -1 或者 null 或者 undefined，所有节点都将展开。</remark>
    public Double? InitialTreeDepth { get; set; }

    /// <summary></summary>
    /// <remark>树图中每个节点的样式，其中 itemStyle.color 表示节点的填充色，用来区别当前节点所对应的子树折叠或展开的状态。</remark>
    public Object ItemStyle { get; set; }

    /// <summary>label 描述了每个节点所对应的文本标签的样式</summary>
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

    /// <summary>定义树图边的样式</summary>
    public Object LineStyle { get; set; }

    /// <summary>树图中个图形和标签高亮的样式</summary>
    public Object Emphasis { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 淡出状态的相关配置。开启 emphasis.focus 后有效。
    /// </remark>
    public Object Blur { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 选中状态的相关配置。开启 selectedMode 后有效。
    /// </remark>
    public Object Select { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 从 v5.0.0 开始支持
    /// 选中模式的配置，表示是否支持多个选中，默认关闭，支持布尔值和字符串，字符串取值可选'single'，'multiple'，'series' 分别表示单选，多选以及选择整个系列。
    /// 从 v5.3.0 开始支持 'series'。
    /// </remark>
    public Object SelectedMode { get; set; }

    /// <summary>叶子节点的特殊配置，如上面的树图实例中，叶子节点和非叶子节点的标签位置不同。</summary>
    public Object Leaves { get; set; }

    /// <summary></summary>
    /// <remark>
    /// series-tree.data 的数据格式是树状结构的，例如：
    /// { // 注意，最外层是一个对象，代表树的根节点
    ///     name: "flare",    // 节点的名称，当前节点 label 对应的文本
    ///     label: {          // 此节点特殊的 label 配置（如果需要的话）。
    ///         ...           // label的格式参见 series-tree.label。
    ///     },
    ///     itemStyle: {      // 此节点特殊的 itemStyle 配置（如果需要的话）。
    ///         ...           // label的格式参见 series-tree.itemStyle。
    ///     },
    ///     children: [
    ///         {
    ///             name: "flex",
    ///             value: 4116,    // value 值，只在 tooltip 中显示
    ///             label: {
    ///                 ...
    ///             },
    ///             itemStyle: {
    ///                 ...
    ///             },
    ///             collapsed: null, // 如果为 true，表示此节点默认折叠。
    ///             children: [...] // 叶子节点没有 children, 可以不写
    ///         },
    ///         ...
    ///     ]
    /// };
    /// </remark>
    public new Object Data { get; set; }

    /// <summary>本系列特定的 tooltip 设定</summary>
    public Object Tooltip { get; set; }

}
