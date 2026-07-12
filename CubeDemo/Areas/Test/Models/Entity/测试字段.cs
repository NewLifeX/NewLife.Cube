using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using XCode;
using XCode.Cache;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace CubeDemo.Areas.Test;

/// <summary>字段类型全覆盖测试实体</summary>
[Serializable]
[DataObject]
[Description("字段类型全覆盖测试实体")]
[BindTable("测试字段", Description = "字段类型全覆盖测试实体", ConnName = "Cube", DbType = DatabaseType.SQLite)]
public partial class 测试字段 : I测试字段, IEntity<I测试字段>
{
    #region 属性
    private Int32 _Id;
    /// <summary>编号</summary>
    [DisplayName("编号")]
    [Description("编号")]
    [DataObjectField(true, true, false, 0)]
    [BindColumn("Id", "编号", "")]
    public Int32 Id { get => _Id; set { if (OnPropertyChanging("Id", value)) { _Id = value; OnPropertyChanged("Id"); } } }

    private String _ShortText;
    /// <summary>短文本。必填的短文本示例</summary>
    [Category("基本信息")]
    [DisplayName("短文本")]
    [Description("短文本。必填的短文本示例")]
    [DataObjectField(false, false, false, 50)]
    [BindColumn("ShortText", "短文本。必填的短文本示例", "")]
    public String ShortText { get => _ShortText; set { if (OnPropertyChanging("ShortText", value)) { _ShortText = value; OnPropertyChanged("ShortText"); } } }

    private String _LongText;
    /// <summary>长文本。大文本（Length>=300）渲染为多行文本域</summary>
    [Category("基本信息")]
    [DisplayName("长文本")]
    [Description("长文本。大文本（Length>=300）渲染为多行文本域")]
    [DataObjectField(false, false, true, 500)]
    [BindColumn("LongText", "长文本。大文本（Length>=300）渲染为多行文本域", "")]
    public String LongText { get => _LongText; set { if (OnPropertyChanging("LongText", value)) { _LongText = value; OnPropertyChanged("LongText"); } } }

    private Int32 _Int32Val;
    /// <summary>整数。Int32 数值</summary>
    [Category("基本信息")]
    [DisplayName("整数")]
    [Description("整数。Int32 数值")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("Int32Val", "整数。Int32 数值", "")]
    public Int32 Int32Val { get => _Int32Val; set { if (OnPropertyChanging("Int32Val", value)) { _Int32Val = value; OnPropertyChanged("Int32Val"); } } }

    private Int64 _Int64Val;
    /// <summary>长整数。Int64 数值</summary>
    [Category("基本信息")]
    [DisplayName("长整数")]
    [Description("长整数。Int64 数值")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("Int64Val", "长整数。Int64 数值", "")]
    public Int64 Int64Val { get => _Int64Val; set { if (OnPropertyChanging("Int64Val", value)) { _Int64Val = value; OnPropertyChanged("Int64Val"); } } }

    private Decimal _DecimalVal;
    /// <summary>金额。Decimal 数值，带精度</summary>
    [Category("基本信息")]
    [DisplayName("金额")]
    [Description("金额。Decimal 数值，带精度")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("DecimalVal", "金额。Decimal 数值，带精度", "", Precision = 18, Scale = 2)]
    public Decimal DecimalVal { get => _DecimalVal; set { if (OnPropertyChanging("DecimalVal", value)) { _DecimalVal = value; OnPropertyChanged("DecimalVal"); } } }

    private Double _DoubleVal;
    /// <summary>双精度。Double 数值</summary>
    [Category("基本信息")]
    [DisplayName("双精度")]
    [Description("双精度。Double 数值")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("DoubleVal", "双精度。Double 数值", "")]
    public Double DoubleVal { get => _DoubleVal; set { if (OnPropertyChanging("DoubleVal", value)) { _DoubleVal = value; OnPropertyChanged("DoubleVal"); } } }

    private Single _FloatVal;
    /// <summary>单精度。Single 数值</summary>
    [Category("基本信息")]
    [DisplayName("单精度")]
    [Description("单精度。Single 数值")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("FloatVal", "单精度。Single 数值", "")]
    public Single FloatVal { get => _FloatVal; set { if (OnPropertyChanging("FloatVal", value)) { _FloatVal = value; OnPropertyChanged("FloatVal"); } } }

