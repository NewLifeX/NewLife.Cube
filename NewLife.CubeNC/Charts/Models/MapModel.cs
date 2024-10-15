namespace NewLife.Cube.Charts.Models;

/// <summary>地图模型</summary>
public class MapModel
{
    /// <summary>中心点。未指定时，以西安为中心</summary>
    public GeoPoint Center { get; set; }

    /// <summary>级别。默认6级，看全国</summary>
    public Int32 Level { get; set; } = 6;

    /// <summary>颜色</summary>
    public String[] Colors { get; set; } = [];

    /// <summary>线路</summary>
    public IList<MapLine> Lines { get; set; } = [];

    /// <summary>节点</summary>
    public IList<MapNode> Nodes { get; set; } = [];
}
