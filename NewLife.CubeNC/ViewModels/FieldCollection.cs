using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using NewLife.Cube.ViewModels;
using NewLife.Reflection;
using XCode;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace NewLife.Cube;

/// <summary>分组可见委托</summary>
/// <param name="entity"></param>
/// <param name="group"></param>
/// <returns></returns>
public delegate Boolean GroupVisibleDelegate(IEntity entity, String group);

/// <summary>字段集合</summary>
public class FieldCollection : List<DataField>
{
    #region 属性
    /// <summary>类型</summary>
    public ViewKinds Kind { get; set; }

    /// <summary>工厂</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public IEntityFactory Factory { get; set; }

    /// <summary>需要隐藏的分组名</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public ICollection<String> HiddenGroups { get; } = new HashSet<String>();

    /// <summary>是否显示分组</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
    public GroupVisibleDelegate GroupVisible { get; set; }
    #endregion

    #region 构造
    /// <summary>实例化一个字段集合</summary>
    /// <param name="kind"></param>
    public FieldCollection(ViewKinds kind) => Kind = kind;

    /// <summary>使用工厂实例化一个字段集合</summary>
    /// <param name="factory"></param>
    /// <param name="kind"></param>
    public FieldCollection(IEntityFactory factory, ViewKinds kind)
    {
        Kind = kind;
        Factory = factory;
        //AddRange(Factory.Fields);

        if (factory != null)
        {
            foreach (var item in factory.Fields)
            {
                Add(item);
            }

            switch (kind)
            {
                case ViewKinds.List:
                    SetRelation(false);
                    break;
                case ViewKinds.Detail:
                    SetRelation(true);
                    break;
                case ViewKinds.AddForm:
                    SetRelation(true);
                    //RemoveCreateField();
                    RemoveUpdateField();
                    break;
                case ViewKinds.EditForm:
                    SetRelation(true);
                    break;
                case ViewKinds.Search:
                    // 有索引的字段
                    var fs = new List<FieldItem>();
                    var ds = new List<String>();
                    foreach (var idx in factory.Table.DataTable.Indexes)
                    {
                        foreach (var elm in idx.Columns)
                        {
                            var dc = factory.Table.DataTable.GetColumn(elm);
                            if (dc != null && !ds.Contains(dc.Name))
                            {
                                ds.Add(dc.Name);
                                fs.Add(factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(dc.Name)));
                            }
                        }
                    }

                    // 有映射的字段
                    foreach (var item in factory.Fields)
                    {
                        if (item.Map != null && !ds.Contains(item.Name))
                        {
                            ds.Add(item.Name);
                            fs.Add(item);
                        }
                    }

                    Clear();
                    foreach (var elm in fs)
                    {
                        AddField(elm.Name);
                    }
                    break;
                default:
                    SetRelation(false);
                    break;
            }
        }
    }
    #endregion

    #region 方法
    /// <summary>为指定字段创建数据字段，可以为空</summary>
    /// <param name="field"></param>
    /// <returns></returns>
    public DataField Create(FieldItem field)
    {
        DataField df = Kind switch
        {
            ViewKinds.List => new ListField(),
            ViewKinds.Detail or ViewKinds.AddForm or ViewKinds.EditForm => new FormField(),
            ViewKinds.Search => new SearchField(),
            _ => throw new NotImplementedException(),
        };
        //df.Sort = Count + 1;
        //df.Sort = Count == 0 ? 1 : (this[Count - 1].Sort + 1);
        if (field != null) df.Fill(field);

        return df;
    }

    /// <summary>为指定字段创建数据字段</summary>
    /// <param name="field"></param>
    /// <returns></returns>
    public DataField Add(FieldItem field)
    {
        var df = Create(field);

        Add(df);

        return df;
    }

    /// <summary>为指定属性创建数据字段</summary>
    /// <param name="property"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public DataField Add(PropertyInfo property)
    {
        DataField df = Kind switch
        {
            ViewKinds.List => new ListField(),
            ViewKinds.Detail or ViewKinds.AddForm or ViewKinds.EditForm => new FormField(),
            ViewKinds.Search => new SearchField(),
            _ => throw new NotImplementedException(),
        };

        if (property != null) df.Fill(property);

        Add(df);

        return df;
    }

    /// <summary>设置扩展关系</summary>
    /// <param name="isForm">是否表单使用</param>
    /// <returns></returns>
    public FieldCollection SetRelation(Boolean isForm)
    {
        var type = Factory.EntityType;
        // 扩展属性
        foreach (var pi in type.GetProperties())
        {
            // 处理带有Map特性的扩展属性
            var map = pi.GetCustomAttribute<MapAttribute>();
            if (map != null) Replace(map.Name, pi.Name);
        }

        if (!isForm)
        {
            // 长字段和密码字段不显示
            NoPass();
        }

        return this;
    }

    private void NoPass()
    {
        for (var i = Count - 1; i >= 0; i--)
        {
            var fi = this[i];
            if (fi.Type == typeof(String) && fi.MapField.IsNullOrEmpty())
            {
                if (fi.Length <= 0 || fi.Length > 1000 ||
                    fi.Name.EqualIgnoreCase("password", "pass", "pwd", "Secret"))
                {
                    RemoveAt(i);
                }
            }
        }
    }
    #endregion

    #region 添加删除替换
    /// <summary>查找指定字段</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Int32 FindIndex(String name) => FindIndex(e => e.Name.EqualIgnoreCase(name));

    /// <summary>从AllFields中添加字段，可以是扩展属性</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public FieldCollection AddField(String name)
    {
        var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
        if (fi != null) Add(fi);

        return this;
    }

    /// <summary>删除字段</summary>
    /// <param name="names">要删除的字段名称，支持*模糊匹配</param>
    /// <returns></returns>
    public FieldCollection RemoveField(params String[] names)
    {
        foreach (var item in names)
        {
            if (!item.IsNullOrEmpty())
            {
                // 模糊匹配
                if (item.Contains('*'))
                    RemoveAll(e => item.IsMatch(e.Name));
                else
                    RemoveAll(e => e.Name.EqualIgnoreCase(item));
            }
        }

        return this;
    }

    /// <summary>删除从指定字段开始的所有字段</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public FieldCollection RemoveBegin(String name)
    {
        if (!name.IsNullOrEmpty())
        {
            for (var i = 0; i < Count; i++)
            {
                var str = this[i].Name;
                if (name.Contains('*') && str.IsMatch(name) || str.EqualIgnoreCase(name))
                {
                    RemoveRange(i, Count - i);
                    break;
                }
            }
        }

        return this;
    }

    /// <summary>操作字段列表，把旧项换成新项</summary>
    /// <param name="oriName"></param>
    /// <param name="newName"></param>
    /// <returns></returns>
    public FieldCollection Replace(String oriName, String newName)
    {
        var idx = FindIndex(e => e.Name.EqualIgnoreCase(oriName));
        if (idx < 0) return this;

        var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(newName));
        if (fi == null) return this;

        // 如果本身就存在目标项，则删除
        var idx2 = FindIndex(e => e.Name.EqualIgnoreCase(fi.Name));
        if (idx2 >= 0) RemoveAt(idx2);

        this[idx] = Create(fi);

        return this;
    }
    #endregion

    #region 创建信息/更新信息
    /// <summary>设置是否显示创建信息</summary>
    /// <returns></returns>
    public FieldCollection RemoveCreateField()
    {
        RemoveAll(e => e.Name.EqualIgnoreCase("CreateUserID", "CreateUser", "CreateTime", "CreateIP"));

        return this;
    }

    /// <summary>设置是否显示更新信息</summary>
    /// <returns></returns>
    public FieldCollection RemoveUpdateField()
    {
        RemoveAll(e => e.Name.EqualIgnoreCase("UpdateUserID", "UpdateUser", "UpdateTime", "UpdateIP"));

        return this;
    }

    /// <summary>设置是否显示备注信息</summary>
    /// <returns></returns>
    public FieldCollection RemoveRemarkField()
    {
        RemoveAll(e => e.Name.EqualIgnoreCase("Remark", "Description"));

        return this;
    }
    #endregion

    #region 自定义字段
    /// <summary>添加定制字段，插入指定列前后</summary>
    /// <param name="name"></param>
    /// <param name="beforeName"></param>
    /// <param name="afterName"></param>
    /// <returns></returns>
    public DataField AddDataField(String name, String beforeName = null, String afterName = null)
    {
        if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));

        var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
        // 有可能fi为空，创建一个所有字段都为空的field
        var field = Create(fi);
        if (field.Name.IsNullOrEmpty()) field.Name = name;

        if (!beforeName.IsNullOrEmpty())
        {
            var idx = FindIndex(beforeName);
            if (idx >= 0)
                Insert(idx, field);
            else
                Add(field);
        }
        else if (!afterName.IsNullOrEmpty())
        {
            var idx = FindIndex(afterName);
            if (idx >= 0)
                Insert(idx + 1, field);
            else
                Add(field);
        }
        else
            Add(field);

        return field;
    }

    /// <summary>添加定制字段，插入指定列前后</summary>
    /// <param name="name"></param>
    /// <param name="beforeName"></param>
    /// <param name="afterName"></param>
    /// <returns></returns>
    public ListField AddListField(String name, String beforeName = null, String afterName = null) => AddDataField(name, beforeName, afterName) as ListField;

    /// <summary>获取指定名称的定制字段</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public DataField GetField(String name) => this.FirstOrDefault(e => name.EqualIgnoreCase(e.Name, e.MapField));
    #endregion

    #region 扩展参数
    public void Expand(IEntity entity, Object parameter, String prefix)
    {
        var fields = this;
        //fields.RemoveField("Argument");

        foreach (var pi in parameter.GetType().GetProperties(true))
        {
            // 添加字段，加个前缀，避免与实体字段冲突
            var ff = fields.Add(pi);
            ff.Name = prefix + ff.Name;
            ff.Category = pi.GetCustomAttribute<CategoryAttribute>()?.Category ?? "参数";

            // 数组转为字符串
            var v = pi.GetValue(parameter);
            if (v is IList list)
            {
                v = list.Join(",");
                ff.Type = v.GetType();
            }

            // 把参数值设置到实体对象的扩展属性里面
            entity.SetItem(ff.Name, v);
        }
    }

    public static Boolean ReadForm(Object parameter, IFormCollection form, String prefix)
    {
        var flag = false;
        foreach (var pi in parameter.GetType().GetProperties(true))
        {
            // 从Request里面获取参数值
            var name = prefix + pi.Name;
            if (!form.ContainsKey(name)) continue;

            var value = form[name].FirstOrDefault();
            flag = true;

            Object v = null;
            if (pi.PropertyType.As<IList>())
            {
                var elmType = pi.PropertyType.GetElementTypeEx();
                var ss = value.Split(",");
                var arr = Array.CreateInstance(elmType, ss.Length);
                for (var i = 0; i < arr.Length; i++)
                {
                    arr.SetValue(ss[i].ChangeType(elmType), i);
                }

                v = arr;
            }
            else
            {
                v = value.ChangeType(pi.PropertyType);
            }

            // 设置到参数对象里面
            parameter.SetValue(pi, v);
        }

        return flag;
    }
    #endregion

    #region 分组
    /// <summary>按类别分组获取字段列表</summary>
    /// <param name="entity">实体对象</param>
    /// <returns></returns>
    public IDictionary<String, IList<DataField>> GroupByCategory(IEntity entity)
    {
        var dic = new Dictionary<String, IList<DataField>>();
        var groupFields = this.GroupBy(e => e.Category + "").ToList();
        foreach (var item in groupFields)
        {
            var key = item.Key.IsNullOrEmpty() ? "默认" : item.Key;
            if (HiddenGroups.Contains(key)) continue;

            if (GroupVisible != null && !GroupVisible(entity, key)) continue;

            if (!dic.TryGetValue(key, out var list))
                dic[key] = list = new List<DataField>();

            //(list as List<DataField>).AddRange(item);
            foreach (var elm in item)
            {
                list.Add(elm);
            }
        }

        return dic;
    }
    #endregion

    #region 克隆
    /// <summary>克隆集合</summary>
    /// <returns></returns>
    public FieldCollection Clone()
    {
        var fs = new FieldCollection(Kind);

        foreach (var item in this)
        {
            fs.Add(item.Clone());
        }

        return fs;
    }
    #endregion
}