    private Boolean _Enable;
    /// <summary>启用。布尔开关</summary>
    [Category("基本信息")]
    [DisplayName("启用")]
    [Description("启用。布尔开关")]
    [DataObjectField(false, false, false, 0)]
    [BindColumn("Enable", "启用。布尔开关", "")]
    public Boolean Enable { get => _Enable; set { if (OnPropertyChanging("Enable", value)) { _Enable = value; OnPropertyChanged("Enable"); } } }

    private DateTime _CreateTime;
    /// <summary>创建时间。日期时间</summary>
    [Category("基本信息")]
    [DisplayName("创建时间")]
    [Description("创建时间。日期时间")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("CreateTime", "创建时间。日期时间", "")]
    public DateTime CreateTime { get => _CreateTime; set { if (OnPropertyChanging("CreateTime", value)) { _CreateTime = value; OnPropertyChanged("CreateTime"); } } }

    private CubeDemo.Areas.Test.测试枚举 _Kind;
    /// <summary>种类。枚举类型，后端下发 lovCode 走 LOV 单选</summary>
    [Category("值集")]
    [DisplayName("种类")]
    [Description("种类。枚举类型，后端下发 lovCode 走 LOV 单选")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("Kind", "种类。枚举类型，后端下发 lovCode 走 LOV 单选", "")]
    public CubeDemo.Areas.Test.测试枚举 Kind { get => _Kind; set { if (OnPropertyChanging("Kind", value)) { _Kind = value; OnPropertyChanged("Kind"); } } }

    private Guid _GuidVal;
    /// <summary>唯一标识。Guid，非主键，渲染为只读文本</summary>
    [Category("高级控件")]
    [DisplayName("唯一标识")]
    [Description("唯一标识。Guid，非主键，渲染为只读文本")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("GuidVal", "唯一标识。Guid，非主键，渲染为只读文本", "")]
    public Guid GuidVal { get => _GuidVal; set { if (OnPropertyChanging("GuidVal", value)) { _GuidVal = value; OnPropertyChanged("GuidVal"); } } }

    private String _FileUrl;
    /// <summary>附件。文件上传，存储上传后返回的 URL</summary>
    [Category("高级控件")]
    [DisplayName("附件")]
    [Description("附件。文件上传，存储上传后返回的 URL")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("FileUrl", "附件。文件上传，存储上传后返回的 URL", "", ItemType = "file")]
    public String FileUrl { get => _FileUrl; set { if (OnPropertyChanging("FileUrl", value)) { _FileUrl = value; OnPropertyChanged("FileUrl"); } } }

    private String _Avatar;
    /// <summary>头像。图片上传，带预览，存储 URL</summary>
    [Category("高级控件")]
    [DisplayName("头像")]
    [Description("头像。图片上传，带预览，存储 URL")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("Avatar", "头像。图片上传，带预览，存储 URL", "", ItemType = "image")]
    public String Avatar { get => _Avatar; set { if (OnPropertyChanging("Avatar", value)) { _Avatar = value; OnPropertyChanged("Avatar"); } } }

    private String _JsonVal;
    /// <summary>Json。Json 编辑器，存储 Json 字符串</summary>
    [Category("高级控件")]
    [DisplayName("Json")]
    [Description("Json。Json 编辑器，存储 Json 字符串")]
    [DataObjectField(false, false, true, 2000)]
    [BindColumn("JsonVal", "Json。Json 编辑器，存储 Json 字符串", "", ItemType = "json")]
    public String JsonVal { get => _JsonVal; set { if (OnPropertyChanging("JsonVal", value)) { _JsonVal = value; OnPropertyChanged("JsonVal"); } } }

