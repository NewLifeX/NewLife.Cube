using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace CubeDemo.Areas.Test;

/// <summary>字段类型全覆盖测试实体</summary>
public partial interface I测试字段
{
    #region 属性
    /// <summary>编号</summary>
    Int32 Id { get; set; }

    /// <summary>短文本。必填的短文本示例</summary>
    String ShortText { get; set; }

    /// <summary>长文本。大文本（Length>=300）渲染为多行文本域</summary>
    String LongText { get; set; }

    /// <summary>整数。Int32 数值</summary>
    Int32 Int32Val { get; set; }

    /// <summary>长整数。Int64 数值</summary>
    Int64 Int64Val { get; set; }

    /// <summary>金额。Decimal 数值，带精度</summary>
    Decimal DecimalVal { get; set; }

    /// <summary>双精度。Double 数值</summary>
    Double DoubleVal { get; set; }

    /// <summary>单精度。Single 数值</summary>
    Single FloatVal { get; set; }

    /// <summary>启用。布尔开关</summary>
    Boolean Enable { get; set; }

    /// <summary>创建时间。日期时间</summary>
    DateTime CreateTime { get; set; }

    /// <summary>种类。枚举类型，后端下发 lovCode 走 LOV 单选</summary>
    CubeDemo.Areas.Test.测试枚举 Kind { get; set; }

    /// <summary>唯一标识。Guid，非主键，渲染为只读文本</summary>
    Guid GuidVal { get; set; }

    /// <summary>附件。文件上传，存储上传后返回的 URL</summary>
    String FileUrl { get; set; }

    /// <summary>头像。图片上传，带预览，存储 URL</summary>
    String Avatar { get; set; }

    /// <summary>Json。Json 编辑器，存储 Json 字符串</summary>
    String JsonVal { get; set; }

    /// <summary>富文本。html 富文本编辑器，存储 html 字符串</summary>
    String HtmlVal { get; set; }

    /// <summary>Markdown。Markdown 富文本编辑器，存储 md 字符串</summary>
    String MarkdownVal { get; set; }

    /// <summary>颜色。颜色选择器，存储色值字符串</summary>
    String ColorVal { get; set; }

    /// <summary>图标。图标选择器，存储图标名字符串</summary>
    String IconVal { get; set; }

    /// <summary>邮箱。邮箱输入，带格式校验</summary>
    String MailVal { get; set; }

    /// <summary>手机。手机输入，带格式校验</summary>
    String MobileVal { get; set; }

    /// <summary>网址。网址输入，带格式校验</summary>
    String UrlVal { get; set; }

    /// <summary>单选。singleSelect，后端下发 lovCode 走 LOV 单选</summary>
    Int32 SingleVal { get; set; }

    /// <summary>多选。multipleSelect，后端下发 lovCode 走 LOV 多选，存储逗号分隔的值</summary>
    String MultiVal { get; set; }
    #endregion
}
