using NewLife.Collections;

namespace NewLife.Cube.Charts;

/// <summary>图表网格</summary>
public class ChartGrid : IDictionarySource
{
    #region 属性
    /// <summary>宽度。单位px，负数表示百分比，默认100%</summary>
    public Int32 Width { get; set; } = -100;

    /// <summary>高度。单位px，负数表示百分比，默认300px</summary>
    public Int32 Height { get; set; } = 300;

    /// <summary>左边距。单位px，负数表示百分比，默认3%</summary>
    public Int32 Left { get; set; } = -3;

    /// <summary>右边距。单位px，负数表示百分比，默认3%</summary>
    public Int32 Right { get; set; } = -3;
    #endregion

    #region 方法
    /// <summary>转字典，方便序列化</summary>
    /// <returns></returns>
    public IDictionary<String, Object> ToDictionary()
    {
        var dic = new Dictionary<String, Object>();

        //if (Width != 0) dic[nameof(Width)] = Width < 0 ? $"{-Width}%" : Width;
        //if (Height != 0) dic[nameof(Height)] = Height < 0 ? $"{-Height}%" : Height;
        if (Left != 0) dic[nameof(Left)] = Left < 0 ? $"{-Left}%" : Left;
        if (Right != 0) dic[nameof(Right)] = Right < 0 ? $"{-Right}%" : Right;

        return dic;
    }
    #endregion
}
