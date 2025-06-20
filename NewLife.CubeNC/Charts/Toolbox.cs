using System;
using System.Collections.Generic;

namespace NewLife.Cube.Charts;

/// <summary>工具栏</summary>
/// <remark>
/// 内置有导出图片，数据视图，动态类型切换，数据区域缩放，重置五个工具。
/// 如下示例：
/// </remark>
public class Toolbox
{
    /// <summary>组件 ID</summary>
    /// <remark>默认不指定。指定则可用于在 option 或者 API 中引用组件。</remark>
    public String Id { get; set; }

    /// <summary>是否显示工具栏组件</summary>
    public Boolean? Show { get; set; }

    /// <summary>工具栏 icon 的布局朝向</summary>
    /// <remark>
    /// 可选：
    /// 'horizontal'
    /// 'vertical'
    /// </remark>
    public String Orient { get; set; }

    /// <summary>工具栏 icon 的大小</summary>
    public Double? ItemSize { get; set; }

    /// <summary>工具栏 icon 每项之间的间隔</summary>
    /// <remark>横向布局时为水平间隔，纵向布局时为纵向间隔。</remark>
    public Double? ItemGap { get; set; }

    /// <summary>是否在鼠标 hover 的时候显示每个工具 icon 的标题。</summary>
    public Boolean? ShowTitle { get; set; }

    /// <summary>各工具配置项</summary>
    /// <remark>
    /// 除了各个内置的工具按钮外，还可以自定义工具按钮。
    /// 注意，自定义的工具名字，只能以 my 开头，例如下例中的 myTool1，myTool2：
    /// {
    ///     toolbox: {
    ///         feature: {
    ///             myTool1: {
    ///                 show: true,
    ///                 title: '自定义扩展方法1',
    ///                 icon: 'path://M432.45,595.444c0,2.177-4.661,6.82-11.305,6.82c-6.475,0-11.306-4.567-11.306-6.82s4.852-6.812,11.306-6.812C427.841,588.632,432.452,593.191,432.45,595.444L432.45,595.444z M421.155,589.876c-3.009,0-5.448,2.495-5.448,5.572s2.439,5.572,5.448,5.572c3.01,0,5.449-2.495,5.449-5.572C426.604,592.371,424.165,589.876,421.155,589.876L421.155,589.876z M421.146,591.891c-1.916,0-3.47,1.589-3.47,3.549c0,1.959,1.554,3.548,3.47,3.548s3.469-1.589,3.469-3.548C424.614,593.479,423.062,591.891,421.146,591.891L421.146,591.891zM421.146,591.891',
    ///                 onclick: function (){
    ///                     alert('myToolHandler1')
    ///                 }
    ///             },
    ///             myTool2: {
    ///                 show: true,
    ///                 title: '自定义扩展方法',
    ///                 icon: 'image://https://echarts.apache.org/zh/images/favicon.png',
    ///                 onclick: function (){
    ///                     alert('myToolHandler2')
    ///                 }
    ///             }
    ///         }
    ///     }
    /// }
    /// </remark>
    public Object Feature { get; set; }

    /// <summary>公用的 icon 样式设置</summary>
    /// <remark>由于 icon 的文本信息只在 icon hover 时候才显示，所以文字相关的配置项请在 emphasis 下设置。</remark>
    public Object IconStyle { get; set; }

    public Object Emphasis { get; set; }

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

    /// <summary>工具栏组件离容器左侧的距离</summary>
    /// <remark>
    /// left 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比，也可以是 'left', 'center', 'right'。
    /// 如果 left 的值为 'left', 'center', 'right'，组件会根据相应的位置自动对齐。
    /// </remark>
    public Object Left { get; set; }

    /// <summary>工具栏组件离容器上侧的距离</summary>
    /// <remark>
    /// top 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比，也可以是 'top', 'middle', 'bottom'。
    /// 如果 top 的值为 'top', 'middle', 'bottom'，组件会根据相应的位置自动对齐。
    /// </remark>
    public Object Top { get; set; }

    /// <summary>工具栏组件离容器右侧的距离</summary>
    /// <remark>
    /// right 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比。
    /// 默认自适应。
    /// </remark>
    public Object Right { get; set; }

    /// <summary>工具栏组件离容器下侧的距离</summary>
    /// <remark>
    /// bottom 的值可以是像 20 这样的具体像素值，可以是像 '20%' 这样相对于容器高宽的百分比。
    /// 默认自适应。
    /// </remark>
    public Object Bottom { get; set; }

    /// <summary>工具栏组件的宽度</summary>
    /// <remark>默认自适应。</remark>
    public Object Width { get; set; }

    /// <summary>工具栏组件的高度</summary>
    /// <remark>默认自适应。</remark>
    public Object Height { get; set; }

    /// <summary>工具箱的 tooltip 配置，配置项同 tooltip</summary>
    /// <remark>
    /// 默认不显示，可以在需要特殊定制文字样式（尤其是想用自定义 CSS 控制文字样式）的时候开启 tooltip，如下示例：
    /// option = {
    ///     tooltip: {
    ///         show: true // 必须引入 tooltip 组件
    ///     },
    ///     toolbox: {
    ///         show: true,
    ///         showTitle: false, // 隐藏默认文字，否则两者位置会重叠
    ///         feature: {
    ///             saveAsImage: {
    ///                 show: true,
    ///                 title: 'Save As Image'
    ///             },
    ///             dataView: {
    ///                 show: true,
    ///                 title: 'Data View'
    ///             },
    ///         },
    ///         tooltip: { // 和 option.tooltip 的配置项相同
    ///             show: true,
    ///             formatter: function (param) {
    ///                 return '<div>' + param.title + '</div>'; // 自定义的 DOM 结构
    ///             },
    ///             backgroundColor: '#222',
    ///             textStyle: {
    ///                 fontSize: 12,
    ///             },
    ///             extraCssText: 'box-shadow: 0 0 3px rgba(0, 0, 0, 0.3);' // 自定义的 CSS 样式
    ///         }
    ///     },
    ///     ...
    /// }
    /// </remark>
    public Tooltip Tooltip { get; set; }

}
