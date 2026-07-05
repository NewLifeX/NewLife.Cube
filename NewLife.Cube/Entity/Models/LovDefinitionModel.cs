using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>值集定义。值和列表的值集定义，支持枚举型和列表型</summary>
public partial class LovDefinitionModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>值集编码。带前缀的完全限定编码，如 Enum.SmartMES.Data.ProcessCard.ProcessCardStatus / List.User</summary>
    public String LovCode { get; set; }

    /// <summary>显示名称</summary>
    public String Name { get; set; }

    /// <summary>值集类型。ENUM=枚举型/LIST=列表型，须与LovCode前缀一致</summary>
    public String Type { get; set; }

    /// <summary>值字段。列表型的值字段名</summary>
    public String ValueField { get; set; }

    /// <summary>标签字段。列表型的标签字段名</summary>
    public String LabelField { get; set; }

    /// <summary>来源。AUTO=自动注册/MANUAL=手工管理</summary>
    public String Source { get; set; }

    /// <summary>启用</summary>
    public Boolean Enabled { get; set; }

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
    public void Copy(LovDefinitionModel model)
    {
        Id = model.Id;
        LovCode = model.LovCode;
        Name = model.Name;
        Type = model.Type;
        ValueField = model.ValueField;
        LabelField = model.LabelField;
        Source = model.Source;
        Enabled = model.Enabled;
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