    private String _HtmlVal;
    /// <summary>富文本。html 富文本编辑器，存储 html 字符串</summary>
    [Category("高级控件")]
    [DisplayName("富文本")]
    [Description("富文本。html 富文本编辑器，存储 html 字符串")]
    [DataObjectField(false, false, true, 2000)]
    [BindColumn("HtmlVal", "富文本。html 富文本编辑器，存储 html 字符串", "", ItemType = "html")]
    public String HtmlVal { get => _HtmlVal; set { if (OnPropertyChanging("HtmlVal", value)) { _HtmlVal = value; OnPropertyChanged("HtmlVal"); } } }

    private String _MarkdownVal;
    /// <summary>Markdown。Markdown 富文本编辑器，存储 md 字符串</summary>
    [Category("高级控件")]
    [DisplayName("Markdown")]
    [Description("Markdown。Markdown 富文本编辑器，存储 md 字符串")]
    [DataObjectField(false, false, true, 2000)]
    [BindColumn("MarkdownVal", "Markdown。Markdown 富文本编辑器，存储 md 字符串", "", ItemType = "markdown")]
    public String MarkdownVal { get => _MarkdownVal; set { if (OnPropertyChanging("MarkdownVal", value)) { _MarkdownVal = value; OnPropertyChanged("MarkdownVal"); } } }

    private String _ColorVal;
    /// <summary>颜色。颜色选择器，存储色值字符串</summary>
    [Category("高级控件")]
    [DisplayName("颜色")]
    [Description("颜色。颜色选择器，存储色值字符串")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("ColorVal", "颜色。颜色选择器，存储色值字符串", "", ItemType = "color")]
    public String ColorVal { get => _ColorVal; set { if (OnPropertyChanging("ColorVal", value)) { _ColorVal = value; OnPropertyChanged("ColorVal"); } } }

    private String _IconVal;
    /// <summary>图标。图标选择器，存储图标名字符串</summary>
    [Category("高级控件")]
    [DisplayName("图标")]
    [Description("图标。图标选择器，存储图标名字符串")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("IconVal", "图标。图标选择器，存储图标名字符串", "", ItemType = "icon")]
    public String IconVal { get => _IconVal; set { if (OnPropertyChanging("IconVal", value)) { _IconVal = value; OnPropertyChanged("IconVal"); } } }

    private String _MailVal;
    /// <summary>邮箱。邮箱输入，带格式校验</summary>
    [Category("高级控件")]
    [DisplayName("邮箱")]
    [Description("邮箱。邮箱输入，带格式校验")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("MailVal", "邮箱。邮箱输入，带格式校验", "", ItemType = "mail")]
    public String MailVal { get => _MailVal; set { if (OnPropertyChanging("MailVal", value)) { _MailVal = value; OnPropertyChanged("MailVal"); } } }

    private String _MobileVal;
    /// <summary>手机。手机输入，带格式校验</summary>
    [Category("高级控件")]
    [DisplayName("手机")]
    [Description("手机。手机输入，带格式校验")]
    [DataObjectField(false, false, true, 50)]
    [BindColumn("MobileVal", "手机。手机输入，带格式校验", "", ItemType = "mobile")]
    public String MobileVal { get => _MobileVal; set { if (OnPropertyChanging("MobileVal", value)) { _MobileVal = value; OnPropertyChanged("MobileVal"); } } }

    private String _UrlVal;
    /// <summary>网址。网址输入，带格式校验</summary>
    [Category("高级控件")]
    [DisplayName("网址")]
    [Description("网址。网址输入，带格式校验")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("UrlVal", "网址。网址输入，带格式校验", "", ItemType = "url")]
    public String UrlVal { get => _UrlVal; set { if (OnPropertyChanging("UrlVal", value)) { _UrlVal = value; OnPropertyChanged("UrlVal"); } } }

    private Int32 _SingleVal;
    /// <summary>单选。singleSelect，后端下发 lovCode 走 LOV 单选</summary>
    [Category("值集")]
    [DisplayName("单选")]
    [Description("单选。singleSelect，后端下发 lovCode 走 LOV 单选")]
    [DataObjectField(false, false, true, 0)]
    [BindColumn("SingleVal", "单选。singleSelect，后端下发 lovCode 走 LOV 单选", "", ItemType = "singleSelect")]
    public Int32 SingleVal { get => _SingleVal; set { if (OnPropertyChanging("SingleVal", value)) { _SingleVal = value; OnPropertyChanged("SingleVal"); } } }

