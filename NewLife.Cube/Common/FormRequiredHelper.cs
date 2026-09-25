using NewLife.Cube.ViewModels;

namespace NewLife.Cube;

/// <summary>表单必填自动推断（OSC-260925）。依据实体 XML 定义文件（Model.xml）生成的类定义元数据，自动推断哪些字段必须由用户手工填写，控制器无需逐字段显式设置 Required。</summary>
/// <remarks>
/// 数据来源（xcode 由 Model.xml 生成到类上的特性 → FieldItem → DataField）：
/// <list type="bullet">
/// <item><c>DataObjectField(primaryKey, identity, nullable, length)</c> → 主键/标识列/可空性</item>
/// <item><c>BindColumn(name, description, rawType, DefaultValue = ...)</c> → 默认值声明</item>
/// <item>属性类型与 setter → 值类型/布尔/枚举与只读</item>
/// <item><c>Map</c> 特性 → 查找展示列（如 ParentID→ParentName），按物理列另行判定</item>
/// </list>
/// 推断规则：主键与标识列、只读、可空列、布尔、声明了默认值的列、审计列（Create*/Update*）、Map 扩展字段、
/// 多租户关闭时的租户字段，以及非字符串的值类型/枚举 → 非必填；仅「非空、无默认值、非审计」的字符串字段 → 必填
/// （名称/编号/标题等业务主字段）。
/// <para>说明：值类型（Int/Boolean/DateTime/枚举）CLR 恒有值，表单空值不提交时后端按 0/默认值落库；
/// 布尔开关 false 是有效值，均不标必填。业务特例可在控制器显式置 Required=true 覆盖（ApplyRequired 保留显式 true）。</para>
/// </remarks>
public static class FormRequiredHelper
{
    #region 规则
    /// <summary>审计字段名。创建/更新的用户、IP、时间由用户/时间/IP 拦截器统一维护</summary>
    static readonly String[] _AuditFields =
    [
        "CreateUser", "CreateUserID", "CreateIP", "CreateTime",
        "UpdateUser", "UpdateUserID", "UpdateIP", "UpdateTime",
    ];

    /// <summary>推断字段是否必须由用户手工填写（表单必填）</summary>
    /// <param name="df">字段</param>
    /// <returns>true 表示必填</returns>
    public static Boolean IsRequired(DataField df)
    {
        if (df == null) return false;

        // 多租户关闭时租户字段非必填（0 表示全局）
        if (df.IsTenantScopeField() && !CubeSetting.Current.EnableTenant) return false;

        // Map 扩展字段（查找展示列）：自身不标必填，物理列作为独立字段另行判定
        if (!df.MapField.IsNullOrEmpty() && !df.MapField.EqualIgnoreCase(df.Name)) return false;

        // 主键/标识列/只读：系统生成或禁止修改
        if (df.PrimaryKey || df.ReadOnly || df.Field?.IsIdentity == true) return false;

        // 可空列（DataObjectField.Nullable=true）
        if (df.Nullable) return false;

        // 非字符串的值类型与布尔/枚举（Int/DateTime/Boolean/…）：CLR 恒有值，空值不提交时后端按 0/默认值落库
        if (df.Type != typeof(String)) return false;

        // 声明了默认值（BindColumn.DefaultValue / 数据库列默认值）：数据库或实体 Valid 自动填充
        if (!GetDefaultValue(df).IsNullOrEmpty()) return false;

        // 审计列：拦截器维护
        if (IsAuditField(df.Name)) return false;

        return true;
    }

    /// <summary>取字段默认值：优先 BindColumn 特性声明，其次数据库列默认值</summary>
    /// <param name="df">字段</param>
    /// <returns>默认值；未声明时 null</returns>
    public static String GetDefaultValue(DataField df) => df?.Field?.Column?.DefaultValue ?? df?.Field?.Field?.DefaultValue;

    /// <summary>是否审计字段（创建/更新用户、IP、时间）</summary>
    /// <param name="name">字段名</param>
    /// <returns>true 表示审计字段</returns>
    public static Boolean IsAuditField(String name)
    {
        if (name.IsNullOrEmpty()) return false;

        foreach (var item in _AuditFields)
        {
            if (name.EqualIgnoreCase(item)) return true;
        }
        return false;
    }
    #endregion
}
