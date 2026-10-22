using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using NewLife;

namespace NewLife.Cube;

/// <summary>枚举标签本地化助手</summary>
/// <remarks>枚举描述属于表结构元数据，保持源文；仅在界面输出时按当前语言翻译。</remarks>
public static class EnumLocalization
{
    /// <summary>获取枚举描述并按当前语言翻译</summary>
    /// <param name="value">枚举值</param>
    /// <returns>本地化描述；没有描述特性时回退枚举名</returns>
    public static String? GetDescription(Enum? value)
    {
        if (value == null) return null;

        return L.T(value.GetDescription() ?? value.ToString());
    }

    /// <summary>获取枚举值到本地化描述的映射</summary>
    /// <param name="enumType">枚举类型</param>
    /// <returns>枚举值到本地化描述的映射</returns>
    public static Dictionary<Int32, String> GetDescriptions(Type enumType)
    {
        var dic = new Dictionary<Int32, String>();
        if (enumType == null || !enumType.IsEnum) return dic;

        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var raw = field.GetValue(null);
            var key = raw == null ? 0 : unchecked((Int32)Convert.ToInt64(raw));

            var label = field.GetCustomAttribute<DisplayNameAttribute>(false)?.DisplayName;
            if (label.IsNullOrEmpty()) label = field.GetCustomAttribute<DescriptionAttribute>(false)?.Description;
            if (label.IsNullOrEmpty()) label = field.Name;

            dic[key] = L.T(label);
        }

        return dic;
    }
}
