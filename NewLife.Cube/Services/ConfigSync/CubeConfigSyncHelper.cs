using NewLife.Configuration;
using NewLife.Cube.ViewModels;
using XCode.Configuration;

namespace NewLife.Cube.Services;

/// <summary>集群配置同步帮助类。供配置控制器解析同步作用域、合并视图与跳过字段只读</summary>
public static class CubeConfigSyncHelper
{
    /// <summary>解析当前请求的同步上下文。未启用同步时管理器和作用域均为空</summary>
    /// <param name="context">当前 Http 上下文，可为空（单元测试直接调用控制器时）</param>
    /// <param name="provider">配置提供者，通常传入 Config&lt;T&gt;.Provider</param>
    /// <returns>同步管理器与作用域。非文件配置、未注册管理器或该作用域未启用时返回空</returns>
    public static (ConfigSyncManager? Manager, String? Scope) Resolve(HttpContext? context, IConfigProvider? provider)
    {
        var manager = context?.RequestServices?.GetService<ConfigSyncManager>();
        if (manager == null) return (null, null);

        var scope = GetScope(provider);
        if (scope.IsNullOrEmpty() || !IsEnabled(manager, scope)) return (null, null);

        return (manager, scope);
    }

    /// <summary>判断作用域是否已注册为同步目标</summary>
    /// <param name="manager">同步管理器</param>
    /// <param name="scope">同步作用域</param>
    /// <returns>是否启用同步</returns>
    public static Boolean IsEnabled(ConfigSyncManager? manager, String? scope) =>
        manager != null && !scope.IsNullOrEmpty() && manager.Targets.Any(e => e.Scope.EqualIgnoreCase(scope));

    /// <summary>从配置提供者推导同步作用域。取文件配置文件名去扩展名，如 Config/XCode.config 得到 XCode</summary>
    /// <param name="provider">配置提供者</param>
    /// <returns>作用域；非文件配置或文件名为空时返回 null</returns>
    public static String? GetScope(IConfigProvider? provider)
    {
        if (provider is not FileConfigProvider file || file.FileName.IsNullOrEmpty()) return null;

        var name = Path.GetFileNameWithoutExtension(file.FileName.GetBasePath());
        return name.IsNullOrEmpty() ? null : name;
    }

    /// <summary>判断键名是否在跳过集合中。支持完整键路径（冒号分隔）、叶子名与嵌套路径匹配，忽略大小写</summary>
    /// <param name="skip">跳过键集合</param>
    /// <param name="name">键名或完整键路径</param>
    /// <returns>是否跳过</returns>
    public static Boolean IsSkipped(ICollection<String>? skip, String name)
    {
        if (skip == null || skip.Count == 0 || name.IsNullOrEmpty()) return false;

        // 完整路径只取最后一段的叶子名参与匹配；跳过项为嵌套路径（如 Sub:Timeout）时，整个字段按跳过处理
        var p = name.LastIndexOf(':');
        var leaf = p >= 0 ? name[(p + 1)..] : name;
        var prefix = name + ":";

        return skip.Any(s => !s.IsNullOrEmpty() &&
            (s.EqualIgnoreCase(name) || s.EqualIgnoreCase(leaf) || s.StartsWithIgnoreCase(prefix)));
    }

    /// <summary>把跳过字段标记为只读。跳过字段只在本机生效，不允许在配置页修改</summary>
    /// <param name="fields">字段集合</param>
    /// <param name="skip">跳过键集合</param>
    public static void ApplySkipReadOnly(IList<DataField> fields, ICollection<String>? skip)
    {
        if (fields == null || skip == null || skip.Count == 0) return;

        foreach (var field in fields)
        {
            if (IsSkipped(skip, field.Name)) field.ReadOnly = true;
        }
    }

    /// <summary>把指定作用域的跳过字段标记为只读</summary>
    /// <param name="fields">字段集合</param>
    /// <param name="manager">同步管理器，为空时不做处理</param>
    /// <param name="scope">同步作用域</param>
    public static void ApplySkipReadOnly(IList<DataField> fields, ConfigSyncManager? manager, String? scope)
    {
        if (fields == null || manager == null || scope.IsNullOrEmpty()) return;

        var target = manager.Targets.FirstOrDefault(e => e.Scope.EqualIgnoreCase(scope));
        if (target != null) ApplySkipReadOnly(fields, target.Skip);
    }
}
