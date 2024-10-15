namespace NewLife.Cube.Charts.Models;

/// <summary>图视图模型</summary>
public class GraphViewModel
{
    /// <summary>标题</summary>
    public String Title { get; set; }

    /// <summary>布局</summary>
    public String Layout { get; set; }

    /// <summary>分类。节点分类</summary>
    public GraphCategory[] Categories { get; set; }

    /// <summary>链接。图形的边，连接线</summary>
    public GraphLink[] Links { get; set; }

    /// <summary>节点。图形的节点</summary>
    public GraphNode[] Nodes { get; set; }
}

/// <summary>图分类</summary>
public class GraphCategory
{
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>符号</summary>
    /// <remarks>
    /// 支持svg图标，例如：image:///icons/http.svg
    /// </remarks>
    public String Symbol { get; set; }
}

/// <summary>图链接</summary>
public class GraphLink
{
    /// <summary>源节点的编号，也可以是名称</summary>
    public String Source { get; set; }

    /// <summary>目标节点的编号，也可以是名称</summary>
    public String Target { get; set; }
}

/// <summary>图节点</summary>
public class GraphNode
{
    /// <summary>编号。唯一确定节点</summary>
    public String Id { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>分类索引。在Categories集合中的索引位置</summary>
    public Int32 Category { get; set; }

    /// <summary>符号大小。例如40</summary>
    public Double SymbolSize { get; set; }

    /// <summary>数值</summary>
    public Double Value { get; set; }
}