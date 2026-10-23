using XCode.Membership;

namespace NewLife.Cube;

/// <summary>工具栏按钮声明特性。标注在控制器公开动作方法上，动作会自动出现在列表页工具栏</summary>
/// <remarks>
/// 只有显式标注的方法才会成为工具栏按钮，避免把控制器里既有的业务动作全部暴露到工具栏。
/// 权限优先取本特性的 <see cref="Permission"/>；未指定时取同一方法上的 <see cref="EntityAuthorizeAttribute"/>，
/// 两者都没有时不做权限限制。
/// 按钮默认提交选中行主键（<c>keys</c>），不需要选中数据时把 <see cref="Include"/> 设为空字符串。
/// </remarks>
/// <example>
/// <code>
/// [ToolbarButton("批量刷新", PermissionFlags.Update, Method = "POST")]
/// public ActionResult Refresh([FromForm] String keys) { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ToolbarButtonAttribute : Attribute
{
    #region 属性
    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>按钮名称。为空时取方法名</summary>
    public String Name { get; set; }

    /// <summary>图标名</summary>
    public String Icon { get; set; }

    /// <summary>按钮风格。primary / danger / warning / success / info</summary>
    public String Type { get; set; }

    /// <summary>按钮类型。默认 request</summary>
    public String Kind { get; set; }

    /// <summary>请求方法。为空时按动作上的 Http* 特性推断，否则 GET</summary>
    public String Method { get; set; }

    /// <summary>跳转地址。Kind=link 时使用</summary>
    public String Url { get; set; }

    /// <summary>提交字段，逗号分隔。默认 keys（选中行主键）；设为空字符串表示不提交任何字段</summary>
    public String Include { get; set; }

    /// <summary>是否携带当前查询参数。仅 Kind=link 有效</summary>
    public Boolean AppendQuery { get; set; }

    /// <summary>链接目标。_blank / _self</summary>
    public String Target { get; set; }

    /// <summary>上传文件类型。Kind=upload 时使用，如 .xlsx,.csv</summary>
    public String Accept { get; set; }

    /// <summary>二次确认文案</summary>
    public String Confirm { get; set; }

    /// <summary>是否要求先选中数据</summary>
    public Boolean RequiresSelection { get; set; }

    /// <summary>前端渲染器名。Kind=custom 时使用</summary>
    public String Renderer { get; set; }

    /// <summary>所需权限。None 表示取动作上的 EntityAuthorize</summary>
    public PermissionFlags Permission { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public ToolbarButtonAttribute() { }

    /// <summary>实例化</summary>
    /// <param name="displayName">显示名</param>
    public ToolbarButtonAttribute(String displayName) => DisplayName = displayName;

    /// <summary>实例化</summary>
    /// <param name="displayName">显示名</param>
    /// <param name="permission">所需权限</param>
    public ToolbarButtonAttribute(String displayName, PermissionFlags permission)
    {
        DisplayName = displayName;
        Permission = permission;
    }
    #endregion
}
