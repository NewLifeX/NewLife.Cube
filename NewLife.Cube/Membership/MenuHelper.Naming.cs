using XCode.Membership;

namespace NewLife.Cube.Membership;

/// <summary>菜单命名契约。显示名只落库源文或用户自定义文本，渲染期按请求语言翻译</summary>
/// <remarks>
/// 自动生成菜单的显示名来自控制器/区域/方法的 DisplayName 特性（即源文），用户可在菜单管理里改名。
/// <c>Menu.Ex4</c> 已被 API 版用作帮助文档链接，因此用 <c>Menu.Ex6</c> 作为命名标记，三态含义：
/// 空表示存量未标记，下次扫描做一次性迁移；<see cref="CustomNameMarker"/> 表示用户自定义，永不覆盖；
/// 其它非空值表示自动项最后一次写入的源文。
/// 只要显示名与标记一致，就说明没有人改过，可以安全跟随代码源文刷新（含代码侧改名）；
/// 不一致说明被人工改过，转为自定义并保留用户输入。
/// </remarks>
public static partial class MenuHelper
{
    /// <summary>用户自定义名称标记。写入菜单扩展字段，此后的扫描不再覆盖该菜单显示名</summary>
    public const String CustomNameMarker = "#custom";

    /// <summary>按代码源文同步菜单显示名</summary>
    /// <param name="menu">菜单</param>
    /// <param name="source">代码源文，来自控制器/区域/方法的 DisplayName 特性</param>
    /// <param name="fallbackNames">旧版本可能落库的名称，如菜单名、类型名</param>
    /// <returns>是否修改了菜单，调用方据此决定是否保存</returns>
    /// <remarks>
    /// 自动项跟随代码源文刷新，用户自定义名称保持不动。
    /// 未标记的存量行按「空名、菜单名、类型名、当前译文、其它语言译文」识别一次自动项，其余视为自定义。
    /// </remarks>
    public static Boolean SyncDisplayName(IMenu menu, String? source, params String?[] fallbackNames)
    {
        if (menu == null || source.IsNullOrEmpty()) return false;

        var marker = menu.Ex6;

        // 用户自定义：永不覆盖
        if (marker == CustomNameMarker) return false;

        // 存量未标记：一次性判定，识别旧版本按启动语言落库的译文
        if (marker == null)
        {
            if (IsAutoName(menu.DisplayName, source, fallbackNames))
            {
                menu.DisplayName = source;
                menu.Ex6 = source;
            }
            else
            {
                menu.Ex6 = CustomNameMarker;
            }

            return true;
        }

        // 自动项且显示名未被人工改动：跟随代码源文（含代码侧改名）
        if (menu.DisplayName == marker)
        {
            if (menu.DisplayName == source) return false;

            menu.DisplayName = source;
            menu.Ex6 = source;

            return true;
        }

        // 标记仍在但显示名已被人工改动：转自定义，保留用户输入
        menu.Ex6 = CustomNameMarker;

        return true;
    }

    /// <summary>把新建的自动菜单标记为自动项</summary>
    /// <param name="menu">菜单</param>
    /// <param name="source">代码源文</param>
    /// <remarks>源文为空（没有 DisplayName 特性）时不标记，留给后续扫描按实体表显示名补齐</remarks>
    public static void MarkAutoName(IMenu menu, String? source)
    {
        if (menu == null || source.IsNullOrEmpty()) return;

        menu.Ex6 = source;
    }

    /// <summary>判断落库名称是否为自动项</summary>
    /// <param name="displayName">落库的显示名</param>
    /// <param name="source">代码源文</param>
    /// <param name="fallbackNames">旧版本可能落库的名称</param>
    /// <returns>需要按源文归一化时返回 true</returns>
    private static Boolean IsAutoName(String? displayName, String source, String?[]? fallbackNames)
    {
        if (displayName.IsNullOrEmpty()) return true;
        if (displayName == source) return true;

        if (fallbackNames != null)
        {
            foreach (var item in fallbackNames)
            {
                if (item != null && displayName == item) return true;
            }
        }

        // 旧版本按启动语言写入当前语言译文
        if (displayName == L.T(source)) return true;

        return LooksLikeTranslatedName(displayName, source);
    }

    /// <summary>判断落库名称是否像旧版本按其它语言写入的译文</summary>
    /// <param name="displayName">落库的显示名</param>
    /// <param name="source">代码源文</param>
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
}
