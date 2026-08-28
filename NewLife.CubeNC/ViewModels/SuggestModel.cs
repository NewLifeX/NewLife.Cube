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

    /// <summary>条件字段。调用后端接口时，从表单读取指定字段的值附加到 URL 查询参数，实现级联过滤。Key 为表单字段 ID，Value 为 URL 参数名</summary>
    public IDictionary<String, String> ConditionFields { get; set; }

    /// <summary>填充字段。选中一条数据后，将后端返回数据中的字段值填充到表单指定字段。Key 为后端返回数据的字段名，Value 为目标表单字段 ID</summary>
    public IDictionary<String, String> FillFields { get; set; }
}