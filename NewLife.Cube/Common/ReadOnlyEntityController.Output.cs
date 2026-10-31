using System.Collections.Concurrent;
using System.Reflection;
using NewLife.Cube.ViewModels;
using NewLife.Data;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

partial class ReadOnlyEntityController<TEntity>
{
    #region 输出投影
    /// <summary>获取输出字段集合。接口回包 = 字段集合 × 行值，集合之外的字段不下发</summary>
    /// <remarks>
    /// 列表取列表字段；详情取详情字段并补上编辑表单字段（详情接口同时是 SPA 编辑回显的数据源）；
    /// 写接口回包取编辑表单字段。主键恒定包含，供行操作与更新定位使用。
    /// </remarks>
    /// <param name="kind">视图类型。只支持 List/Detail/EditForm/AddForm</param>
    /// <param name="entity">当前实体，用于展开嵌入式配置字段；列表场景传 null</param>
    /// <returns>输出字段集合</returns>
    protected virtual IList<DataField> OnGetOutputFields(ViewKinds kind, TEntity entity = null)
    {
        var fields = new List<DataField>();
        switch (kind)
        {
            case ViewKinds.Detail:
                fields.AddRange(OnGetFields(ViewKinds.Detail, entity));
                // 详情接口同时承担编辑回显：补上只在编辑表单出现的字段（多选、虚拟字段等）
                foreach (var df in OnGetFields(ViewKinds.EditForm, entity))
                {
                    if (!fields.Any(e => e.Name.EqualIgnoreCase(df.Name))) fields.Add(df);
                }
                break;
            case ViewKinds.EditForm:
            case ViewKinds.AddForm:
                fields.AddRange(OnGetFields(kind, entity));
                break;
            default:
                fields.AddRange(OnGetFields(ViewKinds.List, entity));
                break;
        }

        // 主键恒下发：行操作与编辑定位都依赖它，即使用户把它从字段集合里移出
        var uk = Factory.Unique;
        if (uk != null && !fields.Any(e => e.Name.EqualIgnoreCase(uk.Name)))
        {
            var fi = Factory.Fields.FirstOrDefault(e => e.Name.EqualIgnoreCase(uk.Name));
            if (fi != null) fields.Insert(0, new DataField(fi));
        }

        return fields;
    }

    /// <summary>按字段集合投影一行数据</summary>
    /// <remarks>回包键默认转小驼峰（与实体属性序列化口径一致），前端按元数据字段名取值时大小写不敏感</remarks>
    /// <param name="entity">实体对象</param>
    /// <param name="fields">输出字段集合。为空时使用列表字段</param>
    /// <param name="camelCase">键名是否转小驼峰。导出保留字段原名，便于与实体列对齐</param>
    /// <returns>字段名与取值的数据行。只写字段不参与投影</returns>
    protected virtual ApiRow OnProject(TEntity entity, IList<DataField> fields = null, Boolean camelCase = true)
    {
        if (entity == null) return null;
        fields ??= OnGetOutputFields(ViewKinds.List);

        var row = new ApiRow();
        // 实际输出的字段名，扩展值只允许回填这些键（集合之外的扩展项不下发）
        var visible = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        foreach (var df in fields)
        {
            // 只写字段（ItemType=password）取值永不下发
            if (df.IsWriteOnly) continue;
            // 行级可见性：DataVisible 为 false 时不输出该列
            if (df.DataVisible != null && !df.DataVisible(entity)) continue;

            visible.Add(df.Name);
            row[camelCase ? ToCamelCase(df.Name) : df.Name] = GetProjectValue(entity, df);
        }

        // 扩展值（虚拟列计算结果等）随行内联；只补字段集合内、尚未取到值的键
        if (entity is IExtend ext)
        {
            foreach (var item in ext.Items)
            {
                if (!visible.Contains(item.Key)) continue;

                var key = camelCase ? ToCamelCase(item.Key) : item.Key;
                if (row.TryGetValue(key, out var current) && current != null) continue;

                row[key] = item.Value;
            }
        }

        return row;
    }

    /// <summary>字段名转小驼峰。ID 特例为 id，其余仅小写首字母（与实体属性序列化一致）</summary>
    /// <param name="name">字段名</param>
    /// <returns>小驼峰字段名</returns>
    protected static String ToCamelCase(String name)
    {
        if (name.IsNullOrEmpty()) return name;
        if (name == "ID") return "id";

        return Char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <summary>属性反射缓存。同一类型的同一字段只解析一次</summary>
    private static readonly ConcurrentDictionary<(Type, String), PropertyInfo?> _propertyCache = new();

    /// <summary>取字段值。列表字段优先用计算委托，其次实体索引器，最后反射虚拟属性</summary>
    /// <param name="entity">实体对象</param>
    /// <param name="field">字段</param>
    /// <returns>字段值</returns>
    protected static Object GetProjectValue(TEntity entity, DataField field)
    {
        if (field is ListField { GetValue: not null } lf) return lf.GetValue(entity);

        var value = entity[field.Name];
        if (value != null) return value;

        // TreeNodeText 等虚拟属性不在实体表结构中，索引器取不到，退回属性反射（按类型缓存 PropertyInfo）
        var key = (entity.GetType(), field.Name.ToLowerInvariant());
        var pi = _propertyCache.GetOrAdd(key, static k => k.Item1.GetProperty(k.Item2, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
        if (pi != null && pi.CanRead && pi.GetIndexParameters().Length == 0) return pi.GetValue(entity);

        return null;
    }
    #endregion

    #region 只写字段
    private HashSet<String> _writeOnlyNames;
    /// <summary>判断是否为只写字段名。更新时提交空值表示不修改</summary>
    /// <param name="name">字段名</param>
    /// <returns>是否只写</returns>
    protected Boolean IsWriteOnlyName(String name)
    {
        _writeOnlyNames ??= new HashSet<String>(OnGetFields(ViewKinds.EditForm, null).Where(e => e.IsWriteOnly).Select(e => e.Name), StringComparer.OrdinalIgnoreCase);

        return !name.IsNullOrEmpty() && _writeOnlyNames.Contains(name);
    }

    /// <summary>判断值是否为空。只写字段留空表示不修改</summary>
    /// <param name="value">字段值</param>
    /// <returns>空值返回 true</returns>
    protected static Boolean IsEmpty(Object value) => value == null || value is String str && str.Length == 0;
    #endregion
}
