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

    /// <summary>描述源文。字段只有描述特性时保存整段描述，供多语言翻译后再拆分显示名与描述</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public String SourceDescription { get; set; }

    /// <summary>是否为字段默认显示名。用于区分控制器自定义的列标题，避免本地化后误判</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public Boolean IsDefaultDisplayName { get; set; } = true;

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

        // 扩展属性透传。核心不解释 props，由前端渲染器按约定消费
        if (Properties != null && Properties.Count > 0) dic["props"] = Properties;

        // 数据源。优先 DataSource 委托（控制器显式配置，如角色组），否则枚举字段自动构建选项
        // 前端 LovSelect 优先使用 dataSource 渲染下拉选项（对齐 MVC _Form_Item/_Form_Int）
        if (DataSource != null)
        {
            try
            {
                var ds = DataSource(null);
                if (ds != null && ds.Count > 0)
                {
                    var sds = new Dictionary<String, String>();
                    foreach (DictionaryEntry item in ds)
                    {
                        if (item.Key != null) sds[item.Key + ""] = item.Value + "";
                    }
                    dic["dataSource"] = sds;

                    // 多选：显式 ItemType=multipleSelect，或字段名以 s 结尾（MVC _Form_Item 约定）
                    if (String.Equals(ItemType, "multipleSelect", StringComparison.OrdinalIgnoreCase) || Name.EndsWith("s"))
                        dic["multiple"] = true;
                }
            }
            catch { /* 委托依赖实体上下文时可能失败，忽略并降级 */ }
        }
        else if (Type != null && Type.IsEnum)
        {
            // 枚举选项：反射枚举成员，标签取 DisplayName/Description/成员名（对齐 MVC _Form_Int）
            var members = Type.GetFields(BindingFlags.Public | BindingFlags.Static);
            var sds = new Dictionary<String, String>();
            foreach (var item in members)
            {
                var value = Convert.ToInt64(item.GetValue(null));
                var label = item.Name;
                var dna = item.GetCustomAttribute<DisplayNameAttribute>(false);
                if (dna != null && !String.IsNullOrEmpty(dna.DisplayName)) label = dna.DisplayName;
                var att = item.GetCustomAttribute<DescriptionAttribute>(false);
                if (att != null && !String.IsNullOrEmpty(att.Description)) label = att.Description;
                sds[value + ""] = L.T(label);
            }
            if (sds.Count > 0) dic["dataSource"] = sds;

            // 级联（LovSelect）：枚举成员带 [Parent] 特性（按简单名识别，与框架解耦，同 LovRegistry.GetEnumOptions 约定）
            // 时，该字段是"子字典"（如工单类型 OrderType 的每个成员归属某板块 Section）。
            // 下发 lovCode（Enum.全名，供前端 LovSelect 拉取带 parent 的选项）+ parentField（父枚举类型名，
            // 约定=同级字段名，如 Section），前端据此把同级父字段当前值传给 LovSelect :parent 做过滤。
            // 无 [Parent] 的枚举（如 Section 本身、优先级）保持普通 select 下拉，行为不变。
            Type parentType = null;
            foreach (var m in members)
            {
                foreach (var a in m.GetCustomAttributes())
                {
                    if (a.GetType().Name == "ParentAttribute")
                    {
                        var pv = a.GetType().GetProperty("Value")?.GetValue(a);
                        if (pv != null) { parentType = pv.GetType(); break; }
                    }
                }
                if (parentType != null) break;
            }
            if (parentType != null)
            {
                dic["lovCode"] = "Enum." + Type.FullName;
                dic["parentField"] = parentType.Name;
            }
        }

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
                if (!lf.View.IsNullOrEmpty()) dic["view"] = lf.View;
                break;
            case SearchField sf:
                if (sf.Multiple) dic["multiple"] = true;
                if (!sf.View.IsNullOrEmpty()) dic["view"] = sf.View;
                break;
            case FormField ff:
                if (!ff.GroupView.IsNullOrEmpty()) dic["groupView"] = ff.GroupView;
                if (!ff.ItemView.IsNullOrEmpty()) dic["itemView"] = ff.ItemView;
                // 统一渲染器名：单字段控件优先 ItemView，多字段合并控件用 GroupView
                if (!ff.ItemView.IsNullOrEmpty()) dic["view"] = ff.ItemView;
                else if (!ff.GroupView.IsNullOrEmpty()) dic["view"] = ff.GroupView;
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
        IsDefaultDisplayName = true;

        // 生成器把整段描述同时写入显示名与描述，保留整段源文，翻译后再拆分
        if (!DisplayName.IsNullOrEmpty() && !Description.IsNullOrEmpty() &&
            (Description.StartsWith(DisplayName + "。", StringComparison.Ordinal) ||
             Description.StartsWith(DisplayName + ". ", StringComparison.Ordinal)))
        {
            SourceDescription = Description;
        }

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
        IsDefaultDisplayName = true;
        if (dis.IsNullOrEmpty() && !des.IsNullOrEmpty())
        {
            // 只有描述特性时，整段描述先作为显示名，并保留源文供多语言翻译后拆分
            dis = des;
            des = null;
            SourceDescription = dis;
        }
        if (!dis.IsNullOrEmpty() && !des.IsNullOrEmpty() &&
            (des.StartsWith(dis + ". ", StringComparison.Ordinal) || des.StartsWith(dis + "。", StringComparison.Ordinal)))
        {
            // 生成器把整段描述同时写入显示名与描述，此时以整段描述为源文翻译后再拆分，词表只需登记一条
            SourceDescription = des;
        }
        if (!dis.IsNullOrEmpty() && des.IsNullOrEmpty())
        {
            // 中文句号与英文句点均可作为显示名与描述的分隔符
            var (name, desc) = SplitFirstSentence(dis);
            if (desc != null)
            {
                dis = name;
                des = desc;
            }
        }
        DisplayName = dis ?? property.Name;
        Description = des;

        var ra = property.GetCustomAttribute<ReadOnlyAttribute>();
        if (ra != null) ReadOnly = ra.IsReadOnly;
    }

    /// <summary>按当前语言本地化显示名、描述与分类</summary>
    /// <remarks>
    /// 字段元数据来自静态缓存，缓存中保存源文；每次请求克隆后调用本方法即可切换语言。
    /// 有整段描述时先翻译再按首句拆分，避免先拆分导致词表键不匹配。
    /// </remarks>
    public virtual void Localize()
    {
        // 本地化前记录是否为默认列标题，供 ListField.GetLinkName 判断
        var isDefault = Field == null || DisplayName == Field.DisplayName;
        IsDefaultDisplayName = isDefault;

        if (!SourceDescription.IsNullOrEmpty() && isDefault)
        {
            var translated = L.T(SourceDescription);
            if (!ReferenceEquals(translated, SourceDescription))
            {
                var (name, desc) = SplitFirstSentence(translated);
                DisplayName = name;
                Description = desc;
            }
            else
            {
                // 整段描述没有译文时，按源文拆分后分别翻译，允许只补显示名或说明
                var (name, desc) = SplitFirstSentence(SourceDescription);
                DisplayName = L.T(name);
                Description = desc == null ? null : L.T(desc);
            }
        }
        else if (Description.IsNullOrEmpty() && !DisplayName.IsNullOrEmpty())
        {
            var (name, desc) = SplitFirstSentence(L.T(DisplayName));
            DisplayName = name;
            Description = desc;
        }
        else
        {
            if (!DisplayName.IsNullOrEmpty()) DisplayName = L.T(DisplayName);
            if (!Description.IsNullOrEmpty()) Description = L.T(Description);
        }

        if (!Category.IsNullOrEmpty()) Category = L.T(Category);
    }

    /// <summary>克隆并按当前语言本地化字段集合</summary>
    /// <param name="fields">源字段集合，来自静态缓存，保留源文</param>
    /// <returns>可直接输出的字段副本</returns>
    public static IList<DataField> Localize(IList<DataField> fields)
    {
        if (fields == null) return [];

        var list = new List<DataField>(fields.Count);
        foreach (var item in fields)
        {
            var df = item.Clone();
            df.Localize();
            list.Add(df);
        }

        return list;
    }

    /// <summary>按句末标点拆分显示名和描述</summary>
    /// <param name="text">完整描述</param>
    /// <returns>显示名与剩余描述；没有第二句时描述为 null</returns>
    public static (String Display, String? Description) SplitFirstSentence(String text)
    {
        if (text.IsNullOrEmpty()) return (text, null);

        var index = FindSentenceEnd(text);
        if (index > 0)
        {
            var display = text[..index];
            var description = text[(index + 1)..].Trim();
            return (display, description.IsNullOrEmpty() ? null : description);
        }

        // 泰语等无句末标点的长描述，取首个空格前的短语作为显示名；短标签整体返回
        if (text.Length > 40)
        {
            var space = text.IndexOf(' ');
            if (space > 0) return (text[..space], text[(space + 1)..].Trim());
        }

        return (text, null);
    }

    private static Int32 FindSentenceEnd(String text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch is '。' or '！' or '？' or '।') return i;

            // 英文句点仅在后面是空白或结尾时拆分，避免小数和 URL
            if (ch is '.' or '!' or '?')
            {
                if (i + 1 >= text.Length || Char.IsWhiteSpace(text[i + 1])) return i;
            }
        }

        return -1;
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
    public static implicit operator DataField(FieldItem obj) => obj == null ? null : new DataField(obj);
    #endregion
}