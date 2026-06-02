using System.Collections.Concurrent;
using XCode;

namespace NewLife.Cube;

/// <summary>实体页面信息。记录某实体类型对应的管理页面路径和主键名</summary>
public class EntityPageInfo
{
    #region 属性
    /// <summary>页面路径，含 Area 前缀。如 /Admin/User</summary>
    public String Url { get; set; }

    /// <summary>主键参数名。如 ID</summary>
    public String PrimaryKey { get; set; }
    #endregion

    #region 方法
    /// <summary>根据主键值构造完整跳转链接</summary>
    /// <param name="keyValue">主键值</param>
    /// <returns>如 /Admin/User?ID=123</returns>
    public String GetLink(Object keyValue) => $"{Url}?{PrimaryKey}={keyValue}";

    /// <summary>构造含占位符的 URL 模板，供代码生成使用</summary>
    /// <param name="mapField">外键字段名，用于生成 Razor 占位符</param>
    /// <returns>如 /Admin/User?ID={OwnerId}</returns>
    public String GetUrlTemplate(String mapField) => $"{Url}?{PrimaryKey}={{{mapField}}}";
    #endregion
}

/// <summary>实体页面注册表。在魔方启动扫描 Controller 时自动建立实体类型到页面路径的映射</summary>
/// <remarks>
/// 用于解决跨 Area 的外键跳转链接缺少 Area 前缀的问题。
/// 注册点：MenuHelper.ScanController()；消费点：ListField.GetUrl()、ViewHelper.MakeListView()。
/// </remarks>
public static class EntityPageRegistry
{
    private static readonly ConcurrentDictionary<Type, EntityPageInfo> _registry = new();

    /// <summary>注册实体类型到页面的映射。同一实体重复注册时后者覆盖前者</summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="url">页面路径，含 Area 前缀。如 /Admin/User</param>
    /// <param name="primaryKey">主键参数名。如 ID</param>
    public static void Register(Type entityType, String url, String primaryKey)
    {
        if (entityType == null || url.IsNullOrEmpty()) return;

        _registry[entityType] = new EntityPageInfo { Url = url, PrimaryKey = primaryKey ?? "ID" };
    }

    /// <summary>获取实体类型对应的页面信息</summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>页面信息，未注册时返回 null</returns>
    public static EntityPageInfo Get(Type entityType)
    {
        if (entityType == null) return null;
        _registry.TryGetValue(entityType, out var info);
        return info;
    }

    /// <summary>根据实体类型和主键值构造完整跳转链接</summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="keyValue">主键值</param>
    /// <returns>完整链接，未注册时返回 null</returns>
    public static String GetLink(Type entityType, Object keyValue)
    {
        var info = Get(entityType);
        return info?.GetLink(keyValue);
    }

    /// <summary>获取所有已注册的实体页面信息</summary>
    /// <returns>类型到页面信息的只读快照</returns>
    public static IReadOnlyDictionary<Type, EntityPageInfo> GetAll() => _registry;
}
