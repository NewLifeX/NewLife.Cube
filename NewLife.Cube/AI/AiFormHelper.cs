using NewLife.Cube.ViewModels;

namespace NewLife.Cube.AI;

/// <summary>AI 表单字段元数据</summary>
public class AiFormField
{
    /// <summary>字段名</summary>
    public String Name { get; set; } = null!;

    /// <summary>显示名</summary>
    public String DisplayName { get; set; } = null!;

    /// <summary>类型名</summary>
    public String Type { get; set; } = null!;

    /// <summary>字段说明</summary>
    public String? Description { get; set; }

    /// <summary>是否必填</summary>
    public Boolean Required { get; set; }

    /// <summary>最大长度</summary>
    public Int32 Length { get; set; }

    /// <summary>枚举允许值</summary>
    public IList<String>? EnumValues { get; set; }

    /// <summary>是否可填。false 表示敏感或自动维护字段，AI 不应填写</summary>
    public Boolean Fillable { get; set; }

    /// <summary>当前值（编辑模式）。仅 edit 模式且存在当前记录时填充，供 AI 基于现状补全/修正</summary>
    public Object? Value { get; set; }
}

/// <summary>AI 表单助手。收集表单字段元数据与类型转换，供 AI 工具 get_form_schema / fill_form 使用</summary>
public static class AiFormHelper
{
    /// <summary>自动维护字段。由框架自动填充，不允许 AI 填写</summary>
    private static readonly String[] _autoFields = ["CreateTime", "CreateUser", "CreateIP", "UpdateTime", "UpdateUser", "UpdateIP"];

    /// <summary>判断是否为自动维护字段</summary>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public static Boolean IsAutoField(String name) => _autoFields.Any(e => name.EqualIgnoreCase(e));

    /// <summary>构建表单字段 Schema，供 AI 识别字段结构与约束</summary>
    /// <param name="fields">表单字段集合</param>
    /// <param name="values">当前记录已有值（编辑模式），按字段名匹配填充到 <see cref="AiFormField.Value"/></param>
    /// <returns></returns>
    public static IList<AiFormField> BuildSchema(FieldCollection fields, IDictionary<String, Object?>? values = null)
    {
        var list = new List<AiFormField>();
        foreach (var item in fields)
        {
            // 排除自增主键
            if (item.PrimaryKey && item.Field?.IsIdentity == true) continue;

            var fi = new AiFormField
            {
                Name = item.Name,
                DisplayName = item.DisplayName,
                Type = item.Type?.Name ?? "unknown",
                Description = item.Description,
                Required = item.Required || !item.Nullable,
                Length = item.Length,
            };
            if (item.Type != null && item.Type.IsEnum)
                fi.EnumValues = Enum.GetNames(item.Type).ToList();

            // 可填：非敏感 + 非自动维护 + 非只读
            fi.Fillable = !IsAutoField(item.Name) && AiDataHelper.IsSafeFieldName(item.Name) && !item.ReadOnly;

            // 编辑模式已有值并入（仅安全字段才有值）
            if (values != null && values.TryGetValue(item.Name, out var v))
                fi.Value = v;

            list.Add(fi);
        }
        return list;
    }

    /// <summary>按字段类型转换/校验 AI 返回的值</summary>
    /// <param name="value">AI 返回的原始值</param>
    /// <param name="field">目标字段</param>
    /// <returns>转换后的值；无法转换时返回 null</returns>
    public static Object? CoerceValue(Object? value, DataField field)
    {
        if (value == null) return null;
        var type = field.Type;
        if (type == null) return value + "";

        if (type == typeof(String)) return value + "";
        if (type == typeof(Boolean)) return value.ToBoolean();
        if (type == typeof(Int32)) return value.ToInt();
        if (type == typeof(Int64)) return value.ToLong();
        if (type == typeof(Int16)) return (Int16)value.ToInt();
        if (type == typeof(Byte)) return (Byte)value.ToInt();
        if (type == typeof(Double)) return value.ToDouble();
        if (type == typeof(Single)) return (Single)value.ToDouble();
        if (type == typeof(Decimal)) return value.ToDecimal();
        if (type == typeof(DateTime))
        {
            var str = value + "";
            if (str.IsNullOrWhiteSpace()) return null;
            var dt = value.ToDateTime();
            return dt.Year <= 1900 ? null : dt;
        }
        if (type.IsEnum)
        {
            try { return Enum.Parse(type, value + "", true); }
            catch { return null; }
        }
        return value;
    }
}
