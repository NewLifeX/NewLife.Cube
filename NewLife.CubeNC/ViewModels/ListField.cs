using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using NewLife.Data;
using XCode.Configuration;

namespace NewLife.Cube.ViewModels;

/// <summary>获取数据委托</summary>
/// <param name="entity"></param>
/// <returns></returns>
public delegate String GetValueDelegate(Object entity);

/// <summary>列表字段</summary>
public class ListField : DataField
{
    #region 属性
    /// <summary>单元格文字</summary>
    public String Text { get; set; }

    /// <summary>单元格标题。数据单元格上的提示文字</summary>
    public String Title { get; set; }

    /// <summary>单元格链接。数据单元格的链接</summary>
    public String Url { get; set; }

    ///// <summary>单元格图标。数据单元格前端显示时的图标或图片</summary>
    //public String Icon { get; set; }

    /// <summary>链接目标。_blank/_self/_parent/_top</summary>
    public String Target { get; set; }

    /// <summary>头部文字</summary>
    public String Header { get; set; }

    /// <summary>头部标题。数据移上去后显示的文字</summary>
    public String HeaderTitle { get; set; }

    ///// <summary>头部链接。一般是排序</summary>
    //public String HeaderUrl { get; set; }

    /// <summary>数据动作。设为action时走ajax请求</summary>
    public String DataAction { get; set; }

    /// <summary>获取数据委托。可用于自定义列表页单元格数值的显示</summary>
    [XmlIgnore, IgnoreDataMember]
    public GetValueDelegate GetValue { get; set; }
    #endregion

    #region 方法
    /// <summary>填充</summary>
    /// <param name="field"></param>
    public override void Fill(FieldItem field)
    {
        base.Fill(field);

        Header = field.DisplayName;
    }

    /// <summary>克隆</summary>
    /// <returns></returns>
    public override DataField Clone()
    {
        var df = base.Clone();
        if (df is ListField field)
        {
            field.Text = Text;
            field.Title = Title;
            field.Url = Url;
            //field.Icon = Icon;
            field.Target = Target;
            field.Header = Header;
            field.HeaderTitle = HeaderTitle;
            field.DataAction = DataAction;
            field.GetValue = GetValue;
        }

        return df;
    }
    #endregion

    #region 数据格式化
    private static readonly Regex _reg = new(@"{(\w+)}", RegexOptions.Compiled);

    private static String Replace(String input, IExtend data) => _reg.Replace(input, m => data[m.Groups[1].Value + ""] + "");

    /// <summary>针对指定实体对象计算DisplayName，替换其中变量</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public virtual String GetDisplayName(IExtend data)
    {
        if (DisplayName.IsNullOrEmpty()) return null;

        return Replace(DisplayName, data);
    }

    /// <summary>针对指定实体对象计算链接名，替换其中变量</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public virtual String GetLinkName(IExtend data)
    {
        // 如果设置了单元格文字，则优先使用。Text>Entity[name]>DisplayName
        var txt = Text;
        if (txt.IsNullOrEmpty())
        {
            // 在数据列中，实体对象取属性值优先于显示名
            if (Field != null && DisplayName == Field.DisplayName) return data[Name] as String;

            txt = DisplayName;
        }

        if (txt.IsNullOrEmpty()) return null;

        //return _reg.Replace(txt, m => data[m.Groups[1].Value + ""] + "");
        return Replace(txt, data);
    }

    /// <summary>针对指定实体对象计算url，替换其中变量</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public virtual String GetUrl(IExtend data)
    {
        var svc = GetService<IUrlExtend>();
        if (svc != null) return svc.Resolve(this, data);

        if (Url.IsNullOrEmpty()) return null;

        //return _reg.Replace(Url, m => data[m.Groups[1].Value + ""] + "");
        return Replace(Url, data);
    }

    /// <summary>针对指定实体对象计算title，替换其中变量</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public virtual String GetTitle(IExtend data)
    {
        if (Title.IsNullOrEmpty()) return null;

        //return _reg.Replace(Title, m => data[m.Groups[1].Value + ""] + "");
        return Replace(Title, data);
    }
    #endregion
}