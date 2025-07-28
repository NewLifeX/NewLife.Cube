namespace NewLife.Cube.Charts;

/// <summary>日历坐标系组件</summary>
/// <remark>
/// 在ECharts中，我们非常有创意地实现了日历图，是通过使用日历坐标系组件来达到日历图效果的，如下方的几个示例图所示，我们可以在热力图、散点图、关系图中使用日历坐标系。
/// 在日历坐标系中使用热力图的示例:
/// 在日历坐标系中使用散点图的示例:
/// 在日历坐标系中使用关系图（以及混合图表）的示例:
/// 灵活利用 echarts 图表和坐标系的组合，以及 API，可以实现更丰富的效果。
/// 在日历中使用文字、
/// 在日历中放置饼图
/// 水平和垂直放置日历
/// 在日历坐标系可以水平放置，也可以垂直放置。如上面的例子，使用热力图时，经常是水平放置的。但是如果需要格子的尺寸大些，水平放置就过于宽了，于是也可以选择垂直放置。参见 calendar.orient。
/// 尺寸的自适应
/// 可以设置日历坐标系使他支持不同尺寸的容器（页面）大小变化的自适应。首先，和 echarts 其他组件一样，日历坐标系可以选择使用 left right top bottom width height 来描述尺寸和位置，从而将日历摆放在上下左右各种位置，并随着页面尺寸变动而改变自身尺寸。另外，也可以使用 cellSize 来固定日历格子的长宽。
/// 中西方日历习惯的支持
/// 中西方日历有所差别，西方常使用星期日作为一周的第一天，中国使用星期一为一周的第一天。日历坐标系做了这种切换的支持。参见 calendar.dayLabel.firstDay。
/// 另外，日历上的『月份』和『星期几』的文字，也可以较方便的切换中英文，甚至自定义。参见 calendar.dayLabel.nameMap calendar.monthLabel.nameMap。
/// </remark>
public class Calendar
{
    /// <summary>组件 ID</summary>
    /// <remark>默认不指定。指定则可用于在 option 或者 API 中引用组件。</remark>
    public String Id { get; set; }

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

    /// <summary>calendar组件离容器左侧的距离</summary>
    /// <remark>
    /// left 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比，也可以是 'left', 'center', 'right'。
    /// 如果 left 的值为 'left', 'center', 'right'，组件会根据相应的位置自动对齐。
    /// </remark>
    public Object Left { get; set; }

    /// <summary>calendar组件离容器上侧的距离</summary>
    /// <remark>
    /// top 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比，也可以是 'top', 'middle', 'bottom'。
    /// 如果 top 的值为 'top', 'middle', 'bottom'，组件会根据相应的位置自动对齐。
    /// </remark>
    public Object Top { get; set; }

    /// <summary>calendar组件离容器右侧的距离</summary>
    /// <remark>
    /// right 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比。
    /// 默认自适应。
    /// </remark>
    public Object Right { get; set; }

    /// <summary>calendar组件离容器下侧的距离</summary>
    /// <remark>
    /// bottom 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比。
    /// 默认自适应。
    /// </remark>
    public Object Bottom { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 日历坐标的整体宽度
    /// 注意: 默认cellSize 为20，若设置了width的值, 则cellSize中的宽度强制转为auto;
    /// </remark>
    public Object Width { get; set; }

    /// <summary>日历坐标的整体高度</summary>
    /// <remark>注意: 默认cellSize 为20，若设置了height的值, 则cellSize中的高度强制转为auto;</remark>
    public Object Height { get; set; }

    /// <summary>必填</summary>
    /// <remark>
    /// 日历坐标的范围 支持多种格式
    /// 使用示例：
    /// // 某一年
    /// range: 2017
    /// // 某个月
    /// range: '2017-02'
    /// // 某个区间
    /// range: ['2017-01-02', '2017-02-23']
    /// // 注意 此写法会识别为['2017-01-01', '2017-02-01']
    /// range: ['2017-01', '2017-02']
    /// </remark>
    public Object Range { get; set; }

    /// <summary></summary>
    /// <remark>
    /// 日历每格框的大小，可设置单值 或数组  第一个元素是宽 第二个元素是高。
    /// 支持设置自适应：auto, 默认为高宽均为20
    /// 使用示例：
    /// // 设置宽高均为20
    /// cellSize: 20
    /// // 设置宽为20，高为40
    /// cellSize: [20, 40]
    /// // 设置宽高均为40
    /// cellSize: [40]
    /// // 设置宽高均自适应
    /// cellSize: 'auto'
    /// // 设置宽自适应，高为40
    /// cellSize: ['auto', 40]
    /// </remark>
    public Object CellSize { get; set; }

    /// <summary>日历坐标的布局朝向</summary>
    /// <remark>
    /// 可选：
    /// 'horizontal'
    /// 'vertical'
    /// </remark>
    public String Orient { get; set; }

    /// <summary>设置日历坐标分隔线的样式</summary>
    public Object SplitLine { get; set; }

    /// <summary>设置日历格的样式</summary>
    public Object ItemStyle { get; set; }

    /// <summary>设置日历坐标中 星期轴的样式</summary>
    public Object DayLabel { get; set; }

    /// <summary>设置日历坐标中 月份轴的样式</summary>
    public Object MonthLabel { get; set; }

    /// <summary>设置日历坐标中 年的样式</summary>
    public Object YearLabel { get; set; }

    /// <summary>图形是否不响应和触发鼠标事件，默认为 false，即响应和触发鼠标事件。</summary>
    public Boolean? Silent { get; set; }

}
