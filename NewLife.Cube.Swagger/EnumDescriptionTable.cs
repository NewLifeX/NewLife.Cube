using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace NewLife.Cube.Swagger;

/// <summary>枚举 OpenAPI 取值表：反射成员 <c>[Description]</c> 与整数值，拼成 <c>0=None 未指定；1=PROD 生产；…</c>。</summary>
internal static class EnumDescriptionTable
{
    /// <summary>description 尾部追加的固定前缀，兼作幂等标记。</summary>
    internal const String Marker = "\n取值：";

    /// <summary>若尚未注入则把取值表追加到 description。</summary>
    internal static void AppendIfNeeded(ref String? description, Type? clrType)
    {
        if (clrType == null) return;

        var enumType = UnwrapEnum(clrType);
        if (enumType == null) return;

        if (description != null && description.Contains(Marker, StringComparison.Ordinal))
            return;

        var table = Build(enumType);
        description = String.IsNullOrEmpty(description)
            ? $"{Marker}{table}"
            : $"{description}{Environment.NewLine}{Marker}{table}";
    }

    /// <summary>把 <c>Section?</c>、<c>WorkOrderEvent[]</c>、<c>List&lt;T&gt;</c> 统一解出内部 enum 类型。</summary>
    internal static Type? UnwrapEnum(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        if (t.IsEnum) return t;

        if (t.IsArray)
        {
            var el = t.GetElementType();
            if (el != null && (Nullable.GetUnderlyingType(el) ?? el).IsEnum) return Nullable.GetUnderlyingType(el) ?? el;
        }

        if (t.IsGenericType)
        {
            var args = t.GetGenericArguments();
            if (args.Length == 1)
            {
                var el = Nullable.GetUnderlyingType(args[0]) ?? args[0];
                if (el.IsEnum) return el;
            }
        }

        return null;
    }

    /// <summary>反射枚举成员拼表。</summary>
    internal static String Build(Type enumType)
    {
        var sb = new StringBuilder();
        var names = Enum.GetNames(enumType);
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            var member = enumType.GetField(name)!;
            var desc = member.GetCustomAttribute<DescriptionAttribute>()?.Description;
            var value = Convert.ToInt64(Enum.Parse(enumType, name));
            if (i > 0) sb.Append('；');
            sb.Append(value).Append('=').Append(name);
            if (!String.IsNullOrEmpty(desc)) sb.Append(' ').Append(desc);
        }

        return sb.ToString();
    }
}