    private String _MultiVal;
    /// <summary>多选。multipleSelect，后端下发 lovCode 走 LOV 多选，存储逗号分隔的值</summary>
    [Category("值集")]
    [DisplayName("多选")]
    [Description("多选。multipleSelect，后端下发 lovCode 走 LOV 多选，存储逗号分隔的值")]
    [DataObjectField(false, false, true, 200)]
    [BindColumn("MultiVal", "多选。multipleSelect，后端下发 lovCode 走 LOV 多选，存储逗号分隔的值", "", ItemType = "multipleSelect")]
    public String MultiVal { get => _MultiVal; set { if (OnPropertyChanging("MultiVal", value)) { _MultiVal = value; OnPropertyChanged("MultiVal"); } } }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(I测试字段 model)
    {
        Id = model.Id;
        ShortText = model.ShortText;
        LongText = model.LongText;
        Int32Val = model.Int32Val;
        Int64Val = model.Int64Val;
        DecimalVal = model.DecimalVal;
        DoubleVal = model.DoubleVal;
        FloatVal = model.FloatVal;
        Enable = model.Enable;
        CreateTime = model.CreateTime;
        Kind = model.Kind;
        GuidVal = model.GuidVal;
        FileUrl = model.FileUrl;
        Avatar = model.Avatar;
        JsonVal = model.JsonVal;
        HtmlVal = model.HtmlVal;
        MarkdownVal = model.MarkdownVal;
        ColorVal = model.ColorVal;
        IconVal = model.IconVal;
        MailVal = model.MailVal;
        MobileVal = model.MobileVal;
        UrlVal = model.UrlVal;
        SingleVal = model.SingleVal;
        MultiVal = model.MultiVal;
    }
    #endregion

    #region 获取/设置 字段值
    /// <summary>获取/设置 字段值</summary>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public override Object this[String name]
    {
        get => name switch
        {
            "Id" => _Id,
            "ShortText" => _ShortText,
            "LongText" => _LongText,
            "Int32Val" => _Int32Val,
            "Int64Val" => _Int64Val,
            "DecimalVal" => _DecimalVal,
            "DoubleVal" => _DoubleVal,
            "FloatVal" => _FloatVal,
            "Enable" => _Enable,
            "CreateTime" => _CreateTime,
            "Kind" => _Kind,
            "GuidVal" => _GuidVal,
            "FileUrl" => _FileUrl,
            "Avatar" => _Avatar,
            "JsonVal" => _JsonVal,
            "HtmlVal" => _HtmlVal,
            "MarkdownVal" => _MarkdownVal,
            "ColorVal" => _ColorVal,
            "IconVal" => _IconVal,
            "MailVal" => _MailVal,
            "MobileVal" => _MobileVal,
            "UrlVal" => _UrlVal,
            "SingleVal" => _SingleVal,
            "MultiVal" => _MultiVal,
            _ => base[name]
        };
        set
        {
            switch (name)
            {
                case "Id": _Id = value.ToInt(); break;
                case "ShortText": _ShortText = Convert.ToString(value); break;
                case "LongText": _LongText = Convert.ToString(value); break;
                case "Int32Val": _Int32Val = value.ToInt(); break;
                case "Int64Val": _Int64Val = value.ToLong(); break;
                case "DecimalVal": _DecimalVal = Convert.ToDecimal(value); break;
                case "DoubleVal": _DoubleVal = value.ToDouble(); break;
                case "FloatVal": _FloatVal = Convert.ToSingle(value); break;
                case "Enable": _Enable = value.ToBoolean(); break;
                case "CreateTime": _CreateTime = value.ToDateTime(); break;
                case "Kind": _Kind = (CubeDemo.Areas.Test.测试枚举)value.ToInt(); break;
                case "GuidVal": _GuidVal = (Guid)value; break;
                case "FileUrl": _FileUrl = Convert.ToString(value); break;
                case "Avatar": _Avatar = Convert.ToString(value); break;
                case "JsonVal": _JsonVal = Convert.ToString(value); break;
                case "HtmlVal": _HtmlVal = Convert.ToString(value); break;
                case "MarkdownVal": _MarkdownVal = Convert.ToString(value); break;
                case "ColorVal": _ColorVal = Convert.ToString(value); break;
                case "IconVal": _IconVal = Convert.ToString(value); break;
                case "MailVal": _MailVal = Convert.ToString(value); break;
                case "MobileVal": _MobileVal = Convert.ToString(value); break;
                case "UrlVal": _UrlVal = Convert.ToString(value); break;
                case "SingleVal": _SingleVal = value.ToInt(); break;
                case "MultiVal": _MultiVal = Convert.ToString(value); break;
                default: base[name] = value; break;
            }
        }
    }
    #endregion

