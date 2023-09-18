namespace NewLife.Cube.ViewModels;

/// <summary>选择建议选择控件所使用的模型</summary>
public class SuggestModel
{
    /// <summary>控件标识</summary>
    public String Id { get; set; }

    /// <summary>数值。未设置时，根据Id从Request中取</summary>
    public String Value { get; set; }

    /// <summary>显示值</summary>
    public String ShowValue { get; set; }

    /// <summary>接口地址。最后可以是key=，用于接收关键字</summary>
    public String Url { get; set; }

    /// <summary>字段列表。名称与显示名</summary>
    public IDictionary<String, String> Fields { get; set; }

    /// <summary>标识字段。大小写必须跟名称列表一致</summary>
    public String IdField { get; set; } = "id";

    /// <summary>名称字段。大小写必须跟名称列表一致</summary>
    public String NameField { get; set; } = "name";

    /// <summary>占位文本</summary>
    public String PlaceHolder { get; set; } = "搜索";
}