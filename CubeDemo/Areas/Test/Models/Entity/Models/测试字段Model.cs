using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using NewLife.Reflection;

namespace CubeDemo.Areas.Test;

/// <summary>字段类型全覆盖测试实体</summary>
public partial class 测试字段Model : IModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>短文本。必填的短文本示例</summary>
    public String ShortText { get; set; }

    /// <summary>长文本。大文本（Length>=300）渲染为多行文本域</summary>
    public String LongText { get; set; }

    /// <summary>整数。Int32 数值</summary>
    public Int32 Int32Val { get; set; }

    /// <summary>长整数。Int64 数值</summary>
    public Int64 Int64Val { get; set; }

    /// <summary>金额。Decimal 数值，带精度</summary>
    public Decimal DecimalVal { get; set; }

    /// <summary>双精度。Double 数值</summary>
    public Double DoubleVal { get; set; }

    /// <summary>单精度。Single 数值</summary>
    public Single FloatVal { get; set; }

    /// <summary>启用。布尔开关</summary>
    public Boolean Enable { get; set; }

    /// <summary>创建时间。日期时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>种类。枚举类型，后端下发 lovCode 走 LOV 单选</summary>
    public CubeDemo.Areas.Test.测试枚举 Kind { get; set; }

    /// <summary>唯一标识。Guid，非主键，渲染为只读文本</summary>
    public Guid GuidVal { get; set; }

    /// <summary>附件。文件上传，存储上传后返回的 URL</summary>
    public String FileUrl { get; set; }

    /// <summary>头像。图片上传，带预览，存储 URL</summary>
    public String Avatar { get; set; }

    /// <summary>Json。Json 编辑器，存储 Json 字符串</summary>
    public String JsonVal { get; set; }

    /// <summary>富文本。html 富文本编辑器，存储 html 字符串</summary>
    public String HtmlVal { get; set; }

    /// <summary>Markdown。Markdown 富文本编辑器，存储 md 字符串</summary>
    public String MarkdownVal { get; set; }

    /// <summary>颜色。颜色选择器，存储色值字符串</summary>
    public String ColorVal { get; set; }

    /// <summary>图标。图标选择器，存储图标名字符串</summary>
    public String IconVal { get; set; }

    /// <summary>邮箱。邮箱输入，带格式校验</summary>
    public String MailVal { get; set; }

    /// <summary>手机。手机输入，带格式校验</summary>
    public String MobileVal { get; set; }

    /// <summary>网址。网址输入，带格式校验</summary>
    public String UrlVal { get; set; }

    /// <summary>单选。singleSelect，后端下发 lovCode 走 LOV 单选</summary>
    public Int32 SingleVal { get; set; }

    /// <summary>多选。multipleSelect，后端下发 lovCode 走 LOV 多选，存储逗号分隔的值</summary>
    public String MultiVal { get; set; }
    #endregion

    #region 获取/设置 字段值
    /// <summary>获取/设置 字段值</summary>
    /// <param name="name">字段名</param>
    /// <returns></returns>
    public virtual Object this[String name]
    {
        get
        {
            return name switch
            {
                "Id" => Id,
                "ShortText" => ShortText,
                "LongText" => LongText,
                "Int32Val" => Int32Val,
                "Int64Val" => Int64Val,
                "DecimalVal" => DecimalVal,
                "DoubleVal" => DoubleVal,
                "FloatVal" => FloatVal,
                "Enable" => Enable,
                "CreateTime" => CreateTime,
                "Kind" => Kind,
                "GuidVal" => GuidVal,
                "FileUrl" => FileUrl,
                "Avatar" => Avatar,
                "JsonVal" => JsonVal,
                "HtmlVal" => HtmlVal,
                "MarkdownVal" => MarkdownVal,
                "ColorVal" => ColorVal,
                "IconVal" => IconVal,
                "MailVal" => MailVal,
                "MobileVal" => MobileVal,
                "UrlVal" => UrlVal,
                "SingleVal" => SingleVal,
                "MultiVal" => MultiVal,
                _ => this.GetValue(name, false),
            };
        }
        set
        {
            switch (name)
            {
                case "Id": Id = value.ToInt(); break;
                case "ShortText": ShortText = Convert.ToString(value); break;
                case "LongText": LongText = Convert.ToString(value); break;
                case "Int32Val": Int32Val = value.ToInt(); break;
                case "Int64Val": Int64Val = value.ToLong(); break;
                case "DecimalVal": DecimalVal = Convert.ToDecimal(value); break;
                case "DoubleVal": DoubleVal = value.ToDouble(); break;
                case "FloatVal": FloatVal = Convert.ToSingle(value); break;
                case "Enable": Enable = value.ToBoolean(); break;
                case "CreateTime": CreateTime = value.ToDateTime(); break;
                case "Kind": Kind = (CubeDemo.Areas.Test.测试枚举)value; break;
                case "GuidVal": GuidVal = (Guid)value; break;
                case "FileUrl": FileUrl = Convert.ToString(value); break;
                case "Avatar": Avatar = Convert.ToString(value); break;
                case "JsonVal": JsonVal = Convert.ToString(value); break;
                case "HtmlVal": HtmlVal = Convert.ToString(value); break;
                case "MarkdownVal": MarkdownVal = Convert.ToString(value); break;
                case "ColorVal": ColorVal = Convert.ToString(value); break;
                case "IconVal": IconVal = Convert.ToString(value); break;
                case "MailVal": MailVal = Convert.ToString(value); break;
                case "MobileVal": MobileVal = Convert.ToString(value); break;
                case "UrlVal": UrlVal = Convert.ToString(value); break;
                case "SingleVal": SingleVal = value.ToInt(); break;
                case "MultiVal": MultiVal = Convert.ToString(value); break;
                default: this.SetValue(name, value); break;
            }
        }
    }
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
}
