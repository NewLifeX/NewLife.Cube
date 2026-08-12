using NewLife.Reflection;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube.AI;

/// <summary>AI 数据助手。负责字段安全过滤和数据收集</summary>
public static class AiDataHelper
{
    /// <summary>文本字段最大长度。超出部分截断，避免大文本撑爆 Prompt</summary>
    public const Int32 MaxTextLength = 500;

    /// <summary>默认敏感字段名模式（忽略大小写）</summary>
    /// <remarks>匹配规则：字段名包含任一模式则视为敏感字段</remarks>
    private static readonly String[] _sensitivePatterns =
    [
        "password", "pwd", "pass",
        "mobile", "phone", "cellphone", "tel",
        "idcard", "idnumber", "identity",
        "email", "mail",
        "token", "accesstoken", "refreshtoken",
        "secret", "appsecret", "apisecret",
        "key", "apikey", "privatekey",
        "salt", "hash", "sign",
        "ip", "ipaddress", "clientip",
        "mac", "macaddress",
        "address", "location", "coordinate",
        "avatar", "photo", "headimg",
        "fingerprint", "deviceid"
    ];

    /// <summary>默认敏感元素类型（忽略大小写）</summary>
    /// <remarks>匹配规则：字段 ItemType 等于任一模式则视为敏感字段，如 [BindColumn(ItemType = "password")]</remarks>
    private static readonly String[] _sensitiveItemTypes =
    [
        "password", "pwd", "pass",
        "secret", "appsecret", "apisecret",
        "token", "accesstoken", "refreshtoken",
        "key", "apikey", "privatekey",
        "sign", "salt", "hash"
    ];

    /// <summary>判断字段名是否安全（仅检查名称黑名单）</summary>
    /// <param name="fieldName">字段名</param>
    /// <returns>true=安全可发送</returns>
    public static Boolean IsSafeFieldName(String fieldName)
    {
        if (fieldName.IsNullOrEmpty()) return false;

        var lower = fieldName.ToLower();
        foreach (var pattern in _sensitivePatterns)
        {
            if (lower.Contains(pattern)) return false;
        }

        return true;
    }

    /// <summary>判断字段是否安全可发送给 AI</summary>
    /// <param name="field">字段元数据</param>
    /// <returns>true=安全可发送</returns>
    public static Boolean IsSafeField(FieldItem field)
    {
        if (field == null) return false;

        var name = field.Name;
        if (name.IsNullOrEmpty()) return false;

        // 检查字段名是否命中敏感模式
        var lower = name.ToLower();
        foreach (var pattern in _sensitivePatterns)
        {
            if (lower.Contains(pattern)) return false;
        }

        // 检查元素类型（ItemType）是否敏感，如 password/secret/token 等
        var itemType = field.Column?.ItemType;
        if (itemType.IsNullOrEmpty()) itemType = field.Field?.ItemType;
        if (!itemType.IsNullOrEmpty())
        {
            foreach (var pattern in _sensitiveItemTypes)
            {
                if (itemType.EqualIgnoreCase(pattern)) return false;
            }
        }

        return true;
    }

    /// <summary>过滤实体字段，仅保留 AI 可用的安全字段（敏感黑名单过滤）</summary>
    /// <param name="allFields">实体所有字段</param>
    /// <param name="entityType">实体类型（预留，保持签名兼容）</param>
    /// <returns>安全字段列表</returns>
    public static IList<FieldItem> FilterSafeFields(FieldItem[] allFields, Type entityType)
    {
        if (allFields == null || allFields.Length == 0) return [];

        // 黑名单过滤：字段名/ItemType 命中敏感模式则排除
        return allFields.Where(IsSafeField).ToList();
    }

    /// <summary>将实体对象转为安全字段的字典（仅包含 AI 可见字段）</summary>
    /// <param name="entity">实体对象</param>
    /// <param name="safeFields">安全字段列表</param>
    /// <returns>字段名→值的字典</returns>
    public static IDictionary<String, Object?> ToSafeDictionary(IEntity entity, IList<FieldItem> safeFields)
    {
        var dic = new Dictionary<String, Object?>();
        foreach (var field in safeFields)
        {
            var value = entity[field.Name];

            // 大文本字段截断，避免撑爆 Prompt
            if (value is String str && str.Length > MaxTextLength)
                value = str[..MaxTextLength] + "...[已截断]";

            dic[field.Name] = value;
        }
        return dic;
    }
}
