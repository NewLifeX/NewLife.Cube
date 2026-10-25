using System.Reflection;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Membership;

/// <summary>菜单命名契约与规范键解析。自动菜单落库类型全名，渲染期按词表翻译</summary>
/// <remarks>
/// 自动生成菜单的规范名是类型全名：实体菜单用实体类 FullName，其余用控制器/区域 FullName。
/// 词表键为 <c>menu:&lt;FullName&gt;</c>，前缀避免与消息键冲突（如消息键 Log 与实体「审计日志」不同义）。
/// <c>Menu.Ex6</c> 记录自动项规范键或 <see cref="CustomNameMarker"/>，据此区分自动项与用户自定义项。
/// 渲染时分层回退：menu 键 → 旧值/自定义名 → 按类型解析出人类可读名 → 短名，绝不把 FullName 直接展示到界面。
/// </remarks>
public static partial class MenuHelper
{
    /// <summary>用户自定义名称标记。写入菜单扩展字段，此后的扫描不再覆盖该菜单显示名</summary>
    public const String CustomNameMarker = "#custom";

    /// <summary>菜单规范键前缀。词表键 = 前缀 + 类型全名</summary>
    public const String MenuKeyPrefix = "menu:";

    /// <summary>计算菜单规范键</summary>
    /// <param name="controllerType">控制器类型</param>
    /// <param name="entityType">实体类型，非实体控制器为 null</param>
    /// <returns>实体类全名，或控制器全名</returns>
    public static String BuildMenuKey(Type controllerType, Type? entityType = null) => entityType?.FullName ?? controllerType?.FullName ?? "";

    /// <summary>按规范键同步菜单显示名</summary>
    /// <param name="menu">菜单</param>
    /// <param name="key">规范键，通常是类型全名</param>
    /// <param name="friendly">人类可读的源文（控制器/实体/区域显示名），用于存量迁移判定</param>
    /// <param name="fallbackNames">旧版本可能落库的名称，如菜单名、类型名</param>
    /// <returns>是否修改了菜单，调用方据此决定是否保存</returns>
    /// <remarks>自动项跟随规范键刷新，用户自定义名称保持不动；未标记的存量行按旧值形态识别一次。</remarks>
    public static Boolean SyncDisplayName(IMenu menu, String? key, String? friendly, params String?[] fallbackNames)
    {
        if (menu == null || key.IsNullOrEmpty()) return false;

        var marker = menu.Ex6;

        // 用户自定义：永不覆盖
        if (marker == CustomNameMarker) return false;

        // 存量未标记：一次性判定，识别旧版本落库的源文或译文
        if (marker == null)
        {
            if (IsAutoName(menu.DisplayName, key, friendly, fallbackNames))
            {
                menu.DisplayName = key;
                menu.Ex6 = key;
            }
            else
            {
                menu.Ex6 = CustomNameMarker;
            }

            return true;
        }

        // 自动项且显示名未被人工改动：跟随代码规范键（含类型改名）
        if (menu.DisplayName == marker)
        {
            if (menu.DisplayName == key) return false;

            menu.DisplayName = key;
            menu.Ex6 = key;

            return true;
        }

        // 标记仍在但显示名已被人工改动：转自定义，保留用户输入
        menu.Ex6 = CustomNameMarker;

        return true;
    }

    /// <summary>把新建的自动菜单标记为自动项</summary>
    /// <param name="menu">菜单</param>
    /// <param name="key">规范键，通常是类型全名</param>
    /// <remarks>同时写入显示名与标记，保证「显示名 == 标记」的自动项约定成立</remarks>
    public static void MarkAutoName(IMenu menu, String? key)
    {
        if (menu == null || key.IsNullOrEmpty()) return;

        menu.DisplayName = key;
        menu.Ex6 = key;
    }

