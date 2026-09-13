using NewLife.Caching;
using NewLife.Cube.Entity;
using NewLife.Cube.Services;
using NewLife.Cube.ViewModels;
using XCode;
using XCode.Configuration;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>为列表/搜索/添加/编辑/详情字段补全 Map 外键候选（OSC-0016 / OSC-26082097c1）。
/// 小表内联 DataSourceMap，大表写 Entity. LovCode；地区字段标 ItemType=area4 走 Cascader。</summary>
public static class MapCandidateFiller
{
    /// <summary>按工厂 AllFields 上的 Map 与成员实体名称启发式，补全字段候选</summary>
    /// <param name="fields">GetPage/GetFields 字段列表</param>
    /// <param name="factory">当前实体工厂</param>
    public static void Apply(IList<DataField> fields, IEntityFactory factory)
    {
        if (fields == null || fields.Count == 0 || factory == null) return;

        var mapFields = factory.AllFields
            .Where(e => e.Map != null && !e.Map.Name.IsNullOrEmpty())
            .GroupBy(e => e.Map.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var df in fields)
        {
            if (df == null) continue;

            // 列表列（ListField）：只给外键编号列补候选，展示列本身已可读；也不做 ItemType/物理列改写
            var forList = df is ListField;

            if (IsAreaLikeField(df))
            {
                // 列表地区列沿用既有级联渲染（前端按 ItemType/地区缓存翻译），不做 area4 改写
                if (forList) continue;

                if (!IsCascaderItemType(df.ItemType)) df.ItemType = "area4";
                RestorePhysicalColumnName(df, factory);
                continue;
            }

            // 列表展示列（String）已是名称，补字典只会多出徽章，且无法翻译多值串（如 RoleIds=1,2）
            if (forList && df.Type == typeof(String)) continue;

            RestorePhysicalColumnName(df, factory);

            var provider = df.MapProvider;
            if (provider == null || provider.EntityType == null)
            {
                FieldItem ext = null;
                if (mapFields.Count > 0)
                {
                    if (!mapFields.TryGetValue(df.Name, out ext) && !df.MapField.IsNullOrEmpty())
                        mapFields.TryGetValue(df.MapField, out ext);
                }
                provider = ext?.Map?.Provider;
            }
            if (provider == null || provider.EntityType == null)
                provider = InferMembershipProvider(df.Name);
            if (provider == null || provider.EntityType == null) continue;

            ApplyProvider(df, provider);
        }
    }

    /// <summary>SetRelation 会把物理列换成查找展示列（RoleID→RoleName、AreaId→AreaPath）。
    /// SPA 表单按 Name 绑定实体 JSON，必须改回物理列，并清掉查找列的 ReadOnly，否则级联/下拉会被禁用。</summary>
    private static void RestorePhysicalColumnName(DataField df, IEntityFactory factory)
    {
        if (df is not FormField) return;
        if (df.MapField.IsNullOrEmpty() || df.Name.EqualIgnoreCase(df.MapField)) return;
        var col = factory.Table.FindByName(df.MapField);
        if (col is null) return;

        df.Name = col.Name;
        if (col.Type != null) df.Type = col.Type;
        df.ReadOnly = col.ReadOnly;
        df.Nullable = col.IsNullable;
        df.Length = col.Length;
        df.PrimaryKey = col.PrimaryKey;
    }

    private static void ApplyProvider(DataField df, MapProvider provider)
    {
        if (!df.LovCode.IsNullOrEmpty())
        {
            // 值集已全面代码优先：Enum./List. 由后端 LovRegistry 反射直读（不落库），命中则无需内联候选
            if (df.LovCode.StartsWith("Enum.", StringComparison.OrdinalIgnoreCase) || df.LovCode.StartsWith("List.", StringComparison.OrdinalIgnoreCase))
                return;
        }
        if (df.DataSourceMap != null && df.DataSourceMap.Count > 0) return;

        var entityType = provider.EntityType;
        var cacheKey = "LovMapCount:" + entityType.FullName;
        var count = MemoryCache.Instance.Get<Int32>(cacheKey);
        if (count <= 0)
        {
            var fact = EntityFactory.CreateFactory(entityType);
            count = fact.Session.Count;
            MemoryCache.Instance.Set(cacheKey, count, 60);
        }

        if (count <= CubeSetting.Current.MaxDropDownList)
        {
            var dic = provider.GetDataSource();
            if (dic == null) return;
            var map2 = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            foreach (var de in dic)
            {
                if (de.Key == null) continue;
                map2[de.Key + ""] = de.Value + "";
            }
            if (map2.Count > 0) df.DataSourceMap = map2;
        }
        else
        {
            df.LovCode = "Entity." + entityType.FullName;
        }
    }

    private static Boolean IsCascaderItemType(String itemType) =>
        !itemType.IsNullOrEmpty() && itemType.EqualIgnoreCase("area", "area4", "cascader");

    private static Boolean IsAreaLikeField(DataField df)
    {
        if (IsCascaderItemType(df.ItemType)) return true;
        if (df.Name.EqualIgnoreCase("AreaId", "AreaID", "AreaIds", "Area")) return true;
        return !df.MapField.IsNullOrEmpty() && df.MapField.EqualIgnoreCase("AreaId", "AreaID", "AreaIds");
    }

    /// <summary>无 Map 时按 OSC-26082097c1 白名单推断成员实体（RoleIds 的 Map 在 RoleNames 上且无 Provider）</summary>
    private static MapProvider InferMembershipProvider(String name)
    {
        if (name.IsNullOrEmpty()) return null;
        var n = name.ToLowerInvariant();
        var type = n switch
        {
            "roleid" or "roleids" => typeof(Role),
            "departmentid" or "departmentids" => typeof(Department),
            "userid" or "userids" or "createuserid" or "updateuserid" or "creatorid" or "updaterid" => typeof(User),
            "menuid" or "menuids" => typeof(Menu),
            _ => null,
        };
        if (type == null) return null;
        var fact = EntityFactory.CreateFactory(type);
        var key = fact?.Unique?.Name ?? "ID";
        return new MapProvider { EntityType = type, Key = key };
    }
}
