﻿using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Data;

namespace NewLife.Web;

/// <summary>分页器。包含分页排序参数，支持构造Url的功能</summary>
public class Pager : PageParameter, IExtend
{
    #region 名称
    /// <summary>名称类。用户可根据需要修改Url参数名</summary>
    public class __
    {
        /// <summary>排序字段</summary>
        public String Sort = "Sort";

        /// <summary>是否降序</summary>
        public String Desc = "Desc";

        /// <summary>页面索引</summary>
        public String PageIndex = "PageIndex";

        /// <summary>页面大小</summary>
        public String PageSize = "PageSize";
    }

    /// <summary>名称类。用户可根据需要修改Url参数名</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public static __ _ = new();
    #endregion

    #region 扩展属性
    /// <summary>参数集合</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public IDictionary<String, String> Params { get; set; } = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);

    /// <summary>分页链接模版。内部将会替换{链接}和{名称}</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public String PageUrlTemplate { get; set; } = "<a href=\"{链接}\">{名称}</a>";

    private static readonly PageParameter _def = new();

    /// <summary>默认参数。如果分页参数为默认参数，则不参与构造Url</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public PageParameter Default { get; set; } = _def;

    /// <summary>获取/设置 参数</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public String this[String key]
    {
        get
        {
            if (key.EqualIgnoreCase(_.Sort))
                return Sort;
            else if (key.EqualIgnoreCase(_.Desc))
                return Desc + "";
            else if (key.EqualIgnoreCase(_.PageIndex))
                return PageIndex + "";
            else if (key.EqualIgnoreCase(_.PageSize))
                return PageSize + "";

            // 为了布尔型取空字符串时得到null。var enable = p["enable"]?.ToBoolean()
            var v = Params[key];
            return v.IsNullOrEmpty() ? null : v;
        }
        set
        {
            if (key.EqualIgnoreCase(_.Sort))
                Sort = value;
            else if (key.EqualIgnoreCase(_.Desc))
                Desc = value.ToBoolean();
            else if (key.EqualIgnoreCase(_.PageIndex))
                PageIndex = value.ToInt();
            else if (key.EqualIgnoreCase(_.PageSize))
                PageSize = value.ToInt();
            else
                Params[key] = value;
        }
    }
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public Pager() { }

    /// <summary>用另一个分页参数实例化</summary>
    /// <param name="pm"></param>
    public Pager(PageParameter pm) : base(pm)
    {
        if (pm is Pager p)
        {
            foreach (var item in p.Params)
            {
                this[item.Key] = item.Value;
            }
        }
    }

    /// <summary>借助字典实例化</summary>
    /// <param name="parameters"></param>
    public Pager(IDictionary<String, String> parameters)
    {
        if (parameters.TryGetValue(nameof(PageIndex), out var str)) PageIndex = str.ToInt();
        if (parameters.TryGetValue(nameof(PageSize), out str)) PageSize = str.ToInt();
        if (parameters.TryGetValue(nameof(Desc), out str)) Desc = str.ToBoolean();
        if (parameters.TryGetValue(nameof(StartRow), out str)) StartRow = str.ToLong();
        if (parameters.TryGetValue(nameof(Sort), out str)) Sort = str;
        //if (parameters.TryGetValue(nameof(OrderBy), out str)) OrderBy = str;

        Params = parameters;
    }
    #endregion

    #region 方法
    /// <summary>获取基础Url，用于附加参数</summary>
    /// <param name="where">查询条件，不包含排序和分页</param>
    /// <param name="order">排序</param>
    /// <param name="page">分页</param>
    /// <param name="excludes">要排除的参数</param>
    /// <returns></returns>
    public virtual StringBuilder GetBaseUrl(Boolean where, Boolean order, Boolean page, String[] excludes = null)
    {
        var sb = new StringBuilder();
        var dic = Params;
        //// 过滤
        //dic = PagerHelper.FilterSpecialChar(dic);

        // 先构造基本条件，再排序到分页
        if (where)
        {
            var ex = new List<String>
            {
                _.Sort,
                _.Desc,
                _.PageIndex,
                _.PageSize,
            };
            if (excludes != null && excludes.Length > 0) ex.AddRange(excludes);
            sb.UrlParamsExcept(dic, ex.ToArray());
        }
        if (order)
        {
            sb.UrlParams(dic, _.Sort, _.Desc);
            if (!dic.ContainsKey(_.Sort) && !Sort.IsNullOrEmpty()) sb.UrlParam(_.Sort, Sort);
            if (!dic.ContainsKey(_.Desc) && Desc) sb.UrlParam(_.Desc, Desc);
        }
        if (page)
        {
            sb.UrlParams(dic, _.PageIndex, _.PageSize);
            if (!dic.ContainsKey(_.PageIndex) && PageIndex > 1) sb.UrlParam(_.PageIndex, PageIndex);
            if (!dic.ContainsKey(_.PageSize) && PageSize > 0 && PageSize != 20) sb.UrlParam(_.PageSize, PageSize);
        }

        return sb;
    }

