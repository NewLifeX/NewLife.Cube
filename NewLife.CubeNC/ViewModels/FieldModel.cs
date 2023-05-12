using NewLife.Cube.Common;

namespace NewLife.Cube.ViewModels;

/// <summary>
/// 字段模型
/// </summary>
public class FieldModel
{
    private String _name;
    private String _columnName;

    /// <summary>
    /// 默认CamelCase小驼峰
    /// </summary>
    readonly FormatType _FormatType = FormatType.CamelCase;

    /// <summary>
    /// 默认CamelCase小驼峰
    /// </summary>
    public FieldModel() { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="formatType">0-小驼峰，1-小写，2-保持默认</param>
    public FieldModel(FormatType formatType) => _FormatType = formatType;

    /// <summary>备注</summary>
    public String Description { get; set; }

    /// <summary>说明</summary>
    public String DisplayName { get; set; }

    /// <summary>属性名</summary>
    public String Name
    {
        get => _name.FormatName(_FormatType);
        internal set => _name = value;
    }

    /// <summary>是否允许空</summary>
    public Boolean IsNullable { get; internal set; }

    /// <summary>长度</summary>
    public Int32 Length { get; internal set; }

    /// <summary>是否数据绑定列</summary>
    public Boolean IsDataObjectField { get; set; }

    /// <summary>是否动态字段</summary>
    public Boolean IsDynamic { get; set; }

    /// <summary>用于数据绑定的字段名</summary>
    /// <remarks>
    /// 默认使用BindColumn特性中指定的字段名，如果没有指定，则使用属性名。
    /// 字段名可能两边带有方括号等标识符
    /// </remarks>
    public String ColumnName
    {
        get => _columnName.FormatName(_FormatType);
        set => _columnName = value;
    }

    /// <summary>是否只读</summary>
    /// <remarks>set { _ReadOnly = value; } 放出只读属性的设置，比如在编辑页面的时候，有的字段不能修改 如修改用户时  不能修改用户名</remarks>
    public Boolean ReadOnly { get; set; }

    /// <summary>
    /// 字段类型
    /// </summary>
    public String TypeStr { get; set; } = nameof(String);

    #region 用于定制字段

    /// <summary>
    /// 是否定制字段
    /// </summary>
    public Boolean IsCustom { get; set; }

    /// <summary>前缀名称。放在某字段之前</summary>
    public String BeforeName { get; set; }

    /// <summary>后缀名称。放在某字段之后</summary>
    public String AfterName { get; set; }

    /// <summary>链接</summary>
    public String Url { get; set; }

    /// <summary>标题。数据单元格上的提示文字</summary>
    public String Title { get; set; }

    /// <summary>头部文字</summary>
    public String Header { get; set; }

    /// <summary>头部链接。一般是排序</summary>
    public String HeaderUrl { get; set; }

    /// <summary>头部标题。数据移上去后显示的文字</summary>
    public String HeaderTitle { get; set; }

    /// <summary>数据动作。设为action时走ajax请求</summary>
    public String DataAction { get; set; }
    #endregion       
}