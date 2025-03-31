using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>模型表。实体表模型</summary>
public partial class ModelTableModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>分类</summary>
    public String Category { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>显示名</summary>
    public String DisplayName { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>路径。全路径</summary>
    public String Url { get; set; }

    /// <summary>控制器。控制器类型全名</summary>
    public String Controller { get; set; }

    /// <summary>表名</summary>
    public String TableName { get; set; }

    /// <summary>连接名</summary>
    public String ConnName { get; set; }

    /// <summary>仅插入。日志型数据</summary>
    public Boolean InsertOnly { get; set; }

    /// <summary>说明</summary>
    public String Description { get; set; }

    /// <summary>创建者</summary>
    public Int32 CreateUserId { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>更新者</summary>
    public Int32 UpdateUserId { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>更新地址</summary>
    public String UpdateIP { get; set; }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(ModelTableModel model)
    {
        Id = model.Id;
        Category = model.Category;
        Name = model.Name;
        DisplayName = model.DisplayName;
        Enable = model.Enable;
        Url = model.Url;
        Controller = model.Controller;
        TableName = model.TableName;
        ConnName = model.ConnName;
        InsertOnly = model.InsertOnly;
        Description = model.Description;
        CreateUserId = model.CreateUserId;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserId = model.UpdateUserId;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
    }
    #endregion
}