    /// <summary>获取排序的Url</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual String GetSortUrl(String name)
    {
        // 首次访问该排序项，默认降序，重复访问取反
        var desc = true;
        if (Sort.EqualIgnoreCase(name)) desc = !Desc;

        var url = GetBaseUrl(true, false, true);
        // 默认排序不处理
        if (!name.EqualIgnoreCase(Default.Sort)) url.UrlParam(_.Sort, name);
        if (desc) url.UrlParam(_.Desc, true);
        return url.Length > 0 ? "?" + url.ToString() : "";
    }

    /// <summary>获取分页Url</summary>
    /// <param name="name"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public virtual String GetPageUrl(String name, Int64 index)
    {
        var url = GetBaseUrl(true, true, false);
        // 当前在非首页而要跳回首页，不写页面序号
        //if (!(PageIndex > 1 && index == 1)) url.UrlParam(_.PageIndex, index);
        // 还是写一下页面序号，因为页面Url本身就有，如果这里不写，有可能首页的href为空
        if (PageIndex != index) url.UrlParam(_.PageIndex, index);
        if (PageSize != Default.PageSize) url.UrlParam(_.PageSize, PageSize);

        var url2 = url.Length > 0 ? "?" + url.ToString() : "";

        var txt = PageUrlTemplate;
        txt = txt.Replace("{链接}", url2);
        txt = txt.Replace("{名称}", name);

        return txt;
    }

    /// <summary>获取分页Url</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public virtual String GetPage(String name)
    {
        if (PageIndex == 1)
        {
            if (name is "首页" or "上一页") return name;
        }
        if (PageIndex >= PageCount)
        {
            if (name is "尾页" or "下一页") return name;
        }

        if (PageIndex > 1)
        {
            if (name == "首页") return GetPageUrl("首页", 1);
            if (name == "上一页") return GetPageUrl("上一页", PageIndex - 1);
        }
        if (PageIndex < PageCount)
        {
            if (name == "尾页") return GetPageUrl("尾页", PageCount);
            if (name == "下一页") return GetPageUrl("下一页", PageIndex + 1);
        }

        return name;
    }

    /// <summary>从Url中解析参数</summary>
    /// <param name="url"></param>
    public virtual void Parse(String url)
    {
        if (url.IsNullOrEmpty()) return;

        var dic = url.SplitAsDictionary("=", "&", true);
        foreach (var item in dic)
        {
            this[item.Key] = item.Value;
        }
    }

    /// <summary>转为分页模型</summary>
    /// <returns></returns>
    public PageModel ToModel() => new() { PageIndex = PageIndex, PageSize = PageSize, TotalCount = TotalCount, LongTotalCount = TotalCount.ToString() };
    #endregion

    #region IExtend接口
    /// <summary>参数集合</summary>
    [XmlIgnore, ScriptIgnore]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IDictionary<String, Object> Items => Params.ToDictionary(e => e.Key, e => (Object)e.Value);

    Object IExtend.this[String key] { get => Params[key]; set => Params[key] = value == null ? null : value + ""; }
    #endregion
}

/// <summary>分页模型</summary>
public class PageModel
{
    /// <summary>获取 或 设置 页面索引。从1开始，默认1</summary>
    /// <remarks>如果设定了开始行，分页时将不再使用PageIndex</remarks>
    public virtual Int32 PageIndex { get; set; }

    /// <summary>获取 或 设置 页面大小。默认20，若为0表示不分页</summary>
    public virtual Int32 PageSize { get; set; }

    /// <summary>获取 或 设置 总记录数</summary>
    public virtual Int64 TotalCount { get; set; }

    /// <summary>获取 或 设置 总记录数，字符串类型</summary>
    public virtual String LongTotalCount { get; set; }

    ///// <summary>获取 页数</summary>
    //public virtual Int64 PageCount { get; set; }
}