    #region 关联映射
    #endregion

    #region 扩展查询
    #endregion

    #region 高级查询
    /// <summary>高级查询</summary>
    /// <param name="kind">种类。枚举类型，后端下发 lovCode 走 LOV 单选</param>
    /// <param name="enable">启用。布尔开关</param>
    /// <param name="start">创建时间开始</param>
    /// <param name="end">创建时间结束</param>
    /// <param name="key">关键字</param>
    /// <param name="page">分页参数信息。可携带统计和数据权限扩展查询等信息</param>
    /// <returns>实体列表</returns>
    public static IList<测试字段> Search(CubeDemo.Areas.Test.测试枚举 kind, Boolean? enable, DateTime start, DateTime end, String key, PageParameter page)
    {
        var exp = new WhereExpression();

        if (kind >= 0) exp &= _.Kind == kind;
        if (enable != null) exp &= _.Enable == enable;
        exp &= _.CreateTime.Between(start, end);
        if (!key.IsNullOrEmpty()) exp &= SearchWhereByKeys(key);

        return FindAll(exp, page);
    }
    #endregion

    #region 字段名
    /// <summary>取得字段类型全覆盖测试实体字段信息的快捷方式</summary>
    public partial class _
    {
        /// <summary>编号</summary>
        public static readonly Field Id = FindByName("Id");

        /// <summary>短文本。必填的短文本示例</summary>
        public static readonly Field ShortText = FindByName("ShortText");

        /// <summary>长文本。大文本（Length>=300）渲染为多行文本域</summary>
        public static readonly Field LongText = FindByName("LongText");

        /// <summary>整数。Int32 数值</summary>
        public static readonly Field Int32Val = FindByName("Int32Val");

        /// <summary>长整数。Int64 数值</summary>
        public static readonly Field Int64Val = FindByName("Int64Val");

        /// <summary>金额。Decimal 数值，带精度</summary>
        public static readonly Field DecimalVal = FindByName("DecimalVal");

        /// <summary>双精度。Double 数值</summary>
        public static readonly Field DoubleVal = FindByName("DoubleVal");

        /// <summary>单精度。Single 数值</summary>
        public static readonly Field FloatVal = FindByName("FloatVal");

        /// <summary>启用。布尔开关</summary>
        public static readonly Field Enable = FindByName("Enable");

        /// <summary>创建时间。日期时间</summary>
        public static readonly Field CreateTime = FindByName("CreateTime");

        /// <summary>种类。枚举类型，后端下发 lovCode 走 LOV 单选</summary>
        public static readonly Field Kind = FindByName("Kind");

        /// <summary>唯一标识。Guid，非主键，渲染为只读文本</summary>
        public static readonly Field GuidVal = FindByName("GuidVal");

        /// <summary>附件。文件上传，存储上传后返回的 URL</summary>
        public static readonly Field FileUrl = FindByName("FileUrl");

        /// <summary>头像。图片上传，带预览，存储 URL</summary>
        public static readonly Field Avatar = FindByName("Avatar");

        /// <summary>Json。Json 编辑器，存储 Json 字符串</summary>
        public static readonly Field JsonVal = FindByName("JsonVal");

        /// <summary>富文本。html 富文本编辑器，存储 html 字符串</summary>
        public static readonly Field HtmlVal = FindByName("HtmlVal");

