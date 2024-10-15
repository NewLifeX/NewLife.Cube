namespace NewLife.Cube.Charts.Models;

/// <summary>地图节点</summary>
public class MapNode
{
    /// <summary>节点名称</summary>
    public String Name { get; set; }

    /// <summary>标题。飞线边上显示文字层</summary>
    public String Title { get; set; }

    /// <summary>宽度</summary>
    public Int32 Width { get; set; } = 420;

    /// <summary>高度</summary>
    public Int32 Height { get; set; } = 200;

    /// <summary>内容</summary>
    public String Content { get; set; }

    /// <summary>线宽。飞线默认8</summary>
    public Int32 LineWidth { get; set; }

    /// <summary>节点位置</summary>
    public GeoPoint Point { get; set; }

    /// <summary>设置宽度</summary>
    public void SetWidth(Double value, Double min, Double max)
    {
        if (min == max)
        {
            LineWidth = 8;
        }
        else
        {
            // 避免最小值被压成1，可能它本身数值也不小
            min *= 0.2;

            LineWidth = (Int32)Math.Round(12 * (value - min) / (max - min) + 1);
            if (LineWidth > 24) LineWidth = 24;
        }
    }
}
