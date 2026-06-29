using System.Reflection;
using NewLife.Cube.Entity;
using NewLife.Log;
using XCode;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Services;

/// <summary>值集自动注册服务。启动时扫描指定命名空间下的 C# 枚举，自动注册为 Enum.xxx 值集</summary>
public class LovAutoRegisterService
{
    /// <summary>默认扫描的命名空间前缀列表</summary>
    public IList<String> NamespacePrefixes { get; } = new List<String> { "NewLife.Cube.Entity" };

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

        // 同步枚举值
        SyncEnumValues(def, enumType);

        return true;
    }

    /// <summary>同步枚举值到 LovEnumItem</summary>
    private static void SyncEnumValues(LovDefinition def, Type enumType)
    {
        var names = Enum.GetNames(enumType);
        var values = Enum.GetValues(enumType);

        // 获取现有记录
        var existingItems = LovEnumItem.FindAll(LovEnumItem._.LovDefId == def.Id);
        var existingMap = existingItems.ToDictionary(e => e.Value, e => e);

        // 当前枚举值集合
        var currentValues = new HashSet<String>();

        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            var value = Convert.ToInt32(values.GetValue(i)!).ToString();
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
                // 已存在，更新 label
                if (existing.Label != label)
                {
                    existing.Label = label;
                    existing.Update();
                }
                // 确保启用
                if (!existing.Enabled)
                {
                    existing.Enabled = true;
                    existing.Update();
                }
            }
            else
            {
                // 新增枚举值
                var item = new LovEnumItem
                {
                    LovDefId = def.Id,
                    Value = value,
                    Label = label,
                    Sort = i,
                    Enabled = true,
                };
                item.Insert();
            }
        }

        // 逻辑禁用已删除的枚举成员
        foreach (var existing in existingItems)
        {
            if (!currentValues.Contains(existing.Value) && existing.Enabled)
            {
                existing.Enabled = false;
                existing.Update();
            }
        }
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