        /// <summary>Markdown。Markdown 富文本编辑器，存储 md 字符串</summary>
        public static readonly Field MarkdownVal = FindByName("MarkdownVal");

        /// <summary>颜色。颜色选择器，存储色值字符串</summary>
        public static readonly Field ColorVal = FindByName("ColorVal");

        /// <summary>图标。图标选择器，存储图标名字符串</summary>
        public static readonly Field IconVal = FindByName("IconVal");

        /// <summary>邮箱。邮箱输入，带格式校验</summary>
        public static readonly Field MailVal = FindByName("MailVal");

        /// <summary>手机。手机输入，带格式校验</summary>
        public static readonly Field MobileVal = FindByName("MobileVal");

        /// <summary>网址。网址输入，带格式校验</summary>
        public static readonly Field UrlVal = FindByName("UrlVal");

        /// <summary>单选。singleSelect，后端下发 lovCode 走 LOV 单选</summary>
        public static readonly Field SingleVal = FindByName("SingleVal");

        /// <summary>多选。multipleSelect，后端下发 lovCode 走 LOV 多选，存储逗号分隔的值</summary>
        public static readonly Field MultiVal = FindByName("MultiVal");

        static Field FindByName(String name) => Meta.Table.FindByName(name);
    }

    /// <summary>取得字段类型全覆盖测试实体字段名称的快捷方式</summary>
    public partial class __
    {
        /// <summary>编号</summary>
        public const String Id = "Id";

        /// <summary>短文本。必填的短文本示例</summary>
        public const String ShortText = "ShortText";

        /// <summary>长文本。大文本（Length>=300）渲染为多行文本域</summary>
        public const String LongText = "LongText";

        /// <summary>整数。Int32 数值</summary>
        public const String Int32Val = "Int32Val";

        /// <summary>长整数。Int64 数值</summary>
        public const String Int64Val = "Int64Val";

        /// <summary>金额。Decimal 数值，带精度</summary>
        public const String DecimalVal = "DecimalVal";

        /// <summary>双精度。Double 数值</summary>
        public const String DoubleVal = "DoubleVal";

        /// <summary>单精度。Single 数值</summary>
        public const String FloatVal = "FloatVal";

        /// <summary>启用。布尔开关</summary>
        public const String Enable = "Enable";

        /// <summary>创建时间。日期时间</summary>
        public const String CreateTime = "CreateTime";

        /// <summary>种类。枚举类型，后端下发 lovCode 走 LOV 单选</summary>
        public const String Kind = "Kind";

        /// <summary>唯一标识。Guid，非主键，渲染为只读文本</summary>
        public const String GuidVal = "GuidVal";

        /// <summary>附件。文件上传，存储上传后返回的 URL</summary>
        public const String FileUrl = "FileUrl";

        /// <summary>头像。图片上传，带预览，存储 URL</summary>
        public const String Avatar = "Avatar";

        /// <summary>Json。Json 编辑器，存储 Json 字符串</summary>
        public const String JsonVal = "JsonVal";

        /// <summary>富文本。html 富文本编辑器，存储 html 字符串</summary>
        public const String HtmlVal = "HtmlVal";

        /// <summary>Markdown。Markdown 富文本编辑器，存储 md 字符串</summary>
        public const String MarkdownVal = "MarkdownVal";

        /// <summary>颜色。颜色选择器，存储色值字符串</summary>
        public const String ColorVal = "ColorVal";

        /// <summary>图标。图标选择器，存储图标名字符串</summary>
        public const String IconVal = "IconVal";

        /// <summary>邮箱。邮箱输入，带格式校验</summary>
        public const String MailVal = "MailVal";

        /// <summary>手机。手机输入，带格式校验</summary>
        public const String MobileVal = "MobileVal";

        /// <summary>网址。网址输入，带格式校验</summary>
        public const String UrlVal = "UrlVal";

        /// <summary>单选。singleSelect，后端下发 lovCode 走 LOV 单选</summary>
        public const String SingleVal = "SingleVal";

        /// <summary>多选。multipleSelect，后端下发 lovCode 走 LOV 多选，存储逗号分隔的值</summary>
        public const String MultiVal = "MultiVal";
    }
    #endregion
}
