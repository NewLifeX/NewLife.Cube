using System.Reflection;
using NewLife.Cube.Entity;
using NewLife.Cube.Enums;
using NewLife.Log;
using XCode;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Services;

/// <summary>值集自动注册服务。启动时扫描指定命名空间下的 C# 枚举，自动注册为 Enum.xxx 值集</summary>
public class LovAutoRegisterService
{
    /// <summary>默认扫描的命名空间前缀列表</summary>
    public IList<String> NamespacePrefixes { get; } = new List<String> { "NewLife.Cube.Entity", typeof(AuthCategory).Namespace };

    /// <summary>是否已启用</summary>
    public Boolean Enabled { get; set; } = true;

    /// <summary>扫描并注册所有枚举类型</summary>
    public Int32 ScanAndRegister()
    {
        if (!Enabled || NamespacePrefixes.Count == 0) return 0;

        using var span = DefaultTracer.Instance?.NewSpan(nameof(ScanAndRegister));

        var count = 0;
        foreach (var prefix in NamespacePrefixes)
        {
            // 查找匹配命名空间前缀的所有程序集
            var assemblies = GetAssembliesByNamespace(prefix);
            foreach (var asm in assemblies)
            {
                try
                {
                    var types = asm.GetTypes().Where(t => t.IsEnum && t.IsPublic && t.Namespace != null && t.Namespace.StartsWith(prefix));
                    foreach (var enumType in types)
                    {
                        if (RegisterEnum(enumType, prefix))
                            count++;
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 跳过无法加载类型的程序集
                }
            }
        }

        return count;
    }

    /// <summary>根据命名空间前缀获取匹配的程序集</summary>
    private static IEnumerable<Assembly> GetAssembliesByNamespace(String prefix)
    {
        // 获取已加载的程序集
        var list = new List<Assembly>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                // 快速判断：程序集名或任何公开类型是否包含该命名空间
                var types = asm.GetExportedTypes();
                if (types.Any(t => t.Namespace != null && t.Namespace.StartsWith(prefix)))
                    list.Add(asm);
            }
            catch
            {
                // 跳过无法反射的程序集
            }
        }
        return list;
    }

    /// <summary>注册一个枚举类型到值集定义表</summary>
    private static Boolean RegisterEnum(Type enumType, String namespacePrefix)
    {
        // 计算 LovCode: Enum.{完全限定类型名}，确保全局唯一且自解释
        // 如 SmartMES.Data.ProcessCard.EnableStatus → Enum.SmartMES.Data.ProcessCard.EnableStatus
        var lovCode = $"Enum.{enumType.FullName}";

        XTrace.WriteLine("Lov: 检测到枚举 {0} → LovCode={1}", enumType.FullName, lovCode);

        // 查找或创建 LovDefinition
        var def = LovDefinition.Find(LovDefinition._.LovCode == lovCode);
        if (def == null)
        {
            def = new LovDefinition
            {
                LovCode = lovCode,
                Name = GetEnumDisplayName(enumType) ?? enumType.Name,
                Type = "ENUM",
                Source = "AUTO",
                Enabled = true,
            };
            def.Insert();
            XTrace.WriteLine("Lov: 自动注册值集 {0}", lovCode);
        }
        else if (def.Source != "AUTO")
        {
            // 手工管理的跳过
            return false;
        }

        // 同步枚举值（被 LovStringValue 标记的枚举使用成员名作为选项值，而非数字）
        // 按特性名识别，避免 NewLife.Cube 与定义该特性的程序集产生编译期耦合
        var useStringValue = enumType.GetCustomAttributes().Any(a => a.GetType().Name == "LovStringValueAttribute");
        SyncEnumValues(def, enumType, useStringValue);

        return true;
    }

    /// <summary>同步枚举值到 LovEnumItem（数据落到 Parameter，不触碰真实表）</summary>
    /// <param name="def">值集定义</param>
    /// <param name="enumType">要同步的枚举类型</param>
    /// <param name="useStringValue">为 true 时使用枚举成员名作为选项值（字符串），否则使用数字值</param>
    private static void SyncEnumValues(LovDefinition def, Type enumType, Boolean useStringValue)
    {
        var names = Enum.GetNames(enumType);
        var values = Enum.GetValues(enumType);

        // 读取现有记录（全部落到 Parameter）
        var existingItems = LovEnumItem.FindAllByLovDefId(def.Id);
        var existingMap = existingItems.ToDictionary(e => e.Value, e => e);

        // 当前枚举值集合
        var currentValues = new HashSet<String>();
        var result = new List<LovEnumItem>();

        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            // 默认用数字值；被 LovStringValue 标记的枚举改用成员名（字符串），便于与以名存储的字段对应
            var value = useStringValue ? name : Convert.ToInt32(values.GetValue(i)!).ToString();
            currentValues.Add(value);

            // 获取枚举成员的描述（可配合 DisplayName 或 Description 特性）
            var member = enumType.GetMember(name).FirstOrDefault();
            var label = name; // 默认使用枚举名
            if (member != null)
            {
                var displayAttr = member.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
                if (displayAttr != null && !String.IsNullOrEmpty(displayAttr.DisplayName))
                    label = displayAttr.DisplayName;
                else
                {
                    var descAttr = member.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
                    if (descAttr != null && !String.IsNullOrEmpty(descAttr.Description))
                        label = descAttr.Description;
                }
            }

            if (existingMap.TryGetValue(value, out var existing))
            {
                // 已存在：复用原行（保留审计字段与手工排序），仅更新可能变化的 label 与启用状态
                existing.Label = label;
                existing.Enabled = true;
                result.Add(existing);
            }
            else
            {
                // 新增枚举值
                result.Add(new LovEnumItem
                {
                    LovDefId = def.Id,
                    Value = value,
                    Label = label,
                    Sort = i,
                    Enabled = true,
                });
            }
        }

        // 逻辑禁用已删除的枚举成员：保留原行并置 Enabled=false
        foreach (var existing in existingItems)
        {
            if (!currentValues.Contains(existing.Value) && existing.Enabled)
            {
                existing.Enabled = false;
                result.Add(existing);
            }
        }

        // 整表覆盖写回 Parameter
        LovEnumItem.SaveAllByLovDefId(def.Id, result);
    }

    /// <summary>获取枚举类型的显示名称，优先 DisplayName 特性，无则返回 null</summary>
    private static String? GetEnumDisplayName(Type enumType)
    {
        var attr = enumType.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
        if (attr != null && !String.IsNullOrEmpty(attr.DisplayName))
            return attr.DisplayName;

        var descAttr = enumType.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        if (descAttr != null && !String.IsNullOrEmpty(descAttr.Description))
            return descAttr.Description;

        return null;
    }
}
