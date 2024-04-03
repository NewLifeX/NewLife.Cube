using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Reflection;
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

    /// <summary>
    /// 链接目标。参考：TargetEnum 
    /// _blank/_self/_parent/_top
    /// 默认：null,会根据皮肤自动判断打开方式，layui:在框架页多标签打开，ace:在当前页面进行跳转
    /// </summary>
    public String Target { get; set; }

    /// <summary>头部文字</summary>
    public String Header { get; set; }

    /// <summary>头部标题。数据移上去后显示的文字</summary>
    public String HeaderTitle { get; set; }

    /// <summary>文本对齐方式</summary>
    public TextAligns TextAlign { get; set; }

    /// <summary>单元格样式</summary>
    public String Class { get; set; }

    ///// <summary>头部链接。一般是排序</summary>
    //public String HeaderUrl { get; set; }

    /// <summary>
    /// 数据动作。参考：DataAction
    /// 默认：null 作为普通url操作；action 走ajax请求</summary>
    public String DataAction { get; set; }

    /// <summary>获取数据委托。可用于自定义列表页单元格数值的显示</summary>
    [XmlIgnore, IgnoreDataMember, JsonIgnore]
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

    ///// <summary>克隆</summary>
    ///// <returns></returns>
    //public override DataField Clone()
    //{
    //    var df = base.Clone();
    //    if (df is ListField field)
    //    {
    //        field.Text = Text;
    //        field.Title = Title;
    //        field.Url = Url;
    //        //field.Icon = Icon;
    //        field.Target = Target;
    //        field.Header = Header;
    //        field.HeaderTitle = HeaderTitle;
    //        field.DataAction = DataAction;
    //        field.GetValue = GetValue;
    //    }

    //    return df;
    //}
    #endregion

    #region 数据格式化
    private static readonly Regex _reg = new(@"{(\w+(?:\.\w+)*)}", RegexOptions.Compiled);
    private static readonly Regex _reg2 = new(@"{page:(\w+)}", RegexOptions.Compiled);

    private static String Replace(String input, IModel data)
    {
        return _reg.Replace(input, m =>
        {
            //var val = data[m.Groups[1].Value + ""];
            // 循环解析多层数值
            var names = m.Groups[1].Value.Split('.');
            var val = data[names[0]];
            for (var i = 1; i < names.Length && val != null; i++)
            {
                if (val is IModel model)
                    val = model[names[i]] ?? val.GetValue(names[i]);
                else
                    val = val.GetValue(names[i]);
            }

            // 特殊处理时间
            if (val is DateTime dt) return dt == dt.Date ? dt.ToString("yyyy-MM-dd") : dt.ToFullString();

            return val + "";
        });
    }

    private static String Replace(String input, IExtend data)
    {
        return _reg2.Replace(input, m =>
        {
            var name = m.Groups[1].Value;
            var val = data[name];

            // 特殊处理时间
            if (val is DateTime dt) return dt == dt.Date ? dt.ToString("yyyy-MM-dd") : dt.ToFullString();

            return val + "";
        });
    }

    /// <summary>针对指定实体对象计算DisplayName，替换其中变量</summary>
    /// <param name="data"></param>
    /// <param name="page"></param>
    /// <returns></returns>
    public virtual String GetDisplayName(IModel data, IExtend page = null)
    {
        if (DisplayName.IsNullOrEmpty()) return null;

        var rs = Replace(DisplayName, data);
        if (page != null && !rs.IsNullOrEmpty()) rs = Replace(rs, page);

        return rs;
    }

    /// <summary>针对指定实体对象计算单元格文字，替换其中变量</summary>
    /// <param name="data"></param>
    /// <param name="page"></param>
    /// <returns></returns>
    public virtual String GetText(IModel data, IExtend page = null)
    {
        var txt = Text;
        if (txt.IsNullOrEmpty()) return null;

        //return _reg.Replace(txt, m => data[m.Groups[1].Value + ""] + "");
        var rs = Replace(txt, data);
        if (page != null && !rs.IsNullOrEmpty()) rs = Replace(rs, page);

        return rs;
    }

    /// <summary>针对指定实体对象计算链接名，替换其中变量</summary>
    /// <param name="data"></param>
    /// <param name="page"></param>
    /// <returns></returns>
    public virtual String GetLineName(IModel data, IExtend page = null)
    {
        // 如果设置了单元格文字，则优先使用。Text>Entity[name]>DisplayName
        var txt = Text;
        if (txt.IsNullOrEmpty())
        {
            // 在数据列中，实体对象取属性值优先于显示名
            if (Field != null && DisplayName == Field.DisplayName) return data[Name] + "";

            txt = DisplayName;
        }

        if (txt.IsNullOrEmpty()) return null;

        //return _reg.Replace(txt, m => data[m.Groups[1].Value + ""] + "");
        var rs = Replace(txt, data);
        if (page != null && !rs.IsNullOrEmpty()) rs = Replace(rs, page);

        return rs;
    }

    /// <summary>针对指定实体对象计算超链接HTML，替换其中变量，支持ILinkExtend</summary>
    /// <param name="data"></param>
    /// <param name="page"></param>
    /// <returns></returns>
    public virtual String GetLink(IModel data, IExtend page = null)
    {
        var svc = GetService<ILinkExtend>();
        if (svc != null) return svc.Resolve(this, data);

        var linkName = GetLineName(data, page);
        //if (linkName.IsNullOrEmpty()) linkName = GetDisplayName(data);

        var url = GetUrl(data, page);
        if (url.IsNullOrEmpty()) return null;

        var title = GetTitle(data, page);
        var target = Target;
        var action = DataAction;

        var sb = Pool.StringBuilder.Get();
        sb.AppendFormat("<a href=\"{0}\"", url);
        if (!target.IsNullOrEmpty()) sb.AppendFormat(" target=\"{0}\"", target);
        if (!action.IsNullOrEmpty()) sb.AppendFormat(" data-action=\"{0}\"", action);
        if (!title.IsNullOrEmpty()) sb.AppendFormat(" title=\"{0}\"", HttpUtility.HtmlEncode(title));
        sb.Append('>');
        sb.Append(linkName);
        sb.Append("</a>");

        var link = sb.Put(true);

        return Replace(link, data);
    }

    /// <summary>针对指定实体对象计算url，替换其中变量</summary>
    /// <param name="data"></param>
    /// <param name="page"></param>
    /// <returns></returns>
    public virtual String GetUrl(IModel data, IExtend page = null)
    {
        var svc = GetService<IUrlExtend>();
        if (svc != null) return svc.Resolve(this, data);

        if (Url.IsNullOrEmpty()) return null;

        //return _reg.Replace(Url, m => data[m.Groups[1].Value + ""] + "");
        var rs = Replace(Url, data);
        if (page != null && !rs.IsNullOrEmpty()) rs = Replace(rs, page);

        return rs;
    }

    /// <summary>针对指定实体对象计算title，替换其中变量</summary>
    /// <param name="data"></param>
    /// <param name="page"></param>
    /// <returns></returns>
    public virtual String GetTitle(IModel data, IExtend page = null)
    {
        if (Title.IsNullOrEmpty()) return null;

        //return _reg.Replace(Title, m => data[m.Groups[1].Value + ""] + "");
        var rs = Replace(Title, data);
        if (page != null && !rs.IsNullOrEmpty()) rs = Replace(rs, page);

        return rs;
    }
    #endregion
}