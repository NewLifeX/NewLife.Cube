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
public class DataField : IDictionarySource
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String DisplayName { get; set; }

    /// <summary>描述</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String Description { get; set; }

    /// <summary>类别</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String Category { get; set; }

    /// <summary>属性类型</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public Type Type { get; set; }

    /// <summary>属性类型</summary>
    public String TypeName => Type?.Name;

    /// <summary>元素类型。image,file-zip,html,singleSelect,multipleSelect</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String ItemType { get; set; }

    /// <summary>长度</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Int32 Length { get; set; }

    /// <summary>精度</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Int32 Precision { get; set; }

    /// <summary>位数</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Int32 Scale { get; set; }

    /// <summary>允许空</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Boolean Nullable { get; set; }

    /// <summary>主键</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Boolean PrimaryKey { get; set; }

    /// <summary>只读</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Boolean ReadOnly { get; set; }

    /// <summary>是否可见</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Boolean Visible { get; set; }

    /// <summary>是否必填</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Boolean Required { get; set; }

    /// <summary>权限相关。用户自由发挥</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String Authority { get; set; }

    /// <summary>扩展字段。用户自由发挥</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String Extended1 { get; set; }

    /// <summary>扩展字段。用户自由发挥</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String Extended2 { get; set; }

    /// <summary>扩展字段。用户自由发挥</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String Extended3 { get; set; }

    /// <summary>原始字段</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public FieldItem Field { get; set; }

    /// <summary>映射字段</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String MapField { get; set; }

    /// <summary>取值字段。列表单元格优先取该字段值，为空时回退到本字段。如名称列优先显示昵称：Name.ValueField=DisplayName</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String ValueField { get; set; }

    /// <summary>LOV 配置代码</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String LovCode { get; set; }

    /// <summary>映射提供者</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public MapProvider MapProvider { get; set; }

    /// <summary>多选数据源</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public DataSourceDelegate DataSource { get; set; }

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

    /// <summary>转字典，用于FastJson序列化。仅输出有意义字段，忽略null/0/false等默认值，显著减小GetPage等元数据接口体积</summary>
    /// <returns></returns>
    public virtual IDictionary<String, Object> ToDictionary()
    {
        var dic = new Dictionary<String, Object>
        {
            ["name"] = Name,
        };

        if (!DisplayName.IsNullOrEmpty()) dic["displayName"] = DisplayName;
        if (!Description.IsNullOrEmpty()) dic["description"] = Description;
        if (!Category.IsNullOrEmpty()) dic["category"] = Category;
        if (!TypeName.IsNullOrEmpty()) dic["typeName"] = TypeName;
        if (!ItemType.IsNullOrEmpty()) dic["itemType"] = ItemType;
        if (Length > 0) dic["length"] = Length;
        if (Precision > 0) dic["precision"] = Precision;
        if (Scale > 0) dic["scale"] = Scale;
        if (Nullable) dic["nullable"] = true;
        if (PrimaryKey) dic["primaryKey"] = true;
        if (ReadOnly) dic["readOnly"] = true;
        if (Visible) dic["visible"] = true;
        if (Required) dic["required"] = true;
        if (!Authority.IsNullOrEmpty()) dic["authority"] = Authority;
        if (!Extended1.IsNullOrEmpty()) dic["extended1"] = Extended1;
        if (!Extended2.IsNullOrEmpty()) dic["extended2"] = Extended2;
        if (!Extended3.IsNullOrEmpty()) dic["extended3"] = Extended3;
        if (!MapField.IsNullOrEmpty()) dic["mapField"] = MapField;
        if (!ValueField.IsNullOrEmpty()) dic["valueField"] = ValueField;
        if (!LovCode.IsNullOrEmpty()) dic["lovCode"] = LovCode;

        // 子类扩展字段
        switch (this)
        {
            case ListField lf:
                if (!lf.Text.IsNullOrEmpty()) dic["text"] = lf.Text;
                if (!lf.Title.IsNullOrEmpty()) dic["title"] = lf.Title;
                if (!lf.Url.IsNullOrEmpty()) dic["url"] = lf.Url;
                if (!lf.Target.IsNullOrEmpty()) dic["target"] = lf.Target;
                if (!lf.Header.IsNullOrEmpty()) dic["header"] = lf.Header;
                if (!lf.HeaderTitle.IsNullOrEmpty()) dic["headerTitle"] = lf.HeaderTitle;
                if (lf.TextAlign != TextAligns.Default) dic["textAlign"] = lf.TextAlign;
                if (!lf.Class.IsNullOrEmpty()) dic["class"] = lf.Class;
                if (lf.MaxWidth > 0) dic["maxWidth"] = lf.MaxWidth;
                if (!lf.DataAction.IsNullOrEmpty()) dic["dataAction"] = lf.DataAction;
                break;
            case SearchField sf:
                if (sf.Multiple) dic["multiple"] = true;
#if MVC
                if (!sf.View.IsNullOrEmpty()) dic["view"] = sf.View;
#endif
                break;
            case FormField ff:
#if MVC
                if (!ff.GroupView.IsNullOrEmpty()) dic["groupView"] = ff.GroupView;
                if (!ff.ItemView.IsNullOrEmpty()) dic["itemView"] = ff.ItemView;
#endif
                if (ff.Expand != null)
                {
                    var exp = ff.Expand;
                    var edic = new Dictionary<String, Object>
                    {
                        ["name"] = exp.Name,
                    };
                    if (exp.Retain) edic["retain"] = true;
                    if (!exp.Prefix.IsNullOrEmpty()) edic["prefix"] = exp.Prefix;
                    if (!exp.Category.IsNullOrEmpty()) edic["category"] = exp.Category;
                    dic["expand"] = edic;
                }
                break;
        }

        return dic;
    }

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

        Category = pi?.GetCustomAttribute<CategoryAttribute>()?.Category;

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

        Category = property?.GetCustomAttribute<CategoryAttribute>()?.Category;

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