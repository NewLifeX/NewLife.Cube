using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>工具栏按钮构建器。把静态集合、动作特性与区域钩子合并后按权限过滤</summary>
internal static class ToolbarBuilder
{
    #region 构建
    /// <summary>构建当前页面的工具栏按钮</summary>
    /// <param name="source">静态按钮集合</param>
    /// <param name="controllerType">控制器类型，用于扫描动作特性</param>
    /// <param name="build">区域/项目级追加钩子，在权限过滤前执行</param>
    /// <param name="user">当前用户</param>
    /// <param name="menu">当前菜单</param>
    /// <returns>已按权限过滤的按钮集合</returns>
    public static IList<ToolbarItem> Build(
        ToolbarCollection source,
        Type controllerType,
        Action<ToolbarCollection> build,
        IUser user,
        IMenu menu)
    {
        var list = (source ?? new ToolbarCollection()).Clone();
        Collect(controllerType, list);
        build?.Invoke(list);

        return Filter(list, user, menu);
    }
    #endregion

    #region 收集与过滤
    /// <summary>收集标注了 <see cref="ToolbarButtonAttribute"/> 的动作方法</summary>
    /// <param name="controllerType">控制器类型</param>
    /// <param name="list">按钮集合</param>
    public static void Collect(Type controllerType, ToolbarCollection list)
    {
        if (controllerType == null || list == null) return;

        foreach (var method in controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            var att = method.GetCustomAttribute<ToolbarButtonAttribute>();
            if (att == null) continue;

            var name = att.Name.IsNullOrEmpty() ? method.Name : att.Name;
            if (list.GetItem(name) != null) continue;

            var kind = att.Kind.IsNullOrEmpty() ? "request" : att.Kind;
            var include = att.Include;
            String[] fields = null;
            if (kind.EqualIgnoreCase("request") || kind.EqualIgnoreCase("upload"))
            {
                if (include == null)
                    fields = kind.EqualIgnoreCase("request") ? ["keys"] : null;
                else if (!include.IsNullOrWhiteSpace())
                    fields = include.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).Where(e => e.Length > 0).ToArray();
            }

            var item = new ToolbarItem
            {
                Name = name,
                DisplayName = att.DisplayName.IsNullOrEmpty() ? name : att.DisplayName,
                Icon = att.Icon,
                Type = att.Type,
                Kind = kind,
                Url = att.Url,
                Action = kind.EqualIgnoreCase("request") || kind.EqualIgnoreCase("upload") ? method.Name : null,
                Method = kind.EqualIgnoreCase("request") || kind.EqualIgnoreCase("upload") ? att.Method ?? GetHttpMethod(method) : null,
                Include = fields,
                AppendQuery = att.AppendQuery,
                Target = att.Target,
                Accept = att.Accept,
                Confirm = att.Confirm,
                RequiresSelection = att.RequiresSelection,
                Renderer = att.Renderer,
            };

            // 权限：特性显式指定优先，其次动作上的授权特性，最后控制器级授权
            if (att.Permission != PermissionFlags.None)
                item.Permissions = [att.Permission];
            else if (method.GetCustomAttribute<EntityAuthorizeAttribute>() is { } au && au.Permissions.Length > 0 && au.Permissions.Any(e => e != PermissionFlags.None))
                item.Permissions = au.Permissions;
            else if (controllerType.GetCustomAttribute<EntityAuthorizeAttribute>() is { } ca && ca.Permissions.Length > 0 && ca.Permissions.Any(e => e != PermissionFlags.None))
                item.Permissions = ca.Permissions;

            list.AddItem(item);
        }
    }

    /// <summary>按当前用户权限过滤按钮。没有菜单时不做权限控制，与 MVC 视图的 Has 一致</summary>
    /// <param name="list">按钮集合</param>
    /// <param name="user">当前用户</param>
    /// <param name="menu">当前菜单</param>
    /// <returns></returns>
    public static IList<ToolbarItem> Filter(ToolbarCollection list, IUser user, IMenu menu)
    {
        var result = new List<ToolbarItem>(list?.Count ?? 0);
        if (list == null) return result;

        foreach (var item in list)
        {
            if (!item.NeedsPermission() || menu == null)
            {
                result.Add(item);
                continue;
            }

            if (user != null && user.Has(menu, item.Permissions)) result.Add(item);
        }

        return result;
    }
    #endregion

    #region 辅助
    private static String GetHttpMethod(MethodInfo method)
    {
        if (method.GetCustomAttribute<HttpPostAttribute>() != null) return "POST";
        if (method.GetCustomAttribute<HttpPutAttribute>() != null) return "PUT";
        if (method.GetCustomAttribute<HttpDeleteAttribute>() != null) return "DELETE";
        if (method.GetCustomAttribute<HttpPatchAttribute>() != null) return "PATCH";
        return null;
    }
    #endregion
}
