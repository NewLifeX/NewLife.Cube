using System.Globalization;
using System.Reflection;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Membership;

/// <summary>菜单命名契约：落库开发者语言快照，渲染期按语言决定用快照还是词表翻译</summary>
/// <remarks>
/// 扫描只在显示名为空时写入开发者语言（控制器 <c>[DisplayName]</c> → 实体表描述 → 类名短名），已有值不覆盖。
/// 渲染时：当前语言与源文语言一致则直接显示库值（管理员改过的名字在这一种语言下生效），
/// 否则按 <c>menu:&lt;FullName&gt;</c> → <c>entity:&lt;实体全名&gt;</c> → 普通词条 → 短名 逐层翻译。
/// 代码改名后库值不会自动跟进，由菜单管理页的「刷新菜单」动作按需重置。
/// </remarks>
public static partial class MenuHelper
{
    /// <summary>菜单词表键前缀。键 = 前缀 + 控制器/区域/命名空间全名</summary>
    public const String MenuKeyPrefix = "menu:";

    /// <summary>实体词表键前缀。键 = 前缀 + 实体类型全名</summary>
    public const String EntityKeyPrefix = "entity:";

    #region 源文与快照
    /// <summary>填充菜单显示名。仅在显示名为空时写入，已有值不覆盖</summary>
    /// <param name="menu">菜单</param>
    /// <param name="sourceName">开发者语言源文</param>
    /// <returns>是否写入，调用方据此决定是否保存</returns>
    public static Boolean FillDisplayName(IMenu menu, String? sourceName)
    {
        if (menu == null || sourceName.IsNullOrEmpty()) return false;
        if (!menu.DisplayName.IsNullOrEmpty()) return false;

        menu.DisplayName = sourceName;

        return true;
    }

    /// <summary>刷新单个菜单的显示名。按 FullName 重新解析代码源文并覆盖库值</summary>
    /// <param name="menu">菜单</param>
    /// <returns>是否改动，调用方据此决定是否保存</returns>
    /// <remarks>FullName 为空时按菜单名在当前已加载程序集里猜控制器，补全后再刷新。</remarks>
    public static Boolean RefreshDisplayName(IMenu menu)
    {
        if (menu == null) return false;

        if (menu.FullName.IsNullOrEmpty()) menu.FullName = FindControllerFullName(menu.Name);
        if (menu.FullName.IsNullOrEmpty()) return false;

        var source = GetSourceName(menu);
        if (source.IsNullOrEmpty()) return false;
        if (menu.DisplayName == source) return false;

        menu.DisplayName = source;

        return true;
    }

