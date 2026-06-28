using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>值集枚举值。枚举型值集的可选值列表</summary>
public partial class LovEnumItemModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>值集定义。关联LovDefinition</summary>
    public Int32 LovDefId { get; set; }

    /// <summary>枚举值。实际存储的值</summary>
    public String Value { get; set; }

    /// <summary>显示文本。枚举值对应的显示名称</summary>
    public String Label { get; set; }

    /// <summary>排序</summary>
    public Int32 Sort { get; set; }

    /// <summary>启用</summary>
    public Boolean Enabled { get; set; }

    /// <summary>扩展数据。JSON格式</summary>
    public String Extra { get; set; }

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
    public void Copy(LovEnumItemModel model)
    {
        Id = model.Id;
        LovDefId = model.LovDefId;
        Value = model.Value;
        Label = model.Label;
        Sort = model.Sort;
        Enabled = model.Enabled;
        Extra = model.Extra;
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
