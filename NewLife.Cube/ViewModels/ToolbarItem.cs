using XCode.Membership;

namespace NewLife.Cube.ViewModels;

/// <summary>工具栏按钮。列表页工具栏上的自定义按钮，由后端声明、前端按配置渲染</summary>
/// <remarks>
/// 内置按钮（添加/导入/导出/批量删除/刷新）仍由 <see cref="PageSetting"/> 控制，本类型只描述自定义按钮。
/// Kind 取值：
/// <list type="bullet">
/// <item>link：跳转地址，可携带当前查询参数</item>
/// <item>request：请求控制器动作，可提交选中行主键与当前查询参数</item>
/// <item>upload：上传文件到控制器动作</item>
/// <item>custom：交由前端已注册的渲染器处理</item>
/// </list>
/// 对照 MVC 的 <c>_List_Toolbar_Custom.cshtml</c> 与 <c>_List_Toolbar_Batch.cshtml</c>：
/// <c>data-url</c> 对应 <see cref="Url"/> / <see cref="Action"/>，<c>data-fields</c> 对应 <see cref="Include"/>，
/// <c>data-method</c> 对应 <see cref="Method"/>。
/// </remarks>
public class ToolbarItem
{
    #region 属性
    /// <summary>唯一名称。同名按钮以后者覆盖前者</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>图标名</summary>
    public String Icon { get; set; }

    /// <summary>按钮风格。primary / danger / warning / success / info，空表示默认</summary>
    public String Type { get; set; }

    /// <summary>按钮类型。link / request / upload / custom</summary>
    public String Kind { get; set; } = "link";

    /// <summary>跳转地址。link 使用完整或相对地址</summary>
    public String Url { get; set; }

    /// <summary>控制器动作名。request / upload 使用，前端拼接当前控制器地址</summary>
    public String Action { get; set; }

    /// <summary>请求方法。GET / POST，空表示 GET</summary>
    public String Method { get; set; }

    /// <summary>提交字段。keys 表示选中行主键，其它名字取当前查询参数</summary>
    public String[] Include { get; set; }

    /// <summary>是否携带当前查询参数。仅 link 有效</summary>
    public Boolean AppendQuery { get; set; }

    /// <summary>链接目标。_blank / _self</summary>
    public String Target { get; set; }

    /// <summary>上传文件类型。如 .xlsx,.csv</summary>
    public String Accept { get; set; }

    /// <summary>二次确认文案。为空则不确认</summary>
    public String Confirm { get; set; }

    /// <summary>是否要求先选中数据</summary>
    public Boolean RequiresSelection { get; set; }

    /// <summary>所需权限。空表示不限制，多个之间是“或”的关系</summary>
    public PermissionFlags[] Permissions { get; set; }

    /// <summary>前端渲染器名。custom 使用，前端未注册时忽略该按钮</summary>
    public String Renderer { get; set; }
    #endregion

    #region 方法
    /// <summary>克隆。字段数组一并复制，避免共享引用</summary>
    /// <returns></returns>
    public virtual ToolbarItem Clone()
    {
        var item = (ToolbarItem)MemberwiseClone();
        if (Include != null) item.Include = [.. Include];
        if (Permissions != null) item.Permissions = [.. Permissions];
        return item;
    }

    /// <summary>是否具有提交选中行主键的语义</summary>
    /// <returns></returns>
    public Boolean HasKeys() => Include != null && Include.Any(e => e.EqualIgnoreCase("keys"));

    /// <summary>是否需要权限校验</summary>
    /// <returns></returns>
    public Boolean NeedsPermission() => Permissions != null && Permissions.Length > 0 && Permissions.Any(e => e != PermissionFlags.None);

    /// <summary>转字典。只输出有意义的字段，减小元数据体积</summary>
    /// <returns></returns>
    public virtual IDictionary<String, Object> ToDictionary()
    {
        var dic = new Dictionary<String, Object>
        {
            ["name"] = Name,
            ["kind"] = Kind.IsNullOrEmpty() ? "link" : Kind,
        };

        if (!DisplayName.IsNullOrEmpty()) dic["displayName"] = DisplayName;
        if (!Icon.IsNullOrEmpty()) dic["icon"] = Icon;
        if (!Type.IsNullOrEmpty()) dic["type"] = Type;
        if (!Url.IsNullOrEmpty()) dic["url"] = Url;
        if (!Action.IsNullOrEmpty()) dic["action"] = Action;
        if (!Method.IsNullOrEmpty()) dic["method"] = Method;
        if (Include != null && Include.Length > 0) dic["include"] = Include;
        if (AppendQuery) dic["appendQuery"] = true;
        if (!Target.IsNullOrEmpty()) dic["target"] = Target;
        if (!Accept.IsNullOrEmpty()) dic["accept"] = Accept;
        if (!Confirm.IsNullOrEmpty()) dic["confirm"] = Confirm;
        if (RequiresSelection) dic["requiresSelection"] = true;
        if (!Renderer.IsNullOrEmpty()) dic["renderer"] = Renderer;

        return dic;
    }
    #endregion
}