    /// <summary>按菜单名猜测控制器类型全名</summary>
    /// <param name="name">菜单名称，通常是去掉 Controller 后缀的类名</param>
    /// <returns>控制器类型全名，找不到返回 null</returns>
    /// <remarks>只用于历史遗留菜单补全 FullName，匹配到多个时取第一个非抽象类型。</remarks>
    public static String? FindControllerFullName(String name)
    {
        if (name.IsNullOrEmpty()) return null;
        if (_controllerNames.TryGetValue(name, out var fullName)) return fullName;

        var typeName = name + "Controller";
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic) continue;

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || !type.Name.EqualIgnoreCase(typeName)) continue;

                fullName = type.FullName;
                break;
            }
            if (!fullName.IsNullOrEmpty()) break;
        }

        _controllerNames[name] = fullName;

        return fullName;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<String, String?> _controllerNames = [];

    /// <summary>解析菜单的开发者语言源文</summary>
    /// <param name="menu">菜单</param>
    /// <returns>控制器/区域/方法的显示名，或实体表描述；无法解析时返回 null</returns>
    /// <remarks>依次取控制器 <c>[DisplayName]</c>、实体表描述、类型短名；动作菜单取方法显示名。</remarks>
    public static String? GetSourceName(IMenu menu)
    {
        if (menu == null) return null;
        if (menu.FullName.IsNullOrEmpty()) return null;

        var fullName = menu.FullName;
        var type = ResolveMenuType(fullName);
        if (type == null) return null;

        // 动作菜单：控制器全名.方法名
        var typeName = type.FullName;
        if (!typeName.IsNullOrEmpty() && fullName.Length > typeName.Length + 1 && fullName.StartsWith(typeName + "."))
        {
            var methodName = fullName[(typeName.Length + 1)..];
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
            {
                var name = method.GetDisplayName();

                return name.IsNullOrEmpty() ? methodName : name;
            }
        }

        // 实体类型本身：取表描述
        if (typeof(IEntity).IsAssignableFrom(type)) return GetEntityDisplayName(type) ?? type.Name;

        // 控制器：显式特性优先，其次实体表描述，最后类名短名
        if (type.Name.EndsWith("Controller"))
        {
            var name = type.GetDisplayName();
            if (!name.IsNullOrEmpty()) return name;

            name = GetEntityDisplayName(GetEntityType(type));
            if (!name.IsNullOrEmpty()) return name;

            return type.Name.TrimSuffix("Controller");
        }

        // 区域等其它类型
        return type.GetDisplayName() ?? type.Name;
    }

    /// <summary>判断当前文化是否与源文语言一致</summary>
    /// <param name="culture">当前文化，空时取本地化文化</param>
    /// <param name="source">开发者语言源文</param>
    /// <returns>一致时返回 true</returns>
    /// <remarks>
    /// 按源文的书写文字判断：含汉字视为中文源，只有简体中文（zh/zh-CN/zh-SG/zh-Hans）算一致，繁體走词表；
    /// 其余视为英文源，英文与不变文化算一致。
    /// </remarks>
    public static Boolean IsSourceLanguage(CultureInfo? culture, String? source)
    {
        if (source.IsNullOrEmpty()) return false;

        culture ??= Localization.Culture ?? CultureInfo.CurrentUICulture;
        var name = culture?.Name ?? "";

        if (source.Any(e => e >= 0x4E00 && e <= 0x9FFF))
        {
            if (name.IsNullOrEmpty()) return false;

            return name.EqualIgnoreCase("zh")
                || name.StartsWith("zh-Hans", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("zh-CHS", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("zh-CN", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("zh-SG", StringComparison.OrdinalIgnoreCase);
        }

        return name.IsNullOrEmpty() || name.StartsWith("en", StringComparison.OrdinalIgnoreCase);
    }
    #endregion

    #region 渲染
    /// <summary>获取菜单在请求语言下的显示名</summary>
    /// <param name="menu">菜单</param>
    /// <returns>本地化显示名</returns>
    public static String? GetDisplayName(IMenu menu)
    {
        if (menu == null) return null;

        var stored = menu.DisplayName;
        var source = GetSourceName(menu);

        // 源文语言界面：直接显示库值，管理员改过的名字在这一种语言下生效
        if (!stored.IsNullOrEmpty() && !source.IsNullOrEmpty() && IsSourceLanguage(Localization.Culture, source)) return stored;

        // 手工菜单（无 FullName）或类型已不存在：库值命中词条则翻译，否则原样显示
        if (source.IsNullOrEmpty())
        {
            if (stored.IsNullOrEmpty()) return menu.Name;

            return Translate(stored) ?? stored;
        }

        // 其它语言：按规范键与词表即时翻译，词条全缺时回退源文，绝不把类型全名铺到界面
        var text = Translate(MenuKeyPrefix + menu.FullName);
        if (text != null) return text;

        var type = ResolveMenuType(menu.FullName);
        var entityType = GetEntityType(type) ?? (type != null && typeof(IEntity).IsAssignableFrom(type) ? type : null);
        text = Translate(EntityKeyPrefix + entityType?.FullName);
        if (text != null) return text;

        return Translate(source) ?? source;
    }

    /// <summary>按词表键翻译，未命中返回 null</summary>
    /// <param name="key">词表键</param>
    /// <returns>译文，未命中返回 null</returns>
    private static String? Translate(String? key)
    {
        if (key.IsNullOrEmpty()) return null;

        var text = L.T(key);

        return text != key ? text : null;
    }
    #endregion

    #region 类型解析
    /// <summary>获取实体表显示名</summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>显示名，非实体或无法解析时返回 null</returns>
    public static String? GetEntityDisplayName(Type? entityType)
    {
        if (entityType == null) return null;

        var factory = EntityFactory.CreateFactory(entityType);

        return factory?.Table?.DataTable?.DisplayName;
    }

    /// <summary>从控制器类型的继承链中提取 ReadOnlyEntityController&lt;TEntity&gt; 的泛型实参</summary>
    /// <param name="controllerType">控制器类型</param>
    /// <returns>实体类型，非实体控制器返回 null</returns>
    internal static Type? GetEntityType(Type? controllerType)
    {
        var t = controllerType;
        while (t != null && t != typeof(Object))
        {
            if (t.IsGenericType)
            {
                var name = t.GetGenericTypeDefinition().Name;
                if (name.StartsWith("ReadOnlyEntityController`") || name.StartsWith("EntityController`")) return t.GetGenericArguments()[0];
            }
            t = t.BaseType;
        }

        return null;
    }
    #endregion
}
