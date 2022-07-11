namespace NewLife.Cube.Charts;

/// <summary>数据缩放</summary>
public class DataZoom
{
    /// <summary>是否显示</summary>
    public Boolean Show { get; set; } = true;

    /// <summary>实时</summary>
    public Boolean RealTime { get; set; } = true;

    /// <summary>开始位置</summary>
    public Int32 Start { get; set; } = 0;

    /// <summary>结束位置</summary>
    public Int32 End { get; set; } = 100;

    /// <summary>要控制的X轴</summary>
    public Int32[] XAxiaIndex { get; set; } = new[] { 0 };

    /// <summary>要控制的Y轴</summary>
    public Int32[] YAxiaIndex { get; set; }
}