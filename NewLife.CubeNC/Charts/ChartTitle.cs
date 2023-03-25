using System.Web.Script.Serialization;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Cube.Charts;

/// <summary>标题，主副标题</summary>
public class ChartTitle : IExtend
{
    #region 属性
    /// <summary>显示</summary>
    public Boolean Show { get; set; }

    /// <summary>一级层叠控制</summary>
    /// <remarks>
    /// 每一个不同的zlevel将产生一个独立的canvas，相同zlevel的组件或图标将在同一个canvas上渲染。
    /// zlevel越高越靠顶层，canvas对象增多会消耗更多的内存和性能，并不建议设置过多的zlevel，大部分情况可以通过二级层叠控制z实现层叠控制。
    /// </remarks>
    public Int32 ZLevel { get; set; }

    /// <summary>二级层叠控制</summary>
    /// <remarks>同一个canvas（相同zlevel）上z越高约靠顶层。</remarks>
    public Int32 Z { get; set; }

    /// <summary>主标题文本，'\n'指定换行</summary>
    public String Text { get; set; }

    /// <summary>主标题文本超链接</summary>
    public String Link { get; set; }

    /// <summary>主标题超链接，'blank' | 'self'</summary>
    public String Target { get; set; }

    /// <summary>副标题文本，'\n'指定换行</summary>
    public String SubText { get; set; }

    /// <summary>副标题文本超链接</summary>
    public String SubLink { get; set; }

    /// <summary>副标题超链接，'blank' | 'self'</summary>
    public String SubTarget { get; set; }

    /// <summary>
    /// 默认值：'left'
    /// 水平安放位置，默认为左侧，可选为：'center' | 'left' | 'right' | {number}（x坐标，单位px）
    /// </summary>
    public String X { get; set; }

    /// <summary>
    /// 默认值：'top'
    /// 垂直安放位置，默认为全图顶端，可选为：'top' | 'bottom' | 'center' | {number}（y坐标，单位px）
    /// </summary>
    public String Y { get; set; }

    /// <summary>
    /// 水平对齐方式，默认根据x设置自动调整，可选为： left' | 'right' | 'center
    /// </summary>
    public String TextAlign { get; set; }

    /// <summary>
    /// 默认值：'rgba(0,0,0,0)'
    /// 标题背景颜色，默认透明
    /// </summary>
    public String BackgroundColor { get; set; }

    /// <summary>
    /// 默认值：'#ccc'
    /// 标题边框颜色
    /// </summary>
    public String BorderColor { get; set; }

    /// <summary>
    /// 标题边框线宽，单位px，默认为0（无边框）
    /// </summary>
    public Int32 BorderWidth { get; set; }

    /// <summary>
    /// 默认值：5
    /// 标题内边距，单位px，默认各方向内边距为5，接受数组分别设定上右下左边距，同css，见下图
    /// </summary>
    public Int32 Padding { get; set; }

    /// <summary>
    /// 默认值：5
    /// 主副标题纵向间隔，单位px，默认为5
    /// </summary>
    public Int32 ItemGap { get; set; }

    /// <summary>
    /// 主标题文本样式
    /// </summary>
    public TextStyle TextStyle { get; set; }

    /// <summary>
    /// 副标题文本样式
    /// </summary>
    public TextStyle SubTextStyle { get; set; }

    /// <summary>
    /// 用于标题定位，数组为横纵相对仪表盘圆心坐标偏移，支持百分比（相对外半径）
    /// </summary>
    public Object OffsetCenter { get; set; }

    /// <summary>扩展字典</summary>
    [ScriptIgnore]
    public IDictionary<String, Object> Items { get; set; } = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>扩展数据</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Object this[String key] { get => Items[key]; set => Items[key] = value; }
    #endregion
}