using XCode.Membership;

namespace NewLife.Cube.ViewModels;

/// <summary>工具栏按钮集合。与 <see cref="FieldCollection"/> 同层，按实体类型静态声明</summary>
/// <remarks>
/// 常规写在控制器静态构造函数里；区域/项目级统一追加建议重写 <c>OnGetToolbar</c>，
/// 避免依赖静态构造顺序（派生控制器没有静态构造时，基类静态构造不会触发）。
/// </remarks>
public class ToolbarCollection : List<ToolbarItem>
{
    #region 添加
    /// <summary>添加按钮。同名覆盖，名称为空时自动生成</summary>
    /// <param name="item">按钮</param>
    /// <returns></returns>
    public ToolbarItem AddItem(ToolbarItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        if (item.Name.IsNullOrEmpty()) item.Name = $"btn{Count + 1}";
        if (item.DisplayName.IsNullOrEmpty()) item.DisplayName = item.Name;
        item.RequiresSelection = item.RequiresSelection || item.HasKeys();

        var idx = FindIndex(e => e.Name.EqualIgnoreCase(item.Name));
        if (idx >= 0)
            this[idx] = item;
        else
            Add(item);

        return item;
    }

    /// <summary>添加跳转按钮</summary>
    /// <param name="displayName">显示名</param>
    /// <param name="url">跳转地址</param>
    /// <param name="icon">图标</param>
    /// <param name="target">目标窗口</param>
    /// <param name="appendQuery">是否携带当前查询参数</param>
    /// <param name="permission">所需权限</param>
    /// <returns></returns>
    public ToolbarItem AddLink(String displayName, String url, String icon = null, String target = null, Boolean appendQuery = false, PermissionFlags permission = PermissionFlags.None)
        => AddItem(new ToolbarItem
        {
            Name = displayName,
            DisplayName = displayName,
            Kind = "link",
            Url = url,
            Icon = icon,
            Target = target,
            AppendQuery = appendQuery,
            Permissions = permission == PermissionFlags.None ? null : [permission],
        });

    /// <summary>添加请求按钮。默认提交选中行主键</summary>
    /// <param name="displayName">显示名</param>
    /// <param name="action">控制器动作名</param>
    /// <param name="method">请求方法，默认 GET</param>
    /// <param name="include">提交字段，默认 keys（选中行主键）</param>
    /// <param name="confirm">二次确认文案</param>
    /// <param name="permission">所需权限</param>
    /// <param name="icon">图标</param>
    /// <returns></returns>
    public ToolbarItem AddAction(String displayName, String action, String method = null, String[] include = null, String confirm = null, PermissionFlags permission = PermissionFlags.None, String icon = null)
        => AddItem(new ToolbarItem
        {
            Name = displayName,
            DisplayName = displayName,
            Kind = "request",
            Action = action,
            Method = method,
            Include = include ?? ["keys"],
            Confirm = confirm,
            Icon = icon,
            Permissions = permission == PermissionFlags.None ? null : [permission],
        });

    /// <summary>添加上传按钮</summary>
    /// <param name="displayName">显示名</param>
    /// <param name="action">控制器动作名</param>
    /// <param name="accept">文件类型，如 .xlsx</param>
    /// <param name="include">随文件一起提交的字段</param>
    /// <param name="permission">所需权限</param>
    /// <returns></returns>
    public ToolbarItem AddUpload(String displayName, String action, String accept = null, String[] include = null, PermissionFlags permission = PermissionFlags.None)
        => AddItem(new ToolbarItem
        {
            Name = displayName,
            DisplayName = displayName,
            Kind = "upload",
            Action = action,
            Accept = accept,
            Include = include,
            Permissions = permission == PermissionFlags.None ? null : [permission],
        });

    /// <summary>添加自定义渲染按钮</summary>
    /// <param name="displayName">显示名</param>
    /// <param name="renderer">前端渲染器名</param>
    /// <param name="permission">所需权限</param>
    /// <returns></returns>
    public ToolbarItem AddCustom(String displayName, String renderer, PermissionFlags permission = PermissionFlags.None)
        => AddItem(new ToolbarItem
        {
            Name = displayName,
            DisplayName = displayName,
            Kind = "custom",
            Renderer = renderer,
            Permissions = permission == PermissionFlags.None ? null : [permission],
        });
    #endregion

    #region 查找删除
    /// <summary>查找指定名称的按钮</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public ToolbarItem GetItem(String name) => this.FirstOrDefault(e => name.EqualIgnoreCase(e.Name));

    /// <summary>移除指定名称的按钮</summary>
    /// <param name="names"></param>
    /// <returns></returns>
    public ToolbarCollection RemoveByName(params String[] names)
    {
        if (names != null)
        {
            foreach (var name in names)
            {
                RemoveAll(e => name.EqualIgnoreCase(e.Name));
            }
        }
        return this;
    }
    #endregion

    #region 克隆
    /// <summary>克隆。按钮深拷贝，供请求期按权限过滤</summary>
    /// <returns></returns>
    public ToolbarCollection Clone()
    {
        var rs = new ToolbarCollection();
        foreach (var item in this)
        {
            rs.AddItem(item.Clone());
        }
        return rs;
    }
    #endregion
}