    /// <summary>判断落库名称是否为自动项</summary>
    /// <param name="displayName">落库的显示名</param>
    /// <param name="key">规范键</param>
    /// <param name="friendly">人类可读的源文</param>
    /// <param name="fallbackNames">旧版本可能落库的名称</param>
    /// <returns>需要按规范键归一化时返回 true</returns>
    private static Boolean IsAutoName(String? displayName, String key, String? friendly, String?[]? fallbackNames)
    {
        if (displayName.IsNullOrEmpty()) return true;
        if (displayName == key) return true;

        if (!friendly.IsNullOrEmpty())
        {
            if (displayName == friendly) return true;
            if (displayName == L.T(friendly)) return true;
            if (LooksLikeTranslatedName(displayName, friendly)) return true;
        }

        if (fallbackNames != null)
        {
            foreach (var item in fallbackNames)
            {
                if (item != null && displayName == item) return true;
            }
        }

        return false;
    }

    /// <summary>判断落库名称是否像旧版本按其它语言写入的译文</summary>
    /// <param name="displayName">落库的显示名</param>
    /// <param name="source">人类可读的源文</param>
    /// <returns>疑似译文时返回 true</returns>
    /// <remarks>
    /// 仅用于存量迁移，不能用于日常判定：源码为中文、落库名为纯 ASCII 的，一律视为旧译文。
    /// 由于只在未标记的存量行上执行一次，不会覆盖后续的用户自定义名称。
    /// </remarks>
    internal static Boolean LooksLikeTranslatedName(String? displayName, String source)
    {
        if (displayName.IsNullOrEmpty()) return false;
        if (!source.Any(e => e >= 0x4E00 && e <= 0x9FFF)) return false;

        return displayName.All(e => e < 0x80);
    }

    /// <summary>获取菜单在请求语言下的显示名</summary>
    /// <param name="menu">菜单</param>
    /// <returns>本地化显示名</returns>
    public static String? GetDisplayName(IMenu menu)
    {
        if (menu == null) return null;

        var name = menu.DisplayName;
        if (name.IsNullOrEmpty()) name = menu.Name;
        if (name.IsNullOrEmpty()) return name;

        // 规范键词条
        var key = MenuKeyPrefix + name;
        var text = L.T(key);
        if (text != key) return text;

        // 旧数据/自定义名：命中词条则翻译
        text = L.T(name);
        if (text != name) return text;

        // 规范键缺译文：按类型解析人类可读名再翻译
        var friendly = ResolveFriendlyName(name);
        if (!friendly.IsNullOrEmpty() && friendly != name)
        {
            text = L.T(friendly);
            return text != friendly ? text : friendly;
        }

        // 兜底：短名，绝不把类型全名铺到界面
        var p = name.LastIndexOf('.');
        return p >= 0 && p < name.Length - 1 ? name[(p + 1)..] : name;
    }

    /// <summary>按规范键解析人类可读名。先取实体表显示名，再取控制器/区域/方法的 DisplayName</summary>
    /// <param name="key">菜单规范键或旧落库名</param>
    /// <returns>人类可读名，无法解析时返回 null</returns>
    internal static String? ResolveFriendlyName(String? key)
    {
        if (key.IsNullOrEmpty()) return null;

        var type = ResolveMenuType(key);
        if (type == null) return null;

        // 实体类型：取表显示名
        if (typeof(IEntity).IsAssignableFrom(type)) return GetEntityDisplayName(type) ?? type.Name;

        // 控制器类型
        if (type.Name.EndsWith("Controller"))
        {
            // 方法级菜单：FullName 末尾为方法名
            var prefix = type.FullName + ".";
            if (prefix != null && key.Length > prefix.Length && key.StartsWith(prefix))
            {
                var methodName = key[prefix.Length..];
                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                if (method != null)
                {
                    var dn = method.GetDisplayName();
                    return dn.IsNullOrEmpty() ? methodName : dn;
                }
            }

            var name = GetEntityDisplayName(GetEntityType(type));
            if (!name.IsNullOrEmpty()) return name;

            return type.GetDisplayName() ?? type.Name.TrimSuffix("Controller");
        }

        // 区域或其它类型
        return type.GetDisplayName() ?? type.Name;
    }

    /// <summary>获取实体表显示名</summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>显示名，非实体或无法解析时返回 null</returns>
    private static String? GetEntityDisplayName(Type? entityType)
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
                if (name.StartsWith("ReadOnlyEntityController`") || name.StartsWith("EntityController`"))
                    return t.GetGenericArguments()[0];
            }
            t = t.BaseType;
        }
        return null;
    }
}
