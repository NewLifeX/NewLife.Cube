using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.Cube.Entity;

/// <summary>值集列表配置。列表型值集的数据源配置</summary>
public partial class LovListConfigModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>值集定义。关联LovDefinition</summary>
    public Int32 LovDefId { get; set; }

    /// <summary>请求地址。数据接口地址，仅后端可见</summary>
    public String RequestUrl { get; set; }

    /// <summary>请求方式。GET/POST</summary>
    public String Method { get; set; }

    /// <summary>是否分页</summary>
    public Boolean Pageable { get; set; }

    /// <summary>页码字段名。分页时页码参数名</summary>
    public String PageNumField { get; set; }

    /// <summary>页量字段名。分页时每页条数参数名</summary>
    public String PageSizeField { get; set; }

    /// <summary>数据路径。从响应中提取数据列表的JSON路径</summary>
    public String DataPath { get; set; }

    /// <summary>总量路径。从响应中提取总数的JSON路径</summary>
    public String TotalPath { get; set; }

    /// <summary>固定参数。每次请求附加的固定参数，JSON格式</summary>
    public String FixedParams { get; set; }

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
    public void Copy(LovListConfigModel model)
    {
        Id = model.Id;
        LovDefId = model.LovDefId;
        RequestUrl = model.RequestUrl;
        Method = model.Method;
        Pageable = model.Pageable;
        PageNumField = model.PageNumField;
        PageSizeField = model.PageSizeField;
        DataPath = model.DataPath;
        TotalPath = model.TotalPath;
        FixedParams = model.FixedParams;
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
