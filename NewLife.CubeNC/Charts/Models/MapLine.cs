namespace NewLife.Cube.Charts.Models;

/// <summary>地图线路</summary>
public class MapLine
{
    /// <summary>线路名称</summary>
    public String Name { get; set; }

    ///// <summary>标题。飞线边上显示文字层</summary>
    //public String Title { get; set; }

    /// <summary>线宽。飞线默认8</summary>
    public Int32 Width { get; set; }

    /// <summary>颜色</summary>
    public String Color { get; set; }

    /// <summary>节点</summary>
    public IList<MapNode> Nodes { get; set; }

    /// <summary>信息窗内容</summary>
    public String Content { get; set; }

    /// <summary>设置宽度</summary>
    /// <param name="value"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    public void SetWidth(Double value, Double min, Double max)
    {
        if (min == max)
        {
            Width = 8;
        }
        else
        {
            // 避免最小值被压成1，可能它本身数值也不小
            min *= 0.2;

            Width = (Int32)Math.Round(12 * (value - min) / (max - min) + 1);
            if (Width > 24) Width = 24;
        }
    }

    ///// <summary>设置宽度</summary>
    ///// <param name="value"></param>
    ///// <param name="min"></param>
    ///// <param name="max"></param>
    //public void SetWidth(Decimal value, Decimal min, Decimal max)
    //{
    //    if (min == max)
    //    {
    //        Width = 8;
    //    }
    //    else
    //    {
    //        // 避免最小值被压成1，可能它本身数值也不小
    //        min *= 0.2m;

    //        Width = (Int32)Math.Round(12 * (value - min) / (max - min) + 1);
    //        if (Width > 24) Width = 24;
    //    }
    //}
}
