using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>值集搜索字段。列表型值集的搜索条件字段定义</summary>
public partial class LovSearchFieldModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>值集定义。关联LovDefinition</summary>
    public Int32 LovDefId { get; set; }

    /// <summary>字段名。搜索参数字段名</summary>
    public String Field { get; set; }

    /// <summary>显示标题</summary>
    public String Title { get; set; }

    /// <summary>控件类型。input/select/lov/datepicker等</summary>
    public String ComponentType { get; set; }

    /// <summary>传参方式。BODY=请求体/QUERY=查询参数</summary>
    public String ParamType { get; set; }

    /// <summary>是否必填</summary>
    public Boolean Required { get; set; }

    /// <summary>默认值</summary>
    public String DefaultValue { get; set; }

    /// <summary>排序</summary>
    public Int32 Sort { get; set; }

    /// <summary>关联值集。该搜索字段渲染为此值集的选择控件</summary>
    public String RefLovCode { get; set; }

    /// <summary>创建用户</summary>
    public Int32 CreateUserID { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>更新用户</summary>
    public Int32 UpdateUserID { get; set; }

    /// <summary>更新地址</summary>
    public String UpdateIP { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>备注</summary>
    public String Remark { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(LovSearchFieldModel model)
    {
        Id = model.Id;
        LovDefId = model.LovDefId;
        Field = model.Field;
        Title = model.Title;
        ComponentType = model.ComponentType;
        ParamType = model.ParamType;
        Required = model.Required;
        DefaultValue = model.DefaultValue;
        Sort = model.Sort;
        RefLovCode = model.RefLovCode;
        CreateUserID = model.CreateUserID;
        CreateIP = model.CreateIP;
        CreateTime = model.CreateTime;
        UpdateUserID = model.UpdateUserID;
        UpdateIP = model.UpdateIP;
        UpdateTime = model.UpdateTime;
        Remark = model.Remark;
    }
    #endregion
}
