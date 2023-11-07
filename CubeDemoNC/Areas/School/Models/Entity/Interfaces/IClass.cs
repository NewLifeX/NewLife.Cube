using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace NewLife.School.Entity;

/// <summary>班级</summary>
public partial interface IClass
{
    #region 属性
    /// <summary>编号</summary>
    Int32 Id { get; set; }

    /// <summary>租户</summary>
    Int32 TenantId { get; set; }

    /// <summary>名称</summary>
    String Name { get; set; }

    /// <summary>启用</summary>
    Boolean Enable { get; set; }

    /// <summary>毕业时间</summary>
    DateTime GraduationDate { get; set; }

    /// <summary>设备型号</summary>
    String Model { get; set; }

    /// <summary>创建者</summary>
    Int32 CreateUserID { get; set; }

    /// <summary>创建时间</summary>
    DateTime CreateTime { get; set; }

    /// <summary>创建地址</summary>
    String CreateIP { get; set; }

    /// <summary>更新者</summary>
    Int32 UpdateUserID { get; set; }

    /// <summary>更新时间</summary>
    DateTime UpdateTime { get; set; }

    /// <summary>更新地址</summary>
    String UpdateIP { get; set; }

    /// <summary>备注</summary>
    String Remark { get; set; }
    #endregion
}
