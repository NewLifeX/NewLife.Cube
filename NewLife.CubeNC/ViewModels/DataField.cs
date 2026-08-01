using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using NewLife.Collections;
using XCode;
using XCode.Configuration;

namespace NewLife.Cube.ViewModels;

/// <summary>获取数据源委托</summary>
/// <param name="entity"></param>
/// <returns></returns>
public delegate IDictionary DataSourceDelegate(Object entity);

/// <summary>数据可见委托</summary>
/// <param name="entity"></param>
/// <returns></returns>
public delegate Boolean DataVisibleDelegate(Object entity);

/// <summary>数据字段</summary>
public class DataField
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>描述</summary>
    public String Description { get; set; }

    /// <summary>类别</summary>
    public String Category { get; set; }

    /// <summary>属性类型</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public Type Type { get; set; }

    /// <summary>属性类型</summary>
    public String TypeName => Type?.Name;

    /// <summary>元素类型。image,file-zip,html,singleSelect,multipleSelect</summary>
    public String ItemType { get; set; }

    /// <summary>长度</summary>
    public Int32 Length { get; set; }

    /// <summary>精度</summary>
    public Int32 Precision { get; set; }

    /// <summary>位数</summary>
    public Int32 Scale { get; set; }

    /// <summary>允许空</summary>
    public Boolean Nullable { get; set; }

    /// <summary>主键</summary>
    public Boolean PrimaryKey { get; set; }

    /// <summary>只读</summary>
    public Boolean ReadOnly { get; set; }

    /// <summary>是否可见</summary>
    public Boolean Visible { get; set; }

    /// <summary>是否必填</summary>
    public Boolean Required { get; set; }

    /// <summary>权限相关。用户自由发挥</summary>
    public String Authority { get; set; }

    /// <summary>扩展字段。用户自由发挥</summary>
    public String Extended1 { get; set; }

    /// <summary>扩展字段。用户自由发挥</summary>
    public String Extended2 { get; set; }

    /// <summary>扩展字段。用户自由发挥</summary>
    public String Extended3 { get; set; }

    /// <summary>原始字段</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public FieldItem Field { get; set; }

    /// <summary>映射字段</summary>
    public String MapField { get; set; }

    /// <summary>LOV 配置代码</summary>
    public String LovCode { get; set; }

    /// <summary>映射提供者</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public MapProvider MapProvider { get; set; }

    /// <summary>多选数据源</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public DataSourceDelegate DataSource { get; set; }

    /// <summary>数据源字典（API/SPA 序列化输出，键值均为字符串）。与 DataSource 委托互补</summary>
    public IDictionary DataSourceMap { get; set; }

    /// <summary>是否显示</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public DataVisibleDelegate DataVisible { get; set; }

    /// <summary>扩展属性</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public IDictionary<String, String> Properties { get; set; } = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
    #endregion

    #region 构造
    /// <summary>实例化数据字段</summary>
    public DataField() { }

    /// <summary>实例化数据字段</summary>
    /// <param name="field"></param>
    public DataField(FieldItem field) => Fill(field);

    /// <summary>已重载</summary>
    /// <returns></returns>
    public override String ToString() => $"{Name} {DisplayName} {Type.Name}";
    #endregion

    #region 方法
    ///// <summary>实例化</summary>
    //public DataField() { }

    /// <summary>从FieldItem填充</summary>
    /// <param name="field"></param>
    public virtual void Fill(FieldItem field)
    {
        Field = field;

        var dc = field.Field;
        //var pi = field.GetValue("_Property", false) as PropertyInfo;
        var pi = field.Property;

        Name = field.Name;
        DisplayName = field.DisplayName;
        Description = field.Description;

        Category = pi?.GetCustomAttribute<CategoryAttribute>()?.Category + "";

        Type = field.Type;
        //DataType = field.Type.Name;

        Length = field.Length;
        Nullable = field.IsNullable;
        PrimaryKey = field.PrimaryKey;
        ReadOnly = field.ReadOnly;

        if (field.Map != null)
        {
            MapField = field.Map.Name;
            MapProvider = field.Map.Provider;
        }

        if (dc != null)
        {
            ItemType = dc.ItemType;
            Precision = dc.Precision;
            Scale = dc.Scale;

            if (dc.Properties != null)
            {
                foreach (var item in dc.Properties)
                {
                    Properties[item.Key] = item.Value;
                }
            }
        }
    }

    /// <summary>从PropertyInfo填充</summary>
    /// <param name="property"></param>
    public virtual void Fill(PropertyInfo property)
    {
        Name = property.Name;
        Type = property.PropertyType;

        Category = property?.GetCustomAttribute<CategoryAttribute>()?.Category + "";

        var df = property.GetCustomAttribute<DataObjectFieldAttribute>();
        if (df != null)
        {
            Length = df.Length;
            Nullable = df.IsNullable;
            PrimaryKey = df.PrimaryKey;
        }

        var dis = property.GetDisplayName();
        var des = property.GetDescription();
        if (dis.IsNullOrEmpty() && !des.IsNullOrEmpty()) { dis = des; des = null; }
        if (!dis.IsNullOrEmpty() && des.IsNullOrEmpty() && dis.Contains("。"))
        {
            des = dis.Substring("。");
            dis = dis.Substring(null, "。");
        }
        DisplayName = dis ?? property.Name;
        Description = des;

        var ra = property.GetCustomAttribute<ReadOnlyAttribute>();
        if (ra != null) ReadOnly = ra.IsReadOnly;
    }

    /// <summary>克隆</summary>
    /// <returns></returns>
    public virtual DataField Clone()
    {
        //var df = GetType().CreateInstance() as DataField;

        //df.Name = Name;
        //df.DisplayName = DisplayName;
        //df.Description = Description;
        //df.Category = Category;

        //df.Type = Type;
        //df.ItemType = ItemType;
        //df.Length = Length;
        //df.Precision = Precision;
        //df.Scale = Scale;
        //df.Nullable = Nullable;
        //df.PrimaryKey = PrimaryKey;
        //df.Readonly = Readonly;

        //df.Field = Field;
        //df.MapField = MapField;
        //df.MapProvider = MapProvider;
        //df.DataSource = DataSource;
        ////df.Properties = Properties;

        //foreach (var item in Properties)
        //{
        //    df.Properties[item.Key] = item.Value;
        //}

        //df._services = _services;

        //return df;

        return MemberwiseClone() as DataField;
    }

    /// <summary>是否大文本字段</summary>
    /// <returns></returns>
    public virtual Boolean IsBigText() => Type == typeof(String) && (Length < 0 || Length >= 300 || Length >= 200 && Name.EqualIgnoreCase("Remark", "Description", "Comment"));

    /// <summary>是否附件列</summary>
    /// <returns></returns>
    public Boolean IsAttachment() => ItemType.EqualIgnoreCase("file", "image") || ItemType.StartsWithIgnoreCase("file-", "image-");

    /// <summary>格式化数据用于显示</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual String FormatValue(Object value) => ViewHelper.FormatValue(!ItemType.IsNullOrEmpty() ? ItemType : Field?.Field?.DataScale, value, Description);

    /// <summary>为 API/SPA 物化数据源字典（布尔/枚举/DataSource 委托），避免前端反复请求</summary>
    public virtual void PrepareForApi()
    {
        if (DataSourceMap != null && DataSourceMap.Count > 0) return;

        var map = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

        if (DataSource != null)
        {
            try
            {
                var dic = DataSource(null);
                if (dic != null)
                {
                    foreach (DictionaryEntry de in dic)
                    {
                        if (de.Key == null) continue;
                        map[de.Key + ""] = de.Value + "";
                    }
                }
            }
            catch { /* 忽略委托异常，回落类型推断 */ }
        }

        if (map.Count == 0 && Type != null)
        {
            var t = System.Nullable.GetUnderlyingType(Type) ?? Type;
            if (t == typeof(Boolean))
            {
                map["true"] = "是";
                map["false"] = "否";
                map["1"] = "是";
                map["0"] = "否";
            }
            else if (t.IsEnum)
            {
                foreach (var v in Enum.GetValues(t))
                {
                    var name = Enum.GetName(t, v);
                    if (name.IsNullOrEmpty()) continue;
                    var fi = t.GetField(name);
                    var dn = fi?.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                        ?? fi?.GetCustomAttribute<DescriptionAttribute>()?.Description
                        ?? name;
                    var num = Convert.ToInt64(v).ToString();
                    map[num] = dn;
                    map[name] = dn;
                }
                if (LovCode.IsNullOrEmpty() && t.FullName != null)
                    LovCode = "Enum." + t.FullName;
            }
        }

        if (map.Count > 0) DataSourceMap = map;
    }
    #endregion

    #region 服务
    private readonly List<Object> _services = [];
    /// <summary>扩展服务</summary>
    [XmlIgnore, IgnoreDataMember]
    public IList<Object> Services => _services;

    /// <summary>添加服务</summary>
    /// <typeparam name="TService"></typeparam>
    /// <param name="service"></param>
    public virtual void AddService<TService>(TService service) => _services.Add(service);

    /// <summary>获取服务</summary>
    /// <typeparam name="TService"></typeparam>
    /// <returns></returns>
    public virtual TService GetService<TService>() => (TService)_services.FirstOrDefault(e => e is TService);
    #endregion

    #region 类型转换
    /// <summary>类型转换</summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(obj))]
    public static implicit operator DataField(FieldItem obj) => !obj.Equals(null) ? new DataField(obj) : null;
    #endregion
}