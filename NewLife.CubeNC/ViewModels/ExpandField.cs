using System.Collections;
using System.ComponentModel;
using System.Reflection;
using NewLife.Reflection;
using XCode;

namespace NewLife.Cube.ViewModels;

/// <summary>扩展字段。在表单页中，把一个Json/Xml字段扩展为多个字段进行展示和编辑</summary>
public class ExpandField
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>获取扩展字段委托。当前字段所表示的对象，各属性作为表单字段展开</summary>
    public Func<Object, Object> Decode { get; set; }

    /// <summary>编码扩展字段。一般是ToJson/ToXml</summary>
    public Func<Object, Object> Encode { get; set; }

    /// <summary>保留扩展字段。默认false，字段被扩展以后，表单上就不再出现原字段</summary>
    public Boolean Retain { get; set; }

    /// <summary>前缀</summary>
    public String Prefix { get; set; }

    /// <summary>分类</summary>
    public String Category { get; set; }
    #endregion

    #region 扩展参数
    /// <summary>当前字段集合加入目标对象作为扩展字段，用于动态表单</summary>
    /// <param name="entity"></param>
    /// <param name="parameter"></param>
    public FieldCollection Expand(IEntity entity, Object parameter)
    {
        var fields = new FieldCollection(ViewKinds.EditForm);

        foreach (var pi in parameter.GetType().GetProperties(true))
        {
            // 添加字段，加个前缀，避免与实体字段冲突
            var ff = fields.Add(pi);
            ff.Name = Prefix + ff.Name;
            ff.Category = Category;
            if (ff.Category.IsNullOrEmpty()) ff.Category = pi.GetCustomAttribute<CategoryAttribute>()?.Category;
            if (ff.Category.IsNullOrEmpty()) ff.Category = "参数";

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

        return fields;
    }

    /// <summary>从表单读取数据到扩展字段的目标对象，稍候序列化并写入扩展字段</summary>
    /// <param name="parameter"></param>
    /// <param name="form"></param>
    /// <returns></returns>
    public Boolean ReadForm(Object parameter, IFormCollection form)
    {
        var flag = false;
        foreach (var pi in parameter.GetType().GetProperties(true))
        {
            // 从Request里面获取参数值
            var name = Prefix + pi.Name;
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
}
