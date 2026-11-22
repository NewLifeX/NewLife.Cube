using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife;
using NewLife.Data;
using NewLife.Reflection;

namespace NewLife.School.Entity;

/// <summary>班级</summary>
public partial class ClassModel : IModel
{
    #region 属性
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>租户</summary>
    public Int32 TenantId { get; set; }

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>启用</summary>
    public Boolean Enable { get; set; }

    /// <summary>毕业时间</summary>
    public DateTime GraduationDate { get; set; }

    /// <summary>班主任</summary>
    public Int32 HeadTeacherId { get; set; }

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

    /// <summary>备注</summary>
    public String Remark { get; set; }
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
                "TenantId" => TenantId,
                "Name" => Name,
                "Enable" => Enable,
                "GraduationDate" => GraduationDate,
                "HeadTeacherId" => HeadTeacherId,
                "CreateUserId" => CreateUserId,
                "CreateTime" => CreateTime,
                "CreateIP" => CreateIP,
                "UpdateUserId" => UpdateUserId,
                "UpdateTime" => UpdateTime,
                "UpdateIP" => UpdateIP,
                "Remark" => Remark,
                _ => this.GetValue(name, false),
            };
        }
        set
        {
            switch (name)
            {
                case "Id": Id = value.ToInt(); break;
                case "TenantId": TenantId = value.ToInt(); break;
                case "Name": Name = Convert.ToString(value); break;
                case "Enable": Enable = value.ToBoolean(); break;
                case "GraduationDate": GraduationDate = value.ToDateTime(); break;
                case "HeadTeacherId": HeadTeacherId = value.ToInt(); break;
                case "CreateUserId": CreateUserId = value.ToInt(); break;
                case "CreateTime": CreateTime = value.ToDateTime(); break;
                case "CreateIP": CreateIP = Convert.ToString(value); break;
                case "UpdateUserId": UpdateUserId = value.ToInt(); break;
                case "UpdateTime": UpdateTime = value.ToDateTime(); break;
                case "UpdateIP": UpdateIP = Convert.ToString(value); break;
                case "Remark": Remark = Convert.ToString(value); break;
                default: this.SetValue(name, value); break;
            }
        }
    }
    #endregion

    #region 拷贝
    /// <summary>拷贝模型对象</summary>
    /// <param name="model">模型</param>
    public void Copy(IClass model)
    {
        Id = model.Id;
        TenantId = model.TenantId;
        Name = model.Name;
        Enable = model.Enable;
        GraduationDate = model.GraduationDate;
        HeadTeacherId = model.HeadTeacherId;
        CreateUserId = model.CreateUserId;
        CreateTime = model.CreateTime;
        CreateIP = model.CreateIP;
        UpdateUserId = model.UpdateUserId;
        UpdateTime = model.UpdateTime;
        UpdateIP = model.UpdateIP;
        Remark = model.Remark;
    }
    #endregion
}
